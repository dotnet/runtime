// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

class MultipleAssembliesWithSamePInvokeTest
{
    [DllImport(@"MAWSPINative", CallingConvention = CallingConvention.StdCall)]
    private static extern int GetInt();

    public static int Main(string[] args)
    {
        try{
            Assert.AreEqual(24, GetInt(), "MultipleAssembliesWithSamePInvoke.GetInt() failed.");
            Assert.AreEqual(24, ManagedDll1.Class1.GetInt(), "ManagedDll.Class1.GetInt() failed.");
            Assert.AreEqual(24, ManagedDll2.Class2.GetInt(), "ManagedDll.Class2.GetInt() failed.");
            
            return 100;
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}"); 
            return 101; 
        }      
    }
}
