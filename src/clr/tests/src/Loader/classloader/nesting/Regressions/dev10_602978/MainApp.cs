// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class RemoteBase
{
    protected interface IProtected { string Touch(); }
    protected static string UseIProtected(IProtected intrf) { return intrf.Touch(); }
}

class LocalImpl : RemoteBase
{
    protected class DirectImpl : IProtected
    {
        string IProtected.Touch() 
        { 
            return "IProtected.Touch"; 
        }
    }

    public static string Touch()
    {
        return RemoteBase.UseIProtected(new DirectImpl());
    }
}

class LocalImpl2 : RemoteBase_InSeparateAssembly
{
    protected class DirectImpl2 : IProtected_InSeparateAssembly
    {
        string IProtected_InSeparateAssembly.Touch()
        {
            return "IProtected_InSeparateAssembly.Touch";
        }
    }

    public static string Touch()
    {
        return RemoteBase_InSeparateAssembly.UseIProtected(new DirectImpl2());
    }
}

static class App
{

    public static int Main() 
    { 
        string res = LocalImpl.Touch();
        if (res != "IProtected.Touch")
        {
            Console.WriteLine("Fail: Expected 'IProtected.Touch', got '" + res + "'");
            return -1;
        }

        res = LocalImpl2.Touch();
        if (res != "IProtected_InSeparateAssembly.Touch")
        {
            Console.WriteLine("Fail: Expected 'IProtected_InSeparateAssembly.Touch', got '" + res + "'");
            return -1;
        }

        Console.WriteLine("Pass");
        return 100;
    }

}
