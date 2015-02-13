// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public class Co6053ToBoolean_all
{
    [Fact]
    public static void FromInt32()
    {
        //[] ToBoolean( Int32 ) - Int32.MinValue
        //[] ToBoolean( Int32 ) - Int32.MaxValue
        //[] ToBoolean( Int32 ) - ((Int32) 0 )

        //// Setup Int32 Test
        Int32[] int3Array = { Int32.MinValue, Int32.MaxValue, ((Int32)0), };
        Boolean[] int3Results = { true, true, false, };

        //// Vanilla Tests
        for (int i = 0; i < int3Array.Length; i++)
        {
            Boolean result = Convert.ToBoolean(int3Array[i]);
            Assert.Equal(int3Results[i], result);
        }
    }

    [Fact]
    public static void FromInt64()
    {
        ///////////////////////////////////////////// Boolean Convert.ToBoolean( Int64 )
        //[] ToBoolean( Int64 ) - Int64.MinValue
        //[] ToBoolean( Int64 ) - Int64.MaxValue
        //[] ToBoolean( Int64 ) - ((Int64) 0 )

        //// Setup Int64 Test
        Int64[] int6Array = { Int64.MinValue, Int64.MaxValue, ((Int64)0), };
        Boolean[] int6Results = { true, true, false, };
        //// Vanilla Tests
        for (int i = 0; i < int6Array.Length; i++)
        {
            Boolean result = Convert.ToBoolean(int6Array[i]);
            Assert.Equal(int6Results[i], result);
        }
    }

    [Fact]
    public static void FromInt16()
    {
        ///////////////////////////////////////////// Boolean Convert.ToBoolean( Int16 )

        //[] ToBoolean( Int16 ) - Int16.MinValue
        //[] ToBoolean( Int16 ) - Int16.MaxValue
        //[] ToBoolean( Int16 ) - ((Int16)0)        

        //// Setup Int16 Test
        Int16[] int1Array = { Int16.MinValue, Int16.MaxValue, ((Int16)0), };
        Boolean[] int1Results = { true, true, false, };
        //// Vanilla Tests
        for (int i = 0; i < int1Array.Length; i++)
        {
            Boolean result = Convert.ToBoolean(int1Array[i]);
            Assert.Equal(int1Results[i], result);
        }
    }

    [Fact]
    public static void FromIString()
    {
        ///////////////////////////////////////////// Boolean Convert.ToBoolean( String )

        //[] ToBoolean( String ) - "True"
        //[] ToBoolean( String ) - "true "
        //[] ToBoolean( String ) - " true"
        //[] ToBoolean( String ) - " true "
        //[] ToBoolean( String ) - " false "
        //[] ToBoolean( String ) - " false"
        //[] ToBoolean( String ) - "false "
        //[] ToBoolean( String ) - "False"

        //// Setup String Test
        String[] striArray = { "True", "true ", " true", " true ", " false ", " false", "false ", "False", };
        Boolean[] striResults = { true, true, true, true, false, false, false, false, };
        //// Vanilla Tests
        for (int i = 0; i < striArray.Length; i++)
        {
            Boolean result = Convert.ToBoolean(striArray[i]);
            Assert.Equal(striResults[i], result);
        }

        Assert.False(Convert.ToBoolean(null), " Got true, expected false");

        //[] ToBoolean( String ) - send in null through string array
        String[] tsA = { null };
        Assert.False(Convert.ToBoolean(tsA[0]), " Got true, expected false");

        ///////////////////////////////////////////// []Boolean Convert.ToBoolean( String, IFormatProvider ) - IFP is ignored

        //// Vanilla Tests
        for (int i = 0; i < striArray.Length; i++)
        {
            Boolean result = Convert.ToBoolean(striArray[i], new TestFormatProvider());
            Assert.Equal(striResults[i], result);
        }

        //[] ToBoolean( String ) - send in null
        Assert.False(Convert.ToBoolean(null, new TestFormatProvider()), " Got true, expected false");

        //[] ToBoolean( String ) - send in null through string array
        Assert.False(Convert.ToBoolean(tsA[0], new TestFormatProvider()), " Got true, expected false");
    }

    [Fact]
    public static void FromByte()
    {
        ///////////////////////////////////////////// Boolean Convert.ToBoolean( Byte )

        //[] ToBoolean( Byte ) - Byte.MinValue

        //[] ToBoolean( Byte ) - Byte.MaxValue

        //// Setup Byte Test
        Byte[] SByteArray = { Byte.MinValue, Byte.MaxValue, };
        Boolean[] SByteResults = { false, true, };
        //// Vanilla Tests
        for (int i = 0; i < SByteArray.Length; i++)
        {
            Boolean result = Convert.ToBoolean(SByteArray[i]);
            Assert.Equal(SByteResults[i], result);
        }
        // Adding tests for new methods
    }

    [Fact]
    public static void FromSByte()
    {
        ///////////////////////////////////////////// Boolean Convert.ToBoolean( SByte )

        //[] ToBoolean( SByte ) - 0
        //[] ToBoolean( SByte ) - SByte.MaxValue

        //// Setup SByte Test
        SByte[] sSByteArray = { 0, SByte.MaxValue, };
        Boolean[] sSByteResults = { false, true, };
        //// Vanilla Tests
        for (int i = 0; i < sSByteArray.Length; i++)
        {
            Boolean result = Convert.ToBoolean(sSByteArray[i]);
            Assert.Equal(sSByteResults[i], result);
        }
    }

    [Fact]
    public static void FromUInt16()
    {
        ///////////////////////////////////////////// Boolean Convert.ToBoolean( UInt16 )

        //[] ToBoolean( UInt16 ) - UInt16.MinValue
        //[] ToBoolean( UInt16 ) - UInt16.MaxValue
        //[] ToBoolean( UInt16 ) - UInt16.MinValue
        //[] ToBoolean( UInt16 ) - UInt16.MaxValue
        //[] ToBoolean( UInt16 ) - ((UInt16)0)

        //// Setup UInt16 Test
        UInt16[] UInt3216Array = { UInt16.MinValue, UInt16.MaxValue, UInt16.MinValue, UInt16.MaxValue, ((UInt16)0), };
        Boolean[] UInt3216Results = { false, true, false, true, false, };
        //// Vanilla Tests
        for (int i = 0; i < UInt3216Array.Length; i++)
        {
            Boolean result = Convert.ToBoolean(UInt3216Array[i]);
            Assert.Equal(UInt3216Results[i], result);
        }
    }

    [Fact]
    public static void FromUInt32()
    {
        ///////////////////////////////////////////// Boolean Convert.ToBoolean( UInt32 )

        //[] ToBoolean( UInt32 ) - UInt32.MinValue
        //[] ToBoolean( UInt32 ) - UInt32.MaxValue

        //// Setup UInt32 Test
        UInt32[] UInt3232Array = { UInt32.MinValue, UInt32.MaxValue };
        Boolean[] UInt3232Results = { false, true, false, };
        //// Vanilla Tests
        for (int i = 0; i < UInt3232Array.Length; i++)
        {
            Boolean result = Convert.ToBoolean(UInt3232Array[i]);
            Assert.Equal(UInt3232Results[i], result);
        }
    }

    [Fact]
    public static void FromUInt64()
    {
        ///////////////////////////////////////////// Boolean Convert.ToBoolean( UInt64 )

        //[] ToBoolean( UInt64 ) - UInt64.MinValue
        //[] ToBoolean( UInt64 ) - UInt64.MaxValue
        //[] ToBoolean( UInt64 ) - new UInt64()

        //// Setup UInt64 Test
        UInt64[] UInt3264Array = { UInt64.MinValue, UInt64.MaxValue, new UInt64(), };
        Boolean[] UInt3264Results = { false, true, false, };
        //// Vanilla Tests
        for (int i = 0; i < UInt3264Array.Length; i++)
        {
            Boolean result = Convert.ToBoolean(UInt3264Array[i]);
            Assert.Equal(UInt3264Results[i], result);
        }
    }

    [Fact]
    public static void FromBoolean()
    {
        ///////////////////////////////////////////// Boolean Convert.ToBoolean( Boolean )

        //[] ToBoolean( Boolean ) - true
        //[] ToBoolean( Boolean ) - false

        //// Setup UInt64 Test
        Boolean[] Boolean3312Array = { true, false };
        Boolean[] Boolean3312Results = { true, false, false };
        //// Vanilla Tests
        for (int i = 0; i < Boolean3312Array.Length; i++)
        {
            Boolean result = Convert.ToBoolean(Boolean3312Array[i]);
            Assert.Equal(Boolean3312Results[i], result);
        }
    }

    [Fact]
    public static void FromSingle()
    {
        // Adding tests for new methods
        ///////////////////////////////////////////// [] Boolean Convert.ToBoolean( Single )

        Single[] sglArray = { Single.Epsilon, Single.MaxValue, Single.MinValue, Single.NaN, Single.NegativeInfinity, Single.PositiveInfinity, 0f, 0.0f, 1.5f, -1.5f, 1.5e30f, -1.7e-100f, -1.7e30f, -1.7e-40f, };
        Boolean[] sglResults = { true, true, true, true, true, true, false, false, true, true, true, false, true, true, };
        //// Vanilla Tests
        for (int i = 0; i < sglArray.Length; i++)
        {
            Boolean result = Convert.ToBoolean(sglArray[i]);
            Assert.Equal(sglResults[i], result);
        }
    }

    [Fact]
    public static void FromDouble()
    {
        ///////////////////////////////////////////// [] Boolean Convert.ToBoolean( Double )

        Double[] dblArray = { Double.Epsilon, Double.MaxValue, Double.MinValue, Double.NaN, Double.NegativeInfinity, Double.PositiveInfinity, 0d, 0.0, 1.5, -1.5, 1.5e300, -1.7e-500, -1.7e300, -1.7e-320, };
        Boolean[] dblResults = { true, true, true, true, true, true, false, false, true, true, true, false, true, true, };

        //// Vanilla Tests
        for (int i = 0; i < dblArray.Length; i++)
        {
            Boolean result = Convert.ToBoolean(dblArray[i]);
            Assert.Equal(dblResults[i], result);
        }
    }

    [Fact]
    public static void FromDecimal()
    {
        ///////////////////////////////////////////// [] Boolean Convert.ToBoolean( Decimal )

        Decimal[] decArray = { Decimal.MaxValue, Decimal.MinValue, Decimal.One, Decimal.Zero, 0m, 0.0m, 1.5m, -1.5m, 500.00m, };
        Boolean[] dcmResults = { true, true, true, false, false, false, true, true, true, };
        //// Vanilla Tests
        for (int i = 0; i < decArray.Length; i++)
        {
            Boolean result = Convert.ToBoolean(decArray[i]);
            Assert.Equal(dcmResults[i], result);
        }
    }

    [Fact]
    public static void FromObject()
    {
        /////////////////////////// Boolean Convert.ToBoolean( Object )

        //[] ToBoolean( Object ) - obj = null
        Assert.False(Convert.ToBoolean((Object)null), " wrong value returned.  expected false");

        //[] ToBoolean( Object, IFP ) - obj = null

        Boolean bTest = Convert.ToBoolean((Object)null, new TestFormatProvider());
        Assert.False(bTest, " wrong value returned.  expected false, got " + bTest);
    }

    [Fact]
    public static void runTest_Negative1()
    {
        //[] ExceptionTest: ToBoolean( String ) - send in "Blah"
        Assert.Throws<FormatException>(() => Convert.ToBoolean("Blah"));
        //[] ExceptionTest: ToBoolean( String ) - send in "Blah"
        Assert.Throws<FormatException>(() => Convert.ToBoolean("Blah", new TestFormatProvider()));
    }

    [Fact]
    public static void runTest_Negative3()
    {
        ///////////////////////////////////////////// []Boolean Convert.ToBoolean( DateTime ) - throws
        Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(DateTime.Now));
    }

    [Fact]
    public static void runTest_Negative4()
    {
        ////////////////////[] ToBoolean( Object ) - Exception Case (Object that does not implement IConvertible) 
        Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(new Object()));
    }

    [Fact]
    public static void runTest_Negative5()
    {
        ///////////////////////////////////////////// []Boolean Convert.ToBoolean( Object, IFormatPRovider )
        Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(new Object(), new TestFormatProvider()));
    }
}

/// <summary>
/// Helper class to test that the IFormatProvider is being called.
/// </summary>
internal class TestFormatProvider : IFormatProvider, ICustomFormatter
{
    public object GetFormat(Type formatType)
    {
        return this;
    }

    public string Format(string format, object arg, IFormatProvider formatProvider)
    {
        return arg.ToString();
    }
}
