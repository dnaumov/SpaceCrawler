using SpaceCrawlerSimulation;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │  SpaceCrawler – Standalone Simulation Console App                          │
// │                                                                             │
// │  Runs a full game simulation from the command line.                         │
// │  Usage:  SpaceCrawlerSimulation [durationSeconds] [aiCount] [seed]          │
// │  Example: SpaceCrawlerSimulation 120 3 42                                   │
// └─────────────────────────────────────────────────────────────────────────────┘

var matchDuration = args.Length >= 1 && float.TryParse(args[0], out var d) ? d : 120f;
var aiCount       = args.Length >= 2 && int.TryParse(args[1], out var n)   ? n : 3;
var seed          = args.Length >= 3 && int.TryParse(args[2], out var s)   ? s : 42;

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║           SpaceCrawler  Simulation               ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine($"  Arena : {SimConstants.ArenaWidth}×{SimConstants.ArenaHeight} S-units");
Console.WriteLine($"  Match : {matchDuration}s  |  AI cells: {aiCount}  |  Seed: {seed}");
Console.WriteLine($"  Rules : T={SimConstants.TickInterval}s, C={SimConstants.PassiveFoodDrain}/T, " +
                  $"death<{SimConstants.NegativeFoodBase} food");
Console.WriteLine();

var sim = new SimulationEngine(seed: seed);

// ── player cell (simple food-sensor + effective-engine blueprint) ──────────────
var playerGrid = new OrganelleType[16];
foreach (var idx in CellBlueprint.NucleusIndices)
{
    playerGrid[idx] = OrganelleType.Nucleus;
}
playerGrid[0]  = OrganelleType.FoodGradientDetector;
playerGrid[1]  = OrganelleType.EffectiveEngine;
playerGrid[14] = OrganelleType.EffectiveEngine;
playerGrid[15] = OrganelleType.Mitochondria;
var playerBp = new CellBlueprint(playerGrid);

sim.CreateCell("Player", new Vec2(SimConstants.ArenaWidth * 0.5f, SimConstants.ArenaHeight * 0.5f), playerBp);
Console.WriteLine($"  Player blueprint: {playerBp.Describe()}");
Console.WriteLine($"  Player elements: {playerBp.ElementCount}, duplicates at: {playerBp.FoodForDuplication} food");

// ── AI cells (random blueprints) ──────────────────────────────────────────────
var rng = new Random(seed + 1);
var aiOrganellePool = new[]
{
    OrganelleType.RandomEngine,
    OrganelleType.EffectiveEngine,
    OrganelleType.Engine,
    OrganelleType.Mitochondria,
    OrganelleType.FoodGradientDetector,
    OrganelleType.SlipperyMembrane
};

for (var i = 0; i < aiCount; i++)
{
    var grid = new OrganelleType[16];
    foreach (var idx in CellBlueprint.NucleusIndices)
    {
        grid[idx] = OrganelleType.Nucleus;
    }

    var freeSlots = Enumerable.Range(0, 16)
        .Where(x => !CellBlueprint.NucleusIndices.Contains(x))
        .ToList();
    var orgCount = rng.Next(2, 9);
    for (var k = 0; k < orgCount && freeSlots.Count > 0; k++)
    {
        var slotIdx = rng.Next(freeSlots.Count);
        grid[freeSlots[slotIdx]] = aiOrganellePool[rng.Next(aiOrganellePool.Length)];
        freeSlots.RemoveAt(slotIdx);
    }

    var bp  = new CellBlueprint(grid);
    var pos = new Vec2((float)rng.NextDouble() * SimConstants.ArenaWidth,
                       (float)rng.NextDouble() * SimConstants.ArenaHeight);
    var cell = sim.CreateCell($"AI-{i + 1}", pos, bp);
    Console.WriteLine($"  AI-{i + 1} blueprint: {bp.Describe()} (dup@{bp.FoodForDuplication})");
}

Console.WriteLine();

// ── run simulation ────────────────────────────────────────────────────────────
const float timeStep     = 0.1f;   // 100 ms logical steps
const float reportEvery  = 10f;    // print state every T seconds

var elapsed     = 0f;
var nextReport  = reportEvery;

Console.WriteLine("──────── Simulation Start ────────");

while (elapsed < matchDuration && sim.AliveCellCount > 0)
{
    sim.Step(timeStep);
    elapsed += timeStep;

    if (elapsed >= nextReport)
    {
        nextReport += reportEvery;
        Console.WriteLine($"\n[t={elapsed:F0}s]  alive={sim.AliveCellCount}  food-items={sim.Foods.Count}");
        foreach (var cell in sim.Cells.OrderByDescending(c => c.FoodCollectedForDup))
        {
            if (!cell.Alive)
            {
                Console.WriteLine($"  {cell.Name,-10} DEAD");
                continue;
            }

            Console.WriteLine($"  {cell.Name,-10} food={cell.Food,6:F1}  dupFood={cell.FoodCollectedForDup}/{cell.Blueprint.FoodForDuplication}  dups={cell.DuplicationCount}  pos={cell.Position}");
        }
    }
}

// ── results ───────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("──────── Match Ended ────────");
Console.WriteLine(elapsed < matchDuration ? $"All cells died at t={elapsed:F1}s." : $"Time limit reached ({matchDuration}s).");
Console.WriteLine();

var winner = sim.GetWinner();
if (winner is null)
{
    Console.WriteLine("No winner.");
}
else
{
    Console.WriteLine($"🏆  Winner: {winner.Name}");
    Console.WriteLine($"   Food reserve  : {winner.Food:F1}");
    Console.WriteLine($"   Dup food coll : {winner.FoodCollectedForDup}");
    Console.WriteLine($"   Duplications  : {winner.DuplicationCount}");
    Console.WriteLine($"   Blueprint     : {winner.Blueprint.Describe()}");
}

Console.WriteLine();
Console.WriteLine("──────── Final Standings ────────");
foreach (var cell in sim.Cells.OrderByDescending(c => c.DuplicationCount)
                               .ThenByDescending(c => c.Food))
{
    var status = cell.Alive ? "alive" : "dead";
    Console.WriteLine($"  {cell.Name,-10} {status,-5} dups={cell.DuplicationCount}  food={cell.Food:F1}  elements={cell.Blueprint.ElementCount}");
}
