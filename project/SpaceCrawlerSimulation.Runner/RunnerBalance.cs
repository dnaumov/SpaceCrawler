internal static class RunnerBalance
{
    public static SimulationBalance Current { get; } = SimulationBalance.LoadFromDirectory(
        Path.Combine(AppContext.BaseDirectory, "balance"),
        message => Console.Error.WriteLine($"Warning: {message}"));
}
