// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text;
using Xunit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe class ThisCallNative
{
    [DllImport(nameof(ThisCallNative), CallingConvention = CallingConvention.ThisCall, EntryPoint = "GetWidthAsLongFromManaged")]
    public static extern int ThisCallWithEmptySignature();
}

unsafe class EmptyThisCallTest
{
    public static int Main(string[] args)
    {
        try
        {
            delegate* unmanaged[Thiscall]<void*, int> fn = &Foo;
            CalliEmptyThisCall((delegate* unmanaged[Thiscall]<int>)fn);
            Console.WriteLine("FAIL: thiscall fptr with no args should have failed");
            return -1;
        }
        catch (InvalidProgramException)
        {
            Console.WriteLine("thiscall fptr with no args failed as expected");
        }

        try
        {
            PinvokeEmptyThisCall();
            Console.WriteLine("FAIL: pinvoke thiscall with no args should have failed");
            return -1;
        }
        catch (InvalidProgramException)
        {
            Console.WriteLine("thiscall pinvoke with no args failed as expected");
        }

        return 100;
    }

    [UnmanagedCallersOnly(CallConvs = new [] {typeof(CallConvThiscall)})]
    private static int Foo(void* a)
    {
        return 0;
    }

    private static int CalliEmptyThisCall(delegate* unmanaged[Thiscall]<int> fn)
    {
        return fn();
    }

    private static void PinvokeEmptyThisCall()
    {
        ThisCallNative.ThisCallWithEmptySignature();
    }
}
