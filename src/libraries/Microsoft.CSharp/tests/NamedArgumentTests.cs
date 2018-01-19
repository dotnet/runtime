// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class NamedArgumentTests
    {
        public class TypeWithMethods
        {
            public int DoStuff(int x, int y) => x + y;

            public long DoStuff(long z, long a) => z * a;

            public int DoStuff(string s, int i) => i;

            public int DoOtherStuff(int x, int y, int z) => x * y + z;
        }

        [Fact]
        public void OnlyNameFirstArgument()
        {
            CallSite<Func<CallSite, object, object, object, object>> callsite =
                CallSite<Func<CallSite, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object, object> target = callsite.Target;
            object res = target(callsite, new TypeWithMethods(), 9, 14);
            Assert.Equal(23, res);
        }

        [Fact]
        public void OnlyNameFirstArgumentMatchesWrongType()
        {
            CallSite<Func<CallSite, object, object, object, object>> callsite =
                CallSite<Func<CallSite, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "s"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object, object> target = callsite.Target;
            string message = Assert.Throws<RuntimeBinderException>(() => target(callsite, new TypeWithMethods(), 9, 14))
                .Message;

            // All but the (string, int) method should have been excluded from consideration, leaving the binder to
            // complain about it not matching types.
            Assert.Contains(
                "'Microsoft.CSharp.RuntimeBinder.Tests.NamedArgumentTests.TypeWithMethods.DoStuff(string, int)'",
                message);
        }

        [Fact]
        public void ResolveThroughNameOnlyFirstArgument()
        {
            CallSite<Func<CallSite, object, object, object, object>> callsite =
                CallSite<Func<CallSite, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "z"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object, object> target = callsite.Target;
            object res = target(callsite, new TypeWithMethods(), 9, 14);
            Assert.Equal(126L, res);
        }

        [Fact]
        public void NonExistentNameOnlyFirstArgument()
        {
            CallSite<Func<CallSite, object, object, object, object>> callsite =
                CallSite<Func<CallSite, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "nada"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object, object> target = callsite.Target;
            string message = Assert.Throws<RuntimeBinderException>(() => target(callsite, new TypeWithMethods(), 9, 14))
                .Message;

            //  The best overload for 'DoStuff' does not have a parameter named 'nada'
            Assert.Contains("'DoStuff'", message);
            Assert.Contains("'nada'", message);
        }

        [Fact]
        public void NameOnlyFirstArgumentWrongPlace()
        {
            CallSite<Func<CallSite, object, object, object, object>> callsite =
                CallSite<Func<CallSite, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "y"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object, object> target = callsite.Target;
            string message = Assert.Throws<RuntimeBinderException>(() => target(callsite, new TypeWithMethods(), 9, 14))
                .Message;

            //  Named argument 'y' is used out-of-position but is followed by an unnamed argument
            Assert.Contains("'y'", message);
        }

        [Fact]
        public void NameOnlyFirstAndSecondWithSecondArgumentWrongPlace()
        {
            CallSite<Func<CallSite, object, object, object, object, object>> callsite =
                CallSite<Func<CallSite, object, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoOtherStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "z"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object, object, object> target = callsite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callsite, new TypeWithMethods(), 9, 14, 13))
                .Message;

            //  Named argument 'z' is used out-of-position but is followed by an unnamed argument
            Assert.Contains("'z'", message);
        }

        [Fact]
        public void NameOnlyFirstAndSecondWithSecondArgumentNotFound()
        {
            CallSite<Func<CallSite, object, object, object, object, object>> callsite =
                CallSite<Func<CallSite, object, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoOtherStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "nada"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object, object, object> target = callsite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callsite, new TypeWithMethods(), 9, 14, 12))
                .Message;

            //  The best overload for 'DoMoreStuff' does not have a parameter named 'nada'
            Assert.Contains("'DoOtherStuff'", message);
            Assert.Contains("'nada'", message);
        }

        [Fact]
        public void NameOnlySecondWithSecondArgumentWrongPlace()
        {
            CallSite<Func<CallSite, object, object, object, object, object>> callsite =
                CallSite<Func<CallSite, object, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoOtherStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "z"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object, object, object> target = callsite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callsite, new TypeWithMethods(), 9, 14, 13))
                .Message;

            //  Named argument 'z' is used out-of-position but is followed by an unnamed argument
            Assert.Contains("'z'", message);
        }

        [Fact]
        public void NameOnlySecondWithSecondArgumentNotFound()
        {
            CallSite<Func<CallSite, object, object, object, object, object>> callsite =
                CallSite<Func<CallSite, object, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoOtherStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "nada"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            Func<CallSite, object, object, object, object, object> target = callsite.Target;
            string message = Assert
                .Throws<RuntimeBinderException>(() => target(callsite, new TypeWithMethods(), 9, 14, 12))
                .Message;

            //  The best overload for 'DoMoreStuff' does not have a parameter named 'nada'
            Assert.Contains("'DoOtherStuff'", message);
            Assert.Contains("'nada'", message);
        }

        class BaseClass
        {
            public virtual int Adder(int x, int y) => x + y;
        }

        class Derived : BaseClass
        {
            public override int Adder(int a, int b) => base.Adder(a, b);
        }

        [Fact]
        public void OverrideChangesName()
        {
            // Static compilation behavior is to use the name of the type of the variable the object is accessed through.
            // Dynamic behavior matches that when the argument is UseCompileTimeType
            // Otherwise it depends on the actual type of the object.

            // Defined in terms of base class. Using "x"
            CallSite<Func<CallSite, BaseClass, int, int, object>> callSite =
                CallSite<Func<CallSite, BaseClass, int, int, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(BaseClass.Adder), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "x"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        }));
            Assert.Equal(9, callSite.Target(callSite, new Derived(), 4, 5));
            Assert.Equal(9, callSite.Target(callSite, new BaseClass(), 4, 5));

            // Defined in terms of base class. Using "a"
            callSite = CallSite<Func<CallSite, BaseClass, int, int, object>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.None, nameof(BaseClass.Adder), Type.EmptyTypes, GetType(),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "a"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                    }));
            string message = Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, new Derived(), 4, 5))
                .Message;

            //  The best overload for 'Adder' does not have a parameter named 'a'
            Assert.Contains("'Adder'", message);
            Assert.Contains("'a'", message);
            Assert.Equal(
                message,
                Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, new BaseClass(), 4, 5)).Message);

            // Defined in terms of Derived, using "a"
            CallSite<Func<CallSite, Derived, int, int, object>> callSiteDerived =
                CallSite<Func<CallSite, Derived, int, int, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(BaseClass.Adder), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "a"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        }));
            Assert.Equal(9, callSiteDerived.Target(callSiteDerived, new Derived(), 4, 5));

            // Defined in terms of Derived, using "x"
            callSiteDerived = CallSite<Func<CallSite, Derived, int, int, object>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.None, nameof(BaseClass.Adder), Type.EmptyTypes, GetType(),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "x"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                    }));
            message = Assert
                .Throws<RuntimeBinderException>(() => callSiteDerived.Target(callSiteDerived, new Derived(), 4, 5))
                .Message;

            //  The best overload for 'Adder' does not have a parameter named 'x'
            Assert.Contains("'Adder'", message);
            Assert.Contains("'x'", message);

            // Using runtime types, and "a"
            CallSite<Func<CallSite, object, object, object, object>> callSiteRuntime =
                CallSite<Func<CallSite, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(BaseClass.Adder), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "a"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        }));

            Assert.Equal(9, callSiteRuntime.Target(callSiteRuntime, new Derived(), 4, 5));
            message = Assert
                .Throws<RuntimeBinderException>(() => callSiteRuntime.Target(callSiteRuntime, new BaseClass(), 4, 5))
                .Message;

            //  The best overload for 'Adder' does not have a parameter named 'a'
            Assert.Contains("'Adder'", message);
            Assert.Contains("'a'", message);

            // Using runtime types, and "x"
            callSiteRuntime = CallSite<Func<CallSite, object, object, object, object>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.None, nameof(BaseClass.Adder), Type.EmptyTypes, GetType(),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                    }));

            Assert.Equal(9, callSiteRuntime.Target(callSiteRuntime, new BaseClass(), 4, 5));
            message = Assert
                .Throws<RuntimeBinderException>(() => callSiteRuntime.Target(callSiteRuntime, new Derived(), 4, 5))
                .Message;

            //  The best overload for 'Adder' does not have a parameter named 'x'
            Assert.Contains("'Adder'", message);
            Assert.Contains("'x'", message);
        }

        // Tests based on the static tests for Roslyn.
        class C1
        {
            static void M(int a, int b)
            {
                Console.Write($"First {a} {b}. ");
            }

            static void M(long b, long a)
            {
                Console.Write($"Second {b} {a}. ");
            }
        }

        [Fact]
        public void TestSimple()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, int, int>> callsite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C1),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "a"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));
            callsite.Target(callsite, typeof(C1), 1, 2);
            callsite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C1),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "a")
                    }));
            callsite.Target(callsite, typeof(C1), 3, 4);
            Assert.Equal("First 1 2. Second 3 4. ", console.ToString());
        }

        class C2
        {
            C2(int a, int b)
            {
                Console.Write($"{a} {b}.");
            }
        }

        [Fact]
        public void TestSimpleConstructor()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Func<CallSite, Type, int, int, C2>> callsite = CallSite<Func<CallSite, Type, int, int, C2>>.Create(
                Binder.InvokeConstructor(
                    CSharpBinderFlags.None, typeof(C2),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.NamedArgument, "a"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));
            callsite.Target(callsite, typeof(C2), 1, 2);
            Assert.Equal("1 2.", console.ToString());
        }

        class C3
        {
            public delegate void MyDelegate(int a, int b);

            public MyDelegate e;

            static void M(int a, int b)
            {
                Console.Write($"{a} {b}. ");
            }

            public C3()
            {
                e += M;
            }
        }

        [Fact]
        public void TestSimpleDelegate()
        {
            // This test differs from its static equivalent considerably, as some event-related matters aren't as directly
            // applicable. We can use it for checking both direct and deferred delegate invocation.

            StringWriter console = new StringWriter();
            Console.SetOut(console);
            C3 targetObject = new C3();
            CallSite<Func<CallSite, object, object>> getCallSite = CallSite<Func<CallSite, object, object>>.Create(
                Binder.GetMember(
                    CSharpBinderFlags.None, "e", typeof(C3),
                    new[] {CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)}));
            object dele = getCallSite.Target(getCallSite, targetObject);
            CallSite<Action<CallSite, object, int, int>> invokeCallSite =
                CallSite<Action<CallSite, object, int, int>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "Invoke", Type.EmptyTypes, typeof(C3),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "a"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        }));
            invokeCallSite.Target(invokeCallSite, dele, 1, 2);
            invokeCallSite = CallSite<Action<CallSite, object, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "e", Type.EmptyTypes, typeof(C3),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "a"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                    }));
            invokeCallSite.Target(invokeCallSite, targetObject, 1, 2);
            Assert.Equal("1 2. 1 2. ", console.ToString());
        }

        class C4
        {
            int this[int a, int b]
            {
                get
                {
                    Console.Write($"Get {a} {b}. ");
                    return 0;
                }
                set { Console.Write($"Set {a} {b} {value}."); }
            }
        }

        [Fact]
        public void TestSimpleIndexer()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            C4 targetObject = new C4();
            CallSite<Func<CallSite, object, int, int, object>> getCallSite =
                CallSite<Func<CallSite, object, int, int, object>>.Create(
                    Binder.GetIndex(
                        CSharpBinderFlags.None, typeof(C4),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.NamedArgument,
                                "a"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        }));
            Assert.Equal(0, getCallSite.Target(getCallSite, targetObject, 1, 2));
            CallSite<Func<CallSite, object, int, int, int, object>> setCallSite =
                CallSite<Func<CallSite, object, int, int, int, object>>.Create(
                    Binder.SetIndex(
                        CSharpBinderFlags.None, typeof(C4),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.NamedArgument,
                                "a"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        }));
            Assert.Equal(5, setCallSite.Target(setCallSite, targetObject, 3, 4, 5));
            Assert.Equal("Get 1 2. Set 3 4 5.", console.ToString());
        }


        class C5
        {
            public C5()
            {
            }

            int this[int a, int b]
            {
                get { throw null; }
            }

            C5(int a, int b)
            {
            }

            // In the static test this is a local function on Main, but to test that without hunting
            // for the name (likely C.<Main>g__local|3_0, but that's implementation-dependent) to
            // push in with reflection we need the support from the compiler that we have yet to make
            // possible. Cut out the chicken-egg problem and just test on a non-local equivalent.
            void Method(int a, int b)
            {
            }
        }

        [Fact]
        public void TestSimpleError()
        {
            CallSite<Func<CallSite, Type, int, int, C5>> ctorCallsite =
                CallSite<Func<CallSite, Type, int, int, C5>>.Create(
                    Binder.InvokeConstructor(
                        CSharpBinderFlags.None, typeof(C5),
                        new[]
                        {
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType,
                                null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.NamedArgument,
                                "b"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                        }));
            string message = Assert
                .Throws<RuntimeBinderException>(() => ctorCallsite.Target(ctorCallsite, typeof(C5), 1, 2))
                .Message;

            // Named argument 'b' is used out-of-position but is followed by an unnamed argument
            Assert.Contains("'b'", message);
            CallSite<Func<CallSite, object, int, int, object>> getCallSite =
                CallSite<Func<CallSite, object, int, int, object>>.Create(
                    Binder.GetIndex(
                        CSharpBinderFlags.None, typeof(C5),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.NamedArgument,
                                "b"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        }));
            message = Assert.Throws<RuntimeBinderException>(() => getCallSite.Target(getCallSite, new C5(), 1, 2))
                .Message;
            Assert.Contains("'b'", message);
            CallSite<Func<CallSite, object, int, int>> invokeCallSite =
                CallSite<Func<CallSite, object, int, int>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, nameof(TypeWithMethods.DoStuff), Type.EmptyTypes, GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "b"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            message = Assert.Throws<RuntimeBinderException>(() => getCallSite.Target(invokeCallSite, new C5(), 1, 2))
                .Message;
            Assert.Contains("'b'", message);
        }

        class C6
        {
            static void M(int first, int other)
            {
                System.Console.Write($"{first} {other}");
            }
        }

        [Fact]
        public void TestPositionalUnaffected()
        {
            CallSite<Action<CallSite, Type, int, int>> invokeCallSite =
                CallSite<Action<CallSite, Type, int, int>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, "M", Type.EmptyTypes, typeof(C6),
                        new[]
                        {
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "first")
                        }));
            string message = Assert
                .Throws<RuntimeBinderException>(() => invokeCallSite.Target(invokeCallSite, typeof(C6), 1, 2))
                .Message;

            // Named argument 'first' specifies a parameter for which a positional argument has already been given
            Assert.Contains("'first'", message);
        }

        class C7
        {
            static void M<T1, T2>(T1 a, T2 b)
            {
                System.Console.Write($"{a} {b}.");
            }
        }

        [Fact]
        public void TestGenericInference()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);

            // Test with types defined fully.
            CallSite<Action<CallSite, Type, int, string>> compileTimeTypeCallSite =
                CallSite<Action<CallSite, Type, int, string>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", new[] {typeof(int), typeof(string)}, typeof(C7),
                        new[]
                        {
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "a"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                        }));
            compileTimeTypeCallSite.Target(compileTimeTypeCallSite, typeof(C7), 1, "hi");
            Assert.Equal("1 hi.", console.ToString());

            // Test with types defined fully in delegate but generic types deduced.
            console.GetStringBuilder().Length = 0;
            compileTimeTypeCallSite = CallSite<Action<CallSite, Type, int, string>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C7),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "a"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));
            compileTimeTypeCallSite.Target(compileTimeTypeCallSite, typeof(C7), 1, "hi");
            Assert.Equal("1 hi.", console.ToString());

            // Test with all types deduced by the dynamic binder.
            console.GetStringBuilder().Length = 0;
            CallSite<Action<CallSite, Type, object, object>> runtimeTypeCallSite =
                CallSite<Action<CallSite, Type, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C7),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "a"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                        }));
            runtimeTypeCallSite.Target(runtimeTypeCallSite, typeof(C7), 1, "hi");
            Assert.Equal("1 hi.", console.ToString());
        }

        class C8
        {
            static void M(int a, int b, int c = 1)
            {
                System.Console.Write($"M {a} {b}");
            }
        }

        [Fact]
        public void TestPositionalUnaffected2()
        {
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C8),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "c"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));
            string message = Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, typeof(C8), 1, 2))
                .Message;

            //  Named argument 'c' is used out-of-position but is followed by an unnamed argument
            Assert.Contains("'c'", message);
        }

        class C9
        {
            static void M(params int[] x)
            {
            }
        }

        [Fact]
        public void TestNamedParams()
        {
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C9),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "x"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));

            string message = Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, typeof(C9), 1, 2))
                .Message;

            // No overload for method 'M' takes 2 arguments
            Assert.Contains("'M'", message);
            Assert.Contains(" 2 ", message);
        }

        [Fact]
        public void TestNamedParams2()
        {
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C9),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "x")
                    }));

            string message = Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, typeof(C9), 1, 2))
                .Message;

            // Named argument 'x' specifies a parameter for which a positional argument has already been given
            Assert.Contains("'x'", message);
        }

        class C10
        {
            static void M(int x, params string[] y)
            {
                System.Console.Write($"{x} {string.Join(",", y)}. ");
            }
        }

        [Fact]
        public void TestNamedParamsVariousForms()
        {
            // This extends the static test in also calling with no arguments for the params section.
            StringWriter console = new StringWriter();
            Console.SetOut(console);

            CallSite<Action<CallSite, Type, int, string>> callSite0 =
                CallSite<Action<CallSite, Type, int, string>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C10),
                        new[]
                        {
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "x"),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "y")
                        }));
            callSite0.Target(callSite0, typeof(C10), 1, "2");

            callSite0 = CallSite<Action<CallSite, Type, int, string>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C10),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "x"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));
            callSite0.Target(callSite0, typeof(C10), 2, "3");

            CallSite<Action<CallSite, Type, int, string[]>> callSite1 =
                CallSite<Action<CallSite, Type, int, string[]>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C10),
                        new[]
                        {
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "x"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                        }));
            callSite1.Target(callSite1, typeof(C10), 3, new[] {"4", "5"});

            CallSite<Action<CallSite, Type, int>> callSite2 = CallSite<Action<CallSite, Type, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C10),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "x")
                    }));
            callSite2.Target(callSite2, typeof(C10), 4);

            Assert.Equal("1 2. 2 3. 3 4,5. 4 . ", console.ToString());
        }

        [Fact]
        public void TestNamedParamsVariousFormsDynamicallyDeducedTypes()
        {
            // This extends the static test in also calling with no arguments for the params section.
            StringWriter console = new StringWriter();
            Console.SetOut(console);

            CallSite<Action<CallSite, Type, object, object>> callSite0 =
                CallSite<Action<CallSite, Type, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C10),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "y")
                        }));
            callSite0.Target(callSite0, typeof(C10), 1, "2");

            callSite0 = CallSite<Action<CallSite, Type, object, object>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C10),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                    }));
            callSite0.Target(callSite0, typeof(C10), 2, "3");

            CallSite<Action<CallSite, Type, object, object>> callSite1 =
                CallSite<Action<CallSite, Type, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C10),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            callSite1.Target(callSite1, typeof(C10), 3, new[] {"4", "5"});

            CallSite<Action<CallSite, Type, object>> callSite2 = CallSite<Action<CallSite, Type, object>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C10),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "x")
                    }));
            callSite2.Target(callSite2, typeof(C10), 4);

            Assert.Equal("1 2. 2 3. 3 4,5. 4 . ", console.ToString());
        }

        [Fact]
        public void TestTwiceNamedParams()
        {
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C9),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "x"),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "x")
                    }));

            string message = Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, typeof(C9), 1, 2))
                .Message;

            // Named argument 'x' cannot be specified multiple times
            Assert.Contains("'x'", message);
        }

        class C11
        {
            static void M(int x, params int[] y)
            {
            }
        }

        [Fact]
        public void TestNamedParams3()
        {
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C11),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "y"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));

            string message = Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, typeof(C11), 1, 2))
                .Message;

            // Named argument 'y' is used out-of-position but is followed by an unnamed argument
            Assert.Contains("'y'", message);
        }

        [Fact]
        public void TestNamedParams4()
        {
            CallSite<Action<CallSite, Type, int, int, int>> callSite =
                CallSite<Action<CallSite, Type, int, int, int>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C11),
                        new[]
                        {
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "x"),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "y"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                        }));

            string message = Assert
                .Throws<RuntimeBinderException>(() => callSite.Target(callSite, typeof(C11), 1, 2, 3))
                .Message;

            // No overload for method 'M' takes 3 arguments
            Assert.Contains("'M'", message);
            Assert.Contains(" 3 ", message);
        }

        class C12
        {
            static void M(int x, params int[] y)
            {
                System.Console.Write($"x={x} y[0]={y[0]} y.Length={y.Length}");
            }
        }

        [Fact]
        public void TestNamedParams5()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C12),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "y"),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "x")
                    }));
            callSite.Target(callSite, typeof(C12), 1, 2);
            Assert.Equal("x=2 y[0]=1 y.Length=1", console.ToString());
        }

        class C13
        {
            static void M(int a = 1, int b = 2, int c = 3)
            {
            }
        }

        [Fact]
        public void TestBadNonTrailing()
        {
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C13),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "c"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));
            string message = Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, typeof(C13), 3, 2)).Message;
            //Named argument 'c' is used out-of-position but is followed by an unnamed argument
            Assert.Contains(" 'c' ", message);
        }

        class C14
        {
            static void M(int a = 1, int b = 2, int c = 3)
            {
            }
            static void M(long c = 1, long b = 2)
            {
                System.Console.Write($"Second {c} {b}. ");
            }
        }

        [Fact]
        public void TestPickGoodOverload()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C14),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "c"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));
            callSite.Target(callSite, typeof(C14), 3, 2);
            Assert.Equal("Second 3 2. ", console.ToString());
        }

        [Fact]
        public void TestPickGoodOverloadDynamicallyTyped()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, object, object>> callSite =
                CallSite<Action<CallSite, Type, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C14),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "c"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            callSite.Target(callSite, typeof(C14), 3, 2);
            Assert.Equal("Second 3 2. ", console.ToString());
        }

        class C15
        {
            static void M(long a = 1, long b = 2, long c = 3)
            {
            }
            static void M(int c = 1, int b = 2)
            {
                System.Console.Write($"Second {c} {b}.");
            }
        }

        [Fact]
        public void TestPickGoodOverload2()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C15),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "c"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));
            callSite.Target(callSite, typeof(C15), 3, 2);
            Assert.Equal("Second 3 2.", console.ToString());
        }

        [Fact]
        public void TestPickGoodOverload2DynamicallyTyped()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, object, object>> callSite =
                CallSite<Action<CallSite, Type, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C15),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "c"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            callSite.Target(callSite, typeof(C15), 3, 2);
            Assert.Equal("Second 3 2.", console.ToString());
        }

        class C16
        {
            static void M(int a, int b, int c = 42)
            {
                System.Console.Write(c);
            }
        }

        [Fact]
        public void TestOptionalValues()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, int, int>> callSite = CallSite<Action<CallSite, Type, int, int>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C16),
                    new[]
                    {
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType, null),
                        CSharpArgumentInfo.Create(
                            CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType, "a"),
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                    }));
            callSite.Target(callSite, typeof(C16), 1, 2);
            Assert.Equal("42", console.ToString());
        }

        [Fact]
        public void TestOptionalValuesRunTimeTypes()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, object, object>> callSite =
                CallSite<Action<CallSite, Type, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C16),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "a"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            callSite.Target(callSite, typeof(C16), 1, 2);
            Assert.Equal("42", console.ToString());
        }

        class C17
        {
            static void M(int a, int b, params int[] c)
            {
            }
        }

        [Fact]
        public void TestParams()
        {
            CallSite<Action<CallSite, Type, int, int, int>> callSite =
                CallSite<Action<CallSite, Type, int, int, int>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C17),
                        new[]
                        {
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "b"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                        }));
            string message = Assert
                .Throws<RuntimeBinderException>(() => callSite.Target(callSite, typeof(C17), 2, 3, 4))
                .Message;
            // Named argument 'b' is used out-of-position but is followed by an unnamed argument
            Assert.Contains(" 'b' ", message);
        }

        [Fact]
        public void TestParamsRuntimeTypes()
        {
            CallSite<Action<CallSite, Type, object, object, object>> callSite =
                CallSite<Action<CallSite, Type, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C17),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "b"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            string message = Assert
                .Throws<RuntimeBinderException>(() => callSite.Target(callSite, typeof(C17), 2, 3, 4))
                .Message;

            // Named argument 'b' is used out-of-position but is followed by an unnamed argument
            Assert.Contains(" 'b' ", message);
        }

        class C18
        {
            static void M(int a, int b, params int[] c)
            {
                System.Console.Write($"{a} {b} {c[0]} {c[1]} Length:{c.Length}");
            }
        }

        [Fact]
        public void TestParams2()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, int, int, int, int>> callSite =
                CallSite<Action<CallSite, Type, int, int, int, int>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C18),
                        new[]
                        {
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.IsStaticType | CSharpArgumentInfoFlags.UseCompileTimeType,
                                null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(
                                CSharpArgumentInfoFlags.NamedArgument | CSharpArgumentInfoFlags.UseCompileTimeType,
                                "b"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null)
                        }));
            callSite.Target(callSite, typeof(C18), 1, 2, 3, 4);
            Assert.Equal("1 2 3 4 Length:2", console.ToString());
        }

        [Fact]
        public void TestParams2RuntimeTypes()
        {
            StringWriter console = new StringWriter();
            Console.SetOut(console);
            CallSite<Action<CallSite, Type, object, object, object, object>> callSite =
                CallSite<Action<CallSite, Type, object, object, object, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded, "M", Type.EmptyTypes, typeof(C18),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.IsStaticType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.NamedArgument, "b"),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            callSite.Target(callSite, typeof(C18), 1, 2, 3, 4);
            Assert.Equal("1 2 3 4 Length:2", console.ToString());
        }
    }
}
