// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public class X
{
    public abstract class AbstractClassWithConstructor
    {
        public AbstractClassWithConstructor()
        {
        }
    }

    public static T TestConstructorMethod<T>() where T : new()
    {
        return new T();
    }

    public interface IItemCreator
    {
        public object CreateItem();
    }

    public sealed class ItemCreator<T> : IItemCreator where T : new()
    {
        public object CreateItem()
        {
            return new T();
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var ok = true;

        Type type = null;
        try
        {
            type = typeof(ItemCreator<>).MakeGenericType(typeof(AbstractClassWithConstructor));
        }
        catch
        {
            //Could check if it is the proper type of exception
        }
        if (type == null) {
            Console.WriteLine("Wasn't able to load type as expected");
        }
        else
        {
            Console.WriteLine("Was able to make type which wasn't expected");
            ok = false;
        }

        MethodInfo baseMethod = typeof(X).GetMethod(nameof(TestConstructorMethod), BindingFlags.Static | BindingFlags.Public);
        if (baseMethod == null)
        {
            Console.WriteLine("baseMethod was null which wasn't expected");
            ok = false;
        }
        MethodInfo method = null;
        try
        {
            method = baseMethod.MakeGenericMethod(typeof(AbstractClassWithConstructor));
        }
        catch
        {
            //Could check if it is the proper method of exception
        }
        if (method == null)
        {
            Console.WriteLine("Wasn't able to load method as expected");
        }
        else
        {
            Console.WriteLine("Was able to make method which wasn't expected");
            ok = false;
        }

        Console.WriteLine(ok ? "PASS" : "FAIL");
        return ok ? 100 : -1;
    }
}
