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
            str = string.Concat(_str, "");
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

    [StructLayout(LayoutKind.Sequential)]
    public class RecursiveTestClass
    {
        public RecursiveTestStruct s;
    }

    public struct RecursiveTestStruct
    {
        public RecursiveTestClass c;
    }

    class StructureTests
    {
        [DllImport("LayoutClassNative")]
        private static extern bool SimpleSeqLayoutClassByRef(SeqClass p);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleExpLayoutClassByRef(ExpClass p);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleBlittableSeqLayoutClassByRef(Blittable p);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleBlittableSeqLayoutClassByOutAttr([Out] Blittable p);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleNestedLayoutClassByValue(NestedLayout p);

        [DllImport("LayoutClassNative", EntryPoint = "Invalid")]
        private static extern void RecursiveNativeLayoutInvalid(RecursiveTestStruct str);

        public static void SequentialClass()
        {
            Console.WriteLine($"Running {nameof(SequentialClass)}...");

            string s = "before";
            var p = new SeqClass(0, false, s);
            Assert.IsTrue(SimpleSeqLayoutClassByRef(p));
        }

        public static void ExplicitClass()
        {
            Console.WriteLine($"Running {nameof(ExplicitClass)}...");

            var p = new ExpClass(DialogResult.None, 10);
            Assert.IsTrue(SimpleExpLayoutClassByRef(p));
        }

        public static void BlittableClass()
        {
            Console.WriteLine($"Running {nameof(BlittableClass)}...");

            Blittable p = new Blittable(10);
            Assert.IsTrue(SimpleBlittableSeqLayoutClassByRef(p));
        }

        public static void BlittableClassByOutAttr()
        {
            Console.WriteLine($"Running {nameof(BlittableClassByOutAttr)}...");

            int a = 10;
            int expected = a + 1;
            Blittable p = new Blittable(a);
            Assert.IsTrue(SimpleBlittableSeqLayoutClassByOutAttr(p));
            Assert.AreEqual(expected, p.a);
        }

        public static void NestedLayoutClass()
        {
            Console.WriteLine($"Running {nameof(NestedLayoutClass)}...");

            string s = "before";
            var p = new SeqClass(0, false, s);
            var target = new NestedLayout
            {
                value = p
            };
            Assert.IsTrue(SimpleNestedLayoutClassByValue(target));
        }

        public static void RecursiveNativeLayout()
        {
            Console.WriteLine($"Running {nameof(RecursiveNativeLayout)}...");

            Assert.Throws<TypeLoadException>(() => RecursiveNativeLayoutInvalid(new RecursiveTestStruct()));
        }

        public static int Main(string[] argv)
        {
            try
            {
                SequentialClass();
                ExplicitClass();
                BlittableClass();
                BlittableClassByOutAttr();
                NestedLayoutClass();
                RecursiveNativeLayout();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
