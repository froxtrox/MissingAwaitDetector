using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.Analyzers;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.DiagnosticTests
{
    public class TaskStateInspectionTests
    {
        // ── Default behaviour (no editorconfig) ────────────────────────────

        [Theory]
        [InlineData("IsCompleted")]
        [InlineData("IsFaulted")]
        [InlineData("IsCanceled")]
        [InlineData("Status")]
        [InlineData("Exception")]
        [InlineData("Id")]
        [InlineData("AsyncState")]
        [InlineData("CreationOptions")]
        public async Task TaskStateProperty_Reports_MAWT003(string propertyName)
        {
            var source = $@"
using System.Threading.Tasks;

class C
{{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {{
        var task = GetNumberAsync();
        var value = task.{{|#0:{propertyName}|}};
    }}
}}";

            var expected = CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                .Diagnostic(DiagnosticIds.TaskStateInspection)
                .WithLocation(0)
                .WithArguments(propertyName);

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskCurrentId_StaticAccess_Reports_MAWT003()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    void M()
    {
        var id = Task.{|#0:CurrentId|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                .Diagnostic(DiagnosticIds.TaskStateInspection)
                .WithLocation(0)
                .WithArguments("CurrentId");

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task UserDefinedClassWithIdProperty_NoDiagnostic()
        {
            var source = @"
class MyService
{
    public int Id { get; set; }
}

class C
{
    void M()
    {
        var svc = new MyService();
        var id = svc.Id;
    }
}";

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyNoDiagnosticAsync(source);
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

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task NonTaskProperty_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async Task M()
    {
        var s = ""hello"";
        var len = s.Length;
    }
}";

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        // ── editorconfig: excluded_task_properties ──────────────────────────

        [Fact]
        public async Task ExcludedTaskProperty_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;
class C
{
    void M()
    {
        var task = Task.FromResult(3);
        var id = task.Id;
    }
}";
            var test = new CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.Test
            {
                TestCode = source,
            };
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig",
                "is_global = true\ndotnet_diagnostic.MAWT003.excluded_task_properties = Id"));

            // No expected diagnostics — Id is excluded
            await test.RunAsync();
        }

        [Fact]
        public async Task ExcludedTaskProperty_OtherPropertiesStillReported()
        {
            // Id excluded, but IsCompleted is still in scope → must still fire
            var source = @"
using System.Threading.Tasks;
class C
{
    void M()
    {
        var task = Task.FromResult(3);
        var id = task.Id;
        var done = task.{|#0:IsCompleted|};
    }
}";
            var test = new CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.Test
            {
                TestCode = source,
            };
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig",
                "is_global = true\ndotnet_diagnostic.MAWT003.excluded_task_properties = Id"));
            test.ExpectedDiagnostics.Add(
                CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                    .Diagnostic(DiagnosticIds.TaskStateInspection)
                    .WithLocation(0)
                    .WithArguments("IsCompleted"));

            await test.RunAsync();
        }

        [Fact]
        public async Task ExcludeMultipleTaskProperties_CommaSeparated()
        {
            // Exclude all diagnostic-only metadata properties; state properties still fire
            var source = @"
using System.Threading.Tasks;
class C
{
    void M()
    {
        var task = Task.FromResult(3);
        _ = task.Id;            // excluded
        _ = task.AsyncState;    // excluded
        _ = task.{|#0:Status|}; // NOT excluded — should still fire
    }
}";
            var test = new CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.Test
            {
                TestCode = source,
            };
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig",
                "is_global = true\ndotnet_diagnostic.MAWT003.excluded_task_properties = Id, CurrentId, AsyncState, CreationOptions"));
            test.ExpectedDiagnostics.Add(
                CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                    .Diagnostic(DiagnosticIds.TaskStateInspection)
                    .WithLocation(0)
                    .WithArguments("Status"));

            await test.RunAsync();
        }

        [Fact]
        public async Task UnknownExcludedTaskPropertyName_Ignored_DefaultsApply()
        {
            // Unknown name → silently ignored → all defaults still apply
            var source = @"
using System.Threading.Tasks;
class C
{
    void M()
    {
        var task = Task.FromResult(3);
        var id = task.{|#0:Id|};
    }
}";
            var test = new CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.Test
            {
                TestCode = source,
            };
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig",
                "is_global = true\ndotnet_diagnostic.MAWT003.excluded_task_properties = NonExistentProperty"));
            test.ExpectedDiagnostics.Add(
                CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                    .Diagnostic(DiagnosticIds.TaskStateInspection)
                    .WithLocation(0)
                    .WithArguments("Id"));

            await test.RunAsync();
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task IsCompletedSuccessfully_Reports_MAWT003()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    void M()
    {
        var task = Task.FromResult(3);
        var done = task.{|#0:IsCompletedSuccessfully|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                .Diagnostic(DiagnosticIds.TaskStateInspection)
                .WithLocation(0)
                .WithArguments("IsCompletedSuccessfully");

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task ValueTaskIsCompleted_Reports_MAWT003()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    void M()
    {
        var vt = new ValueTask<int>(42);
        var done = vt.{|#0:IsCompleted|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                .Diagnostic(DiagnosticIds.TaskStateInspection)
                .WithLocation(0)
                .WithArguments("IsCompleted");

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task MultipleStatePropertiesSameMethod_Reports_MultipleDiagnostics()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    void M()
    {
        var task = Task.FromResult(3);
        var a = task.{|#0:IsCompleted|};
        var b = task.{|#1:Status|};
    }
}";

            var expected1 = CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                .Diagnostic(DiagnosticIds.TaskStateInspection)
                .WithLocation(0)
                .WithArguments("IsCompleted");

            var expected2 = CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                .Diagnostic(DiagnosticIds.TaskStateInspection)
                .WithLocation(1)
                .WithArguments("Status");

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyAnalyzerAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task TaskPropertyOnAwaitedResult_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class MyResult
{
    public int Status { get; set; }
}

class C
{
    Task<MyResult> GetResultAsync() => Task.FromResult(new MyResult());

    async Task M()
    {
        var result = await GetResultAsync();
        var s = result.Status;
    }
}";

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ExcludeAllDefaultProperties_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;
class C
{
    void M()
    {
        var task = Task.FromResult(3);
        _ = task.Id;
        _ = task.IsCompleted;
        _ = task.Status;
    }
}";
            var test = new CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.Test
            {
                TestCode = source,
            };
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig",
                "is_global = true\ndotnet_diagnostic.MAWT003.excluded_task_properties = IsCompleted, IsCompletedSuccessfully, IsFaulted, IsCanceled, Status, Exception, Id, AsyncState, CreationOptions, CurrentId"));

            await test.RunAsync();
        }

        [Fact]
        public async Task ExcludedPropertyWithCommaAndSpaces_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;
class C
{
    void M()
    {
        var task = Task.FromResult(3);
        _ = task.Id;
        _ = task.AsyncState;
    }
}";
            var test = new CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.Test
            {
                TestCode = source,
            };
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig",
                "is_global = true\ndotnet_diagnostic.MAWT003.excluded_task_properties = Id , AsyncState"));

            await test.RunAsync();
        }

        [Fact]
        public async Task NonGenericTaskIsCompleted_Reports_MAWT003()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task DoWorkAsync() => Task.CompletedTask;

    void M()
    {
        var task = DoWorkAsync();
        var done = task.{|#0:IsCompleted|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                .Diagnostic(DiagnosticIds.TaskStateInspection)
                .WithLocation(0)
                .WithArguments("IsCompleted");

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskResultProperty_NoDiagnostic_ForMawt003()
        {
            // Result is handled by MAWT002, not MAWT003
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        var x = task.Result;
    }
}";

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task CustomClassWithIsCompletedProperty_NoDiagnostic()
        {
            var source = @"
class MyClass
{
    public bool IsCompleted => true;
}

class C
{
    void M()
    {
        var obj = new MyClass();
        var done = obj.IsCompleted;
    }
}";

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task ExcludedPropertyWithIsCompleted_NoDiagnostic()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    void M()
    {
        var task = GetNumberAsync();
        var done = task.IsCompleted;
    }
}";
            var test = new CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.Test
            {
                TestCode = source,
            };
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig",
                "is_global = true\ndotnet_diagnostic.MAWT003.excluded_task_properties = IsCompleted"));

            await test.RunAsync();
        }

        [Fact]
        public async Task ValueTaskStatus_Reports_MAWT003()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    ValueTask<int> GetNumberAsync() => new ValueTask<int>(42);

    void M()
    {
        var task = GetNumberAsync();
        var s = task.{|#0:IsCompleted|};
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                .Diagnostic(DiagnosticIds.TaskStateInspection)
                .WithLocation(0)
                .WithArguments("IsCompleted");

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task TaskIsFaultedInAsyncMethod_Reports_MAWT003()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    Task<int> GetNumberAsync() => Task.FromResult(3);

    async Task M()
    {
        var task = GetNumberAsync();
        if (task.{|#0:IsFaulted|}) return;
        await task;
    }
}";

            var expected = CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>
                .Diagnostic(DiagnosticIds.TaskStateInspection)
                .WithLocation(0)
                .WithArguments("IsFaulted");

            await CSharpAnalyzerVerifier<TaskStateInspectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }
    }
}
