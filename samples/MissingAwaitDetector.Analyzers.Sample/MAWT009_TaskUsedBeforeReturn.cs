namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT009_TaskUsedBeforeReturn
{
    private static Task<int> CalculateAsync() => Task.FromResult(3);
    private static Task DoWorkAsync() => Task.CompletedTask;

    // ── BAD EXAMPLES — MAWT009 info squiggles appear here ─────────────────

    public static Task<int> BadExample_InspectedBeforeReturn()
    {
        var task = CalculateAsync();
        Console.WriteLine(task);
        return task;
    }

    public static Task BadExample_StateCheckedBeforeReturn()
    {
        var task = DoWorkAsync();
        _ = task.IsCompleted;
        return task;
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static Task<int> GoodExample_DirectReturn() => CalculateAsync();

    public static Task<int> GoodExample_StoreAndReturn()
    {
        var task = CalculateAsync();
        return task;
    }

    public static async Task GoodExamplesAsync()
    {
        int result = await CalculateAsync();
        Console.WriteLine(result);
    }
}
