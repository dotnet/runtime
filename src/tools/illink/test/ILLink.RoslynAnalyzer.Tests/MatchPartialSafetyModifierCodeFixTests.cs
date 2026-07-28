// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies the <c>CS0764</c> and <c>CS9390</c> code fix, which copies the safety modifier onto the partial
    /// declaration that is missing it.
    /// </summary>
    public class MatchPartialSafetyModifierCodeFixTests
    {
        [Fact]
        public async Task AddsUnsafeToImplementingPart()
        {
            string source = """
                partial class C
                {
                    public unsafe partial int M();
                }

                partial class C
                {
                    public partial int {|CS0764:M|}() => 0;
                }
                """;

            string fixedSource = """
                partial class C
                {
                    public unsafe partial int M();
                }

                partial class C
                {
                    public unsafe partial int M() => 0;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, MatchPartialSafetyModifierCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task AddsUnsafeToDefiningPart()
        {
            // The compiler always reports on the implementing part, so the fixer has to edit the other one.
            string source = """
                partial class C
                {
                    public partial int M();
                }

                partial class C
                {
                    public unsafe partial int {|CS0764:M|}() => 0;
                }
                """;

            string fixedSource = """
                partial class C
                {
                    public unsafe partial int M();
                }

                partial class C
                {
                    public unsafe partial int M() => 0;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, MatchPartialSafetyModifierCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task AddsSafeToImplementingPart()
        {
            string source = """
                using System.Runtime.InteropServices;

                partial class C
                {
                    public static safe partial int M(int x);
                }

                partial class C
                {
                    [DllImport("nativelib")]
                    public static extern partial int {|CS9390:M|}(int x);
                }
                """;

            string fixedSource = """
                using System.Runtime.InteropServices;

                partial class C
                {
                    public static safe partial int M(int x);
                }

                partial class C
                {
                    [DllImport("nativelib")]
                    public static safe extern partial int M(int x);
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, MatchPartialSafetyModifierCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }
        [Fact]
        public async Task DoesNotOfferFixWhenPartsDisagreeInOppositeDirections()
        {
            // The parts declare conflicting contracts, so the compiler reports both CS0764 and CS9390. Adding
            // the missing modifier would put 'safe' and 'unsafe' on the same declaration, which is an error.
            string source = """
                using System.Runtime.InteropServices;

                partial class C
                {
                    public static unsafe partial int M(int x);
                }

                partial class C
                {
                    [DllImport("nativelib")]
                    public static safe extern partial int {|CS0764:{|CS9390:M|}|}(int x);
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, MatchPartialSafetyModifierCodeFixProvider>(
                    source);
            await test.RunAsync();
        }
    }
}
#endif
