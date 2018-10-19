// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

#pragma warning disable 618
[StructLayout(LayoutKind.Sequential)]
public struct Stru_Seq_DateAsStructAsFld
{
    [MarshalAs(UnmanagedType.Struct)]
    public DateTime dt;

    public int iInt;

    [MarshalAs(UnmanagedType.BStr)]
    public string bstr;
}

[StructLayout(LayoutKind.Explicit)]
public struct Stru_Exp_DateAsStructAsFld
{

    [FieldOffset(0)]
    public int iInt;

    [FieldOffset(8)]
    [MarshalAs(UnmanagedType.Struct)]
    public DateTime dt;
}

class DatetimeTest
{
    private static DateTime ExpectedRetdate;

    #region PInvoke
    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern bool Marshal_In_stdcall([In][MarshalAs(UnmanagedType.Struct)] DateTime t);

    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool Marshal_InOut_cdecl([In, Out][MarshalAs(UnmanagedType.Struct)] ref DateTime t);

    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern bool Marshal_Out_stdcall([Out][MarshalAs(UnmanagedType.Struct)] out DateTime t);

    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool MarshalSeqStruct_InOut_cdecl([In, Out][MarshalAs(UnmanagedType.Struct)] ref Stru_Seq_DateAsStructAsFld t);

    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool MarshalExpStruct_InOut_cdecl([In, Out][MarshalAs(UnmanagedType.Struct)] ref Stru_Exp_DateAsStructAsFld t);
    #endregion

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool Del_Marshal_InOut_cdecl([In, Out][MarshalAs(UnmanagedType.Struct)] ref DateTime t);

    #region delegatePinvoke

    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    private static extern Del_Marshal_InOut_cdecl GetDel_Marshal_InOut_cdecl();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate bool Del_Marshal_Out_stdcall([Out][MarshalAs(UnmanagedType.Struct)] out DateTime t);

    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern Del_Marshal_Out_stdcall GetDel_Marshal_Out_stdcall();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool Del_MarshalSeqStruct_InOut_cdecl([In, Out][MarshalAs(UnmanagedType.Struct)] ref Stru_Seq_DateAsStructAsFld t);

    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern Del_MarshalSeqStruct_InOut_cdecl GetDel_Del_MarshalSeqStruct_InOut_cdecl();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate bool Del_MarshalExpStruct_InOut_cdecl([In, Out][MarshalAs(UnmanagedType.Struct)] ref Stru_Exp_DateAsStructAsFld t);

    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern Del_MarshalExpStruct_InOut_cdecl GetDel_Del_MarshalExpStruct_InOut_cdecl();

    #endregion

    #region ReversePInvoke

    [DllImport("NativeDateTime.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool RevP_Marshal_InOut_cdecl(Del_Marshal_InOut_cdecl d);

    public static bool RevPMethod_Marshal_InOut_cdecl(ref DateTime d)
    {
        ExpectedRetdate = new DateTime(1947, 8, 15);
        Assert.AreEqual(ExpectedRetdate, d, "RevPMethod_Marshal_InOut_cdecl : Date didn't match to expected date");
        d = d.AddDays(-1);
        return true;
    }

    #endregion

    static int Main(string[] args)
    {
        try{
            ExpectedRetdate = new DateTime(1947, 8, 15);

            #region Pinvoke
            DateTime Date1 = new DateTime(2008, 7, 4);
            Assert.IsTrue(Marshal_In_stdcall(Date1), "Marshal_In_stdcall : Returned false");
            Assert.IsTrue(Marshal_InOut_cdecl(ref Date1), "Marshal_InOut_cdecl : Returned false");
            Assert.AreEqual(ExpectedRetdate, Date1, "Marshal_InOut_cdecl : Returned date is wrong");
            
            DateTime Date2;
            Assert.IsTrue(Marshal_Out_stdcall(out Date2), "Marshal_In_stdcall : Returned false");
            Assert.AreEqual(ExpectedRetdate, Date2, "Marshal_InOut_cdecl : Returned date is wrong");

            Stru_Seq_DateAsStructAsFld StDate1;
            StDate1.dt = new DateTime(2008, 7, 4);
            StDate1.iInt = 100;
            StDate1.bstr = "Managed";
            Assert.IsTrue(MarshalSeqStruct_InOut_cdecl(ref StDate1), "MarshalSeqStruct_InOut_cdecl : Native side check failed");
            Assert.AreEqual(ExpectedRetdate, StDate1.dt, "MarshalSeqStruct_InOut_cdecl : Returned date is wrong");

            Stru_Exp_DateAsStructAsFld StDate2;
            StDate2.dt = new DateTime(2008, 7, 4);
            StDate2.iInt = 100;
            Assert.IsTrue(MarshalExpStruct_InOut_cdecl(ref StDate2), "MarshalExpStruct_InOut_cdecl : Native side check failed");
            Assert.AreEqual(ExpectedRetdate, StDate2.dt, "MarshalExpStruct_InOut_cdecl : Returned date is wrong");

            #endregion

            #region DelegatePInvoke

            Del_Marshal_InOut_cdecl del1 = GetDel_Marshal_InOut_cdecl();
            DateTime Date4 = new DateTime(2008, 7, 4);
            Assert.IsTrue(del1(ref Date4), "GetDel_Marshal_InOut_cdecl : Returned false");
            Assert.AreEqual(ExpectedRetdate, Date4, "GetDel_Marshal_InOut_cdecl : Returned date is wrong");

            Del_Marshal_Out_stdcall del3 = GetDel_Marshal_Out_stdcall();
            DateTime Date6;
            Assert.IsTrue(del3(out Date6), "GetDel_Marshal_Out_stdcall : Returned false");
            Assert.AreEqual(ExpectedRetdate, Date6, "GetDel_Marshal_Out_stdcall : Returned date is wrong");
            
            Stru_Seq_DateAsStructAsFld StDate3;
            StDate3.dt = new DateTime(2008, 7, 4);
            StDate3.iInt = 100;
            StDate3.bstr = "Managed";
            Del_MarshalSeqStruct_InOut_cdecl del4 = GetDel_Del_MarshalSeqStruct_InOut_cdecl();
            Assert.IsTrue(del4(ref StDate3), "MarshalSeqStruct_InOut_cdecl : Native side check failed");
            Assert.AreEqual(ExpectedRetdate, StDate3.dt, "MarshalSeqStruct_InOut_cdecl : Returned date is wrong");

            Stru_Exp_DateAsStructAsFld StDate4;
            StDate4.dt = new DateTime(2008, 7, 4);
            StDate4.iInt = 100;

            Del_MarshalExpStruct_InOut_cdecl del5 = GetDel_Del_MarshalExpStruct_InOut_cdecl();
            Assert.IsTrue(del5(ref StDate4), "MarshalExpStruct_InOut_cdecl : Native side check failed");
            Assert.AreEqual(ExpectedRetdate, StDate4.dt, "MarshalExpStruct_InOut_cdecl : Returned date is wrong");

            #endregion

            #region ReversePInvoke
            Assert.IsTrue(RevP_Marshal_InOut_cdecl(new Del_Marshal_InOut_cdecl(RevPMethod_Marshal_InOut_cdecl)), "RevP_Marshal_InOut_cdecl : Returned false");
            #endregion
            
            return 100;
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}"); 
            return 101; 
        }
    }
}
#pragma warning restore 618

