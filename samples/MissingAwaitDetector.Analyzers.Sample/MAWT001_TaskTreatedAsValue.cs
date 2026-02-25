namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT001_TaskTreatedAsValue
{
    private static Task<int> GetNumberAsync() => Task.FromResult(3);
    private static Task DoWorkAsync() => Task.CompletedTask;

    // ── BAD EXAMPLES — MAWT001 squiggles appear here ──────────────────────

    public static void BadExample_StringInterpolation()
    {

        var task = GetNumberAsync();
        Console.WriteLine($"Result: {task}");
    }

    public static void BadExample_ToString()
    {
        var task = GetNumberAsync();
        string? s = task.ToString();
        Console.WriteLine(s);
    }

    public static void BadExample_GetHashCode()
    {
        var task = GetNumberAsync();
        int h = task.GetHashCode();
        Console.WriteLine(h);
    }

    public static void BadExample_NonGenericToString()
    {
        var task = DoWorkAsync();
        string? s = task.ToString();
        Console.WriteLine(s);
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static async Task GoodExamplesAsync()
    {
        int result = await GetNumberAsync();
        Console.WriteLine($"Result: {result}");
        Console.WriteLine(result.ToString());
        Console.WriteLine(result.GetHashCode());
        await DoWorkAsync();
        Console.WriteLine("Work done.");
    }
}
