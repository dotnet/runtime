// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public static class TestExtensionMethod
    {
        public static DelegateTests.TestStruct TestFunc(this DelegateTests.TestClass testparam)
        {
            return testparam.structField;
        }
    }

    public static unsafe class DelegateTests
    {
        public struct TestStruct
        {
            public object o1;
            public object o2;
        }

        public class TestClass
        {
            public TestStruct structField;
        }

        private static void EmptyFunc() { }

        public delegate TestStruct StructReturningDelegate();

        [Fact]
        public static void ClosedStaticDelegate()
        {
            TestClass foo = new TestClass();
            foo.structField.o1 = new object();
            foo.structField.o2 = new object();
            StructReturningDelegate testDelegate = foo.TestFunc;
            TestStruct returnedStruct = testDelegate();
            Assert.Same(foo.structField.o1, returnedStruct.o1);
            Assert.Same(foo.structField.o2, returnedStruct.o2);
        }

        public class A { }
        public class B : A { }
        public delegate A DynamicInvokeDelegate(A nonRefParam1, B nonRefParam2, ref A refParam, out B outParam);

        public static A DynamicInvokeTestFunction(A nonRefParam1, B nonRefParam2, ref A refParam, out B outParam)
        {
            outParam = (B)refParam;
            refParam = nonRefParam2;
            return nonRefParam1;
        }

        [Fact]
        public static void DynamicInvoke()
        {
            A a1 = new A();
            A a2 = new A();
            B b1 = new B();
            B b2 = new B();

            DynamicInvokeDelegate testDelegate = DynamicInvokeTestFunction;

            // Check that the delegate behaves as expected
            A refParam = b2;
            B outParam = null;
            A returnValue = testDelegate(a1, b1, ref refParam, out outParam);
            Assert.Same(returnValue, a1);
            Assert.Same(refParam, b1);
            Assert.Same(outParam, b2);

            // Check dynamic invoke behavior
            object[] parameters = new object[] { a1, b1, b2, null };

            object retVal = testDelegate.DynamicInvoke(parameters);
            Assert.Same(retVal, a1);
            Assert.Same(parameters[2], b1);
            Assert.Same(parameters[3], b2);

            // Check invoke on a delegate that takes no parameters.
            Action emptyDelegate = EmptyFunc;
            emptyDelegate.DynamicInvoke(new object[] { });
            emptyDelegate.DynamicInvoke(null);
        }

        [Fact]
        public static void DynamicInvoke_MissingTypeForDefaultParameter_Succeeds()
        {
            // Passing Type.Missing with default.
            Delegate d = new IntIntDelegateWithDefault(IntIntMethod);
            d.DynamicInvoke(7, Type.Missing);
        }

        [Fact]
        public static void DynamicInvoke_MissingTypeForNonDefaultParameter_ThrowsArgumentException()
        {
            Delegate d = new IntIntDelegate(IntIntMethod);
            Assert.Throws<ArgumentException>("parameters", () => d.DynamicInvoke(7, Type.Missing));
        }

        [Theory]
        [InlineData(new object[] { 7 }, new object[] { 8 })]
        [InlineData(new object[] { null }, new object[] { 1 })]
        public static void DynamicInvoke_RefValueTypeParameter(object[] args, object[] expected)
        {
            Delegate d = new RefIntDelegate(RefIntMethod);
            d.DynamicInvoke(args);
            Assert.Equal(expected, args);
        }

        [Fact]
        public static void DynamicInvoke_NullRefValueTypeParameter_ReturnsValueTypeDefault()
        {
            Delegate d = new RefValueTypeDelegate(RefValueTypeMethod);
            object[] args = new object[] { null };
            d.DynamicInvoke(args);
            MyStruct s = (MyStruct)(args[0]);
            Assert.Equal(s.X, 7);
            Assert.Equal(s.Y, 8);
        }

        [Fact]
        public static void DynamicInvoke_TypeDoesntExactlyMatchRefValueType_ThrowsArgumentException()
        {
            Delegate d = new RefIntDelegate(RefIntMethod);
            Assert.Throws<ArgumentException>(null, () => d.DynamicInvoke((uint)7));
            Assert.Throws<ArgumentException>(null, () => d.DynamicInvoke(IntEnum.One));
        }

        [Theory]
        [InlineData(7, (short)7)] // uint -> int
        [InlineData(7, IntEnum.Seven)] // Enum (int) -> int
        [InlineData(7, ShortEnum.Seven)] // Enum (short) -> int
        public static void DynamicInvoke_ValuePreservingPrimitiveWidening_Succeeds(object o1, object o2)
        {
            Delegate d = new IntIntDelegate(IntIntMethod);
            d.DynamicInvoke(o1, o2);
        }

        [Theory]
        [InlineData(IntEnum.Seven, 7)]
        [InlineData(IntEnum.Seven, (short)7)]
        public static void DynamicInvoke_ValuePreservingWideningToEnum_Succeeds(object o1, object o2)
        {
            Delegate d = new EnumEnumDelegate(EnumEnumMethod);
            d.DynamicInvoke(o1, o2);
        }
        
        [Fact]
        public static void DynamicInvoke_SizePreservingNonVauePreservingConversion_ThrowsArgumentException()
        {
            Delegate d = new IntIntDelegate(IntIntMethod);
            Assert.Throws<ArgumentException>(null, () => d.DynamicInvoke(7, (uint)7));
            Assert.Throws<ArgumentException>(null, () => d.DynamicInvoke(7, U4.Seven));
        }

        [Fact]
        public static void DynamicInvoke_NullValueType_Succeeds()
        {
            Delegate d = new ValueTypeDelegate(ValueTypeMethod);
            d.DynamicInvoke(new object[] { null });
        }

        [Fact]
        public static void DynamicInvoke_ConvertMatchingTToNullable_Succeeds()
        {
            Delegate d = new NullableDelegate(NullableMethod);
            d.DynamicInvoke(7);
        }

        [Fact]
        public static void DynamicInvoke_ConvertNonMatchingTToNullable_ThrowsArgumentException()
        {
            Delegate d = new NullableDelegate(NullableMethod);
            Assert.Throws<ArgumentException>(null, () => d.DynamicInvoke((short)7));
            Assert.Throws<ArgumentException>(null, () => d.DynamicInvoke(IntEnum.Seven));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_AllPrimitiveParametersWithMissingValues()
        {
            object[] parameters = new object[13];
            for (int i = 0; i < parameters.Length; i++) { parameters[i] = Type.Missing; }

            Assert.Equal(
                "True, test, c, 2, -1, -3, 4, -5, 6, -7, 8, 9.1, 11.12",
                (string)(new AllPrimitivesWithDefaultValues(AllPrimitivesMethod)).DynamicInvoke(parameters));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_AllPrimitiveParametersWithAllExplicitValues()
        {
            Assert.Equal(
                "False, value, d, 102, -101, -103, 104, -105, 106, -107, 108, 109.1, 111.12",
                (string)(new AllPrimitivesWithDefaultValues(AllPrimitivesMethod)).DynamicInvoke(
                    new object[13]
                    {
                        false,
                        "value",
                        'd',
                        (byte)102,
                        (sbyte)-101,
                        (short)-103,
                        (UInt16)104,
                        (int)-105,
                        (UInt32)106,
                        (long)-107,
                        (UInt64)108,
                        (Single)109.1,
                        (Double)111.12
                    }
                    ));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_AllPrimitiveParametersWithSomeExplicitValues()
        {
            Assert.Equal(
                "False, test, d, 2, -101, -3, 104, -5, 106, -7, 108, 9.1, 111.12",
                (string)(new AllPrimitivesWithDefaultValues(AllPrimitivesMethod)).DynamicInvoke(
                    new object[13]
                    {
                        false,
                        Type.Missing,
                        'd',
                        Type.Missing,
                        (sbyte)-101,
                        Type.Missing,
                        (UInt16)104,
                        Type.Missing,
                        (UInt32)106,
                        Type.Missing,
                        (UInt64)108,
                        Type.Missing,
                        (Double)111.12
                    }
                    ));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_StringParameterWithMissingValue()
        {
            Assert.Equal
                ("test",
                (string)(new StringWithDefaultValue(StringMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_StringParameterWithExplicitValue()
        {
            Assert.Equal(
                "value",
                (string)(new StringWithDefaultValue(StringMethod)).DynamicInvoke(new object[] { "value" }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_ReferenceTypeParameterWithMissingValue()
        {
            Assert.Null((new ReferenceWithDefaultValue(ReferenceMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_ReferenceTypeParameterWithExplicitValue()
        {
            CustomReferenceType referenceInstance = new CustomReferenceType();
            Assert.Same(
                referenceInstance,
                (new ReferenceWithDefaultValue(ReferenceMethod)).DynamicInvoke(new object[] { referenceInstance }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_ValueTypeParameterWithMissingValue()
        {
            Assert.Equal(
                0,
                ((CustomValueType)(new ValueTypeWithDefaultValue(ValueTypeMethod)).DynamicInvoke(new object[] { Type.Missing })).Id);
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_ValueTypeParameterWithExplicitValue()
        {
            Assert.Equal(
                1,
                ((CustomValueType)(new ValueTypeWithDefaultValue(ValueTypeMethod)).DynamicInvoke(new object[] { new CustomValueType { Id = 1 } })).Id);
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DateTimeParameterWithMissingValue()
        {
            Assert.Equal(
                new DateTime(42),
                (DateTime)(new DateTimeWithDefaultValueAttribute(DateTimeMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DateTimeParameterWithExplicitValue()
        {
            Assert.Equal(
                new DateTime(43),
                (DateTime)(new DateTimeWithDefaultValueAttribute(DateTimeMethod)).DynamicInvoke(new object[] { new DateTime(43) }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DecimalParameterWithAttributeAndMissingValue()
        {
            Assert.Equal(
                new decimal(4, 3, 2, true, 1),
                (decimal)(new DecimalWithDefaultValueAttribute(DecimalMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DecimalParameterWithAttributeAndExplicitValue()
        {
            Assert.Equal(
                new decimal(12, 13, 14, true, 1),
                (decimal)(new DecimalWithDefaultValueAttribute(DecimalMethod)).DynamicInvoke(new object[] { new decimal(12, 13, 14, true, 1) }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DecimalParameterWithMissingValue()
        {
            Assert.Equal(
                3.14m,
                (decimal)(new DecimalWithDefaultValue(DecimalMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DecimalParameterWithExplicitValue()
        {
            Assert.Equal(
                103.14m,
                (decimal)(new DecimalWithDefaultValue(DecimalMethod)).DynamicInvoke(new object[] { 103.14m }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_NullableIntWithMissingValue()
        {
            Assert.Null((int?)(new NullableIntWithDefaultValue(NullableIntMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_NullableIntWithExplicitValue()
        {
            Assert.Equal(
                (int?)42,
                (int?)(new NullableIntWithDefaultValue(NullableIntMethod)).DynamicInvoke(new object[] { (int?)42 }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_EnumParameterWithMissingValue()
        {
            Assert.Equal(
                IntEnum.Seven,
                (IntEnum)(new EnumWithDefaultValue(EnumMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_OptionalParameter_WithExplicitValue()
        {
            Assert.Equal(
                "value",
                (new OptionalObjectParameter(ObjectMethod)).DynamicInvoke(new object[] { "value" }));
        }

        [Fact]
        public static void DynamicInvoke_OptionalParameter_WithMissingValue()
        {
            Assert.Equal(
                Type.Missing,
                (new OptionalObjectParameter(ObjectMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_OptionalParameterUnassingableFromMissing_WithMissingValue()
        {
            Assert.Throws<ArgumentException>(() => (new OptionalStringParameter(StringMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_ParameterSpecification_ArrayOfStrings()
        {
            Assert.Equal(
                "value",
               (new StringParameter(StringMethod)).DynamicInvoke(new string[] { "value" }));
        }

        [Fact]
        public static void DynamicInvoke_ParameterSpecification_ArrayOfMissing()
        {
            Assert.Same(
                Missing.Value,
                (new OptionalObjectParameter(ObjectMethod)).DynamicInvoke(new Missing[] { Missing.Value }));
        }

        private static void IntIntMethod(int expected, int actual)
        {
            Assert.Equal(expected, actual);
        }

        private delegate void IntIntDelegate(int expected, int actual);
        private delegate void IntIntDelegateWithDefault(int expected, int actual = 7);

        private static void RefIntMethod(ref int i) => i++;

        private delegate void RefIntDelegate(ref int i);

        private struct MyStruct
        {
            public int X;
            public int Y;
        }

        private static void RefValueTypeMethod(ref MyStruct s)
        {
            s.X += 7;
            s.Y += 8;
        }

        private delegate void RefValueTypeDelegate(ref MyStruct s);

        private static void EnumEnumMethod(IntEnum expected, IntEnum actual)
        {
            Assert.Equal(expected, actual);
        }

        private delegate void EnumEnumDelegate(IntEnum expected, IntEnum actual);

        private static void ValueTypeMethod(MyStruct s)
        {
            Assert.Equal(s.X, 0);
            Assert.Equal(s.Y, 0);
        }

        private delegate void ValueTypeDelegate(MyStruct s);

        private static void NullableMethod(int? n)
        {
            Assert.True(n.HasValue);
            Assert.Equal(n.Value, 7);
        }

        private delegate void NullableDelegate(int? s);

        private enum ShortEnum : short
        {
            One = 1,
            Seven = 7,
        }

        private enum IntEnum : int
        {
            One = 1,
            Seven = 7,
        }

        private enum U4 : uint
        {
            One = 1,
            Seven = 7,
        }

        private delegate string AllPrimitivesWithDefaultValues(
            bool boolean = true,
            string str = "test",
            char character = 'c',
            byte unsignedbyte = 2,
            sbyte signedbyte = -1,
            Int16 int16 = -3,
            UInt16 uint16 = 4,
            Int32 int32 = -5,
            UInt32 uint32 = 6,
            Int64 int64 = -7,
            UInt64 uint64 = 8,
            Single single = (Single)9.1,
            Double dbl = 11.12);

        private static string AllPrimitivesMethod(
            bool boolean,
            string str,
            char character,
            byte unsignedbyte,
            sbyte signedbyte,
            Int16 int16,
            UInt16 uint16,
            Int32 int32,
            UInt32 uint32,
            Int64 int64,
            UInt64 uint64,
            Single single,
            Double dbl)
        {
            return FormattableString.Invariant($"{boolean}, {str}, {character}, {unsignedbyte}, {signedbyte}, {int16}, {uint16}, {int32}, {uint32}, {int64}, {uint64}, {single}, {dbl}");
        }

        private delegate string StringParameter(string parameter);
        private delegate string StringWithDefaultValue(string parameter = "test");
        private static string StringMethod(string parameter)
        {
            return parameter;
        }

        private class CustomReferenceType { };

        private delegate CustomReferenceType ReferenceWithDefaultValue(CustomReferenceType parameter = null);
        private static CustomReferenceType ReferenceMethod(CustomReferenceType parameter)
        {
            return parameter;
        }

        private struct CustomValueType { public int Id; };

        private delegate CustomValueType ValueTypeWithDefaultValue(CustomValueType parameter = default(CustomValueType));
        private static CustomValueType ValueTypeMethod(CustomValueType parameter)
        {
            return parameter;
        }

        private delegate DateTime DateTimeWithDefaultValueAttribute([DateTimeConstant(42)] DateTime parameter);
        private static DateTime DateTimeMethod(DateTime parameter)
        {
            return parameter;
        }

        private delegate decimal DecimalWithDefaultValueAttribute([DecimalConstant(1, 1, 2, 3, 4)] decimal parameter);
        private delegate decimal DecimalWithDefaultValue(decimal parameter = 3.14m);
        private static decimal DecimalMethod(decimal parameter)
        {
            return parameter;
        }

        private delegate int? NullableIntWithDefaultValue(int? parameter = null);
        private static int? NullableIntMethod(int? parameter)
        {
            return parameter;
        }

        private delegate IntEnum EnumWithDefaultValue(IntEnum parameter = IntEnum.Seven);
        private static IntEnum EnumMethod(IntEnum parameter = IntEnum.Seven)
        {
            return parameter;
        }

        private delegate object OptionalObjectParameter([Optional] object parameter);
        private static object ObjectMethod(object parameter)
        {
            return parameter;
        }

        private delegate string OptionalStringParameter([Optional] string parameter);
    }
}
