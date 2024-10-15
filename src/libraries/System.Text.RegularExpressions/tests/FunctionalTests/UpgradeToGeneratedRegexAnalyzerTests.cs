// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = System.Text.RegularExpressions.Tests.CSharpCodeFixVerifier<
    System.Text.RegularExpressions.Generator.UpgradeToGeneratedRegexAnalyzer,
    System.Text.RegularExpressions.Generator.UpgradeToGeneratedRegexCodeFixer>;

namespace System.Text.RegularExpressions.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/69823", TestRuntimes.Mono)]
    public class UpgradeToGeneratedRegexAnalyzerTests
    {
        private const string UseRegexSourceGeneratorDiagnosticId = @"SYSLIB1045";

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
        var regex = new Regex("""", matchTimeout: TimeSpan.FromSeconds(10), options: RegexOptions.None);
    }
}" };

            yield return new object[] { @"using System;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var regex = new Regex(matchTimeout: TimeSpan.FromSeconds(10), pattern: """", options: RegexOptions.None);
    }
}" };

            yield return new object[] { @"using System;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var regex = new Regex(matchTimeout: TimeSpan.FromSeconds(10), options: RegexOptions.None, pattern: """");
    }
}" };
        }

        [Theory]
        [MemberData(nameof(ConstructorWithTimeoutTestData))]
        public async Task NoDiagnosticForConstructorWithTimeout(string test)
        {
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Theory]
        [MemberData(nameof(InvocationTypes))]
        public async Task TopLevelStatements(InvocationType invocationType)
        {
            string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;
            string test = @"using System.Text.RegularExpressions;
var isMatch = [|" + ConstructRegexInvocation(invocationType, pattern: "\"\"") + @"|]" + isMatchInvocation + ";";
            string fixedCode = @"using System.Text.RegularExpressions;
var isMatch = MyRegex().IsMatch("""");

partial class Program
{
    [GeneratedRegex("""")]
    private static partial Regex MyRegex();
}";
            await new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = fixedCode,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
            }.RunAsync();
        }

        public static IEnumerable<object[]> StaticInvocationWithTimeoutTestData()
        {
            foreach (string method in new[] { "Count", "EnumerateMatches", "IsMatch", "Match", "Matches", "Split" })
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

            await VerifyCS.VerifyAnalyzerAsync(test, ReferenceAssemblies.Net.Net60);
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
            await new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = test,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact]
        public async Task CodeFixSupportsInvalidPatternFromWhichOptionsCanBeParsed()
        {
            string pattern = ".{99999999999}"; // throws during real parse
            string test = $@"using System.Text;
using System.Text.RegularExpressions;

public class Program
{{
    public static void Main(string[] args)
    {{
        var isMatch = [|Regex.IsMatch("""", @""{pattern}"")|];
    }}
}}";

            string fixedSource = @$"using System.Text;
using System.Text.RegularExpressions;

public partial class Program
{{
    public static void Main(string[] args)
    {{
        var isMatch = MyRegex().IsMatch("""");
    }}

    [GeneratedRegex(@""{pattern}"")]
    private static partial Regex MyRegex();
}}";
            // We successfully offer the diagnostic and make the fix despite the pattern
            // being invalid, because it was valid enough to parse out any options in the pattern.
            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }

        [Fact]
        public async Task CodeFixIsNotOfferedForInvalidPatternFromWhichOptionsCannotBeParsed()
        {
            string pattern = "\\g"; // throws during pre-parse for options
            string test = $@"using System.Text;
using System.Text.RegularExpressions;

public class Program
{{
    public static void Main(string[] args)
    {{
        var isMatch = Regex.IsMatch("""", @""{pattern}""); // fixer not offered
    }}
}}";
            // We need to be able to parse the pattern sufficiently that we can extract
            // any inline options; in this case we can't, so we don't offer the fix.
            await VerifyCS.VerifyCodeFixAsync(test, test);
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
        var isMatch = [|" + ConstructRegexInvocation(invocationType, "\"\"") + @"|]" + isMatchInvocation + @";
    }
}", @"using System.Text;
using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [GeneratedRegex("""")]
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
        var isMatch = [|" + ConstructRegexInvocation(invocationType, "\"\"") + @"|]" + isMatchInvocation + @";
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

    [GeneratedRegex("""")]
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
        var isMatch = [|" + ConstructRegexInvocation(invocationType, "\"\"") + @"|]" + isMatchInvocation + @";
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

    [GeneratedRegex("""")]
    private static partial Regex MyRegex();
}" };
            }
        }

        [Theory]
        [MemberData(nameof(ConstantPatternTestData))]
        public async Task DiagnosticEmittedForConstantPattern(string test, string fixedSource)
        {
            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
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
        var isMatch = [|" + ConstructRegexInvocation(invocationType, "\"\"", "RegexOptions.None") + @"|]" + isMatchInvocation + @";
    }
}", @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [GeneratedRegex("""", RegexOptions.None)]
    private static partial Regex MyRegex();
}" };

                // Test options as local constant
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        var isMatch = [|" + ConstructRegexInvocation(invocationType, "\"\"", "options") + @"|]" + isMatchInvocation + @";
    }
}", @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        var isMatch = MyRegex().IsMatch("""");
    }

    [GeneratedRegex("""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}" };

                // Test options as constant field
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    const RegexOptions Options = RegexOptions.None;

    public static void Main(string[] args)
    {
        var isMatch = [|" + ConstructRegexInvocation(invocationType, "\"\"", "Options") + @"|]" + isMatchInvocation + @";
    }
}", @"using System.Text.RegularExpressions;

public partial class Program
{
    const RegexOptions Options = RegexOptions.None;

    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [GeneratedRegex("""", RegexOptions.None)]
    private static partial Regex MyRegex();
}" };
            }
        }

        [Theory]
        [MemberData(nameof(ConstantOptionsTestData))]
        public async Task DiagnosticEmittedForConstantOptions(string test, string fixedSource)
        {
            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
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
        [|Regex.@@Method@@(""input"", ""a|b"", RegexOptions.None)|];
    }
}";
            const string fixedSourceWithOptions = @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        MyRegex().@@Method@@(""input"");
    }

    [GeneratedRegex(""a|b"", RegexOptions.None)]
    private static partial Regex MyRegex();
}";

            const string testTemplateWithoutOptions = @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        [|Regex.@@Method@@(""input"", ""a|b"")|];
    }
}";
            const string fixedSourceWithoutOptions = @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        MyRegex().@@Method@@(""input"");
    }

    [GeneratedRegex(""a|b"")]
    private static partial Regex MyRegex();
}";

            foreach (bool includeRegexOptions in new[] { true, false })
            {
                foreach (string methodName in new[] { "Count", "EnumerateMatches", "IsMatch", "Match", "Matches", "Split" })
                {
                    if (includeRegexOptions)
                    {
                        yield return new object[] { testTemplateWithOptions.Replace("@@Method@@", methodName), fixedSourceWithOptions.Replace("@@Method@@", methodName) };
                    }
                    else
                    {
                        yield return new object[] { testTemplateWithoutOptions.Replace("@@Method@@", methodName), fixedSourceWithoutOptions.Replace("@@Method@@", methodName) };

                    }
                }
            }

            // Replace has one additional parameter so we treat that case separately.

            yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        [|Regex.Replace(""input"", ""a[b|c]*"", ""replacement"", RegexOptions.CultureInvariant)|];
    }
}
", @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        MyRegex().Replace(""input"", ""replacement"");
    }

    [GeneratedRegex(""a[b|c]*"", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
" };

            yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        [|Regex.Replace(""input"", ""a[b|c]*"", ""replacement"")|];
    }
}
", @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        MyRegex().Replace(""input"", ""replacement"");
    }

    [GeneratedRegex(""a[b|c]*"")]
    private static partial Regex MyRegex();
}
" };
        }

        [Theory]
        [MemberData(nameof(StaticInvocationsAndFixedSourceTestData))]
        public async Task DiagnosticAndCodeFixForAllStaticMethods(string test, string fixedSource)
         => await VerifyCS.VerifyCodeFixAsync(test, fixedSource);

        [Fact]
        public async Task CodeFixSupportsNesting()
        {
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
                    Regex regex = [|new Regex(""pattern"", RegexOptions.IgnorePatternWhitespace)|];
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

                [GeneratedRegex(""pattern"", RegexOptions.IgnorePatternWhitespace)]
                private static partial Regex MyRegex();
            }
        }
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
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
        Regex regex1 = [|new Regex(""a|b"")|];
        Regex regex2 = [|new Regex(""c|d"", RegexOptions.CultureInvariant)|];
    }
}
";
            string fixedSource = @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main()
    {
        Regex regex1 = MyRegex();
        Regex regex2 = MyRegex1();
    }

    [GeneratedRegex(""a|b"")]
    private static partial Regex MyRegex();
    [GeneratedRegex(""c|d"", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex1();
}
";
            await new VerifyCS.Test
            {
                TestCode = test,
                FixedCode = fixedSource,
                NumberOfFixAllIterations = 2,
            }.RunAsync();
        }

        [Fact]
        public async Task CodeFixerSupportsNamedParameters()
        {
            string test = @"using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        Regex r = [|new Regex(options: RegexOptions.None, pattern: ""a|b"")|];
    }
}";

            string fixedSource = @"using System.Text.RegularExpressions;

partial class Program
{
    static void Main(string[] args)
    {
        Regex r = MyRegex();
    }

    [GeneratedRegex(""a|b"", RegexOptions.None)]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }

        [Fact]
        public async Task CodeFixerDoesNotSimplifyStyle()
        {
            string test = @"using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        int i = (4 - 4); // this shouldn't be changed by fixer
        Regex r = [|new Regex(options: RegexOptions.None, pattern: ""a|b"")|];
    }
}";

            string fixedSource = @"using System.Text.RegularExpressions;

partial class Program
{
    static void Main()
    {
        int i = (4 - 4); // this shouldn't be changed by fixer
        Regex r = MyRegex();
    }

    [GeneratedRegex(""a|b"", RegexOptions.None)]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }

        [Fact]
        public async Task TopLevelStatements_MultipleSourceFiles()
        {
            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { "public class C { }", @"var r = [|new System.Text.RegularExpressions.Regex("""")|];" },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedState =
                {
                    Sources = { "public class C { }", @"var r = MyRegex();

partial class Program
{
    [System.Text.RegularExpressions.GeneratedRegex("""")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}" }
                },
            }.RunAsync();
        }

        [Fact]
        public async Task LeadingAndTrailingTriviaIsPreservedByFixer()
        {
            string test = @"using System.Text.RegularExpressions;

static class Class
{
    public static string CollapseWhitespace(this string text) =>
        [|Regex.Replace(text, "" \\s+"" , ""  "")|];
}";

            string expectedFixedCode = @"using System.Text.RegularExpressions;

static partial class Class
{
    public static string CollapseWhitespace(this string text) =>
        MyRegex().Replace(text, ""  "");
    [GeneratedRegex("" \\s+"")]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, expectedFixedCode);
        }

        [Fact]
        public async Task VerbatimStringLiteralSyntaxPreservedByFixer()
        {
            string test = @"using System.Text.RegularExpressions;

static class Class
{
    public static string CollapseWhitespace(this string text) =>
        [|Regex.Replace(text, @"" \s+"" , @""  "")|];
}";

            string expectedFixedCode = @"using System.Text.RegularExpressions;

static partial class Class
{
    public static string CollapseWhitespace(this string text) =>
        MyRegex().Replace(text, @""  "");
    [GeneratedRegex(@"" \s+"")]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, expectedFixedCode);
        }

        [Fact]
        public async Task RawStringLiteralSyntaxPreservedByFixer()
        {
            string test = @"using System.Text.RegularExpressions;

static class Class
{
    public static string CollapseWhitespace(this string text) =>
        [|Regex.Replace(text, """"""
                              \s+
                              """""",
                              """""""" hello """""" world """""""")|];
}";

            string expectedFixedCode = @"using System.Text.RegularExpressions;

static partial class Class
{
    public static string CollapseWhitespace(this string text) =>
        MyRegex().Replace(text, """""""" hello """""" world """""""");
    [GeneratedRegex(""""""
                              \s+
                              """""")]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, expectedFixedCode);
        }

        [Fact]
        public async Task InterpolatedStringLiteralSyntaxPreservedByFixer()
        {
            string test = @"using System.Text.RegularExpressions;

partial class Program
{
    static void Main(string[] args)
    {
        const string pattern = @""a|b\s\n"";
        const string pattern2 = $""{pattern}2"";

        Regex regex = [|new Regex(pattern2)|];
    }
}";

            string expectedFixedCode = @"using System.Text.RegularExpressions;

partial class Program
{
    static void Main(string[] args)
    {
        const string pattern = @""a|b\s\n"";
        const string pattern2 = $""{pattern}2"";

        Regex regex = MyRegex();
    }

    [GeneratedRegex(@""a|b\s\n2"")]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, expectedFixedCode);
        }

        [Fact]
        public async Task InterpolatedStringLiteralSyntaxFixedWhenStringLiteralIsConstantField()
        {
            string test = @"using System.Text.RegularExpressions;

partial class Program
{
    const string pattern = @""a|b\s\n"";
    const string pattern2 = $""{pattern}2"";

    static void Main(string[] args)
    {
        Regex regex = [|new Regex(pattern2)|];
    }
}";

            string expectedFixedCode = @"using System.Text.RegularExpressions;

partial class Program
{
    const string pattern = @""a|b\s\n"";
    const string pattern2 = $""{pattern}2"";

    static void Main(string[] args)
    {
        Regex regex = MyRegex();
    }

    [GeneratedRegex(pattern2)]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, expectedFixedCode);
        }

        [Fact]
        public async Task InterpolatedStringLiteralSyntaxFixedWhenStringLiteralIsExternalConstantField()
        {
            string test = @"using System.Text.RegularExpressions;

internal class GlobalConstants
{
    const string pattern = @""a|b\s\n"";
    internal const string pattern2 = $""{pattern}2"";
}
partial class Program
{
    static void Main(string[] args)
    {
        Regex regex = [|new Regex(GlobalConstants.pattern2)|];
    }
}";

            string expectedFixedCode = @"using System.Text.RegularExpressions;

internal class GlobalConstants
{
    const string pattern = @""a|b\s\n"";
    internal const string pattern2 = $""{pattern}2"";
}
partial class Program
{
    static void Main(string[] args)
    {
        Regex regex = MyRegex();
    }

    [GeneratedRegex(GlobalConstants.pattern2)]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, expectedFixedCode);
        }

        [Fact]
        public async Task TestAsArgument()
        {
            string test = @"using System.Text.RegularExpressions;
public class C
{
    void M1(Regex r) => _ = r;
    void M2() => M1([|new Regex("""")|]);
}
";

            string fixedCode = @"using System.Text.RegularExpressions;
public partial class C
{
    void M1(Regex r) => _ = r;
    void M2() => M1(MyRegex());
    [GeneratedRegex("""")]
    private static partial Regex MyRegex();
}
";

            await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
        }

        [Fact]
        public async Task InvalidRegexOptions()
        {
            string test = @"using System.Text.RegularExpressions;

public class A
{
    public void Foo()
    {
        Regex regex = [|new Regex(""pattern"", (RegexOptions)0x0800)|];
    }
}
";
            string fixedSource = @"using System.Text.RegularExpressions;

public partial class A
{
    public void Foo()
    {
        Regex regex = MyRegex();
    }

    [GeneratedRegex(""pattern"", (RegexOptions)(2048))]
    private static partial Regex MyRegex();
}
";

            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }

        [Fact]
        public async Task InvalidRegexOptions_LocalConstant()
        {
            string test = @"using System.Text.RegularExpressions;

public class A
{
    public void Foo()
    {
        const RegexOptions MyOptions = (RegexOptions)0x0800;
        Regex regex = [|new Regex(""pattern"", MyOptions)|];
    }
}
";
            string fixedSource = @"using System.Text.RegularExpressions;

public partial class A
{
    public void Foo()
    {
        const RegexOptions MyOptions = (RegexOptions)0x0800;
        Regex regex = MyRegex();
    }

    [GeneratedRegex(""pattern"", (RegexOptions)(2048))]
    private static partial Regex MyRegex();
}
";

            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }

        [Fact]
        public async Task InvalidRegexOptions_Negative()
        {
            string test = @"using System.Text.RegularExpressions;

public class A
{
    public void Foo()
    {
        Regex regex = [|new Regex(""pattern"", (RegexOptions)(-10000))|];
    }
}
";
            string fixedSource = @"using System.Text.RegularExpressions;

public partial class A
{
    public void Foo()
    {
        Regex regex = MyRegex();
    }

    [GeneratedRegex(""pattern"", (RegexOptions)(-10000))]
    private static partial Regex MyRegex();
}
";
            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }

        public static IEnumerable<object[]> DetectsCurrentCultureTestData()
        {
            foreach (InvocationType invocationType in new[] { InvocationType.Constructor, InvocationType.StaticMethods })
            {
                string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;

                foreach (bool useInlineIgnoreCase in new[] { true, false })
                {
                    string pattern = useInlineIgnoreCase ? "\"(?:(?>abc)(?:(?s)d|e)(?:(?:(?xi)ki)*))\"" : "\"abc\"";
                    string options = useInlineIgnoreCase ? "RegexOptions.None" : "RegexOptions.IgnoreCase";

                    // Test using current culture
                    yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var isMatch = [|" + ConstructRegexInvocation(invocationType, pattern, options) + @"|]" + isMatchInvocation + @";
    }
}", @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [GeneratedRegex(" + $"{pattern}, {options}, \"{CultureInfo.CurrentCulture.Name}" + @""")]
    private static partial Regex MyRegex();
}" };

                    // Test using CultureInvariant which should default to the 2 parameter constructor
                    options = useInlineIgnoreCase ? "RegexOptions.CultureInvariant" : "RegexOptions.IgnoreCase | RegexOptions.CultureInvariant";
                    yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var isMatch = [|" + ConstructRegexInvocation(invocationType, pattern, options) + @"|]" + isMatchInvocation + @";
    }
}", @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [GeneratedRegex(" + $"{pattern}, {options}" + @")]
    private static partial Regex MyRegex();
}" };
                }
            }
        }

        public static IEnumerable<object[]> NoOptionsCultureTestData()
        {
            foreach (InvocationType invocationType in new[] { InvocationType.Constructor, InvocationType.StaticMethods })
            {
                string isMatchInvocation = invocationType == InvocationType.Constructor ? @".IsMatch("""")" : string.Empty;

                // Test no options passed in
                yield return new object[] { @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var isMatch = [|" + ConstructRegexInvocation(invocationType, "\"(?i)abc\"") + @"|]" + isMatchInvocation + @";
    }
}", @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        var isMatch = MyRegex().IsMatch("""");
    }

    [GeneratedRegex(" + $"\"(?i)abc\", RegexOptions.None, \"{CultureInfo.CurrentCulture.Name}" + @""")]
    private static partial Regex MyRegex();
}" };
            }
        }

        [Theory]
        [MemberData(nameof(DetectsCurrentCultureTestData))]
        [MemberData(nameof(NoOptionsCultureTestData))]
        public async Task DetectsCurrentCulture(string test, string fixedSource)
            => await VerifyCS.VerifyCodeFixAsync(test, fixedSource);

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
                [InvocationType.StaticMethods],
                [InvocationType.Constructor]
            };

        public enum InvocationType
        {
            StaticMethods,
            Constructor
        }

        #endregion Test helpers
    }
}
