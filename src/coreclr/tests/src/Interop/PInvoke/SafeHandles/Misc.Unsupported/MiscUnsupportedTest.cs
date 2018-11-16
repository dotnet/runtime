// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using SafeHandlesTests;
using TestLibrary;

public class SHTester_Misc
{
    public static int Main()
    {
        try
        {
            RunSHMiscTests();
            RunChildSHStructParamTests();

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHArrayParam(SafeFileHandle[] arr, Int32[] arrInt32s, int length);

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern StructWithSHFld SHReturnStruct();

    [DllImport("PInvoke_SafeHandle", SetLastError = true)]
    public static extern bool SHStructParam_In2([In]StructWithSHArrayFld s, Int32[] arrInt32s, int length);

    /// <summary>
    ///runs all other miscellaneous tests;
    ///it uses the PInvoke signatures defined above it 
    ///1-passing arrays of SHs as parameters
    ///2-passing arrays of structures (with SH subclass fields) as parameters
    ///3-returning SHs from unmanaged code as pure return values
    ///4-returning structures (with SH subclass fields) from unmanaged code as pure return values
    ///5-passing nested structures (with the nested structure having a SH subclass field)
    ///6-passing structures with SH Array fields
    ///7-passing mixed params (SH, SH subclass, subclass of SH subclass)
    ///8-passing struct params that have many handle fields [in, ref (with and without changes to flds)]
    ///9-passing SH subclass in Dispatch\UnknownWrapper, expecting a VARIANT (of type VT_DISPATCH or
    ///VT_UNKNOWN) on the managed side; as params and as fields
    /// </summary>
    public static void RunSHMiscTests()
    {
        Console.WriteLine("\nRunSHMiscTests():");

        ///1-passing arrays of SHs as parameters

        SafeFileHandle[] hndArray = new SafeFileHandle[Helper.N];
        //the following array will contain the 32-bit values corresponding to hndArray's elements
        Int32[] hndArrayInt32s = new Int32[Helper.N];
        for (int i = 0; i < Helper.N; i++)
        {
            hndArray[i] = Helper.NewSFH();
            hndArrayInt32s[i] = Helper.SHInt32(hndArray[i]);
        }
        Console.WriteLine("Testing SHArrayParam...");
        Assert.Throws<MarshalDirectiveException>(() => SHArrayParam(hndArray, hndArrayInt32s, Helper.N), "FAILED!  Exception not thrown.");

        //4-returning structures (with SH subclass fields) from unmanaged code as pure return values
        Console.WriteLine("Testing SHReturnStruct...");
        Assert.Throws<MarshalDirectiveException>(() => SHReturnStruct(), "FAILED!  Exception not thrown.");

        //6-passing structures with SH Array fields
        hndArray = new SafeFileHandle[Helper.N];
        //the following array will contain the 32-bit values corresponding to hndArray's elements
        hndArrayInt32s = new Int32[Helper.N];
        for (int i = 0; i < Helper.N; i++)
        {
            hndArray[i] = Helper.NewSFH();
            hndArrayInt32s[i] = Helper.SHInt32(hndArray[i]);
        }
        StructWithSHArrayFld sWithArrFld = new StructWithSHArrayFld();
        sWithArrFld.sharr = hndArray; //assign hnd array to hnd array field
        Console.WriteLine("Testing SHStructParam_In2...");
        Assert.Throws<TypeLoadException>(() => SHStructParam_In2(sWithArrFld, hndArrayInt32s, Helper.N), "FAILED!  Exception not thrown.");
    }

    /// <summary>
    ///passing SafeFileHandle subclass parameters to unmanaged code in various combinations and forms;
    ///it uses the PInvoke signatures defined above it 
    ///1-passing SafeFileHandle subclass parameters individually in separate methods (In, out, ref)
    ///2-passing SafeFileHandle subclass parameters in combination in the same method
    /// </summary>

    [DllImport("PInvoke_SafeHandle", PreserveSig = false, SetLastError = true)]
    public static extern StructWithChildSHFld SHStructParam_OutRetVal();

    /// <summary>
    ///passing structures (with SafeFileHandle subclass fields) as parameters in various combinations and forms;
    ///it uses the PInvoke signatures defined above it
    ///1-passing structures (In, out, ref) (with SafeFileHandle subclass fields) individually in separate methods
    ///2-passing structures (In, out, ref) (with SafeFileHandle subclass fields) in combination in the same method
    /// </summary>
    public static void RunChildSHStructParamTests()
    {
        Console.WriteLine("\nRunChildSHStructParamTests():");

        StructWithChildSHFld s = new StructWithChildSHFld();
        s.hnd = Helper.NewChildSFH(); //get a new SH
        Int32 hndInt32 = Helper.SHInt32(s.hnd); //get the 32-bit value associated with s.hnd

        Console.WriteLine("Testing SHStructParam_OutRetVal...");
        Assert.Throws<MarshalDirectiveException>(() => s = SHStructParam_OutRetVal(), "FAILED!  Exception not thrown.");
    }
}