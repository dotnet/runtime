// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using CoreFXTestLibrary;

class GitHub118072
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static MethodInfo GetMI1() => typeof(GitHub118072).GetMethod(nameof(CallMethod1));
    [MethodImpl(MethodImplOptions.NoInlining)]
    static MethodInfo GetMI2() => typeof(GitHub118072).GetMethod(nameof(CallMethod2));
    [MethodImpl(MethodImplOptions.NoInlining)]
    static MethodInfo GetMI3() => typeof(GitHub118072).GetMethod(nameof(CallMethod1));
    [MethodImpl(MethodImplOptions.NoInlining)]
    static MethodInfo GetMI4() => typeof(GitHub118072).GetMethod(nameof(CallMethod2));

    [TestMethod]
    public static void RunTest()
    {
        Type current = typeof(object);

        GetMI1().MakeGenericMethod(typeof(object)).Invoke(null, []);
        current = FillCache(current);
        GetMI2().MakeGenericMethod(typeof(object)).Invoke(null, []);
        current = FillCache(current);
        GetMI3().MakeGenericMethod(typeof(object)).Invoke(null, []);
        current = FillCache(current);
        GetMI4().MakeGenericMethod(typeof(object)).Invoke(null, []);

        static Type FillCache(Type current)
        {
            for (int i = 0; i < 400; i++)
            {
                Type next = typeof(MyClass<>).MakeGenericType(current);
                Activator.CreateInstance(next);
                current = next;
            }

            return current;
        }
    }

    public static string CallMethod1<T>() => default(MyStruct).Method<T>();

    public static string CallMethod2<T>() => default(MyStruct).Method<T>();

    struct MyStruct
    {
        public string Method<T>() => typeof(T).Name;
    }

    class MyClass<T> { }
}
