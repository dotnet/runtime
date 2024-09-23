// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace CuriouslyRecurringPatternThroughInterface
{
    interface IGeneric<T_IGeneric>
    {
    }
    interface ICuriouslyRecurring<T_ICuriouslyRecurring> : IGeneric<CuriouslyRecurringThroughInterface<T_ICuriouslyRecurring>>
    {
    }
    class CuriouslyRecurringThroughInterface<T_CuriouslyRecurringThroughInterface> : ICuriouslyRecurring<T_CuriouslyRecurringThroughInterface>
    {
    }

    class BaseClassWhereAImplementsB<A,B> where A : B {}
    interface ICuriouslyRecurring2<T_ICuriouslyRecurring> : IGeneric<DerivedCuriouslyRecurringThroughInterface<T_ICuriouslyRecurring>>
    {
    }
    class DerivedCuriouslyRecurringThroughInterface<T_CuriouslyRecurringThroughInterface> : BaseClassWhereAImplementsB<DerivedCuriouslyRecurringThroughInterface<T_CuriouslyRecurringThroughInterface>,ICuriouslyRecurring2<T_CuriouslyRecurringThroughInterface>>, ICuriouslyRecurring2<T_CuriouslyRecurringThroughInterface>
    {
    }

    public class Program
    {
        static object _o;

        public static int Main()
        {
            TestIfCuriouslyRecurringInterfaceCanBeLoaded();
            TestIfCuriouslyRecurringInterfaceCanCast();
            TestIfCuriouslyRecurringInterfaceCanBeUsedAsConstraint();
            TestIfCuriouslyRecurringInterfaceCanBeUsedAsConstraintWorker();
            return 100; // Failures cause exceptions
        }

        public static void TestIfCuriouslyRecurringInterfaceCanBeLoaded()
        {
            Console.WriteLine("TestIfCuriouslyRecurringInterfaceCanBeLoaded");

            // Test that the a generic using a variant of the curiously recurring pattern involving an interface can be loaded.
            _o = typeof(CuriouslyRecurringThroughInterface<int>);
            Console.WriteLine("Found type: {0}", _o);
            Console.WriteLine("Curiously recurring interface: {0}", typeof(ICuriouslyRecurring<>));
            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]: {0}", typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]);
            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces().Length: {0}", typeof(ICuriouslyRecurring<>).GetInterfaces().Length);
            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0]: {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0]);
            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments().Length: {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments().Length);
            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0]: {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0]);
            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetGenericArguments()[0]: {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetGenericArguments()[0]);
            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetGenericArguments()[0]==typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]: {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetGenericArguments()[0]==typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]);
            if (!(typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetGenericArguments()[0]==typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]))
            {
                throw new Exception("Fail checking for condition that should be true - 2");
            }

            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces().Length: {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces().Length);

            // On CoreCLR this gets the Open type, which isn't really correct, but it has been that way for a very long time
            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces()[0]: {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces()[0]);
            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces()[0]==typeof(ICuriouslyRecurring<>): {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces()[0]==typeof(ICuriouslyRecurring<>));

            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces()[0].GetGenericTypeDefinition()==typeof(ICuriouslyRecurring<>): {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces()[0].GetGenericTypeDefinition()==typeof(ICuriouslyRecurring<>));

            Console.WriteLine("typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces()[0].GetGenericArguments()[0]==typeof(ICuriouslyRecurring<>)GetGenericArguments()[0]: {0}", typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces()[0].GetGenericArguments()[0]==typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]);
            if (!(typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0].GetInterfaces()[0].GetGenericArguments()[0]==typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]))
            {
                throw new Exception("Fail checking for condition that should be true - 2");
            }
        }

        public static void TestIfCuriouslyRecurringInterfaceCanCast()
        {
            Console.WriteLine("TestIfCuriouslyRecurringInterfaceCanCast");
            Console.WriteLine("typeof(ICuriouslyRecurring<>).MakeGenericType(typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]).IsAssignableFrom(typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0]): {0}", typeof(ICuriouslyRecurring<>).MakeGenericType(typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]).IsAssignableFrom(typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0]));
            if (!(typeof(ICuriouslyRecurring<>).MakeGenericType(typeof(ICuriouslyRecurring<>).GetGenericArguments()[0]).IsAssignableFrom(typeof(ICuriouslyRecurring<>).GetInterfaces()[0].GetGenericArguments()[0])))
            {
                throw new Exception("Fail");
            }
        }

        public static void TestIfCuriouslyRecurringInterfaceCanBeUsedAsConstraint()
        {
            Console.WriteLine("TestIfCuriouslyRecurringInterfaceCanBeUsedAsConstraint");
            TestIfCuriouslyRecurringInterfaceCanBeUsedAsConstraintWorker();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TestIfCuriouslyRecurringInterfaceCanBeUsedAsConstraintWorker()
        {
            // Test that the a generic using a variant of the curiously recurring pattern involving an interface and constraint can be loaded.
            // This test is just like TestIfCuriouslyRecurringInterfaceCanBeLoaded, except that it is structured so that we perform a cast via a constraint at type load time
            _o = typeof(DerivedCuriouslyRecurringThroughInterface<int>);
            Console.WriteLine("Found type: {0}", _o);
        }
    }
}
