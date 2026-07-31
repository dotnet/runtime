// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies that <c>IL5009</c> reports undocumented <c>unsafe</c> regions and respects an existing
    /// <c>// SAFETY:</c> comment.
    /// </summary>
    public class UnsafeBlockMissingSafetyCommentAnalyzerTests
    {
        [Fact]
        public async Task ReportsUndocumentedUnsafeBlock()
        {
            string source = """
                class C
                {
                    unsafe void M(int* p)
                    {
                        {|IL5009:unsafe|}
                        {
                            int x = *p;
                        }
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Theory]
        [InlineData("// SAFETY: p is validated by the caller")]
        [InlineData("// SAFETY:")]
        [InlineData("//SAFETY: no space after the comment marker")]
        [InlineData("// Reads one element. SAFETY: p is validated by the caller")]
        [InlineData("/* SAFETY: p is validated by the caller */")]
        public async Task DoesNotReportDocumentedUnsafeBlock(string comment)
        {
            string source = $$"""
                class C
                {
                    unsafe void M(int* p)
                    {
                        {{comment}}
                        unsafe
                        {
                            int x = *p;
                        }
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Theory]
        // The marker is matched as a whole word, so these are not safety comments.
        [InlineData("// UNSAFETY: this is a different word")]
        [InlineData("// SAFETYNET: this is a different word")]
        // It is case-sensitive so the convention stays greppable.
        [InlineData("// safety: lowercase does not count")]
        [InlineData("// Safety: mixed case does not count")]
        // The marker has to be followed by a colon.
        [InlineData("// SAFETY concerns are handled elsewhere")]
        public async Task ReportsUnsafeBlockWithoutTheSafetyMarker(string comment)
        {
            string source = $$"""
                class C
                {
                    unsafe void M(int* p)
                    {
                        {{comment}}
                        {|IL5009:unsafe|}
                        {
                            int x = *p;
                        }
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportMarkerOnInnerLineOfBlockComment()
        {
            string source = """
                class C
                {
                    unsafe void M(int* p)
                    {
                        /*
                         * SAFETY: p is validated by the caller
                         */
                        unsafe
                        {
                            int x = *p;
                        }
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportNestedUnsafeBlock()
        {
            // The nested region is covered by the reasoning recorded for the one that contains it.
            string source = """
                class C
                {
                    unsafe void M(int* p)
                    {
                        // SAFETY: p is validated by the caller
                        unsafe
                        {
                            unsafe
                            {
                                int x = *p;
                            }
                        }
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task ReportsUndocumentedUnsafeExpression()
        {
            string source = """
                class C
                {
                    static unsafe int Read() => 0;

                    void M()
                    {
                        int x = {|IL5009:unsafe|}(Read());
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportDocumentedUnsafeExpression()
        {
            // An unsafe expression sits inside a larger statement, so its comment is written above that statement.
            string source = """
                class C
                {
                    static unsafe int Read() => 0;

                    void M()
                    {
                        // SAFETY: Read only touches memory it owns
                        int x = unsafe(Read());
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task ReportsUndocumentedUnsafeExpressionInFieldInitializer()
        {
            // A field initializer cannot contain an unsafe block, so the expression form is the only option.
            string source = """
                class C
                {
                    static unsafe int Read() => 0;

                    static int Value = {|IL5009:unsafe|}(Read());
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportUnsafeModifierOnTupleReturningMember()
        {
            // The 'unsafe' modifier is followed by '(' here, but it introduces no region. This shape is common,
            // for example across the hardware intrinsics APIs.
            string source = """
                class C
                {
                    public static unsafe (int, int) GetPair() => default;

                    unsafe (int, int) Prop { get; set; }

                    unsafe (int, int) Field;

                    unsafe (int, int) this[int i] => default;
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportUnsafeExpressionNestedInDocumentedBlock()
        {
            string source = """
                class C
                {
                    static unsafe int Read() => 0;

                    void M()
                    {
                        // SAFETY: Read only touches memory it owns
                        unsafe
                        {
                            int y = unsafe(Read());
                        }
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task ReportsUnsafeExpressionInsideComplexExpression()
        {
            string source = """
                class C
                {
                    static int Foo() => 0;
                    static unsafe int Baz() => 0;
                    static int z = 0;

                    void M()
                    {
                        var x = Foo() + Foo() * {|IL5009:unsafe|}(Baz()) + z;
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task ReportsEveryUnsafeExpressionInOneStatement()
        {
            // A leading unsafe expression must not suppress its siblings: the enclosing binary expression starts
            // with the same keyword token, which is not the same thing as containing an unsafe region.
            string source = """
                class C
                {
                    static unsafe int Foo() => 0;
                    static int Bar() => 0;
                    static unsafe int Baz() => 0;
                    static int z = 0;

                    void M()
                    {
                        var x = {|IL5009:unsafe|}(Foo()) + Bar() * {|IL5009:unsafe|}(Baz()) + z;
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportUnsafeExpressionNestedInUnsafeExpression()
        {
            string source = """
                class C
                {
                    static unsafe int Foo() => 0;
                    static unsafe int Baz() => 0;

                    void M()
                    {
                        var x = {|IL5009:unsafe|}(Foo() + unsafe(Baz()));
                    }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }

        [Fact]
        public async Task DoesNotReportUnsafeModifier()
        {
            // The modifier is a contract, not a region. IL5005 covers its documentation.
            string source = """
                class C
                {
                    unsafe void M() { }
                }
                """;

            await UnsafeMigrationTestHelpers
                .CreateAnalyzerTest<UnsafeBlockMissingSafetyCommentAnalyzer>(source)
                .RunAsync();
        }
    }
}
#endif
