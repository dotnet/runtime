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
        GetMI1().MakeGenericMethod(typeof(object)).Invoke(null, []);
        FlushCache();
        GetMI2().MakeGenericMethod(typeof(object)).Invoke(null, []);
        FlushCache();
        GetMI3().MakeGenericMethod(typeof(object)).Invoke(null, []);
        FlushCache();
        GetMI4().MakeGenericMethod(typeof(object)).Invoke(null, []);

        static void FlushCache()
        {
            // Make sure the cached type loader contexts are flushed
            for (int j = 0; j < 10; j++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
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
