// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// Verifies the <c>IL5006</c> code fix across pointer-bearing methods, constructors, properties, events, and fields.
    /// The expected output checks that <c>unsafe</c> is added at the correct declaration level and modifier position.
    /// </summary>
    public class AddUnsafeToPointerSignatureCodeFixTests
    {
        public static TheoryData<string, string> PointerMembers => new()
        {
            {
                """
                class C
                {
                    public int* {|IL5006:M|}(int* value) => value;
                }
                """,
                """
                class C
                {
                    public unsafe int* M(int* value) => value;
                }
                """
            },
            {
                """
                class C
                {
                    public {|IL5006:C|}(delegate*<void> callback) { }
                }
                """,
                """
                class C
                {
                    public unsafe C(delegate*<void> callback) { }
                }
                """
            },
            {
                """
                class C
                {
                    public int* {|IL5006:P|} { get; set; }
                }
                """,
                """
                class C
                {
                    public unsafe int* P { get; set; }
                }
                """
            },
            {
                """
                class C
                {
                    public int* {|IL5006:this|}[int* index] => index;
                }
                """,
                """
                class C
                {
                    public unsafe int* this[int* index] => index;
                }
                """
            },
            {
                """
                class C
                {
                    public delegate*<void> {|IL5006:F1|}, F2;
                }
                """,
                """
                class C
                {
                    public unsafe delegate*<void> F1, F2;
                }
                """
            },
            {
                """
                class Outer<T>
                {
                    public delegate void D();
                }

                class C
                {
                    public event Outer<int*[]>.D {|IL5006:E|};
                }
                """,
                """
                class Outer<T>
                {
                    public delegate void D();
                }

                class C
                {
                    public unsafe event Outer<int*[]>.D E;
                }
                """
            },
            {
                """
                class Outer<T>
                {
                    public delegate void D();
                }

                class C
                {
                    public event Outer<int*[]>.D {|IL5006:E|}
                    {
                        add { }
                        remove { }
                    }
                }
                """,
                """
                class Outer<T>
                {
                    public delegate void D();
                }

                class C
                {
                    public unsafe event Outer<int*[]>.D E
                    {
                        add { }
                        remove { }
                    }
                }
                """
            },
            {
                """
                class C
                {
                    public static int* operator {|IL5006:+|}(C value, int offset) => null;
                }
                """,
                """
                class C
                {
                    public static unsafe int* operator +(C value, int offset) => null;
                }
                """
            },
            {
                """
                class C
                {
                    public static explicit operator {|IL5006:int*|}(C value) => null;
                }
                """,
                """
                class C
                {
                    public static unsafe explicit operator int*(C value) => null;
                }
                """
            },
            {
                """
                class C
                {
                    void M()
                    {
                        static int* {|IL5006:Local|}(int* value) => value;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        static unsafe int* Local(int* value) => value;
                    }
                }
                """
            },
            {
                """
                class C
                {
                    required public int* {|IL5006:P|} { get; set; }
                }
                """,
                """
                class C
                {
                    required public unsafe int* P { get; set; }
                }
                """
            },
            {
                """
                class C
                {
                    /// <summary>Returns the supplied pointer.</summary>
                    int* {|IL5006:M|}(int* value) => value;
                }
                """,
                """
                class C
                {
                    /// <summary>Returns the supplied pointer.</summary>
                    unsafe int* M(int* value) => value;
                }
                """
            },
        };

        [Theory]
        [MemberData(nameof(PointerMembers))]
        public async Task AddsUnsafeToPointerSignature(string source, string fixedSource)
        {
            var test = UnsafeMigrationTestHelpers
                .CreateCodeFixTest<PointerSignatureRequiresUnsafeAnalyzer, AddUnsafeToPointerSignatureCodeFixProvider>(
                    source,
                    fixedSource);
            await test.RunAsync();
        }
    }
}
#endif
