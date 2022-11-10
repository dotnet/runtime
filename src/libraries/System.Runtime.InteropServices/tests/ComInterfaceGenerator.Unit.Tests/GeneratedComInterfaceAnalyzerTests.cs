// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;

using VerifyCS = LibraryImportGenerator.UnitTests.Verifiers.CSharpAnalyzerVerifier<Microsoft.Interop.GeneratedComInterfaceAttributeAnalyzer>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class GeneratedComInterfaceAnalyzerTests
    {
        static string _usings = $$"""
            #pragma warning disable CS8019
            using System.Runtime.InteropServices.Marshalling;
            using System.Runtime.InteropServices;
            #pragma warning restore CS8019
            """;

        public static async Task<ImmutableArray<Diagnostic>> CompileAndRunAnalyzer(params string[] codeSnippets)
        {
            Compilation comp = await TestUtils.CreateCompilation(codeSnippets);
            // Ensure no compiler errors
            Assert.Empty(comp.GetDiagnostics());
            // Ensure no analyzer erros
            return await TestUtils.RunAnalyzers(comp, new GeneratedComInterfaceAttributeAnalyzer());
        }

        [Fact]
        public async Task BasicWithBothAttributesUsesIUnknown()
        {
            string snippet =
                $$$"""

                [GeneratedComInterface(typeof(MyComWrappers))]
                [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
                interface IFoo
                {
                    void Bar() {}
                }

                public partial class MyComWrappers : GeneratedComWrappersBase<ComObject>
                {
                }

                """;
            await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);

        }

        [Fact]
        public async Task BasicWithGeneratedAttribute()
        {
            string snippet =
                $$$"""

                [GeneratedComInterface(typeof(MyComWrappers))]
                interface IFoo
                {
                    void Bar() {}
                }

                public partial class MyComWrappers : GeneratedComWrappersBase<ComObject>
                {
                }

                """;
            await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
        }

        [Fact]
        public async Task BasicWithNonGeneratedAttributeUsesIUnknown()
        {
            string snippet =
                $$$"""

                [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
                interface IFoo
                {
                    void Bar() {}
                }

                public partial class MyComWrappers : GeneratedComWrappersBase<ComObject>
                {
                }

                """;
            await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
        }

        [Fact]
        public async Task BasicWithNonGeneratedAttributeUsesIDispatch()
        {
            string snippet =
                $$$"""

                [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
                interface IFoo
                {
                    void Bar() {}
                }

                public partial class MyComWrappers : GeneratedComWrappersBase<ComObject>
                {
                }

                """;
            await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
        }

        [Fact]
        public async Task PartialTypeHasBothAttributesWithIUnknown()
        {
            string snippet =
                $$$"""

                [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface(typeof(MyComWrappers))]
                partial interface IFoo
                {
                    void Lorem() {}
                }

                public partial class MyComWrappers : GeneratedComWrappersBase<ComObject>
                {
                }

                """;
            await VerifyCS.VerifyAnalyzerAsync(_usings + snippet);
        }

        [Fact]
        public async Task BasicWithBothAttributesUsesIDispatch()
        {
            string snippet = $$$"""

                [GeneratedComInterface(typeof(MyComWrappers))]
                [{|#0:InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)|}]
                interface IFoo
                {
                    void Bar() {}
                }

                public partial class MyComWrappers : GeneratedComWrappersBase<ComObject>
                {
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                _usings + snippet,
                VerifyCS.Diagnostic(GeneratorDiagnostics.InterfaceTypeNotSupported)
                    .WithLocation(0)
                    .WithArguments("InterfaceTypeAttribute", "InterfaceIsIDispatch"));
        }

        [Fact]
        public async Task PartialTypeHasBothAttributesWithIDispatch()
        {
            string snippet =
                $$$"""

                [{|#0:InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)|}]
                partial interface IFoo
                {
                    void Bar() {}
                }

                [GeneratedComInterface(typeof(MyComWrappers))]
                partial interface IFoo
                {
                    void Lorem() {}
                }

                public partial class MyComWrappers : GeneratedComWrappersBase<ComObject>
                {
                }

                """;
            await VerifyCS.VerifyAnalyzerAsync(
                _usings + snippet,
                VerifyCS.Diagnostic(GeneratorDiagnostics.InterfaceTypeNotSupported)
                    .WithLocation(0)
                    .WithArguments("InterfaceTypeAttribute", "InterfaceIsIDispatch"));
        }
    }
}
