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
        [MarshalAs(UnmanagedType.BStr)]
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
        Object o;
        SomeTestStruct someTs = new SomeTestStruct();
        StructWithFxdLPSTRSAFld someTs_FxdLPSTR = new StructWithFxdLPSTRSAFld();

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
            Marshal.SizeOf(someTs_FxdLPSTR.GetType());
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
            Marshal.OffsetOf(someTs_FxdLPSTR.GetType(), "Arr");
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

        //////////////////////////////////////////////////////////////
        //GetComInterfaceForObject
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting GetComInterfaceForObject...");
#if BUG_878933
        Console.WriteLine("\n\tPassing Object = null ");
        try
        {
            Marshal.GetComInterfaceForObject(null, null);
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

        Console.WriteLine("DONE testing GetComInterfaceForObject.");
#endif

        //////////////////////////////////////////////////////////////
        //GetObjectForIUnknown
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting GetObjectForIUnknown...");
#if BUG_879254
        Console.WriteLine("\n\tPassing IntPtr = IntPtr.Zero ");
        try
        {
            Marshal.GetObjectForIUnknown(IntPtr.Zero);
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

        Console.WriteLine("DONE testing GetObjectForIUnknown.");
#endif
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

        Console.WriteLine("DONE testing IsComObject.");

        //////////////////////////////////////////////////////////////
        //QueryInterface
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting QueryInterface...");
#if BUG_878933
        Console.WriteLine("\n\tPassing IUnkn = IntPtr.Zero");
        try
        {
            IntPtr temp = IntPtr.Zero;
            Guid g = Guid.Empty;
            Marshal.QueryInterface(IntPtr.Zero, ref g, out temp);
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

        Console.WriteLine("DONE testing QueryInterface.");

        //////////////////////////////////////////////////////////////
        //AddRef
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting AddRef...");
        Console.WriteLine("\n\tPassing IUnkn = IntPtr.Zero");
        try
        {
            Marshal.AddRef(IntPtr.Zero);
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

        Console.WriteLine("DONE testing AddRef.");

        //////////////////////////////////////////////////////////////
        //Release
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting Release...");
        Console.WriteLine("\n\tPassing IUnkn = IntPtr.Zero");
        try
        {
            Marshal.Release(IntPtr.Zero);
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

        Console.WriteLine("DONE testing Release.");

#if BUG_879276
        //////////////////////////////////////////////////////////////
        //GetNativeVariantForObject
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting GetNativeVariantForObject...");
        Console.WriteLine("\n\tPassing pDstNativeVariant = IntPtr.Zero");
        try
        {
            Marshal.GetNativeVariantForObject("Some Object", IntPtr.Zero);
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

        Console.WriteLine("DONE testing GetNativeVariantForObject.");

        //////////////////////////////////////////////////////////////
        //GetObjectForNativeVariant
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting GetObjectForNativeVariant...");
        Console.WriteLine("\n\tPassing pSrcNativeVariant = IntPtr.Zero");
        try
        {
            Marshal.GetObjectForNativeVariant<Object>(IntPtr.Zero);
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

        Console.WriteLine("DONE testing GetObjectForNativeVariant.");
#endif

#if BUG_879277
        //////////////////////////////////////////////////////////////
        //GetObjectsForNativeVariants
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting GetObjectsForNativeVariants...");
        Console.WriteLine("\n\tPassing aSrcNativeVariant = IntPtr.Zero");
        try
        {
            Marshal.GetObjectsForNativeVariants<Object>(IntPtr.Zero, 0);
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

        Console.WriteLine("\n\tPassing cVars < 0");
        try
        {
            Marshal.GetObjectsForNativeVariants(new IntPtr(123), -77);
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

        Console.WriteLine("\n\tTesting the generic version of the API");
        Variant v = new Variant();
        v.vt = 0;
        v.wReserved1 = 0;
        v.wReserved2 = 0;
        v.wReserved3 = 0;
        v.bstrVal = IntPtr.Zero;
        v.pRecInfo = IntPtr.Zero;
        IntPtr parray = Marshal.AllocHGlobal(1 * Marshal.SizeOf(v));
        Marshal.GetNativeVariantForObject<ushort>(0, parray);

        ushort[] variantsArrayGeneric = Marshal.GetObjectsForNativeVariants<ushort>(parray, 1);
        Object[] variantsArray = Marshal.GetObjectsForNativeVariants(parray, 1);

        if (variantsArrayGeneric.Length != variantsArray.Length)
        {
            retVal = 0;
            Console.WriteLine("\t\tGeneric and non generic version calls returned different sized arrays\n\t\t\t");
        }

        for (int i = 0; i < variantsArray.Length; i++)
        {
            if ((ushort)variantsArray[i] != variantsArrayGeneric[i])
            {
                retVal = 0;
                Console.WriteLine("\t\tGeneric and non generic version calls returned different arrays\n\t\t\t");
            }
        }

        bool thrown = false;
        try
        {
            String[] marray = Marshal.GetObjectsForNativeVariants<String>(parray, 1);
        }
        catch (InvalidCastException e)
        {
            thrown = true;
            Console.WriteLine("Expected invalid cast exception was thrown.");
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }
        if (thrown != true)
        {
            Console.WriteLine("Expected invalid cast exception was NOT thrown.");
            retVal = 0;
        }

        thrown = false;
        try
        {
            int[] marray = Marshal.GetObjectsForNativeVariants<int>(parray, 1);
        }
        catch (InvalidCastException e)
        {
            thrown = true;
            Console.WriteLine("Expected invalid cast exception was thrown.");
        }
        catch (Exception e)
        {
            retVal = 0;
            Console.WriteLine("\t\tUNEXPECTED EXCEPTION:\n\t\t\t" + e.ToString());
        }
        if (thrown != true)
        {
            Console.WriteLine("Expected invalid cast exception was NOT thrown.");
            retVal = 0;
        }

        Console.WriteLine("DONE testing GetObjectsForNativeVariants.");
#endif

#if BUG_879277
        //////////////////////////////////////////////////////////////
        //GetStartComSlot
        /////////////////////////////////////////////////////////////
        Console.WriteLine("\nTesting GetStartComSlot...");
        Console.WriteLine("\n\tPassing t = null");
        try
        {
            Marshal.GetStartComSlot(null);
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

        Console.WriteLine("DONE testing GetStartComSlot.");
#endif

        Console.WriteLine((retVal == 0) ? "\nFAILED!" : "\nPASSED!");
        return retVal;
    }
}