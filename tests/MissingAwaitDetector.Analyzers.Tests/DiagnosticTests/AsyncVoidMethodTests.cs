using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.DiagnosticTests
{
    public class AsyncVoidMethodTests
    {
        [Fact]
        public async Task AsyncVoidMethod_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async void {|#0:DoWorkAsync|}()
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("DoWorkAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidLocalFunction_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    void M()
    {
        async void {|#0:LocalAsync|}()
        {
            await Task.Delay(1);
        }

        LocalAsync();
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("LocalAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidWithException_Reports_MAWT010()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async void {|#0:RiskyAsync|}()
    {
        await Task.Delay(1);
        throw new InvalidOperationException();
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("RiskyAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncTaskMethod_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async Task DoWorkAsync()
    {
        await Task.Delay(1);
    }
}";

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task AsyncTaskOfTMethod_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async Task<int> GetNumberAsync()
    {
        await Task.Delay(1);
        return 42;
    }
}";

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task SyncVoidMethod_NoDiagnostic()
        {
            var source = @"
class C
{
    void DoWork()
    {
        var x = 42;
    }
}";

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task EventHandler_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async void OnButtonClick(object sender, EventArgs e)
    {
        await Task.Delay(1);
    }
}";

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task EventHandlerDerivedEventArgs_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class MyEventArgs : EventArgs
{
    public int Value { get; set; }
}

class C
{
    async void OnCustomEvent(object sender, MyEventArgs e)
    {
        await Task.Delay(1);
    }
}";

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task NotEventHandler_WrongFirstParam_Reports_MAWT010()
        {
            // First param is string, not object — not a valid event handler pattern
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async void {|#0:HandleEvent|}(string sender, EventArgs e)
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("HandleEvent");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task NotEventHandler_WrongSecondParam_Reports_MAWT010()
        {
            // Second param is int, not EventArgs — not a valid event handler pattern
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async void {|#0:HandleEvent|}(object sender, int value)
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("HandleEvent");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidSingleParam_Reports_MAWT010()
        {
            // Single parameter — not the event handler pattern
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async void {|#0:ProcessAsync|}(EventArgs e)
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("ProcessAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidNoParams_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async void {|#0:FireAsync|}()
    {
        await Task.CompletedTask;
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("FireAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncValueTaskMethod_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async ValueTask DoWorkAsync()
    {
        await Task.Delay(1);
    }
}";

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task AsyncVoidInNestedClass_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class Outer
{
    class Inner
    {
        async void {|#0:DoWorkAsync|}()
        {
            await Task.Delay(1);
        }
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("DoWorkAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidInPartialClass_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

partial class C
{
    async void {|#0:DoWorkAsync|}()
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("DoWorkAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidStaticMethod_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    static async void {|#0:DoWorkAsync|}()
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("DoWorkAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidGenericMethod_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async void {|#0:Process|}<T>(T item)
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("Process");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidOverriddenMethod_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class Base
{
    public virtual void DoWork() { }
}

class Derived : Base
{
    public override async void {|#0:DoWork|}()
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("DoWork");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidLambda_NoDiagnostic()
        {
            // Analyzer only registers on MethodDeclaration and LocalFunctionStatement, not lambdas
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Action a = async () =>
        {
            await Task.Delay(1);
        };
    }
}";

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task EventHandlerThreeParams_Reports_MAWT010()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async void {|#0:OnEvent|}(object sender, EventArgs e, int extra)
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("OnEvent");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task EventHandlerDerivedMultipleLevels_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class MyBaseArgs : EventArgs { }
class MyDerivedArgs : MyBaseArgs { }

class C
{
    async void OnEvent(object sender, MyDerivedArgs e)
    {
        await Task.Delay(1);
    }
}";

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task AsyncVoidExpressionBodiedMethod_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async void {|#0:DoWorkAsync|}() => await Task.Delay(1);
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("DoWorkAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidMethodInStruct_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

struct S
{
    async void {|#0:DoWorkAsync|}()
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("DoWorkAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AsyncVoidLocalFunctionInMethod_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    void M()
    {
        async void {|#0:LocalAsync|}()
        {
            await Task.Delay(1);
        }
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("LocalAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task EventHandlerWithNullableEventArgs_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    async void OnEvent(object sender, EventArgs e)
    {
        await Task.Delay(1);
    }
}";

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task AsyncVoidProtectedMethod_Reports_MAWT010()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    protected async void {|#0:DoWorkAsync|}()
    {
        await Task.Delay(1);
    }
}";

            var expected = CSharpAnalyzerVerifier<AsyncVoidAnalyzer>
                .Diagnostic(DiagnosticIds.AsyncVoidMethod)
                .WithLocation(0)
                .WithArguments("DoWorkAsync");

            await CSharpAnalyzerVerifier<AsyncVoidAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }
    }
}
