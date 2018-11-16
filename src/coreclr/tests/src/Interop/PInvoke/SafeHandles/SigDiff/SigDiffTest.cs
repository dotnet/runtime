// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using SafeHandlesTests;
using TestLibrary;

public class SHTester_SigDiff
{
    public static int Main()
    {
        try
        {
            RunSHParamTests();
            RunSHStructParamTests();

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    } //end of Main

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHParam_In([In]SafeHandle sh1, Int32 sh1Value);

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHParam_Out(out SafeHandle sh1);

    [DllImport("PInvoke_SafeHandle", PreserveSig = false, SetLastError = true)]
    public static extern SafeHandle SHParam_OutRetVal();

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHParam_Ref(ref SafeHandle sh1, Int32 sh1Value);

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHParam_Multiple([In]SafeHandle sh1, out SafeHandle sh2, ref SafeHandle sh3, Int32 sh1Value, Int32 sh3Value);

    /// <summary>
    ///passing SH parameters to unmanaged code in various combinations and forms;
    ///it uses the PInvoke signatures defined above it 
    ///1-  passing SH parameters individually in separate methods (In, out, ref)
    ///2-  passing SH parameters in combination in the same method
    /// </summary>
    public static void RunSHParamTests()
    {
        Console.WriteLine("\nRunSHParamTests():");

        //1-  passing SH parameters individually in separate methods (In, out, ref)

        //get a new SH
        SafeHandle hnd = Helper.NewSFH(); //NOTE that this is equivalent to SafeHandle = SafeFileHandle.CreateFile();
        Int32 hndInt32 = Helper.SHInt32(hnd); //get the 32-bit value associated with hnd

        Console.WriteLine("Testing SHParam_In...");
        Assert.IsTrue(SHParam_In(hnd, hndInt32), "FAILED! SHParam_In did not receive hnd as expected.");
        //check that the value of the HANDLE did not change
        Assert.IsFalse(Helper.IsChanged(hnd), "FAILED! SHParam_In did not return hnd as expected.");

        Console.WriteLine("Testing SHParam_Out...");
        Assert.Throws<MarshalDirectiveException>(() => SHParam_Out(out hnd), "FAILED!  Exception not thrown.");

        Console.WriteLine("Testing SHParam_OutRetVal...");
        hnd = null;
        Assert.Throws<MarshalDirectiveException>(() => hnd = SHParam_OutRetVal(), "FAILED!  Exception not thrown.");

        hnd = Helper.NewSFH(); //get a new value
        hndInt32 = Helper.SHInt32(hnd);
        Console.WriteLine("Testing SHParam_Ref...");
        Assert.Throws<MarshalDirectiveException>(() => SHParam_Ref(ref hnd, hndInt32), "FAILED!  Exception not thrown.");

        //2-  passing SH parameters in combination in the same method

        //initialize parameters
        SafeHandle hnd1 = Helper.NewSFH();
        Int32 hnd1Int32 = Helper.SHInt32(hnd1); //get the 32-bit value associated with hnd1

        SafeHandle hnd2 = null; //out parameter

        SafeHandle hnd3 = Helper.NewSFH();
        Int32 hnd3Int32 = Helper.SHInt32(hnd3); //get the 32-bit value associated with hnd3

        Console.WriteLine("Testing SHParam_Multiple...");
        Assert.Throws<MarshalDirectiveException>(() => SHParam_Multiple(hnd1, out hnd2, ref hnd3, hnd1Int32, hnd3Int32), "FAILED!  Exception not thrown.");
    } //end of static method RunParamTests

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHStructParam_In([In]StructWithBaseSHFld s, Int32 shfldValue);

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHStructParam_Out(out StructWithBaseSHFld s);

    [DllImport("PInvoke_SafeHandle", PreserveSig = false, SetLastError = true)]
    public static extern StructWithBaseSHFld SHStructParam_OutRetVal();

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHStructParam_Ref1(ref StructWithBaseSHFld s, Int32 shfldValue);

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHStructParam_Ref2(ref StructWithBaseSHFld s, Int32 shfldValue);

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHStructParam_Multiple1([In]StructWithBaseSHFld sh1, out StructWithBaseSHFld sh2,
        ref StructWithBaseSHFld sh3, Int32 sh1fldValue, Int32 sh2fldValue);

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHStructParam_Multiple2([In]StructWithBaseSHFld sh1, ref StructWithBaseSHFld sh2, Int32 sh1fldValue, Int32 sh2fldValue);

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHStructParam_Multiple3([In]StructWithBaseSHFld sh1, ref StructWithBaseSHFld sh2, Int32 sh1fldValue, Int32 sh2fldValue);

    /// <summary>
    ///passing structures (with SH fields) as parameters in various combinations and forms;
    ///it uses the PInvoke signatures defined above it
    ///1-  passing structures (In, out, ref) (with SH fields) individually in separate methods
    ///2-  passing structures (In, out, ref) (with SH fields) in combination in the same method
    /// </summary>
    public static void RunSHStructParamTests()
    {
        Console.WriteLine("\nRunSHStructParamTests():");

        //1-  passing structures (In, out, ref) (with SH fields) individually in separate methods

        //initialize a new StructWithBaseSHFld
        StructWithBaseSHFld s = new StructWithBaseSHFld();
        s.hnd = Helper.NewSFH(); //get a new SH
        Int32 hndInt32 = Helper.SHInt32(s.hnd); //get the 32-bit value associated with s.hnd

        Console.WriteLine("Testing SHStructParam_In...");
        Assert.IsTrue(SHStructParam_In(s, hndInt32), "FAILED! SHStructParam_In did not receive param as expected.");
        Assert.IsFalse(Helper.IsChanged(s.hnd), "FAILED! SHStructParam_In did not return param as expected.");

        Console.WriteLine("Testing SHStructParam_Out...");
        Assert.Throws<NotSupportedException>(() => SHStructParam_Out(out s), "FAILED!  Exception not thrown.");

        Console.WriteLine("Testing SHStructParam_OutRetVal...");
        Assert.Throws<MarshalDirectiveException>(() => s = SHStructParam_OutRetVal(), "FAILED!  Exception not thrown.");

        s.hnd = Helper.NewSFH(); //get a new SH
        hndInt32 = Helper.SHInt32(s.hnd); //get the 32-bit value associated with s.hnd
        Console.WriteLine("Testing SHStructParam_Ref1 (does not change value of handle field)...");
        Assert.IsTrue(SHStructParam_Ref1(ref s, hndInt32), "FAILED! SHStructParam_Ref1 did not receive param as expected.");
        //check that the value of the HANDLE field is not changed
        Assert.IsFalse(Helper.IsChanged(s.hnd), "FAILED! SHStructParam_Ref1 did not return param as expected.");

        Console.WriteLine("Testing SHStructParam_Ref2 (does change value of handle field)...");
        Assert.Throws<NotSupportedException>(() => SHStructParam_Ref2(ref s, hndInt32), "FAILED!  Exception not thrown.");

        //2-  passing structures (In, out, ref) (with SH fields) in combination in the same method

        //initialize parameters
        StructWithBaseSHFld s1 = new StructWithBaseSHFld();
        s1.hnd = Helper.NewSFH();
        Int32 hnd1Int32 = Helper.SHInt32(s1.hnd); //get the 32-bit value associated with s1.hnd
        StructWithBaseSHFld s2; //out parameter
        StructWithBaseSHFld s3 = new StructWithBaseSHFld();
        s3.hnd = Helper.NewSFH();
        Int32 hnd3Int32 = Helper.SHInt32(s3.hnd); //get the 32-bit value associated with s3.hnd

        Console.WriteLine("Testing SHStructParam_Multiple1 (takes an out struct as one of the params and so is expected to result in an exception)...");
        Assert.Throws<NotSupportedException>(() => SHStructParam_Multiple1(s1, out s2, ref s3, hnd1Int32, hnd3Int32), "FAILED!  Exception not thrown.");

        s3.hnd = Helper.NewSFH();
        hnd3Int32 = Helper.SHInt32(s3.hnd); //get the 32-bit value associated with s3.hnd
        Console.WriteLine("Testing SHStructParam_Multiple2 (takes a ref struct as one of the params)...");
        Assert.IsTrue(SHStructParam_Multiple2(s1, ref s3, hnd1Int32, hnd3Int32), "FAILED! SHStructParam_Multiple2 did not receive parameter(s) as expected.");
        Assert.IsFalse(Helper.IsChanged(s1.hnd) || Helper.IsChanged(s3.hnd), "FAILED! SHStructParam_Multiple2 did not return handles as expected.");

        Console.WriteLine("Testing SHStructParam_Multiple3 (takes a ref struct as one of the params and changes its handle field and so is expected to result in an exception)...");
        Assert.Throws<NotSupportedException>(() => SHStructParam_Multiple3(s1, ref s3, hnd1Int32, hnd3Int32), "FAILED!  Exception not thrown.");
    }
}