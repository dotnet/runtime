// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyCS = System.Text.RegularExpressions.Tests.CSharpCodeFixVerifier<
    System.Text.RegularExpressions.Generator.UpgradeToGeneratedRegexAnalyzer,
    System.Text.RegularExpressions.Generator.UpgradeToGeneratedRegexCodeFixer>;

namespace System.Text.RegularExpressions.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/69823", TestRuntimes.Mono)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
    public class UpgradeToGeneratedRegexConstantPreservationTests
    {
        [Fact]
        public async Task PreservesConstantRegexOptionsReference_LocalConstant()
        {
            string test = @"using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        const RegexOptions MyOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        Regex regex = [|new Regex(""asdf"", MyOptions)|];
    }
}";

            string fixedSource = @"using System.Text.RegularExpressions;

public partial class Program
{
    public static void Main(string[] args)
    {
        const RegexOptions MyOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        Regex regex = MyRegex();
    }

    [GeneratedRegex(""asdf"", MyOptions)]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }

        [Fact]
        public async Task PreservesConstantRegexOptionsReference_ClassConstant()
        {
            string test = @"using System.Text.RegularExpressions;

public class Program
{
    private const RegexOptions MyOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

    public static void Main(string[] args)
    {
        Regex regex = [|new Regex(""asdf"", MyOptions)|];
    }
}";

            string fixedSource = @"using System.Text.RegularExpressions;

public partial class Program
{
    private const RegexOptions MyOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

    public static void Main(string[] args)
    {
        Regex regex = MyRegex();
    }

    [GeneratedRegex(""asdf"", MyOptions)]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }

        [Fact]
        public async Task PreservesConstantRegexOptionsReference_StaticMethod()
        {
            string test = @"using System.Text.RegularExpressions;

public class Program
{
    private const RegexOptions MyOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

    public static void Main(string[] args)
    {
        bool isMatch = [|Regex.IsMatch(""input"", ""pattern"", MyOptions)|];
    }
}";

            string fixedSource = @"using System.Text.RegularExpressions;

public partial class Program
{
    private const RegexOptions MyOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

    public static void Main(string[] args)
    {
        bool isMatch = MyRegex().IsMatch(""input"");
    }

    [GeneratedRegex(""pattern"", MyOptions)]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }

        [Fact]
        public async Task PreservesConstantRegexOptionsReference_ExternalConstant()
        {
            string test = @"using System.Text.RegularExpressions;

public class RegexConstants
{
    public const RegexOptions DefaultOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
}

public class Program
{
    public static void Main(string[] args)
    {
        Regex regex = [|new Regex(""asdf"", RegexConstants.DefaultOptions)|];
    }
}";

            string fixedSource = @"using System.Text.RegularExpressions;

public class RegexConstants
{
    public const RegexOptions DefaultOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
}

public partial class Program
{
    public static void Main(string[] args)
    {
        Regex regex = MyRegex();
    }

    [GeneratedRegex(""asdf"", RegexConstants.DefaultOptions)]
    private static partial Regex MyRegex();
}";

            await VerifyCS.VerifyCodeFixAsync(test, fixedSource);
        }
    }
}