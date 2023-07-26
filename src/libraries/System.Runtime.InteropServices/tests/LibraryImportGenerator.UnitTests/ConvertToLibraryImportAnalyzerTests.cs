// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Xunit;

using static Microsoft.Interop.Analyzers.ConvertToLibraryImportAnalyzer;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpAnalyzerVerifier<Microsoft.Interop.Analyzers.ConvertToLibraryImportAnalyzer>;

namespace LibraryImportGenerator.UnitTests
{
    public class ConvertToLibraryImportAnalyzerTests
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
            new object[] { typeof(IntPtr) },
            new object[] { typeof(ConsoleKey) }, // enum
        };

        public static IEnumerable<object[]> UnsupportedTypes() => new[]
        {
            new object[] { typeof(System.Runtime.InteropServices.CriticalHandle) },
            new object[] { typeof(System.Runtime.InteropServices.HandleRef) },
            new object[] { typeof(System.Text.StringBuilder) },
        };

        [Theory]
        [MemberData(nameof(MarshallingRequiredTypes))]
        [MemberData(nameof(NoMarshallingRequiredTypes))]
        public async Task TypeRequiresMarshalling_ReportsDiagnostic(Type type)
        {
            string source = DllImportWithType(type.FullName!);
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToLibraryImport)
                    .WithLocation(0)
                    .WithArguments("Method_Parameter"),
                VerifyCS.Diagnostic(ConvertToLibraryImport)
                    .WithLocation(1)
                    .WithArguments("Method_Return"));
        }

        [Fact]
        public async Task FunctionPointer_ReportsDiagnostic()
        {
            string source = DllImportWithType("delegate* unmanaged<void>");
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToLibraryImport)
                    .WithLocation(0)
                    .WithArguments("Method_Parameter"),
                VerifyCS.Diagnostic(ConvertToLibraryImport)
                    .WithLocation(1)
                    .WithArguments("Method_Return"));
        }

        [Theory]
        [MemberData(nameof(MarshallingRequiredTypes))]
        [MemberData(nameof(NoMarshallingRequiredTypes))]
        public async Task ByRef_ReportsDiagnostic(Type type)
        {
            string typeName = type.FullName!;
            string source = $$"""
                using System.Runtime.InteropServices;
                unsafe partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern void {|#0:Method_In|}(in {{typeName}} p);

                    [DllImport("DoesNotExist")]
                    public static extern void {|#1:Method_Out|}(out {{typeName}} p);

                    [DllImport("DoesNotExist")]
                    public static extern void {|#2:Method_Ref|}(ref {{typeName}} p);
                }
                """;
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToLibraryImport)
                    .WithLocation(0)
                    .WithArguments("Method_In"),
                VerifyCS.Diagnostic(ConvertToLibraryImport)
                    .WithLocation(1)
                    .WithArguments("Method_Out"),
                VerifyCS.Diagnostic(ConvertToLibraryImport)
                    .WithLocation(2)
                    .WithArguments("Method_Ref"));
        }

        [Fact]
        public async Task SetLastErrorTrue_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", SetLastError = false)]
                    public static extern void {|#0:Method1|}();

                    [DllImport("DoesNotExist", SetLastError = true)]
                    public static extern void {|#1:Method2|}();
                }

                """;
            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ConvertToLibraryImport)
                    .WithLocation(0)
                    .WithArguments("Method1"),
                VerifyCS.Diagnostic(ConvertToLibraryImport)
                    .WithLocation(1)
                    .WithArguments("Method2"));
        }

        [Theory]
        [MemberData(nameof(UnsupportedTypes))]
        public async Task UnsupportedType_NoDiagnostic(Type type)
        {
            string source = DllImportWithType(type.FullName!);
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Theory]
        [InlineData(UnmanagedType.IDispatch)]
        [InlineData(UnmanagedType.IInspectable)]
        [InlineData(UnmanagedType.IUnknown)]
        [InlineData(UnmanagedType.SafeArray)]
        public async Task UnsupportedUnmanagedType_NoDiagnostic(UnmanagedType unmanagedType)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                unsafe partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern void Method_Parameter([MarshalAs(UnmanagedType.{{unmanagedType}}, MarshalType = "DNE")]int p);

                    [DllImport("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.{{unmanagedType}}, MarshalType = "DNE")]
                    public static extern int Method_Return();
                }
                """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task UnmanagedTypeInterfaceWithComImportType_NoDiagnostic()
        {
            string source = $$"""
                using System.Runtime.InteropServices;

                [ComImport]
                [Guid("8509bcd0-45bc-4b04-bb45-f3cac0b4cabd")]
                interface IFoo
                {
                    void Bar();
                }

                unsafe partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern void Method_Parameter([MarshalAs(UnmanagedType.Interface)]IFoo p);

                    [DllImport("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.Interface, MarshalType = "DNE")]
                    public static extern IFoo Method_Return();
                }
                """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task LibraryImport_NoDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void Method();
                }
                partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern partial void Method();
                }
                """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NotDllImport_NoDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    public static extern bool Method1(bool p, in bool pIn, ref bool pRef, out bool pOut);
                    public static extern int Method2(int p, in int pIn, ref int pRef, out int pOut);
                }
                """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        private static string DllImportWithType(string typeName) => $$"""
            using System.Runtime.InteropServices;
            unsafe partial class Test
            {
                [DllImport("DoesNotExist")]
                public static extern void {|#0:Method_Parameter|}({{typeName}} p);

                [DllImport("DoesNotExist")]
                public static extern {{typeName}} {|#1:Method_Return|}();
            }
            """;

        [Fact]
        public async Task BestFitMapping_True_NoDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", BestFitMapping = true)]
                    public static extern void Method2();
                }

               """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task ThrowOnUnmappableChar_True_NoDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;
               partial class Test
               {
                   [DllImport("DoesNotExist", ThrowOnUnmappableChar = true)]
                   public static extern void Method2();
               }

               """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task BestFitMapping_Assembly_True_NoDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;
               [assembly:BestFitMapping(true)]
               partial class Test
               {
                   [DllImport("DoesNotExist")]
                   public static extern void Method2();
               }

               """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task BestFitMapping_Type_True_NoDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;
               [BestFitMapping(true)]
               partial class Test
               {
                   [DllImport("DoesNotExist")]
                   public static extern void Method2();
               }

               """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task BestFitMapping_Assembly_False_Diagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;
               [assembly:BestFitMapping(false)]
               partial class Test
               {
                   [DllImport("DoesNotExist")]
                   public static extern void [|Method2|]();
               }

               """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task BestFitMapping_Type_False_Diagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;
               [BestFitMapping(false)]
               partial class Test
               {
                   [DllImport("DoesNotExist")]
                   public static extern void [|Method2|]();
               }

               """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task Varargs_NoDiagnostic()
        {
            string source = """
              using System.Runtime.InteropServices;
              partial class Test
              {
                  [DllImport("DoesNotExist")]
                  public static extern void Method2(__arglist);
              }

              """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    }
}
