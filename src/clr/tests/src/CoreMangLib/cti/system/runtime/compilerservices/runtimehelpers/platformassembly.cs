// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Platform types for RunClassConstructor tests

using System;
using System.Security;

#if WINCORESYS
[assembly: AllowPartiallyTrustedCallers]
#endif
public class Watcher
{
    public static bool f_hasRun = false;
}

[System.Security.SecurityCritical]
public class CriticalHasCctor
{

    public static int x;
    public static bool hasRun;
    static CriticalHasCctor() {
        Console.WriteLine("Diagnostic: inside CriticalHasCctor..cctor");
        //x = 2;
        Watcher.f_hasRun = true;
    }
}

[System.Security.SecuritySafeCritical]
public class SafeCriticalHasCctor
{

    static SafeCriticalHasCctor()
    {
        Console.WriteLine("Diagnostic: inside SafeCriticalHasCctor..cctor");
        Watcher.f_hasRun = true;
    }
}

[System.Security.SecuritySafeCritical]
public class SafeCriticalThrowingCctor
{
    static SafeCriticalThrowingCctor()
    {
        throw new ArgumentException("I have an argument.");
    }

}