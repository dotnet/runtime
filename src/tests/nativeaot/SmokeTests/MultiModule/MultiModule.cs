// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public class ReflectionTest
{
    const int Pass = 100;
    const int Fail = -1;

    [Fact]
    public static int TestEntryPoint()
    {
        if (TestStaticBases() == Fail)
            return Fail;

        if (TestSharedGenerics() == Fail)
            return Fail;

        if (TestGenericTLS() == Fail)
            return Fail;

        if (TestInjectedEnumMethods() == Fail)
            return Fail;

        return Pass;
    }
    
    public static int TestStaticBases()
    {
        Console.WriteLine("Testing static bases in library code are available..");
        MultiModuleLibrary.ReturnValue = 50;
        MultiModuleLibrary.ThreadStaticInt = 50;
        
        MultiModuleLibrary.StaticString = MultiModuleLibrary.ReturnValue.ToString() + MultiModuleLibrary.ThreadStaticInt.ToString();
        if (MultiModuleLibrary.StaticString != "5050")
            return Fail;
        
        if (MultiModuleLibrary.ReturnValue + MultiModuleLibrary.ThreadStaticInt != 100)
            return Fail;
        
        return Pass;
    }

    public static int TestSharedGenerics()
    {
        Console.WriteLine("Testing generic dictionaries can be folded properly..");

        // Use a generic dictionary that also exists in the library
        if (!MultiModuleLibrary.GenericClass<string>.IsT("Hello"))
            return Fail;
        if (!MultiModuleLibrary.GenericClass<string>.IsMdArrayOfT(new string[0, 0]))
            return Fail;

        if (!MultiModuleLibrary.GenericClass<MultiModuleLibrary.GenericStruct<string>>.IsArrayOfT(new MultiModuleLibrary.GenericStruct<string>[0]))
            return Fail;
        if (!MultiModuleLibrary.GenericClass<MultiModuleLibrary.GenericStruct<string>>.IsT(new MultiModuleLibrary.GenericStruct<string>()))
            return Fail;

        if (!MultiModuleLibrary.MethodThatUsesGenerics())
            return Fail;

        return Pass;
    }

    public static int TestGenericTLS()
    {
        Console.WriteLine("Testing thread statics on generic types shared between modules are shared properly..");

        if (!MultiModuleLibrary.MethodThatUsesGenericWithTLS())
            return Fail;

        MultiModuleLibrary.GenericClassWithTLS<int>.ThreadStaticInt += 1;
        if (MultiModuleLibrary.GenericClassWithTLS<int>.ThreadStaticInt != 2)
            return Fail;

        return Pass;
    }

    public static int TestInjectedEnumMethods()
    {
        Console.WriteLine("Testing context-injected methods on enums..");
        if (!MultiModuleLibrary.MyEnum.One.Equals(MultiModuleLibrary.MyEnum.One))
            return Fail;

        return Pass;
    }
}
