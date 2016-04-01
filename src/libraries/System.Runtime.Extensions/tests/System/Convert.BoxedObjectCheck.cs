// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public class ConvertBoxedObjectCheckTests
    {
        public static IEnumerable<object[]> DefaultToTypeValues()
        {
            yield return new object[] { true };
            yield return new object[] { false };
            yield return new object[] { Byte.MinValue };
            yield return new object[] { Byte.MaxValue };
            yield return new object[] { SByte.MinValue };
            yield return new object[] { SByte.MaxValue };
            yield return new object[] { (SByte)0 };
            yield return new object[] { Int16.MinValue };
            yield return new object[] { Int16.MaxValue };
            yield return new object[] { (Int16)0 };
            yield return new object[] { UInt16.MinValue };
            yield return new object[] { UInt16.MaxValue };
            yield return new object[] { Int32.MinValue };
            yield return new object[] { Int32.MaxValue };
            yield return new object[] { (Int32)0 };
            yield return new object[] { UInt32.MinValue };
            yield return new object[] { UInt32.MaxValue };
            yield return new object[] { Int64.MinValue };
            yield return new object[] { Int64.MaxValue };
            yield return new object[] { (Int64)0 };
            yield return new object[] { UInt64.MinValue };
            yield return new object[] { UInt64.MaxValue };
            yield return new object[] { Char.MinValue };
            yield return new object[] { Char.MaxValue };
            yield return new object[] { (Char)0 };
            yield return new object[] { Double.MinValue };
            yield return new object[] { Double.MaxValue };
            yield return new object[] { (Double)0 };
            yield return new object[] { Single.MinValue };
            yield return new object[] { Single.MaxValue };
            yield return new object[] { (Single)0 };
            yield return new object[] { Decimal.MinValue };
            yield return new object[] { Decimal.MaxValue };
            yield return new object[] { (Decimal)0 };
            yield return new object[] { DateTime.MinValue };
            yield return new object[] { DateTime.Now };
            yield return new object[] { DateTime.MaxValue };
        }

        [Theory]
        [MemberData(nameof(DefaultToTypeValues))]
        public static void TestConvertedCopies(object testValue)
        {
            Assert.All(DefaultToTypeValues(), input =>
            {
                try
                {
                    object converted = ((IConvertible)testValue).ToType(input[0].GetType(), null);
                    Assert.NotSame(testValue, converted);
                }
                catch (InvalidCastException) { }
                catch (OverflowException) { }
            });
        }

        [Theory]
        [MemberData(nameof(DefaultToTypeValues))]
        public static void ChangeTypeIdentity(object testValue)
        {
            object copy = GetBoxedCopy(testValue);
            Assert.NotSame(testValue, copy);
            Assert.Equal(testValue, copy);
        }

        public static object GetBoxedCopy(object obj)
        {
            Type type = obj.GetType();
            if (type == typeof(Boolean))
                return Convert.ChangeType(obj, typeof(Boolean));
            else if (type == typeof(Byte))
                return Convert.ChangeType(obj, typeof(Byte));
            else if (type == typeof(SByte))
                return Convert.ChangeType(obj, typeof(SByte));
            else if (type == typeof(Int16))
                return Convert.ChangeType(obj, typeof(Int16));
            else if (type == typeof(Int32))
                return Convert.ChangeType(obj, typeof(Int32));
            else if (type == typeof(Int64))
                return Convert.ChangeType(obj, typeof(Int64));
            else if (type == typeof(UInt16))
                return Convert.ChangeType(obj, typeof(UInt16));
            else if (type == typeof(UInt32))
                return Convert.ChangeType(obj, typeof(UInt32));
            else if (type == typeof(UInt64))
                return Convert.ChangeType(obj, typeof(UInt64));
            else if (type == typeof(IntPtr))
                return Convert.ChangeType(obj, typeof(IntPtr));
            else if (type == typeof(UIntPtr))
                return Convert.ChangeType(obj, typeof(UIntPtr));
            else if (type == typeof(Char))
                return Convert.ChangeType(obj, typeof(Char));
            else if (type == typeof(Double))
                return Convert.ChangeType(obj, typeof(Double));
            else if (type == typeof(Single))
                return Convert.ChangeType(obj, typeof(Single));
            else if (type == typeof(Decimal))
                return Convert.ChangeType(obj, typeof(Decimal));
            else
                // Not a primitive type
                return RuntimeHelpers.GetObjectValue(obj);
        }
    }
}
