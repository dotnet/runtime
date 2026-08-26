// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

// Task and Task<T> are not sealed, so a covariant override may return a type that
// derives from Task, but is not itself Task or Task<T>. Such an override is not
// task-returning as far as the runtime is concerned, while the method that it
// overrides may well be a runtime async method.
namespace CustomTaskCovariantReturn
{
    public class Program
    {
        internal static string Trace;

        public class MyTask : Task
        {
            public MyTask(Action action) : base(action) => RunSynchronously();
        }

        public class MyTask<T> : Task<T>
        {
            public MyTask(Func<T> func) : base(func) => RunSynchronously();
        }

        public class Base
        {
            public virtual async Task M1()
            {
                Trace += "Base.M1;";
            }

            public virtual async Task<int> M2()
            {
                Trace += "Base.M2;";
                return 1;
            }
        }

        public class Derived : Base
        {
            public override MyTask M1() => new MyTask(() => Trace += "Derived.M1;");

            public override MyTask<int> M2() => new MyTask<int>(() =>
            {
                Trace += "Derived.M2;";
                return 42;
            });
        }

        public class Derived2 : Derived
        {
            public override MyTask M1() => new MyTask(() =>
            {
                Trace += "Derived2.M1;";
                base.M1().GetAwaiter().GetResult();
            });

            public override MyTask<int> M2() => new MyTask<int>(() =>
            {
                Trace += "Derived2.M2;";
                return base.M2().GetAwaiter().GetResult() + 1;
            });
        }

        // awaiting the result of the call directly
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task CallM1(Base b) => await b.M1();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<int> CallM2(Base b) => await b.M2();

        // the same, but the returned task is observed as an object as well,
        // so the call itself cannot be a runtime async call.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<Task> CallM1ViaTask(Base b)
        {
            Task t = b.M1();
            await t;
            return t;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<int> CallM2ViaTask(Base b)
        {
            Task<int> t = b.M2();
            int result = await t;
            Assert.Equal(typeof(MyTask<int>), t.GetType());
            return result;
        }

        [Fact]
        public static void TestCustomTaskOverrideViaTask()
        {
            // check year to not be concerned with devirtualization.
            Base b = DateTime.Now.Year > 0 ? new Derived() : new Base();

            Trace = null;
            Task t = CallM1ViaTask(b).GetAwaiter().GetResult();
            Assert.Equal("Derived.M1;", Trace);
            Assert.IsType<MyTask>(t);

            Trace = null;
            Assert.Equal(42, CallM2ViaTask(b).GetAwaiter().GetResult());
            Assert.Equal("Derived.M2;", Trace);
        }

        [Fact]
        public static void TestCustomTaskOverride()
        {
            Base b = DateTime.Now.Year > 0 ? new Derived() : new Base();

            Trace = null;
            CallM1(b).GetAwaiter().GetResult();
            Assert.Equal("Derived.M1;", Trace);

            Trace = null;
            Assert.Equal(42, CallM2(b).GetAwaiter().GetResult());
            Assert.Equal("Derived.M2;", Trace);
        }

        [Fact]
        public static void TestCustomTaskOverrideOfCustomTaskOverride()
        {
            Base b = DateTime.Now.Year > 0 ? new Derived2() : new Base();

            Trace = null;
            CallM1(b).GetAwaiter().GetResult();
            Assert.Equal("Derived2.M1;Derived.M1;", Trace);

            Trace = null;
            Assert.Equal(43, CallM2(b).GetAwaiter().GetResult());
            Assert.Equal("Derived2.M2;Derived.M2;", Trace);
        }

        [Fact]
        public static void TestCustomTaskOverrideCalledDirectly()
        {
            Derived d = DateTime.Now.Year > 0 ? new Derived2() : new Derived();

            Trace = null;
            d.M1().GetAwaiter().GetResult();
            Assert.Equal("Derived2.M1;Derived.M1;", Trace);

            Trace = null;
            Assert.Equal(43, d.M2().GetAwaiter().GetResult());
            Assert.Equal("Derived2.M2;Derived.M2;", Trace);
        }
    }
}

// The same as above, but the overridden methods are not runtime async.
namespace CustomTaskCovariantReturnWithoutRuntimeAsync
{
    public class Program
    {
        internal static string Trace;

        public class MyTask : Task
        {
            public MyTask(Action action) : base(action) => RunSynchronously();
        }

        public class MyTask<T> : Task<T>
        {
            public MyTask(Func<T> func) : base(func) => RunSynchronously();
        }

        public class Base
        {
            [RuntimeAsyncMethodGeneration(false)]
            public virtual async Task M1()
            {
                Trace += "Base.M1;";
            }

            [RuntimeAsyncMethodGeneration(false)]
            public virtual async Task<int> M2()
            {
                Trace += "Base.M2;";
                return 1;
            }
        }

        public class Derived : Base
        {
            public override MyTask M1() => new MyTask(() => Trace += "Derived.M1;");

            public override MyTask<int> M2() => new MyTask<int>(() =>
            {
                Trace += "Derived.M2;";
                return 42;
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task CallM1(Base b) => await b.M1();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<int> CallM2(Base b) => await b.M2();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task CallM1ViaTask(Base b)
        {
            Task t = b.M1();
            await t;
            Assert.Equal(typeof(MyTask), t.GetType());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<int> CallM2ViaTask(Base b)
        {
            Task<int> t = b.M2();
            int result = await t;
            Assert.Equal(typeof(MyTask<int>), t.GetType());
            return result;
        }

        [Fact]
        public static void TestCustomTaskOverrideViaTaskWithoutRuntimeAsync()
        {
            Base b = DateTime.Now.Year > 0 ? new Derived() : new Base();

            Trace = null;
            CallM1ViaTask(b).GetAwaiter().GetResult();
            Assert.Equal("Derived.M1;", Trace);

            Trace = null;
            Assert.Equal(42, CallM2ViaTask(b).GetAwaiter().GetResult());
            Assert.Equal("Derived.M2;", Trace);
        }

        [Fact]
        public static void TestCustomTaskOverrideWithoutRuntimeAsync()
        {
            Base b = DateTime.Now.Year > 0 ? new Derived() : new Base();

            Trace = null;
            CallM1(b).GetAwaiter().GetResult();
            Assert.Equal("Derived.M1;", Trace);

            Trace = null;
            Assert.Equal(42, CallM2(b).GetAwaiter().GetResult());
            Assert.Equal("Derived.M2;", Trace);
        }
    }
}

// The same as above, but the methods and/or their declaring types are generic.
namespace CustomTaskCovariantReturnGenerics
{
    public class Program
    {
        internal static string Trace;

        public class MyTask : Task
        {
            public MyTask(Action action) : base(action) => RunSynchronously();
        }

        public class MyTask<T> : Task<T>
        {
            public MyTask(Func<T> func) : base(func) => RunSynchronously();
        }

        public struct S : IEquatable<S>
        {
            public S(int num) => this.num = num;

            public int num;

            public bool Equals(S other) => other.num == num;
        }

        // a generic method on a non-generic type - the element type is a method type parameter
        public class Base
        {
            public virtual async Task<T> M1<T>(T t)
            {
                Trace += "Base.M1;";
                return t;
            }

            public virtual async Task M2<T>(T t)
            {
                Trace += "Base.M2;";
            }
        }

        public class Derived : Base
        {
            public override MyTask<T> M1<T>(T t) => new MyTask<T>(() =>
            {
                Trace += "Derived.M1;";
                return t;
            });

            public override MyTask M2<T>(T t) => new MyTask(() => Trace += "Derived.M2;");
        }

        // a generic type - the element type is a type parameter of the declaring type
        public class GBase<T>
        {
            public virtual async Task<T> M1(T t)
            {
                Trace += "GBase.M1;";
                return t;
            }

            public virtual async Task M2(T t)
            {
                Trace += "GBase.M2;";
            }

            public virtual async Task<List<T>> M3(T t)
            {
                Trace += "GBase.M3;";
                return new List<T> { t };
            }

            public virtual async Task<U> M4<U>(U u)
            {
                Trace += "GBase.M4;";
                return u;
            }

            public virtual async Task<T[]> M5(T t)
            {
                Trace += "GBase.M5;";
                return new T[] { t };
            }
        }

        public class GDerived<T> : GBase<T>
        {
            public override MyTask<T> M1(T t) => new MyTask<T>(() =>
            {
                Trace += "GDerived.M1;";
                return t;
            });

            public override MyTask M2(T t) => new MyTask(() => Trace += "GDerived.M2;");

            public override MyTask<List<T>> M3(T t) => new MyTask<List<T>>(() =>
            {
                Trace += "GDerived.M3;";
                return new List<T> { t };
            });

            public override MyTask<U> M4<U>(U u) => new MyTask<U>(() =>
            {
                Trace += "GDerived.M4;";
                return u;
            });

            public override MyTask<T[]> M5(T t) => new MyTask<T[]>(() =>
            {
                Trace += "GDerived.M5;";
                return new T[] { t };
            });
        }

        // the derived type closes the instantiation of the base type
        public class ClosedDerived : GBase<int>
        {
            public override MyTask<int> M1(int t) => new MyTask<int>(() =>
            {
                Trace += "ClosedDerived.M1;";
                return t + 1;
            });
        }

        // the derived type instantiates the base type with a composed type
        public class ListDerived<U> : GBase<List<U>>
        {
            public override MyTask<List<U>> M1(List<U> t) => new MyTask<List<U>>(() =>
            {
                Trace += "ListDerived.M1;";
                return t;
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<T> CallM1<T>(Base b, T t) => await b.M1<T>(t);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task CallM2<T>(Base b, T t) => await b.M2<T>(t);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<T> CallGM1<T>(GBase<T> b, T t) => await b.M1(t);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task CallGM2<T>(GBase<T> b, T t) => await b.M2(t);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<List<T>> CallGM3<T>(GBase<T> b, T t) => await b.M3(t);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<U> CallGM4<T, U>(GBase<T> b, U u) => await b.M4<U>(u);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<T[]> CallGM5<T>(GBase<T> b, T t) => await b.M5(t);

        [Fact]
        public static void TestGenericMethodCovariantOverride()
        {
            // check year to not be concerned with devirtualization.
            Base b = DateTime.Now.Year > 0 ? new Derived() : new Base();

            Trace = null;
            Assert.Equal(42, CallM1(b, 42).GetAwaiter().GetResult());
            Assert.Equal("Derived.M1;", Trace);

            Trace = null;
            Assert.Equal("hi", CallM1(b, "hi").GetAwaiter().GetResult());
            Assert.Equal("Derived.M1;", Trace);

            Trace = null;
            CallM2(b, 42).GetAwaiter().GetResult();
            Assert.Equal("Derived.M2;", Trace);

            Trace = null;
            CallM2(b, "hi").GetAwaiter().GetResult();
            Assert.Equal("Derived.M2;", Trace);
        }

        // non-generic callers, so that the calls are runtime async calls with
        // fully concrete instantiations.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<int> CallGM1Int(GBase<int> b) => await b.M1(42);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<string> CallGM1String(GBase<string> b) => await b.M1("hi");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task CallGM2Int(GBase<int> b) => await b.M2(42);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<List<int>> CallGM3Int(GBase<int> b) => await b.M3(42);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<int> CallM1Int(Base b) => await b.M1<int>(42);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<string> CallM1String(Base b) => await b.M1<string>("hi");

        [Fact]
        public static void TestGenericTypeCovariantOverrideNonGenericCaller()
        {
            GBase<int> bi = DateTime.Now.Year > 0 ? new GDerived<int>() : new GBase<int>();

            Trace = null;
            Assert.Equal(42, CallGM1Int(bi).GetAwaiter().GetResult());
            Assert.Equal("GDerived.M1;", Trace);

            Trace = null;
            CallGM2Int(bi).GetAwaiter().GetResult();
            Assert.Equal("GDerived.M2;", Trace);

            Trace = null;
            Assert.Equal(new List<int> { 42 }, CallGM3Int(bi).GetAwaiter().GetResult());
            Assert.Equal("GDerived.M3;", Trace);

            GBase<string> bs = DateTime.Now.Year > 0 ? new GDerived<string>() : new GBase<string>();

            Trace = null;
            Assert.Equal("hi", CallGM1String(bs).GetAwaiter().GetResult());
            Assert.Equal("GDerived.M1;", Trace);
        }

        [Fact]
        public static void TestGenericMethodCovariantOverrideNonGenericCaller()
        {
            Base b = DateTime.Now.Year > 0 ? new Derived() : new Base();

            Trace = null;
            Assert.Equal(42, CallM1Int(b).GetAwaiter().GetResult());
            Assert.Equal("Derived.M1;", Trace);

            Trace = null;
            Assert.Equal("hi", CallM1String(b).GetAwaiter().GetResult());
            Assert.Equal("Derived.M1;", Trace);
        }

        [Fact]
        public static void TestGenericTypeCovariantOverride()
        {
            GBase<int> bi = DateTime.Now.Year > 0 ? new GDerived<int>() : new GBase<int>();

            Trace = null;
            Assert.Equal(42, CallGM1(bi, 42).GetAwaiter().GetResult());
            Assert.Equal("GDerived.M1;", Trace);

            Trace = null;
            CallGM2(bi, 42).GetAwaiter().GetResult();
            Assert.Equal("GDerived.M2;", Trace);

            Trace = null;
            Assert.Equal(new List<int> { 42 }, CallGM3(bi, 42).GetAwaiter().GetResult());
            Assert.Equal("GDerived.M3;", Trace);

            Trace = null;
            Assert.Equal(new int[] { 42 }, CallGM5(bi, 42).GetAwaiter().GetResult());
            Assert.Equal("GDerived.M5;", Trace);

            Trace = null;
            Assert.Equal("hi", CallGM4(bi, "hi").GetAwaiter().GetResult());
            Assert.Equal("GDerived.M4;", Trace);

            Trace = null;
            Assert.Equal(11, CallGM4(bi, 11).GetAwaiter().GetResult());
            Assert.Equal("GDerived.M4;", Trace);

            GBase<string> bs = DateTime.Now.Year > 0 ? new GDerived<string>() : new GBase<string>();

            Trace = null;
            Assert.Equal("hi", CallGM1(bs, "hi").GetAwaiter().GetResult());
            Assert.Equal("GDerived.M1;", Trace);

            Trace = null;
            CallGM2(bs, "hi").GetAwaiter().GetResult();
            Assert.Equal("GDerived.M2;", Trace);

            Trace = null;
            Assert.Equal(new List<string> { "hi" }, CallGM3(bs, "hi").GetAwaiter().GetResult());
            Assert.Equal("GDerived.M3;", Trace);
        }

        [Fact]
        public static void TestGenericTypeCovariantOverrideWithStruct()
        {
            GBase<S> b = DateTime.Now.Year > 0 ? new GDerived<S>() : new GBase<S>();

            Trace = null;
            Assert.Equal(new S(42), CallGM1(b, new S(42)).GetAwaiter().GetResult());
            Assert.Equal("GDerived.M1;", Trace);

            Trace = null;
            Assert.Equal(new S(42), CallGM4(b, new S(42)).GetAwaiter().GetResult());
            Assert.Equal("GDerived.M4;", Trace);
        }

        [Fact]
        public static void TestClosedGenericBaseCovariantOverride()
        {
            GBase<int> b = DateTime.Now.Year > 0 ? new ClosedDerived() : new GBase<int>();

            Trace = null;
            Assert.Equal(43, CallGM1(b, 42).GetAwaiter().GetResult());
            Assert.Equal("ClosedDerived.M1;", Trace);
        }

        [Fact]
        public static void TestComposedGenericBaseCovariantOverride()
        {
            GBase<List<int>> b = DateTime.Now.Year > 0 ? new ListDerived<int>() : new GBase<List<int>>();

            Trace = null;
            Assert.Equal(new List<int> { 42 }, CallGM1(b, new List<int> { 42 }).GetAwaiter().GetResult());
            Assert.Equal("ListDerived.M1;", Trace);

            GBase<List<string>> bs = DateTime.Now.Year > 0 ? new ListDerived<string>() : new GBase<List<string>>();

            Trace = null;
            Assert.Equal(new List<string> { "hi" }, CallGM1(bs, new List<string> { "hi" }).GetAwaiter().GetResult());
            Assert.Equal("ListDerived.M1;", Trace);
        }
    }
}
