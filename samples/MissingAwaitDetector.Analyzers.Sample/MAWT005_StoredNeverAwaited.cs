namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT005_StoredNeverAwaited
{
    private static Task<int> GetNumberAsync() => Task.FromResult(3);
    private static Task DoWorkAsync() => Task.CompletedTask;

    // ── BAD EXAMPLES — MAWT005 squiggles appear here ──────────────────────

    public static void BadExample_StoredNeverUsed()
    {
        var task = GetNumberAsync();
        Console.WriteLine("Didn't wait for it.");
    }

    public static void BadExample_NonGenericStoredNeverUsed()
    {
        var task = DoWorkAsync();
        Console.WriteLine("Work may not have finished.");
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static async Task GoodExamplesAsync()
    {
        var task = GetNumberAsync();
        int result = await task;
        Console.WriteLine(result);

        var t1 = GetNumberAsync();
        var t2 = GetNumberAsync();
        var results = await Task.WhenAll(t1, t2);
        Console.WriteLine(string.Join(", ", results));

        var tasks = new List<Task<int>>();
        var t3 = GetNumberAsync();
        tasks.Add(t3);                    // Add() counts as "handled"
        Console.WriteLine(await Task.WhenAll(tasks));
    }

    public static Task<int> GoodExample_StoreAndReturn()
    {
        var task = GetNumberAsync();
        return task;
    }
}
