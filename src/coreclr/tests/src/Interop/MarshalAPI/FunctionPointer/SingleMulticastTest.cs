// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using TestLibrary;

partial class FunctionPtr
{
    public delegate bool DelegateWithLong(long l); //Singlecast delegate
    public delegate void MultiDelegateWithLong(long l); //Multicast delegate

    public static void RunGetFcnPtrSingleMulticastTest()
    {
        Console.WriteLine($"Running {nameof(RunGetFcnPtrSingleMulticastTest)}...");

        DelegateWithLong del = new DelegateWithLong(Method);
        MultiDelegateWithLong multidel = new MultiDelegateWithLong(Method2);

        {
            IntPtr fcnptr = Marshal.GetFunctionPointerForDelegate<DelegateWithLong>(del);
            Assert.IsTrue(FunctionPointerNative.CheckFcnPtr(fcnptr));
        }

        {
            IntPtr fcnptr = Marshal.GetFunctionPointerForDelegate<MultiDelegateWithLong>(multidel);
            FunctionPointerNative.CheckFcnPtr(fcnptr);
        }

        bool Method(long l)
        {
            if (l != 999999999999)
                return false;
            else
                return true;
        }

        void Method2(long l)
        {
            if (l != 999999999999)
                throw new Exception("Failed multicast call");
        }
    }
}
