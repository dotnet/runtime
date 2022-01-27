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
    public static extern int [|Method2|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);

    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"", CharSet = CharSet.Unicode)]
    public static partial int {{|CS8795:Method2|}}(out int ret);
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
    [DllImport(""DoesNotExist"", BestFitMapping = false, EntryPoint = ""Entry"")]
    public static extern int [|Method1|](out int ret);

    [DllImport(""DoesNotExist"", ThrowOnUnmappableChar = false)]
    public static extern int [|Method2|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method2|}}(out int ret);
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
    [DllImport(""DoesNotExist"", SetLastError = true, EntryPoint = ""Entry"", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern int [|Method|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static partial int {{|CS8795:Method|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource);
        }
    }
}
