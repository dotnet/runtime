// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions.Generator;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = System.Text.RegularExpressions.Tests.CSharpCodeFixVerifier<
    System.Text.RegularExpressions.Generator.UpgradeToRegexGeneratorAnalyzer,
    System.Text.RegularExpressions.Generator.UpgradeToRegexGeneratorCodeFixer>;

namespace System.Text.RegularExpressions.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/69823", TestRuntimes.Mono)]
    public class UpgradeToRegexGeneratorAnalyzerTests
    {
        private const string UseRegexSourceGeneratorDiagnosticId = @"SYSLIB1046";

        [Fact]
        public async Task NoDiagnosticsForEmpty()
            => await VerifyCS.VerifyAnalyzerAsync(source: string.Empty);

        public static IEnumerable<object[]> ConstructorWithTimeoutTestData()
        {
            yield return new object[] { @"using System;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var regex = new Regex("""", RegexOptions.None, TimeSpan.FromSeconds(10));
    }
}" };

            yield return new object[] { @"using System;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var regex = new Regex("""", timeout: TimeSpan.FromSeconds(10));
    }
}" };

            yield return new object[] { @"using System;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var regex = new Regex(timeout: TimeSpan.FromSeconds(10), pattern: """");
    }
}" };
        }

        [Theory]
        [MemberData(nameof(ConstructorWithTimeoutTestData))]
        public async Task NoDiagnosticForConstructorWithTimeout(string test)
        {
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task NoDiagnosticForTopLevelStatements()
        {
            string test = @"using System.Text.RegularExpressions;

Regex r = new Regex("""");";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        public static IEnumerable<object[]> StaticInvocationWithTimeoutTestData()
        {
            foreach(string method in new[] { "Count",  "EnumerateMatches", "IsMatch", "Match", "Matches", "Split"})
            {
                yield return new object[] { @"using System;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        Regex." + method + @"(""input"", ""a|b"", RegexOptions.None, TimeSpan.FromSeconds(10));
    }
}" };
            }

            // Replace is special since it takes an extra argument
            yield return new object[] { @"using System;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        Regex.Replace(""input"", ""a|b"", ""replacement"" ,RegexOptions.None, TimeSpan.FromSeconds(10));
    }
}" };
        }

        [Theory]
        [MemberData(nameof(StaticInvocationWithTimeoutTestData))]
        public async Task NoDiagnosticForStaticInvocationWithTimeout(string test)
            => await VerifyCS.VerifyAnalyzerAsync(test);

        [Theory]
        [MemberData(nameof(InvocationTypes))]
        public async Task NoDiagnosticsForNet60(InvocationType invocationType)
        {
            string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;
            string test = @"using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, pattern: "\"\"") + isMatchInvocation + @";
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test, ReferenceAssemblies.Net.Net60, usePreviewLanguageVersion: true);
        }

        [Theory]
        [MemberData(nameof(InvocationTypes))]
        public async Task NoDiagnosticsForLowerLanguageVersion(InvocationType invocationType)
        {
            string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;
            string test = @"using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "\"\"") + isMatchInvocation + @";
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test, null, usePreviewLanguageVersion: false);
        }

        public static IEnumerable<object[]> ConstantPatternTestData()
        {
            foreach (InvocationType invocationType in new[] { InvocationType.Constructor, InvocationType.StaticMethods })
            {
                string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;
                // Test constructor with a passed in literal pattern.
                yield return new object[] { @"using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var isMatch = {|#0:" + ConstructRegexInvocation(invocationType, "\"\"") + @"|}" + isMatchInvocation + @";
    }
}", @"using System.Text;
using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [RegexGenerator("""")]
    private static partial Regex MyRegex();
}" };

                // Test constructor with a local constant pattern.
                yield return new object[] { @"using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        const string pattern = @"""";
        var isMatch = {|#0:" + ConstructRegexInvocation(invocationType, "\"\"") + @"|}" + isMatchInvocation + @";
    }
}", @"using System.Text;
using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        const string pattern = @"""";
        var isMatch = MyRegex().IsMatch("""");
    }

    [RegexGenerator("""")]
    private static partial Regex MyRegex();
}" };

                // Test constructor with a constant field pattern.
                yield return new object[] { @"using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    private const string pattern = @"""";

    public static void Main(string[] args)
    {
        var isMatch = {|#0:" + ConstructRegexInvocation(invocationType, "\"\"") + @"|}" + isMatchInvocation + @";
    }
}", @"using System.Text;
using System.Text.RegularExpressions;

public partial class Program
{
    private const string pattern = @"""";

    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [RegexGenerator("""")]
    private static partial Regex MyRegex();
}" };
            }
        }

        [Theory]
        [MemberData(nameof(ConstantPatternTestData))]
        public async Task DiagnosticEmittedForConstantPattern(string test, string fixedSource)
        {
            DiagnosticResult expectedDiagnostic = VerifyCS.Diagnostic(UseRegexSourceGeneratorDiagnosticId).WithLocation(0);
            await VerifyCS.VerifyCodeFixAsync(test, expectedDiagnostic, fixedSource);
        }

        public static IEnumerable<object[]> VariablePatternTestData()
        {
            foreach (InvocationType invocationType in new[] { InvocationType.Constructor, InvocationType.StaticMethods })
            {
                string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;
                // Test constructor with passed in parameter
                yield return new object[] { @"using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "args[0]") + isMatchInvocation + @";
    }
}" };

                // Test constructor with passed in variable
                yield return new object[] { @"using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        string somePattern = """";
        var isMatch = " + ConstructRegexInvocation(invocationType, "somePattern") + isMatchInvocation + @";
    }
}" };

                // Test constructor with readonly property
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public string Pattern { get; }

    public void M()
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "Pattern") + isMatchInvocation + @";
    }
}" };

                // Test constructor with field
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public readonly string Pattern;

    public void M()
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "Pattern") + isMatchInvocation + @";
    }
}" };

                // Test constructor with return method
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public string GetMyPattern() => """";

    public void M()
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "GetMyPattern()") + isMatchInvocation + @";
    }
}" };
            }
        }

        [Theory]
        [MemberData(nameof(VariablePatternTestData))]
        public async Task DiagnosticNotEmittedForVariablePattern(string test)
            => await VerifyCS.VerifyAnalyzerAsync(test);

        public static IEnumerable<object[]> ConstantOptionsTestData()
        {
            foreach (InvocationType invocationType in new[] { InvocationType.Constructor, InvocationType.StaticMethods })
            {
                string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;
                // Test options as passed in literal
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var isMatch = {|#0:" + ConstructRegexInvocation(invocationType, "\"\"", "RegexOptions.None") + @"|}" + isMatchInvocation + @";
    }
}", @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [RegexGenerator("""", RegexOptions.None)]
    private static partial Regex MyRegex();
}" };

                // Test options as local constant
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        var isMatch = {|#0:" + ConstructRegexInvocation(invocationType, "\"\"", "options") + @"|}" + isMatchInvocation + @";
    }
}", @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        var isMatch = MyRegex().IsMatch("""");
    }

    [RegexGenerator("""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}" };

                // Test options as constant field
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    const RegexOptions Options = RegexOptions.None;

    public static void Main(string[] args)
    {
        var isMatch = {|#0:" + ConstructRegexInvocation(invocationType, "\"\"", "Options") + @"|}" + isMatchInvocation + @";
    }
}", @"using System.Text.RegularExpressions;

public partial class Program
{
    const RegexOptions Options = RegexOptions.None;

    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [RegexGenerator("""", RegexOptions.None)]
    private static partial Regex MyRegex();
}" };
            }
        }

        [Theory]
        [MemberData(nameof(ConstantOptionsTestData))]
        public async Task DiagnosticEmittedForConstantOptions(string test, string fixedSource)
        {
            DiagnosticResult expected = VerifyCS.Diagnostic(UseRegexSourceGeneratorDiagnosticId).WithLocation(0);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixedSource);
        }

        public static IEnumerable<object[]> VariableOptionsTestData()
        {
            foreach (InvocationType invocationType in new[] { InvocationType.Constructor, InvocationType.StaticMethods })
            {
                string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;
                // Test options as passed in parameter
                yield return new object[] { @"using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(RegexOptions options)
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "\"\"", "options") + isMatchInvocation + @";
    }
}" };

                // Test options as passed in variable
                yield return new object[] { @"using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        RegexOptions options = RegexOptions.None;
        var isMatch = " + ConstructRegexInvocation(invocationType, "\"\"", "options") + isMatchInvocation + @";
    }
}" };

                // Test options as readonly property
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public RegexOptions Options { get; }

    public void M()
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "\"\"", "Options") + isMatchInvocation + @";
    }
}" };

                // Test options as readonly field
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public readonly RegexOptions Options;

    public void M()
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "\"\"", "Options") + isMatchInvocation + @";
    }
}" };

                // Test options as return method.
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public RegexOptions GetMyOptions() => RegexOptions.None;

    public void M()
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "\"\"", "GetMyOptions()") + isMatchInvocation + @";
    }
}" };
            }
        }

        [Theory]
        [MemberData(nameof(VariableOptionsTestData))]
        public async Task DiagnosticNotEmittedForVariableOptions(string test)
            => await VerifyCS.VerifyAnalyzerAsync(test);

        public static IEnumerable<object[]> StaticInvocationsAndFixedSourceTestData()
        {
            const string testTemplateWithOptions = @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        {|#0:Regex.@@Method@@(""input"", ""a|b"", RegexOptions.None)|};
    }
}";
            const string fixedSourceWithOptions = @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        MyRegex().@@Method@@(""input"");
    }

    [RegexGenerator(""a|b"", RegexOptions.None)]
    private static partial Regex MyRegex();
}";
            DiagnosticResult expectedDiagnostic = VerifyCS.Diagnostic(UseRegexSourceGeneratorDiagnosticId).WithLocation(0);

            const string testTemplateWithoutOptions = @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        {|#0:Regex.@@Method@@(""input"", ""a|b"")|};
    }
}";
            const string fixedSourceWithoutOptions = @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        MyRegex().@@Method@@(""input"");
    }

    [RegexGenerator(""a|b"")]
    private static partial Regex MyRegex();
}";

            foreach (bool includeRegexOptions in new[] { true, false })
            {
                foreach (string methodName in new[] { "Count", "EnumerateMatches" , "IsMatch", "Match", "Matches", "Split" })
                {
                    if (includeRegexOptions)
                    {
                        yield return new object[] { testTemplateWithOptions.Replace("@@Method@@", methodName), expectedDiagnostic, fixedSourceWithOptions.Replace("@@Method@@", methodName) };
                    }
                    else
                    {
                        yield return new object[] { testTemplateWithoutOptions.Replace("@@Method@@", methodName), expectedDiagnostic, fixedSourceWithoutOptions.Replace("@@Method@@", methodName) };

                    }
                }
            }

            // Replace has one additional parameter so we treat that case separately.

            yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        {|#0:Regex.Replace(""input"", ""a[b|c]*"", ""replacement"", RegexOptions.CultureInvariant)|};
    }
}
", expectedDiagnostic, @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        MyRegex().Replace(""input"", ""replacement"");
    }

    [RegexGenerator(""a[b|c]*"", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
" };

            yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        {|#0:Regex.Replace(""input"", ""a[b|c]*"", ""replacement"")|};
    }
}
", expectedDiagnostic, @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        MyRegex().Replace(""input"", ""replacement"");
    }

    [RegexGenerator(""a[b|c]*"")]
    private static partial Regex MyRegex();
}
" };
        }

        [Theory]
        [MemberData(nameof(StaticInvocationsAndFixedSourceTestData))]
        public async Task DiagnosticAndCodeFixForAllStaticMethods(string test, DiagnosticResult expectedDiagnostic, string fixedSource)
         => await VerifyCS.VerifyCodeFixAsync(test, expectedDiagnostic, fixedSource);

        [Fact]
        public async Task CodeFixSupportsNesting()
        {
            DiagnosticResult expectedDiagnostic = VerifyCS.Diagnostic(UseRegexSourceGeneratorDiagnosticId).WithLocation(0);
            string test = @"using System.Text.RegularExpressions;

public class A
{
    public partial class B
    {
        public class C
        {
            public partial class D
            {
                public void Foo()
                {
                    Regex regex = {|#0:new Regex(""pattern"", RegexOptions.IgnorePatternWhitespace)|};
                }
            }
        }
    }
}
";
            string fixedSource = @"using System.Text.RegularExpressions;

public partial class A
{
    public partial class B
    {
        public partial class C
        {
            public partial class D
            {
                public void Foo()
                {
                    Regex regex = MyRegex();
                }

                [RegexGenerator(""pattern"", RegexOptions.IgnorePatternWhitespace)]
                private static partial Regex MyRegex();
            }
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(test, expectedDiagnostic, fixedSource);
        }

        [Theory]
        [MemberData(nameof(InvocationTypes))]
        public async Task NoDiagnosticForRegexOptionsNonBacktracking(InvocationType invocationType)
        {
            string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;
            string test = @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var isMatch = " + ConstructRegexInvocation(invocationType, "\"\"", "RegexOptions.IgnoreCase | RegexOptions.NonBacktracking") + isMatchInvocation + @";
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AnayzerSupportsMultipleDiagnostics()
        {
            string test = @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main()
    {
        Regex regex1 = {|#0:new Regex(""a|b"")|};
        Regex regex2 = {|#1:new Regex(""c|d"", RegexOptions.CultureInvariant)|};
    }
}
";
            DiagnosticResult[] expectedDiagnostics = new[]
            {
                VerifyCS.Diagnostic(UseRegexSourceGeneratorDiagnosticId).WithLocation(0),
                VerifyCS.Diagnostic(UseRegexSourceGeneratorDiagnosticId).WithLocation(1)
            };

            string fixedSource = @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main()
    {
        Regex regex1 = MyRegex();
        Regex regex2 = MyRegex1();
    }

    [RegexGenerator(""a|b"")]
    private static partial Regex MyRegex();
    [RegexGenerator(""c|d"", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex1();
}
";

            await VerifyCS.VerifyCodeFixAsync(test, expectedDiagnostics, fixedSource, 2);
        }

        [Fact]
        public async Task CodeFixerSupportsNamedParameters()
        {
            string test = @"using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        Regex r = {|#0:new Regex(options: RegexOptions.None, pattern: ""a|b"")|};
    }
}";
            DiagnosticResult expectedDiagnostic = VerifyCS.Diagnostic(UseRegexSourceGeneratorDiagnosticId).WithLocation(0);

            string fixedSource = @"using System.Text.RegularExpressions;

partial class Program
{
    static void Main(string[] args)
    {
        Regex r = MyRegex();
    }

    [RegexGenerator(""a|b"", RegexOptions.None)]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, expectedDiagnostic, fixedSource);
        }

        #region Test helpers

        private static string ConstructRegexInvocation(InvocationType invocationType, string pattern, string? options = null)
            => invocationType switch
            {
                InvocationType.StaticMethods => (pattern is null, options is null) switch
                {
                    (false, true) => $"Regex.IsMatch(\"\", {pattern})",
                    (false, false) => $"Regex.IsMatch(\"\", {pattern}, {options})",
                    _ => throw new InvalidOperationException()
                },
                InvocationType.Constructor => (pattern is null, options is null) switch
                {
                    (false, true) => $"new Regex({pattern})",
                    (false, false) => $"new Regex({pattern}, {options})",
                    _ => throw new InvalidOperationException()
                },
                _ => throw new ArgumentOutOfRangeException(nameof(invocationType))
            };

        public static IEnumerable<object[]> InvocationTypes
            => new object[][]
            {
                new object[] { InvocationType.StaticMethods },
                new object[] { InvocationType.Constructor }
            };

        public enum InvocationType
        {
            StaticMethods,
            Constructor
        }

        #endregion Test helpers
    }
}
