// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.common.common;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.common.common
{
    // <Title> Dynamic and static interaction utility class </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Verify
    {
        internal static int Eval(Func<bool> testmethod)
        {
            int result = 0;
            try
            {
                if (!testmethod())
                {
                    result++;
                    //System.Console.WriteLine("Test failed at {0}\n", testmethod.Method.Name);
                }
            }
            catch (Exception e)
            {
                result++;
                //System.Console.WriteLine("Catch an unknown exception when run test {0}, \nexception: {1}", testmethod.Method.Name, e.ToString());
            }

            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target001.target001
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The target type is predefined value type </Title>
    // <Description>
    // bool, char, sbyte, byte and their nullable form.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class TargetIsPredefinedValueType
    {
        #region non-nullable
        private static bool BoolTypeWithIdentityConversionInIfStatement()
        {
            int failcount = 0;
            dynamic d = false;
            if (d)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool CharTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            dynamic d = 'a';
            char result = d;
            if (result != 'a')
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool SByteTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            dynamic d = (sbyte)-128;
            sbyte result = d;
            if (result != -128)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ByteTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            dynamic d = (byte)0;
            byte result = d;
            if (result != 0)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        #endregion
        #region nullable
        private static bool NullableBoolTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            bool? origin = true;
            dynamic d = origin;
            bool? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableCharTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            char? origin = '\0';
            dynamic d = origin;
            char? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableSbyteTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            sbyte? origin = 127;
            dynamic d = origin;
            sbyte? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableByteTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            byte? origin = 255;
            dynamic d = origin;
            byte? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(BoolTypeWithIdentityConversionInIfStatement);
            result += Verify.Eval(CharTypeWithIdentityConversionInAssignment);
            result += Verify.Eval(SByteTypeWithIdentityConversionInAssignment);
            result += Verify.Eval(ByteTypeWithIdentityConversionInAssignment);
            result += Verify.Eval(NullableBoolTypeWithIdentityConversionInAssignment);
            result += Verify.Eval(NullableCharTypeWithIdentityConversionInAssignment);
            result += Verify.Eval(NullableSbyteTypeWithIdentityConversionInAssignment);
            result += Verify.Eval(NullableByteTypeWithIdentityConversionInAssignment);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target002.target002
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The target type is predefined value type </Title>
    // <Description>
    // short, ushort, int, uint, long, ulong and their nullable form.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class TargetIsPredefinedValueType
    {
        #region non-nullable
        private static bool ShortTypeWithNumbericConversionFromSbyteInAssignment()
        {
            int failcount = 0;
            sbyte origin = sbyte.MinValue;
            dynamic d = origin;
            short result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool UshortTypeWithNumbericConversionFromCharInAssignment()
        {
            int failcount = 0;
            char origin = char.MinValue;
            dynamic d = origin;
            ushort result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool IntTypeWithNumbericConversionFromSbyteInAssignment()
        {
            int failcount = 0;
            sbyte origin = sbyte.MinValue;
            dynamic d = origin;
            int result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool UintTypeWithNumbericConversionFromByteInAssignment()
        {
            int failcount = 0;
            byte origin = byte.MaxValue;
            dynamic d = origin;
            uint result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool LongTypeWithNumbericConversionFromShortInAssignment()
        {
            int failcount = 0;
            short origin = short.MaxValue;
            dynamic d = origin;
            long result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool UlongTypeWithNumbericConversionFromUshortInAssignment()
        {
            int failcount = 0;
            ushort origin = ushort.MaxValue;
            dynamic d = origin;
            ulong result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        #endregion
        #region nullable
        private static bool NullableShortTypeWithNumbericConversionFromByteInAssignment()
        {
            int failcount = 0;
            byte? origin = byte.MaxValue;
            dynamic d = origin;
            short? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableUshortTypeWithNumbericConversionFromByteInAssignment()
        {
            int failcount = 0;
            byte? origin = byte.MinValue;
            dynamic d = origin;
            short? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableIntTypeWithNumbericConversionFromShortInAssignment()
        {
            int failcount = 0;
            short? origin = short.MaxValue;
            dynamic d = origin;
            int? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableUintTypeWithNumbericConversionFromUshortInAssignment()
        {
            int failcount = 0;
            ushort? origin = ushort.MaxValue;
            dynamic d = origin;
            uint? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableLongTypeWithNumbericConversionFromIntInAssignment()
        {
            int failcount = 0;
            int? origin = int.MinValue;
            dynamic d = origin;
            long? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableUlongTypeWithNullableNumbericConversionFromUintInAssignment()
        {
            int failcount = 0;
            uint origin = uint.MaxValue;
            dynamic d = origin;
            ulong? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(ShortTypeWithNumbericConversionFromSbyteInAssignment);
            result += Verify.Eval(UshortTypeWithNumbericConversionFromCharInAssignment);
            result += Verify.Eval(IntTypeWithNumbericConversionFromSbyteInAssignment);
            result += Verify.Eval(UintTypeWithNumbericConversionFromByteInAssignment);
            result += Verify.Eval(LongTypeWithNumbericConversionFromShortInAssignment);
            result += Verify.Eval(UlongTypeWithNumbericConversionFromUshortInAssignment);
            result += Verify.Eval(NullableShortTypeWithNumbericConversionFromByteInAssignment);
            result += Verify.Eval(NullableUshortTypeWithNumbericConversionFromByteInAssignment);
            result += Verify.Eval(NullableIntTypeWithNumbericConversionFromShortInAssignment);
            result += Verify.Eval(NullableUintTypeWithNumbericConversionFromUshortInAssignment);
            result += Verify.Eval(NullableLongTypeWithNumbericConversionFromIntInAssignment);
            result += Verify.Eval(NullableUlongTypeWithNullableNumbericConversionFromUintInAssignment);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target003.target003
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The target type is predefined value type </Title>
    // <Description>
    // float, double, decimal, GUID, DateTime and their nullable form.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class TargetIsPredefinedValueType
    {
        #region non-nullable
        private static bool FloatTypeWithNumbericConversionFromIntInAssignment()
        {
            int failcount = 0;
            int origin = int.MaxValue;
            dynamic d = origin;
            float result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool DoubleTypeWithNumbericConversionFromUlongInAssignment()
        {
            int failcount = 0;
            ulong origin = ulong.MinValue;
            dynamic d = origin;
            double result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool DecimalTypeWithNumbericConversionFromUintInAssignment()
        {
            int failcount = 0;
            uint origin = uint.MaxValue;
            dynamic d = origin;
            decimal result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool GuidTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            Guid origin = new Guid("11111111-2222-3333-4444-555555555555");
            dynamic d = origin;
            Guid result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool DatetimeTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            DateTime origin = DateTime.Now;
            dynamic d = origin;
            DateTime result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        #endregion
        #region nullable
        private static bool NullableFloatTypeWithNumbericConversionFromLongInAssignment()
        {
            int failcount = 0;
            long? origin = long.MaxValue;
            dynamic d = origin;
            float? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableDoubleTypeWithNumbericConversionFromFloatInAssignment()
        {
            int failcount = 0;
            float? origin = +0.0f;
            dynamic d = origin;
            double? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableDecimalTypeWithNumbericConversionFromUlongInAssignment()
        {
            int failcount = 0;
            ulong? origin = ulong.MaxValue;
            dynamic d = origin;
            decimal? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableGuidTypeWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            Guid? origin = new Guid("11111111-2222-3333-4444-555555555555");
            dynamic d = origin;
            Guid? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableDatetimeTypeWithNullableIdentityConversionInAssignment()
        {
            int failcount = 0;
            DateTime origin = DateTime.Now;
            dynamic d = origin;
            DateTime? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(FloatTypeWithNumbericConversionFromIntInAssignment);
            result += Verify.Eval(DoubleTypeWithNumbericConversionFromUlongInAssignment);
            result += Verify.Eval(DecimalTypeWithNumbericConversionFromUintInAssignment);
            result += Verify.Eval(GuidTypeWithIdentityConversionInAssignment);
            result += Verify.Eval(DatetimeTypeWithIdentityConversionInAssignment);
            result += Verify.Eval(NullableFloatTypeWithNumbericConversionFromLongInAssignment);
            result += Verify.Eval(NullableDoubleTypeWithNumbericConversionFromFloatInAssignment);
            result += Verify.Eval(NullableDecimalTypeWithNumbericConversionFromUlongInAssignment);
            result += Verify.Eval(NullableGuidTypeWithIdentityConversionInAssignment);
            result += Verify.Eval(NullableDatetimeTypeWithNullableIdentityConversionInAssignment);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target004.target004
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The target type is user-defined value type </Title>
    // <Description>
    // User defined struct, generic struct, enum and their nullable form.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public enum E
    {
        One,
        Two
    }

    public struct S
    {
        public int f;
        public S(int v)
        {
            f = v;
        }
    }

    public struct S2
    {
        public int f;
        public S2(int v)
        {
            f = v;
        }

        public static implicit operator S2(S s1)
        {
            return new S2(s1.f);
        }
    }

    public struct GS<T>
    {
        public T f;
        public GS(T v)
        {
            f = v;
        }
    }

    public class TargetIsUserdefinedValueType
    {
        #region non-nullable
        private static bool UserdefinedStructWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            S origin = new S(10);
            dynamic d = origin;
            S result = d;
            if (result.f != origin.f)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool UserdefinedStructWithUserdefinedImplicitConversionInAssignment()
        {
            int failcount = 0;
            S origin = new S(10);
            dynamic d = origin;
            S2 result = d;
            if (result.f != origin.f)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool UserdefinedGenericStructWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            GS<int> origin = new GS<int>(10);
            dynamic d = origin;
            GS<int> result = d;
            if (result.f != origin.f)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool UserdefinedEnumWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            E origin = E.One;
            dynamic d = origin;
            E result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        #endregion
        #region nullable
        private static bool NullableUserdefinedStructWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            S? origin = new S(10);
            dynamic d = origin;
            S? result = d;
            if (result.Value.f != origin.Value.f)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableUserdefinedStructWithNullableUserdefinedImplicitConversionInAssignment()
        {
            int failcount = 0;
            S origin = new S(10);
            dynamic d = origin;
            S2? result = d;
            if (result.Value.f != origin.f)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableUserdefinedGenericStructWithNullableIdentityConversionInAssignment()
        {
            int failcount = 0;
            GS<int> origin = new GS<int>(10);
            dynamic d = origin;
            GS<int>? result = d;
            if (result.Value.f != origin.f)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool NullableUserdefinedEnumWithNullableIdentityConversionInAssignment()
        {
            int failcount = 0;
            E origin = E.One;
            dynamic d = origin;
            E? result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(UserdefinedStructWithIdentityConversionInAssignment);
            result += Verify.Eval(UserdefinedStructWithUserdefinedImplicitConversionInAssignment);
            result += Verify.Eval(UserdefinedGenericStructWithIdentityConversionInAssignment);
            result += Verify.Eval(UserdefinedEnumWithIdentityConversionInAssignment);
            result += Verify.Eval(NullableUserdefinedStructWithIdentityConversionInAssignment);
            result += Verify.Eval(NullableUserdefinedStructWithNullableUserdefinedImplicitConversionInAssignment);
            result += Verify.Eval(NullableUserdefinedGenericStructWithNullableIdentityConversionInAssignment);
            result += Verify.Eval(NullableUserdefinedEnumWithNullableIdentityConversionInAssignment);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target005.target005
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The target type is predefined reference type </Title>
    // <Description>
    // string, object, System.ValueType, System.Enum, System.Array, System.Delegate
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public enum E
    {
        One,
        Two
    }

    public delegate int D();
    public struct S
    {
        public static int M()
        {
            return 10;
        }

        public int f;
        public S(int v)
        {
            f = v;
        }
    }

    public class MyException : Exception
    {
        public int code;
        public MyException(int c)
        {
            code = c;
        }
    }

    public class TargetIsPredefinedReferenceType
    {
        private static bool StringWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            string origin = "aabb";
            dynamic d = origin;
            string result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ObjectWithReferenceConversionFromStringInAssignment()
        {
            int failcount = 0;
            // no callsite for object
            //
            string origin = "aabb";
            dynamic d = origin;
            object result = d;
            if ((string)result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ObjectWithBoxingConversionFromIntInAssignment()
        {
            int failcount = 0;
            // no callsite for object
            //
            int origin = 10;
            dynamic d = origin;
            object result = d;
            if ((int)result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ValueTypeWithBoxingConversionFromUserdefinedStructInAssignment()
        {
            int failcount = 0;
            S origin = new S(10);
            dynamic d = origin;
            ValueType result = d;
            if (((S)result).f != origin.f)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool EnumWithBoxingConversionFromUserdefinedEnumInAssignment()
        {
            int failcount = 0;
            E origin = E.Two;
            dynamic d = origin;
            Enum result = d;
            if ((E)result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ArrayWithReferenceConversionFromUserdefinedArrayInAssignment()
        {
            int failcount = 0;
            var origin = new[]
            {
            1, 2, 3
            }

            ;
            dynamic d = origin;
            Array result = d;
            if (((int[])result)[1] != origin[1])
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool DelegateWithReferenceConversionFromUserdefinedDelegateInAssignment()
        {
            int failcount = 0;
            D origin = S.M;
            dynamic d = origin;
            Delegate result = d;
            if ((int)result.DynamicInvoke(null) != origin())
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ExceptionWithReferenceConversionFromUserdefinedExceptionInAssignment()
        {
            int failcount = 0;
            var origin = new MyException(0xE000);
            dynamic d = origin;
            Exception result = d;
            if (((MyException)result).code != origin.code)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(StringWithIdentityConversionInAssignment);
            result += Verify.Eval(ObjectWithReferenceConversionFromStringInAssignment);
            result += Verify.Eval(ObjectWithBoxingConversionFromIntInAssignment);
            result += Verify.Eval(ValueTypeWithBoxingConversionFromUserdefinedStructInAssignment);
            result += Verify.Eval(EnumWithBoxingConversionFromUserdefinedEnumInAssignment);
            result += Verify.Eval(ArrayWithReferenceConversionFromUserdefinedArrayInAssignment);
            result += Verify.Eval(DelegateWithReferenceConversionFromUserdefinedDelegateInAssignment);
            result += Verify.Eval(ExceptionWithReferenceConversionFromUserdefinedExceptionInAssignment);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target006.target006
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The target type is array type </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class TargetIsArray
    {
        private static bool ArrayWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            var origin = new[]
            {
            1, 2, 3
            }

            ;
            dynamic d = origin;
            int[] result = d;
            if (result[1] != origin[1])
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ArrayWithReferenceConversionInAssignment()
        {
            int failcount = 0;
            var origin = new[]
            {
            "aa", "bb", "cc"
            }

            ;
            dynamic d = origin;
            object[] result = d;
            if ((string)(result[1]) != origin[1])
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(ArrayWithIdentityConversionInAssignment);
            result += Verify.Eval(ArrayWithReferenceConversionInAssignment);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target007.target007
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The target type is delegate </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int D();
    public delegate T GD<T>();
    public delegate T1 VGD<out T1, in T2>(T2 i);
    public class C
    {
        public static int M1()
        {
            return 10;
        }

        public static int M2(int i)
        {
            return i;
        }
    }

    public class TargetIsDelegate
    {
        private static bool DelegateWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            D origin = C.M1;
            dynamic d = origin;
            D result = d;
            if (result() != origin())
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool GenericDelegateWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            GD<int> origin = C.M1;
            dynamic d = origin;
            GD<int> result = d;
            if (result() != origin())
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool VariantGenericDelegateWithIdentityConversionInAssignment()
        {
            int failcount = 0;
            VGD<int, int> origin = C.M2;
            dynamic d = origin;
            VGD<int, int> result = d;
            if (result(11) != origin(11))
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(DelegateWithIdentityConversionInAssignment);
            result += Verify.Eval(GenericDelegateWithIdentityConversionInAssignment);
            result += Verify.Eval(VariantGenericDelegateWithIdentityConversionInAssignment);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target008.target008
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The target type is user-defined class </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class B
    {
    }

    public class C : B
    {
        public C()
        {
        }

        public int f1 = 10;
        public C(int v)
        {
            f1 = v;
        }

        public static implicit operator C2(C c)
        {
            return new C2(c.f1);
        }
    }

    public class C2
    {
        public int f1 = 10;
        public C2(int v)
        {
            f1 = v;
        }
    }

    public class GB<T>
    {
    }

    public class GGC<T> : GB<T>
    {
    }

    public class TargetIsUserdefinedClass
    {
        private static bool UserdefinedClassWithReferenceConversionInAssignment()
        {
            int failcount = 0;
            var origin = new C();
            dynamic d = origin;
            B result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool UserdefinedClassWithUserdefinedImplicitConversionInAssignment()
        {
            int failcount = 0;
            var origin = new C(22);
            dynamic d = origin;
            C2 result = d;
            if (result.f1 != origin.f1)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool UserdefinedGenericClassWithReferenceConversionInAssignment()
        {
            int failcount = 0;
            var origin = new GGC<int>();
            dynamic d = origin;
            GB<int> result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(UserdefinedClassWithReferenceConversionInAssignment);
            result += Verify.Eval(UserdefinedClassWithUserdefinedImplicitConversionInAssignment);
            result += Verify.Eval(UserdefinedGenericClassWithReferenceConversionInAssignment);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target010.target010
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The target type is type parameter </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class B
    {
    }

    public interface I
    {
    }

    public class C : B, I
    {
    }

    public class TargetIsTypeParameter
    {
        private static bool TestMethodForReferenceConversionFromStringToObjectInAssignment<T>() where T : class
        {
            int failcount = 0;
            var origin = "aa";
            dynamic d = origin;
            T result = d;
            if (result != (object)origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ReferenceConversionFromStringToObjectInAssignment()
        {
            return TestMethodForReferenceConversionFromStringToObjectInAssignment<object>();
        }

        private static bool TestMethodForReferenceConversionToBaseClassInAssignment<T>() where T : B
        {
            int failcount = 0;
            var origin = new C();
            dynamic d = origin;
            T result = d;
            if (result != (B)origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ReferenceConversionToBaseClassInAssignment()
        {
            return TestMethodForReferenceConversionToBaseClassInAssignment<B>();
        }

        private static bool TestMethodForReferenceConversionToBaseInterfaceInAssignment<T>() where T : class, I
        {
            int failcount = 0;
            var origin = new C();
            dynamic d = origin;
            T result = d;
            if (result != (I)origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed at conversion result");
            }

            return failcount == 0;
        }

        private static bool ReferenceConversionToBaseInterfaceInAssignment()
        {
            return TestMethodForReferenceConversionToBaseInterfaceInAssignment<I>();
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(ReferenceConversionFromStringToObjectInAssignment);
            result += Verify.Eval(ReferenceConversionToBaseClassInAssignment);
            result += Verify.Eval(ReferenceConversionToBaseInterfaceInAssignment);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.target012.target012
{
    // <Title>Guid as dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class C
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic g1 = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            Strc str1 = new Strc()
            {
                Val = g1
            }

            ;
            if (str1.Val.Equals(g1))
                return 0;
            else
                return 1;
        }
    }

    internal struct Strc
    {
        public Guid Val;
        public static void Log(string s)
        {
            System.Console.WriteLine("{0}", s);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.conversion001.conversion001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d;
            d = int.MaxValue;
            //int to long
            if ((long)d != int.MaxValue)
                return 1;
            //int to ulong
            if ((ulong)d != int.MaxValue)
                return 1;
            //int to uint
            if ((uint)d != int.MaxValue)
                return 1;
            //int to double
            if ((double)d != int.MaxValue)
                return 1;
            //int to float
            if ((float)d != int.MaxValue)
                return 1;
            //int to decimal
            if ((decimal)d != int.MaxValue)
                return 1;
            d = short.MaxValue;
            //short to int
            if ((int)d != short.MaxValue)
                return 1;
            //short to long
            if ((long)d != short.MaxValue)
                return 1;
            //short to uint
            if ((uint)d != short.MaxValue)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.conversion002.conversion002
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> Implicit conversion from runtime type of dynamic object </Title>
    // <Description>
    // No implicit conversion but exist explicit conversion.
    // There are no distinction between implicit and explicit conversion operators in convert of expression tree.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class C
    {
        public int f1 = 10;
        public C(int v)
        {
            f1 = v;
        }

        public static explicit operator C2(C c)
        {
            return new C2(c.f1);
        }
    }

    public class C2
    {
        public int f1 = 10;
        public C2(int v)
        {
            f1 = v;
        }
    }

    public class ConversionInRuntimeType
    {
        private static bool NoPredefinedImplicitConversionButExistExplicitConversion()
        {
            int failcount = 0;
            ulong origin = 100;
            dynamic d = origin;
            try
            {
                int result = (int)d;
                if ((ulong)result != origin)
                {
                    failcount++;
                    System.Console.WriteLine("Test failed at conversion result");
                }
            }
            catch (Exception e)
            {
                failcount++;
                System.Console.WriteLine("Test failed: catch an unknown exception {0}", e);
            }

            return failcount == 0;
        }

        private static bool NoUserdefinedImplicitConversionButExistExplicitConversion()
        {
            int failcount = 0;
            C origin = new C(22);
            dynamic d = origin;
            try
            {
                C2 result = (C2)d;
                if (result.f1 != origin.f1)
                {
                    failcount++;
                    System.Console.WriteLine("Test failed at conversion result");
                }
            }
            catch (Exception e)
            {
                failcount++;
                System.Console.WriteLine("Test failed: catch an unknown exception {0}", e);
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(NoPredefinedImplicitConversionButExistExplicitConversion);
            result += Verify.Eval(NoUserdefinedImplicitConversionButExistExplicitConversion);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.conversion003.conversion003
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title>  Implicit conversion from runtime type of dynamic object  </Title>
    // <Description>
    // No implicit and explicit conversions.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class C
    {
    }

    public class C2
    {
    }

    public class ConversionInRuntimeType
    {
        private static bool NoPredefinedImplicitAndExplicitConversion()
        {
            int failcount = 0;
            string origin = "aa";
            dynamic d = origin;
            try
            {
                int result = d;
                failcount++;
                System.Console.WriteLine("Test failed: should throw exception");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "string", "int"))
                    failcount++;
            }
            catch (Exception e)
            {
                failcount++;
                System.Console.WriteLine("Test failed: catch an unknown exception {0}", e);
            }

            return failcount == 0;
        }

        private static bool NoPredefinedImplicitAndExplicitConversionForArray()
        {
            int failcount = 0;
            var origin = new int[]
            {
            1, 2, 3
            }

            ;
            dynamic d = origin;
            try
            {
                long[] result = d;
                failcount++;
                System.Console.WriteLine("Test failed: should throw exception");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "int[]", "long[]"))
                    failcount++;
            }
            catch (Exception e)
            {
                failcount++;
                System.Console.WriteLine("Test failed: catch an unknown exception {0}", e);
            }

            return failcount == 0;
        }

        private static bool NoUserdefinedImplicitAndExplicitConversion()
        {
            int failcount = 0;
            C origin = new C();
            dynamic d = origin;
            try
            {
                C2 result = d;
                failcount++;
                System.Console.WriteLine("Test failed: should throw exception");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "C", "C2"))
                    failcount++;
            }
            catch (Exception e)
            {
                failcount++;
                System.Console.WriteLine("Test failed: catch an unknown exception {0}", e);
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(NoPredefinedImplicitAndExplicitConversion);
            result += Verify.Eval(NoPredefinedImplicitAndExplicitConversionForArray);
            result += Verify.Eval(NoUserdefinedImplicitAndExplicitConversion);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.conversion005.conversion005
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = null;
            string s = d;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context001.context001
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in assignment operators </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int D(int i);
    public class C
    {
        public int F1;
        public static uint F2;
        public D F3;
        public static D F4;
        public static int M(int i)
        {
            return i;
        }

        public long P1
        {
            get;
            set;
        }

        public static ulong P2
        {
            get;
            set;
        }

        private short _f1;
        public short this[int i]
        {
            get
            {
                return _f1;
            }

            set
            {
                _f1 = value;
            }
        }
    }

    public class ConversionInAssignmentOperators
    {
        private static bool LeftIsLocalVariable()
        {
            int failcount = 0;
            sbyte origin = 10;
            dynamic d = origin;
            int result = d;
            if (result != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool LeftIsInstanceField()
        {
            int failcount = 0;
            sbyte origin = 10;
            dynamic d = origin;
            var c = new C();
            c.F1 = d;
            if (c.F1 != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool LeftIsStaticField()
        {
            int failcount = 0;
            byte origin = 10;
            dynamic d = origin;
            C.F2 = d;
            if (C.F2 != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool LeftIsInstanceProperty()
        {
            int failcount = 0;
            sbyte origin = 10;
            dynamic d = origin;
            var c = new C();
            c.P1 = d;
            if (c.P1 != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool LeftIsStaticProperty()
        {
            int failcount = 0;
            byte origin = 10;
            dynamic d = origin;
            C.P2 = d;
            if (C.P2 != (ulong)origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool LeftIsInstanceDelegateField()
        {
            int failcount = 0;
            D origin = C.M;
            dynamic d = origin;
            var c = new C();
            c.F3 = d;
            if (c.F3(11) != origin(11))
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool LeftIsStaticDelegateField()
        {
            int failcount = 0;
            D origin = C.M;
            dynamic d = origin;
            C.F4 = d;
            if (C.F4(22) != origin(22))
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool LeftIsInstanceIndexer()
        {
            int failcount = 0;
            sbyte origin = 10;
            dynamic d = origin;
            var c = new C();
            c[1] = d;
            if (c[1] != origin)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InObjectInitializer()
        {
            int failcount = 0;
            sbyte origin = 10;
            dynamic d = origin;
            byte origin2 = 20;
            dynamic d2 = origin2;
            var c = new C()
            {
                F1 = d,
                P1 = d2
            }

            ;
            if ((c.F1 != origin) || (c.P1 != origin2))
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(LeftIsLocalVariable);
            result += Verify.Eval(LeftIsInstanceField);
            result += Verify.Eval(LeftIsStaticField);
            result += Verify.Eval(LeftIsInstanceProperty);
            result += Verify.Eval(LeftIsStaticProperty);
            result += Verify.Eval(LeftIsInstanceDelegateField);
            result += Verify.Eval(LeftIsStaticDelegateField);
            result += Verify.Eval(LeftIsInstanceIndexer);
            result += Verify.Eval(InObjectInitializer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context002.context002
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in boolean expression </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Linq;

    public class ConversionInBooleanExpression
    {
        private static bool InIfStatement()
        {
            int failcount = 0;
            bool origin = false;
            dynamic d = origin;
            if (d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InWhileStatement()
        {
            int failcount = 0;
            bool origin = false;
            dynamic d = origin;
            while (d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InDoWhileStatement()
        {
            int failcount = 0;
            bool origin = false;
            dynamic d = origin;
            int count = 0;
            do
            {
                if (count == 0)
                {
                    count++;
                }
                else
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                    break;
                }
            }
            while (d);
            return failcount == 0;
        }

        private static bool InForStatement()
        {
            int failcount = 0;
            bool origin = false;
            dynamic d = origin;
            for (; d;)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InConditionalOperator()
        {
            int failcount = 0;
            bool origin = false;
            dynamic d = origin;
            if (d ? true : false)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InWhereClauseOfQueryExpression()
        {
            int failcount = 0;
            var a = new[]
            {
            1, 2, 3
            }

            ;
            bool origin = true;
            dynamic d = origin;
            var q = (
                from m in a
                where d
                select m).ToArray();
            if (q[1] != a[1])
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(InIfStatement);
            result += Verify.Eval(InWhileStatement);
            result += Verify.Eval(InDoWhileStatement);
            result += Verify.Eval(InForStatement);
            result += Verify.Eval(InConditionalOperator);
            result += Verify.Eval(InWhereClauseOfQueryExpression);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context002a.context002a
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in boolean expression </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Linq;

    public class ConversionInBooleanExpression
    {
        public class C
        {
            public C()
            {
            }

            public C(int f)
            {
                Field = f;
            }

            public int Field;
            public static explicit operator bool (C p1)
            {
                if (p1.Field == 0)
                    return true;
                else
                    return false;
            }
        }

        private static bool InIfStatement0()
        {
            int failcount = 0;
            C origin = new C(0);
            dynamic d = origin;
            if ((bool)d)
            {
            }
            else
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InIfStatement1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            if ((bool)d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InWhileStatement0()
        {
            int failcount = 0;
            C origin = new C(0);
            dynamic d = origin;
            while ((bool)d)
            {
                failcount++; //touch here once.
                break;
            }

            return failcount == 1;
        }

        private static bool InWhileStatement1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            while ((bool)d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InDoWhileStatement0()
        {
            int failcount = 0;
            C origin = new C(0);
            dynamic d = origin;
            int count = 0;
            do
            {
                if (count == 0)
                {
                    count++; // touch here once then break;
                    break;
                }
                else
                {
                }
            }
            while (d);
            return failcount == 0 && count == 1;
        }

        private static bool InDoWhileStatement1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            int count = 0;
            do
            {
                if (count == 0)
                {
                    count++;
                }
                else
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                    break;
                }
            }
            while ((bool)d);
            return failcount == 0;
        }

        private static bool InForStatement0()
        {
            int failcount = 0;
            int count = 0;
            C origin = new C(0);
            dynamic d = origin;
            for (; (bool)d;)
            {
                if (count == 0)
                {
                    count++; // touch here once then break;
                    break;
                }
                else
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                    break;
                }
            }

            return failcount == 0 && count == 1;
        }

        private static bool InForStatement1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            for (; (bool)d;)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InConditionalOperator0()
        {
            int failcount = 0;
            C origin = new C(0);
            dynamic d = origin;
            if (!((bool)d ? true : false))
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InConditionalOperator1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            if ((bool)d ? true : false)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InWhereClauseOfQueryExpression0()
        {
            int failcount = 0;
            var a = new[]
            {
            1, 2, 3
            }

            ;
            C origin = new C(0);
            dynamic d = origin;
            var q = (
                from m in a
                where (bool)d
                select m).ToArray();
            if (q[1] != a[1])
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InWhereClauseOfQueryExpression1()
        {
            int failcount = 0;
            var a = new[]
            {
            1, 2, 3
            }

            ;
            C origin = new C(1);
            dynamic d = origin;
            var q = (
                from m in a
                where (bool)d
                select m).ToArray();
            if (q.Length != 0)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(InIfStatement0);
            result += Verify.Eval(InWhileStatement0);
            result += Verify.Eval(InDoWhileStatement0);
            result += Verify.Eval(InForStatement0);
            result += Verify.Eval(InConditionalOperator0);
            result += Verify.Eval(InWhereClauseOfQueryExpression0);
            result += Verify.Eval(InIfStatement1);
            result += Verify.Eval(InWhileStatement1);
            result += Verify.Eval(InDoWhileStatement1);
            result += Verify.Eval(InForStatement1);
            result += Verify.Eval(InConditionalOperator1);
            result += Verify.Eval(InWhereClauseOfQueryExpression1);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context002b.context002b
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in boolean expression </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Linq;

    public class ConversionInBooleanExpression
    {
        public class C
        {
            public C()
            {
            }

            public C(int f)
            {
                Field = f;
            }

            public int Field;
            public static implicit operator bool (C p1)
            {
                if (p1.Field == 0)
                    return true;
                else
                    return false;
            }
        }

        private static bool InIfStatement0()
        {
            int failcount = 0;
            C origin = new C(0);
            dynamic d = origin;
            if (!d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InIfStatement1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            if (d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InWhileStatement0()
        {
            int failcount = 0;
            C origin = new C(0);
            dynamic d = origin;
            while (!d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InWhileStatement1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            while (d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InDoWhileStatement0()
        {
            int failcount = 0;
            C origin = new C(0);
            dynamic d = origin;
            int count = 0;
            do
            {
                if (count == 0)
                {
                    count++;
                }
                else
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                    break;
                }
            }
            while (!d);
            return failcount == 0;
        }

        private static bool InDoWhileStatement1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            int count = 0;
            do
            {
                if (count == 0)
                {
                    count++;
                }
                else
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                    break;
                }
            }
            while (d);
            return failcount == 0;
        }

        private static bool InForStatement0()
        {
            int failcount = 0;
            C origin = new C(0);
            dynamic d = origin;
            for (; !d;)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InForStatement1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            for (; d;)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InConditionalOperator0()
        {
            int failcount = 0;
            C origin = new C(0);
            dynamic d = origin;
            if (!(d ? true : false))
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InConditionalOperator1()
        {
            int failcount = 0;
            C origin = new C(1);
            dynamic d = origin;
            if (d ? true : false)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InWhereClauseOfQueryExpression0()
        {
            int failcount = 0;
            var a = new[]
            {
            1, 2, 3
            }

            ;
            C origin = new C(0);
            dynamic d = origin;
            var q = (
                from m in a
                where d
                select m).ToArray();
            if (q[1] != a[1])
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InWhereClauseOfQueryExpression1()
        {
            int failcount = 0;
            var a = new[]
            {
            1, 2, 3
            }

            ;
            C origin = new C(1);
            dynamic d = origin;
            var q = (
                from m in a
                where !d
                select m).ToArray();
            if (q[1] != a[1])
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(InIfStatement0);
            result += Verify.Eval(InWhileStatement0);
            result += Verify.Eval(InDoWhileStatement0);
            result += Verify.Eval(InForStatement0);
            result += Verify.Eval(InConditionalOperator0);
            result += Verify.Eval(InWhereClauseOfQueryExpression0);
            result += Verify.Eval(InIfStatement1);
            result += Verify.Eval(InWhileStatement1);
            result += Verify.Eval(InDoWhileStatement1);
            result += Verify.Eval(InForStatement1);
            result += Verify.Eval(InConditionalOperator1);
            result += Verify.Eval(InWhereClauseOfQueryExpression1);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context002c.context002c
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in boolean expression </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class ConversionInBooleanExpression
    {
        public class C
        {
            public static bool operator true(C c)
            {
                return true;
            }

            public static bool operator false(C c)
            {
                return false;
            }
        }

        public class C1
        {
            public static bool operator true(C1 c)
            {
                return false;
            }

            public static bool operator false(C1 c)
            {
                return true;
            }
        }

        private static bool InIfStatement0()
        {
            int failcount = 0;
            C origin = new C();
            dynamic d = origin;
            if (d)
            {
            }
            else
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InIfStatement1()
        {
            int failcount = 0;
            C1 origin = new C1();
            dynamic d = origin;
            if (d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InWhileStatement0()
        {
            int failcount = 0;
            C origin = new C();
            dynamic d = origin;
            while (d)
            {
                failcount++; //touch here once.
                break;
            }

            return failcount == 1;
        }

        private static bool InWhileStatement1()
        {
            int failcount = 0;
            C1 origin = new C1();
            dynamic d = origin;
            while (d)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InDoWhileStatement0()
        {
            int failcount = 0;
            C origin = new C();
            dynamic d = origin;
            int count = 0;
            do
            {
                if (count == 0)
                {
                    count++; // touch here once then break;
                    break;
                }
                else
                {
                }
            }
            while (d);
            return failcount == 0 && count == 1;
        }

        private static bool InDoWhileStatement1()
        {
            int failcount = 0;
            C1 origin = new C1();
            dynamic d = origin;
            int count = 0;
            do
            {
                if (count == 0)
                {
                    count++;
                }
                else
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                    break;
                }
            }
            while (d);
            return failcount == 0;
        }

        private static bool InForStatement0()
        {
            int failcount = 0;
            int count = 0;
            C origin = new C();
            dynamic d = origin;
            for (; d;)
            {
                if (count == 0)
                {
                    count++; // touch here once then break;
                    break;
                }
                else
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                    break;
                }
            }

            return failcount == 0 && count == 1;
        }

        private static bool InForStatement1()
        {
            int failcount = 0;
            C1 origin = new C1();
            dynamic d = origin;
            for (; d;)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
                break;
            }

            return failcount == 0;
        }

        private static bool InConditionalOperator0()
        {
            int failcount = 0;
            C origin = new C();
            dynamic d = origin;
            if (!(d ? true : false))
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool InConditionalOperator1()
        {
            int failcount = 0;
            C1 origin = new C1();
            dynamic d = origin;
            if (d ? true : false)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(InIfStatement0);
            result += Verify.Eval(InWhileStatement0);
            result += Verify.Eval(InDoWhileStatement0);
            result += Verify.Eval(InForStatement0);
            result += Verify.Eval(InConditionalOperator0);
            result += Verify.Eval(InIfStatement1);
            result += Verify.Eval(InWhileStatement1);
            result += Verify.Eval(InDoWhileStatement1);
            result += Verify.Eval(InForStatement1);
            result += Verify.Eval(InConditionalOperator1);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context003.context003
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in return statement </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class ConversionInReturn
    {
        public static ulong TestingMethod(byte origin)
        {
            dynamic d = origin;
            return d;
        }

        private static bool InMethod()
        {
            int failcount = 0;
            if (TestingMethod(25) != 25)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static byte s_origin_TestingProperty;
        public static int TestingProperty
        {
            get
            {
                dynamic d = s_origin_TestingProperty;
                return d;
            }

            set
            {
                s_origin_TestingProperty = (byte)value;
            }
        }

        private static bool InPropertyGet()
        {
            int failcount = 0;
            TestingProperty = 33;
            if (TestingProperty != 33)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        public int this[byte origin]
        {
            get
            {
                dynamic d = origin;
                return d;
            }
        }

        private static bool InIndexerGet()
        {
            int failcount = 0;
            var t = new ConversionInReturn();
            if (t[33] != 33)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        public static long operator +(ConversionInReturn c, sbyte origin)
        {
            dynamic d = origin;
            return d;
        }

        private static bool InOperator()
        {
            int failcount = 0;
            var t = new ConversionInReturn();
            if ((t + 44) != 44)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(InMethod);
            result += Verify.Eval(InPropertyGet);
            result += Verify.Eval(InIndexerGet);
            result += Verify.Eval(InOperator);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context004.context004
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in yield return statement </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Collections;
    using System.Collections.Generic;

    public class NCTestingReturnIEnumeratorT : IEnumerable<long>
    {
        public NCTestingReturnIEnumeratorT(byte origin)
        {
            _origin = origin;
        }

        private byte _origin;
        IEnumerator<long> IEnumerable<long>.GetEnumerator()
        {
            dynamic d = _origin;
            yield return d;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return 1;
        }
    }

    public class NCTestingReturnIEnumerator : IEnumerable
    {
        public NCTestingReturnIEnumerator(byte origin)
        {
            _origin = origin;
        }

        private byte _origin;
        public IEnumerator GetEnumerator()
        {
            dynamic d = _origin;
            yield return d;
        }
    }

    public class ConversionInYieldReturn
    {
        public static IEnumerable TestingReturnIEnumerable(byte origin)
        {
            dynamic d = origin;
            yield return d;
        }

        private static bool IteratorReturnIEnumerable()
        {
            int failcount = 0;
            // no callsite
            //
            bool isPass = false;
            foreach (var v in TestingReturnIEnumerable(25))
            {
                if ((byte)v == 25)
                {
                    isPass = true;
                }

                break;
            }

            if (!isPass)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        public static IEnumerable<long> TestingReturnIEnumerableT(byte origin)
        {
            dynamic d = origin;
            yield return d;
        }

        private static bool IteratorReturnIEnumerableT()
        {
            int failcount = 0;
            bool isPass = false;
            foreach (var v in TestingReturnIEnumerableT(33))
            {
                if (v == 33)
                {
                    isPass = true;
                }

                break;
            }

            if (!isPass)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool IteratorReturnIEnumerator()
        {
            int failcount = 0;
            // no callsite
            //
            bool isPass = false;
            foreach (var v in new NCTestingReturnIEnumerator(25))
            {
                if ((byte)v == 25)
                {
                    isPass = true;
                }

                break;
            }

            if (!isPass)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool IteratorReturnIEnumeratorT()
        {
            int failcount = 0;
            bool isPass = false;
            foreach (var v in new NCTestingReturnIEnumeratorT(33))
            {
                if (v == 33)
                {
                    isPass = true;
                }

                break;
            }

            if (!isPass)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(IteratorReturnIEnumerable);
            result += Verify.Eval(IteratorReturnIEnumerableT);
            result += Verify.Eval(IteratorReturnIEnumerator);
            result += Verify.Eval(IteratorReturnIEnumeratorT);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context005.context005
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in evaluating the type of null coalescing operator </Title>
    // <Description>
    // Any type is implicitly convertible to dynamic
    // and dynamic is implicitly convertible to any type.
    // but previous should be high priority.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class ConversionInNullCoalescing
    {
        private static bool DynamicObjectInSecondOperandAndFirstIsNull()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d = origin;
            long? first = null;
            if ((long)(first ?? d) != 24)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool DynamicObjectInSecondOperand()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d = origin;
            long? first = 33;
            if ((long)(first ?? d) != 33)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool DynamicObjectInFirstOperand()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d = origin;
            long? second = 2;
            if ((long)(d ?? second) != 24)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool DynamicObjectInFirstOperandAndFirstIsNull()
        {
            int failcount = 0;
            dynamic d = null;
            long? second = 2;
            if ((long)(d ?? second) != 2)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(DynamicObjectInSecondOperandAndFirstIsNull);
            result += Verify.Eval(DynamicObjectInSecondOperand);
            result += Verify.Eval(DynamicObjectInFirstOperand);
            result += Verify.Eval(DynamicObjectInFirstOperandAndFirstIsNull);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context006.context006
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in evaluating the type of conditional operator </Title>
    // <Description>
    // Any type is implicitly convertible to dynamic
    // and dynamic is implicitly convertible to any type.
    // but previous should be high priority.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class ConversionInConditionalOperator
    {
        private static bool DynamicObjectInSecondAndThirdOperandAndConditionIsTrue()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d1 = origin;
            dynamic d2 = 33;
            bool cond = true;
            if ((int)(cond ? d1 : d2) != 24)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool DynamicObjectInSecondAndThirdOperandAndConditionIsFalse()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d1 = origin;
            dynamic d2 = 33;
            bool cond = false;
            if ((int)(cond ? d1 : d2) != 33)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool DynamicObjectInSecondOperandAndConditionIsTrue()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d = origin;
            long third = 33;
            bool cond = true;
            if ((long)(cond ? d : third) != 24)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool DynamicObjectInThirdOperandAndConditionIsFalse()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d = origin;
            ulong second = 33;
            bool cond = false;
            if ((ulong)(cond ? second : d) != 24)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool DynamicObjectInSecondOperandAndThirdIsNullableAndConditionIsTrue()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d = origin;
            long? third = 33;
            bool cond = true;
            if ((long)(cond ? d : third) != 24)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool DynamicObjectInThirdOperandAndSecondIsNullableAndConditionIsFalse()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d = origin;
            ulong? second = 33;
            bool cond = false;
            if ((ulong)(cond ? second : d) != 24)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(DynamicObjectInSecondAndThirdOperandAndConditionIsTrue);
            result += Verify.Eval(DynamicObjectInSecondAndThirdOperandAndConditionIsFalse);
            result += Verify.Eval(DynamicObjectInSecondOperandAndConditionIsTrue);
            result += Verify.Eval(DynamicObjectInThirdOperandAndConditionIsFalse);
            result += Verify.Eval(DynamicObjectInSecondOperandAndThirdIsNullableAndConditionIsTrue);
            result += Verify.Eval(DynamicObjectInThirdOperandAndSecondIsNullableAndConditionIsFalse);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context007.context007
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in array creation expressions </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class ConversionInArrayCreation
    {
        private static bool DynamicObjectInDimensionExpressionList()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d = origin;
            var a = new int[(int)d];
            a[0] = 10;
            if ((a[0] != 10) || (a.Length != 24))
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool DynamicObjectInArrayInitializer()
        {
            int failcount = 0;
            byte origin = 24;
            dynamic d = origin;
            dynamic d2 = 33;
            var a = new int[]
            {
            d, d2
            }

            ;
            if ((a[0] != 24) || (a[1] != 33))
            {
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(DynamicObjectInDimensionExpressionList);
            result += Verify.Eval(DynamicObjectInArrayInitializer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context008b.context008b
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in using statement </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class C : IDisposable
    {
        public static bool CalledDisposeFlag = false;
        public void Dispose()
        {
            CalledDisposeFlag = true;
        }

        public int M(int i)
        {
            return i;
        }
    }

    public class ConversionInUsingStatement
    {
        private static bool DynamicLocalVariableDefinitionInUsing()
        {
            int failcount = 0;
            C.CalledDisposeFlag = false;
            using (dynamic d = new C())
            {
                if (((int)d.M(10)) != 10)
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                }
            }

            if (!C.CalledDisposeFlag)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Didn't called the Dispose");
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(DynamicLocalVariableDefinitionInUsing);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context010.context010
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in boolean expression </Title>
    // <Description>
    // The runtime type doesn't convertible to bool
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class C1
    {
        public static bool operator true(C1 c)
        {
            return true;
        }

        public static bool operator false(C1 c)
        {
            return false;
        }
    }

    public class C2
    {
    }

    public class ConversionInUsingStatement
    {
        private static bool RuntimeTypeNotConvertibleToBoolButWithTrueOperator()
        {
            int failcount = 1;
            dynamic d = new C1();
            if (d)
            {
                failcount--;
            }
            else
            {
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }

            return failcount == 0;
        }

        private static bool RuntimeTypeNotConvertibleToBool()
        {
            int failcount = 0;
            dynamic d = new C2();
            try
            {
                if (d)
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "C2", "bool"))
                    failcount++;
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(RuntimeTypeNotConvertibleToBool);
            result += Verify.Eval(RuntimeTypeNotConvertibleToBoolButWithTrueOperator);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context011.context011
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The conversion occurs in using statement </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class C1
    {
    }

    public class C : IDisposable
    {
        public static bool CalledDisposeFlag = false;
        void IDisposable.Dispose()
        {
            CalledDisposeFlag = true;
        }

        public int M(int i)
        {
            return i;
        }
    }

    public class ConversionInUsingStatement
    {
        private static bool DynamicObjectDoesntImplementIDisposable()
        {
            int failcount = 0;
            dynamic d = new C1();
            try
            {
                using (d)
                {
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                }

                failcount++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "C1", "System.IDisposable"))
                    failcount++;
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(DynamicObjectDoesntImplementIDisposable);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.context012.context012
{
    // <Area> Dynamic -- implicit conversion</Area>
    // <Title> The dynamic value is null when converting </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class NullDynamicValue
    {
        private static bool InUsing()
        {
            int failcount = 0;
            dynamic d = null;
            try
            {
                using (IDisposable i = d)
                {
                }
            }
            catch (Exception e)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Caught an unexpected exception {0}", e);
            }

            return failcount == 0;
        }

        private static bool InAssignment()
        {
            int failcount = 0;
            dynamic d = null;
            try
            {
                int result = d;
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
            }
            catch (Exception e)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Caught an unexpected exception {0}", e);
            }

            return failcount == 0;
        }

        private static bool InBooleanExpression()
        {
            int failcount = 0;
            dynamic d = null;
            try
            {
                if (d)
                {
                    failcount++;
                    System.Console.WriteLine("Test failed: Conversion result is incorrect");
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.ValueCantBeNull, e.Message, "bool"))
                    failcount++;
            }
            catch (Exception e)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Caught an unexpected exception {0}", e);
            }

            return failcount == 0;
        }

        private static bool TestingInReturn()
        {
            dynamic d = null;
            return d;
        }

        private static bool InReturn()
        {
            int failcount = 0;
            try
            {
                TestingInReturn();
                failcount++;
                System.Console.WriteLine("Test failed: Conversion result is incorrect");
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.ValueCantBeNull, e.Message, "bool"))
                    failcount++;
            }
            catch (Exception e)
            {
                failcount++;
                System.Console.WriteLine("Test failed: Caught an unexpected exception {0}", e);
            }

            return failcount == 0;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(InUsing);
            result += Verify.Eval(InAssignment);
            result += Verify.Eval(InBooleanExpression);
            result += Verify.Eval(InReturn);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.dynamicobj001.dynamicobj001
{
    // <Area> Dynamic -- identity conversion</Area>
    // <Title> The conversion between dynamic and object as parameter type (generic)</Title>
    // <Description> List<object> is not implicitly convertible to IEnumerable<dynamic> and vice-versa - error CS0266 </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            ICollection<dynamic> v1 = new List<object>();
            ICollection<object> v2 = new List<dynamic>();
            IEnumerable<dynamic> v3 = new List<object>();
            IEnumerable<object> v4 = new List<dynamic>();
            IDictionary<dynamic, int> v5 = new Dictionary<object, int>();
            IDictionary<object, int> v6 = new Dictionary<dynamic, int>();
            IList<dynamic> v7 = new List<object>();
            IList<object> v8 = new List<dynamic>();
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.dynamicobj002.dynamicobj002
{
    // <Area> Dynamic -- identity conversion</Area>
    // <Title> The conversion between dynamic and object as parameter type (generic)</Title>
    // <Description> List<object> is not implicitly convertible to IEnumerable<dynamic> and vice-versa - error CS0266 </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        private static IEnumerable<object> s_v4 = null;
        private ICollection<object> _v6;
        private IList<object> _v8 = null;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            IEnumerable<dynamic> v3;
            v3 = new List<object>();
            s_v4 = new List<dynamic>();
            ICollection<dynamic> v5 = null;
            v5 = new List<object>();
            return 0;
        }

        private void M(int n = -1)
        {
            _v6 = new List<dynamic>();
            IList<dynamic> v7;
            v7 = new List<object>();
            _v8 = new List<dynamic>();
            IDictionary<dynamic, string> v9;
            v9 = new Dictionary<object, string>();
            IDictionary<object, decimal> v10 = new Dictionary<dynamic, decimal>();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.dynamicobj003.dynamicobj003
{
    // <Area> Dynamic -- identity conversion</Area>
    // <Title> The conversion between dynamic and object as parameter type (generic)</Title>
    // <Description> List<object> is not implicitly convertible to IEnumerable<dynamic> and vice-versa - error CS0266 </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class TestClass
    {
        [Fact]
        public void RunTest()
        {
            Test.DynamicCSharpRunTest();
        }
    }

    public struct Test
    {
        internal delegate List<object> MyDel01();
        private delegate List<dynamic> MyDel02();
        private static List<object> M4Del01()
        {
            return new List<object>();
        }

        internal List<dynamic> M4Del02()
        {
            return new List<dynamic>();
        }

        internal List<object> Prop01
        {
            get
            {
                return new List<object>();
            }
        }

        public List<dynamic> Prop02
        {
            get
            {
                return new List<dynamic>();
            }
        }

        internal List<object> this[int n]
        {
            get
            {
                return new List<object>(n);
            }
        }

        public List<dynamic> this[long n1, short n2]
        {
            get
            {
                return new List<dynamic>((int)n1);
            }
        }

        private static IEnumerable<object> s_v4 = null;
        private ICollection<object> _v6;
        private IList<object> _v8;
        private IDictionary<string, dynamic> _v9;
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            IEnumerable<dynamic> v3 = null;
            MyDel01 d01 = new MyDel01(M4Del01);
            v3 = d01();
            s_v4 = t.Prop02;
            ICollection<dynamic> v5 = t[99];
            return 0;
        }

        private void M(string s = "AAA")
        {
            _v6 = this[1, 2];
            IList<dynamic> v7;
            v7 = Prop01;
            MyDel02 d02 = new MyDel02(M4Del02);
            _v8 = d02();
            _v9 = new Dictionary<string, object>();
            IDictionary<byte, object> v10 = new Dictionary<byte, dynamic>();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.dynamicobj004.dynamicobj004
{
    // <Area> Dynamic -- identity conversion</Area>
    // <Title> The conversion between dynamic and object as parameter type (generic)</Title>
    // <Description> List<object> is not implicitly convertible to IEnumerable<dynamic> and vice-versa - error CS0266 </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            IEnumerable<dynamic> v1 = new Stack<object>();
            IEnumerable<object> v2 = new Queue<dynamic>();
            // ICollection<dynamic> v3 = new Stack<object>(); // only ICollection not ICollection<T>
            // ICollection<object> v4 = new Queue<dynamic>();
            IEnumerable<KeyValuePair<int, dynamic>> v5 = new Dictionary<int, object>();
            IEnumerable<KeyValuePair<int, object>> v6 = new SortedDictionary<int, dynamic>();
            ICollection<KeyValuePair<int, dynamic>> v7 = new Dictionary<int, object>();
            ICollection<KeyValuePair<int, object>> v8 = new Dictionary<int, dynamic>();
            IDictionary<int, dynamic> v9 = new Dictionary<int, object>();
            IDictionary<int, object> v10 = new Dictionary<int, dynamic>();
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.dynamicobj005.dynamicobj005
{
    // <Area> Dynamic -- identity conversion</Area>
    // <Title> The conversion between dynamic and object as parameter type (generic)</Title>
    // <Description> List<object> is not implicitly convertible to IEnumerable<dynamic> and vice-versa - error CS0266 </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class TestClass
    {
        [Fact]
        public void RunTest()
        {
            Test.DynamicCSharpRunTest();
        }
    }

    public struct Test
    {
        private static IEnumerable<dynamic> s_v3;
        private IList<object> _v6;
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            ICollection<dynamic> v1 = Test.SPropObj;
            ICollection<object> v2;
            v2 = t.MethDyn();
            s_v3 = t[100];
            IEnumerable<object> v4 = t.PropDyn;
            DelObj d1 = new DelObj(t.M4DelObj);
            IList<dynamic> v5 = d1(-1);
            DelDyn d2 = new DelDyn(Test.M4DelDyn);
            t._v6 = d2();
            return 0;
        }

        public static MyStack<object> SPropObj
        {
            get
            {
                return new MyStack<object>();
            }
        }

        public MyStack<dynamic> PropDyn
        {
            get
            {
                return new MyStack<dynamic>();
            }
        }

        public MyStack<dynamic> MethDyn(string s = "AAA")
        {
            return new MyStack<dynamic>();
        }

        public MyStack<object> this[long n]
        {
            get
            {
                return new MyStack<object>();
            }
        }

        private delegate MyStack<object> DelObj(int x, string s = null);
        private delegate MyStack<dynamic> DelDyn(int x = 0, int y = 0);
        public MyStack<object> M4DelObj(int x, string s = "Hi")
        {
            return new MyStack<object>();
        }

        public static MyStack<dynamic> M4DelDyn(int x = 1, int y = 1)
        {
            return new MyStack<dynamic>();
        }
    }

    public class MyStack<T> : IEnumerable<T>, ICollection<T>, IList<T>
    {
        private int _count = 0;
        private const int maxcount = 128;
        private T[] _ary = null;
        public int IndexOf(T t)
        {
            return 1;
        }

        public void Insert(int n, T t)
        {
            Push(t);
        }

        public void RemoveAt(int n)
        {
        }

        public T this[int n]
        {
            get
            {
                return Pop();
            }

            set
            {
            }
        }

        public void Clear()
        {
        }

        public bool Contains(T t)
        {
            return true;
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public int Count
        {
            get
            {
                return 1;
            }
        }

        public void CopyTo(T[] t, int n)
        {
        }

        public bool Remove(T t)
        {
            return true;
        }

        public void Add(T t)
        {
            Push(t);
        }

        public void Push(T t)
        {
            if (null == _ary)
            {
                _ary = new T[maxcount];
                _count = 0;
            }

            if (_count + 1 < maxcount)
                _ary[_count++] = t;
            else
                throw new OverflowException("max=128");
        }

        public T Pop()
        {
            if (null == _ary || 0 == _count)
            {
                return default(T);
            }

            return _ary[_count--];
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return (T)_ary[i];
            }

            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.dynamicobj006.dynamicobj006
{
    //<Area>Conversion</Area>
    //<Title>Regression</Title>
    //<Description>regression test</Description>
    //<Related bugs></Related bugs>
    //<Expects Status=success></Expects Status>
    //<Expects Status=warning>\(12,16\).*CS0649</Expects>
    //<Code>
    public interface I<T>
    {
    }

    internal struct S<T> : I<T>
    {
        public int x;
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            S<object> s = new S<object>();
            I<dynamic> d = s;
            var x = (S<object>)d;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.dynamicobj007.dynamicobj007
{
    //<Area>Conversion</Area>
    //<Title>Regression</Title>
    //<Description></Description>
    //<Related bugs>Dev11:11329</Related bugs>
    //<Expects Status=success></Expects Status>
    //<Code>
    using System;

    public class P
    {
        public class My
        {
            public void Foo()
            {
                P.s_status = 0;
            }
        }

        private static int s_status = 1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Action<object> a = (dynamic x) => x.Foo();
            a(new My());
            return s_status;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.dlgate003.dlgate003
{
    // <Title>Delegate conversions</Title>
    // <Description>
    // Tests to figure out if the right conversion from method groups to delegates are applied
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public delegate dynamic D();
        public delegate decimal D2();
        public delegate string D3();
        public delegate void D4();
        public static object Foo()
        {
            return new object();
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            //delegate returns dynamic, methods return non-dynamic
            D del = delegate ()
            {
                return 4;
            }

            ;
            var x = del();
            if (x == 4)
                rez++;
            del = () => 5;
            x = del();
            if (x == 5)
                rez++;
            del = Foo;
            var obj = del();
            if (obj != null)
                rez++;
            //delegate returns dynamic, methods return null
            del = delegate ()
            {
                return null;
            }

            ;
            obj = del();
            if (obj == null)
                rez++;
            del = () => null;
            obj = del();
            if (obj == null)
                rez++;
            //delegates returning non-dynamic, but we return dynamic
            D2 del2 = delegate ()
            {
                dynamic d = 3;
                return d;
            }

            ;
            var dyn = del2();
            if (dyn == 3)
                rez++;
            del2 = () => (dynamic)5;
            dyn = del2();
            if (dyn == 5)
                rez++;
            D3 del3 = delegate ()
            {
                dynamic d = 3;
                return d.ToString();
            }

            ;
            var dyn2 = del3();
            if (dyn2 == "3")
                rez++;
            del3 = () => (dynamic)(5.ToString());
            dyn2 = del3();
            if (dyn2 == "5")
                rez++;
            return rez == 9 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.dlgate004.dlgate004
{
    // <Title>Delegate conversions</Title>
    // <Description>
    // Tests to figure out if the right conversion from method groups to delegates are applied
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public delegate int D(string x);
        public delegate int D2(dynamic d);
        public static int Foo(dynamic x)
        {
            return 1;
        }

        public static int Foo(int x)
        {
            return x;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            D d = Foo;
            var r = d("3");
            if (r == 1)
                rez++;
            D2 dd = Foo;
            r = dd(3);
            if (r == 1)
                rez++;
            return rez == 2 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.arrayinit001.arrayinit001
{
    // <Title>Array initializer conversion</Title>
    // <Description>
    // Tests to figure out if the type of the arrays is the expected one
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            //array initializer
            dynamic d = 3;
            var arr = new[]
            {
            1, 2, d
            }

            ;
            int[] arr2 = new int[]
            {
            1, 2, d
            }

            ;
            int rez = 0;
            try
            {
                arr[0].Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    rez++;
            }

            if (arr2[2] == d)
                rez++; //this should work
            return rez == 2 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.ternary001.ternary001
{
    // <Title>Ternary operator</Title>
    // <Description>
    // // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(30,32\).*CS0429</Expects>

    public class CC
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = 3;
            int x = 3;
            int rez = 0;
            try
            {
                var t = true ? x : d;
                t.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    rez++;
            }

            try
            {
                var t = true ? d : x;
                t.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    rez++;
            }

            return rez == 2 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.using001.using001
{
    // <Title> Conversion </Title>
    // <Description> These scenarios are still using normal conversion
    //    Let dynamic object to do the conversion to IDisposable is covered in IDO test cases
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    using System;

    public struct SDImp : IDisposable
    {
        public void Dispose()
        {
            Test.Output += "SDImp";
        }
    }

    public struct SDExp : IDisposable
    {
        void IDisposable.Dispose()
        {
            Test.Output += "SDExp";
        }
    }

    public struct S
    {
        public void Dispose()
        {
            System.Console.WriteLine("S");
        }
    }

    public class Test
    {
        public class CDImp : IDisposable
        {
            public void Dispose()
            {
                Test.Output += "CDImp";
            }
        }

        public class CDExp : IDisposable
        {
            void IDisposable.Dispose()
            {
                Test.Output += "CDExp";
            }
        }

        public class C
        {
            public void Dispose()
            {
                System.Console.WriteLine("C");
            }
        }

        public static string Output;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d1 = new CDImp();
            dynamic d2 = new CDExp();
            dynamic d3 = new SDImp();
            dynamic d4 = new SDExp();

            Output = "";

            //
            using (IDisposable r1 = d1, r2 = d2, r3 = d3, r4 = d4)
            {
            }

            //
            using (d1)
            {
            }

            using (d2)
            {
            }

            using (d3)
            {
            }

            using (d4)
            {
            }

            if (Output != "SDExpSDImpCDExpCDImpCDImpCDExpSDImpSDExp")
                return 1;

            return 0 == RunTests() ? 0 : 1;
        }

        private static int RunTests()
        {
            int ret = 0;
            // class
            dynamic d = new C();
            try
            {
                using (IDisposable res = d)
                {
                }

                ret++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Test.C", "System.IDisposable"))
                    ret++;
            }

            try
            {
                using (d)
                {
                }

                ret++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Test.C", "System.IDisposable"))
                    ret++;
            }

            // struct
            dynamic dd = new S();
            try
            {
                using (IDisposable res = dd)
                {
                }

                ret++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "S", "System.IDisposable"))
                    ret++;
            }

            try
            {
                using (dd)
                {
                }

                ret++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "S", "System.IDisposable"))
                    ret++;
            }

            return ret;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.array001.array001
{
    // <Title>Array and interfaces conversions</Title>
    // <Description>
    //
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic[] arr = new dynamic[]
            {
            "x", "y", "z"
            }

            ;
            IEnumerable<dynamic> ienum = arr;
            dynamic rez = "";
            foreach (var x in ienum)
            {
                rez += x;
            }

            if (rez == "xyz")
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.array002.array002
{
    // <Title>Array and interfaces conversions</Title>
    // <Description>
    //
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            M1();
            M2();
            M3();
            return 0;
        }

        private static void M1()
        {
            //bindImplicitConversionFromNullable
            S<dynamic>? sn = null;
            I<object> fooo = sn;
        }

        private static void M2()
        {
            //bindImplicitConversionFromArray
            dynamic[] da = null;
            IEnumerable<object> ieo = da;
        }

        private static void M3()
        {
            //BindNubConversion
            S<dynamic> s = new S<dynamic>();
            S<object>? sn = s;
        }
    }

    public interface I<T>
    {
    }

    internal struct S<T> : I<T>
    {
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.array003.array003
{
    // <Title>Array and interfaces conversions</Title>
    // <Description>
    // use dynamic array as the source for a query expression
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Linq;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dr = "";
            dynamic[] darr = new dynamic[]
            {
            "x", "y"
            }

            ;
            var dr2 =
                from x in darr
                select x;
            foreach (var i in dr2)
            {
                dr += i;
            }

            return dr == "xy" ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.bug819947ulngenum.bug819947ulngenum
{
    // <Title> Interaction between object and dynamic</Title>
    // <Description>
    //      [binder] NullReferenceException thrown at runtime when doing enum comparison of a dynamic variable with ulong as the underlying enum type
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    using System;

    namespace Tests
    {
        //[Serializable]
        public enum ByteEnum : byte
        {
            Zero,
            One,
            Min = byte.MinValue,
            Max = byte.MaxValue
        }

        public enum SByteEnum : sbyte
        {
            MOne = -1,
            One = 1,
            Min = sbyte.MinValue,
            Max = sbyte.MaxValue
        }

        public enum ShortEnum : short
        {
            One = 1,
            Two,
            Three
        }

        public enum UShortEnum : ushort
        {
            One,
            AnotherOne = One,
            Two,
            Three,
            Min = ushort.MinValue,
            Max = ushort.MaxValue
        }

        [Flags]
        public enum UIntEnum : uint
        {
            Zero = 0,
            One = 1,
            Two = 2,
            Four = 4,
            Eight = 8
        }

        public enum LongEnum : long
        {
            MZero = -0,
            Zero = 0,
            Min = long.MinValue,
            Max = long.MaxValue
        }

        public enum ULongEnum : ulong
        {
            One = 1,
            Zero = 0,
            Min = ulong.MinValue,
            Max = ulong.MaxValue
        }

        public class B819947
        {
            public static int M(ULongEnum p)
            {
                return 1;
            }

            public static int M(ulong p)
            {
                return 2;
            }

            public static int M(long p)
            {
                return 3;
            }

            public static int M(object p)
            {
                return 4;
            }

            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                // original - ulong
                dynamic d;
                ulong val = 1;
                d = (ULongEnum)val;
                bool ret = (d == ULongEnum.One);
                d = (ULongEnum)ulong.MinValue;
                ret &= (d != ULongEnum.Max);
                var r1 = d + 1UL;
                ret &= r1 - 1 == d;
                // Runtime EX -> d + 1L;
                // should call enum
                ret &= 1 == M(d);
                // cast to another enum
                dynamic d1 = (LongEnum)d;
                ret &= (d1 >= LongEnum.MZero);
                // should call obj
                ret &= 4 == M(d1);
                checked
                {
                    val = ulong.MaxValue;
                    d = (ULongEnum)val;
                    ret &= (d == ULongEnum.Max); // Overflow
                }

                unchecked
                {
                    d = (ULongEnum)ulong.MaxValue;
                    ret &= !d.Equals(ULongEnum.Max - 1); // Overflow
                }

                var r2 = d - 1UL;
                // Long (works before)
                long? v2 = 0;
                d = (LongEnum)v2;
                ret &= (d >= LongEnum.MZero);
                d = (LongEnum)long.MinValue;
                ret &= d <= LongEnum.Min;
                d = (UIntEnum)0;
                ret &= d + (byte)1 < UIntEnum.Two;
                ret &= new B819947().RunTest();
                return ret ? 0 : 1;
            }

            private bool RunTest()
            {
                bool ret = true;
                dynamic d = UShortEnum.One;
                ret &= (d == UShortEnum.AnotherOne);
                d = (UShortEnum)ushort.MaxValue;
                ret &= (d > UShortEnum.Two);
                short vs = 2;
                d = (ShortEnum)vs;
                ret &= (d + (int)1 == ShortEnum.Three);
                sbyte? vn = -1;
                d = (SByteEnum)vn;
                ret &= (d + (sbyte)2 == SByteEnum.One);
                d = (ByteEnum)byte.MaxValue;
                ret &= (d > ByteEnum.Min);
                return ret;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.numeric001.numeric001
{
    // <Title>Numeric Conversions</Title>
    // <Description> </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Linq.Expressions;

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            double x = uint.MaxValue;
            dynamic d = x;
            uint i = (uint)x;
            uint j = (uint)d;
            Expression<Func<object, uint>> lambda = foo => (uint)(double)foo;
            uint res = lambda.Compile()(x);
            Func<object, uint> lambda2 = foo => (uint)(double)foo;
            uint res2 = lambda2(x);
            if (i != j)
                return 1;
            if (i != res)
                return 1;
            if (i != res2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.numeric002.numeric002
{
    // <Title>Numeric Conversions</Title>
    // <Description> </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Linq.Expressions;

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            ulong i, j;
            double x = ulong.MaxValue;
            dynamic d;

            unchecked
            {
                d = x;
                i = (ulong)x;
                j = (ulong)d;
            }

            Expression<Func<object, ulong>> lambda = foo => unchecked((ulong)(double)foo);
            ulong res = lambda.Compile()(x);
            Func<object, ulong> lambda2 = foo => unchecked((ulong)(double)foo);
            ulong res2 = lambda2(x);
            if (i != j)
                return 1;
            if (i != res)
                return 1;
            if (i != res2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.numeric003.numeric003
{
    // <Title>Numeric Conversions</Title>
    // <Description> </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Linq.Expressions;

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            double x = ushort.MaxValue;
            dynamic d = x;
            ushort i = (ushort)x;
            ushort j = (ushort)d;
            Expression<Func<object, ushort>> lambda = foo => (ushort)(double)foo;
            ushort res = lambda.Compile()(x);
            Func<object, ushort> lambda2 = foo => (ushort)(double)foo;
            ushort res2 = lambda2(x);
            if (i != j)
                return 1;
            if (i != res)
                return 1;
            if (i != res2)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.cnst001.cnst001
{
    // <Title> Conversion -- Implicit constant expression conversions </Title>
    // <Description>
    //    A constant expression of type int can be converted to type sbyte, byte, short, ushort, uint or ulong
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class MyClass
    {
        public static sbyte operator %(MyClass mine, sbyte operand)
        {
            return (sbyte)(operand + 1);
        }

        public static byte operator |(MyClass mine, byte operand)
        {
            return (byte)(operand + 2);
        }
    }

    public struct MyStruct
    {
        public static short operator /(short operand, MyStruct mine)
        {
            return (short)(operand - 1);
        }

        public static ushort operator ^(ushort operand, MyStruct mine)
        {
            return (ushort)(operand - 2);
        }
    }

    public enum Eint
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public enum Elong : long
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public enum Eshort : short
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public class Test
    {
        public const int CMemint = 10;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            const int CLocint = -20;
            int result = 0;
            dynamic dret;
            dynamic duint = (uint)1;
            dynamic dlong = (long)2;
            dynamic dulong = (ulong)3;
            dynamic dc = new MyClass();
            dynamic ds = new MyStruct();
            dynamic des = Eshort.EM0;
            dynamic del = Elong.EM3;
            dret = duint + (1 + (-1));
            if ((dret.GetType() != typeof(uint)) || (dret != (uint)1))
                result++;
            // numeric promotion to long
            dret = duint + (-1 + (-1));
            if ((dret.GetType() != typeof(long)) || (dret != -1L))
                result++;
            dret = int.MinValue - dlong;
            if ((dret.GetType() != typeof(long)) || (dret != ((long)int.MinValue - 2)))
                result++;
            dret = dulong * int.MaxValue;
            if ((dret.GetType() != typeof(ulong)) || (dret != ((ulong)int.MaxValue * 3)))
                result++;
            dret = CLocint / ds;
            if ((dret.GetType() != typeof(short)) || (dret != (short)-21))
                result++;
            dret = dc % (-1);
            if ((dret.GetType() != typeof(sbyte)) || (dret != (sbyte)0))
                result++;
            dret = ((int)Eint.EM1) & duint;
            if ((dret.GetType() != typeof(uint)) || (dret != (uint)1))
                result++;
            dret = dc | checked((int)Elong.EM2);
            if ((dret.GetType() != typeof(byte)) || (dret != (byte)4))
                result++;
            dret = CMemint ^ ds;
            if ((dret.GetType() != typeof(ushort)) || (dret != (ushort)8))
                result++;
            dret = (dulong == default(int));
            if ((dret.GetType() != typeof(bool)) || (dret != false))
                result++;
            dret = (~CMemint > dlong);
            if ((dret.GetType() != typeof(bool)) || (dret != (~10 > 2)))
                result++;
            dret = duint;
            dret += (true ? 100 : CLocint);
            if ((dret.GetType() != typeof(uint)) || (dret != (uint)101))
                result++;
            dret = des + 1;
            if ((dret.GetType() != typeof(Eshort)) || (dret != Eshort.EM1))
                result++;
            dret = (1 + 2) + des;
            if ((dret.GetType() != typeof(Eshort)) || (dret != Eshort.EM3))
                result++;
            dret = del - 1;
            if ((dret.GetType() != typeof(Elong)) || (dret != Elong.EM2))
                result++;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.cnst001b.cnst001b
{
    // <Title> Conversion -- Implicit constant expression conversions </Title>
    // <Description>
    //    A constant expression of type int can be converted to type sbyte, byte, short, ushort, uint or ulong
    //    the value of constant expression is out of the range of the destination type.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class MyClass
    {
        public static sbyte operator %(MyClass mine, sbyte operand)
        {
            return (sbyte)(operand + 1);
        }

        public static byte operator |(MyClass mine, byte operand)
        {
            return (byte)(operand + 2);
        }
    }

    internal struct MyStruct
    {
        public static short operator /(short operand, MyStruct mine)
        {
            return (short)(operand - 1);
        }

        public static ushort operator ^(ushort operand, MyStruct mine)
        {
            return (ushort)(operand - 2);
        }
    }

    internal enum Eint
    {
        EM0,
        EM1 = -1,
        EM2 = -2,
        EM3 = -3,
        EM4 = -4,
        EM5 = -5
    }

    internal enum Elong : long
    {
        EM0,
        EM1 = -1000,
        EM2 = -2000,
        EM3 = -3000,
        EM4 = -4000,
        EM5 = -5000
    }

    internal enum Eshort : short
    {
        EM0,
        EM1,
        EM2
    }

    public class Test
    {
        public const int CMemint = 65538;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            const int CLocint = -65537;
            int result = 0;
            int flag = 1;
            dynamic dret;
            dynamic duint = (uint)1;
            dynamic dlong = (long)2;
            dynamic dulong = (ulong)3;
            dynamic dc = new MyClass();
            dynamic ds = new MyStruct();
            dynamic de = Eshort.EM0;
            flag = 1;
            try
            {
                dret = dulong * int.MinValue;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*", "ulong", "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = CLocint / ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "/", "int", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dc % (int.MaxValue);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "%", "MyClass", "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dc | checked((int)Elong.EM2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "|", "MyClass", "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = CMemint ^ ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "^", "int", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = (dulong == (default(int) - 1));
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "==", "ulong", "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = ((~10) > dulong);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">", "int", "ulong"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dulong;
                dret += (true ? -100 : CLocint);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+=", "ulong", "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = de + CLocint;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "Eshort", "int"))
                {
                    flag = 0;
                }
            }

            result += flag;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.cnst002.cnst002
{
    // <Title> Conversion -- Implicit constant expression conversions </Title>
    // <Description>
    //    A constant expression of type long can be converted to ulong
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(66,33\).*CS0078</Expects>

    public class MyClass
    {
        public static ulong operator %(MyClass mine, ulong operand)
        {
            return (operand + 1);
        }

        public static ulong operator |(MyClass mine, ulong operand)
        {
            return (operand + 2);
        }
    }

    public struct MyStruct
    {
        public static ulong operator /(ulong operand, MyStruct mine)
        {
            return (operand - 1);
        }

        public static ulong operator ^(ulong operand, MyStruct mine)
        {
            return (operand - 2);
        }
    }

    public enum Eint
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public enum Elong : long
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public enum Eulong : ulong
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public class Test
    {
        public const long CMemlong = 10;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            const long CLoclong = -20;
            int result = 0;
            dynamic dret;
            dynamic dulong = 3UL;
            dynamic dc = new MyClass();
            dynamic ds = new MyStruct();
            dynamic de = Eulong.EM3;
            dret = dulong * (1L + (-1l));
            if ((dret.GetType() != typeof(ulong)) || (dret != 0UL))
                result++;
            dret = dulong + long.MaxValue;
            if ((dret.GetType() != typeof(ulong)) || (dret != ((ulong)long.MaxValue + 3UL)))
                result++;
            dret = -CLoclong / ds;
            if ((dret.GetType() != typeof(ulong)) || (dret != 19UL))
                result++;
            dret = dc % CMemlong;
            if ((dret.GetType() != typeof(ulong)) || (dret != 11UL))
                result++;
            dret = ((long)Eint.EM1) & dulong;
            if ((dret.GetType() != typeof(ulong)) || (dret != 1UL))
                result++;
            dret = dc | checked((long)Elong.EM2);
            if ((dret.GetType() != typeof(ulong)) || (dret != 4UL))
                result++;
            dret = CMemlong ^ ds;
            if ((dret.GetType() != typeof(ulong)) || (dret != 8UL))
                result++;
            dret = (dulong == default(long));
            if ((dret.GetType() != typeof(bool)) || (dret != false))
                result++;
            dret = (dulong == 1L);
            if ((dret.GetType() != typeof(bool)) || (dret != false))
                result++;
            dret = dulong;
            dret += (true ? 100 : CLoclong);
            if ((dret.GetType() != typeof(ulong)) || (dret != 103UL))
                result++;
            dret = (de + 2L);
            if ((dret.GetType() != typeof(Eulong)) || (dret != Eulong.EM5))
                result++;
            dret = (1L + de);
            if ((dret.GetType() != typeof(Eulong)) || (dret != Eulong.EM4))
                result++;
            dret = (de - 2L);
            if ((dret.GetType() != typeof(Eulong)) || (dret != Eulong.EM1))
                result++;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.cnst002b.cnst002b
{
    // <Title> Conversion -- Implicit constant expression conversions </Title>
    // <Description>
    //    A constant expression of type long can be converted to ulong. the value of constant expression is negative
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(70,38\).*CS0078</Expects>

    public class MyClass
    {
        public static ulong operator %(MyClass mine, ulong operand)
        {
            return (operand + 1);
        }

        public static ulong operator |(MyClass mine, ulong operand)
        {
            return (operand + 2);
        }
    }

    internal struct MyStruct
    {
        public static ulong operator /(ulong operand, MyStruct mine)
        {
            return (operand - 1);
        }

        public static ulong operator ^(ulong operand, MyStruct mine)
        {
            return (operand - 2);
        }
    }

    internal enum Eint
    {
        EM0,
        EM1 = -1,
        EM2 = -2,
        EM3 = -3,
        EM4 = -4,
        EM5 = -5
    }

    internal enum Elong : long
    {
        EM0,
        EM1 = -1,
        EM2 = -2,
        EM3 = -3,
        EM4 = -4,
        EM5 = -5
    }

    internal enum Eulong : ulong
    {
        EM0,
        EM1,
        EM2,
        EM3,
        EM4,
        EM5
    }

    public class Test
    {
        public const long CMemlong = -10;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            const long CLoclong = 20;
            int result = 0;
            int flag = 1;
            dynamic dret;
            dynamic dulong = 3UL;
            dynamic dc = new MyClass();
            dynamic ds = new MyStruct();
            dynamic de = Eulong.EM3;
            flag = 1;
            try
            {
                dret = dulong + (-1L + (-1l));
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "ulong", "long"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = long.MinValue * dulong;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*", "long", "ulong"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = -CLoclong / ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "/", "long", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dc % CMemlong;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "%", "MyClass", "long"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = ((long)Eint.EM1) & dulong;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "&", "long", "ulong"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dc | checked((long)Elong.EM2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "|", "MyClass", "long"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = CMemlong ^ ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "^", "long", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = (dulong == (default(long) - 1));
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "==", "ulong", "long"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = ((~(10L)) > dulong);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, ">", "long", "ulong"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dulong;
                dret += (true ? -100 : CLoclong);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+=", "ulong", "long"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = (de + -1L);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "Eulong", "long"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = (de - (-2L));
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-", "Eulong", "long"))
                {
                    flag = 0;
                }
            }

            result += flag;
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.conversions.cnst003.cnst003
{
    // <Title> Conversion -- Implicit constant expression conversions </Title>
    // <Description>
    //    No constant expression conversions for char, string, bool, float, double, decimal, byte, sbyte, short, ushort, uint, ulong
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class MyClass
    {
        public static byte operator +(MyClass mine, byte operand)
        {
            return (byte)(operand + 1);
        }

        public static sbyte operator -(MyClass mine, sbyte operand)
        {
            return (sbyte)(operand + 2);
        }

        public static int operator *(MyClass mine, int operand)
        {
            return (operand + 3);
        }
    }

    internal struct MyStruct
    {
        public static byte operator +(byte operand, MyStruct mine)
        {
            return (byte)(operand - 1);
        }

        public static sbyte operator -(sbyte operand, MyStruct mine)
        {
            return (sbyte)(operand - 2);
        }

        public static int operator *(int operand, MyStruct mine)
        {
            return (operand - 3);
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            const sbyte CLocsbyte = 0;
            const byte CLocbyte = 0;
            const short CLocshort = 0;
            const ushort CLocushort = 0;
            int result = 0;
            int flag = 1;
            dynamic dret;
            dynamic dulong = 3UL;
            dynamic dc = new MyClass();
            dynamic ds = new MyStruct();
            flag = 1;
            try
            {
                dret = dc + 1U;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "MyClass", "uint"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = 1UL - ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-", "ulong", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dc - CLocbyte;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-", "MyClass", "byte"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = CLocsbyte + ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "sbyte", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dc + CLocshort;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "MyClass", "short"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = CLocushort - ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "-", "ushort", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dc * 0.0F;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*", "MyClass", "float"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = 1.0D * ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*", "double", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dc * 0.0M;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*", "MyClass", "decimal"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = true * ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*", "bool", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = dc + '0';
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "+", "MyClass", "char"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                dret = "1" * ds;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, ex.Message, "*", "string", "MyStruct"))
                {
                    flag = 0;
                }
            }

            result += flag;
            return result;
        }
    }
    // </Code>
}
