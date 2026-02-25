using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.DiagnosticTests
{
    public class ValueTaskMisuseTests
    {
        [Fact]
        public async Task ValueTaskAwaitedMultipleTimes_Reports_MAWT006()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt = GetNumberAsync();
        var r1 = await vt;
        var r2 = {|#0:await vt|};
    }
}";

            var expected = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(0)
                .WithArguments("awaited multiple times");

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ValueTaskAwaitedAfterOtherAsync_Reports_MAWT006()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);
    Task DoOtherWorkAsync() => Task.CompletedTask;

    async Task M()
    {
        var vt = GetNumberAsync();
        await DoOtherWorkAsync();
        var result = {|#0:await vt|};
    }
}";

            var expected = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(0)
                .WithArguments("awaited after another asynchronous operation (must be awaited immediately)");

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ValueTaskAwaitedImmediately_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var result = await GetNumberAsync();
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ValueTaskStoredAndAwaitedImmediately_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt = GetNumberAsync();
        var result = await vt;
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task RegularTaskMultipleAwaits_NoDiagnostic()
        {
            // Regular Task (not ValueTask) - multiple awaits is fine
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var task = GetNumberAsync();
        var r1 = await task;
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task NonGenericValueTaskAwaitedMultipleTimes_Reports_MAWT006()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask DoWorkAsync() => default;

    async Task M()
    {
        var vt = DoWorkAsync();
        await vt;
        {|#0:await vt|};
    }
}";

            var expected = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(0)
                .WithArguments("awaited multiple times");

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ValueTaskAwaitedThreeTimes_Reports_TwoDiagnostics()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt = GetNumberAsync();
        var r1 = await vt;
        var r2 = {|#0:await vt|};
        var r3 = {|#1:await vt|};
    }
}";

            var expected1 = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(0)
                .WithArguments("awaited multiple times");

            var expected2 = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(1)
                .WithArguments("awaited multiple times");

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task ValueTaskWithConfigureAwait_AwaitedAfterOtherAsync_Reports_MAWT006()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);
    Task DoOtherWorkAsync() => Task.CompletedTask;

    async Task M()
    {
        var vt = GetNumberAsync();
        await DoOtherWorkAsync();
        var result = {|#0:await vt.ConfigureAwait(false)|};
    }
}";

            var expected = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(0)
                .WithArguments("awaited after another asynchronous operation (must be awaited immediately)");

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ValueTaskImmediatelyAwaitedWithConfigureAwait_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt = GetNumberAsync();
        var result = await vt.ConfigureAwait(false);
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TwoValueTaskVariables_IndependentTracking_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt1 = GetNumberAsync();
        var r1 = await vt1;
        var vt2 = GetNumberAsync();
        var r2 = await vt2;
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ValueTaskCreatedAndReturnedNotAwaited_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    ValueTask<int> M()
    {
        var vt = GetNumberAsync();
        return vt;
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ValueTaskInSeparateMethod_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M1()
    {
        var vt = GetNumberAsync();
        var r = await vt;
    }

    async Task M2()
    {
        var vt = GetNumberAsync();
        var r = await vt;
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task RegularTask_AwaitedAfterOtherAsync_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);
    Task DoOtherWorkAsync() => Task.CompletedTask;

    async Task M()
    {
        var task = GetNumberAsync();
        await DoOtherWorkAsync();
        var result = await task;
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ValueTaskAwaitedAfterSyncOperation_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt = GetNumberAsync();
        var x = 42;
        var result = await vt;
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ValueTaskAwaitedInBothIfBranches_Reports_MAWT006()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt = GetNumberAsync();
        if (true)
            await vt;
        else
            {|#0:await vt|};
    }
}";

            var expected = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(0)
                .WithArguments("awaited multiple times");

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task GenericValueTaskOfString_AwaitedMultipleTimes_Reports_MAWT006()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<string> GetNameAsync() => new ValueTask<string>(""hello"");

    async Task M()
    {
        var vt = GetNameAsync();
        var a = await vt;
        var b = {|#0:await vt|};
    }
}";

            var expected = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(0)
                .WithArguments("awaited multiple times");

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TwoValueTasks_OnlySecondMisused_Reports_MAWT006()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt1 = GetNumberAsync();
        var vt2 = GetNumberAsync();
        var a = await vt1;
        var b = {|#0:await vt2|};
        var c = {|#1:await vt2|};
    }
}";

            var expected1 = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(0)
                .WithArguments("awaited after another asynchronous operation (must be awaited immediately)");

            var expected2 = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(1)
                .WithArguments("awaited multiple times");

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task NonGenericValueTask_AwaitedTwice_Reports_MAWT006()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask DoWorkAsync() => default;

    async Task M()
    {
        var vt = DoWorkAsync();
        await vt;
        {|#0:await vt|};
    }
}";

            var expected = CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>
                .Diagnostic(DiagnosticIds.ValueTaskMisuse)
                .WithLocation(0)
                .WithArguments("awaited multiple times");

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ValueTaskAwaitedOnce_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt = GetNumberAsync();
        var result = await vt;
    }
}";

            await CSharpAnalyzerVerifier<ValueTaskMisuseAnalyzer>.VerifyNoDiagnosticAsync(source);
        }
    }
}
