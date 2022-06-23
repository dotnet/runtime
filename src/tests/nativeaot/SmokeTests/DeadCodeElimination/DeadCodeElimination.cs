// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Program
{
    static int Main()
    {
        SanityTest.Run();
        TestInstanceMethodOptimization.Run();
        TestAbstractTypeNeverDerivedVirtualsOptimization.Run();
        TestAbstractNeverDerivedWithDevirtualizedCall.Run();
        TestAbstractDerivedByUnrelatedTypeWithDevirtualizedCall.Run();
        TestUnusedDefaultInterfaceMethod.Run();

        return 100;
    }

    class SanityTest
    {
        class PresentType { }

        class NotPresentType { }

        public static void Run()
        {
            typeof(PresentType).ToString();

            if (!IsTypePresent(typeof(SanityTest), nameof(PresentType)))
                throw new Exception();

            ThrowIfPresent(typeof(SanityTest), nameof(NotPresentType));
        }
    }

    class TestInstanceMethodOptimization
    {
        class UnreferencedType { }

        class NeverAllocatedType
        {
            public Type DoSomething() => typeof(UnreferencedType);
        }

#if DEBUG
        static NeverAllocatedType s_instance = null;
#else
        static object s_instance = new object[10];
#endif

        public static void Run()
        {
            Console.WriteLine("Testing instance methods on unallocated types");

#if DEBUG
            if (s_instance != null)
                s_instance.DoSomething();
#else
            // In release builds additionally test that the "is" check didn't introduce the constructed type
            if (s_instance is NeverAllocatedType never)
                never.DoSomething();
#endif

            ThrowIfPresent(typeof(TestInstanceMethodOptimization), nameof(UnreferencedType));
        }
    }

    class TestAbstractTypeNeverDerivedVirtualsOptimization
    {
        class UnreferencedType1
        {
        }

        class TheBase
        {
            public virtual object Something() => new object();
        }

        abstract class AbstractDerived : TheBase
        {
            // We expect "Something" to be generated as a throwing helper.
            [MethodImpl(MethodImplOptions.NoInlining)]
            public sealed override object Something() => new UnreferencedType1();
            // We expect "callvirt Something" to get devirtualized here.
            [MethodImpl(MethodImplOptions.NoInlining)]
            public object TrySomething() => Something();
        }

        abstract class AbstractDerivedAgain : AbstractDerived
        {
        }

        static TheBase s_b = new TheBase();
        static AbstractDerived s_d = null;

        public static void Run()
        {
            Console.WriteLine("Testing virtual methods on never derived abstract types");

            // Make sure Something is seen virtually used.
            s_b.Something();

            // Force a constructed MethodTable for AbstractDerived and AbstractDerivedAgain into closure
            typeof(AbstractDerivedAgain).ToString();

            if (s_d != null)
            {
                s_d.TrySomething();
            }

            // This optimization got disabled, but if it ever gets re-enabled, this test
            // will ensure we don't reintroduce the old bugs (this was a compiler crash).
            //ThrowIfPresent(typeof(TestAbstractTypeNeverDerivedVirtualsOptimization), nameof(UnreferencedType1));
        }
    }

    class TestAbstractNeverDerivedWithDevirtualizedCall
    {
        static void DoIt(Derived d) => d?.DoSomething();

        abstract class Base
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public virtual void DoSomething() => new UnreferencedType1().ToString();
        }

        sealed class Derived : Base { }

        class UnreferencedType1 { }

        public static void Run()
        {
            Console.WriteLine("Testing abstract classes that might have methods reachable through devirtualization");

            // Force a vtable for Base
            typeof(Base).ToString();

            // Do a devirtualizable virtual call to something that was never allocated
            // and uses a virtual method implementation from Base.
            DoIt(null);

            // This optimization got disabled, but if it ever gets re-enabled, this test
            // will ensure we don't reintroduce the old bugs (this was a compiler crash).
            //ThrowIfPresent(typeof(TestAbstractNeverDerivedWithDevirtualizedCall), nameof(UnreferencedType1));
        }
    }

    class TestAbstractDerivedByUnrelatedTypeWithDevirtualizedCall
    {
        static void DoIt(Derived1 d) => d?.DoSomething();

        abstract class Base
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public virtual void DoSomething() => new UnreferencedType1().ToString();
        }

        sealed class Derived1 : Base { }

        sealed class Derived2 : Base
        {
            public override void DoSomething() { }
        }

        class UnreferencedType1 { }

        public static void Run()
        {
            Console.WriteLine("Testing more abstract classes that might have methods reachable through devirtualization");

            // Force a vtable for Base
            typeof(Base).ToString();

            // Do a devirtualizable virtual call to something that was never allocated
            // and uses a virtual method implementation from Base.
            DoIt(null);

            new Derived2().DoSomething();

            // This optimization got disabled, but if it ever gets re-enabled, this test
            // will ensure we don't reintroduce the old bugs (this was a compiler crash).
            //ThrowIfPresent(typeof(TestAbstractDerivedByUnrelatedTypeWithDevirtualizedCall), nameof(UnreferencedType1));
        }
    }

    class TestUnusedDefaultInterfaceMethod
    {
        interface IFoo<T>
        {
            void DoSomething();
        }

        interface IBar<T> : IFoo<T>
        {
            void IFoo<T>.DoSomething()
            {
                Activator.CreateInstance(typeof(NeverReferenced));
            }
        }

        class NeverReferenced { }

        class SomeInstance : IBar<object>
        {
            void IFoo<object>.DoSomething() { }
        }

        static IFoo<object> s_instance = new SomeInstance();

        public static void Run()
        {
            s_instance.DoSomething();

            ThrowIfPresent(typeof(TestUnusedDefaultInterfaceMethod), nameof(NeverReferenced));
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    private static bool IsTypePresent(Type testType, string typeName) => testType.GetNestedType(typeName, BindingFlags.NonPublic | BindingFlags.Public) != null;

    private static void ThrowIfPresent(Type testType, string typeName)
    {
        if (IsTypePresent(testType, typeName))
        {
            throw new Exception(typeName);
        }
    }
}
