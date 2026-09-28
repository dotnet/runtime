// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies that <c>IL5007</c> reports methods with <c>LibraryImportAttribute</c> that do not declare an
    /// explicit safety contract, independently of the shape the source generator would emit for them.
    /// </summary>
    public class LibraryImportRequiresExplicitSafetyAnalyzerTests
    {
        public static TheoryData<string> UnannotatedDeclarations => new()
        {
            // A blittable signature is implemented by an `extern` forwarder.
            "static partial void {|IL5007:Method|}(int i);",
            // A signature that needs marshalling is implemented by a wrapper around an `extern` local function.
            "static partial void {|IL5007:Method|}(string s);",
            "static partial void {|IL5007:Method|}();",
        };

        [Theory]
        [MemberData(nameof(UnannotatedDeclarations))]
        public async Task ReportsMethodWithoutSafetyModifier(string declaration)
        {
            string source = $$"""
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib")]
                    {{declaration}}
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<LibraryImportRequiresExplicitSafetyAnalyzer>(source, updatedMemorySafetyRules: false)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportMethodMarkedUnsafe()
        {
            string source = """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib")]
                    static unsafe partial void Method(int i);
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<LibraryImportRequiresExplicitSafetyAnalyzer>(source, updatedMemorySafetyRules: false)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportMethodMarkedSafe()
        {
            // 'safe' is only allowed on 'extern' members today, so this uses the shape the generator emits for a
            // blittable signature: an 'extern' implementing part that agrees with the user's declaration.
            string source = """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib")]
                    public static safe partial int Method(int i);
                }

                partial class C
                {
                    [DllImport("nativelib", EntryPoint = "Method", ExactSpelling = true)]
                    public static safe extern partial int Method(int i);
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<LibraryImportRequiresExplicitSafetyAnalyzer>(source, updatedMemorySafetyRules: false)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportMethodWithoutLibraryImportAttribute()
        {
            string source = """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [DllImport("nativelib")]
                    static extern unsafe void Method(int i);

                    static partial void Other(int i);
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<LibraryImportRequiresExplicitSafetyAnalyzer>(source, updatedMemorySafetyRules: false)
                .RunAsync();
        }

        [Fact]
        public async Task ReportsOnceForPartialMemberWithImplementation()
        {
            // The wrapper shape the generator emits for a signature that needs marshalling: the implementing part
            // is not 'extern', so neither part carries a safety modifier and only the user's part is reported.
            string source = """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib")]
                    public static partial int {|IL5007:Method|}(int i);
                }

                partial class C
                {
                    public static partial int Method(int i) => 0;
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<LibraryImportRequiresExplicitSafetyAnalyzer>(source, updatedMemorySafetyRules: false)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportWhenUpdatedRulesAreEnabled()
        {
            // Once the assembly is on the updated rules the generator reports SYSLIB1064 for the same methods,
            // so this analyzer would only be a second copy of the same message.
            string source = """
                using System.Runtime.InteropServices;

                partial class C
                {
                    [LibraryImport("nativelib")]
                    static partial void Method(int i);
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<LibraryImportRequiresExplicitSafetyAnalyzer>(source, updatedMemorySafetyRules: true)
                .RunAsync();
        }
    }
}
#endif
