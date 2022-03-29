// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Program
{
    static int Main()
    {
        SanityTest.Run();
        TestInstanceMethodOptimization.Run();
        TestAbstractTypeVirtualsOptimization.Run();
        TestAbstractTypeNeverDerivedVirtualsOptimization.Run();

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

    class TestAbstractTypeVirtualsOptimization
    {
        class UnreferencedType1 { }
        class UnreferencedType2 { }
        class ReferencedType1 { }

        abstract class Base
        {
            public virtual Type GetTheType() => typeof(UnreferencedType1);
            public virtual Type GetOtherType() => typeof(ReferencedType1);
        }

        abstract class Mid : Base
        {
            public override Type GetTheType() => typeof(UnreferencedType2);
        }

        class Derived : Mid
        {
            public override Type GetTheType() => null;
        }

        static Base s_instance = Activator.CreateInstance<Derived>();

        public static void Run()
        {
            Console.WriteLine("Testing virtual methods on abstract types");

            s_instance.GetTheType();
            s_instance.GetOtherType();

            ThrowIfPresent(typeof(TestAbstractTypeVirtualsOptimization), nameof(UnreferencedType1));
            ThrowIfPresent(typeof(TestAbstractTypeVirtualsOptimization), nameof(UnreferencedType2));
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

            ThrowIfPresent(typeof(TestAbstractTypeNeverDerivedVirtualsOptimization), nameof(UnreferencedType1));
        }
    }

    private static bool IsTypePresent(Type testType, string typeName) => testType.GetNestedType(typeName, BindingFlags.NonPublic | BindingFlags.Public) != null;

    private static void ThrowIfPresent(Type testType, string typeName)
    {
        if (IsTypePresent(testType, typeName))
        {
            throw new Exception(typeName);
        }
    }
}
