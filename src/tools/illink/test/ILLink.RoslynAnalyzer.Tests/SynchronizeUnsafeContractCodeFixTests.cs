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

        [Fact]
        public async Task MarksRootOfTheOverrideChainUnsafe()
        {
            // The compiler compares an override against the original definition of its chain, so the root is
            // what has to be annotated. Marking the immediate base would only move the diagnostic onto it.
            string source = """
                abstract class A
                {
                    public abstract int M();
                }

                abstract class B : A
                {
                    public override int M() => 0;
                }

                class C : B
                {
                    public unsafe override int {|CS9364:M|}() => 0;
                }
                """;

            string fixedSource = """
                abstract class A
                {
                    public abstract unsafe int M();
                }

                abstract class B : A
                {
                    public override int M() => 0;
                }

                class C : B
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
        public async Task MarksBothTheBaseAndTheInterfaceMemberUnsafe()
        {
            // A member that overrides one member while implementing another reports both diagnostics, and stays
            // broken unless both contracts are annotated.
            string source = """
                interface I
                {
                    int M();
                }

                abstract class A
                {
                    public abstract int M();
                }

                class C : A, I
                {
                    public unsafe override int {|CS9365:{|CS9364:M|}|}() => 0;
                }
                """;

            string fixedSource = """
                interface I
                {
                    unsafe int M();
                }

                abstract class A
                {
                    public abstract unsafe int M();
                }

                class C : A, I
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
        public async Task MarksBaseOfFieldLikeEventUnsafe()
        {
            // The semantic model has no symbol for the event declaration itself, only for its declarator.
            string source = """
                using System;

                abstract class B
                {
                    public abstract event Action E;
                }

                class D : B
                {
                    public override unsafe event Action {|CS9364:E|};
                }
                """;

            string fixedSource = """
                using System;

                abstract class B
                {
                    public abstract unsafe event Action E;
                }

                class D : B
                {
                    public override unsafe event Action E;
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
        public async Task MarksInterfaceEventOfFieldLikeEventUnsafe()
        {
            string source = """
                using System;

                interface I
                {
                    event Action E;
                }

                class C : I
                {
                    public unsafe event Action {|CS9365:E|};
                }
                """;

            string fixedSource = """
                using System;

                interface I
                {
                    unsafe event Action E;
                }

                class C : I
                {
                    public unsafe event Action E;
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
        public async Task MarksInterfaceEventOfOneOfSeveralDeclaratorsUnsafe()
        {
            // A field-like event declaration declares one symbol per variable, so the fix has to follow the
            // declarator the diagnostic points at rather than the declaration.
            string source = """
                using System;

                interface I
                {
                    event Action Second;
                }

                class C : I
                {
                    public unsafe event Action First, {|CS9365:Second|};
                }
                """;

            string fixedSource = """
                using System;

                interface I
                {
                    unsafe event Action Second;
                }

                class C : I
                {
                    public unsafe event Action First, Second;
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
        public async Task MarksInterfaceMemberImplementedByTheOverrideRootUnsafe()
        {
            // The interface implementation belongs to the member that declares it, not to the most derived
            // override, so annotating only the override root would leave a fresh CS9365 behind on it.
            string source = """
                interface I
                {
                    int M();
                }

                class A : I
                {
                    public virtual int M() => 0;
                }

                class C : A
                {
                    public unsafe override int {|CS9364:M|}() => 0;
                }
                """;

            string fixedSource = """
                interface I
                {
                    unsafe int M();
                }

                class A : I
                {
                    public virtual unsafe int M() => 0;
                }

                class C : A
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
        public async Task MarksSharedDeclarationOfSeveralInterfacesOnce()
        {
            // Two constructions of the same interface are two contracts sharing one declaration, which the
            // editor can only be asked to replace once.
            string source = """
                interface I<T>
                {
                    void M();
                }

                class C : I<int>, I<string>
                {
                    public unsafe void {|CS9365:{|CS9365:M|}|}() { }
                }
                """;

            string fixedSource = """
                interface I<T>
                {
                    unsafe void M();
                }

                class C : I<int>, I<string>
                {
                    public unsafe void M() { }
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
        public async Task RemovesUnsafeFromDerivedFieldLikeEvent()
        {
            string source = """
                using System;

                abstract class B
                {
                    public abstract event Action E;
                }

                class D : B
                {
                    public override unsafe event Action {|CS9364:E|};
                }
                """;

            string fixedSource = """
                using System;

                abstract class B
                {
                    public abstract event Action E;
                }

                class D : B
                {
                    public override event Action E;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, SynchronizeUnsafeContractCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }
    }
}
#endif
