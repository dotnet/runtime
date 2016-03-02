// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Security;

interface IFoo
{
    object GetService(Type t);
}

[SecurityCritical]
public class Foo : IFoo   
{
    public object GetService(Type serviceType)
    {
        return this;
    }
}

public class Class1
{
    [SecuritySafeCritical]
    public static int Main()
    {
        try 
        {
            Foo f = new Foo();
            object o = f.GetService(null);                     // <-- MethodAccessException
            Console.WriteLine("Success.");
            return 100;
        }
        catch (Exception e) 
        {
            Console.WriteLine("Error: " + e);
            return 101;
        }

 
    }
}
