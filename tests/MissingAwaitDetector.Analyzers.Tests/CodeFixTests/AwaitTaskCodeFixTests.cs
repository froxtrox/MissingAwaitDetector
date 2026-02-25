using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.CodeFixes;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.CodeFixTests
{
    public class AwaitTaskCodeFixTests
    {
        // ── MAWT002: .Result -> await ──────────────────────────────────────

        [Fact]
        public async Task Fix_TaskResult_AddsAwaitAndMakesMethodAsync()
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

            var fixedSource = @"
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

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        // ── MAWT002: .Wait() -> await ─────────────────────────────────────

        [Fact]
        public async Task Fix_TaskWait_AddsAwaitAndMakesMethodAsync()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    async Task M()
    {
        var task = DoWorkAsync();
        await task;
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Wait()");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        // ── MAWT002: .GetAwaiter().GetResult() -> await ───────────────────

        [Fact]
        public async Task Fix_GetAwaiterGetResult_AddsAwaitAndMakesMethodAsync()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var result = await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("GetAwaiter().GetResult()");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        // ── MAWT008: .Result in async method -> await ─────────────────────

        [Fact]
        public async Task Fix_SyncUnwrapInAsyncMethod_ReplacesWithAwait()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var result = await GetNumberAsync();
        await Task.CompletedTask;
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SyncUnwrapInAsyncMethod)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        // ── MAWT005: Stored never awaited -> add await ────────────────────

        [Fact]
        public async Task Fix_StoredNeverAwaited_AddsAwaitAndMakesMethodAsync()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var task = await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<StoredNeverAwaitedAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(0)
                .WithArguments("task");

            await CSharpCodeFixVerifier<StoredNeverAwaitedAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        // Note: MAWT001 (TaskTreatedAsValue) code fix tests are omitted because
        // the current code fix naively wraps expressions in 'await' which doesn't
        // fully resolve cases like task.ToString() (needs (await task).ToString())
        // or interpolation. This is a known limitation to address separately.

        // ── Void return type -> Task return type ──────────────────────────

        [Fact]
        public async Task Fix_VoidMethod_ChangesReturnTypeToTask()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    void M()
    {
        {|#0:DoWorkAsync().Wait()|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    async Task M()
    {
        await DoWorkAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Wait()");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        // ── Already async method — no double-async ────────────────────────

        [Fact]
        public async Task Fix_AlreadyAsyncMethod_DoesNotAddAsyncAgain()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var result = await GetNumberAsync();
        await Task.CompletedTask;
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SyncUnwrapInAsyncMethod)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task Fix_PublicVoidMethod_PreservesPublicModifier()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    public void M()
    {
        var result = {|#0:GetNumberAsync().Result|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    public async Task M()
    {
        var result = await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_StaticVoidMethod_AddsAsyncAndPreservesStatic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static Task DoWorkAsync() => Task.CompletedTask;

    static void M()
    {
        var task = DoWorkAsync();
        {|#0:task.Wait()|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    static Task DoWorkAsync() => Task.CompletedTask;

    static async Task M()
    {
        var task = DoWorkAsync();
        await task;
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Wait()");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_PublicStaticMethod_PreservesAllModifiers()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static Task<int> GetNumberAsync() => Task.FromResult(3);

    public static void M()
    {
        var result = {|#0:GetNumberAsync().Result|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    static Task<int> GetNumberAsync() => Task.FromResult(3);

    public static async Task M()
    {
        var result = await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_IntReturnType_UpgradesToTaskOfInt()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    int M()
    {
        return {|#0:GetNumberAsync().Result|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task<int> M()
    {
        return await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_StoredNeverAwaited_ValueTask_AddsAwait()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var vt = await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<StoredNeverAwaitedAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(0)
                .WithArguments("vt");

            await CSharpCodeFixVerifier<StoredNeverAwaitedAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_GetAwaiterGetResultInAsyncMethod_ReplacesWithAwait()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var result = await GetNumberAsync();
        await Task.CompletedTask;
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SyncUnwrapInAsyncMethod)
                .WithLocation(0)
                .WithArguments("GetAwaiter().GetResult()");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_WaitInAsyncMethod_ReplacesWithAwait()
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

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    async Task M()
    {
        var task = DoWorkAsync();
        await task;
        await Task.CompletedTask;
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SyncUnwrapInAsyncMethod)
                .WithLocation(0)
                .WithArguments("Wait()");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_StoredNeverAwaited_InAsyncMethod_AddsAwaitOnly()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var {|#0:task = GetNumberAsync()|};
        await Task.CompletedTask;
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var task = await GetNumberAsync();
        await Task.CompletedTask;
    }
}";

            var expected = CSharpCodeFixVerifier<StoredNeverAwaitedAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.StoredNeverAwaited)
                .WithLocation(0)
                .WithArguments("task");

            await CSharpCodeFixVerifier<StoredNeverAwaitedAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_ValueTaskResult_AddsAwaitAndMakesAsync()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    void M()
    {
        var result = {|#0:GetNumberAsync().Result|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    async Task M()
    {
        var result = await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_ProtectedVirtualMethod_PreservesModifiers()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    protected virtual void M()
    {
        var result = {|#0:GetNumberAsync().Result|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    protected virtual async Task M()
    {
        var result = await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_InNestedClass_AddsAwaitCorrectly()
        {
            var source = @"
using System.Threading.Tasks;

class Outer
{
    class Inner
    {
        Task<int> GetNumberAsync() => Task.FromResult(3);

        void M()
        {
            var result = {|#0:GetNumberAsync().Result|};
        }
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class Outer
{
    class Inner
    {
        Task<int> GetNumberAsync() => Task.FromResult(3);

        async Task M()
        {
            var result = await GetNumberAsync();
        }
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_StringReturnType_UpgradesToTaskOfString()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<string> GetNameAsync() => Task.FromResult(""hello"");

    string M()
    {
        return {|#0:GetNameAsync().Result|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<string> GetNameAsync() => Task.FromResult(""hello"");

    async Task<string> M()
    {
        return await GetNameAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_Result_OnStaticMethodCall_AddsAwait()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static Task<int> GetNumberAsync() => Task.FromResult(3);

    static void M()
    {
        var x = {|#0:GetNumberAsync().Result|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    static Task<int> GetNumberAsync() => Task.FromResult(3);

    static async Task M()
    {
        var x = await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_InternalMethod_PreservesInternalModifier()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    internal void M()
    {
        var x = {|#0:GetNumberAsync().Result|};
    }
}";

            var fixedSource = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    internal async Task M()
    {
        var x = await GetNumberAsync();
    }
}";

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Result");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }

        [Fact]
        public async Task Fix_WaitCall_ReplacesWithAwait()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task ProcessAsync() => Task.CompletedTask;

    void M()
    {
        {|#0:ProcessAsync().Wait()|};
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

            var expected = CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .Diagnostic(DiagnosticIds.SynchronousUnwrap)
                .WithLocation(0)
                .WithArguments("Wait()");

            await CSharpCodeFixVerifier<SynchronousUnwrapAnalyzer, AwaitTaskCodeFixProvider>
                .VerifyCodeFixAsync(source, expected, fixedSource);
        }
    }
}
