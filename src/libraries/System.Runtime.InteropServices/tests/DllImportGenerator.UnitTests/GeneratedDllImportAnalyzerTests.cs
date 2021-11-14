// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.GeneratedDllImportAnalyzer;

using VerifyCS = DllImportGenerator.UnitTests.Verifiers.CSharpAnalyzerVerifier<Microsoft.Interop.Analyzers.GeneratedDllImportAnalyzer>;

namespace DllImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class GeneratedDllImportAnalyzerTests
    {
        [ConditionalFact]
        public async Task NonPartialMethod_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public static void {|#0:Method1|}() { }

    [GeneratedDllImport(""DoesNotExist"")]
    static void {|#1:Method2|}() { }

    [GeneratedDllImport(""DoesNotExist"")]
    public static extern void {|#2:ExternMethod1|}();

    [GeneratedDllImport(""DoesNotExist"")]
    static extern void {|#3:ExternMethod2|}();
}
";
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(0)
                    .WithArguments("Method1"),
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(1)
                    .WithArguments("Method2"),
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(2)
                    .WithArguments("ExternMethod1"),
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(3)
                    .WithArguments("ExternMethod2"));
        }

        [ConditionalFact]
        public async Task NonStaticMethod_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public partial void {|#0:Method1|}();

    [GeneratedDllImport(""DoesNotExist"")]
    partial void {|#1:Method2|}();
}

partial class Test
{
    public partial void {|#3:Method1|}() { }
}
";
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(0)
                    .WithArguments("Method1"),
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(1)
                    .WithArguments("Method2"),
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(3)
                    .WithArguments("Method1"));
        }

        [ConditionalFact]
        public async Task NonPartialNonStaticMethod_ReportsDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public void {|#0:Method1|}() { }

    [GeneratedDllImport(""DoesNotExist"")]
    void {|#1:Method2|}() { }

    [GeneratedDllImport(""DoesNotExist"")]
    public extern void {|#2:ExternMethod1|}();

    [GeneratedDllImport(""DoesNotExist"")]
    extern void {|#3:ExternMethod2|}();
}
";
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(0)
                    .WithArguments("Method1"),
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(1)
                    .WithArguments("Method2"),
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(2)
                    .WithArguments("ExternMethod1"),
                VerifyCS.Diagnostic(GeneratedDllImportMissingModifiers)
                    .WithLocation(3)
                    .WithArguments("ExternMethod2"));
        }

        [ConditionalFact]
        public async Task NotGeneratedDllImport_NoDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    public void Method1() { }
    partial void Method2();
}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [ConditionalFact]
        public async Task StaticPartialMethod_NoDiagnostic()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    static partial void Method2();
}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [ConditionalTheory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]

        public async Task NonPartialParentType_Diagnostic(string typeKind)
        {
            string source = $@"
using System.Runtime.InteropServices;
{typeKind} {{|#0:Test|}}
{{
    [GeneratedDllImport(""DoesNotExist"")]
    static partial void {{|CS0751:Method2|}}();
}}
";

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(GeneratedDllImportContainingTypeMissingModifiers)
                    .WithLocation(0)
                    .WithArguments("Test"));
        }

        [ConditionalTheory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("record")]

        public async Task NonPartialGrandparentType_Diagnostic(string typeKind)
        {

            string source = $@"
using System.Runtime.InteropServices;
{typeKind} {{|#0:Test|}}
{{
    partial class TestInner
    {{
        [GeneratedDllImport(""DoesNotExist"")]
        static partial void Method2();
    }}
}}
";

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(GeneratedDllImportContainingTypeMissingModifiers)
                    .WithLocation(0)
                    .WithArguments("Test"));
        }
    }
}
