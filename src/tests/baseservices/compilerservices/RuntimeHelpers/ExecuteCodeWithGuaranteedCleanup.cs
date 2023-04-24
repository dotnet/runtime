// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
using System;
using System.Runtime.CompilerServices;

class GCD
{
    private int _val = -2;
    private int _exitcode = -1;
    public GCD() {}
    public int GetExitCode(){ return _exitcode;}
    public void g ()
    {
        throw new System.Exception("TryCode test");
    }
    public void TryCode0 (object obj)
    {
        _val = (int)obj;
        g();
    }
    public void CleanupCode0 (object obj, bool excpThrown)
    {
        if(excpThrown && ((int)obj == _val))
        {
            _exitcode = 100;
        }
    }
}

class ExecuteCodeWithGuaranteedCleanupTest
{
    public static void Run()
    {
        GCD gcd = new GCD();
        RuntimeHelpers.TryCode t = new RuntimeHelpers.TryCode(gcd.TryCode0);
        RuntimeHelpers.CleanupCode c = new RuntimeHelpers.CleanupCode(gcd.CleanupCode0);
        int val = 21;
        try
        {
#pragma warning disable SYSLIB0004 // CER is obsolete
            RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(t, c, val);
#pragma warning restore SYSLIB0004
        }
        catch (Exception Ex)
        {

        }

        int res = gcd.GetExitCode();
        if (res != 100)
            throw new Exception($"{nameof(ExecuteCodeWithGuaranteedCleanupTest)} failed. Result: {res}");
    }
}
