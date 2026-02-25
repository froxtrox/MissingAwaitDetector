using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.CodeFixes;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.CodeFixTests
{
    public class FireAndForgetCodeFixTests
    {
        // ── Option 1: Add await ────────────────────────────────────────────

        [Fact]
        public async Task Fix_AddAwait_SimpleFireAndForget()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    async Task M()
    {
        await ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddAwait_GenericTaskFireAndForget()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("GetNumberAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddAwait_MemberAccessFireAndForget()
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

            var fixedSource = @"
using System.Threading.Tasks;

class Service
{
    public Task ProcessAsync() => Task.CompletedTask;
}

class C
{
    async Task M()
    {
        var svc = new Service();
        await svc.ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        // ── Option 2: Explicit discard ─────────────────────────────────────

        [Fact]
        public async Task Fix_AddDiscard_SimpleFireAndForget()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    void M()
    {
        _ = ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 1);
        }

        [Fact]
        public async Task Fix_AddDiscard_MemberAccessFireAndForget()
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

            var fixedSource = @"
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
        _ = svc.ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 1);
        }

        // ── Await changes void -> Task ─────────────────────────────────────

        [Fact]
        public async Task Fix_AddAwait_ChangesVoidToTask()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    async Task M()
    {
        await ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        // ── Discard preserves void method ──────────────────────────────────

        [Fact]
        public async Task Fix_AddDiscard_PreservesVoidReturnType()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        _ = GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("GetNumberAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 1);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task Fix_AddAwait_AlreadyAsyncMethod_DoesNotAddAsyncAgain()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    async Task M()
    {
        {|#0:ProcessAsync()|};
        await Task.CompletedTask;
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    async Task M()
    {
        await ProcessAsync();
        await Task.CompletedTask;
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddAwait_StaticMethod_AddsAsyncAndPreservesStatic()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    static Task ProcessAsync() => Task.CompletedTask;

    static async Task M()
    {
        await ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddAwait_PublicMethod_PreservesPublicModifier()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    public void M()
    {
        {|#0:ProcessAsync()|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    public async Task M()
    {
        await ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddAwait_PublicStaticMethod_PreservesAllModifiers()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static Task ProcessAsync() => Task.CompletedTask;

    public static void M()
    {
        {|#0:ProcessAsync()|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    static Task ProcessAsync() => Task.CompletedTask;

    public static async Task M()
    {
        await ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddAwait_ValueTaskMethod_AddsAwait()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    ValueTask ProcessAsync() => default;

    async Task M()
    {
        await ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddDiscard_ValueTaskMethod_AddsDiscard()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    ValueTask ProcessAsync() => default;

    void M()
    {
        _ = ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 1);
        }

        [Fact]
        public async Task Fix_AddDiscard_StaticMethod_PreservesModifier()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    static Task ProcessAsync() => Task.CompletedTask;

    static void M()
    {
        _ = ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 1);
        }

        [Fact]
        public async Task Fix_AddAwait_MultipleStatementsInMethod_OnlyModifiesTarget()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    void M()
    {
        var x = 42;
        {|#0:ProcessAsync()|};
        Console.WriteLine(x);
    }
}";

            var fixedSource = @"
using System;
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    async Task M()
    {
        var x = 42;
        await ProcessAsync();
        Console.WriteLine(x);
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddDiscard_InAsyncMethod_PreservesAsyncAndReturnType()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    async Task M()
    {
        {|#0:ProcessAsync()|};
        await Task.CompletedTask;
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    async Task M()
    {
        _ = ProcessAsync();
        await Task.CompletedTask;
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 1);
        }

        [Fact]
        public async Task Fix_AddAwait_ProtectedMethod_PreservesModifier()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    protected void M()
    {
        {|#0:ProcessAsync()|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    protected async Task M()
    {
        await ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddAwait_InNestedClass_AddsAwaitCorrectly()
        {
            var source = @"
using System.Threading.Tasks;

class Outer
{
    class Inner
    {
        Task ProcessAsync() => Task.CompletedTask;

        void M()
        {
            {|#0:ProcessAsync()|};
        }
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class Outer
{
    class Inner
    {
        Task ProcessAsync() => Task.CompletedTask;

        async Task M()
        {
            await ProcessAsync();
        }
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 0);
        }

        [Fact]
        public async Task Fix_AddDiscard_PublicMethod_PreservesModifier()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    public void M()
    {
        {|#0:ProcessAsync()|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    public void M()
    {
        _ = ProcessAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 1);
        }

        [Fact]
        public async Task Fix_AddDiscard_InNestedClass_AddsDiscardCorrectly()
        {
            var source = @"
using System.Threading.Tasks;

class Outer
{
    class Inner
    {
        Task ProcessAsync() => Task.CompletedTask;

        void M()
        {
            {|#0:ProcessAsync()|};
        }
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class Outer
{
    class Inner
    {
        Task ProcessAsync() => Task.CompletedTask;

        void M()
        {
            _ = ProcessAsync();
        }
    }
}";

            var expected = CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .Diagnostic(DiagnosticIds.FireAndForget)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpCodeFixVerifier<FireAndForgetAnalyzer, FireAndForgetCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource, codeActionIndex: 1);
        }
    }
}
