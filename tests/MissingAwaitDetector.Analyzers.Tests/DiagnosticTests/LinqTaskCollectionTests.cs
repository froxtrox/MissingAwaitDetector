using System.Threading.Tasks;
using MissingAwaitDetector.Analyzers.Analyzers;
using MissingAwaitDetector.Analyzers.Tests.Verifiers;
using Xunit;

namespace MissingAwaitDetector.Analyzers.Tests.DiagnosticTests
{
    public class LinqTaskCollectionTests
    {
        [Fact]
        public async Task SelectProducingTaskCollection_Reports_MAWT007()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    void M()
    {
        var list = new List<int> { 1, 2, 3 };
        var tasks = {|#0:list.Select(x => ProcessAsync(x))|}.ToList();
    }
}";

            var expected = CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>
                .Diagnostic(DiagnosticIds.LinqTaskCollection)
                .WithLocation(0);

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task SelectWithWhenAll_NoDiagnostic()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    async Task M()
    {
        var list = new List<int> { 1, 2, 3 };
        var results = await Task.WhenAll(list.Select(x => ProcessAsync(x)));
    }
}";

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task NonTaskSelect_NoDiagnostic()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M()
    {
        var list = new List<int> { 1, 2, 3 };
        var doubled = list.Select(x => x * 2).ToList();
    }
}";

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        // ── New edge-case tests ─────────────────────────────────────────────

        [Fact]
        public async Task SelectToArray_Reports_MAWT007()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    void M()
    {
        var list = new List<int> { 1, 2, 3 };
        var tasks = {|#0:list.Select(x => ProcessAsync(x))|}.ToArray();
    }
}";

            var expected = CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>
                .Diagnostic(DiagnosticIds.LinqTaskCollection)
                .WithLocation(0);

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task SelectToHashSet_Reports_MAWT007()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    void M()
    {
        var list = new List<int> { 1, 2, 3 };
        var tasks = {|#0:list.Select(x => ProcessAsync(x))|}.ToHashSet();
    }
}";

            var expected = CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>
                .Diagnostic(DiagnosticIds.LinqTaskCollection)
                .WithLocation(0);

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task WhereSelectChain_Reports_MAWT007()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    void M()
    {
        var list = new List<int> { 1, 2, 3 };
        var tasks = {|#0:list.Where(x => x > 1).Select(x => ProcessAsync(x))|}.ToList();
    }
}";

            var expected = CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>
                .Diagnostic(DiagnosticIds.LinqTaskCollection)
                .WithLocation(0);

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task SelectWithWhenAny_NoDiagnostic()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    async Task M()
    {
        var list = new List<int> { 1, 2, 3 };
        var first = await Task.WhenAny(list.Select(x => ProcessAsync(x)));
    }
}";

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task SelectWithoutMaterialization_Reports_MAWT007()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    void M()
    {
        var list = new List<int> { 1, 2, 3 };
        var tasks = {|#0:list.Select(x => ProcessAsync(x))|};
    }
}";

            var expected = CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>
                .Diagnostic(DiagnosticIds.LinqTaskCollection)
                .WithLocation(0);

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task SelectToDictionary_Reports_MAWT007()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    void M()
    {
        var list = new List<int> { 1, 2, 3 };
        var dict = {|#0:list.Select(x => ProcessAsync(x))|}.ToDictionary(t => t.Id);
    }
}";

            var expected = CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>
                .Diagnostic(DiagnosticIds.LinqTaskCollection)
                .WithLocation(0);

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task SelectManyProducingTasks_Reports_MAWT007()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    void M()
    {
        var numbers = new[] { 1, 2, 3 };
        var tasks = {|#0:numbers.SelectMany(n => new[] { ProcessAsync(n), ProcessAsync(n + 1) })|}.ToList();
    }
}";

            var expected = CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>
                .Diagnostic(DiagnosticIds.LinqTaskCollection)
                .WithLocation(0);

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task SelectToArrayMaterialization_Reports_MAWT007()
        {
            var source = @"
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    void M()
    {
        var numbers = new[] { 1, 2, 3 };
        var tasks = {|#0:numbers.Select(n => ProcessAsync(n))|}.ToArray();
    }
}";

            var expected = CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>
                .Diagnostic(DiagnosticIds.LinqTaskCollection)
                .WithLocation(0);

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task NonTaskSelectProducingStrings_NoDiagnostic()
        {
            var source = @"
using System.Linq;

class C
{
    void M()
    {
        var numbers = new[] { 1, 2, 3 };
        var strings = numbers.Select(n => n.ToString()).ToList();
    }
}";

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }

        [Fact]
        public async Task SelectOnArrayWithToHashSet_Reports_MAWT007()
        {
            var source = @"
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    void M()
    {
        var numbers = new[] { 1, 2, 3 };
        var tasks = {|#0:numbers.Select(n => ProcessAsync(n))|}.ToHashSet();
    }
}";

            var expected = CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>
                .Diagnostic(DiagnosticIds.LinqTaskCollection)
                .WithLocation(0);

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyAnalyzerAsync(source, expected);
        }

        [Fact]
        public async Task SelectComposedWithWhenAll_NoDiagnostic()
        {
            var source = @"
using System.Linq;
using System.Threading.Tasks;

class C
{
    Task<int> ProcessAsync(int x) => Task.FromResult(x * 2);

    async Task M()
    {
        var numbers = new[] { 1, 2, 3 };
        await Task.WhenAll(numbers.Select(n => ProcessAsync(n)));
    }
}";

            await CSharpAnalyzerVerifier<LinqTaskCollectionAnalyzer>.VerifyNoDiagnosticAsync(source);
        }
    }
}
