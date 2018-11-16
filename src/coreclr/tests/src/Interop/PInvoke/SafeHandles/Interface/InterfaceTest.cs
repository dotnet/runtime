// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using SafeHandlesTests;
using System.Threading;
using TestLibrary;

[StructLayout(LayoutKind.Sequential)]
public struct StructMAIntf
{
    [MarshalAs(UnmanagedType.Interface)]
    public SafeFileHandle hnd;
}

public class SHtoIntfTester
{
    public static int Main()
    {
        try
        {
            RunTests();

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }

    [DllImport("PInvoke_SafeHandle_MarshalAs_Interface")]
    public static extern bool SH_MAIntf([MarshalAs(UnmanagedType.Interface)]SafeFileHandle sh, Int32 shVal, Int32 shfld1Val, Int32 shfld2Val);

    [DllImport("PInvoke_SafeHandle_MarshalAs_Interface")]
    public static extern bool SH_MAIntf_Ref([MarshalAs(UnmanagedType.Interface)]ref SafeFileHandle sh, Int32 shVal, Int32 shfld1Val, Int32 shfld2Val);

    [DllImport("PInvoke_SafeHandle_MarshalAs_Interface")]
    public static extern bool SHFld_MAIntf(StructMAIntf s, Int32 shndVal, Int32 shfld1Val, Int32 shfld2Val);

    public static void RunTests()
    {
        Console.WriteLine("RunTests started");

        ////////////////////////////////////////////////////////
        SafeFileHandle sh = Helper.NewSFH();
        Int32 shVal = Helper.SHInt32(sh);
        sh.shfld1 = Helper.NewSFH(); //SH field of SFH class
        Int32 shfld1Val = Helper.SHInt32(sh.shfld1);
        sh.shfld2 = Helper.NewSFH(); //SFH field of SFH class
        Int32 shfld2Val = Helper.SHInt32(sh.shfld2);

        //NOTE: SafeHandle is now ComVisible(false)...QIs for IDispatch or the class interface on a 
        //    type with a ComVisible(false) type in its hierarchy are no longer allowed; so calling 
        //    the DW ctor with a SH subclass causes an invalidoperationexception to be thrown since
        //    the ctor QIs for IDispatch
        Console.WriteLine("Testing SH_MAIntf...");
        Assert.Throws<InvalidOperationException>(() => SH_MAIntf(sh, shVal, shfld1Val, shfld2Val), "Did not throw InvalidOperationException!");

        ////////////////////////////////////////////////////////
        sh = Helper.NewSFH();
        shVal = Helper.SHInt32(sh);
        sh.shfld1 = Helper.NewSFH(); //SH field of SFH class
        shfld1Val = Helper.SHInt32(sh.shfld1);
        sh.shfld2 = Helper.NewSFH(); //SFH field of SFH class
        shfld2Val = Helper.SHInt32(sh.shfld2);

        //NOTE: SafeHandle is now ComVisible(false)...QIs for IDispatch or the class interface on a 
        //    type with a ComVisible(false) type in its hierarchy are no longer allowed; so calling 
        //    the DW ctor with a SH subclass causes an invalidoperationexception to be thrown since
        //    the ctor QIs for IDispatch
        Console.WriteLine("Testing SH_MAIntf_Ref...");
        Assert.Throws<InvalidOperationException>(() => SH_MAIntf_Ref(ref sh, shVal, shfld1Val, shfld2Val), "Did not throw InvalidOperationException!");

        ////////////////////////////////////////////////////////
        StructMAIntf s = new StructMAIntf();
        s.hnd = Helper.NewSFH();
        Int32 shndVal = Helper.SHInt32(s.hnd);
        s.hnd.shfld1 = Helper.NewSFH(); //SH field of SFH field of struct
        shfld1Val = Helper.SHInt32(s.hnd.shfld1);
        s.hnd.shfld2 = Helper.NewSFH(); //SFH field of SFH field of struct
        shfld2Val = Helper.SHInt32(s.hnd.shfld2);

        //NOTE: SafeHandle is now ComVisible(false)...QIs for IDispatch or the class interface on a 
        //    type with a ComVisible(false) type in its hierarchy are no longer allowed; so calling 
        //    the DW ctor with a SH subclass causes an invalidoperationexception to be thrown since
        //    the ctor QIs for IDispatch
        Console.WriteLine("Testing SHFld_MAIntf...");
        Assert.Throws<InvalidOperationException>(() => SHFld_MAIntf(s, shndVal, shfld1Val, shfld2Val), "Did not throw InvalidOperationException!");
    
        Console.WriteLine("RunTests end");
    }
}