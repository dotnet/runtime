// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security;
// using System.Security.Permissions;
using System.Collections.Generic;
using Xunit;

// [KeyContainerPermission(SecurityAction.Demand, Flags = KeyContainerPermissionFlags.Delete)]
public static class GenericSecurity
{
    private static int s_result = 99;

    private static void Gen<T>() where T : System.Exception, new()
    {
        try
        {
            throw new T();
        }
        catch (T)
        {
            Console.WriteLine("Pass");
            s_result = 100;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Gen<Exception>();
        }
        catch
        {
            Console.WriteLine("Fail");
            s_result = -2;
        }
        return s_result;
    }
}
