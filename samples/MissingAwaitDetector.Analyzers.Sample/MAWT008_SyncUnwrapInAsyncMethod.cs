using MissingAwaitDetector;

namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT008_SyncUnwrapInAsyncMethod
{
    private static Task<int> GetNumberAsync() => Task.FromResult(3);
    private static Task DoWorkAsync() => Task.CompletedTask;

    // ── BAD EXAMPLES — MAWT008 squiggles appear here ──────────────────────

    public static async Task BadExample_ResultInAsyncMethod()
    {
        int result = GetNumberAsync().Result;
        Console.WriteLine(result);
        await Task.CompletedTask;
    }

    public static async Task BadExample_WaitInAsyncMethod()
    {
        var task = DoWorkAsync();
        task.Wait();
        await Task.CompletedTask;
    }

    public static async Task BadExample_GetAwaiterGetResultInAsyncMethod()
    {
        int result = GetNumberAsync().GetAwaiter().GetResult();
        Console.WriteLine(result);
        await Task.CompletedTask;
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static async Task GoodExamplesAsync()
    {
        int result = await GetNumberAsync();
        Console.WriteLine(result);
        await DoWorkAsync();
    }

    [AllowSynchronousIO("Bridging legacy sync interface — reviewed")]
    public static async Task LegacyBridgeAsync()
    {
        int result = GetNumberAsync().GetAwaiter().GetResult();
        Console.WriteLine(result);
        await Task.CompletedTask;
    }
}
