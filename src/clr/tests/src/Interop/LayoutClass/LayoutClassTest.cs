using System;
using System.Security;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

namespace PInvokeTests
{
    [StructLayout(LayoutKind.Sequential)]
    public class SeqClass
    {
        public int a;
        public bool b;
        public string str;

        public SeqClass(int _a, bool _b, string _str)
        {
            a = _a;
            b = _b;
            str = String.Concat(_str, "");
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ExpClass
    {
        [FieldOffset(0)]
        public DialogResult type;

        [FieldOffset(8)]
        public int i;

        [FieldOffset(8)]
        public bool b;

        [FieldOffset(8)]
        public double c;

        public ExpClass(DialogResult t, int num)
        {
            type = t;
            b = false;
            c = num;
            i = num;
        }
        public ExpClass(DialogResult t, double dnum)
        {
            type = t;
            b = false;
            i = 0;
            c = dnum;
        }
        public ExpClass(DialogResult t, bool bnum)
        {
            type = t;
            i = 0;
            c = 0;
            b = bnum;
        }
     }

    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2
    }

    
    [StructLayout(LayoutKind.Sequential)]
    public class Blittable
    {
        public int a;

        public Blittable(int _a)
        {
            a = _a;
        }
    }

    public struct NestedLayout
    {
        public SeqClass value;
    }

    class StructureTests
    {
        [DllImport("LayoutClassNative")]
        private static extern bool SimpleSeqLayoutClassByRef(SeqClass p);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleExpLayoutClassByRef(ExpClass p);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleNestedLayoutClassByValue(NestedLayout p);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleBlittableSeqLayoutClassByRef(Blittable p);

        public static bool SequentialClass()
        {
            string s = "before";
            bool retval = true;
            SeqClass p = new SeqClass(0, false, s);

            TestFramework.BeginScenario("Test #1 Pass a sequential layout class.");

            try
            {
                retval = SimpleSeqLayoutClassByRef(p);

                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->SequentialClass : Unexpected error occured on unmanaged side");
                    return false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("04", "Unexpected exception: " + e.ToString());
                retval = false;
            }

            return retval;
        }

        public static bool ExplicitClass()
        {
            ExpClass p;
            bool retval = false;

            TestFramework.BeginScenario("Test #2 Pass an explicit layout class.");

            try
            {
                p = new ExpClass(DialogResult.None, 10);
                retval = SimpleExpLayoutClassByRef(p);

                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->ExplicitClass : Unexpected error occured on unmanaged side");
                    return false;
                }

            }
            catch (Exception e)
            {
                TestFramework.LogError("03", "Unexpected exception: " + e.ToString());
                retval = false;
            }

            return retval;
        }

        public static bool BlittableClass()
        {
            bool retval = true;
            Blittable p = new Blittable(10);

            TestFramework.BeginScenario("Test #3 Pass a blittable sequential layout class.");

            try
            {
                retval = SimpleBlittableSeqLayoutClassByRef(p);

                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->Blittable : Unexpected error occured on unmanaged side");
                    return false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("04", "Unexpected exception: " + e.ToString());
                retval = false;
            }

            return retval;
        }
        
        public static bool NestedLayoutClass()
        {
            string s = "before";
            bool retval = true;
            SeqClass p = new SeqClass(0, false, s);
            NestedLayout target = new NestedLayout
            {
                value = p
            };

            TestFramework.BeginScenario("Test #4 Nested sequential layout class in a structure.");

            try
            {
                retval = SimpleNestedLayoutClassByValue(target);

                if (retval == false)
                {
                    TestFramework.LogError("01", "PInvokeTests->NestedLayoutClass : Unexpected error occured on unmanaged side");
                    return false;
                }
            }
            catch (Exception e)
            {
                TestFramework.LogError("04", "Unexpected exception: " + e.ToString());
                retval = false;
            }

            return retval;
        }

        public static int Main(string[] argv)
        {
            bool retVal = true;

            retVal = retVal && SequentialClass();
            retVal = retVal && ExplicitClass();
            retVal = retVal && BlittableClass();
            retVal = retVal && NestedLayoutClass();

            return (retVal ? 100 : 101);
        }


    }
}
