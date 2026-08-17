// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class CovariantReturns
{
    [Fact]
    public static void Test0EntryPoint()
    {
        Test0().Wait();
    }

    [Fact]
    public static void Test1EntryPoint()
    {
        Test1().Wait();
    }

    [Fact]
    public static void Test2EntryPoint()
    {
        Test2().Wait();
    }

    [Fact]
    public static void Test2AEntryPoint()
    {
        Test2A().Wait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Test0()
    {
        Base b = new Base();
        await b.M1();
        Assert.Equal("Base.M1;", b.Trace);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Test1()
    {
        // check year to not be concerned with devirtualization.
        Base b = DateTime.Now.Year > 0 ? new Derived() : new Base();
        await b.M1();
        Assert.Equal("Derived.M1;Base.M1;", b.Trace);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Test2()
    {
        Base b = DateTime.Now.Year > 0 ? new Derived2() : new Base();
        await b.M1();
        Assert.Equal("Derived2.M1;Derived.M1;Base.M1;", b.Trace);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Test2A()
    {
        Base b = DateTime.Now.Year > 0 ? new Derived2A() : new Base();
        await b.M1();
        Assert.Equal("Derived2A.M1;DerivedA.M1;Base.M1;", b.Trace);
    }

    struct S1
    {
        public Guid guid;
        public int num;

        public S1(int num)
        {
            this.guid = Guid.NewGuid();
            this.num = num;
        }
    }

    class Base
    {
        public string Trace;
        public virtual Task M1()
        {
            Trace += "Base.M1;";
            return Task.CompletedTask;
        }
    }

    class Derived : Base
    {
        public override Task<S1> M1()
        {
            Trace += "Derived.M1;";
            base.M1().GetAwaiter().GetResult();
            return Task.FromResult(new S1(42));
        }
    }

    class Derived2 : Derived
    {
        public override async Task<S1> M1()
        {
            Trace += "Derived2.M1;";
            await Task.Delay(1);
            await base.M1();
            return new S1(4242);
        }
    }

    class DerivedA : Base
    {
        public async override Task<S1> M1()
        {
            Trace += "DerivedA.M1;";
            await base.M1();
            return new S1(42);
        }
    }

    class Derived2A : DerivedA
    {
        public override async Task<S1> M1()
        {
            Trace += "Derived2A.M1;";
            await Task.Delay(1);
            await base.M1();
            return new S1(4242);
        }
    }
}

namespace AsyncMicro
{
    public class Program
    {
        internal static string Trace;

        [Fact]
        public static void TestPrRepro()
        {
            Derived2 test = new();
            Test(test).GetAwaiter().GetResult();
            Assert.Equal("Task<int> Derived2.Foo;Task<int> Derived.Foo;", Trace);
        }

        private static async Task Test(Base b)
        {
            await b.Foo();
        }

        public class Base
        {
            public virtual async Task Foo()
            {
                Trace += "Task Base.Foo;";
            }
        }

        public class Derived : Base
        {
            public override async Task<int> Foo()
            {
                Trace += "Task<int> Derived.Foo;";
                return 123;
            }
        }

        public class Derived2 : Derived
        {
            public override async Task<int> Foo()
            {
                Trace += "Task<int> Derived2.Foo;";
                return await base.Foo();
            }
        }
    }
}

namespace CovariantReturnWithoutRuntimeAsync
{
    public class Program
    {
        internal static int Result;

        [Fact]
        public static void TestCovariantReturnWithoutRuntimeAsync()
        {
            Result = 0;
            CallInstance(new Derived()).GetAwaiter().GetResult();
            Assert.Equal(42, Result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task CallInstance(Base b) => await b.InstanceMethod();

        public class Base
        {
            public virtual async Task InstanceMethod()
            {
            }
        }

        public class Derived : Base
        {
            [RuntimeAsyncMethodGenerationAttribute(false)]
            public override async Task<int> InstanceMethod()
            {
                await Task.Yield();
                Result = 42;
                return 42;
            }
        }
    }
}

namespace GenericVirtualMethod
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task CallInstance(Base b) => await b.InstanceMethod<object>();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task CallInstanceValueType(Base b) => await b.InstanceMethod<int>();

        [Fact]
        public static void TestGenericVirtualMethod()
        {
            CallInstance(new Derived()).GetAwaiter().GetResult();
            CallInstanceValueType(new Derived()).GetAwaiter().GetResult();
        }
        public class Base
        {
            public virtual async Task InstanceMethod<T>()
            {
            }
        }
        public class Mid : Base
        {
            public override async Task<int> InstanceMethod<T>()
            {
                throw new Exception();
            }
        }
        public class Derived : Mid
        {
            public override async Task<int> InstanceMethod<T>()
            {
                int result = typeof(T).FullName.Length;
                await Task.Yield();
                return result;
            }
        }
    }
}

namespace AsyncInterfaceGenericMethod
{
    public class Program
    {
        interface IFoo
        {
            Task<int> AsyncInterfaceMethod<T>();
        }

        class Foo : IFoo
        {
            async Task<int> IFoo.AsyncInterfaceMethod<T>()
            {
                await Task.Yield();
                return typeof(T).FullName.Length;
            }
        }

        static async Task Run()
        {
            IFoo f = new Foo();
            int x = await f.AsyncInterfaceMethod<object>();
            Assert.Equal(typeof(object).FullName.Length, x);
        }

        static async Task RunValueType()
        {
            IFoo f = new Foo();
            int x = await f.AsyncInterfaceMethod<int>();
            Assert.Equal(typeof(int).FullName.Length, x);
        }

        [Fact]
        public static void TestAsyncInterfaceGenericMethod()
        {
            Run().GetAwaiter().GetResult();
            RunValueType().GetAwaiter().GetResult();
        }
    }
}


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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124238")]
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
