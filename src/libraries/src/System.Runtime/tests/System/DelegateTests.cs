// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
