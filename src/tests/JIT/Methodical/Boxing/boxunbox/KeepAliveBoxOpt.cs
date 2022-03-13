using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public class KeepAliveBoxFieldsTest
{
    private static string _failedTest;
    private static string _failedFieldName;

    public static int Main()
    {
        TestLocalsOneBlock();
        Reset();
        TestImplicitByrefsOneBlock();
        Reset();
        TestExplicitByrefsOneBlock();
        Reset();
        TestClassWrapperOneBlock();
        Reset();
        TestFinallyLocals();
        Reset();
        TestThrowCatchFinallyLocals();
        Reset();

        return 100;
    }

    public static void TestLocalsOneBlock()
    {
        var s1 = new ComplexStructWithExplicitLayout();
        s1.ObjectField = new Reporter("s1.ObjectField", nameof(TestLocalsOneBlock));
        s1.AnotherObjectField = new Reporter("s1.AnotherObjectField", nameof(TestLocalsOneBlock));
        s1.StructWithRef.ObjectField = new Reporter("s1.StructWithRef.ObjectField", nameof(TestLocalsOneBlock));
        var s2 = new SimpleStructWithExplicitLayout();
        s2.ObjectField = new Reporter("s2.ObjectField", nameof(TestLocalsOneBlock));
        var s3 = new SimpleStructWithAutoLayout();
        s3.ObjectField = new Reporter("s3.ObjectField", nameof(TestLocalsOneBlock));
        s3.AnotherObjectField = new Reporter("s3.AnotherObjectField", nameof(TestLocalsOneBlock));
        var s4 = new SingleObjectStruct();
        s4.ObjectField = new Reporter("s4.ObjectField", nameof(TestLocalsOneBlock));
        var s5 = new StructWithoutObjectFields();
        var s6src = new SingleObjectStruct();
        s6src.ObjectField = new Reporter("s6src.ObjectField", nameof(TestLocalsOneBlock));
        var s6 = new SingleObjectStruct?(s6src);

        GC.Collect(2, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();

        CheckSuccess();

        GC.KeepAlive(s1);
        GC.KeepAlive(s2);
        GC.KeepAlive(s3);
        GC.KeepAlive(s4);
        GC.KeepAlive(s5);
        GC.KeepAlive(s6);
    }

    public static void TestImplicitByrefsOneBlock()
    {
        var s1 = new ComplexStructWithExplicitLayout();
        s1.ObjectField = new Reporter("s1.ObjectField", nameof(TestImplicitByrefsOneBlock));
        s1.AnotherObjectField = new Reporter("s1.AnotherObjectField", nameof(TestImplicitByrefsOneBlock));
        s1.StructWithRef.ObjectField = new Reporter("s1.StructWithRef.ObjectField", nameof(TestImplicitByrefsOneBlock));
        var s2 = new SimpleStructWithExplicitLayout();
        s2.ObjectField = new Reporter("s2.ObjectField", nameof(TestImplicitByrefsOneBlock));
        var s3 = new SimpleStructWithAutoLayout();
        s3.ObjectField = new Reporter("s3.ObjectField", nameof(TestImplicitByrefsOneBlock));
        s3.AnotherObjectField = new Reporter("s3.AnotherObjectField", nameof(TestImplicitByrefsOneBlock));
        var s4 = new SingleObjectStruct();
        s4.ObjectField = new Reporter("s4.ObjectField", nameof(TestImplicitByrefsOneBlock));
        var s5 = new StructWithoutObjectFields();
        var s6src = new SingleObjectStruct();
        s6src.ObjectField = new Reporter("s6src.ObjectField", nameof(TestImplicitByrefsOneBlock));
        var s6 = new SingleObjectStruct?(s6src);

        TestImplicitByrefsOneBlockInner(s1, s2, s3, s4, s5, s6);
    }

    public static void TestImplicitByrefsOneBlockInner(
        ComplexStructWithExplicitLayout s1,
        SimpleStructWithExplicitLayout s2,
        SimpleStructWithAutoLayout s3,
        SingleObjectStruct s4,
        StructWithoutObjectFields s5,
        SingleObjectStruct? s6)
    {
        GC.Collect(2, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();

        CheckSuccess();

        GC.KeepAlive(s1);
        GC.KeepAlive(s2);
        GC.KeepAlive(s3);
        GC.KeepAlive(s4);
        GC.KeepAlive(s5);
        GC.KeepAlive(s6);
    }

    public static void TestExplicitByrefsOneBlock()
    {
        var s1 = new ComplexStructWithExplicitLayout();
        s1.ObjectField = new Reporter("s1.ObjectField", nameof(TestExplicitByrefsOneBlock));
        s1.AnotherObjectField = new Reporter("s1.AnotherObjectField", nameof(TestExplicitByrefsOneBlock));
        s1.StructWithRef.ObjectField = new Reporter("s1.StructWithRef.ObjectField", nameof(TestExplicitByrefsOneBlock));
        var s2 = new SimpleStructWithExplicitLayout();
        s2.ObjectField = new Reporter("s2.ObjectField", nameof(TestExplicitByrefsOneBlock));
        var s3 = new SimpleStructWithAutoLayout();
        s3.ObjectField = new Reporter("s3.ObjectField", nameof(TestExplicitByrefsOneBlock));
        s3.AnotherObjectField = new Reporter("s3.AnotherObjectField", nameof(TestExplicitByrefsOneBlock));
        var s4 = new SingleObjectStruct();
        s4.ObjectField = new Reporter("s4.ObjectField", nameof(TestExplicitByrefsOneBlock));
        var s5 = new StructWithoutObjectFields();
        var s6src = new SingleObjectStruct();
        s6src.ObjectField = new Reporter("s6src.ObjectField", nameof(TestExplicitByrefsOneBlock));
        var s6 = new SingleObjectStruct?(s6src);

        TestExplicitByrefsOneBlockInner(ref s1, ref s2, ref s3, ref s4, ref s5, ref s6);
    }

    public static void TestExplicitByrefsOneBlockInner(
        ref ComplexStructWithExplicitLayout s1,
        ref SimpleStructWithExplicitLayout s2,
        ref SimpleStructWithAutoLayout s3,
        ref SingleObjectStruct s4,
        ref StructWithoutObjectFields s5,
        ref SingleObjectStruct? s6)
    {
        GC.Collect(2, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();

        CheckSuccess();

        GC.KeepAlive(s1);
        GC.KeepAlive(s2);
        GC.KeepAlive(s3);
        GC.KeepAlive(s4);
        GC.KeepAlive(s5);
        GC.KeepAlive(s6);
    }

    public static void TestClassWrapperOneBlock()
    {
        var c1 = new Class<ComplexStructWithExplicitLayout>();
        c1.Field.ObjectField = new Reporter("c1.Field.ObjectField", nameof(TestClassWrapperOneBlock));
        c1.Field.AnotherObjectField = new Reporter("c1.Field.AnotherObjectField", nameof(TestClassWrapperOneBlock));
        c1.Field.StructWithRef.ObjectField = new Reporter("c1.Field.StructWithRef.ObjectField", nameof(TestClassWrapperOneBlock));
        var c2 = new Class<SimpleStructWithExplicitLayout>();
        c2.Field = new SimpleStructWithExplicitLayout();
        c2.Field.ObjectField = new Reporter("c2.Field.ObjectField", nameof(TestClassWrapperOneBlock));
        var c3 = new Class<SimpleStructWithAutoLayout>();
        c3.Field.ObjectField = new Reporter("c3.Field.ObjectField", nameof(TestClassWrapperOneBlock));
        c3.Field.AnotherObjectField = new Reporter("c3.Field.AnotherObjectField", nameof(TestClassWrapperOneBlock));
        var c4 = new Class<SingleObjectStruct>();
        c4.Field.ObjectField = new Reporter("c4.Field.ObjectField", nameof(TestClassWrapperOneBlock));
        var c5 = new Class<StructWithoutObjectFields>();
        var c6 = new Class<SingleObjectStruct?>();
        var s6src = new SingleObjectStruct();
        s6src.ObjectField = new Reporter("s6src.ObjectField", nameof(TestClassWrapperOneBlock));
        c6.Field = new SingleObjectStruct?(s6src);

        TestClassWrapperOneBlockInner(c1, c2, c3, c4, c5, c6);
    }

    public static void TestClassWrapperOneBlockInner(
        Class<ComplexStructWithExplicitLayout> c1,
        Class<SimpleStructWithExplicitLayout> c2,
        Class<SimpleStructWithAutoLayout> c3,
        Class<SingleObjectStruct> c4,
        Class<StructWithoutObjectFields> c5,
        Class<SingleObjectStruct?> c6)
    {
        GC.Collect(2, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();

        CheckSuccess();

        GC.KeepAlive(c1.Field);
        GC.KeepAlive(c2.Field);
        GC.KeepAlive(c3.Field);
        GC.KeepAlive(c4.Field);
        GC.KeepAlive(c5.Field);
        GC.KeepAlive(c6.Field);
    }

    public static void TestFinallyLocals()
    {
        var s1 = new ComplexStructWithExplicitLayout();
        var s2 = new SimpleStructWithExplicitLayout();
        var s3 = new SimpleStructWithAutoLayout();
        var s4 = new SingleObjectStruct();
        var s5 = new StructWithoutObjectFields();
        var s6 = new SingleObjectStruct?();

        try
        {
            s1.ObjectField = new Reporter("s1.ObjectField", nameof(TestFinallyLocals));
            s1.AnotherObjectField = new Reporter("s1.AnotherObjectField", nameof(TestFinallyLocals));
            s1.StructWithRef.ObjectField = new Reporter("s1.StructWithRef.ObjectField", nameof(TestFinallyLocals));
            s2.ObjectField = new Reporter("s2.ObjectField", nameof(TestFinallyLocals));
            s3.ObjectField = new Reporter("s3.ObjectField", nameof(TestFinallyLocals));
            s3.AnotherObjectField = new Reporter("s3.AnotherObjectField", nameof(TestFinallyLocals));
            s4.ObjectField = new Reporter("s4.ObjectField", nameof(TestFinallyLocals));
            var s6src = new SingleObjectStruct();
            s6src.ObjectField = new Reporter("s6src.ObjectField", nameof(TestFinallyLocals));
            s6 = new SingleObjectStruct?(s6src);

            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }
        finally
        {
            CheckSuccess();

            GC.KeepAlive(s1);
            GC.KeepAlive(s2);
            GC.KeepAlive(s3);
            GC.KeepAlive(s4);
            GC.KeepAlive(s5);
            GC.KeepAlive(s6);
        }
    }

    public static void TestThrowCatchFinallyLocals()
    {
        var s1 = new ComplexStructWithExplicitLayout();
        var s2 = new SimpleStructWithExplicitLayout();
        var s3 = new SimpleStructWithAutoLayout();
        var s4 = new SingleObjectStruct();
        var s5 = new StructWithoutObjectFields();
        var s6 = new SingleObjectStruct?();

        try
        {
            s1.ObjectField = new Reporter("s1.ObjectField", nameof(TestThrowCatchFinallyLocals));
            s1.AnotherObjectField = new Reporter("s1.AnotherObjectField", nameof(TestThrowCatchFinallyLocals));
            s1.StructWithRef.ObjectField = new Reporter("s1.StructWithRef.ObjectField", nameof(TestThrowCatchFinallyLocals));
            s2.ObjectField = new Reporter("s2.ObjectField", nameof(TestThrowCatchFinallyLocals));
            s3.ObjectField = new Reporter("s3.ObjectField", nameof(TestThrowCatchFinallyLocals));
            s3.AnotherObjectField = new Reporter("s3.AnotherObjectField", nameof(TestThrowCatchFinallyLocals));
            s4.ObjectField = new Reporter("s4.ObjectField", nameof(TestThrowCatchFinallyLocals));
            var s6src = new SingleObjectStruct();
            s6src.ObjectField = new Reporter("s6src.ObjectField", nameof(TestThrowCatchFinallyLocals));
            s6 = new SingleObjectStruct?(s6src);

            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            throw new Exception();
        }
        catch
        {
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }
        finally
        {
            CheckSuccess();

            GC.KeepAlive(s1);
            GC.KeepAlive(s2);
            GC.KeepAlive(s3);
            GC.KeepAlive(s4);
            GC.KeepAlive(s5);
            GC.KeepAlive(s6);
        }
    }

    public class Reporter
    {
        private readonly string _fieldName;
        private readonly string _test;

        public Reporter(string fieldName, string test) => (_fieldName, _test) = (fieldName, test);

        ~Reporter()
        {
            _failedTest = _test;
            _failedFieldName = _fieldName;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckSuccess()
    {
        if (_failedTest is not null || _failedFieldName is not null)
        {
            Console.Error.WriteLine($"{_failedTest} failed to keep {_failedFieldName} alive");
            Environment.Exit(-1);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Reset()
    {
        GC.Collect(2, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();

        _failedTest = null;
        _failedFieldName = null;
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct ComplexStructWithExplicitLayout
{
    [FieldOffset(0)]
    public int IntegerField;
    [FieldOffset(1)]
    public byte ByteField;
    [FieldOffset(8)]
    public object ObjectField;

    [FieldOffset(17)]
    public byte AnotherByteField;

    [FieldOffset(24)]
    public SimpleStructWithExplicitLayout StructWithRef;

    [FieldOffset(80)]
    public object AnotherObjectField;
}

[StructLayout(LayoutKind.Explicit)]
public struct SimpleStructWithExplicitLayout
{
    [FieldOffset(8)]
    public object ObjectField;
}

public struct SimpleStructWithAutoLayout
{
    public object ObjectField;
    public object AnotherObjectField;
}

public struct SingleObjectStruct
{
    public object ObjectField;
}

public struct StructWithoutObjectFields
{
    public int IntegerField;
    public short ShortField;
    public long LongField;
    public Vector128<int> VectorField;
}

public class Class<T>
{
    public T Field;
}
