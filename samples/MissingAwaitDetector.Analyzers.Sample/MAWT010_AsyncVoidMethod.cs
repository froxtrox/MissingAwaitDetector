namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT010_AsyncVoidMethod
{
    // ── BAD EXAMPLES — MAWT010 squiggles appear here ──────────────────────

    public static void BadExample_AsyncVoidLocalFunction()
    {
        async void LocalAsync()
        {
            await Task.Delay(1);
        }

        LocalAsync();
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static async Task GoodExamplesAsync()
    {
        await GoodExample_AsyncTask();
    }

    public static async Task GoodExample_AsyncTask()
    {
        await Task.Delay(1);
    }

    // Event handler pattern — async void is allowed
    public static async void GoodExample_EventHandler(object sender, EventArgs e)
    {
        await Task.Delay(1);
    }
}
