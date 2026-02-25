namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT006_ValueTaskMisuse
{
    private static ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);
    private static Task DoOtherWorkAsync() => Task.CompletedTask;

    // ── BAD EXAMPLES — MAWT006 squiggles appear here ──────────────────────

    public static async Task BadExample_MultipleAwaits()
    {
        var vt = GetNumberAsync();
        int r1 = await vt;
        int r2 = await vt;
        Console.WriteLine(r1 + r2);
    }

    public static async Task BadExample_DelayedAwait()
    {
        var vt = GetNumberAsync();
        await DoOtherWorkAsync();
        int result = await vt;
        Console.WriteLine(result);
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static async Task GoodExamplesAsync()
    {
        int r1 = await GetNumberAsync();
        Console.WriteLine(r1);

        var vt = GetNumberAsync();
        int r2 = await vt;
        Console.WriteLine(r2);

        // Store the unwrapped int — not the ValueTask — when you need multiple uses
        int value = await GetNumberAsync();
        Console.WriteLine(value);
        Console.WriteLine(value);
    }
}
