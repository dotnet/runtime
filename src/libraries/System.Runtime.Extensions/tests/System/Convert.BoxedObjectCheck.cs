// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests
{
    public class ConvertBoxedObjectCheckTests
    {
        [Fact]
        public static void ChangeTypeIdentity()
        {
            object[] testValues =
            {
            true, false,
            Byte.MinValue, Byte.MaxValue,
            SByte.MinValue,SByte.MaxValue, (SByte)0,
            Int16.MinValue, Int16.MaxValue, (Int16)0,
            UInt16.MinValue, UInt16.MaxValue,
            Int32.MinValue, Int32.MaxValue, (Int32)0,
            UInt32.MinValue, UInt32.MaxValue,
            Int64.MinValue, Int64.MaxValue, (Int64)0,
            UInt64.MinValue, UInt64.MaxValue,
            Char.MinValue, Char.MaxValue, (Char)0,
            Double.MinValue, Double.MaxValue, (Double)0,
            Single.MinValue, Single.MaxValue, (Single)0,
            Decimal.MinValue, Decimal.MaxValue, (Decimal)0,
            DateTime.MinValue, DateTime.Now, DateTime.MaxValue
        };

            foreach (object obj in testValues)
            {
                object copy = GetBoxedCopy(obj);
                Assert.NotSame(obj, copy);
                Assert.Equal(obj, copy);
            }
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
