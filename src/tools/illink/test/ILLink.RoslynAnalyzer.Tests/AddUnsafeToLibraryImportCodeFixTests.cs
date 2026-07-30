// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies the <c>IL5007</c> code fix, which conservatively marks methods with
    /// <c>LibraryImportAttribute</c> as <c>unsafe</c> so a code base can be migrated to the updated memory safety
    /// rules before the opt-in is flipped.
    /// </summary>
    public class AddUnsafeToLibraryImportCodeFixTests
    {
        public static TheoryData<string, string> Declarations => new()
        {
            {
                """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib")]
                    static partial void {|IL5007:Method|}(int i);
                }
                """,
                """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib")]
                    static unsafe partial void Method(int i);
                }
                """
            },
            {
                """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib", StringMarshalling = StringMarshalling.Utf8)]
                    static partial void {|IL5007:Method|}(string s);
                }
                """,
                """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib", StringMarshalling = StringMarshalling.Utf8)]
                    static unsafe partial void Method(string s);
                }
                """
            },
            {
                // The fix keeps documentation and attribute trivia attached to the declaration.
                """
                using System.Runtime.InteropServices;

                partial class C
                {
                    /// <summary>Invokes native code.</summary>
                    [LibraryImport("nativelib")]
                    static partial void {|IL5007:Method|}(int i);
                }
                """,
                """
                using System.Runtime.InteropServices;

                partial class C
                {
                    /// <summary>Invokes native code.</summary>
                    [LibraryImport("nativelib")]
                    static unsafe partial void Method(int i);
                }
                """
            },
        };

        [Theory]
        [MemberData(nameof(Declarations))]
        public async Task AddsUnsafeToLibraryImportMethod(string source, string fixedSource)
        {
            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<LibraryImportRequiresExplicitSafetyAnalyzer, AddUnsafeToLibraryImportCodeFixProvider>(
                    source,
                    fixedSource,
                    updatedMemorySafetyRules: false);
            // LibraryImportAttribute only exists in the live reference pack.
            test.ReferenceAssemblies = new ReferenceAssemblies(string.Empty);
            test.TestState.AdditionalReferences.AddRange(SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences());
            await test.RunAsync();
        }

        [Fact]
        public async Task IsIdempotentForAlreadyAnnotatedMethods()
        {
            string source = """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib")]
                    static unsafe partial void Method(int i);
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<LibraryImportRequiresExplicitSafetyAnalyzer, AddUnsafeToLibraryImportCodeFixProvider>(
                    source,
                    updatedMemorySafetyRules: false);
            test.ReferenceAssemblies = new ReferenceAssemblies(string.Empty);
            test.TestState.AdditionalReferences.AddRange(SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences());
            await test.RunAsync();
        }
    }
}
#endif
