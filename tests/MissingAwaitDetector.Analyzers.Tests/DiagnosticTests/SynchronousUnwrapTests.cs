using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.DiagnosticTests
{
    public class SynchronousUnwrapTests
    {
        [Fact]
        public async Task TaskResult_Reports_MAWT002()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        var result = {|#0:task.Result|};
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskWait_Reports_MAWT002()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    void M()
    {
        var task = DoWorkAsync();
        {|#0:task.Wait()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Wait()");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task GetAwaiterGetResult_Reports_MAWT002()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var result = {|#0:GetNumberAsync().GetAwaiter().GetResult()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("GetAwaiter().GetResult()");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task SyncUnwrapInAsyncMethod_Reports_MAWT008()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var result = {|#0:GetNumberAsync().Result|};
        await Task.CompletedTask;
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SyncUnwrapInAsyncMethod)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task MainMethod_GetAwaiterGetResult_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class Program
{
    static Task<int> GetNumberAsync() => Task.FromResult(3);

    static void Main()
    {
        var result = GetNumberAsync().GetAwaiter().GetResult();
    }
}";

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task AllowSynchronousIOAttribute_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace MissingAwaitDetector
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AllowSynchronousIOAttribute : Attribute
    {
        public string Reason { get; }
        public AllowSynchronousIOAttribute(string reason = null) { Reason = reason; }
    }
}

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    [MissingAwaitDetector.AllowSynchronousIO(""Legacy"")]
    void M()
    {
        var result = GetNumberAsync().GetAwaiter().GetResult();
    }
}";

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task AwaitedTask_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var result = await GetNumberAsync();
    }
}";

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ResultInsideContinueWith_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        GetNumberAsync().ContinueWith(t => Console.WriteLine(t.Result));
    }
}";

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task TaskWaitWithTimeout_Reports_MAWT002()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    void M()
    {
        var task = DoWorkAsync();
        {|#0:task.Wait(5000)|};
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Wait()");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskWaitWithCancellationToken_Reports_MAWT002()
        {
            var source = @"
using System.Threading;
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    void M()
    {
        var task = DoWorkAsync();
        {|#0:task.Wait(CancellationToken.None)|};
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Wait()");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ValueTaskResult_Reports_MAWT002()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    void M()
    {
        var vt = GetNumberAsync();
        var result = {|#0:vt.Result|};
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ResultInLambda_Reports_MAWT002()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        Action a = () =>
        {
            var x = {|#0:GetNumberAsync().Result|};
        };
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task WaitInAsyncMethod_Reports_MAWT008()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    async Task M()
    {
        var task = DoWorkAsync();
        {|#0:task.Wait()|};
        await Task.CompletedTask;
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SyncUnwrapInAsyncMethod)
                .WithLocation(0)
                .WithArguments("Wait()");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task GetAwaiterGetResultInAsyncMethod_Reports_MAWT008()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var result = {|#0:GetNumberAsync().GetAwaiter().GetResult()|};
        await Task.CompletedTask;
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SyncUnwrapInAsyncMethod)
                .WithLocation(0)
                .WithArguments("GetAwaiter().GetResult()");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ChainedResult_OnNestedTask_Reports_MAWT002()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<Task<int>> GetNestedAsync() => Task.FromResult(Task.FromResult(3));

    void M()
    {
        var outer = {|#0:GetNestedAsync().Result|};
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ResultInAsyncLambda_Reports_MAWT002()
        {
            // Lambda breaks GetContainingMethod, so it returns null → fires MAWT002, not MAWT008
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        Func<Task> f = async () =>
        {
            var x = {|#0:GetNumberAsync().Result|};
            await Task.CompletedTask;
        };
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task MainMethodWithStringArgs_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class Program
{
    static Task<int> GetNumberAsync() => Task.FromResult(3);

    static void Main(string[] args)
    {
        var result = GetNumberAsync().GetAwaiter().GetResult();
    }
}";

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task AllowSynchronousIOOnAsyncMethod_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace MissingAwaitDetector
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AllowSynchronousIOAttribute : Attribute
    {
        public string Reason { get; }
        public AllowSynchronousIOAttribute(string reason = null) { Reason = reason; }
    }
}

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    [MissingAwaitDetector.AllowSynchronousIO(""Legacy"")]
    async Task M()
    {
        var result = GetNumberAsync().Result;
        await Task.CompletedTask;
    }
}";

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ResultInsideNestedContinueWith_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        GetNumberAsync().ContinueWith(t =>
        {
            t.ContinueWith(inner => Console.WriteLine(inner.Result));
        });
    }
}";

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ResultOnDirectMethodCall_Reports_MAWT002()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var x = {|#0:GetNumberAsync().Result|};
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task GetAwaiterGetResultOnValueTask_Reports_MAWT002()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    void M()
    {
        var x = {|#0:GetNumberAsync().GetAwaiter().GetResult()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("GetAwaiter().GetResult()");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ResultInAsyncLocalFunction_Reports_MAWT008()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        async Task Inner()
        {
            var x = {|#0:GetNumberAsync().Result|};
        }
    }
}";

            var expected = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SyncUnwrapInAsyncMethod)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task MultipleUnwrapsInSameMethod_Reports_MultipleDiagnostics()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var x = {|#0:GetNumberAsync().Result|};
        {|#1:GetNumberAsync().Wait()|};
    }
}";

            var expected1 = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            var expected2 = CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(1)
                .WithArguments("Wait()");

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task GetResultWithoutGetAwaiter_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    int GetResult() => 42;

    void M()
    {
        var x = GetResult();
    }
}";

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ResultOnNonTaskType_NoDiagnostic()
        {
            var source = @"
class Wrapper
{
    public int Result => 42;
}

class C
{
    void M()
    {
        var w = new Wrapper();
        var x = w.Result;
    }
}";

            await CSharpAnalyzerVerifier<SynchronousUnwrapAnalyzer>.VerifyNoDiagnosticAsync(source);
        }
    }
}
