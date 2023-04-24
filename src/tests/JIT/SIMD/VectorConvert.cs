// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;

partial class VectorTest
{
    const int Pass = 100;
    const int Fail = -1;

    const int DefaultSeed = 20010415;
    static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    static Random random;
    // Arrays to use for creating random Vectors.
    static Double[] doubles;
    static Single[] singles;
    static Int64[] int64s;
    static UInt64[] uint64s;
    static Int32[] int32s;
    static UInt32[] uint32s;
    static Int16[] int16s;
    static UInt16[] uint16s;
    static SByte[] sbytes;
    static Byte[] bytes;

    // Arrays with some boundary condition values
    static Double[] firstDoubles;
    static Single[] firstSingles;
    static Int64[] firstInt64s;
    static UInt64[] firstUInt64s;
    static Int32[] firstInt32s;
    static UInt32[] firstUInt32s;
    static Int16[] firstInt16s;
    static UInt16[] firstUInt16s;
    static SByte[] firstSBytes;
    static Byte[] firstBytes;

    static VectorTest()
    {
        doubles = new Double[Vector<Double>.Count];
        singles = new Single[Vector<Single>.Count];
        int64s = new Int64[Vector<Int64>.Count];
        uint64s = new UInt64[Vector<UInt64>.Count];
        int32s = new Int32[Vector<Int32>.Count];
        uint32s = new UInt32[Vector<UInt32>.Count];
        int16s = new Int16[Vector<Int16>.Count];
        uint16s = new UInt16[Vector<UInt16>.Count];
        sbytes = new SByte[Vector<SByte>.Count];
        bytes = new Byte[Vector<Byte>.Count];
        unchecked {
            firstDoubles = new Double[] { Double.MinValue, Double.MaxValue,
                                        (Double)Int64.MinValue, (Double)(Int64.MinValue),
                                        (Double)(UInt64.MinValue), (Double)(UInt64.MaxValue) };
            firstSingles = new Single[] { Single.MinValue, Single.MaxValue,
                                        (Single)Int32.MinValue, (Single)(Int32.MinValue),
                                        (Single)(UInt32.MinValue), (Single)(UInt32.MaxValue) };
            firstInt64s = new Int64[] { Int64.MinValue, Int64.MaxValue,
                                        (Int64)Double.MinValue, (Int64)(Double.MinValue),
                                        (Int64)(UInt64.MinValue), (Int64)(UInt64.MaxValue) };
            firstUInt64s = new UInt64[] { UInt64.MinValue, UInt64.MaxValue,
                                        (UInt64)Double.MinValue, (UInt64)(Double.MinValue),
                                        (UInt64)(Int64.MinValue), (UInt64)(Int64.MaxValue) };
            firstInt32s = new Int32[] { Int32.MinValue, Int32.MaxValue,
                                        (Int32)Single.MinValue, (Int32)(Single.MinValue),
                                        (Int32)(UInt32.MinValue), (Int32)(UInt32.MaxValue) };
            firstUInt32s = new UInt32[] { UInt32.MinValue, UInt32.MaxValue,
                                        (UInt32)Single.MinValue, (UInt32)(Single.MinValue),
                                        (UInt32)(Int32.MinValue), (UInt32)(Int32.MaxValue) };
            firstInt16s = new Int16[] { Int16.MinValue, Int16.MaxValue,
                                       SByte.MinValue, SByte.MaxValue,
                                       (Int16)(UInt16.MinValue), (Int16)(UInt16.MaxValue) };
            firstUInt16s = new UInt16[] { UInt16.MinValue, UInt16.MaxValue,
                                       Byte.MinValue, Byte.MaxValue,
                                       (UInt16)(Int16.MinValue), (UInt16)(Int16.MaxValue) };
            firstSBytes = new SByte[] { SByte.MinValue, SByte.MaxValue,
                                       (SByte)(Byte.MinValue), (SByte)(Byte.MaxValue) };
            firstBytes = new Byte[] { Byte.MinValue, Byte.MaxValue,
                                       (Byte)(SByte.MinValue), (Byte)(SByte.MaxValue) };
        }

        random = new Random(Seed);
    }

    static T getRandomValue<T>(int i, int j) where T : struct
    {
        int element = i + j * Vector<T>.Count;
        int sign = (random.Next(0, 2) < 1) ? -1 : 1;
        double randomDouble = random.NextDouble();
        if (typeof(T) == typeof(float))
        {
            if (element < firstSingles.Length) return (T)(object)firstSingles[element];
            float floatValue = (float)randomDouble * (float)(Int32.MaxValue) * (float)sign;
            return (T)(object)floatValue;
        }
        if (typeof(T) == typeof(double))
        {
            if (element < firstDoubles.Length) return (T)(object)firstDoubles[element];
            return (T)(object)(randomDouble * (double)(Int64.MaxValue) * (double)sign);
        }
        if (typeof(T) == typeof(Int64))
        {
            if (element < firstInt64s.Length) return (T)(object)firstInt64s[element];
            return (T)(object)(Int64)(randomDouble * (double)(Int64.MaxValue) * (double)sign);
        }
        if (typeof(T) == typeof(UInt64))
        {
            if (element < firstUInt64s.Length) return (T)(object)firstUInt64s[element];
            return (T)(object)(UInt64)(randomDouble * (double)(Int64.MaxValue));
        }
        if ((typeof(T) == typeof(Int32)) && (element < firstInt32s.Length)) return (T)(object)firstInt32s[element];
        if ((typeof(T) == typeof(UInt32)) && (element < firstUInt32s.Length)) return (T)(object)firstUInt32s[element];
        if ((typeof(T) == typeof(Int16)) && (element < firstInt16s.Length)) return (T)(object)firstInt16s[element];
        if ((typeof(T) == typeof(UInt16)) && (element < firstUInt16s.Length)) return (T)(object)firstUInt16s[element];
        if ((typeof(T) == typeof(SByte)) && (element < firstSBytes.Length)) return (T)(object)firstSBytes[element];
        if ((typeof(T) == typeof(Byte)) && (element < firstBytes.Length)) return (T)(object)firstBytes[element];

        int intValue = sign * (int)(randomDouble * (double)(Int32.MaxValue));
        T value = GetValueFromInt<T>(intValue);
        return value;
    }

    static Vector<T> getRandomVector<T>(T[] valueArray, int j) where T : struct
    {
        for (int i = 0; i < Vector<T>.Count; i++)
        {
            valueArray[i] = getRandomValue<T>(i, j);
        }
        return new Vector<T>(valueArray);
    }

    class VectorConvertTest
    {
        public static int VectorConvertSingleInt(Vector<Single> A)
        {
            Vector<Int32> B = Vector.ConvertToInt32(A);
            Vector<Single> C = Vector.ConvertToSingle(B);

            int returnVal = Pass;
            for (int i = 0; i < Vector<Single>.Count; i++)
            {
                Int32 int32Val = (Int32)A[i];
                Single cvtSglVal = (Single)int32Val;
                if (B[i] != int32Val)
                {
                    Console.WriteLine("B[" + i + "] = " + B[i] + ", int32Val = " + int32Val);
                    returnVal = Fail;
                }
                if (C[i] != cvtSglVal)
                {
                    Console.WriteLine("C[" + i + "] = " + C[i] + ", cvtSglVal = " + cvtSglVal);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }

        public static int VectorConvertSingleUInt(Vector<Single> A)
        {
            Vector<UInt32> B = Vector.ConvertToUInt32(A);
            Vector<Single> C = Vector.ConvertToSingle(B);

            int returnVal = Pass;
            for (int i = 0; i < Vector<Single>.Count; i++)
            {
                UInt32 uint32Val = (UInt32)A[i];
                Single cvtSglVal = (Single)uint32Val;
                if ((B[i] != uint32Val) || (C[i] != cvtSglVal))
                {
                    Console.WriteLine("A[{0}] = {1}, B[{0}] = {2}, C[{0}] = {3}, uint32Val = {4}, cvtSglVal = {5}",
                                      i, A[i], B[i], C[i], uint32Val, cvtSglVal);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }

        public static int VectorConvertDoubleInt64(Vector<Double> A)
        {
            Vector<Int64> B = Vector.ConvertToInt64(A);
            Vector<Double> C = Vector.ConvertToDouble(B);

            int returnVal = Pass;
            for (int i = 0; i < Vector<Double>.Count; i++)
            {
                Int64 int64Val = (Int64)A[i];
                Double cvtDblVal = (Double)int64Val;
                if (B[i] != int64Val)
                {
                    Console.WriteLine("B[" + i + "] = " + B[i] + ", int64Val = " + int64Val);
                    returnVal = Fail;
                }
                if (C[i] != cvtDblVal)
                {
                    Console.WriteLine("C[" + i + "] = " + C[i] + ", cvtDblVal = " + cvtDblVal);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }

        public static int VectorConvertDoubleUInt64(Vector<Double> A)
        {
            Vector<UInt64> B = Vector.ConvertToUInt64(A);
            Vector<Double> C = Vector.ConvertToDouble(B);

            int returnVal = Pass;
            for (int i = 0; i < Vector<Double>.Count; i++)
            {
                UInt64 uint64Val = (UInt64)A[i];
                Double cvtDblVal = (Double)uint64Val;
                if ((B[i] != uint64Val) || (C[i] != cvtDblVal))
                {
                    Console.WriteLine("A[{0}] = {1}, B[{0}] = {2}, C[{0}] = {3}, uint64Val = {4}, cvtDblVal = {5}",
                                      i, A[i], B[i], C[i], uint64Val, cvtDblVal);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }

        public static int VectorConvertDoubleSingle(Vector<Double> A1, Vector<Double> A2)
        {
            Vector<Single> B = Vector.Narrow(A1, A2);
            Vector<Double> C1, C2;
            Vector.Widen(B, out C1, out C2);

            int returnVal = Pass;
            for (int i = 0; i < Vector<Double>.Count; i++)
            {
                Single sglVal1 = (Single)A1[i];
                Single sglVal2 = (Single)A2[i];
                Double dblVal1 = (Double)sglVal1;
                Double dblVal2 = (Double)sglVal2;
                if (B[i] != sglVal1)
                {
                    Console.WriteLine("B[" + i + "] = " + B[i] + ", sglVal1 = " + sglVal1);
                    returnVal = Fail;
                }
                int i2 = i + Vector<Double>.Count;
                if (B[i2] != sglVal2)
                {
                    Console.WriteLine("B[" + i2 + "] = " + B[i2] + ", sglVal2 = " + sglVal2);
                    returnVal = Fail;
                }
                if (C1[i] != dblVal1)
                {
                    Console.WriteLine("C1[" + i + "] = " + C1[i] + ", dblVal1 = " + dblVal1);
                    returnVal = Fail;
                }
                if (C2[i] != dblVal2)
                {
                    Console.WriteLine("C2[" + i + "] = " + C2[i] + ", dblVal2 = " + dblVal2);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }

        public static int VectorConvertInt64And32(Vector<Int64> A1, Vector<Int64> A2)
        {
            Vector<Int32> B = Vector.Narrow(A1, A2);
            Vector<Int64> C1, C2;
            Vector.Widen(B, out C1, out C2);

            int returnVal = Pass;
            for (int i = 0; i < Vector<Int64>.Count; i++)
            {
                Int32 smallVal1 = (Int32)A1[i];
                Int32 smallVal2 = (Int32)A2[i];
                Int64 largeVal1 = (Int64)smallVal1;
                Int64 largeVal2 = (Int64)smallVal2;
                if (B[i] != smallVal1)
                {
                    Console.WriteLine("B[" + i + "] = " + B[i] + ", smallVal1 = " + smallVal1);
                    returnVal = Fail;
                }
                int i2 = i + Vector<Int64>.Count;
                if (B[i2] != smallVal2)
                {
                    Console.WriteLine("B[" + i2 + "] = " + B[i2] + ", smallVal2 = " + smallVal2);
                    returnVal = Fail;
                }
                if (C1[i] != largeVal1)
                {
                    Console.WriteLine("C1[" + i + "] = " + C1[i] + ", largeVal1 = " + largeVal1);
                    returnVal = Fail;
                }
                if (C2[i] != largeVal2)
                {
                    Console.WriteLine("C2[" + i + "] = " + C2[i] + ", largeVal2 = " + largeVal2);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }

        public static int VectorConvertInt32And16(Vector<Int32> A1, Vector<Int32> A2)
        {
            Vector<Int16> B = Vector.Narrow(A1, A2);
            Vector<Int32> C1, C2;
            Vector.Widen(B, out C1, out C2);

            int returnVal = Pass;
            for (int i = 0; i < Vector<Int32>.Count; i++)
            {
                Int16 smallVal1 = (Int16)A1[i];
                Int16 smallVal2 = (Int16)A2[i];
                Int32 largeVal1 = (Int32)smallVal1;
                Int32 largeVal2 = (Int32)smallVal2;
                if (B[i] != smallVal1)
                {
                    Console.WriteLine("B[" + i + "] = " + B[i] + ", smallVal1 = " + smallVal1);
                    returnVal = Fail;
                }
                int i2 = i + Vector<Int32>.Count;
                if (B[i2] != smallVal2)
                {
                    Console.WriteLine("B[" + i2 + "] = " + B[i2] + ", smallVal2 = " + smallVal2);
                    returnVal = Fail;
                }
                if (C1[i] != largeVal1)
                {
                    Console.WriteLine("C1[" + i + "] = " + C1[i] + ", largeVal1 = " + largeVal1);
                    returnVal = Fail;
                }
                if (C2[i] != largeVal2)
                {
                    Console.WriteLine("C2[" + i + "] = " + C2[i] + ", largeVal2 = " + largeVal2);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }
        
        public static int VectorConvertInt16And8(Vector<Int16> A1, Vector<Int16> A2)
        {
            Vector<SByte> B = Vector.Narrow(A1, A2);
            Vector<Int16> C1, C2;
            Vector.Widen(B, out C1, out C2);

            int returnVal = Pass;
            for (int i = 0; i < Vector<Int16>.Count; i++)
            {
                SByte smallVal1 = (SByte)A1[i];
                SByte smallVal2 = (SByte)A2[i];
                Int16 largeVal1 = (Int16)smallVal1;
                Int16 largeVal2 = (Int16)smallVal2;
                if (B[i] != smallVal1)
                {
                    Console.WriteLine("B[" + i + "] = " + B[i] + ", smallVal1 = " + smallVal1);
                    returnVal = Fail;
                }
                int i2 = i + Vector<Int16>.Count;
                if (B[i2] != smallVal2)
                {
                    Console.WriteLine("B[" + i2 + "] = " + B[i2] + ", smallVal2 = " + smallVal2);
                    returnVal = Fail;
                }
                if (C1[i] != largeVal1)
                {
                    Console.WriteLine("C1[" + i + "] = " + C1[i] + ", largeVal1 = " + largeVal1);
                    returnVal = Fail;
                }
                if (C2[i] != largeVal2)
                {
                    Console.WriteLine("C2[" + i + "] = " + C2[i] + ", largeVal2 = " + largeVal2);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }
        
        public static int VectorConvertUInt64And32(Vector<UInt64> A1, Vector<UInt64> A2)
        {
            Vector<UInt32> B = Vector.Narrow(A1, A2);
            Vector<UInt64> C1, C2;
            Vector.Widen(B, out C1, out C2);

            int returnVal = Pass;
            for (int i = 0; i < Vector<UInt64>.Count; i++)
            {
                UInt32 smallVal1 = (UInt32)A1[i];
                UInt32 smallVal2 = (UInt32)A2[i];
                UInt64 largeVal1 = (UInt64)smallVal1;
                UInt64 largeVal2 = (UInt64)smallVal2;
                if (B[i] != smallVal1)
                {
                    Console.WriteLine("B[" + i + "] = " + B[i] + ", smallVal1 = " + smallVal1);
                    returnVal = Fail;
                }
                int i2 = i + Vector<UInt64>.Count;
                if (B[i2] != smallVal2)
                {
                    Console.WriteLine("B[" + i2 + "] = " + B[i2] + ", smallVal2 = " + smallVal2);
                    returnVal = Fail;
                }
                if (C1[i] != largeVal1)
                {
                    Console.WriteLine("C1[" + i + "] = " + C1[i] + ", largeVal1 = " + largeVal1);
                    returnVal = Fail;
                }
                if (C2[i] != largeVal2)
                {
                    Console.WriteLine("C2[" + i + "] = " + C2[i] + ", largeVal2 = " + largeVal2);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }

        public static int VectorConvertUInt32And16(Vector<UInt32> A1, Vector<UInt32> A2)
        {
            Vector<UInt16> B = Vector.Narrow(A1, A2);
            Vector<UInt32> C1, C2;
            Vector.Widen(B, out C1, out C2);

            int returnVal = Pass;
            for (int i = 0; i < Vector<UInt32>.Count; i++)
            {
                UInt16 smallVal1 = (UInt16)A1[i];
                UInt16 smallVal2 = (UInt16)A2[i];
                UInt32 largeVal1 = (UInt32)smallVal1;
                UInt32 largeVal2 = (UInt32)smallVal2;
                if (B[i] != smallVal1)
                {
                    Console.WriteLine("B[" + i + "] = " + B[i] + ", smallVal1 = " + smallVal1);
                    returnVal = Fail;
                }
                int i2 = i + Vector<UInt32>.Count;
                if (B[i2] != smallVal2)
                {
                    Console.WriteLine("B[" + i2 + "] = " + B[i2] + ", smallVal2 = " + smallVal2);
                    returnVal = Fail;
                }
                if (C1[i] != largeVal1)
                {
                    Console.WriteLine("C1[" + i + "] = " + C1[i] + ", largeVal1 = " + largeVal1);
                    returnVal = Fail;
                }
                if (C2[i] != largeVal2)
                {
                    Console.WriteLine("C2[" + i + "] = " + C2[i] + ", largeVal2 = " + largeVal2);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }
        
        public static int VectorConvertUInt16And8(Vector<UInt16> A1, Vector<UInt16> A2)
        {
            Vector<Byte> B = Vector.Narrow(A1, A2);
            Vector<UInt16> C1, C2;
            Vector.Widen(B, out C1, out C2);

            int returnVal = Pass;
            for (int i = 0; i < Vector<UInt16>.Count; i++)
            {
                Byte smallVal1 = (Byte)A1[i];
                Byte smallVal2 = (Byte)A2[i];
                UInt16 largeVal1 = (UInt16)smallVal1;
                UInt16 largeVal2 = (UInt16)smallVal2;
                if (B[i] != smallVal1)
                {
                    Console.WriteLine("B[" + i + "] = " + B[i] + ", smallVal1 = " + smallVal1);
                    returnVal = Fail;
                }
                int i2 = i + Vector<UInt16>.Count;
                if (B[i2] != smallVal2)
                {
                    Console.WriteLine("B[" + i2 + "] = " + B[i2] + ", smallVal2 = " + smallVal2);
                    returnVal = Fail;
                }
                if (C1[i] != largeVal1)
                {
                    Console.WriteLine("C1[" + i + "] = " + C1[i] + ", largeVal1 = " + largeVal1);
                    returnVal = Fail;
                }
                if (C2[i] != largeVal2)
                {
                    Console.WriteLine("C2[" + i + "] = " + C2[i] + ", largeVal2 = " + largeVal2);
                    returnVal = Fail;
                }
            }
            return returnVal;
        }
    }

    static int Main()
    {
        int returnVal = Pass;

        for (int i = 0; i < 10; i++)
        {
            Vector<Single> singleVector = getRandomVector<Single>(singles, i);
            if (VectorConvertTest.VectorConvertSingleInt(singleVector) != Pass)
            {
                Console.WriteLine("Testing Converts Between Single and Int32 failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<Single> singleVector = getRandomVector<Single>(singles, i);
            if (VectorConvertTest.VectorConvertSingleUInt(singleVector) != Pass)
            {
                Console.WriteLine("Testing Converts Between Single and UInt32 failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<Double> doubleVector = getRandomVector<Double>(doubles, i);
            if (VectorConvertTest.VectorConvertDoubleInt64(doubleVector) != Pass)
            {
                Console.WriteLine("Testing Converts between Double and Int64 failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<Double> doubleVector = getRandomVector<Double>(doubles, i);
            if (VectorConvertTest.VectorConvertDoubleUInt64(doubleVector) != Pass)
            {
                Console.WriteLine("Testing Converts between Double and UInt64 failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<Double> doubleVector1 = getRandomVector<Double>(doubles, i);
            Vector<Double> doubleVector2 = getRandomVector<Double>(doubles, i);
            if (VectorConvertTest.VectorConvertDoubleSingle(doubleVector1, doubleVector2) != Pass)
            {
                Console.WriteLine("Testing Converts between Single and Double failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<Int64> int64Vector1 = getRandomVector<Int64>(int64s, i);
            Vector<Int64> int64Vector2 = getRandomVector<Int64>(int64s, i);
            if (VectorConvertTest.VectorConvertInt64And32(int64Vector1, int64Vector2) != Pass)
            {
                Console.WriteLine("Testing Converts between Int64 and Int32 failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<Int32> int32Vector1 = getRandomVector<Int32>(int32s, i);
            Vector<Int32> int32Vector2 = getRandomVector<Int32>(int32s, i);
            if (VectorConvertTest.VectorConvertInt32And16(int32Vector1, int32Vector2) != Pass)
            {
                Console.WriteLine("Testing Converts between Int32 and Int16 failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<Int16> int16Vector1 = getRandomVector<Int16>(int16s, i);
            Vector<Int16> int16Vector2 = getRandomVector<Int16>(int16s, i);
            if (VectorConvertTest.VectorConvertInt16And8(int16Vector1, int16Vector2) != Pass)
            {
                Console.WriteLine("Testing Converts between Int16 and SByte failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<UInt64> uint64Vector1 = getRandomVector<UInt64>(uint64s, i);
            Vector<UInt64> uint64Vector2 = getRandomVector<UInt64>(uint64s, i);
            if (VectorConvertTest.VectorConvertUInt64And32(uint64Vector1, uint64Vector2) != Pass)
            {
                Console.WriteLine("Testing Converts between UInt64 and UInt32 failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<UInt32> uint32Vector1 = getRandomVector<UInt32>(uint32s, i);
            Vector<UInt32> uint32Vector2 = getRandomVector<UInt32>(uint32s, i);
            if (VectorConvertTest.VectorConvertUInt32And16(uint32Vector1, uint32Vector2) != Pass)
            {
                Console.WriteLine("Testing Converts between UInt32 and UInt16 failed");
                returnVal = Fail;
            }
        }
        
        for (int i = 0; i < 10; i++)
        {
            Vector<UInt16> uint16Vector1 = getRandomVector<UInt16>(uint16s, i);
            Vector<UInt16> uint16Vector2 = getRandomVector<UInt16>(uint16s, i);
            if (VectorConvertTest.VectorConvertUInt16And8(uint16Vector1, uint16Vector2) != Pass)
            {
                Console.WriteLine("Testing Converts between UInt16 and Byte failed");
                returnVal = Fail;
            }
        }

        JitLog jitLog = new JitLog();       
        // SIMD conversions from floating point to unsigned are not supported on x86 or x64
   
        if (!jitLog.Check("System.Numerics.Vector:ConvertToInt32(struct):struct")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector:ConvertToSingle(struct):struct")) returnVal = Fail;
        // SIMD Conversion to Int64 is not supported on x86
#if !TARGET_32BIT
        if (!jitLog.Check("System.Numerics.Vector:ConvertToInt64(struct):struct")) returnVal = Fail;
#endif // !TARGET_32BIT
        if (!jitLog.Check("System.Numerics.Vector:ConvertToDouble(struct):struct")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector:Narrow(struct,struct):struct")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector:Widen(struct,byref,byref)")) returnVal = Fail;
        jitLog.Dispose();

        return returnVal;
    }
}

