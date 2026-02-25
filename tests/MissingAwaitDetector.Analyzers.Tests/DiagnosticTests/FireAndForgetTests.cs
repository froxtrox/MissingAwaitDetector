using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.DiagnosticTests
{
    public class FireAndForgetTests
    {
        [Fact]
        public async Task TaskReturningMethodNotAwaited_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    void M()
    {
        {|#0:ProcessAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task GenericTaskReturningMethodNotAwaited_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        {|#0:GetNumberAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("GetNumberAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task MemberAccessFireAndForget_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class Service
{
    public Task ProcessAsync() => Task.CompletedTask;
}

class C
{
    void M()
    {
        var svc = new Service();
        {|#0:svc.ProcessAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AwaitedTask_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    async Task M()
    {
        await ProcessAsync();
    }
}";

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task StoredTask_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    void M()
    {
        var task = ProcessAsync();
    }
}";

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ExplicitDiscard_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    void M()
    {
        _ = ProcessAsync();
    }
}";

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task FireAndForgetAttribute_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace MissingAwaitDetector
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FireAndForgetAttribute : Attribute
    {
        public string Reason { get; }
        public FireAndForgetAttribute(string reason = null) { Reason = reason; }
    }
}

class C
{
    [MissingAwaitDetector.FireAndForget(""Background telemetry"")]
    Task SendTelemetryAsync() => Task.CompletedTask;

    void M()
    {
        SendTelemetryAsync();
    }
}";

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task NonTaskMethod_NoDiagnostic()
        {
            var source = @"
class C
{
    int GetNumber() => 42;

    void M()
    {
        GetNumber();
    }
}";

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task ConditionalAccessFireAndForget_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class Service
{
    public Task ProcessAsync() => Task.CompletedTask;
}

class C
{
    void M()
    {
        Service svc = null;
        {|#0:svc?.ProcessAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("svc?.ProcessAsync()");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ValueTaskReturningMethodNotAwaited_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask ProcessAsync() => default;

    void M()
    {
        {|#0:ProcessAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task FireAndForgetInConstructor_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static Task InitAsync() => Task.CompletedTask;

    C()
    {
        {|#0:InitAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("InitAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task FireAndForgetInLambda_Reports_MAWT004()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    void M()
    {
        Action a = () =>
        {
            {|#0:ProcessAsync()|};
        };
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task FireAndForgetInStaticMethod_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static Task ProcessAsync() => Task.CompletedTask;

    static void M()
    {
        {|#0:ProcessAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task FireAndForgetInAsyncMethod_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    async Task M()
    {
        {|#0:ProcessAsync()|};
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ReturnedTask_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    Task M()
    {
        return ProcessAsync();
    }
}";

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskInIfCondition_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        if (GetNumberAsync() != null)
        {
        }
    }
}";

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task FireAndForgetAttribute_OnInterfaceMethod_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace MissingAwaitDetector
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FireAndForgetAttribute : Attribute
    {
        public string Reason { get; }
        public FireAndForgetAttribute(string reason = null) { Reason = reason; }
    }
}

interface IService
{
    [MissingAwaitDetector.FireAndForget(""Background"")]
    Task SendAsync();
}

class Service : IService
{
    public Task SendAsync() => Task.CompletedTask;
}

class C
{
    void M()
    {
        IService svc = new Service();
        svc.SendAsync();
    }
}";

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ChainedFluentCallReturningTask_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class Builder
{
    public Builder Configure() => this;
    public Task RunAsync() => Task.CompletedTask;
}

class C
{
    void M()
    {
        var builder = new Builder();
        {|#0:builder.Configure().RunAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("RunAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskRunNotAwaited_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    void M()
    {
        {|#0:Task.Run(() => { })|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("Run");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task MultipleFireAndForgets_Reports_MultipleDiagnostics()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;
    Task SaveAsync() => Task.CompletedTask;

    void M()
    {
        {|#0:ProcessAsync()|};
        {|#1:SaveAsync()|};
    }
}";

            var expected1 = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            var expected2 = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(1)
                .WithArguments("SaveAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task FireAndForgetInsideTryCatch_Reports_MAWT004()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    void M()
    {
        try
        {
            {|#0:ProcessAsync()|};
        }
        catch (Exception) { }
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task GenericValueTaskMethodNotAwaited_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    void M()
    {
        {|#0:GetNumberAsync()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("GetNumberAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task FireAndForgetInFinally_Reports_MAWT004()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task CleanupAsync() => Task.CompletedTask;

    void M()
    {
        try { }
        finally
        {
            {|#0:CleanupAsync()|};
        }
    }
}";

            var expected = CSharpAnalyzerVerifier<FireAndForgetAnalyzer>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("CleanupAsync");

            await CSharpAnalyzerVerifier<FireAndForgetAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }
    }
}
