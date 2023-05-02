// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class StartupHook
{
    // None of these hooks should be called, because they have the
    // wrong signature (it should be static void Initialize()). This
    // is used to check that the provider code properly detects the
    // case where there are multiple incorrect Initialize
    // methods. Instead, the startup hook provider code should throw
    // an exception.

#if NONVOID_RETURN
    public static int Initialize()
    {
        Console.WriteLine("Hello from startup hook returning int!");
        return 10;
    }
#endif

#if NOT_PARAMETERLESS
    public static void Initialize(int input)
    {
        Console.WriteLine("Hello from startup hook taking int! Input: " + input);
    }
#endif

#if INSTANCE_METHOD
    public void Initialize()
    {
        Console.WriteLine("Hello from startup hook with instance method!");
    }
#endif
}
