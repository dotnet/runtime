// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies the <c>CS9389</c> code fix for every extern declaration shape supported by the compiler.
    /// The expected output also checks modifier ordering and preservation on destructors and local functions.
    /// </summary>
    public class AddUnsafeToExternCodeFixTests
    {
        public static TheoryData<string, string> MemberKinds => new()
        {
            {
                """
                class C
                {
                    public {|CS9389:extern|} void M();
                }
                """,
                """
                class C
                {
                    public extern unsafe void M();
                }
                """
            },
            {
                """
                class C
                {
                    public {|CS9389:extern|} int P { get; set; }
                }
                """,
                """
                class C
                {
                    public extern unsafe int P { get; set; }
                }
                """
            },
            {
                """
                class C
                {
                    public {|CS9389:extern|} int this[int index] { get; set; }
                }
                """,
                """
                class C
                {
                    public extern unsafe int this[int index] { get; set; }
                }
                """
            },
            {
                """
                using System;

                class C
                {
                    public static {|CS9389:extern|} event Action E;
                }
                """,
                """
                using System;

                class C
                {
                    public static extern unsafe event Action E;
                }
                """
            },
            {
                """
                class C
                {
                    public {|CS9389:extern|} C(int value);
                }
                """,
                """
                class C
                {
                    public extern unsafe C(int value);
                }
                """
            },
            {
                """
                class C
                {
                    public static {|CS9389:extern|} C operator +(C left, C right);
                }
                """,
                """
                class C
                {
                    public static extern unsafe C operator +(C left, C right);
                }
                """
            },
            {
                """
                class C
                {
                    public static {|CS9389:extern|} explicit operator int(C value);
                }
                """,
                """
                class C
                {
                    public static extern unsafe explicit operator int(C value);
                }
                """
            },
            {
                """
                class C
                {
                    {|CS9389:extern|} ~C();
                }
                """,
                """
                class C
                {
                    extern unsafe ~C();
                }
                """
            },
            {
                """
                class C
                {
                    void M()
                    {
                        static {|CS9389:extern|} void Local();
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        static extern unsafe void Local();
                    }
                }
                """
            },
            {
                """
                class C
                {
                    required public {|CS9389:extern|} int P { get; set; }
                }
                """,
                """
                class C
                {
                    required public extern unsafe int P { get; set; }
                }
                """
            },
            {
                """
                class C
                {
                    /// <safety>Audited interop boundary.</safety>
                    {|CS9389:extern|} void M();
                }
                """,
                """
                class C
                {
                    /// <safety>Audited interop boundary.</safety>
                    extern safe void M();
                }
                """
            },
            {
                """
                class C
                {
                    /// <summary>Invokes native code.</summary>
                    {|CS9389:extern|} void M();
                }
                """,
                """
                class C
                {
                    /// <summary>Invokes native code.</summary>
                    extern unsafe void M();
                }
                """
            },
        };

        [Theory]
        [MemberData(nameof(MemberKinds))]
        public async Task AddsUnsafeToExternMember(string source, string fixedSource)
        {
            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<DynamicallyAccessedMembersAnalyzer, AddUnsafeToExternCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }
    }
}
#endif
