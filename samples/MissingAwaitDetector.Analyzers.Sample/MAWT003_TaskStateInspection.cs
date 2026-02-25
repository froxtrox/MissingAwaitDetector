namespace MissingAwaitDetector.Analyzers.Sample;

public static class MAWT003_TaskStateInspection
{
    private static Task<int> GetNumberAsync() => Task.FromResult(3);

    // ── BAD EXAMPLES — MAWT003 squiggles appear here ──────────────────────

    public static void BadExample_IsCompleted()
    {
        var task = GetNumberAsync();
        if (task.IsCompleted) Console.WriteLine("Done");
    }

    public static void BadExample_Status()
    {
        var task = GetNumberAsync();
        Console.WriteLine(task.Status);
    }

    public static void BadExample_IsFaulted()
    {
        var task = GetNumberAsync();
        if (task.IsFaulted) Console.WriteLine("Faulted");
    }

    public static void BadExample_IsCanceled()
    {
        var task = GetNumberAsync();
        if (task.IsCanceled) Console.WriteLine("Canceled");
    }

#if NET5_0_OR_GREATER
    public static void BadExample_IsCompletedSuccessfully()
    {
        var task = GetNumberAsync();
        if (task.IsCompletedSuccessfully) Console.WriteLine("Success");
    }
#endif

    public static void BadExample_Exception()
    {
        var task = GetNumberAsync();
        Console.WriteLine(task.Exception?.Message);
    }

    public static void BadExample_Id()
    {
        var task = GetNumberAsync();
        Console.WriteLine($"Task id: {task.Id}");
    }

    public static void BadExample_CurrentId()
    {
        Console.WriteLine($"Current task id: {Task.CurrentId}");
    }

    public static void BadExample_AsyncState()
    {
        var task = GetNumberAsync();
        Console.WriteLine(task.AsyncState);
    }

    public static void BadExample_CreationOptions()
    {
        var task = GetNumberAsync();
        Console.WriteLine(task.CreationOptions);
    }

    // ── GOOD EXAMPLES — no squiggles ───────────────────────────────────────

    public static async Task GoodExamplesAsync()
    {
        try
        {
            int result = await GetNumberAsync();
            Console.WriteLine($"Result: {result}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Canceled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Faulted: {ex.Message}");
        }

        // Use Activity.Current or Guid for correlation — not Task.Id
        var correlationId = Guid.NewGuid();
        Console.WriteLine($"Correlation id: {correlationId}");
    }
}
