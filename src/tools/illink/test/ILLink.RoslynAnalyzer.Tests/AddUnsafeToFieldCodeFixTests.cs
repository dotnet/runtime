// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies the <c>CS9392</c> fix for explicit and extended-layout fields, properties, and field-like events.
    /// It also covers Fix All behavior and the unfixable primary-constructor backing-field case.
    /// </summary>
    public class AddUnsafeToFieldCodeFixTests
    {
        public static TheoryData<string, string> FieldLikeMembers => new()
        {
            {
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [FieldOffset(0)]
                    public int {|CS9392:F|};
                }
                """,
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [FieldOffset(0)]
                    public unsafe int F;
                }
                """
            },
            {
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public int {|CS9392:P|} { get; set; }
                }
                """,
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public unsafe int P { get; set; }
                }
                """
            },
            {
                """
                using System;
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public event Action {|CS9392:E|};
                }
                """,
                """
                using System;
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public unsafe event Action E;
                }
                """
            },
            {
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public int {|CS9392:P|} => field;
                }
                """,
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    public unsafe int P => field;
                }
                """
            },
            {
                """
                using System.Runtime.InteropServices;

                [ExtendedLayout(ExtendedLayoutKind.CUnion)]
                struct S
                {
                    public int {|CS9392:F|};
                }
                """,
                """
                using System.Runtime.InteropServices;

                [ExtendedLayout(ExtendedLayoutKind.CUnion)]
                struct S
                {
                    public unsafe int F;
                }
                """
            },
            {
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    required public int {|CS9392:P|} { get; set; }
                }
                """,
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    [field: FieldOffset(0)]
                    required public unsafe int P { get; set; }
                }
                """
            },
            {
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    /// <summary>Stores the native value.</summary>
                    [FieldOffset(0)]
                    int {|CS9392:F|};
                }
                """,
                """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                class C
                {
                    /// <summary>Stores the native value.</summary>
                    [FieldOffset(0)]
                    unsafe int F;
                }
                """
            },
        };

        [Theory]
        [MemberData(nameof(FieldLikeMembers))]
        public async Task AddsUnsafeToFieldLikeMember(string source, string fixedSource)
        {
            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeToFieldCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }

        [Fact]
        public async Task DoesNotOfferFixForPrimaryConstructorBackingField()
        {
            var source = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                record struct R([field: FieldOffset(0)] int {|CS9392:X|});
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeToFieldCodeFixProvider>(source);
            await test.RunAsync();
        }

        [Fact]
        public async Task FixAllAddsUnsafeToEveryField()
        {
            var source = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                struct S
                {
                    [FieldOffset(0)] public int {|CS9392:F1|};
                    [FieldOffset(4)] public int {|CS9392:F2|};
                }
                """;
            var fixedSource = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                struct S
                {
                    [FieldOffset(0)] public unsafe int F1;
                    [FieldOffset(4)] public unsafe int F2;
                }
                """;

            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeToFieldCodeFixProvider>(
                    source,
                    fixedSource);
            test.BatchFixedCode = fixedSource;
            test.NumberOfIncrementalIterations = 2;
            test.NumberOfFixAllIterations = 1;
            await test.RunAsync();
        }
    }
}
#endif
