using MissingAwaitDetector;

namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT002_SynchronousUnwrap
{
    private static Task<int> GetNumberAsync() => Task.FromResult(3);
    private static Task DoWorkAsync() => Task.CompletedTask;

    // ── BAD EXAMPLES — MAWT002 squiggles appear here ──────────────────────

    public static void BadExample_ResultProperty()
    {
        int result = GetNumberAsync().Result;
        Console.WriteLine(result);
    }

    public static void BadExample_WaitMethod()
    {
        DoWorkAsync().Wait();
    }

    public static void BadExample_GetAwaiterGetResult()
    {
        int result = GetNumberAsync().GetAwaiter().GetResult();
        Console.WriteLine(result);
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static async Task GoodExamplesAsync()
    {
        int result = await GetNumberAsync();
        Console.WriteLine(result);
        await DoWorkAsync();
    }

    [AllowSynchronousIO("Legacy sync interface — reviewed")]
    public static void LegacyAdapter()
    {
        int result = GetNumberAsync().GetAwaiter().GetResult();
        Console.WriteLine(result);
    }

}
