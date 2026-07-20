// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies the <c>IL5005</c> fixer removes legacy unsafe scopes without weakening pointer compatibility.
    /// Explicit and extended-layout cases confirm that required <c>CS9392</c> markers become <c>safe</c> instead.
    /// </summary>
    public class RemoveUndocumentedUnsafeCodeFixTests
    {
        [Fact]
        public async Task RemovesUnsafeFromOrdinaryMember()
        {
            var source = """
                class C
                {
                    public {|IL5005:unsafe|} void M() { }
                }
                """;
            var fixedSource = """
                class C
                {
                    public void M() { }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeMemberMissingSafetyDocumentationAnalyzer, RemoveUndocumentedUnsafeCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task RemovesUnsafeFromAccessor()
        {
            var source = """
                class C
                {
                    public int P
                    {
                        {|IL5005:unsafe|} get => 0;
                    }
                }
                """;
            var fixedSource = """
                class C
                {
                    public int P
                    {
                        get => 0;
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeMemberMissingSafetyDocumentationAnalyzer, RemoveUndocumentedUnsafeCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task ReplacesUnsafeWithSafeForExplicitLayoutField()
        {
            var source = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [FieldOffset(0)]
                    public {|IL5005:unsafe|} int F;
                }
                """;
            var fixedSource = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [FieldOffset(0)]
                    public safe int F;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeMemberMissingSafetyDocumentationAnalyzer, RemoveUndocumentedUnsafeCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task ReplacesUnsafeWithSafeForExplicitLayoutProperty()
        {
            var source = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public {|IL5005:unsafe|} int P { get; set; }
                }
                """;
            var fixedSource = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public safe int P { get; set; }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeMemberMissingSafetyDocumentationAnalyzer, RemoveUndocumentedUnsafeCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task ReplacesUnsafeWithSafeForExplicitLayoutEvent()
        {
            var source = """
                using System;
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public {|IL5005:unsafe|} event Action E;
                }
                """;
            var fixedSource = """
                using System;
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public safe event Action E;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeMemberMissingSafetyDocumentationAnalyzer, RemoveUndocumentedUnsafeCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task ReplacesUnsafeWithSafeForExtendedLayoutField()
        {
            var source = """
                using System.Runtime.InteropServices;

                [ExtendedLayout(ExtendedLayoutKind.CUnion)]
                struct S
                {
                    public {|IL5005:unsafe|} int F;
                }
                """;
            var fixedSource = """
                using System.Runtime.InteropServices;

                [ExtendedLayout(ExtendedLayoutKind.CUnion)]
                struct S
                {
                    public safe int F;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeMemberMissingSafetyDocumentationAnalyzer, RemoveUndocumentedUnsafeCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task FixAllKeepsUnsafeOnPointerSignatures()
        {
            var source = """
                class C
                {
                    public {|IL5005:unsafe|} void M() { }
                    public {|IL5005:unsafe|} int* Pointer(int* value) => value;
                }
                """;
            var fixedSource = """
                class C
                {
                    public void M() { }
                    public {|IL5005:unsafe|} int* Pointer(int* value) => value;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeMemberMissingSafetyDocumentationAnalyzer, RemoveUndocumentedUnsafeCodeFixProvider>(
                    source,
                    fixedSource);
            test.BatchFixedCode = fixedSource;
            test.FixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;
            test.BatchFixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;
            await test.RunAsync();
        }
    }
}
#endif
