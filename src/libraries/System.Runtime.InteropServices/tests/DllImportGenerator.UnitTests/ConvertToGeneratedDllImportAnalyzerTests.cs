using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.ConvertToGeneratedDllImportAnalyzer;

using VerifyCS = DllImportGenerator.UnitTests.Verifiers.CSharpAnalyzerVerifier<Microsoft.Interop.Analyzers.ConvertToGeneratedDllImportAnalyzer>;

namespace DllImportGenerator.UnitTests
{
    public class ConvertToGeneratedDllImportAnalyzerTests
    {
        public static IEnumerable<object[]> MarshallingRequiredTypes() => new[]
        {
            new object[] { typeof(bool) },
            new object[] { typeof(char) },
            new object[] { typeof(string) },
            new object[] { typeof(int[]) },
            new object[] { typeof(string[]) },
            new object[] { typeof(ConsoleKeyInfo) }, // struct
        };

        public static IEnumerable<object[]> NoMarshallingRequiredTypes() => new[]
        {
            new object[] { typeof(byte) },
            new object[] { typeof(int) },
            new object[] { typeof(byte*) },
            new object[] { typeof(int*) },
            new object[] { typeof(bool*) },
            new object[] { typeof(char*) },
            new object[] { typeof(delegate* <void>) },
            new object[] { typeof(IntPtr) },
            new object[] { typeof(ConsoleKey) }, // enum
        };

        [Theory]
        [MemberData(nameof(MarshallingRequiredTypes))]
        public async Task TypeRequiresMarshalling_ReportsDiagnostic(Type type)
        {
            string source = DllImportWithType(type.FullName!);
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(0)
                    .WithArguments("Method_Parameter"),
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(1)
                    .WithArguments("Method_Return"));
        }

        [Theory]
        [MemberData(nameof(MarshallingRequiredTypes))]
        [MemberData(nameof(NoMarshallingRequiredTypes))]
        public async Task ByRef_ReportsDiagnostic(Type type)
        {
            string typeName = type.FullName!;
            string source = @$"
using System.Runtime.InteropServices;
unsafe partial class Test
{{
    [DllImport(""DoesNotExist"")]
    public static extern void {{|#0:Method_In|}}(in {typeName} p);

    [DllImport(""DoesNotExist"")]
    public static extern void {{|#1:Method_Out|}}(out {typeName} p);

    [DllImport(""DoesNotExist"")]
    public static extern void {{|#2:Method_Ref|}}(ref {typeName} p);
}}
";
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(0)
                    .WithArguments("Method_In"),
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(1)
                    .WithArguments("Method_Out"),
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(2)
                    .WithArguments("Method_Ref"));
        }

        [Fact]
        public async Task PreserveSigFalse_ReportsDiagnostic()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", PreserveSig = false)]
    public static extern void {{|#0:Method1|}}();

    [DllImport(""DoesNotExist"", PreserveSig = true)]
    public static extern void Method2();
}}
";
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(0)
                    .WithArguments("Method1"));
        }

        [Fact]
        public async Task SetLastErrorTrue_ReportsDiagnostic()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", SetLastError = false)]
    public static extern void Method1();

    [DllImport(""DoesNotExist"", SetLastError = true)]
    public static extern void {{|#0:Method2|}}();
}}
";
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(0)
                    .WithArguments("Method2"));
        }

        [Theory]
        [MemberData(nameof(NoMarshallingRequiredTypes))]
        public async Task BlittablePrimitive_NoDiagnostic(Type type)
        {
            string source = DllImportWithType(type.FullName!);
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NotDllImport_NoDiagnostic()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    public static extern bool Method1(bool p, in bool pIn, ref bool pRef, out bool pOut);
    public static extern int Method2(int p, in int pIn, ref int pRef, out int pOut);
}}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        private static string DllImportWithType(string typeName) => @$"
using System.Runtime.InteropServices;
unsafe partial class Test
{{
    [DllImport(""DoesNotExist"")]
    public static extern void {{|#0:Method_Parameter|}}({typeName} p);

    [DllImport(""DoesNotExist"")]
    public static extern {typeName} {{|#1:Method_Return|}}();
}}
";
    }
}
