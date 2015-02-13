// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Co8826BoxedObjectCheck
{
    [Fact]
    public static void runTest()
    {
        //--------------------------------------------------------------------------
        // Variable definitions.
        //--------------------------------------------------------------------------
        object[] tests = new object[] {
                                           true, false,
                                             Byte.MinValue,   Byte.MaxValue, (Byte)0,
                                            SByte.MinValue,  SByte.MaxValue, (SByte)0,
                                            Int16.MinValue,  Int16.MaxValue, (Int16)0,
                                           UInt16.MinValue, UInt16.MaxValue, (UInt16)0,
                                            Int32.MinValue,  Int32.MaxValue, (Int32)0,
                                           UInt32.MinValue, UInt32.MaxValue, (UInt32)0,
                                            Int64.MinValue,  Int64.MaxValue, (Int64)0,
                                           UInt64.MinValue, UInt64.MaxValue, (UInt64)0,
                                             Char.MinValue,   Char.MaxValue, (Char)0,
                                           Double.MinValue, Double.MaxValue, (Double)0,
                                           Single.MinValue, Single.MaxValue, (Single)0,
                                           DateTime.MinValue, DateTime.Now, DateTime.MaxValue};
        //[] bug 108408 fix makes some assumptions about the primitive types as described below. We test some of these assumptions
        // 1) All primitives support IConvertible
        // 2) All primitives in the future will support IConvertible
        // 3) If someValue is a boxed primitive, ((IConvertible)someValue).ToType(typeof(object), null) will return a copy of someValue
        // 4) #3 will remain true in the future

        foreach (Object o in tests)
        {
            //Logger.LogInformation("Testing equals, referenceEquals with object: " + o.ToString());

            //[]==operator - should return reference equals and we expect false
            Assert.False(o == ValueTypeSafety.GetSafeObject(o), "Err_9345sgd! ==operator - should return reference equals and we expect false");

            //[]Equals method - should return value equals and we expect true
            Assert.Equal(ValueTypeSafety.GetSafeObject(o), o);
            //[]2Equals method - should return value equals and we expect true
            Assert.Equal(o, ValueTypeSafety.GetSafeObject(o));

            //[]Static Equals method - should return value equals and we expect true
            Assert.True(Object.Equals(ValueTypeSafety.GetSafeObject(o), o), "Err_93427rsg! Static Equals method - should return value equals and we expect true");

            //[]ReferenceEquals method - should return value equals and we expect false
            Assert.False(Object.ReferenceEquals(ValueTypeSafety.GetSafeObject(o), o), "Err_93427rsg! ReferenceEquals method - should return value equals and we expect false");
        }
        //[]Enum types

        //Object o1 = E.ONE;

        //Logger.LogInformation(Object.ReferenceEquals(ValueTypeSafety.GetSafeObject(o1), o1));
        //Logger.LogInformation((ValueTypeSafety.GetSafeObject(o1) == o1));

        //Logger.LogInformation(o1.GetType().IsPrimitive);

        //IConvertible icon = (IConvertible)o1;
        //Object o2 = icon.ToType(typeof(object), null);
        //Logger.LogInformation(Object.ReferenceEquals(ValueTypeSafety.GetSafeObject(o2), o2));
        //Logger.LogInformation((ValueTypeSafety.GetSafeObject(o2) == o2));
    }
}

internal class ValueTypeSafety
{
    public static object GetSafeObject(object theValue)
    {
        if (null == theValue)
            return null;
        // These are all the primitive types.
        Type valueType = theValue.GetType();
        if (valueType == typeof(Boolean))
            return Convert.ChangeType(theValue, typeof(Boolean), null);
        else if (valueType == typeof(Byte))
            return Convert.ChangeType(theValue, typeof(Byte), null);
        else if (valueType == typeof(SByte))
            return Convert.ChangeType(theValue, typeof(SByte), null);
        else if (valueType == typeof(Int16))
            return Convert.ChangeType(theValue, typeof(Int16), null);
        else if (valueType == typeof(Int32))
            return Convert.ChangeType(theValue, typeof(Int32), null);
        else if (valueType == typeof(Int64))
            return Convert.ChangeType(theValue, typeof(Int64), null);
        else if (valueType == typeof(UInt16))
            return Convert.ChangeType(theValue, typeof(UInt16), null);
        else if (valueType == typeof(UInt32))
            return Convert.ChangeType(theValue, typeof(UInt32), null);
        else if (valueType == typeof(UInt64))
            return Convert.ChangeType(theValue, typeof(UInt64), null);
        else if (valueType == typeof(IntPtr))
            return Convert.ChangeType(theValue, typeof(IntPtr), null);
        else if (valueType == typeof(UIntPtr))
            return Convert.ChangeType(theValue, typeof(UIntPtr), null);
        else if (valueType == typeof(Char))
            return Convert.ChangeType(theValue, typeof(Char), null);
        else if (valueType == typeof(Double))
            return Convert.ChangeType(theValue, typeof(Double), null);
        else if (valueType == typeof(Single))
            return Convert.ChangeType(theValue, typeof(Single), null);
        else
            // not anyone of the primitive types
            return RuntimeHelpers.GetObjectValue(theValue);
    }
}

internal enum E
{
    ONE = 1,
}
