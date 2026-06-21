internal static class GeneticAlgorithmRunner
{
    private const int DefaultGenerations = 100;
    private const int DefaultPopulationSize = 20;
    private const float DefaultMatchDuration = 60f;
    private const int DefaultSeed = 42;
    private const float TimeStep = 0.1f;
    private const float MutationRate = 0.12f;

    private static readonly int[] MutableSlots = Enumerable.Range(0, 16)
        .Where(index => !CellBlueprint.NucleusIndices.Contains(index))
        .ToArray();

    private static readonly OrganelleType[] GenePool = Enum.GetValues<OrganelleType>()
        .Where(type => type != OrganelleType.Nucleus)
        .ToArray();

    public static void Run(string[] args)
    {
        var generations = ParsePositiveInt(args, 0, DefaultGenerations);
        var populationSize = Math.Max(4, ParsePositiveInt(args, 1, DefaultPopulationSize));
        var matchDuration = ParsePositiveFloat(args, 2, DefaultMatchDuration);
        var seed = ParseInt(args, 3, DefaultSeed);
        var rng = new Random(seed);

        Console.WriteLine("SpaceCrawler Genetic Algorithm");
        Console.WriteLine($"Generations: {generations} | Population: {populationSize} | " +
                          $"Match: {matchDuration:F0}s | Seed: {seed}");
        Console.WriteLine("Fitness: surviving lineage, births, biomass/fuel");
        Console.WriteLine();

        var population = Enumerable.Range(0, populationSize)
            .Select(_ => CreateRandomBlueprint(rng))
            .ToList();

        EvaluatedGenome? bestEver = null;
        List<EvaluatedGenome> finalRanking = [];

        for (var generation = 1; generation <= generations; generation++)
        {
            var generationSeed = unchecked(seed + generation * 7_919);
            var ranking = Evaluate(population, matchDuration, generationSeed)
                .OrderByDescending(result => result.Fitness)
                .ToList();

            finalRanking = ranking;
            var best = ranking[0];
            if (bestEver is null || best.Fitness > bestEver.Fitness)
            {
                bestEver = best with { Blueprint = Clone(best.Blueprint) };
            }

            if (generation == 1 || generation % 10 == 0 || generation == generations)
            {
                var average = ranking.Average(result => result.Fitness);
                Console.WriteLine(
                    $"Generation {generation,3}/{generations}: best={best.Fitness,9:F1} " +
                    $"avg={average,9:F1} alive={best.AliveLineage,2} births={best.Births,2} " +
                    $"blueprint={best.Blueprint.Describe()}");
            }

            if (generation < generations)
            {
                population = BreedNextGeneration(ranking, populationSize, rng);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Final generation - top genomes");
        for (var index = 0; index < Math.Min(5, finalRanking.Count); index++)
        {
            PrintGenome(index + 1, finalRanking[index]);
        }

        Console.WriteLine();
        Console.WriteLine("Best genome observed across all generations");
        PrintGenome(1, bestEver!);
        PrintGrid(bestEver!.Blueprint);
    }

    private static IEnumerable<EvaluatedGenome> Evaluate(
        IReadOnlyList<CellBlueprint> population,
        float matchDuration,
        int seed)
    {
        var simulation = new SimulationEngine(seed: seed, balance: RunnerBalance.Current);
        var positionRng = new Random(unchecked(seed + 1));

        for (var index = 0; index < population.Count; index++)
        {
            var position = new Vec2(
                Lerp(SimConstants.CellHalfSize, SimConstants.ArenaWidth - SimConstants.CellHalfSize,
                    positionRng.NextDouble()),
                Lerp(SimConstants.CellHalfSize, SimConstants.ArenaHeight - SimConstants.CellHalfSize,
                    positionRng.NextDouble()));
            simulation.CreateCell($"Genome-{index + 1}", position, population[index]);
        }

        while (simulation.ElapsedTime < matchDuration && simulation.AliveCellCount > 0)
        {
            simulation.Step(Math.Min(TimeStep, matchDuration - simulation.ElapsedTime));
        }

        foreach (var blueprint in population)
        {
            var lineage = simulation.Cells
                .Where(cell => ReferenceEquals(cell.Blueprint, blueprint))
                .ToList();
            var alive = lineage.Count(cell => cell.Alive);
            var births = Math.Max(0, lineage.Count - 1);
            var duplications = lineage.Sum(cell => cell.DuplicationCount);
            var foodReserve = lineage.Where(cell => cell.Alive).Sum(cell => cell.Food);
            var fitness = alive * 1_000.0 + births * 250.0 + duplications * 100.0 +
                          foodReserve;

            yield return new EvaluatedGenome(
                blueprint, fitness, alive, births, duplications, foodReserve);
        }
    }

    private static List<CellBlueprint> BreedNextGeneration(
        IReadOnlyList<EvaluatedGenome> ranking,
        int populationSize,
        Random rng)
    {
        var parentPool = ranking.Take(Math.Max(2, populationSize / 2)).ToArray();
        var next = new List<CellBlueprint>(populationSize)
        {
            Clone(ranking[0].Blueprint),
            Clone(ranking[1].Blueprint)
        };

        while (next.Count < populationSize)
        {
            var firstParent = TournamentSelect(parentPool, rng).Blueprint;
            var secondParent = TournamentSelect(parentPool, rng).Blueprint;
            next.Add(Mutate(Crossover(firstParent, secondParent, rng), rng));
        }

        return next;
    }

    private static EvaluatedGenome TournamentSelect(IReadOnlyList<EvaluatedGenome> pool, Random rng)
    {
        var best = pool[rng.Next(pool.Count)];
        for (var round = 1; round < 3; round++)
        {
            var challenger = pool[rng.Next(pool.Count)];
            if (challenger.Fitness > best.Fitness)
            {
                best = challenger;
            }
        }

        return best;
    }

    private static CellBlueprint CreateRandomBlueprint(Random rng)
    {
        var grid = CreateBaseGrid();
        var slots = MutableSlots.OrderBy(_ => rng.Next()).ToArray();
        var organelleCount = rng.Next(2, 9);
        for (var index = 0; index < organelleCount; index++)
        {
            grid[slots[index]] = RandomNonEmptyGene(rng);
        }

        return new CellBlueprint(grid);
    }

    private static CellBlueprint Crossover(CellBlueprint first, CellBlueprint second, Random rng)
    {
        var grid = CreateBaseGrid();
        foreach (var slot in MutableSlots)
        {
            grid[slot] = rng.Next(2) == 0 ? first.Grid[slot] : second.Grid[slot];
        }

        return new CellBlueprint(grid);
    }

    private static CellBlueprint Mutate(CellBlueprint blueprint, Random rng)
    {
        var grid = (OrganelleType[])blueprint.Grid.Clone();
        foreach (var slot in MutableSlots)
        {
            if (rng.NextDouble() < MutationRate)
            {
                grid[slot] = GenePool[rng.Next(GenePool.Length)];
            }
        }

        return new CellBlueprint(grid);
    }

    private static CellBlueprint Clone(CellBlueprint blueprint) =>
        new((OrganelleType[])blueprint.Grid.Clone(), blueprint.Connections);

    private static OrganelleType[] CreateBaseGrid()
    {
        var grid = new OrganelleType[16];
        foreach (var index in CellBlueprint.NucleusIndices)
        {
            grid[index] = OrganelleType.Nucleus;
        }

        return grid;
    }

    private static OrganelleType RandomNonEmptyGene(Random rng)
    {
        OrganelleType gene;
        do
        {
            gene = GenePool[rng.Next(GenePool.Length)];
        } while (gene == OrganelleType.Empty);

        return gene;
    }

    private static void PrintGenome(int rank, EvaluatedGenome genome)
    {
        Console.WriteLine(
            $"#{rank}: fitness={genome.Fitness:F1}, alive={genome.AliveLineage}, " +
            $"births={genome.Births}, biomass={genome.FoodReserve:F1}");
        Console.WriteLine($"    {genome.Blueprint.Describe()}");
    }

    private static void PrintGrid(CellBlueprint blueprint)
    {
        Console.WriteLine("Grid:");
        for (var row = 0; row < 4; row++)
        {
            Console.WriteLine("  " + string.Join(" | ", blueprint.Grid
                .Skip(row * 4)
                .Take(4)
                .Select(type => type.SerializedName())));
        }
    }

    private static int ParsePositiveInt(string[] args, int index, int fallback) =>
        index < args.Length && int.TryParse(args[index], out var value) && value > 0
            ? value
            : fallback;

    private static int ParseInt(string[] args, int index, int fallback) =>
        index < args.Length && int.TryParse(args[index], out var value) ? value : fallback;

    private static float ParsePositiveFloat(string[] args, int index, float fallback) =>
        index < args.Length && float.TryParse(args[index], out var value) && value > 0
            ? value
            : fallback;

    private static float Lerp(float min, float max, double amount) =>
        min + (max - min) * (float)amount;

    private sealed record EvaluatedGenome(
        CellBlueprint Blueprint,
        double Fitness,
        int AliveLineage,
        int Births,
        int Duplications,
        float FoodReserve);
}
