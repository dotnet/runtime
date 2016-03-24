// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class myClass
{
    public string myString;

    // The C# compiler converts destructors to Finalize methods. Without a Finalize method,
    // a long weak reference to an object of the type becomes a short weak reference.
    ~myClass()
    {
    }
}

public class WRHelper
{
    public static myClass CreateAnObject(string s)
    {
        myClass _mc = new myClass();
        _mc.myString = s;
        return _mc;
    }

    public static bool VerifyObject(WeakReference wr, string s)
    {
        if (((myClass)wr.Target).myString != s)
            return false;
        else
            return true;
    }
}