// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
    ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class RequiresUnsafeAnalyzerTests
    {
        static readonly string unsafeAttribute = @"
#nullable enable

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class RequiresUnsafeAttribute : Attribute
    { }
}";

        static async Task VerifyRequiresUnsafeAnalyzer(
            string source,
            params DiagnosticResult[] expected)
        {
            await VerifyCS.VerifyAnalyzerAsync(
                source + unsafeAttribute,
                consoleApplication: false,
                TestCaseUtils.UseMSBuildProperties(MSBuildPropertyOptionNames.EnableUnsafeAnalyzer),
                Array.Empty<MetadataReference>(),
                allowUnsafe: true,
                expected);
        }

        [Fact]
        public async Task SimpleDiagnostic()
        {
            var test = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public int M1() => 0;

                int M2() => M1();
            }
            class D
            {
                public int M3(C c) => c.M1();

                public class E
                {
                    public int M4(C c) => c.M1();
                }
            }
            public class E
            {
                public class F
                {
                    public int M5(C c) => c.M1();
                }
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(
                source: test,
                new[] {
                    // /0/Test0.cs(8,17): warning IL3059: Using member 'C.M1()' which has 'RequiresUnsafeAttribute' requires an unsafe context, such as an unsafe block or a method marked with 'RequiresUnsafeAttribute'.
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe).WithSpan(8, 17, 8, 19).WithArguments("C.M1()", "", ""),
                    // /0/Test0.cs(12,27): warning IL3059: Using member 'C.M1()' which has 'RequiresUnsafeAttribute' requires an unsafe context, such as an unsafe block or a method marked with 'RequiresUnsafeAttribute'.
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe).WithSpan(12, 27, 12, 31).WithArguments("C.M1()", "", ""),
                    // /0/Test0.cs(16,31): warning IL3059: Using member 'C.M1()' which has 'RequiresUnsafeAttribute' requires an unsafe context, such as an unsafe block or a method marked with 'RequiresUnsafeAttribute'.
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe).WithSpan(16, 31, 16, 35).WithArguments("C.M1()", "", ""),
                    // /0/Test0.cs(23,31): warning IL3059: Using member 'C.M1()' which has 'RequiresUnsafeAttribute' requires an unsafe context, such as an unsafe block or a method marked with 'RequiresUnsafeAttribute'.
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe).WithSpan(23, 31, 23, 35).WithArguments("C.M1()", "", "")
                });
        }

        [Fact]
        public Task InLambda()
        {
            var src = """
            using System;
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafeAttribute]
                public int M1() => 0;

                Action M2()
                {
                    return () => M1();
                }
            }
            """;
            var diag = new[] {
                // /0/Test0.cs(11,22): warning IL3059: Using member 'C.M1()' which has 'RequiresUnsafeAttribute' requires an unsafe context, such as an unsafe block or a method marked with 'RequiresUnsafeAttribute'.
                VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe).WithSpan(11, 22, 11, 24).WithArguments("C.M1()", "", "")
            };
            return VerifyRequiresUnsafeAnalyzer(src, diag);
        }

        [Fact]
        public Task InLocalFunc()
        {
            var src = """
            using System;
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public int M1() => 0;

                Action M2()
                {
                    void Wrapper() => M1();
                    return Wrapper;
                }
            }
            """;
            return VerifyRequiresUnsafeAnalyzer(
                source: src,
                new[] {
                    // /0/Test0.cs(11,27): warning IL3059: Using member 'C.M1()' which has 'RequiresUnsafeAttribute' requires an unsafe context, such as an unsafe block or a method marked with 'RequiresUnsafeAttribute'.
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe).WithSpan(11, 27, 11, 29).WithArguments("C.M1()", "", "")
                });
        }

        [Fact]
        public Task InCtor()
        {
            var src = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public static int M1() => 0;

                public C() => M1();
            }
            """;
            return VerifyRequiresUnsafeAnalyzer(
                source: src,
                expected: new[] {
                    // /0/Test0.cs(9,19): warning IL3059: Using member 'C.M1()' which has 'RequiresUnsafeAttribute' requires an unsafe context, such as an unsafe block or a method marked with 'RequiresUnsafeAttribute'.
                    VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe).WithSpan(8, 19, 8, 21).WithArguments("C.M1()", "", "")
                });
        }

        [Fact]
        public async Task RequiresUnsafeOnConstructor()
        {
            var source = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public C() { }

                public void M()
                {
                    new C();
                }
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source,
                // /0/Test0.cs(10,9): warning IL3059: Using member 'C.C()' which has 'RequiresUnsafeAttribute' requires an unsafe context, such as an unsafe block or a method marked with 'RequiresUnsafeAttribute'.
                VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafe).WithSpan(10, 9, 10, 16).WithArguments("C.C()", "", "")
            );
        }

        [Fact]
        public async Task RequiresUnsafeInSameScope()
        {
            var source = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public void M1() { }

                [RequiresUnsafe]
                public void M2()
                {
                    M1(); // Should not warn - already in RequiresUnsafe scope
                }
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source);
        }

        [Fact]
        public async Task RequiresUnsafeOnStaticConstructor()
        {
            var source = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                static C() { }
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source,
                // /0/Test0.cs(6,12): warning IL3061: 'RequiresUnsafeAttribute' cannot be placed directly on static constructor 'C.C()'.
                VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafeOnStaticConstructor).WithSpan(6, 12, 6, 13).WithArguments("C..cctor()")
            );
        }

        [Fact]
        public async Task RequiresUnsafeAttributeMismatchOnOverride()
        {
            var source = """
            using System.Diagnostics.CodeAnalysis;

            public class Base
            {
                [RequiresUnsafe]
                public virtual void M() { }
            }

            public class Derived : Base
            {
                public override void M() { } // Should warn about mismatch
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source,
                // (11,26): warning IL3060: Base member 'Base.M()' with 'RequiresUnsafeAttribute' has a derived member 'Derived.M()' without 'RequiresUnsafeAttribute'. 'RequiresUnsafeAttribute' annotations must match across all interface implementations or overrides.
                VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafeAttributeMismatch).WithSpan(11, 26, 11, 27).WithArguments("Base member 'Base.M()' with 'RequiresUnsafeAttribute' has a derived member 'Derived.M()' without 'RequiresUnsafeAttribute'")
            );
        }

        [Fact]
        public async Task RequiresUnsafeAttributeMismatchOnInterface()
        {
            var source = """
            using System.Diagnostics.CodeAnalysis;

            public interface IFoo
            {
                [RequiresUnsafe]
                void M();
            }

            public class Foo : IFoo
            {
                public void M() { } // Should warn about mismatch
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source,
                // (11,17): warning IL3060: Interface member 'IFoo.M()' with 'RequiresUnsafeAttribute' has an implementation member 'Foo.M()' without 'RequiresUnsafeAttribute'. 'RequiresUnsafeAttribute' annotations must match across all interface implementations or overrides.
                VerifyCS.Diagnostic(DiagnosticId.RequiresUnsafeAttributeMismatch).WithSpan(11, 17, 11, 18).WithArguments("Interface member 'IFoo.M()' with 'RequiresUnsafeAttribute' has an implementation member 'Foo.M()' without 'RequiresUnsafeAttribute'")
            );
        }

        [Fact]
        public async Task RequiresUnsafeInsideUnsafeBlock()
        {
            var src = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public int M1() => 0;

                public int M2()
                {
                    unsafe
                    {
                        return M1();
                    }
                }
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source: src);
        }

        [Fact]
        public async Task RequiresUnsafeInsideUnsafeMethod()
        {
            var src = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public int M1() => 0;

                public unsafe int M2()
                {
                    return M1();
                }
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source: src);
        }

        [Fact]
        public async Task RequiresUnsafeInsideUnsafeClass()
        {
            var src = """
            using System.Diagnostics.CodeAnalysis;

            public unsafe class C
            {
                [RequiresUnsafe]
                public int M1() => 0;

                public int M2()
                {
                    return M1();
                }
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source: src);
        }

        [Fact]
        public async Task RequiresUnsafeInsideUnsafeProperty()
        {
            var src = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public int M1() => 0;

                public unsafe int P => M1();
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source: src);
        }

        [Fact]
        public async Task RequiresUnsafeInsideUnsafeLocalFunction()
        {
            var src = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public int M1() => 0;

                public int M2()
                {
                    unsafe int Local() => M1();
                    return Local();
                }
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source: src);
        }

        [Fact]
        public async Task RequiresUnsafeInsideUnsafeConstructor()
        {
            var src = """
            using System.Diagnostics.CodeAnalysis;

            public class C
            {
                [RequiresUnsafe]
                public static int M1() => 0;

                public unsafe C()
                {
                    _ = M1();
                }
            }
            """;

            await VerifyRequiresUnsafeAnalyzer(source: src);
        }
    }
}
#endif
