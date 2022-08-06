// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Devirtualization
{
    internal static int Run()
    {
        RegressionBug73076.Run();
        DevirtualizationCornerCaseTests.Run();

        return 100;
    }

    class RegressionBug73076
    {
        interface IFactory
        {
            BaseFtnn<T> Make<T>();
        }

        class Factory : IFactory
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public BaseFtnn<T> Make<T>() => new DerivedFtnn<T>();
        }

        class BaseFtnn<T>
        {
            public virtual string GetId() => "Base";
        }

        class DerivedFtnn<T> : BaseFtnn<T>
        {
            public override string GetId() => "Derived";
        }

        public static void Run()
        {
            IFactory factory = new Factory();

            // This is a generic virtual method call so we'll only ever see BaseFtnn and DerivedFtnn instantiated
            // over __Canon at compile time.
            var made = factory.Make<object>();
            if (made.GetId() != "Derived")
                throw new Exception();
        }
    }

    class DevirtualizationCornerCaseTests
    {
        interface IIntf1
        {
            int GetValue();
        }

        class Intf1Impl : IIntf1
        {
            public virtual int GetValue() => 123;
        }

        [DynamicInterfaceCastableImplementation]
        interface IIntf1Impl : IIntf1
        {
            int IIntf1.GetValue() => 456;
        }

        class Intf1CastableImpl : IDynamicInterfaceCastable
        {
            public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType) => typeof(IIntf1Impl).TypeHandle;
            public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented) => true;
        }

        interface IIntf2
        {
            int GetValue();
        }

        class Intf2Impl1 : IIntf2
        {
            public virtual int GetValue() => 123;
        }

        class Intf2Impl2<T> : IIntf2
        {
            public virtual int GetValue() => 456;
        }

        static void AssertEqual<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
                throw new Exception();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestIntf1(IIntf1 o, int expected) => AssertEqual(expected, o.GetValue());

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestIntf2(IIntf2 o, int expected) => AssertEqual(expected, o.GetValue());

        public static void Run()
        {
            TestIntf1(new Intf1Impl(), 123);
            TestIntf1((IIntf1)new Intf1CastableImpl(), 456);

            TestIntf2(new Intf2Impl1(), 123);
            TestIntf2((IIntf2)Activator.CreateInstance(typeof(Intf2Impl2<>).MakeGenericType(typeof(object))), 456);
        }
    }
}
