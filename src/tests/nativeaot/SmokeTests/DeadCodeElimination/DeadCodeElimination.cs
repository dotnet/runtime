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
        TestArrayElementTypeOperations.Run();
        TestStaticVirtualMethodOptimizations.Run();

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

#if !DEBUG
        static object s_instance = new object[10];
#endif

        public static void Run()
        {
            Console.WriteLine("Testing instance methods on unallocated types");

#if DEBUG
            NeverAllocatedType instance = null;
            if (instance != null)
                instance.DoSomething();
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

    class TestArrayElementTypeOperations
    {
        public static void Run()
        {
            Console.WriteLine("Testing array element type optimizations");

            // We consider valuetype elements of arrays constructed...
            {
                Array arr = new NeverAllocated1[1];
                ThrowIfNotPresent(typeof(TestArrayElementTypeOperations), nameof(Marker1));

                // The reason they're considered constructed is runtime magic here
                // Make sure that works too.
                object o = arr.GetValue(0);
                if (!o.ToString().Contains(nameof(Marker1)))
                    throw new Exception();
            }

            // ...but not nullable...
            {
                Array arr = new Nullable<NeverAllocated2>[1];
                arr.GetValue(0);
                ThrowIfPresent(typeof(TestArrayElementTypeOperations), nameof(Marker2));
            }


            // ...or reference type element types
            {
                Array arr = new NeverAllocated3[1];
                arr.GetValue(0);
                ThrowIfPresent(typeof(TestArrayElementTypeOperations), nameof(Marker3));
            }
        }

        class Marker1 { }
        struct NeverAllocated1
        {
            public override string ToString() => typeof(Marker1).ToString();
        }

        class Marker2 { }
        struct NeverAllocated2
        {
            public override string ToString() => typeof(Marker2).ToString();
        }

        class Marker3 { }
        class NeverAllocated3
        {
            public override string ToString() => typeof(Marker3).ToString();
        }
    }

    class TestStaticVirtualMethodOptimizations
    {
        interface IFoo
        {
            static abstract Type Frob();
        }

        struct StructWithReachableStaticVirtual : IFoo
        {
            public static Type Frob() => typeof(Marker1);
        }

        class ClassWithUnreachableStaticVirtual : IFoo
        {
            public static Type Frob() => typeof(Marker2);
        }

        class Marker1 { }
        class Marker2 { }

        static Type Call<T>() where T : IFoo => T.Frob();

        public static void Run()
        {
            Console.WriteLine("Testing unused static virtual method optimization");

            // No shared generic code - we should not see IFoo.Frob as "virtually used"
            Call<StructWithReachableStaticVirtual>();

            // Implements IFoo.Frob, but there's no consumption place, so won't be generated.
            new ClassWithUnreachableStaticVirtual().ToString();

            ThrowIfNotPresent(typeof(TestStaticVirtualMethodOptimizations), nameof(Marker1));
            ThrowIfPresent(typeof(TestStaticVirtualMethodOptimizations), nameof(Marker2));
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

    private static void ThrowIfNotPresent(Type testType, string typeName)
    {
        if (!IsTypePresent(testType, typeName))
        {
            throw new Exception(typeName);
        }
    }
}
