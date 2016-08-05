// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using CoreFXTestLibrary;

class MarshalClassTests
{
    //definition of structure that will be used in testing of structs with Fixed BSTR Safearray fields
    internal struct Variant
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr bstrVal;
        public IntPtr pRecInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithFxdLPSTRSAFld
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.LPStr, SizeConst = 0)]
        public String[] Arr;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SomeTestStruct
    {
        public int i;
        //[MarshalAs(UnmanagedType.BStr)]
        public String s;
    }

    public enum TestEnum
    {
        red,
        green,
        blue
    }

#if BUG_876976
    public struct TestStructWithEnumArray
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public TestEnum[] ArrayOfEnum;
    }
#endif

    [STAThread]
    static int Main()
    {
        int retVal = 100;

        IntPtr ip;
        SomeTestStruct someTs = new SomeTestStruct();

#if BUG_876976
        Console.WriteLine("Testing SizeOf...");
        try
        {
            TestStructWithEnumArray s = new TestStructWithEnumArray();
            s.ArrayOfEnum = new TestEnum[3];
            s.ArrayOfEnum[0] = TestEnum.red;
            s.ArrayOfEnum[1] = TestEnum.green;
            s.ArrayOfEnum[2] = TestEnum.blue;
            Console.WriteLine("\tFirst call to SizeOf with TestStructWithEnumArray...");
            int retsize = Marshal.SizeOf(s.GetType());
            if (retsize != 12)
            {
                retVal = 0;
                Console.WriteLine("\t\tSize returned != 12");
                Console.WriteLine("\t\tReturned size = " + retsize);
            }

            retsize = 0;
            Console.WriteLine("\tSecond call to SizeOf with TestStructWithEnumArray...");
            retsize = Marshal.SizeOf(typeof(TestStructWithEnumArray));
            int genericRetsize = Marshal.SizeOf<TestStructWithEnumArray>();

            if (retsize != genericRetsize)
            {
                retVal = 0;
                Console.WriteLine("\t\tERROR: Generic and non generic versions of the API did not return the same size!");
            }

            if (retsize != 12)
            {
                retVal = 0;
                Console.WriteLine("\t\tSize returned != 12");
                Console.WriteLine("\t\tReturned size = " + retsize);
            }
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }
#endif

#if BUG_879268
        //////////////////////////////////////////////////////////////
        //StructureToPtr
        /////////////////////////////////////////////////////////////
        Console.WriteLine("Testing StructureToPtr...");
        Console.WriteLine("\tPassing IntPtr=IntPtr.Zero");
        ip = IntPtr.Zero;
        try
        {
            Marshal.StructureToPtr<SomeTestStruct>(someTs, ip, true);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("\n\tPassing structure=null");
        ip = new IntPtr(123);
        try
        {
            Marshal.StructureToPtr<Object>(null, ip, true);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }
#endif
        Console.WriteLine("\n\tPassing proper structure, proper IntPtr and fDeleteOld=true to hit remaining code paths");
        ip = Marshal.AllocHGlobal(Marshal.SizeOf(someTs));
        someTs.s = "something";
        Marshal.StructureToPtr(someTs, ip, false);
#if BUG_879268 
        Marshal.StructureToPtr(someTs, ip, true);
#endif
        Console.WriteLine("DONE testing StructureToPtr.");


        //////////////////////////////////////////////////////////////
        //PtrToStructure
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting PtrToStructure...");
#if BUG_878933
        Console.WriteLine("\tPassing IntPtr=IntPtr.Zero");
        ip = IntPtr.Zero;
        try
        {
            Marshal.PtrToStructure(ip, someTs);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("\n\tPassing structure=null");
        ip = new IntPtr(123);
        try
        {
            Marshal.PtrToStructure(ip, null);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }
#endif

        Console.WriteLine("\n\tPassing a value class to method override that expects a class and returns void");
        try
        {
            ip = new IntPtr(123);
            Marshal.PtrToStructure<SomeTestStruct>(ip, someTs);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ae.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("DONE testing PtrToStructure.");

#if BUG_879277
        //////////////////////////////////////////////////////////////
        //DestroyStructure
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting DestroyStructure...");
        Console.WriteLine("\tPassing IntPtr=IntPtr.Zero");
        ip = IntPtr.Zero;
        try
        {
            Marshal.DestroyStructure<SomeTestStruct>(ip);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("\n\tPassing structuretype=null");
        ip = new IntPtr(123);
        try
        {
            Marshal.DestroyStructure(ip, null);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("\n\tPassing structuretype that does not have layout i.e. it has AUTO layout");
        try
        {
            Marshal.DestroyStructure(ip, someTs_Auto.GetType());
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ae.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("\n\tPassing structuretype that does have layout i.e. the positive test case");
        ip = Marshal.AllocHGlobal(Marshal.SizeOf(someTs));
        someTs.s = null;
        Marshal.StructureToPtr(someTs, ip, false);
        Marshal.DestroyStructure<SomeTestStruct>(ip);

        Console.WriteLine("DONE testing DestroyStructure.");
#endif

        //////////////////////////////////////////////////////////////
        //SizeOf
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting SizeOf...");
        Console.WriteLine("\n\tPassing structure=null");
        try
        {
            Marshal.SizeOf(null);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

#if BUG_879234 
        Console.WriteLine("\n\tPassing structure that has no layout and CANNOT be marshaled");
        try
        {
            Marshal.SizeOf(typeof(StructWithFxdLPSTRSAFld));
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ae.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }
#endif

        Console.WriteLine("\n\tPassing structure that has layout and can be marshaled");
        Marshal.SizeOf(someTs.GetType());

        Console.WriteLine("DONE testing SizeOf.");

#if BUG_879276
        //////////////////////////////////////////////////////////////
        //UnsafeAddrOfPinnedArrayElement
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting UnsafeAddrOfPinnedArrayElement...");
        Console.WriteLine("\tPassing arr=null");
        try
        {
            Marshal.UnsafeAddrOfPinnedArrayElement<Object>(null, 123);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("DONE testing UnsafeAddrOfPinnedArrayElement.");
#endif

#if BUG_879276
        //////////////////////////////////////////////////////////////
        //OffsetOf
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting OffsetOf...");

        Console.WriteLine("\n\tMake sure that generic and non generic versions of the API returns the same offset.");
        IntPtr nonGenericOffsetCall = Marshal.OffsetOf(typeof(SomeTestStruct), "i");
        IntPtr genericOffsetCall = Marshal.OffsetOf<SomeTestStruct>("i");
        if (nonGenericOffsetCall != genericOffsetCall)
        {
            retVal = 0;
            Console.WriteLine("\t\tERROR: Generic and non generic versions of the API did not return the same offset!");
        }

        Console.WriteLine("\n\tPassing structure that has no layout and CANNOT be marshaled");
        try
        {
            Marshal.OffsetOf(typeof(StructWithFxdLPSTRSAFld), "Arr");
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ae.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("DONE testing OffsetOf.");
#endif

        //////////////////////////////////////////////////////////////
        //PtrToStringAnsi
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting PtrToStringAnsi...");
        Console.WriteLine("\n\tPassing ptr = null");
        try
        {
            Marshal.PtrToStringAnsi(IntPtr.Zero, 123);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("\n\tPassing len < 0 ");
        try
        {
            Marshal.PtrToStringAnsi(new IntPtr(123), -77);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ae.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("DONE testing PtrToStringAnsi.");

        //////////////////////////////////////////////////////////////
        //PtrToStringUni
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting PtrToStringUni...");
        Console.WriteLine("\n\tPassing len < 0 ");
        try
        {
            Marshal.PtrToStringUni(new IntPtr(123), -77);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentException ae)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ae.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("DONE testing PtrToStringUni.");

        //////////////////////////////////////////////////////////////
        //Copy
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting Copy...");
        Console.WriteLine("\n\tPassing psrc = null ");
        try
        {
            byte[] barr = null;
            Marshal.Copy(barr, 0, new IntPtr(123), 10);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("\n\tPassing startindex > numelem ");
        try
        {
            byte[] barr = new byte[2];
            Marshal.Copy(barr, 100, new IntPtr(123), 2);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentOutOfRangeException ae)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ae.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }

        Console.WriteLine("DONE testing Copy.");
        
#if ISSUE_6605
        //////////////////////////////////////////////////////////////
        //IsComObject
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting IsComObject...");
        Console.WriteLine("\n\tPassing Object = null ");
        try
        {
            Marshal.IsComObject(null);
            retVal = 0;
            Console.WriteLine("\t\tNO EXCEPTION THROWN! FAILED!");
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("\t\tCaught Expected Exception:\n\t\t\t" + ane.ToString());
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }
#endif
        Console.WriteLine("DONE testing IsComObject.");

        return retVal;
    }
}
