// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies that <c>IL5010</c> reports an explicit <c>safe</c> modifier that records no justification.
    /// </summary>
    public class SafeModifierMissingJustificationAnalyzerTests
    {
        [Fact]
        public async Task ReportsUndocumentedSafeExternMethod()
        {
            string source = """
                using System.Runtime.InteropServices;

                class C
                {
                    [DllImport("nativelib")]
                    public static {|IL5010:safe|} extern int M(int x);
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<SafeModifierMissingJustificationAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportDocumentedSafeExternMethod()
        {
            string source = """
                using System.Runtime.InteropServices;

                class C
                {
                    /// <safety>The native entry point only reads the value it is given.</safety>
                    [DllImport("nativelib")]
                    public static safe extern int M(int x);
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<SafeModifierMissingJustificationAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task ReportsUndocumentedSafeFieldInExplicitLayout()
        {
            string source = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                struct S
                {
                    [FieldOffset(0)]
                    public {|IL5010:safe|} int Value;
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<SafeModifierMissingJustificationAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportDocumentedSafeFieldInExplicitLayout()
        {
            string source = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                struct S
                {
                    /// <safety>Both overlapping fields are unmanaged and the same size.</safety>
                    [FieldOffset(0)]
                    public safe int Value;
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<SafeModifierMissingJustificationAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportMembersWithoutSafeModifier()
        {
            string source = """
                using System.Runtime.InteropServices;

                class C
                {
                    [DllImport("nativelib")]
                    public static unsafe extern int M(int x);

                    public void Other() { }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<SafeModifierMissingJustificationAnalyzer>(source)
                .RunAsync();
        }
    }
}
#endif
