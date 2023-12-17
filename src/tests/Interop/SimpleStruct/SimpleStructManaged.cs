using System;
using System.Security;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

namespace PInvokeTests
{
    #region structure def

    [SecuritySafeCritical]
    [StructLayout(LayoutKind.Sequential)]
    public struct Sstr
    {
        public int a;
        public bool b;
        public string str;

        public Sstr(int _a, bool _b, string _str)
        {
            a = _a;
            b = _b;
            str = String.Concat(_str, "");
        }
    }

    //Using this structure for pass by value scenario
    //because we don't support returning a structure
    //containing a non-blittable type like string.

    [SecuritySafeCritical]
    [StructLayout(LayoutKind.Sequential)]
    public struct Sstr_simple
    {
        public int a;
        public bool b;
        public double c;

        public Sstr_simple(int _a, bool _b, double _c)
        {
            a = _a;
            b = _b;
            c = _c;
        }
    }

    [SecuritySafeCritical]
    [StructLayout(LayoutKind.Explicit)]
    public struct ExplStruct
    {
        [FieldOffset(0)]
        public DialogResult type;

        [FieldOffset(8)]
        public int i;

        [FieldOffset(8)]
        public bool b;

        [FieldOffset(8)]
        public double c;

        public ExplStruct(DialogResult t, int num)
        {
            type = t;
            b = false;
            c = num;
            i = num;
        }
        public ExplStruct(DialogResult t, double dnum)
        {
            type = t;
            b = false;
            i = 0;
            c = dnum;
        }
        public ExplStruct(DialogResult t, bool bnum)
        {
            type = t;
            i = 0;
            c = 0;
            b = bnum;
        }
     }

    [StructLayout(LayoutKind.Auto)]
    public struct AutoStruct
    {
        public int i;
        public double d;
    }

    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2
    }
    #endregion

    public class StructureTests
    {
        #region direct Pinvoke declarartions

        #region cdecl
        //Simple struct - sequential layout by ref
        [DllImport("SimpleStructNative", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool CdeclSimpleStructByRef(ref Sstr p);

        [DllImport("SimpleStructNative", EntryPoint = "GetFptr")]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        public static extern CdeclSimpleStructByRefDelegate GetFptrCdeclSimpleStructByRef(int i);

        //Simple struct - sequential layout by value
        [DllImport("SimpleStructNative", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CdeclSimpleStruct(Sstr_simple p, ref bool retval);

        [DllImport("SimpleStructNative", EntryPoint = "GetFptr")]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        public static extern CdeclSimpleStructDelegate GetFptrCdeclSimpleStruct(int i);

        //Simple struct - explicit layout by value
        [DllImport("SimpleStructNative", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CdeclSimpleExplStruct(ExplStruct p, ref bool retval);

        [DllImport("SimpleStructNative", EntryPoint = "GetFptr")]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        public static extern CdeclSimpleExplStructDelegate GetFptrCdeclSimpleExplStruct(int i);

        //Simple struct - explicit layout by ref
        [DllImport("SimpleStructNative", EntryPoint = "GetFptr")]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        public static extern CdeclSimpleExplStructByRefDelegate GetFptrCdeclSimpleExplStructByRef(int i);

        [DllImport("SimpleStructNative", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool CdeclSimpleExplStructByRef(ref ExplStruct p);

        [DllImport("SimpleStructNative")]
        private static extern void Invalid(AutoStruct s);
        [DllImport("SimpleStructNative")]
        private static extern AutoStruct InvalidReturn();

        #endregion

        #endregion

        #region delegate pinvoke
        //Simple struct - sequential layout by value

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CdeclSimpleStructDelegate(Sstr_simple p, ref bool retval);

        //Simple struct - explicit layout by value

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CdeclSimpleExplStructDelegate(ExplStruct p, ref bool retval);

        //Simple struct - sequential layout by ref

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool CdeclSimpleStructByRefDelegate(ref Sstr p);

        //Simple struct - explicit layout by ref

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool CdeclSimpleExplStructByRefDelegate(ref ExplStruct p);

        #endregion

        #region reverse pinvoke

        #endregion

        #region public methods related to pinvoke declrarations
        //Simple struct - sequential layout by ref
        [System.Security.SecuritySafeCritical]
        public static bool DoCdeclSimpleStructByRef(ref Sstr p)
        {
            return CdeclSimpleStructByRef(ref p);
        }

        //Simple struct - explicit by ref
        [System.Security.SecuritySafeCritical]
        public static bool DoCdeclSimpleExplStructByRef(ref ExplStruct p)
        {
            return CdeclSimpleExplStructByRef(ref p);
        }

        //Simple struct - sequential layout by value
        [System.Security.SecuritySafeCritical]
        public static Sstr_simple DoCdeclSimpleStruct(Sstr_simple p, ref bool retval)
        {
            IntPtr st = CdeclSimpleStruct(p, ref retval);
            Sstr_simple simple = Marshal.PtrToStructure<Sstr_simple>(st);
            return simple;
        }

        //Simple Struct - Explicit layout by value
        [System.Security.SecuritySafeCritical]
        public static ExplStruct DoCdeclSimpleExplStruct(ExplStruct p, ref bool retval)
        {
            IntPtr st = CdeclSimpleExplStruct(p, ref retval);
            ExplStruct simple = Marshal.PtrToStructure<ExplStruct>(st);
            return simple;
        }

        #endregion

        #region test methods
        //Simple sequential struct by reference testcase
        [System.Security.SecuritySafeCritical]
        public static bool PosTest1()
        {
            string s = "before";
            string changedValue = "after";
            bool retval = true;
            Sstr p = new Sstr(0, false, s);

            TestFramework.BeginScenario("Test #1 (Roundtrip of a simple structre by reference. Verify that values updated on unmanaged side reflect on managed side)");

            //Direct pinvoke

            //cdecl calling convention.
            try
            {
                TestFramework.LogInformation(" Case 2: Direct p/invoke cdecl calling convention");
                retval = DoCdeclSimpleStructByRef(ref p);

                if ((p.a != 100) || (!p.b) || (!p.str.Equals(changedValue)))
                {
                    Console.WriteLine("\nExpected values:\n SimpleStruct->a=" + 100 + "\nSimpleStruct->b=TRUE\n" + "SimpleStruct->str=after\n");
                    Console.WriteLine("\nActual values:\n SimpleStruct->a=" + p.a + "\nSimpleStruct->b=" + p.b + "\nSimpleStruct->str=" + p.str + "\n");
                    TestFramework.LogError("03", "PInvokeTests->PosTest1 : Returned values are different from expected values");
                    retval = false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("04", "Unexpected exception: " + e.ToString());
                retval = false;
            }

            //Delegate pinvoke

            //cdecl
            try
            {
                TestFramework.LogInformation(" Case 4: Delegate p/invoke - cdecl calling convention");
                CdeclSimpleStructByRefDelegate std = GetFptrCdeclSimpleStructByRef(14);

                retval = std(ref p);

                if ((p.a != 100) || (!p.b) || (!p.str.Equals(changedValue)))
                {
                    Console.WriteLine("\nExpected values:\n SimpleStruct->a=" + 100 + "\nSimpleStruct->b=TRUE\n" + "SimpleStruct->str=after\n");
                    Console.WriteLine("\nActual values:\n SimpleStruct->a=" + p.a + "\nSimpleStruct->b=" + p.b + "\nSimpleStruct->str=" + p.str + "\n");
                    TestFramework.LogError("01", "PInvokeTests->PosTest1 : Returned values are different from expected values");
                    retval = false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("02", "Unexpected exception: " + e.ToString());
                retval = false;
            }


            return retval;
        }

        //Simple Sequential struct by value
        [System.Security.SecuritySafeCritical]
        public static bool PosTest2()
        {
            string s = "Before";
            bool retval = true;
            double d = 3.142;
            Sstr p = new Sstr(100, false, s);

            TestFramework.BeginScenario("\n\nTest #2 (Roundtrip of a simple structre by value. Verify that values updated on unmanaged side reflect on managed side)");
            //direct pinvoke

           // //cdecl calling convention
            try
            {
                TestFramework.LogInformation(" Case 2: Direct p/invoke cdecl calling convention");
                Sstr_simple simple = new Sstr_simple(100, false, d);
                simple = DoCdeclSimpleStruct(simple, ref retval);

                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->PosTest2 : values of passed in structure not matched with expected once on unmanaged side.");
                    return false;
                }
                if ((simple.a != 101) || (!simple.b) || (simple.c != 10.11))
                {
                    Console.WriteLine("\nExpected values:\n SimpleStruct->a=101\nSimpleStruct->b=TRUE\nSimpleStruct->c=10.11\n");
                    Console.WriteLine("\nActual values:\n SimpleStruct->a=" + simple.a + "\nSimpleStruct->b=" + simple.b + "\nSimpleStruct->c=" + simple.c + "\n");
                    TestFramework.LogError("02", "PInvokeTests->PosTest2 : Returned values are different from expected values");
                    retval = false;
                }
            }
            catch (Exception e)
            {

                TestFramework.LogError("03", "Unexpected exception: " + e.ToString());
                retval = false;
            }

           // //delegate pinvoke

           // //cdecl calling convention
            try
            {
                TestFramework.LogInformation(" Case 4: Delegate p/invoke cdecl calling convention");
                Sstr_simple simple = new Sstr_simple(100, false, d);
                CdeclSimpleStructDelegate std = GetFptrCdeclSimpleStruct(16);

                IntPtr st = std(simple, ref retval);
                simple = Marshal.PtrToStructure<Sstr_simple>(st);


                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->PosTest2 : values of passed in structure not matched with expected once on unmanaged side.");
                    return false;
                }
                if ((simple.a != 101) || (!simple.b) || (simple.c != 10.11))
                {
                    Console.WriteLine("\nExpected values:\n SimpleStruct->a=101\nSimpleStruct->b=TRUE\nSimpleStruct->c=10.11\n");
                    Console.WriteLine("\nActual values:\n SimpleStruct->a=" + simple.a + "\nSimpleStruct->b=" + simple.b + "\nSimpleStruct->c=" + simple.c + "\n");
                    TestFramework.LogError("02", "PInvokeTests->PosTest2 : Returned values are different from expected values");
                    retval = false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("03", "Unexpected exception: " + e.ToString());
                retval = false;
            }
            return retval;
        }

        //Simple struct explicit layout by reference.
        [System.Security.SecuritySafeCritical]
        public static bool PosTest3()
        {
            ExplStruct p;
            bool retval = false;

            TestFramework.BeginScenario("\n\nTest #3 (Roundtrip of a simple structre (explicit layout) by reference. Verify that values updated on unmanaged side reflect on managed side)");
            //direct pinvoke

            //cdecl
            try
            {
                p = new ExplStruct(DialogResult.None, 10);
                TestFramework.LogInformation(" Case 2: Direct p/invoke cdecl calling convention");
                retval = DoCdeclSimpleExplStructByRef(ref p);

                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->PosTest3 : Unexpected error occurred on unmanaged side");
                    return false;
                }
                if ((p.type != DialogResult.OK) || (!p.b))
                {
                    Console.WriteLine("\nExpected values:\n SimpleStruct->type=1\nSimpleStruct->b=TRUE\n");
                    Console.WriteLine("\nActual values:\n SimpleStruct->type=" + p.type + "\nSimpleStruct->b="  + p.b);
                    TestFramework.LogError("02", "PInvokeTests->PosTest3 : Returned values are different from expected values");
                    retval = false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("03", "Unexpected exception: " + e.ToString());
                retval = false;
            }

            //Delegate pinvoke --- cdecl
            try
            {
                p = new ExplStruct(DialogResult.None, 10);
                TestFramework.LogInformation(" Case 4: Delegate p/invoke cdecl calling convention");
                CdeclSimpleExplStructByRefDelegate std = GetFptrCdeclSimpleExplStructByRef(18);

                retval = std(ref p);

                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->PosTest3 : Unexpected error occurred on unmanaged side");
                    return false;
                }
                if ((p.type != DialogResult.OK) || (!p.b))
                {
                    Console.WriteLine("\nExpected values:\n SimpleStruct->type=1\nSimpleStruct->b=TRUE\n");
                    Console.WriteLine("\nActual values:\n SimpleStruct->type=" + p.type + "\nSimpleStruct->b=" + p.b);
                    TestFramework.LogError("02", "PInvokeTests->PosTest3 : Returned values are different from expected values");
                    retval = false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("03", "Unexpected exception: " + e.ToString());
                retval = false;
            }


            return retval;
        }

        //Simple struct explicit layout by value.
        [System.Security.SecuritySafeCritical]
        public static bool PosTest4()
        {
            ExplStruct p;
            bool retval = false;

            TestFramework.BeginScenario("\n\nTest #4 (Roundtrip of a simple structre (Explicit layout) by value. Verify that values updated on unmanaged side reflect on managed side)");
            //direct pinvoke

            //cdecl
            try
            {
                p = new ExplStruct(DialogResult.OK, false);
                TestFramework.LogInformation(" Case 2: Direct p/invoke cdecl calling convention");
                p = DoCdeclSimpleExplStruct(p, ref retval);
                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->PosTest2 : values of passed in structure not matched with expected once on unmanaged side.");
                    return false;
                }
                if ((p.type != DialogResult.Cancel) || (p.c != 3.142))
                {
                    Console.WriteLine("\nExpected values:\n SimpleStruct->a=2\nSimpleStruct->c=3.142\n");
                    Console.WriteLine("\nActual values:\n SimpleStruct->a=" + p.type + "\nSimpleStruct->c=" + p.c + "\n");
                    TestFramework.LogError("02", "PInvokeTests->PosTest4 : Returned values are different from expected values");
                    retval = false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("03", "Unexpected exception: " + e.ToString());
                retval = false;
            }

            //delegate pinvoke

            //cdecl
            try
            {
                p = new ExplStruct(DialogResult.OK, false);
                TestFramework.LogInformation(" Case 4: Direct p/invoke cdecl calling convention");

                CdeclSimpleExplStructDelegate std = GetFptrCdeclSimpleExplStruct(20);

                IntPtr st = std(p, ref retval);
                p = Marshal.PtrToStructure<ExplStruct>(st);

                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->PosTest2 : values of passed in structure not matched with expected once on unmanaged side.");
                    return false;
                }
                if ((p.type != DialogResult.Cancel) || (p.c != 3.142))
                {
                    Console.WriteLine("\nExpected values:\n SimpleStruct->a=2\nSimpleStruct->c=3.142\n");
                    Console.WriteLine("\nActual values:\n SimpleStruct->a=" + p.type + "\nSimpleStruct->c=" + p.c + "\n");
                    TestFramework.LogError("02", "PInvokeTests->PosTest4 : Returned values are different from expected values");
                    retval = false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("03", "Unexpected exception: " + e.ToString());
                retval = false;
            }
            return retval;
        }

        public static bool AutoStructNegativeTest()
        {
            bool pass = false;
            try
            {
                Invalid(new AutoStruct());
            }
            catch (MarshalDirectiveException)
            {
                pass = true;
            }
            catch (Exception)
            {
            }
            try
            {
                _ = InvalidReturn();
            }
            catch (MarshalDirectiveException)
            {
                pass &= true;
            }
            catch (Exception)
            {
            }
            return pass;
        }

        #endregion

        [Fact]
        [OuterLoop]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
        public static int TestEntryPoint()
        {
            bool retVal = true;

            retVal = retVal && PosTest1();
            retVal = retVal && PosTest2();
            retVal = retVal && PosTest3();
            retVal = retVal && AutoStructNegativeTest();

            // https://github.com/dotnet/runtime/issues/5552
            // retVal = retVal && PosTest4();

            if (!retVal)
                Console.WriteLine("FAIL");
            else
                Console.WriteLine("PASS");
            return (retVal ? 100 : 101);
        }


    }
}
