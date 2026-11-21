// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

// The Invoke method of delegate types is only made reflectable if something in the program reflects on
// Invoke of a delegate type that is not statically known. The pattern being tested must be the only one
// in this test.
class Program
{
    static int Main()
    {
        Delegate del = GetDelegate();
#if GETTYPE_ON_DELEGATE
        MethodInfo invokeMethod = del.GetType().GetMethod("Invoke");
        if ((int)invokeMethod.Invoke(del, [41]) != 42)
            throw new Exception("GetType");
#else
        if ((int)del.DynamicInvoke(41) != 42)
            throw new Exception("DynamicInvoke");
#endif

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Delegate GetDelegate() => new IncrementDelegate(value => value + 1);
}

delegate int IncrementDelegate(int value);
