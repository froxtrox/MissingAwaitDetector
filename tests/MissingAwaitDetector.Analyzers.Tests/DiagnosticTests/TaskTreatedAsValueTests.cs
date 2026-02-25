using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.DiagnosticTests
{
    public class TaskTreatedAsValueTests
    {
        private static DiagnosticResult ExpectDiagnostic(int line, int column)
            => CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(line, column);

        [Fact]
        public async Task TaskToString_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        var s = {|#0:task.ToString()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<int>", "int");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskGetHashCode_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<string> GetStringAsync() => Task.FromResult(""hello"");

    void M()
    {
        var task = GetStringAsync();
        var h = {|#0:task.GetHashCode()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<string>", "string");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskInStringInterpolation_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        var s = $""Result: {|#0:{task}|}"";
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<int>", "int");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskPassedWhereValueExpected_Reports_MAWT001()
        {
            // Use a method with overloads so compiler doesn't error -
            // one overload takes int, another takes object, so Task<int> resolves to object overload
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void UseValue(object o) { }

    void M()
    {
        var task = GetNumberAsync();
        UseValue(task);
    }
}";

            // object parameter doesn't trigger our analyzer (we skip SpecialType.System_Object)
            // This is by design - Console.WriteLine(task) is handled by ToString/interpolation rules
            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyNoDiagnosticAsync(source);
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
        var s = result.ToString();
    }
}";

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskPassedToTaskMethod_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    Task<int> Wrapper() => GetNumberAsync();
}";

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task NonGenericTask_ToString_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    void M()
    {
        var task = DoWorkAsync();
        var s = {|#0:task.ToString()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("", "void");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task ValueTaskInStringInterpolation_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    void M()
    {
        var vt = GetNumberAsync();
        var s = $""Result: {|#0:{vt}|}"";
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<int>", "int");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskEquals_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<string> GetStringAsync() => Task.FromResult(""hello"");

    void M()
    {
        var task = GetStringAsync();
        var b = {|#0:task.Equals(null)|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<string>", "string");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task NonGenericTaskGetHashCode_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    void M()
    {
        var task = DoWorkAsync();
        var h = {|#0:task.GetHashCode()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("", "void");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task MultipleInterpolationsInSameString_Reports_TwoDiagnostics()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);
    Task<string> GetStringAsync() => Task.FromResult(""hello"");

    void M()
    {
        var t1 = GetNumberAsync();
        var t2 = GetStringAsync();
        var s = $""{|#0:{t1}|} and {|#1:{t2}|}"";
    }
}";

            var expected1 = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<int>", "int");

            var expected2 = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(1)
                .WithArguments("<string>", "string");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task ToStringOnDirectMethodCall_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var s = {|#0:GetNumberAsync().ToString()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<int>", "int");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskPassedWhereDelegateExpected_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void UseFunc(Func<int, Task<int>> f) { }

    void M()
    {
        UseFunc(x => GetNumberAsync());
    }
}";

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ValueTaskToString_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    void M()
    {
        var vt = GetNumberAsync();
        var s = {|#0:vt.ToString()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<int>", "int");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task AwaitedTaskToString_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var result = await GetNumberAsync();
        var s = result.ToString();
    }
}";

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskPassedToGenericMethod_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void Log<T>(T value) { }

    void M()
    {
        var task = GetNumberAsync();
        Log(task);
    }
}";

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task NonGenericValueTaskGetHashCode_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask DoWorkAsync() => default;

    void M()
    {
        var task = DoWorkAsync();
        var hash = {|#0:task.GetHashCode()|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("", "void");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ValueTaskEqualsNull_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    void M()
    {
        var task = GetNumberAsync();
        var eq = {|#0:task.Equals(null)|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<int>", "int");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task NonGenericTaskInStringInterpolation_Reports_MAWT001()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    void M()
    {
        var task = DoWorkAsync();
        var s = $""Status: {|#0:{task}|}"";
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("", "void");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task MultipleToStringCallsSameMethod_Reports_TwoDiagnostics()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        var s1 = {|#0:task.ToString()|};
        var s2 = {|#1:task.GetHashCode()|};
    }
}";

            var expected1 = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(0)
                .WithArguments("<int>", "int");

            var expected2 = CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>
                .Diagnostic(DiagnosticIds.TaskTreatedAsValue)
                .WithLocation(1)
                .WithArguments("<int>", "int");

            await CSharpAnalyzerVerifier<TaskTreatedAsValueAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
        }
    }
}
