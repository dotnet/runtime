// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.ConvertToGeneratedDllImportFixer;

using VerifyCS = DllImportGenerator.UnitTests.Verifiers.CSharpCodeFixVerifier<
    Microsoft.Interop.Analyzers.ConvertToGeneratedDllImportAnalyzer,
    Microsoft.Interop.Analyzers.ConvertToGeneratedDllImportFixer>;

namespace DllImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class ConvertToGeneratedDllImportFixerTests
    {
        [ConditionalFact]
        public async Task Basic()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"")]
    public static extern int [|Method|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [ConditionalFact]
        public async Task Comments()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    // P/Invoke
    [DllImport(/*name*/""DoesNotExist"")] // comment
    public static extern int [|Method1|](out int ret);

    /** P/Invoke **/
    [DllImport(""DoesNotExist"") /*name*/]
    // < ... >
    public static extern int [|Method2|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    // P/Invoke
    [GeneratedDllImport(/*name*/""DoesNotExist"")] // comment
    public static partial int {{|CS8795:Method1|}}(out int ret);

    /** P/Invoke **/
    [GeneratedDllImport(""DoesNotExist"") /*name*/]
    // < ... >
    public static partial int {{|CS8795:Method2|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [ConditionalFact]
        public async Task MultipleAttributes()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [System.ComponentModel.Description(""Test""), DllImport(""DoesNotExist"")]
    public static extern int [|Method1|](out int ret);

    [System.ComponentModel.Description(""Test"")]
    [DllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static extern int [|Method2|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [System.ComponentModel.Description(""Test""), GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);

    [System.ComponentModel.Description(""Test"")]
    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int {{|CS8795:Method2|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [ConditionalFact]
        public async Task NamedArguments()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static extern int [|Method1|](out int ret);

    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", CharSet = CharSet.Unicode)]
    public static extern string [|Method2|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);

    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"", StringMarshalling = StringMarshalling.Utf16)]
    public static partial string {{|CS8795:Method2|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [ConditionalFact]
        public async Task RemoveableNamedArguments()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", ExactSpelling = true)]
    public static extern int [|Method|](out int ret);

    [DllImport(""DoesNotExist"", BestFitMapping = false, EntryPoint = ""Entry"")]
    public static extern int [|Method1|](out int ret);

    [DllImport(""DoesNotExist"", ThrowOnUnmappableChar = false)]
    public static extern int [|Method2|](out int ret);

    [DllImport(""DoesNotExist"", PreserveSig = true)]
    public static extern int [|Method3|](out int ret);

    [DllImport(""DoesNotExist"", CharSet = CharSet.Unicode)]
    public static extern int [|Method4|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method|}}(out int ret);

    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method2|}}(out int ret);

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method3|}}(out int ret);

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method4|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [ConditionalFact]
        public async Task ReplaceableExplicitPlatformDefaultCallingConvention()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", CallingConvention = CallingConvention.Winapi, EntryPoint = ""Entry"")]
    public static extern int [|Method1|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [ConditionalTheory]
        [InlineData(CallingConvention.Cdecl, typeof(CallConvCdecl))]
        [InlineData(CallingConvention.StdCall, typeof(CallConvStdcall))]
        [InlineData(CallingConvention.ThisCall, typeof(CallConvThiscall))]
        [InlineData(CallingConvention.FastCall, typeof(CallConvFastcall))]
        public async Task ReplaceableCallingConvention(CallingConvention callConv, Type callConvType)
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", CallingConvention = CallingConvention.{callConv}, EntryPoint = ""Entry"")]
    public static extern int [|Method1|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    [UnmanagedCallConv(CallConvs = new System.Type[] {{ typeof({callConvType.FullName}) }})]
    public static partial int {{|CS8795:Method1|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [ConditionalFact]
        public async Task PreferredAttributeOrder()
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", SetLastError = true, EntryPoint = ""Entry"", CharSet = CharSet.Unicode)]
    public static extern string [|Method|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial string {{|CS8795:Method|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }

        [InlineData(CharSet.Ansi, 'A')]
        [InlineData(CharSet.Unicode, 'W')]
        [ConditionalTheory]
        public async Task ExactSpelling_False_NoAutoCharSet_Provides_No_Suffix_And_Suffix_Fix(CharSet charSet, char suffix)
        {
            string source = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", ExactSpelling = false, CharSet = CharSet.{charSet})]
    public static extern void [|Method|]();
}}";
            string fixedSourceNoSuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceNoSuffix, "ConvertToGeneratedDllImport");
            string fixedSourceWithSuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry{suffix}"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceWithSuffix, $"ConvertToGeneratedDllImport{suffix}");
        }

        [ConditionalFact]
        public async Task ExactSpelling_False_AutoCharSet_Provides_No_Suffix_And_Both_Suffix_Fixes()
        {
            string source = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", ExactSpelling = false, CharSet = CharSet.Auto)]
    public static extern void [|Method|]();
}}";
            string fixedSourceNoSuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceNoSuffix, "ConvertToGeneratedDllImport");
            string fixedSourceWithASuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""EntryA"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceWithASuffix, "ConvertToGeneratedDllImportA");
            string fixedSourceWithWSuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""EntryW"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceWithWSuffix, "ConvertToGeneratedDllImportW");
        }

        [ConditionalFact]
        public async Task ExactSpelling_False_ImplicitAnsiCharSet_Provides_No_Suffix_And_Suffix_Fix()
        {
            string source = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", ExactSpelling = false)]
    public static extern void [|Method|]();
}}";
            string fixedSourceNoSuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceNoSuffix, "ConvertToGeneratedDllImport");
            string fixedSourceWithASuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""EntryA"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceWithASuffix, "ConvertToGeneratedDllImportA");
        }

        [ConditionalFact]
        public async Task ExactSpelling_False_ConstantNonLiteralEntryPoint()
        {
            string source = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    private const string EntryPoint = ""Entry"";
    [DllImport(""DoesNotExist"", EntryPoint = EntryPoint, CharSet = CharSet.Ansi, ExactSpelling = false)]
    public static extern void [|Method|]();
}}";
            string fixedSourceWithASuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    private const string EntryPoint = ""Entry"";
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = EntryPoint + ""A"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceWithASuffix, "ConvertToGeneratedDllImportA");
        }

        [ConditionalFact]
        public async Task Implicit_ExactSpelling_False_Offers_Suffix_Fix()
        {
            string source = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", CharSet = CharSet.Ansi)]
    public static extern void [|Method|]();
}}";
            string fixedSourceWithASuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""MethodA"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceWithASuffix, "ConvertToGeneratedDllImportA");
        }

        [ConditionalFact]
        public async Task ExactSpelling_False_NameOfEntryPoint()
        {
            string source = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    private const string Foo = ""Bar"";
    [DllImport(""DoesNotExist"", EntryPoint = nameof(Foo), CharSet = CharSet.Ansi, ExactSpelling = false)]
    public static extern void [|Method|]();
}}";
            string fixedSourceWithASuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    private const string Foo = ""Bar"";
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = nameof(Foo) + ""A"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceWithASuffix, "ConvertToGeneratedDllImportA");
        }

        [ConditionalFact]
        public async Task ExactSpelling_False_ImplicitEntryPointName()
        {
            string source = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", CharSet = CharSet.Ansi, ExactSpelling = false)]
    public static extern void [|Method|]();
}}";
            string fixedSourceWithASuffix = $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""MethodA"")]
    public static partial void {{|CS8795:Method|}}();
}}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSourceWithASuffix, "ConvertToGeneratedDllImportA");
        }

        [ConditionalFact]
        public async Task PreserveSigFalseSignatureModified()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class Test
{
    [DllImport(""DoesNotExist"", PreserveSig = false)]
    public static extern void [|VoidMethod|](bool param);
    [DllImport(""DoesNotExist"", PreserveSig = false)]
    public static extern long [|Method|](bool param);

    public static void Code()
    {
        Test.VoidMethod(true);
        Test.Method(true);
        long value = Test.Method(true);
        value = Test.Method(true);
    }
}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {|CS8795:VoidMethod|}(bool param);
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {|CS8795:Method|}(bool param, out long @return);

    public static void Code()
    {
        Marshal.ThrowExceptionForHR(Test.VoidMethod(true));
        Marshal.ThrowExceptionForHR(Test.Method(true, out _));
        Marshal.ThrowExceptionForHR(Test.Method(true, out long value));
        Marshal.ThrowExceptionForHR(Test.Method(true, out value));
    }
}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }
    }
}
