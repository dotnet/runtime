// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Xunit;

using static Microsoft.Interop.Analyzers.ConvertToGeneratedDllImportAnalyzer;

using VerifyCS = DllImportGenerator.UnitTests.Verifiers.CSharpAnalyzerVerifier<Microsoft.Interop.Analyzers.ConvertToGeneratedDllImportAnalyzer>;

namespace DllImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
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

        public static IEnumerable<object[]> UnsupportedTypes() => new[]
        {
            new object[] { typeof(System.Runtime.InteropServices.CriticalHandle) },
            new object[] { typeof(System.Runtime.InteropServices.HandleRef) },
            new object[] { typeof(System.Text.StringBuilder) },
        };

        [ConditionalTheory]
        [MemberData(nameof(MarshallingRequiredTypes))]
        [MemberData(nameof(NoMarshallingRequiredTypes))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60909", typeof(PlatformDetection), nameof(PlatformDetection.IsArm64Process), nameof(PlatformDetection.IsWindows))]
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

        [ConditionalTheory]
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

        [ConditionalFact]
        public async Task PreserveSigFalse_ReportsDiagnostic()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", PreserveSig = false)]
    public static extern void {{|#0:Method1|}}();

    [DllImport(""DoesNotExist"", PreserveSig = true)]
    public static extern void {{|#1:Method2|}}();
}}
";
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(0)
                    .WithArguments("Method1"),
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(1)
                    .WithArguments("Method2"));
        }

        [ConditionalFact]
        public async Task SetLastErrorTrue_ReportsDiagnostic()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", SetLastError = false)]
    public static extern void {{|#0:Method1|}}();

    [DllImport(""DoesNotExist"", SetLastError = true)]
    public static extern void {{|#1:Method2|}}();
}}
";
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(0)
                    .WithArguments("Method1"),
                VerifyCS.Diagnostic(ConvertToGeneratedDllImport)
                    .WithLocation(1)
                    .WithArguments("Method2"));
        }

        [ConditionalTheory]
        [MemberData(nameof(UnsupportedTypes))]
        public async Task UnsupportedType_NoDiagnostic(Type type)
        {
            string source = DllImportWithType(type.FullName!);
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [ConditionalTheory]
        [InlineData(UnmanagedType.Interface)]
        [InlineData(UnmanagedType.IDispatch)]
        [InlineData(UnmanagedType.IInspectable)]
        [InlineData(UnmanagedType.IUnknown)]
        [InlineData(UnmanagedType.SafeArray)]
        public async Task UnsupportedUnmanagedType_NoDiagnostic(UnmanagedType unmanagedType)
        {
            string source = $@"
using System.Runtime.InteropServices;
unsafe partial class Test
{{
    [DllImport(""DoesNotExist"")]
    public static extern void Method_Parameter([MarshalAs(UnmanagedType.{unmanagedType}, MarshalType = ""DNE"")]int p);

    [DllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.{unmanagedType}, MarshalType = ""DNE"")]
    public static extern int Method_Return();
}}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [ConditionalFact]
        public async Task GeneratedDllImport_NoDiagnostic()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method();
}}
partial class Test
{{
    [DllImport(""DoesNotExist"")]
    public static extern partial void Method();
}}
";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [ConditionalFact]
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
