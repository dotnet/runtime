// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies the <c>IL5009</c> code fix, which inserts a <c>// SAFETY: TODO</c> stub above an undocumented
    /// <c>unsafe</c> region.
    /// </summary>
    public class AddSafetyCommentCodeFixTests
    {
        [Fact]
        public async Task AddsCommentAboveUnsafeBlock()
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

            string fixedSource = """
                class C
                {
                    unsafe void M(int* p)
                    {
                        // SAFETY: TODO
                        unsafe
                        {
                            int x = *p;
                        }
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeBlockMissingSafetyCommentAnalyzer, AddSafetyCommentCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task AddsCommentAboveStatementContainingUnsafeExpression()
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

            string fixedSource = """
                class C
                {
                    static unsafe int Read() => 0;

                    void M()
                    {
                        // SAFETY: TODO
                        int x = unsafe(Read());
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeBlockMissingSafetyCommentAnalyzer, AddSafetyCommentCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task PreservesExistingLeadingComment()
        {
            string source = """
                class C
                {
                    unsafe void M(int* p)
                    {
                        // Read the first element.
                        {|IL5009:unsafe|}
                        {
                            int x = *p;
                        }
                    }
                }
                """;

            string fixedSource = """
                class C
                {
                    unsafe void M(int* p)
                    {
                        // SAFETY: TODO
                        // Read the first element.
                        unsafe
                        {
                            int x = *p;
                        }
                    }
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<UnsafeBlockMissingSafetyCommentAnalyzer, AddSafetyCommentCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }
    }
}
#endif
