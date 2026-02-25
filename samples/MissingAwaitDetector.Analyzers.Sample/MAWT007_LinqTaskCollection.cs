namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT007_LinqTaskCollection
{
    private static Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    // ── BAD EXAMPLES — MAWT007 squiggles appear here ──────────────────────

    public static void BadExample_SelectToList()
    {
        var items = new List<int> { 1, 2, 3 };
        var tasks = items.Select(x => ProcessAsync(x)).ToList();
        Console.WriteLine($"Started {tasks.Count} tasks (results unobserved)");
    }

    public static void BadExample_SelectToArray()
    {
        var items = new List<int> { 4, 5, 6 };
        var tasks = items.Select(x => ProcessAsync(x)).ToArray();
        Console.WriteLine($"Got {tasks.Length} tasks");
    }

    public static void BadExample_SelectNotMaterialized()
    {
        var items = new List<int> { 7, 8 };
        var taskSeq = items.Select(x => ProcessAsync(x));
        Console.WriteLine(taskSeq.GetType().Name);
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static async Task GoodExamplesAsync()
    {
        var items = new List<int> { 1, 2, 3 };

        int[] results = await Task.WhenAll(items.Select(x => ProcessAsync(x)));
        Console.WriteLine(string.Join(", ", results));

        int[] results2 = await Task.WhenAll(items.Select(x => ProcessAsync(x)).ToList());
        Console.WriteLine(string.Join(", ", results2));

        var doubled = items.Select(x => x * 2).ToList();
        Console.WriteLine(string.Join(", ", doubled));
    }
}
