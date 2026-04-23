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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/127197", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotNativeAot))]
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
