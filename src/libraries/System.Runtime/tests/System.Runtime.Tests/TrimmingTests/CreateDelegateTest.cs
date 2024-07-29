// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

/// <summary>
/// Tests calling CreateDelegate with a methodName of a non-public method on
/// a base class works correctly in a trimmed app.
/// </summary>
class Program
{
    delegate int GetIntDelegate();

    static int Main()
    {
        GetIntDelegate d = (GetIntDelegate)Delegate.CreateDelegate(typeof(GetIntDelegate), typeof(Derived), "GetInt");

        int result = d();
        if (result != 42)
        {
            return -1;
        }

        return 100;
    }
}

class Base
{
    private static int GetInt() => 42;
}

class Derived : Base { }
