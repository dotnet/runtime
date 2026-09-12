// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Xunit;

namespace ComInterfaceGenerator.Unit.Tests
{
    /// <summary>
    /// Verifies that the generated output compiles under the updated memory safety rules ("unsafe evolution"),
    /// where an <c>unsafe</c> modifier on a type establishes no context for the members inside it.
    /// </summary>
    public class UnsafeCodeGeneration
    {
        [Fact]
        public async Task ComInterfaceOutputCompilesUnderUpdatedRules()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                partial interface INativeAPI
                {
                    void Method();
                    int MethodWithArgs(int a, string s);
                    int Property { get; set; }
                }
                """;

            await new UpdatedRulesTest<Microsoft.Interop.ComInterfaceGenerator>
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task InheritedPointerSignatureCompilesUnderUpdatedRules()
        {
            // The base method carries 'unsafe' on the member rather than on the type, and the derived interface
            // declares none at all, so the shadowing method the generator emits into the user's own partial part
            // is the one shape that cannot borrow an unsafe context from anything the user wrote.
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [assembly:DisableRuntimeMarshalling]

                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                partial interface IComInterfaceBase
                {
                    unsafe void Method(void* pBuffer);
                }

                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541E")]
                partial interface IComInterfaceDerived : IComInterfaceBase
                {
                    void Method2();
                }
                """;

            await new UpdatedRulesTest<Microsoft.Interop.ComInterfaceGenerator>
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task InheritedPointerSignatureCompilesUnderLegacyRules()
        {
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [assembly:DisableRuntimeMarshalling]

                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                partial interface IComInterfaceBase
                {
                    unsafe void Method(void* pBuffer);
                }

                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541E")]
                partial interface IComInterfaceDerived : IComInterfaceBase
                {
                    void Method2();
                }
                """;

            await new LegacyRulesTest<Microsoft.Interop.ComInterfaceGenerator>
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task PointerSignatureWithTypeLevelUnsafeCompilesUnderLegacyRules()
        {
            // The user is free to put 'unsafe' on the type rather than on the member. The generated stub copies
            // the member's modifiers, so it has none of its own, and under the legacy rules it still has to end
            // up inside some unsafe context.
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [assembly:DisableRuntimeMarshalling]

                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                unsafe partial interface IComInterfaceBase
                {
                    void Method(void* pBuffer);
                }
                """;

            await new LegacyRulesTest<Microsoft.Interop.ComInterfaceGenerator>
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        /// <summary>
        /// Runs <typeparamref name="TGenerator"/> without the updated memory safety rules, so that the output is
        /// checked against the rules today's users actually compile with.
        /// </summary>
        private sealed class LegacyRulesTest<TGenerator>
            : Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<
                TGenerator, Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer>.Test
            where TGenerator : new()
        {
            public LegacyRulesTest()
                : base(referenceAncillaryInterop: true)
            {
            }
        }

        [Fact]
        public async Task ComClassOutputCompilesUnderUpdatedRules()
        {
            // The COM class output comes from a different generator than the interface output, so it needs its
            // own verifier to be exercised at all.
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                partial interface INativeAPI
                {
                    void Method();
                }

                [GeneratedComClass]
                partial class C : INativeAPI
                {
                    public void Method() { }
                }
                """;

            await new UpdatedRulesTest<Microsoft.Interop.ComClassGenerator>
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        [Fact]
        public async Task VtableIndexStubOutputCompilesUnderUpdatedRules()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [UnmanagedObjectUnwrapper<UnmanagedObjectUnwrapper.TestUnwrapper>]
                partial interface INativeAPI : IUnmanagedInterfaceType
                {
                    static unsafe void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;
                    [VirtualMethodIndex(0)]
                    void Method();
                    [VirtualMethodIndex(1)]
                    int MethodWithArgs(int a);
                }
                """;

            await new UpdatedRulesTest<Microsoft.Interop.VtableIndexStubGenerator>
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            }.RunAsync();
        }

        /// <summary>
        /// Runs <typeparamref name="TGenerator"/> with the updated memory safety rules enabled, so that any
        /// generated member left without an unsafe context of its own fails the test.
        /// </summary>
        private sealed class UpdatedRulesTest<TGenerator>
            : Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<
                TGenerator, Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer>.Test
            where TGenerator : new()
        {
            public UpdatedRulesTest()
                : base(referenceAncillaryInterop: true)
            {
                // CS9377 ("the 'unsafe' modifier does not have any effect here") reports an ineffective modifier
                // on a generated type, which is the whole point of these tests. It sits above the test
                // framework's default warning level, so without this it could never be observed.
                SolutionTransforms.Add(static (solution, projectId) =>
                {
                    var options = (CSharpCompilationOptions)solution.GetProject(projectId)!.CompilationOptions!;
                    return solution.WithProjectCompilationOptions(projectId, options.WithWarningLevel(9999));
                });
            }
        }
    }
}
