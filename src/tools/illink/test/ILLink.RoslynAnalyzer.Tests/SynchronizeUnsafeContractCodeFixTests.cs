// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies the <c>CS9364</c>, <c>CS9365</c> and <c>CS9366</c> code fix, which resolves a member that is
    /// <c>unsafe</c> while the member it overrides or implements is not.
    /// </summary>
    public class SynchronizeUnsafeContractCodeFixTests
    {
        public static TheoryData<string, string> RemoveFromDerived => new()
        {
            {
                """
                abstract class B
                {
                    public abstract int M();
                }

                class D : B
                {
                    public unsafe override int {|CS9364:M|}() => 0;
                }
                """,
                """
                abstract class B
                {
                    public abstract int M();
                }

                class D : B
                {
                    public override int M() => 0;
                }
                """
            },
            {
                """
                interface I
                {
                    int M();
                }

                class C : I
                {
                    public unsafe int {|CS9365:M|}() => 0;
                }
                """,
                """
                interface I
                {
                    int M();
                }

                class C : I
                {
                    public int M() => 0;
                }
                """
            },
            {
                """
                interface I
                {
                    int M();
                }

                class C : I
                {
                    unsafe int I.{|CS9366:M|}() => 0;
                }
                """,
                """
                interface I
                {
                    int M();
                }

                class C : I
                {
                    int I.M() => 0;
                }
                """
            },
            {
                """
                abstract class B
                {
                    public abstract int P { get; }
                }

                class D : B
                {
                    public unsafe override int P => {|CS9364:0|};
                }
                """,
                """
                abstract class B
                {
                    public abstract int P { get; }
                }

                class D : B
                {
                    public override int P => 0;
                }
                """
            },
        };

        [Theory]
        [MemberData(nameof(RemoveFromDerived))]
        public async Task RemovesUnsafeFromDerivedMember(string source, string fixedSource)
        {
            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, SynchronizeUnsafeContractCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task MarksBaseMemberUnsafe()
        {
            string source = """
                abstract class B
                {
                    public abstract int M();
                }

                class D : B
                {
                    public unsafe override int {|CS9364:M|}() => 0;
                }
                """;

            string fixedSource = """
                abstract class B
                {
                    public abstract unsafe int M();
                }

                class D : B
                {
                    public unsafe override int M() => 0;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, SynchronizeUnsafeContractCodeFixProvider>(
                    source,
                    fixedSource);
            test.CodeActionIndex = 1;
            await test.RunAsync();
        }

        [Fact]
        public async Task ReplacesUnsafeWithSafeOnExternMember()
        {
            // Removing 'unsafe' outright would only trade CS9364 for CS9389, so the narrowing edit uses 'safe'.
            string source = """
                abstract class B
                {
                    public abstract object M();
                }

                class D : B
                {
                    public unsafe extern override object {|CS9364:M|}();
                }
                """;

            string fixedSource = """
                abstract class B
                {
                    public abstract object M();
                }

                class D : B
                {
                    public safe extern override object M();
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, SynchronizeUnsafeContractCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task MarksEveryPartOfPartialBaseMemberUnsafe()
        {
            // Both parts must be annotated, otherwise the fix trades CS9364 for CS0764.
            string source = """
                partial class B
                {
                    public virtual partial int M();
                }

                partial class B
                {
                    public virtual partial int M() => 0;
                }

                class D : B
                {
                    public unsafe override int {|CS9364:M|}() => 0;
                }
                """;

            string fixedSource = """
                partial class B
                {
                    public virtual unsafe partial int M();
                }

                partial class B
                {
                    public virtual unsafe partial int M() => 0;
                }

                class D : B
                {
                    public unsafe override int M() => 0;
                }
                """;
            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, SynchronizeUnsafeContractCodeFixProvider>(
                    source,
                    fixedSource);
            test.CodeActionIndex = 1;
            await test.RunAsync();
        }

        [Fact]
        public async Task MarksInterfaceMemberUnsafe()
        {
            string source = """
                interface I
                {
                    int M();
                }

                class C : I
                {
                    public unsafe int {|CS9365:M|}() => 0;
                }
                """;

            string fixedSource = """
                interface I
                {
                    unsafe int M();
                }

                class C : I
                {
                    public unsafe int M() => 0;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, SynchronizeUnsafeContractCodeFixProvider>(
                    source,
                    fixedSource);
            test.CodeActionIndex = 1;
            await test.RunAsync();
        }
    }
}
#endif
