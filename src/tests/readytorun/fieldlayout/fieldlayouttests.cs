using System;
using System.Runtime.Intrinsics;
using Xunit;

public class Test
{
    // This test uses the same set of types as the type system unittests use, and attempts to validate that the R2R usage of said types works well.
    // This is done by touching the various types, and then relying on the verification logic in R2R images to detect failures.
    [Fact]
    public static void TestEntryPoint()
    {
        ContainsGCPointersFieldsTest.Test();
//        ExplicitTest.Test(); // Explicit layout is known to not quite match the runtime, and if enabled this set of tests will fail.
        SequentialTest.Test();
        AutoTest.Test();
        EnumAlignmentTest.Test();
        AutoTestWithVector128.Test();
        AutoTestWithVector256.Test();
        AutoTestWithVector512.Test();
    }
}

class EnumAlignmentTest
{
    static EnumAlignment.LongIntEnumStruct _fld1;
    static EnumAlignment.LongIntEnumStructFieldStruct _fld2;
    static EnumAlignment.IntShortEnumStruct _fld3;
    static EnumAlignment.IntShortEnumStructFieldStruct _fld4;
    static EnumAlignment.ShortByteEnumStruct _fld5;
    static EnumAlignment.ShortByteEnumStructFieldStruct _fld6;
    static EnumAlignment.LongIntEnumStructAuto _fld7;
    static EnumAlignment.LongIntEnumStructAutoFieldStruct _fld8;
    static EnumAlignment.IntShortEnumStructAuto _fld9;
    static EnumAlignment.IntShortEnumStructAutoFieldStruct _fld10;
    static EnumAlignment.ShortByteEnumStructAuto _fld11;
    static EnumAlignment.ShortByteEnumStructAutoFieldStruct _fld12;

    public static void Test()
    {
        _fld1._1 = EnumAlignment.LongEnum.Val;
        _fld1._2 = EnumAlignment.IntEnum.Val;
        _fld1._3 = EnumAlignment.LongEnum.Val;
        _fld1._4 = EnumAlignment.IntEnum.Val;

        _fld2._0 = 0;
        _fld2._struct = _fld1;

        _fld3._1 = EnumAlignment.IntEnum.Val;
        _fld3._2 = EnumAlignment.ShortEnum.Val;
        _fld3._3 = EnumAlignment.IntEnum.Val;
        _fld3._4 = EnumAlignment.ShortEnum.Val;

        _fld4._0 = 1;
        _fld4._struct = _fld3;

        _fld5._1 = EnumAlignment.ShortEnum.Val;
        _fld5._2 = EnumAlignment.ByteEnum.Val;
        _fld5._3 = EnumAlignment.ShortEnum.Val;
        _fld5._4 = EnumAlignment.ByteEnum.Val;

        _fld6._0 = 2;
        _fld6._struct = _fld5;

        _fld7._1 = EnumAlignment.LongEnum.Val;
        _fld7._2 = EnumAlignment.IntEnum.Val;
        _fld7._3 = EnumAlignment.LongEnum.Val;
        _fld7._4 = EnumAlignment.IntEnum.Val;

        _fld8._0 = 3;
        _fld8._struct = _fld7;

        _fld9._1 = EnumAlignment.IntEnum.Val;
        _fld9._2 = EnumAlignment.ShortEnum.Val;
        _fld9._3 = EnumAlignment.IntEnum.Val;
        _fld9._4 = EnumAlignment.ShortEnum.Val;

        _fld10._0 = 4;
        _fld10._struct = _fld9;

        _fld11._1 = EnumAlignment.ShortEnum.Val;
        _fld11._2 = EnumAlignment.ByteEnum.Val;
        _fld11._3 = EnumAlignment.ShortEnum.Val;
        _fld11._4 = EnumAlignment.ByteEnum.Val;

        _fld12._0 = 5;
        _fld12._struct = _fld11;
    }
}

class AutoTest
{
    static Auto.StructWithBool _fld1;
    static Auto.StructWithIntChar _fld2;
    static Auto.StructWithChar _fld3;
    static Auto.ClassContainingStructs _fld4 = new Auto.ClassContainingStructs();
    static Auto.BaseClass7BytesRemaining _fld5 = new Auto.BaseClass7BytesRemaining();
    static Auto.BaseClass4BytesRemaining _fld6 = new Auto.BaseClass4BytesRemaining();
    static Auto.BaseClass3BytesRemaining _fld7 = new Auto.BaseClass3BytesRemaining();
    static Auto.OptimizePartial _fld8 = new Auto.OptimizePartial();
    static Auto.Optimize7Bools _fld9 = new Auto.Optimize7Bools();
    static Auto.OptimizeAlignedFields _fld10 = new Auto.OptimizeAlignedFields();
    static Auto.OptimizeLargestField _fld11 = new Auto.OptimizeLargestField();
    static Auto.NoOptimizeMisaligned _fld12 = new Auto.NoOptimizeMisaligned();
    static Auto.NoOptimizeCharAtSize2Alignment _fld13 = new Auto.NoOptimizeCharAtSize2Alignment();
    static Auto.MinPacking<long> _fld14 = new Auto.MinPacking<long>();

    public static void Test()
    {
        _fld1.MyStructBool = true;

        _fld2.MyStructInt = 1;
        _fld2.MyStructChar = 'A';

        _fld3.MyStructChar = 'B';

        _fld4.MyStructWithChar = _fld3;
        _fld4.MyStructWithIntChar = _fld2;
        _fld4.MyStructWithBool = _fld1;
        _fld4.MyString1 = "Str";
        _fld4.MyBool1 = false;
        _fld4.MyBool2 = true;

        _fld5.MyBool1 = false;
        _fld5.MyLong1 = 2;
        _fld5.MyString1 = "Str2";
        _fld5.MyDouble1 = 1.0;
        _fld5.MyByteArray1 = new byte[3];

        _fld6.MyLong1 = 3;
        _fld6.MyUint1 = 4;

        _fld7.MyBool1 = true;
        _fld7.MyInt1 = 5;
        _fld7.MyString1 = "str3";

        _fld8.OptBool = false;
        _fld8.OptChar = 'B';
        _fld8.NoOptLong = 6;
        _fld8.NoOptString = "STR4";

        _fld9.OptBool1 = true;
        _fld9.OptBool2 = false;
        _fld9.OptBool3 = true;
        _fld9.OptBool4 = true;
        _fld9.OptBool5 = false;
        _fld9.OptBool6 = true;
        _fld9.OptBool7 = false;
        _fld9.NoOptBool8 = true;
        _fld9.NoOptString = "STR5";

        _fld10.OptBool1 = false;
        _fld10.OptBool2 = true;
        _fld10.OptBool3 = false;
        _fld10.NoOptBool4 = true;
        _fld10.OptChar1 = 'C';
        _fld10.OptChar2 = 'D';
        _fld10.NoOptString = "STR6";

        _fld13.NoOptChar = 'E';

        _fld14._value = 7;
        _fld14._byte = 8;
    }
}

class AutoTestWithVector128
{
    static Auto.int8x16x2 _fld1 = new Auto.int8x16x2();
    static Auto.Wrapper_int8x16x2 _fld2 = new Auto.Wrapper_int8x16x2();
    static Auto.Wrapper_int8x16x2_2 _fld3 = new Auto.Wrapper_int8x16x2_2();

    public static void Test()
    {
        _fld1._0 = new Vector128<byte>();
        _fld1._1 = new Vector128<byte>();

        _fld2.fld = _fld1;

        _fld3.fld1 = true;
        _fld3.fld2 = _fld1;
    }
}

class AutoTestWithVector256
{
    static Auto.int8x32x2 _fld1 = new Auto.int8x32x2();
    static Auto.Wrapper_int8x32x2 _fld2 = new Auto.Wrapper_int8x32x2();
    static Auto.Wrapper_int8x32x2_2 _fld3 = new Auto.Wrapper_int8x32x2_2();

    public static void Test()
    {
        _fld1._0 = new Vector256<byte>();
        _fld1._1 = new Vector256<byte>();

        _fld2.fld = _fld1;

        _fld3.fld1 = true;
        _fld3.fld2 = _fld1;
    }
}

class AutoTestWithVector512
{
    static Auto.int8x64x2 _fld1 = new Auto.int8x64x2();
    static Auto.Wrapper_int8x64x2 _fld2 = new Auto.Wrapper_int8x64x2();
    static Auto.Wrapper_int8x64x2_2 _fld3 = new Auto.Wrapper_int8x64x2_2();

    public static void Test()
    {
        _fld1._0 = new Vector512<byte>();
        _fld1._1 = new Vector512<byte>();

        _fld2.fld = _fld1;

        _fld3.fld1 = true;
        _fld3.fld2 = _fld1;
    }
}

class SequentialTest
{
    static Sequential.Class1 _fld1 = new Sequential.Class1();
    static Sequential.Class2 _fld2 = new Sequential.Class2();
    static Sequential.Struct0 _fld3;
    static Sequential.Struct1 _fld4;
    static Sequential.ClassDoubleBool _fld5 = new Sequential.ClassDoubleBool();
    static Sequential.ClassBoolDoubleBool _fld6 = new Sequential.ClassBoolDoubleBool();
    static Sequential.StructStructByte_StructByteAuto _fld7;
    static Sequential.StructStructByte_Struct2BytesAuto _fld8;
    static Sequential.StructStructByte_Struct3BytesAuto _fld9;
    static Sequential.StructStructByte_Struct4BytesAuto _fld10;
    static Sequential.StructStructByte_Struct5BytesAuto _fld11;
    static Sequential.StructStructByte_Struct8BytesAuto _fld12;
    static Sequential.StructStructByte_Struct9BytesAuto _fld13;
    static Sequential.StructStructByte_Int128StructAuto _fld14;
    static Sequential.StructStructByte_UInt128StructAuto _fld15;

    public static void Test()
    {
        _fld1.MyClass1SelfRef = _fld1;
        _fld1.MyChar = 'A';
        _fld1.MyInt = 1;
        _fld1.MyString = "STR";
        _fld1.MyBool = true;

        _fld2.MyClass1SelfRef = _fld1;
        _fld2.MyChar = 'B';
        _fld2.MyInt = 2;
        _fld2.MyString = "STR2";
        _fld2.MyBool = false;
        _fld2.MyInt2 = 3;

        _fld3.b1 = true;
        _fld3.b2 = false;
        _fld3.b3 = true;
        _fld3.i1 = 4;
        _fld3.s1 = "str";

        _fld4.MyStruct0 = _fld3;
        _fld4.MyBool = false;

        _fld5.bool1 = true;
        _fld5.double1 = 1.0;

        _fld6.bool1 = false;
        _fld6.bool2 = true;
        _fld6.double1 = 2.0;

        _fld7.fld2 = default(Auto.StructByte);
        _fld8.fld2 = default(Auto.Struct2Bytes);
        _fld9.fld2 = default(Auto.Struct3Bytes);
        _fld10.fld2 = default(Auto.Struct4Bytes);
        _fld11.fld2 = default(Auto.Struct5Bytes);
        _fld12.fld2 = default(Auto.Struct8Bytes);
        _fld13.fld2 = default(Auto.Struct9Bytes);
        _fld14.fld2 = default(Auto.Int128Struct);
        _fld15.fld2 = default(Auto.UInt128Struct);
    }
}

class ExplicitTest
{
    static Explicit.Class1 _fld1 = new Explicit.Class1();
    static Explicit.Class2 _fld2 = new Explicit.Class2();
    static Explicit.ExplicitSize _fld3 = new Explicit.ExplicitSize();
    static Explicit.ExplicitEmptyClass _fld4 = new Explicit.ExplicitEmptyClass();
    static Explicit.ExplicitEmptyClassSize0 _fld5 = new Explicit.ExplicitEmptyClassSize0();
    static Explicit.ExplicitEmptyStruct _fld6 = new Explicit.ExplicitEmptyStruct();

    public static void Test()
    {
        _fld1.Bar = true;
        _fld1.Baz = 'A';

        _fld2.Baz = 'B';
        _fld2.Bar = false;
        _fld2.Lol = 1;
        _fld2.Omg = 2;

        _fld3.Omg = 3;
        _fld3.Lol = 4;
    }
}
class ContainsGCPointersFieldsTest
{
    static ContainsGCPointers.NoPointers _fld1;
    static ContainsGCPointers.StillNoPointers _fld2;
    static ContainsGCPointers.ClassNoPointers _fld3 = new ContainsGCPointers.ClassNoPointers();
    static ContainsGCPointers.HasPointers _fld4;
    static ContainsGCPointers.FieldHasPointers _fld5;
    static ContainsGCPointers.ClassHasPointers _fld6 = new ContainsGCPointers.ClassHasPointers();
    static ContainsGCPointers.BaseClassHasPointers _fld7 = new ContainsGCPointers.BaseClassHasPointers();
    static ContainsGCPointers.ClassHasIntArray _fld8 = new ContainsGCPointers.ClassHasIntArray();
    static ContainsGCPointers.ClassHasArrayOfClassType _fld9 = new ContainsGCPointers.ClassHasArrayOfClassType();

    public static void Test()
    {
        _fld1.int1 = 1;
        _fld1.byte1 = 2;
        _fld1.char1 = '0';
        _fld2.bool1 = true;

        _fld2.noPointers1 = _fld1;

        _fld3.char1 = '2';

        _fld4.string1 = "STR";

        _fld5.hasPointers1.string1 = "STR2";

        _fld6.classHasPointers1 = new ContainsGCPointers.ClassHasPointers();

        _fld7.classHasPointers1 = new ContainsGCPointers.ClassHasPointers();

        _fld8.intArrayField = new int[1];

        _fld9.classTypeArray = new ContainsGCPointers.ClassNoPointers[1];
    }
}
