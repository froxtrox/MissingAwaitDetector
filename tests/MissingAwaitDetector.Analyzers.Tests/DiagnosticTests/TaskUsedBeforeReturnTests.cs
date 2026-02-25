using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.DiagnosticTests
{
    public class TaskUsedBeforeReturnTests
    {
        [Fact]
        public async Task TaskInspectedBeforeReturn_Reports_MAWT009()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    Task<int> M()
    {
        var {|#0:task = CalculateAsync()|};
        Console.WriteLine(task);
        return task;
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>
                .Diagnostic(DiagnosticIds.TaskUsedBeforeReturn)
                .WithLocation(0)
                .WithArguments("task");

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskDirectReturn_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    Task<int> M()
    {
        return CalculateAsync();
    }
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskStoredAndReturnedNoUse_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    Task<int> M()
    {
        var task = CalculateAsync();
        return task;
    }
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task AsyncMethod_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    async Task<int> M()
    {
        var result = await CalculateAsync();
        return result;
    }
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task MultipleTasks_OneUsedBeforeReturn_Reports_MAWT009()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    Task<int> M()
    {
        var task1 = CalculateAsync();
        var {|#0:task2 = CalculateAsync()|};
        Console.WriteLine(task2);
        return task2;
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>
                .Diagnostic(DiagnosticIds.TaskUsedBeforeReturn)
                .WithLocation(0)
                .WithArguments("task2");

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskPropertyAccessBeforeReturn_Reports_MAWT009()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    Task<int> M()
    {
        var {|#0:task = CalculateAsync()|};
        var done = task.IsCompleted;
        return task;
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>
                .Diagnostic(DiagnosticIds.TaskUsedBeforeReturn)
                .WithLocation(0)
                .WithArguments("task");

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskPassedAsArgumentBeforeReturn_Reports_MAWT009()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);
    void Log(object o) { }

    Task<int> M()
    {
        var {|#0:task = CalculateAsync()|};
        Log(task);
        return task;
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>
                .Diagnostic(DiagnosticIds.TaskUsedBeforeReturn)
                .WithLocation(0)
                .WithArguments("task");

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskUsedInConditionalBeforeReturn_Reports_MAWT009()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    Task<int> M()
    {
        var {|#0:task = CalculateAsync()|};
        if (task.IsCompleted)
            return task;
        return task;
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>
                .Diagnostic(DiagnosticIds.TaskUsedBeforeReturn)
                .WithLocation(0)
                .WithArguments("task");

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task VoidMethodWithTaskVariable_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    void M()
    {
        var task = CalculateAsync();
        Console.WriteLine(task);
    }
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskNotReturned_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);
    Task<int> OtherAsync() => Task.FromResult(5);

    Task<int> M()
    {
        var task = CalculateAsync();
        Console.WriteLine(task);
        return OtherAsync();
    }
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ExpressionBodiedMember_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    Task<int> M() => CalculateAsync();
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskUsedInConditionalExpression_Reports_MAWT009()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    Task<int> M()
    {
        var {|#0:task = CalculateAsync()|};
        Console.WriteLine(task);
        return task;
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>
                .Diagnostic(DiagnosticIds.TaskUsedBeforeReturn)
                .WithLocation(0)
                .WithArguments("task");

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskDirectlyReturnedFromMethod_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    Task<int> M()
    {
        var task = CalculateAsync();
        return task;
    }
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task TaskStoredAndImmediatelyReturned_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<string> GetNameAsync() => Task.FromResult(""hello"");

    Task<string> M()
    {
        return GetNameAsync();
    }
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task AsyncMethodWithAwait_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    async Task<int> M()
    {
        var task = CalculateAsync();
        Console.WriteLine(task);
        return await task;
    }
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task NonTaskReturnType_NoDiagnostic()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    Task<int> CalculateAsync() => Task.FromResult(3);

    void M()
    {
        var task = CalculateAsync();
        Console.WriteLine(task);
    }
}";

            await CSharpAnalyzerVerifier<TaskUsedBeforeReturnAnalyzer>.VerifyNoDiagnosticAsync(source);
        }
    }
}
