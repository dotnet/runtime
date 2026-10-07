// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Threading.Tasks;
using ILLink.CodeFix;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public partial class UnsafeContextCodeFixTests
    {
        private const string BodyWideKey = nameof(UnsafeContextCodeFixProvider) + ".BodyWide";
        private const string ExpressionFirstKey = nameof(UnsafeContextCodeFixProvider) + ".ExpressionFirst";

        [Fact]
        public Task BodyWideAction_WrapsWholeBody()
        {
            return VerifyAsync("""
                class C
                {
                    static void Safe() { }

                    void M(int* p)
                    {
                        {|CS9360:*|}p = 1;
                        Safe();
                        Safe();
                        Safe();
                        {|CS9360:*|}p = 2;
                    }
                }
                """, """
                class C
                {
                    static void Safe() { }

                    void M(int* p)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 1;
                            Safe();
                            Safe();
                            Safe();
                            *p = 2;
                        }
                    }
                }
                """, equivalenceKey: BodyWideKey);
        }

        [Fact]
        public Task SparseOperations_SeparateBlocksByDefault()
        {
            return VerifyAsync("""
                class C
                {
                    static void Safe() { }

                    void M(int* p)
                    {
                        {|CS9360:*|}p = 1;
                        Safe();
                        Safe();
                        Safe();
                        {|CS9360:*|}p = 2;
                    }
                }
                """, """
                class C
                {
                    static void Safe() { }

                    void M(int* p)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 1;
                        }
                        Safe();
                        Safe();
                        Safe();
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 2;
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task ExpressionFirstAction_UsesWrapper()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe int Get() => 0;

                    int M()
                    {
                        int value = {|CS9362:Get()|};
                        return value;
                    }
                }
                """, """
                class C
                {
                    static unsafe int Get() => 0;

                    int M()
                    {
                        int value = /* SAFETY: To be audited */ unsafe(Get());
                        return value;
                    }
                }
                """, equivalenceKey: ExpressionFirstKey);
        }

        [Fact]
        public Task CallerArgumentExpression_NeverWrapsCapturedArgument()
        {
            // The lambda rules out a block, and a wrapper would change the captured message: no fix is offered.
            const string source = """
                using System;
                using System.Runtime.CompilerServices;

                class C
                {
                    static void Check(bool condition, Action callback, [CallerArgumentExpression(nameof(condition))] string message = "") { }

                    void M(int* p)
                    {
                        Check({|CS9360:*|}p == 0, static () => { });
                    }
                }
                """;
            return VerifyAsync(source, source);
        }

        [Fact]
        public Task CallerArgumentExpression_InConstructorInitializer_NeverWrapsCapturedArgument()
        {
            // Initializer arguments are captured like any other, and no block can cover an initializer: no fix is offered.
            const string source = """
                using System.Runtime.CompilerServices;

                class Base
                {
                    public Base(int value, [CallerArgumentExpression(nameof(value))] string expression = "") { }
                }

                class Derived : Base
                {
                    static unsafe int Get() => 0;

                    public Derived()
                        : base({|CS9362:Get()|})
                    {
                    }

                    public Derived(bool flag)
                        : this({|CS9362:Get()|}, flag)
                    {
                    }

                    public Derived(int value, bool flag, [CallerArgumentExpression(nameof(value))] string expression = "")
                        : base(value)
                    {
                    }
                }
                """;
            return VerifyAsync(source, source);
        }

        [Fact]
        public Task ExtendsAdjacentPlaceholderBlock()
        {
            return VerifyAsync("""
                class C
                {
                    void M(int* p, int* q)
                    {
                        // Copies the value.
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 1;
                        }
                        {|CS9360:*|}q = 2;
                    }
                }
                """, """
                class C
                {
                    void M(int* p, int* q)
                    {
                        // Copies the value.
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 1;
                            *q = 2;
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task DoesNotExtendAuditedBlock()
        {
            return VerifyAsync("""
                class C
                {
                    void M(int* p, int* q)
                    {
                        // SAFETY: p points to a live local.
                        unsafe
                        {
                            *p = 1;
                        }
                        {|CS9360:*|}q = 2;
                    }
                }
                """, """
                class C
                {
                    void M(int* p, int* q)
                    {
                        // SAFETY: p points to a live local.
                        unsafe
                        {
                            *p = 1;
                        }
                        // SAFETY: To be audited
                        unsafe
                        {
                            *q = 2;
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task OutVariableInIfCondition_IsLiftedBeforeStatement()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe bool TryGet(out int value) { value = 0; return true; }

                    int M(bool ready, int* p)
                    {
                        if (!ready || !{|CS9362:TryGet(out int value)|})
                            return {|CS9360:*|}p;

                        return value;
                    }
                }
                """, """
                class C
                {
                    static unsafe bool TryGet(out int value) { value = 0; return true; }

                    int M(bool ready, int* p)
                    {
                        int value;
                        // SAFETY: To be audited
                        unsafe
                        {
                            if (!ready || !TryGet(out value))
                                return *p;
                        }

                        return value;
                    }
                }
                """);
        }

        [Fact]
        public Task HeaderOnlyIf_WrapsCondition()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe bool TryGet(out int value) { value = 0; return true; }

                    int M(bool ready)
                    {
                        if (!ready || !{|CS9362:TryGet(out int value)|})
                            return 0;

                        return value;
                    }
                }
                """, """
                class C
                {
                    static unsafe bool TryGet(out int value) { value = 0; return true; }

                    int M(bool ready)
                    {
                        if (!ready || !/* SAFETY: To be audited */ unsafe(TryGet(out int value)))
                            return 0;

                        return value;
                    }
                }
                """);
        }

        [Fact]
        public Task MixedDiagnostics_ShareOneBlock()
        {
            return VerifyAsync("""
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                class C
                {
                    static unsafe void Use(Span<byte> span) { }

                    void M(byte* p)
                    {
                        {|CS9360:*|}p = 0;
                        {|CS9362:Use({|CS9361:stackalloc byte[16]|})|};
                    }
                }
                """, """
                using System;
                using System.Runtime.CompilerServices;

                [module: SkipLocalsInit]

                class C
                {
                    static unsafe void Use(Span<byte> span) { }

                    void M(byte* p)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = 0;
                            Use(stackalloc byte[16]);
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task GeneratedCode_NoFix()
        {
            const string source = """
                // <auto-generated/>
                class C
                {
                    void M(int* p)
                    {
                        {|CS9360:*|}p = 0;
                    }
                }
                """;
            return VerifyAsync(source, source);
        }

        [Fact]
        public Task BaseConstructorInvocation_NoFix()
        {
            const string source = """
                class Base
                {
                    public unsafe Base(int value) { }
                }

                class Derived : Base
                {
                    public Derived()
                        {|CS9362:: base(0)|}
                    {
                    }
                }
                """;
            return VerifyAsync(source, source);
        }

        [Fact]
        public Task UnsafeCallTakingLambda_NoFix()
        {
            // The only covering context would also cover the lambda body.
            const string source = """
                using System;

                class C
                {
                    static unsafe int Run(Func<int, int> callback) => callback(0);

                    int M() => {|CS9362:Run(static x => x + 1)|};
                }
                """;
            return VerifyAsync(source, source);
        }

        [Fact]
        public Task ConditionalDirectiveInExpressionBody_NoFix()
        {
            // Converting the body to a block would separate `#if` from its `#else` and `#endif`.
            const string source = """
                class C
                {
                    static unsafe void A() { }

                    void M() =>
                #if DEBUG
                        A();
                #else
                        {|CS9362:A()|};
                #endif
                }
                """;
            return VerifyAsync(source, source);
        }

        [Fact]
        public Task NotUpdatedMemorySafetyRules_NoFix()
        {
            // Legacy rules also report CS9360 under the new language version, but only v2 code gets inner contexts.
            const string source = """
                class C
                {
                    void M(int* p)
                    {
                        {|CS9360:*|}p = 0;
                    }
                }
                """;
            return VerifyAsync(source, source, updatedMemorySafetyRules: false);
        }
    }
}
#endif
