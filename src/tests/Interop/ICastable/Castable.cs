// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

public interface IRetArg<T>
{
    T ReturnArg(T t);
}

public interface IRetThis
{
    IRetThis ReturnThis();
    Type GetMyType();
}


public interface IUnimplemented
{
    void UnimplementedMethod();
}

public interface IExtra
{
    int InnocentMethod();
}

public class CastableException : Exception {};


public class RetArgImpl: IRetArg<string>
{
    public string ReturnArg(string t)
    {
        Console.WriteLine("ReturnArg has been called.");
        return t;
    }
}

public class GenRetArgImpl<T>: IRetArg<T>
{
    public T ReturnArg(T t)
    {
        Console.WriteLine(String.Format("Generic ReturnArg has been called. My type is {0}", GetType()));
        return t;
    }
}


public class RetThisImpl: IRetThis
{
    public IRetThis ReturnThis()
    {
        Console.WriteLine("RetThis has been called.");
        return this;
    }

    public Type GetMyType()
    {
        Console.WriteLine(String.Format("GetMyType has been called. My type is {0}", GetType()));
        return GetType();
    }
}


public class Castable : ICastable, IExtra
{
    private Dictionary<Type, Type> _interface2impl;

    public Castable(Dictionary<Type, Type> interface2impl)
    {
        _interface2impl = interface2impl;
    }

    public bool IsInstanceOfInterface(RuntimeTypeHandle interfaceType, out Exception castError)
    {
        Console.WriteLine(String.Format("IsInstanceOfInterface has been called for type {0}", Type.GetTypeFromHandle(interfaceType)));
        if (_interface2impl == null)
        {
            castError = new CastableException();
            return false;
        }
        castError = null;
        return _interface2impl.ContainsKey(Type.GetTypeFromHandle(interfaceType));
    }

    public RuntimeTypeHandle GetImplType(RuntimeTypeHandle interfaceType)
    {
        Console.WriteLine(String.Format("GetImplType has been called for type {0}", Type.GetTypeFromHandle(interfaceType)));
        return _interface2impl[Type.GetTypeFromHandle(interfaceType)].TypeHandle;
    }

    public int InnocentMethod()
    {
        Console.WriteLine(String.Format("InnocentMethod has been called. My type is {0}", GetType()));
        return 3;
    }
}

public class BadCastable : ICastable
{
    public bool IsInstanceOfInterface(RuntimeTypeHandle interfaceType, out Exception castError)
    {
        castError = null;
        return true;
    }

    public RuntimeTypeHandle GetImplType(RuntimeTypeHandle interfaceType)
    {
        return default(RuntimeTypeHandle);
    }
}

public class CastableTests
{
    public static void Assert(bool value, string message)
    {
        Xunit.Assert.True(value, message);
    }

    [Fact]
    [SkipOnMono("ICastable is unsupported on Mono")]
    public static void Test()
    {
        //Console.WriteLine("Execution started. Attach debugger and press enter.");
        //Console.ReadLine();

        try
        {
            object implProxy = new Castable(
                 new Dictionary<Type, Type>()
                 {
                     { typeof(IRetArg<string>), typeof(RetArgImpl) },
                     { typeof(IRetArg<int>), typeof(GenRetArgImpl<int>) },
                     { typeof(IRetThis), typeof(RetThisImpl) },
                     { typeof(IExtra), null }, //we should never use it
                 }
            );

            // testing simple cases
            Assert(implProxy is IRetThis, "implProxy should be castable to IRetThis via is");
            Assert(!(implProxy is IUnimplemented), "implProxy should not be castable to IUnimplemented via is");
            Assert((implProxy as IRetThis) != null, "implProxy should be castable to IRetThis is as");
            Assert((implProxy as IUnimplemented) == null, "implProxy should not be castable to IUnimplemented is as");
            var retThis = (IRetThis)implProxy;
            Assert(object.ReferenceEquals(retThis.ReturnThis(), implProxy), "RetThis should return implProxy");
            Assert(retThis.GetMyType() == typeof(Castable), "GetMyType should return typeof(Castable)");

            Assert(!(implProxy is IUnimplemented), "implProxy should not be castable to IUnimplemented via is");
            Assert((implProxy as IUnimplemented) == null, "implProxy should not be castable to IUnimplemented via as");


            // testing generics
            IRetArg<string> retArgStr = (IRetArg<string>)implProxy;
            Assert(retArgStr.ReturnArg("hohoho") == "hohoho", "retArgStr.ReturnArg() should return arg");

            IRetArg<int> retArgInt = (IRetArg<int>)implProxy;
            Assert(retArgInt.ReturnArg(42) == 42, "retArgInt.ReturnArg() should return arg");


            // testing Castable implementing other interfaces
            var extra = (IExtra)implProxy;
            Assert(extra.InnocentMethod() == 3, "InnocentMethod() should be called on Castable and return 3");

            // testing error handling
            try
            {
                var _ = (IUnimplemented)implProxy;
                Assert(false, "pProxy should not be castable to I1");
            }
            catch (InvalidCastException) {}

            object nullCastable = new Castable(null);
            try
            {
                var _ = (IRetThis)nullCastable;
                Assert(false, "Exceptions should be thrown from IsInstanceOfInterface");
            }
            catch (CastableException) {}

            Assert(!(nullCastable is IRetThis), "null castable shouldn't be allowed to be casted to anything");

            var shouldBeNull = nullCastable as IRetThis;
            Assert(shouldBeNull == null, "shouldBeNull should be assigned null");

            object badCastable = new BadCastable();
            try
            {
                var r = (IRetThis)badCastable;
                r.ReturnThis();
                Assert(false, "Exceptions should be thrown from ReturnThis()");
            }
            catch (EntryPointNotFoundException) {}

            //delegate testing
            Func<int> fInt = new Func<int>(extra.InnocentMethod);
            Assert(fInt() == 3, "Delegate call to InnocentMethod() should return 3");

            Func<IRetThis> func = new Func<IRetThis>(retThis.ReturnThis);
            Assert(object.ReferenceEquals(func(), implProxy), "Delegate call to ReturnThis() should return this");
       }
       catch (Exception e)
       {
            Assert(false, e.ToString());
       }
    }
}
