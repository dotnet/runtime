// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if UNSAFE_MIGRATION
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public partial class UnsafeContextCodeFixTests
    {
        [Theory]
        [InlineData("if")]
        [InlineData("while")]
        public Task ControlFlowCondition_PrefersBlock(string statement)
        {
            return VerifyAsync($$"""
                using System.Security.Cryptography;

                class C
                {
                    static unsafe int GetRandomBytes(byte* buffer, int length) => 0;

                    void M(byte* buffer, int length)
                    {
                        {{statement}} ({|CS9362:GetRandomBytes(buffer, length)|} != 0)
                            throw new CryptographicException();
                    }
                }
                """, $$"""
                using System.Security.Cryptography;

                class C
                {
                    static unsafe int GetRandomBytes(byte* buffer, int length) => 0;

                    void M(byte* buffer, int length)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            {{statement}} (GetRandomBytes(buffer, length) != 0)
                                throw new CryptographicException();
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task EmbeddedCondition_PrefersBlock()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe bool Check() => true;

                    void M(bool ready)
                    {
                        if (ready)
                            if ({|CS9362:Check()|})
                                return;
                    }
                }
                """, """
                class C
                {
                    static unsafe bool Check() => true;

                    void M(bool ready)
                    {
                        if (ready)
                        {
                            // SAFETY: To be audited
                            unsafe
                            {
                                if (Check())
                                    return;
                            }
                        }
                    }
                }
                """);
        }

        [Theory]
        [InlineData("System.Action callback = static () => { };")]
        [InlineData("void callback() { }")]
        public Task ConditionWithNestedFunction_UsesExpression(string declaration)
        {
            return VerifyAsync($$"""
                class C
                {
                    static unsafe bool Check() => true;

                    void M()
                    {
                        if ({|CS9362:Check()|})
                        {
                            {{declaration}}
                            callback();
                        }
                    }
                }
                """, $$"""
                class C
                {
                    static unsafe bool Check() => true;

                    void M()
                    {
                        if (/* SAFETY: To be audited */ unsafe(Check()))
                        {
                            {{declaration}}
                            callback();
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task HeaderOnlyCondition_WrapsWholeLoop()
        {
            return VerifyAsync("""
                class C
                {
                    static void Use(int x) { }

                    void M(int* p, int a, int b)
                    {
                        while ({|CS9360:*|}p != 0)
                        {
                            Use(a);
                            Use(b);
                        }
                    }
                }
                """, """
                class C
                {
                    static void Use(int x) { }

                    void M(int* p, int a, int b)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            while (*p != 0)
                            {
                                Use(a);
                                Use(b);
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task HeaderAndBody_WrapsWholeLoop()
        {
            return VerifyAsync("""
                class C
                {
                    void M(int* p)
                    {
                        while ({|CS9360:*|}p != 0)
                        {
                            {|CS9360:*|}p = 0;
                        }
                    }
                }
                """, """
                class C
                {
                    void M(int* p)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            while (*p != 0)
                            {
                                *p = 0;
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task BodyOnly_WrapsInsideBody()
        {
            return VerifyAsync("""
                class C
                {
                    void M(int* p, int length)
                    {
                        for (int i = 0; i < length; i++)
                        {
                            p{|CS9360:[|}i] = 0;
                        }
                    }
                }
                """, """
                class C
                {
                    void M(int* p, int length)
                    {
                        for (int i = 0; i < length; i++)
                        {
                            // SAFETY: To be audited
                            unsafe
                            {
                                p[i] = 0;
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task ElseIf_WrapsWholeChain()
        {
            return VerifyAsync("""
                class C
                {
                    static void Use() { }

                    void M(int* p, bool a)
                    {
                        if (a)
                        {
                            Use();
                        }
                        else if ({|CS9360:*|}p == 0)
                        {
                            Use();
                        }
                    }
                }
                """, """
                class C
                {
                    static void Use() { }

                    void M(int* p, bool a)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            if (a)
                            {
                                Use();
                            }
                            else if (*p == 0)
                            {
                                Use();
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task EmbeddedStatement_AddsBraces()
        {
            return VerifyAsync("""
                class C
                {
                    void M(int* p, bool c)
                    {
                        if (c)
                            {|CS9360:*|}p = 0;

                        if (c) {|CS9360:*|}p = 1;
                    }
                }
                """, """
                class C
                {
                    void M(int* p, bool c)
                    {
                        if (c)
                        {
                            // SAFETY: To be audited
                            unsafe
                            {
                                *p = 0;
                            }
                        }

                        if (c)
                        {
                            // SAFETY: To be audited
                            unsafe
                            {
                                *p = 1;
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task StackedFixed_WrapsInnermostBody()
        {
            return VerifyAsync("""
                class C
                {
                    static unsafe int Compare(char* a, char* b) => 0;

                    int M(string a, string b)
                    {
                        fixed (char* pA = a)
                        fixed (char* pB = b)
                        {
                            return {|CS9362:Compare(pA, pB)|};
                        }
                    }
                }
                """, """
                class C
                {
                    static unsafe int Compare(char* a, char* b) => 0;

                    int M(string a, string b)
                    {
                        fixed (char* pA = a)
                        fixed (char* pB = b)
                        {
                            // SAFETY: To be audited
                            unsafe
                            {
                                return Compare(pA, pB);
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task SwitchExpressionArms_OneContext()
        {
            return VerifyAsync("""
                class C
                {
                    int M(int kind, byte* data)
                    {
                        return kind switch
                        {
                            1 => {|CS9360:*|}(sbyte*)data,
                            2 => {|CS9360:*|}(short*)data,
                            _ => 0,
                        };
                    }
                }
                """, """
                class C
                {
                    int M(int kind, byte* data)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            return kind switch
                            {
                                1 => *(sbyte*)data,
                                2 => *(short*)data,
                                _ => 0,
                            };
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task StatementWithLambda_UsesExpressionOutsideLambda()
        {
            return VerifyAsync("""
                using System;

                class C
                {
                    static unsafe object Get(object o) => o;

                    static void Register(Func<object, object> callback, object state) { }

                    void M(object obj)
                    {
                        Register(static s => s, {|CS9362:Get(obj)|});
                    }
                }
                """, """
                using System;

                class C
                {
                    static unsafe object Get(object o) => o;

                    static void Register(Func<object, object> callback, object state) { }

                    void M(object obj)
                    {
                        Register(static s => s, /* SAFETY: To be audited */ unsafe(Get(obj)));
                    }
                }
                """);
        }

        [Fact]
        public Task LambdaBody_PlannedInsideLambda()
        {
            return VerifyAsync("""
                using System;

                class C
                {
                    static unsafe void Act() { }

                    void M()
                    {
                        Action a = () =>
                        {
                            {|CS9362:Act()|};
                        };
                        a();
                    }
                }
                """, """
                using System;

                class C
                {
                    static unsafe void Act() { }

                    void M()
                    {
                        Action a = () =>
                        {
                            // SAFETY: To be audited
                            unsafe
                            {
                                Act();
                            }
                        };
                        a();
                    }
                }
                """);
        }

        [Fact]
        public Task CatchFilter_WrapsTryStatement()
        {
            return VerifyAsync("""
                using System;

                class C
                {
                    static unsafe bool Filter(Exception e) => true;

                    static void Work() { }

                    void M()
                    {
                        try
                        {
                            Work();
                        }
                        catch (Exception e) when ({|CS9362:Filter(e)|})
                        {
                            Work();
                        }
                    }
                }
                """, """
                using System;

                class C
                {
                    static unsafe bool Filter(Exception e) => true;

                    static void Work() { }

                    void M()
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            try
                            {
                                Work();
                            }
                            catch (Exception e) when (Filter(e))
                            {
                                Work();
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task ImplicitEnumeration_WrapsWholeForEach()
        {
            return VerifyAsync("""
                using System.Collections.Generic;

                class Items
                {
                    public unsafe IEnumerator<int> GetEnumerator() => null!;
                }

                class C
                {
                    static void Use(int x) { }

                    void M(Items items)
                    {
                        {|CS9362:foreach|} (int item in items)
                        {
                            Use(item);
                        }
                    }
                }
                """, """
                using System.Collections.Generic;

                class Items
                {
                    public unsafe IEnumerator<int> GetEnumerator() => null!;
                }

                class C
                {
                    static void Use(int x) { }

                    void M(Items items)
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            foreach (int item in items)
                            {
                                Use(item);
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task LabelStaysOutsideBlock()
        {
            return VerifyAsync("""
                class C
                {
                    void M(int* p, int n)
                    {
                    Retry:
                        {|CS9360:*|}p = n;
                        if (n-- > 0)
                            goto Retry;
                    }
                }
                """, """
                class C
                {
                    void M(int* p, int n)
                    {
                    Retry:
                        // SAFETY: To be audited
                        unsafe
                        {
                            *p = n;
                        }
                        if (n-- > 0)
                            goto Retry;
                    }
                }
                """);
        }

        [Fact]
        public Task ConstructorInitializerArgument()
        {
            return VerifyAsync("""
                class Base
                {
                    public Base(int value) { }
                }

                class Derived : Base
                {
                    static unsafe int Get() => 0;

                    public Derived()
                        : base({|CS9362:Get()|})
                    {
                    }
                }
                """, """
                class Base
                {
                    public Base(int value) { }
                }

                class Derived : Base
                {
                    static unsafe int Get() => 0;

                    public Derived()
                        : base(/* SAFETY: To be audited */ unsafe(Get()))
                    {
                    }
                }
                """);
        }

        [Fact]
        public Task ConstructorConstraint()
        {
            return VerifyAsync("""
                class U
                {
                    public unsafe U() { }
                }

                class G<T> where T : new() { }

                class C
                {
                    object M() => new {|CS9376:G<U>|}();
                }
                """, """
                class U
                {
                    public unsafe U() { }
                }

                class G<T> where T : new() { }

                class C
                {
                    object M()
                    {
                        // SAFETY: To be audited
                        unsafe
                        {
                            return new G<U>();
                        }
                    }
                }
                """);
        }

        [Fact]
        public Task AwaitInsideBlock()
        {
            // `await` is allowed in unsafe contexts under the updated rules.
            return VerifyAsync("""
                using System.Threading.Tasks;

                class C
                {
                    static unsafe int Get(int value) => value;

                    async Task<int> M()
                    {
                        int x = {|CS9362:Get(await Task.FromResult(1))|};
                        return x;
                    }
                }
                """, """
                using System.Threading.Tasks;

                class C
                {
                    static unsafe int Get(int value) => value;

                    async Task<int> M()
                    {
                        int x;
                        // SAFETY: To be audited
                        unsafe
                        {
                            x = Get(await Task.FromResult(1));
                        }
                        return x;
                    }
                }
                """);
        }

        [Fact]
        public Task MethodGroupConversion_ConvertsToGetAccessor()
        {
            // `unsafe(M)` is still an error: the conversion to `Action` happens outside the unsafe expression.
            return VerifyAsync("""
                using System;

                class C
                {
                    unsafe void M() { }
                    Action P => {|CS9362:M|};
                }
                """, """
                using System;

                class C
                {
                    unsafe void M() { }
                    Action P
                    {
                        get
                        {
                            // SAFETY: To be audited
                            unsafe
                            {
                                return M;
                            }
                        }
                    }
                }
                """);
        }
    }
}
#endif
