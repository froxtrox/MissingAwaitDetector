using MissingAwaitDetector;

namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT004_FireAndForget
{
    private static Task ProcessAsync() => Task.CompletedTask;
    private static Task<int> GetNumberAsync() => Task.FromResult(3);

    // [FireAndForget] on the callee opts it out of MAWT004 at all call sites
    [FireAndForget("Background telemetry — exceptions are intentionally swallowed")]
    private static Task SendTelemetryAsync() => Task.CompletedTask;

    // ── BAD EXAMPLES — MAWT004 squiggles appear here ──────────────────────

    public static void BadExample_SimpleDiscard()
    {
        ProcessAsync();
    }

    public static void BadExample_GenericTaskDiscard()
    {
        GetNumberAsync();
    }

    public static void BadExample_MemberAccess()
    {
        var svc = new SampleService();
        svc.ProcessAsync();
    }

    public static void BadExample_ConditionalAccess()
    {
        SampleService? svc = new SampleService();
        svc?.ProcessAsync();
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static async Task GoodExamplesAsync()
    {
        await ProcessAsync();
        var t = ProcessAsync(); await t;
        _ = ProcessAsync();

#pragma warning disable CS4014
        SendTelemetryAsync();
#pragma warning restore CS4014
    }
}

public class SampleService
{
    public Task ProcessAsync() => Task.CompletedTask;
}
