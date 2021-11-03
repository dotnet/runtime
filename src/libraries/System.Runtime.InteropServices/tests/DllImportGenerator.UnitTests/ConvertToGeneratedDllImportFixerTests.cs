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
        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Basic(bool usePreprocessorDefines)
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"")]
    public static extern int [|Method|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = usePreprocessorDefines
                ? @$"
using System.Runtime.InteropServices;
partial class Test
{{
#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method|}}(out int ret);
#else
    [DllImport(""DoesNotExist"")]
    public static extern int Method(out int ret);
#endif
}}"
                : @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource,
                usePreprocessorDefines ? WithPreprocessorDefinesKey : NoPreprocessorDefinesKey);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Comments(bool usePreprocessorDefines)
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
            string fixedSource = usePreprocessorDefines
                ? @$"
using System.Runtime.InteropServices;
partial class Test
{{
    // P/Invoke
#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(/*name*/""DoesNotExist"")] // comment
    public static partial int {{|CS8795:Method1|}}(out int ret);
#else
    [DllImport(/*name*/""DoesNotExist"")] // comment
    public static extern int Method1(out int ret);
#endif

    /** P/Invoke **/
#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(""DoesNotExist"") /*name*/]
    // < ... >
    public static partial int {{|CS8795:Method2|}}(out int ret);
#else
    [DllImport(""DoesNotExist"") /*name*/]
    // < ... >
    public static extern int Method2(out int ret);
#endif
}}"
                : @$"
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
                fixedSource,
                usePreprocessorDefines ? WithPreprocessorDefinesKey : NoPreprocessorDefinesKey);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MultipleAttributes(bool usePreprocessorDefines)
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
            string fixedSource = usePreprocessorDefines
                ? @$"
using System.Runtime.InteropServices;
partial class Test
{{
#if DLLIMPORTGENERATOR_ENABLED
    [System.ComponentModel.Description(""Test""), GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);
#else
    [System.ComponentModel.Description(""Test""), DllImport(""DoesNotExist"")]
    public static extern int Method1(out int ret);
#endif

#if DLLIMPORTGENERATOR_ENABLED
    [System.ComponentModel.Description(""Test"")]
    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static partial int {{|CS8795:Method2|}}(out int ret);
#else
    [System.ComponentModel.Description(""Test"")]
    [DllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.I4)]
    public static extern int Method2(out int ret);
#endif
}}"
                : @$"
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
                fixedSource,
                usePreprocessorDefines ? WithPreprocessorDefinesKey : NoPreprocessorDefinesKey);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task NamedArguments(bool usePreprocessorDefines)
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
            string fixedSource = usePreprocessorDefines
                ? @$"
using System.Runtime.InteropServices;
partial class Test
{{
#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);
#else
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static extern int Method1(out int ret);
#endif

#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"", CharSet = CharSet.Unicode)]
    public static partial int {{|CS8795:Method2|}}(out int ret);
#else
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", CharSet = CharSet.Unicode)]
    public static extern int Method2(out int ret);
#endif
}}" : @$"
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
                fixedSource,
                usePreprocessorDefines ? WithPreprocessorDefinesKey : NoPreprocessorDefinesKey);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RemoveableNamedArguments(bool usePreprocessorDefines)
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
            string fixedSource = usePreprocessorDefines
                ? @$"
using System.Runtime.InteropServices;
partial class Test
{{
#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);
#else
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", BestFitMapping = false)]
    public static extern int Method1(out int ret);
#endif

#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int {{|CS8795:Method2|}}(out int ret);
#else
    [DllImport(""DoesNotExist"", ThrowOnUnmappableChar = false)]
    public static extern int Method2(out int ret);
#endif
}}" : @$"
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
                fixedSource,
                usePreprocessorDefines ? WithPreprocessorDefinesKey : NoPreprocessorDefinesKey);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReplaceableExplicitPlatformDefaultCallingConvention(bool usePreprocessorDefines)
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", CallingConvention = CallingConvention.Winapi, EntryPoint = ""Entry"")]
    public static extern int [|Method1|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = usePreprocessorDefines
                ? @$"
using System.Runtime.InteropServices;
partial class Test
{{
#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);
#else
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", CallingConvention = CallingConvention.Winapi)]
    public static extern int Method1(out int ret);
#endif
}}" : @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    public static partial int {{|CS8795:Method1|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource,
                usePreprocessorDefines ? WithPreprocessorDefinesKey : NoPreprocessorDefinesKey);
        }

        [ConditionalTheory]
        [InlineData(CallingConvention.Cdecl, typeof(CallConvCdecl), true)]
        [InlineData(CallingConvention.Cdecl, typeof(CallConvCdecl), false)]
        [InlineData(CallingConvention.StdCall, typeof(CallConvStdcall), true)]
        [InlineData(CallingConvention.StdCall, typeof(CallConvStdcall), false)]
        [InlineData(CallingConvention.ThisCall, typeof(CallConvThiscall), true)]
        [InlineData(CallingConvention.ThisCall, typeof(CallConvThiscall), false)]
        [InlineData(CallingConvention.FastCall, typeof(CallConvFastcall), true)]
        [InlineData(CallingConvention.FastCall, typeof(CallConvFastcall), false)]
        public async Task ReplaceableCallingConvention(CallingConvention callConv, Type callConvType, bool usePreprocessorDefines)
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", CallingConvention = CallingConvention.{callConv}, EntryPoint = ""Entry"")]
    public static extern int [|Method1|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = usePreprocessorDefines
                ? @$"
using System.Runtime.InteropServices;
partial class Test
{{
#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    [UnmanagedCallConv(CallConvs = new System.Type[] {{ typeof({callConvType.FullName}) }})]
    public static partial int {{|CS8795:Method1|}}(out int ret);
#else
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", CallingConvention = CallingConvention.{callConv})]
    public static extern int Method1(out int ret);
#endif
}}" : @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"")]
    [UnmanagedCallConv(CallConvs = new System.Type[] {{ typeof({callConvType.FullName}) }})]
    public static partial int {{|CS8795:Method1|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource,
                usePreprocessorDefines ? WithPreprocessorDefinesKey : NoPreprocessorDefinesKey);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task PreferredAttributeOrder(bool usePreprocessorDefines)
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [DllImport(""DoesNotExist"", SetLastError = true, EntryPoint = ""Entry"", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern int [|Method|](out int ret);
}}";
            // Fixed source will have CS8795 (Partial method must have an implementation) without generator run
            string fixedSource = usePreprocessorDefines
                ? @$"
using System.Runtime.InteropServices;
partial class Test
{{
#if DLLIMPORTGENERATOR_ENABLED
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static partial int {{|CS8795:Method|}}(out int ret);
#else
    [DllImport(""DoesNotExist"", EntryPoint = ""Entry"", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern int Method(out int ret);
#endif
}}" : @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint = ""Entry"", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static partial int {{|CS8795:Method|}}(out int ret);
}}";
            await VerifyCS.VerifyCodeFixAsync(
                source,
                fixedSource,
                usePreprocessorDefines ? WithPreprocessorDefinesKey : NoPreprocessorDefinesKey);
        }
    }
}
