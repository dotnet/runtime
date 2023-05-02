// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Interop.Analyzers;
using Xunit;
using static Microsoft.Interop.Analyzers.ConvertToLibraryImportFixer;

using VerifyCS = LibraryImportGenerator.UnitTests.Verifiers.CSharpCodeFixVerifier<
    Microsoft.Interop.Analyzers.ConvertToLibraryImportAnalyzer,
    Microsoft.Interop.Analyzers.ConvertToLibraryImportFixer>;

namespace LibraryImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class ConvertToLibraryImportFixerTests
    {
        private const string ConvertToLibraryImportKey = "ConvertToLibraryImport,";

        [Fact]
        public async Task Basic()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern int [|Method|](out int ret);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int {|CS8795:Method|}(out int ret);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [Fact]
        public async Task Comments()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    // P/Invoke
                    [DllImport(/*name*/"DoesNotExist")] // comment
                    public static extern int [|Method1|](out int ret);

                    /** P/Invoke **/
                    [DllImport("DoesNotExist") /*name*/]
                    // < ... >
                    public static extern int [|Method2|](out int ret);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    // P/Invoke
                    [LibraryImport(/*name*/"DoesNotExist")] // comment
                    public static partial int {|CS8795:Method1|}(out int ret);

                    /** P/Invoke **/
                    [LibraryImport("DoesNotExist") /*name*/]
                    // < ... >
                    public static partial int {|CS8795:Method2|}(out int ret);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [Fact]
        public async Task MultipleAttributes()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [System.ComponentModel.Description("Test"), DllImport("DoesNotExist")]
                    public static extern int [|Method1|](out int ret);

                    [System.ComponentModel.Description("Test")]
                    [DllImport("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.I4)]
                    public static extern int [|Method2|](out int ret);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [System.ComponentModel.Description("Test"), LibraryImport("DoesNotExist")]
                    public static partial int {|CS8795:Method1|}(out int ret);

                    [System.ComponentModel.Description("Test")]
                    [LibraryImport("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.I4)]
                    public static partial int {|CS8795:Method2|}(out int ret);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [Fact]
        public async Task NamedArguments()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", EntryPoint = "Entry")]
                    public static extern int [|Method1|](out int ret);

                    [DllImport("DoesNotExist", EntryPoint = "Entry", CharSet = CharSet.Unicode)]
                    public static extern string [|Method2|](out int ret);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "Entry")]
                    public static partial int {|CS8795:Method1|}(out int ret);

                    [LibraryImport("DoesNotExist", EntryPoint = "Entry", StringMarshalling = StringMarshalling.Utf16)]
                    public static partial string {|CS8795:Method2|}(out int ret);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [Fact]
        public async Task RemoveableNamedArguments()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", EntryPoint = "Entry", ExactSpelling = true)]
                    public static extern int [|Method|](out int ret);

                    [DllImport("DoesNotExist", BestFitMapping = false, EntryPoint = "Entry")]
                    public static extern int [|Method1|](out int ret);

                    [DllImport("DoesNotExist", ThrowOnUnmappableChar = false)]
                    public static extern int [|Method2|](out int ret);

                    [DllImport("DoesNotExist", PreserveSig = true)]
                    public static extern int [|Method3|](out int ret);

                    [DllImport("DoesNotExist", CharSet = CharSet.Unicode)]
                    public static extern int [|Method4|](out int ret);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "Entry")]
                    public static partial int {|CS8795:Method|}(out int ret);

                    [LibraryImport("DoesNotExist", EntryPoint = "Entry")]
                    public static partial int {|CS8795:Method1|}(out int ret);

                    [LibraryImport("DoesNotExist")]
                    public static partial int {|CS8795:Method2|}(out int ret);

                    [LibraryImport("DoesNotExist")]
                    public static partial int {|CS8795:Method3|}(out int ret);

                    [LibraryImport("DoesNotExist")]
                    public static partial int {|CS8795:Method4|}(out int ret);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [Fact]
        public async Task ReplaceableExplicitPlatformDefaultCallingConvention()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", CallingConvention = CallingConvention.Winapi, EntryPoint = "Entry")]
                    public static extern int [|Method1|](out int ret);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "Entry")]
                    public static partial int {|CS8795:Method1|}(out int ret);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [Theory]
        [InlineData(CallingConvention.Cdecl, typeof(CallConvCdecl))]
        [InlineData(CallingConvention.StdCall, typeof(CallConvStdcall))]
        [InlineData(CallingConvention.ThisCall, typeof(CallConvThiscall))]
        [InlineData(CallingConvention.FastCall, typeof(CallConvFastcall))]
        public async Task ReplaceableCallingConvention(CallingConvention callConv, Type callConvType)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", CallingConvention = CallingConvention.{{callConv}}, EntryPoint = "Entry")]
                    public static extern int [|Method1|](out int ret);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = $$"""
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "Entry")]
                    [UnmanagedCallConv(CallConvs = new System.Type[] { typeof({{callConvType.FullName}}) })]
                    public static partial int {|CS8795:Method1|}(out int ret);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [Fact]
        public async Task PreferredAttributeOrder()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", SetLastError = true, EntryPoint = "Entry", CharSet = CharSet.Unicode)]
                    public static extern string [|Method|](out int ret);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "Entry", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
                    public static partial string {|CS8795:Method|}(out int ret);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [InlineData(CharSet.Ansi, 'A')]
        [InlineData(CharSet.Unicode, 'W')]
        [Theory]
        public async Task ExactSpelling_False_NoAutoCharSet_Provides_No_Suffix_And_Suffix_Fix(CharSet charSet, char suffix)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", EntryPoint = "Entry", ExactSpelling = false, CharSet = CharSet.{{charSet}})]
                    public static extern void [|Method|]();
                }
                """;
            string fixedSourceNoSuffix = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "Entry")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceNoSuffix, ConvertToLibraryImportKey);
            string fixedSourceWithSuffix = $$"""
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "Entry{{suffix}}")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceWithSuffix, $"{ConvertToLibraryImportKey}{suffix},");
        }

        [Fact]
        public async Task ExactSpelling_False_AutoCharSet_Provides_No_Suffix_And_Both_Suffix_Fixes()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", EntryPoint = "Entry", ExactSpelling = false, CharSet = CharSet.Auto)]
                    public static extern void [|Method|]();
                }
                """;
            string fixedSourceNoSuffix = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "Entry")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceNoSuffix, ConvertToLibraryImportKey);
            string fixedSourceWithASuffix = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "EntryA")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceWithASuffix, $"{ConvertToLibraryImportKey}A,");
            string fixedSourceWithWSuffix = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "EntryW")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceWithWSuffix, $"{ConvertToLibraryImportKey}W,");
        }

        [Fact]
        public async Task ExactSpelling_False_ImplicitAnsiCharSet_Provides_No_Suffix_And_Suffix_Fix()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", EntryPoint = "Entry", ExactSpelling = false)]
                    public static extern void [|Method|]();
                }
                """;
            string fixedSourceNoSuffix = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "Entry")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceNoSuffix, ConvertToLibraryImportKey);
            string fixedSourceWithASuffix = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "EntryA")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceWithASuffix, $"{ConvertToLibraryImportKey}A,");
        }

        [Fact]
        public async Task ExactSpelling_False_ConstantNonLiteralEntryPoint()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    private const string EntryPoint = "Entry";
                    [DllImport("DoesNotExist", EntryPoint = EntryPoint, CharSet = CharSet.Ansi, ExactSpelling = false)]
                    public static extern void [|Method|]();
                }
                """;
            string fixedSourceWithASuffix = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    private const string EntryPoint = "Entry";
                    [LibraryImport("DoesNotExist", EntryPoint = EntryPoint + "A")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceWithASuffix, $"{ConvertToLibraryImportKey}A,");
        }

        [Fact]
        public async Task Implicit_ExactSpelling_False_Offers_Suffix_Fix()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", CharSet = CharSet.Ansi)]
                    public static extern void [|Method|]();
                }
                """;
            string fixedSourceWithASuffix = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "MethodA")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceWithASuffix, $"{ConvertToLibraryImportKey}A,");
        }

        [Fact]
        public async Task ExactSpelling_False_NameOfEntryPoint()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    private const string Foo = "Bar";
                    [DllImport("DoesNotExist", EntryPoint = nameof(Foo), CharSet = CharSet.Ansi, ExactSpelling = false)]
                    public static extern void [|Method|]();
                }
                """;
            string fixedSourceWithASuffix = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    private const string Foo = "Bar";
                    [LibraryImport("DoesNotExist", EntryPoint = nameof(Foo) + "A")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceWithASuffix, $"{ConvertToLibraryImportKey}A,");
        }

        [Fact]
        public async Task ExactSpelling_False_ImplicitEntryPointName()
        {
            string source = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", CharSet = CharSet.Ansi, ExactSpelling = false)]
                    public static extern void [|Method|]();
                }
                """;
            string fixedSourceWithASuffix = """

                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist", EntryPoint = "MethodA")]
                    public static partial void {|CS8795:Method|}();
                }
                """;
            await VerifyCodeFixAsync(source, fixedSourceWithASuffix, $"{ConvertToLibraryImportKey}A,");
        }

        [Fact]
        public async Task PreserveSigFalseSignatureModified()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist", PreserveSig = false)]
                    public static extern void [|VoidMethod|](int param);
                    [DllImport("DoesNotExist", PreserveSig = false)]
                    public static extern long [|Method|](int param);

                    public static void Code()
                    {
                        Test.VoidMethod(1);
                        Test.Method(1);
                        long value = Test.Method(1);
                        value = Test.Method(1);
                    }
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int {|CS8795:VoidMethod|}(int param);
                    [LibraryImport("DoesNotExist")]
                    public static partial int {|CS8795:Method|}(int param, out long @return);

                    public static void Code()
                    {
                        Marshal.ThrowExceptionForHR(Test.VoidMethod(1));
                        Marshal.ThrowExceptionForHR(Test.Method(1, out _));
                        Marshal.ThrowExceptionForHR(Test.Method(1, out long value));
                        Marshal.ThrowExceptionForHR(Test.Method(1, out value));
                    }
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [Fact]
        public async Task MakeEnclosingTypesPartial()
        {
            string source = """
                using System.Runtime.InteropServices;

                class Enclosing
                {
                    class Test
                    {
                        [DllImport("DoesNotExist")]
                        public static extern int [|Method|](out int ret);
                        [DllImport("DoesNotExist")]
                        public static extern int [|Method2|](out int ret);
                        [DllImport("DoesNotExist")]
                        public static extern int [|Method3|](out int ret);
                    }
                }
                partial class EnclosingPartial
                {
                    class Test
                    {
                        [DllImport("DoesNotExist")]
                        public static extern int [|Method|](out int ret);
                        [DllImport("DoesNotExist")]
                        public static extern int [|Method2|](out int ret);
                        [DllImport("DoesNotExist")]
                        public static extern int [|Method3|](out int ret);
                    }
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;

                partial class Enclosing
                {
                    partial class Test
                    {
                        [LibraryImport("DoesNotExist")]
                        public static partial int {|CS8795:Method|}(out int ret);
                        [LibraryImport("DoesNotExist")]
                        public static partial int {|CS8795:Method2|}(out int ret);
                        [LibraryImport("DoesNotExist")]
                        public static partial int {|CS8795:Method3|}(out int ret);
                    }
                }
                partial class EnclosingPartial
                {
                    partial class Test
                    {
                        [LibraryImport("DoesNotExist")]
                        public static partial int {|CS8795:Method|}(out int ret);
                        [LibraryImport("DoesNotExist")]
                        public static partial int {|CS8795:Method2|}(out int ret);
                        [LibraryImport("DoesNotExist")]
                        public static partial int {|CS8795:Method3|}(out int ret);
                    }
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [Fact]
        public async Task BooleanMarshalAsAdded()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern bool [|Method|](bool b);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.Bool)]
                    public static partial bool {|CS8795:Method|}([MarshalAs(UnmanagedType.Bool)] bool b);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        // There's not a good way today to add a unit test for any changes to project settings from a code fix.
        // Roslyn does special testing for their cases.
        [Fact]
        public async Task FixThatAddsUnsafeToProjectUpdatesLibraryImport()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern int [|Method|](out int ret);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int {|CS8795:Method|}(out int ret);
                }
                """;
            await VerifyCodeFixNoUnsafeAsync(
                source,
                fixedSource,
                $"{ConvertToLibraryImportKey}AddUnsafe,");
        }

        [Fact]
        public async Task UserBlittableStructConvertsWithNoWarningCodeAction()
        {
            string source = """
                using System.Runtime.InteropServices;

                struct Blittable
                {
                    private short s;
                    private short t;
                }

                partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](Blittable b);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;

                struct Blittable
                {
                    private short s;
                    private short t;
                }

                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|CS8795:Method|}(Blittable b);
                }
                """;
            await VerifyCodeFixAsync(
                source,
                fixedSource,
                ConvertToLibraryImportKey);
        }

        [Fact]
        public async Task UserNonBlittableStructConvertsOnlyWithWarningCodeAction()
        {
            string source = """
                using System.Runtime.InteropServices;

                struct NonBlittable
                {
                    private string s;
                    private short t;
                }

                partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](NonBlittable b);
                }
                """;
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = """
                using System.Runtime.InteropServices;

                struct NonBlittable
                {
                    private string s;
                    private short t;
                }

                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|CS8795:Method|}(NonBlittable b);
                }
                """;
            // Verify that we don't update this signature with the "no additional work required" action.
            await VerifyCodeFixAsync(
                source,
                source,
                ConvertToLibraryImportKey);

            await VerifyCodeFixAsync(
                source,
                fixedSource,
                $"{ConvertToLibraryImportKey}{ConvertToLibraryImportAnalyzer.MayRequireAdditionalWork},");
        }

        [Fact]
        public async Task UserBlittableStructFixAllConvertsOnlyNoWarningLocations()
        {
            string source = """
                using System.Runtime.InteropServices;

                struct Blittable
                {
                    private short s;
                    private short t;
                }

                struct NonBlittable
                {
                    private string s;
                    private short t;
                }

                partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](int i);
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](Blittable b);
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](NonBlittable b);
                }
                """;
            // Fixed sources will have CS8795 (Partial method must have an implementation) without generator run
            string blittableOnlyFixedSource = """
                using System.Runtime.InteropServices;

                struct Blittable
                {
                    private short s;
                    private short t;
                }

                struct NonBlittable
                {
                    private string s;
                    private short t;
                }

                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|CS8795:Method|}(int i);
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|CS8795:Method|}(Blittable b);
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](NonBlittable b);
                }
                """;
            // Verify that we only fix the blittable cases, not the non-blittable cases.
            await VerifyCodeFixAsync(
                source,
                blittableOnlyFixedSource,
                ConvertToLibraryImportKey);
        }

        [Fact]
        public async Task UserNonBlittableStructFixAllConvertsAllLocations()
        {
            string source = """
                using System.Runtime.InteropServices;

                struct Blittable
                {
                    private short s;
                    private short t;
                }

                struct NonBlittable
                {
                    private string s;
                    private short t;
                }

                partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](int i);
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](Blittable b);
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](NonBlittable b);
                }
                """;
            // Fixed sources will have CS8795 (Partial method must have an implementation) without generator run
            string nonBlittableOnlyFixedSource = """
                using System.Runtime.InteropServices;

                struct Blittable
                {
                    private short s;
                    private short t;
                }

                struct NonBlittable
                {
                    private string s;
                    private short t;
                }

                partial class Test
                {
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](int i);
                    [DllImport("DoesNotExist")]
                    public static extern void [|Method|](Blittable b);
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|CS8795:Method|}(NonBlittable b);
                }
                """;
            // Fixed sources will have CS8795 (Partial method must have an implementation) without generator run
            string allFixedSource = """
                using System.Runtime.InteropServices;

                struct Blittable
                {
                    private short s;
                    private short t;
                }

                struct NonBlittable
                {
                    private string s;
                    private short t;
                }

                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|CS8795:Method|}(int i);
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|CS8795:Method|}(Blittable b);
                    [LibraryImport("DoesNotExist")]
                    public static partial void {|CS8795:Method|}(NonBlittable b);
                }
                """;

            // Verify that we fix all cases when we do fix-all on a P/Invoke with a non-blittable user type which
            // will require the user to write additional code.
            await VerifyCodeFixAsync(
                source,
                nonBlittableOnlyFixedSource,
                allFixedSource,
                $"{ConvertToLibraryImportKey}{ConvertToLibraryImportAnalyzer.MayRequireAdditionalWork},");
        }

        private static async Task VerifyCodeFixAsync(string source, string fixedSource)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            await test.RunAsync();
        }

        private static async Task VerifyCodeFixAsync(string source, string fixedSource, string equivalenceKey)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionEquivalenceKey = equivalenceKey,
            };

            test.FixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;

            await test.RunAsync();
        }

        private static async Task VerifyCodeFixAsync(string source, string fixedSource, string batchFixedSource, string equivalenceKey)
        {
            var test = new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                BatchFixedCode = batchFixedSource,
                CodeActionEquivalenceKey = equivalenceKey,
            };

            test.FixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;

            await test.RunAsync();
        }

        private static async Task VerifyCodeFixNoUnsafeAsync(string source, string fixedSource, string equivalenceKey)
        {
            var test = new TestNoUnsafe
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionEquivalenceKey = equivalenceKey,
            };

            await test.RunAsync();
        }

        class TestNoUnsafe : VerifyCS.Test
        {
            protected override CompilationOptions CreateCompilationOptions() => ((CSharpCompilationOptions)base.CreateCompilationOptions()).WithAllowUnsafe(false);
        }
    }
}
