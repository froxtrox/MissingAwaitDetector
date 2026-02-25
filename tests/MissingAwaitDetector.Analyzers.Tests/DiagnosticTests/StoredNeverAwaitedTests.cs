using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.DiagnosticTests
{
    public class StoredNeverAwaitedTests
    {
        [Fact]
        public async Task TaskStoredNeverAwaited_Reports_MAWT005()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var {|#0:task = GetNumberAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(0)
                .WithArguments("task");

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskStoredAndAwaited_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var task = GetNumberAsync();
        var result = await task;
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskStoredAndReturned_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    Task<int> M()
    {
        var task = GetNumberAsync();
        return task;
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskStoredAndComposed_WhenAll_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var task1 = GetNumberAsync();
        var task2 = GetNumberAsync();
        await Task.WhenAll(task1, task2);
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task NonTaskVariable_NoDiagnostic()
        {
            var source = @"
class C
{
    void M()
    {
        var x = 42;
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task MultipleTasks_OneAwaited_OneNot_Reports_MAWT005()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var task1 = GetNumberAsync();
        var {|#0:task2 = GetNumberAsync()|};
        await task1;
    }
}";

            var expected = CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(0)
                .WithArguments("task2");

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskComposedWithWhenAny_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var task1 = GetNumberAsync();
        var task2 = GetNumberAsync();
        await Task.WhenAny(task1, task2);
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskPassedToContinueWith_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        task.ContinueWith(t => Console.WriteLine(t.Result));
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskInCollectionInitializer_NoDiagnostic()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        var tasks = new List<Task<int>> { task };
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskStoredInImplicitArray_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        var arr = new[] { task };
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ValueTaskStoredNeverAwaited_Reports_MAWT005()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    void M()
    {
        var {|#0:vt = GetNumberAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(0)
                .WithArguments("vt");

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskWithConfigureAwait_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var task = GetNumberAsync();
        var result = await task.ConfigureAwait(false);
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskInLocalFunction_StoredNeverAwaited_Reports_MAWT005()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        void LocalFunc()
        {
            var {|#0:task = GetNumberAsync()|};
        }
        LocalFunc();
    }
}";

            var expected1 = CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(0)
                .WithArguments("task");

            var expected2 = CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(0)
                .WithArguments("task");

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task TaskStoredAndPassedToListAdd_NoDiagnostic()
        {
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var tasks = new List<Task<int>>();
        var task = GetNumberAsync();
        tasks.Add(task);
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ExplicitDiscardVariable_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var _ = GetNumberAsync();
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task MultipleTaskVariables_AllNeverAwaited_Reports_MultipleDiagnostics()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);
    Task ProcessAsync() => Task.CompletedTask;

    void M()
    {
        var {|#0:task1 = GetNumberAsync()|};
        var {|#1:task2 = ProcessAsync()|};
    }
}";

            var expected1 = CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(0)
                .WithArguments("task1");

            var expected2 = CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(1)
                .WithArguments("task2");

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task TaskPassedToWaitAll_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        Task.WaitAll(task);
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskUsedInContinueWith_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        task.ContinueWith(t => Console.WriteLine(t.Result));
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskStoredInArray_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        var tasks = new[] { task };
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskReturned_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    Task<int> M()
    {
        var task = GetNumberAsync();
        return task;
    }
}";

            await CSharpAnalyzerVerifier<StoredNeverAwaitedAnalyzer>.VerifyNoDiagnosticAsync(source);
        }
    }
}
