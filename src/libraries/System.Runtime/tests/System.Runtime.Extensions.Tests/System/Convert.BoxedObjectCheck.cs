// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            yield return new object[] { byte.MinValue };
            yield return new object[] { byte.MaxValue };
            yield return new object[] { sbyte.MinValue };
            yield return new object[] { sbyte.MaxValue };
            yield return new object[] { (sbyte)0 };
            yield return new object[] { short.MinValue };
            yield return new object[] { short.MaxValue };
            yield return new object[] { (short)0 };
            yield return new object[] { ushort.MinValue };
            yield return new object[] { ushort.MaxValue };
            yield return new object[] { int.MinValue };
            yield return new object[] { int.MaxValue };
            yield return new object[] { (int)0 };
            yield return new object[] { uint.MinValue };
            yield return new object[] { uint.MaxValue };
            yield return new object[] { long.MinValue };
            yield return new object[] { long.MaxValue };
            yield return new object[] { (long)0 };
            yield return new object[] { ulong.MinValue };
            yield return new object[] { ulong.MaxValue };
            yield return new object[] { char.MinValue };
            yield return new object[] { char.MaxValue };
            yield return new object[] { (char)0 };
            yield return new object[] { double.MinValue };
            yield return new object[] { double.MaxValue };
            yield return new object[] { (double)0 };
            yield return new object[] { float.MinValue };
            yield return new object[] { float.MaxValue };
            yield return new object[] { (float)0 };
            yield return new object[] { decimal.MinValue };
            yield return new object[] { decimal.MaxValue };
            yield return new object[] { (decimal)0 };
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
            if (type == typeof(bool))
                return Convert.ChangeType(obj, typeof(bool));
            else if (type == typeof(byte))
                return Convert.ChangeType(obj, typeof(byte));
            else if (type == typeof(sbyte))
                return Convert.ChangeType(obj, typeof(sbyte));
            else if (type == typeof(short))
                return Convert.ChangeType(obj, typeof(short));
            else if (type == typeof(int))
                return Convert.ChangeType(obj, typeof(int));
            else if (type == typeof(long))
                return Convert.ChangeType(obj, typeof(long));
            else if (type == typeof(ushort))
                return Convert.ChangeType(obj, typeof(ushort));
            else if (type == typeof(uint))
                return Convert.ChangeType(obj, typeof(uint));
            else if (type == typeof(ulong))
                return Convert.ChangeType(obj, typeof(ulong));
            else if (type == typeof(IntPtr))
                return Convert.ChangeType(obj, typeof(IntPtr));
            else if (type == typeof(UIntPtr))
                return Convert.ChangeType(obj, typeof(UIntPtr));
            else if (type == typeof(char))
                return Convert.ChangeType(obj, typeof(char));
            else if (type == typeof(double))
                return Convert.ChangeType(obj, typeof(double));
            else if (type == typeof(float))
                return Convert.ChangeType(obj, typeof(float));
            else if (type == typeof(decimal))
                return Convert.ChangeType(obj, typeof(decimal));
            else
                // Not a primitive type
                return RuntimeHelpers.GetObjectValue(obj);
        }
    }
}
