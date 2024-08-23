using System;
using System.Security;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

namespace LayoutClass
{
    [StructLayout(LayoutKind.Sequential)]
    public class EmptyBase
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    public class EmptyBase2 : EmptyBase
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SeqDerivedClass : EmptyBase
    {
        public int a;

        public SeqDerivedClass(int _a)
        {
            a = _a;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SeqDerivedClass2 : EmptyBase2
    {
        public int a;

        public SeqDerivedClass2(int _a)
        {
            a = _a;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public sealed class SeqClass
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

    [StructLayout(LayoutKind.Sequential)]
    public sealed class SealedBlittable
    {
        public int a;

        public SealedBlittable(int _a)
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

    [SkipOnMono("needs triage")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/81673", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public class LayoutClassTest
    {
        private const string SimpleBlittableSeqLayoutClass_UpdateField = nameof(SimpleBlittableSeqLayoutClass_UpdateField);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleSeqLayoutClassByRef(SeqClass p);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleSeqLayoutClassByRefNull([In, Out] SeqClass p);

        [DllImport("LayoutClassNative")]
        private static extern bool DerivedSeqLayoutClassByRef(EmptyBase p, int expected);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleExpLayoutClassByRef(ExpClass p);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleBlittableSeqLayoutClass_Null(Blittable p);

        [DllImport("LayoutClassNative", EntryPoint = SimpleBlittableSeqLayoutClass_UpdateField)]
        private static extern bool SimpleBlittableSeqLayoutClassByRef(Blittable p);

        [DllImport("LayoutClassNative", EntryPoint = SimpleBlittableSeqLayoutClass_UpdateField)]
        private static extern bool SimpleBlittableSeqLayoutClassByInAttr([In] Blittable p);

        [DllImport("LayoutClassNative", EntryPoint = SimpleBlittableSeqLayoutClass_UpdateField)]
        private static extern bool SimpleBlittableSeqLayoutClassByOutAttr([Out] Blittable p);

        [DllImport("LayoutClassNative", EntryPoint = SimpleBlittableSeqLayoutClass_UpdateField)]
        private static extern bool SealedBlittableSeqLayoutClassByRef(SealedBlittable p);

        [DllImport("LayoutClassNative", EntryPoint = SimpleBlittableSeqLayoutClass_UpdateField)]
        private static extern bool SealedBlittableSeqLayoutClassByInAttr([In] SealedBlittable p);

        [DllImport("LayoutClassNative", EntryPoint = SimpleBlittableSeqLayoutClass_UpdateField)]
        private static extern bool SealedBlittableSeqLayoutClassByOutAttr([Out] SealedBlittable p);

        [DllImport("LayoutClassNative")]
        private static extern bool PointersEqual(SealedBlittable obj, ref int field);

        [DllImport("LayoutClassNative")]
        private static extern bool PointersEqual(Blittable obj, ref int field);

        [DllImport("LayoutClassNative")]
        private static extern bool SimpleNestedLayoutClassByValue(NestedLayout p);

        [DllImport("LayoutClassNative", EntryPoint = "Invalid")]
        private static extern void RecursiveNativeLayoutInvalid(RecursiveTestStruct str);

        [Fact]
        public static void SequentialClass()
        {
            Console.WriteLine($"Running {nameof(SequentialClass)}...");

            string s = "before";
            var p = new SeqClass(0, false, s);
            Assert.True(SimpleSeqLayoutClassByRef(p));
        }

        [Fact]
        public static void SequentialClassNull()
        {
            Console.WriteLine($"Running {nameof(SequentialClassNull)}...");

            Assert.True(SimpleSeqLayoutClassByRefNull(null));
        }

        [Fact]
        public static void DerivedClassWithEmptyBase()
        {
            Console.WriteLine($"Running {nameof(DerivedClassWithEmptyBase)}...");

            string s = "before";
            Assert.True(DerivedSeqLayoutClassByRef(new SeqDerivedClass(42), 42));
            Assert.True(DerivedSeqLayoutClassByRef(new SeqDerivedClass2(42), 42));
        }

        [Fact]
        public static void ExplicitClass()
        {
            Console.WriteLine($"Running {nameof(ExplicitClass)}...");

            var p = new ExpClass(DialogResult.None, 10);
            Assert.True(SimpleExpLayoutClassByRef(p));
        }

        private static void ValidateBlittableClassInOut(Func<Blittable, bool> pinvoke)
        {
            int a = 10;
            int expected = a + 1;
            Blittable p = new Blittable(a);
            Assert.True(pinvoke(p));
            Assert.Equal(expected, p.a);
        }

        [Fact]
        public static void BlittableClass()
        {
            // [Compat] Marshalled with [In, Out] behaviour by default
            Console.WriteLine($"Running {nameof(BlittableClass)}...");
            ValidateBlittableClassInOut(SimpleBlittableSeqLayoutClassByRef);
        }

        [Fact]
        public static void BlittableClassNull()
        {
            // [Compat] Marshalled with [In, Out] behaviour by default
            Console.WriteLine($"Running {nameof(BlittableClassNull)}...");
            Assert.True(SimpleBlittableSeqLayoutClass_Null(null));
        }

        [Fact]
        public static void BlittableClassByInAttr()
        {
            // [Compat] Marshalled with [In, Out] behaviour even when only [In] is specified
            Console.WriteLine($"Running {nameof(BlittableClassByInAttr)}...");
            ValidateBlittableClassInOut(SimpleBlittableSeqLayoutClassByInAttr);
        }

        [Fact]
        public static void BlittableClassByOutAttr()
        {
            // [Compat] Marshalled with [In, Out] behaviour even when only [Out] is specified
            Console.WriteLine($"Running {nameof(BlittableClassByOutAttr)}...");
            ValidateBlittableClassInOut(SimpleBlittableSeqLayoutClassByOutAttr);
        }

        private static void ValidateSealedBlittableClassInOut(Func<SealedBlittable, bool> pinvoke)
        {
            int a = 10;
            int expected = a + 1;
            SealedBlittable p = new SealedBlittable(a);
            Assert.True(pinvoke(p));
            Assert.Equal(expected, p.a);
        }

        [Fact]
        public static void SealedBlittableClass()
        {
            // [Compat] Marshalled with [In, Out] behaviour by default
            Console.WriteLine($"Running {nameof(SealedBlittableClass)}...");
            ValidateSealedBlittableClassInOut(SealedBlittableSeqLayoutClassByRef);
        }

        [Fact]
        public static void SealedBlittableClassByInAttr()
        {
            // [Compat] Marshalled with [In, Out] behaviour even when only [In] is specified
            Console.WriteLine($"Running {nameof(SealedBlittableClassByOutAttr)}...");
            ValidateSealedBlittableClassInOut(SealedBlittableSeqLayoutClassByInAttr);
        }

        [Fact]
        public static void SealedBlittableClassByOutAttr()
        {
            // [Compat] Marshalled with [In, Out] behaviour even when only [Out] is specified
            Console.WriteLine($"Running {nameof(SealedBlittableClassByOutAttr)}...");
            ValidateSealedBlittableClassInOut(SealedBlittableSeqLayoutClassByOutAttr);
        }

        [Fact]
        public static void SealedBlittablePinned()
        {
            Console.WriteLine($"Running {nameof(SealedBlittablePinned)}...");
            var blittable = new SealedBlittable(1);
            Assert.True(PointersEqual(blittable, ref blittable.a));
        }

        [Fact]
        public static void BlittablePinned()
        {
            Console.WriteLine($"Running {nameof(BlittablePinned)}...");
            var blittable = new Blittable(1);
            Assert.True(PointersEqual(blittable, ref blittable.a));
        }

        [Fact]
        public static void NestedLayoutClass()
        {
            Console.WriteLine($"Running {nameof(NestedLayoutClass)}...");

            string s = "before";
            var p = new SeqClass(0, false, s);
            var target = new NestedLayout
            {
                value = p
            };
            Assert.True(SimpleNestedLayoutClassByValue(target));
        }

        [Fact]
        public static void RecursiveNativeLayout()
        {
            Console.WriteLine($"Running {nameof(RecursiveNativeLayout)}...");

            Assert.Throws<TypeLoadException>(() => RecursiveNativeLayoutInvalid(new RecursiveTestStruct()));
        }
    }
}
