// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpCodeFixVerifier<
       Microsoft.Interop.Analyzers.ConvertComImportToGeneratedComInterfaceAnalyzer,
          Microsoft.Interop.Analyzers.ConvertComImportToGeneratedComInterfaceFixer>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class ConvertToGeneratedComInterfaceTests
    {
        [Fact]
        public async Task Empty_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices;

                [ComImport]
                [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
                [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                public interface [|I|]
                {
                }
                """;

            string fixedSource = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public partial interface I
               {
               }
               """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task PrimitiveArgument_ReportsDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface [|I|]
               {
                    void Foo(int a);
               }
               """;

            string fixedSource = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public partial interface I
               {
                    void Foo(int a);
               }
               """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task Bool_MarshalsAsVariantBool()
        {
            string source = """
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface [|I|]
               {
                    bool Foo(bool a);
               }
               """;

            string fixedSource = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public partial interface I
               {
                   [return: MarshalAs(UnmanagedType.VariantBool)]
                   bool Foo([MarshalAs(UnmanagedType.VariantBool)] bool a);
               }
               """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task String_AddsStringMarshallingBStr()
        {
            string source = """
                using System.Runtime.InteropServices;

                [ComImport]
                [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
                [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                public interface [|I|]
                {
                    string Foo(string a);
                }
                """;

            string fixedSource = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface(StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(BStrStringMarshaller))]
                [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
                [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                public partial interface I
                {
                    string Foo(string a);
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task Array_DoesNotReportDiagnostic()
        {
            // The default behavior in ComImport for arrays is to marshal as a SAFEARRAY. We don't support SAFEARRAY's, so we don't want to offer a fix here.
            string source = """
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface I
               {
                   void Foo(int[] a);
               }
               """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task Delegate_DoesNotReportDiagnostic()
        {
            // The default behavior in ComImport for delegates is to marshal as a COM object with an undefined interface. We don't support that interface, so we don't offer a fix here.
            string source = """
               using System;
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface I
               {
                   void Foo(Action a);
               }
               """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task Object_DoesNotReportDiagnostic()
        {
            // The default behavior in ComImport for Object is to marshal as a VARIANT. We don't support VARIANTs, so we don't offer a fix here.
            string source = """
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface I
               {
                   object Foo(object o);
               }
               """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task SystemArray_DoesNotReportDiagnostic()
        {
            // The default behavior in ComImport for System.Array is to marshal as a COM interface. We don't support that interface, so we don't offer a fix here.
            string source = """
               using System;
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface I
               {
                   Array Foo(Array o);
               }
               """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task Delegate_WithMarshalAs_ReportsDiagnostic()
        {
            string source = """
               using System;
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface [|I|]
               {
                   void Foo([MarshalAs(UnmanagedType.FunctionPtr)] Action a);
               }
               """;

            string fixedSource = """
                using System;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
                [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                public partial interface I
                {
                    void Foo([MarshalAs(UnmanagedType.FunctionPtr)] Action a);
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task Array_WithMarshalAs_ReportsDiagnostic()
        {
            string source = """
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface [|I|]
               {
                   void Foo([MarshalAs(UnmanagedType.LPArray, SizeConst = 10)] int[] a);
               }
               """;

            string fixedSource = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
                [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                public partial interface I
                {
                    void Foo([MarshalAs(UnmanagedType.LPArray, SizeConst = 10)] int[] a);
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task InterfaceInheritance_RemovesShadowingMembers()
        {
            string source = """
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface [|I|]
               {
                   void Foo(int a);
               }

               [ComImport]
               [Guid("F59AB2FE-523D-4B28-911C-21363808C51E")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface [|J|] : I
               {
                   new void Foo(int a);

                   void Bar(short a);
               }
               """;

            string fixedSource = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
                [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                public partial interface I
                {
                    void Foo(int a);
                }

                [GeneratedComInterface]
                [Guid("F59AB2FE-523D-4B28-911C-21363808C51E")]
                [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                public partial interface J : I
                {
                    void Bar(short a);
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task HResultLikeType_MarshalsAsError()
        {
            string source = """
               using System.Runtime.InteropServices;

               [ComImport]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public interface [|I|]
               {
                   [PreserveSig]
                   HResult Foo();
               }

               [StructLayout(LayoutKind.Sequential)]
               public struct HResult
               {
                  public int Value;
               }
               """;

            string fixedSource = """
               using System.Runtime.InteropServices;
               using System.Runtime.InteropServices.Marshalling;

               [GeneratedComInterface]
               [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
               [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
               public partial interface I
               {
                   [PreserveSig]
                   [return: MarshalAs(UnmanagedType.Error)]
                   HResult Foo();
               }
               
               [StructLayout(LayoutKind.Sequential)]
               public struct HResult
               {
                  public int Value;
               }
               """;

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task UnsupportedInterfaceTypes_DoesNotReportDiagnostic()
        {
            // This also tests the case where InterfaceType is missing (defaulting to ComInterfaceType.InterfaceIsDual).
            string source = """
                 using System.Runtime.InteropServices;

                 [ComImport]
                 [Guid("73EB4AF8-BE9C-4b49-B3A4-24F4FF657B26")]
                 public interface IInterfaceIsDualMissingAttribute
                 {
                 }

                 [ComImport]
                 [Guid("5DA39CDF-DCAD-447A-836E-EA80DB34D81B")]
                 [InterfaceType(ComInterfaceType.InterfaceIsDual)]
                 public interface IInterfaceIsDual
                 {
                 }

                 [ComImport]
                 [Guid("F59AB2FE-523D-4B28-911C-21363808C51E")]
                 [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
                 public interface IInterfaceIsIDispatch
                 {
                 }

                 [ComImport]
                 [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
                 [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
                 public interface IInterfaceIsIInspectable
                 {
                 }
                 """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }
    }
}
