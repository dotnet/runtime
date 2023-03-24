// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is conformance test for conv described in ECMA-335 Table III.8: Conversion Operations.
// It tests int32/int64/float/double as the source and sbyte/byte/short/ushort/int/uint/long/ulong
// as the dst.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace TestCasts
{
    public class Program
    {
        static int failedCount;

        const bool ExpectException = true;
        const bool DontExpectException = false;

        const bool UnspecifiedBehaviour = true;

        static void GenerateTest<F, T>(F from, OpCode fromOpcode, OpCode convOpcode, bool exceptionExpected, T expectedTo, bool undefined = false) where F : struct where T : struct, IEquatable<T>
        {
            bool checkResult = !exceptionExpected && !undefined;
            Debug.Assert(!exceptionExpected || !checkResult);
            Debug.Assert(checkResult || expectedTo.Equals(default(T)));

            Type[] args = Array.Empty<Type>(); // No args.
            Type returnType = typeof(T);
            string name = "DynamicConvertFrom" + typeof(F).FullName + "To" + typeof(T).FullName + from.ToString() + "Op" + convOpcode.Name;
            DynamicMethod dm = new DynamicMethod(name, returnType, args);

            ILGenerator generator = dm.GetILGenerator();

            if (typeof(F) == typeof(int)) generator.Emit(fromOpcode, (int)(object)from);
            else if (typeof(F) == typeof(long)) generator.Emit(fromOpcode, (long)(object)from);
            else if (typeof(F) == typeof(nint)) generator.Emit(fromOpcode, (nint)(object)from);
            else if (typeof(F) == typeof(float)) generator.Emit(fromOpcode, (float)(object)from);
            else if (typeof(F) == typeof(double)) generator.Emit(fromOpcode, (double)(object)from);
            else
            {
                throw new NotSupportedException();
            }

            generator.Emit(convOpcode);
            generator.Emit(OpCodes.Ret);

            try
            {
                T res = (T)dm.Invoke(null, BindingFlags.Default, null, Array.Empty<object>(), null);
                if (exceptionExpected)
                {
                    failedCount++;
                    Console.WriteLine("No exception in " + name);
                }
                if (checkResult && !expectedTo.Equals(res))
                {
                    failedCount++;
                    Console.WriteLine("Wrong result in " + name);
                }
            }
            catch
            {
                if (!exceptionExpected)
                {
                    failedCount++;
                    Console.WriteLine("Not expected exception in " + name);
                }
            }
        }

        static void TestConvertFromInt4()
        {
            TestConvertFromInt4ToI1();
            TestConvertFromInt4ToU1();
            TestConvertFromInt4ToI2();
            TestConvertFromInt4ToU2();
            TestConvertFromInt4ToI4();
            TestConvertFromInt4ToU4();
            TestConvertFromInt4ToI8();
            TestConvertFromInt4ToU8();
        }

        static void TestConvertFromInt4ToI1()
        {
            OpCode sourceOp = OpCodes.Ldc_I4;

            OpCode convNoOvf = OpCodes.Conv_I1;
            GenerateTest<int, sbyte>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<int, sbyte>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<int, sbyte>(sbyte.MaxValue, sourceOp, convNoOvf, DontExpectException, sbyte.MaxValue);
            GenerateTest<int, sbyte>(sbyte.MinValue, sourceOp, convNoOvf, DontExpectException, sbyte.MinValue);
            GenerateTest<int, sbyte>(byte.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<int, sbyte>(byte.MinValue, sourceOp, convNoOvf, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<int, sbyte>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<int, sbyte>(int.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_I1;
            GenerateTest<int, sbyte>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<int, sbyte>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<int, sbyte>(sbyte.MaxValue, sourceOp, convOvf, DontExpectException, sbyte.MaxValue);
            GenerateTest<int, sbyte>(sbyte.MinValue, sourceOp, convOvf, DontExpectException, sbyte.MinValue);
            GenerateTest<int, sbyte>(byte.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, sbyte>(byte.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<int, sbyte>(int.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, sbyte>(int.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I1_Un;
            GenerateTest<int, sbyte>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<int, sbyte>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, sbyte>(sbyte.MaxValue, sourceOp, convOvfUn, DontExpectException, sbyte.MaxValue);
            GenerateTest<int, sbyte>(sbyte.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, sbyte>(byte.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, sbyte>(byte.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<int, sbyte>(int.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, sbyte>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt4ToU1()
        {
            OpCode sourceOp = OpCodes.Ldc_I4;

            OpCode convNoOvf = OpCodes.Conv_U1;
            GenerateTest<int, byte>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<int, byte>(-1, sourceOp, convNoOvf, DontExpectException, byte.MaxValue);
            GenerateTest<int, byte>(sbyte.MaxValue, sourceOp, convNoOvf, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<int, byte>(sbyte.MinValue, sourceOp, convNoOvf, DontExpectException, (byte)sbyte.MaxValue + 1);
            GenerateTest<int, byte>(byte.MaxValue, sourceOp, convNoOvf, DontExpectException, byte.MaxValue);
            GenerateTest<int, byte>(byte.MinValue, sourceOp, convNoOvf, DontExpectException, byte.MinValue);
            GenerateTest<int, byte>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, byte.MaxValue);
            GenerateTest<int, byte>(int.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_U1;
            GenerateTest<int, byte>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<int, byte>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, byte>(sbyte.MaxValue, sourceOp, convOvf, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<int, byte>(sbyte.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, byte>(byte.MaxValue, sourceOp, convOvf, DontExpectException, byte.MaxValue);
            GenerateTest<int, byte>(byte.MinValue, sourceOp, convOvf, DontExpectException, byte.MinValue);
            GenerateTest<int, byte>(int.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, byte>(int.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U1_Un;
            GenerateTest<int, byte>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<int, byte>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, byte>(sbyte.MaxValue, sourceOp, convOvfUn, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<int, byte>(sbyte.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, byte>(byte.MaxValue, sourceOp, convOvfUn, DontExpectException, byte.MaxValue);
            GenerateTest<int, byte>(byte.MinValue, sourceOp, convOvfUn, DontExpectException, byte.MinValue);
            GenerateTest<int, byte>(int.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, byte>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt4ToI2()
        {
            OpCode sourceOp = OpCodes.Ldc_I4;

            OpCode convNoOvf = OpCodes.Conv_I2;
            GenerateTest<int, short>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<int, short>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<int, short>(short.MaxValue, sourceOp, convNoOvf, DontExpectException, short.MaxValue);
            GenerateTest<int, short>(short.MinValue, sourceOp, convNoOvf, DontExpectException, short.MinValue);
            GenerateTest<int, short>(ushort.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<int, short>(ushort.MinValue, sourceOp, convNoOvf, DontExpectException, 0);
            GenerateTest<int, short>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<int, short>(int.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_I2;
            GenerateTest<int, short>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<int, short>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<int, short>(short.MaxValue, sourceOp, convOvf, DontExpectException, short.MaxValue);
            GenerateTest<int, short>(short.MinValue, sourceOp, convOvf, DontExpectException, short.MinValue);
            GenerateTest<int, short>(ushort.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, short>(ushort.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<int, short>(int.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, short>(int.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I2_Un;
            GenerateTest<int, short>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<int, short>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, short>(short.MaxValue, sourceOp, convOvfUn, DontExpectException, short.MaxValue);
            GenerateTest<int, short>(short.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, short>(ushort.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, short>(ushort.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<int, short>(int.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, short>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt4ToU2()
        {
            OpCode sourceOp = OpCodes.Ldc_I4;

            OpCode convNoOvf = OpCodes.Conv_U2;
            GenerateTest<int, ushort>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<int, ushort>(-1, sourceOp, convNoOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<int, ushort>(short.MaxValue, sourceOp, convNoOvf, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<int, ushort>(short.MinValue, sourceOp, convNoOvf, DontExpectException, (short)short.MaxValue + 1);
            GenerateTest<int, ushort>(ushort.MaxValue, sourceOp, convNoOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<int, ushort>(ushort.MinValue, sourceOp, convNoOvf, DontExpectException, ushort.MinValue);
            GenerateTest<int, ushort>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<int, ushort>(int.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_U2;
            GenerateTest<int, ushort>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<int, ushort>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, ushort>(short.MaxValue, sourceOp, convOvf, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<int, ushort>(short.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, ushort>(ushort.MaxValue, sourceOp, convOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<int, ushort>(ushort.MinValue, sourceOp, convOvf, DontExpectException, ushort.MinValue);
            GenerateTest<int, ushort>(int.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, ushort>(int.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U2_Un;
            GenerateTest<int, ushort>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<int, ushort>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, ushort>(short.MaxValue, sourceOp, convOvfUn, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<int, ushort>(short.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, ushort>(ushort.MaxValue, sourceOp, convOvfUn, DontExpectException, ushort.MaxValue);
            GenerateTest<int, ushort>(ushort.MinValue, sourceOp, convOvfUn, DontExpectException, ushort.MinValue);
            GenerateTest<int, ushort>(int.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, ushort>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt4ToI4()
        {
            OpCode sourceOp = OpCodes.Ldc_I4;

            OpCode convNoOvf = OpCodes.Conv_I4;
            GenerateTest<int, int>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<int, int>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<int, int>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, int.MaxValue);
            GenerateTest<int, int>(int.MinValue, sourceOp, convNoOvf, DontExpectException, int.MinValue);

            OpCode convOvf = OpCodes.Conv_Ovf_I4;
            GenerateTest<int, int>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<int, int>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<int, int>(int.MaxValue, sourceOp, convOvf, DontExpectException, int.MaxValue);
            GenerateTest<int, int>(int.MinValue, sourceOp, convOvf, DontExpectException, int.MinValue);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I4_Un;
            GenerateTest<int, int>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<int, int>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<int, int>(int.MaxValue, sourceOp, convOvfUn, DontExpectException, int.MaxValue);
            GenerateTest<int, int>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt4ToU4()
        {
            OpCode sourceOp = OpCodes.Ldc_I4;

            OpCode convNoOvf = OpCodes.Conv_U4;
            GenerateTest<int, uint>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<int, uint>(-1, sourceOp, convNoOvf, DontExpectException, uint.MaxValue);
            GenerateTest<int, uint>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, int.MaxValue);
            GenerateTest<int, uint>(int.MinValue, sourceOp, convNoOvf, DontExpectException, (uint)int.MaxValue + 1);

            OpCode convOvf = OpCodes.Conv_Ovf_U4;
            GenerateTest<int, uint>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<int, uint>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, uint>(int.MaxValue, sourceOp, convOvf, DontExpectException, int.MaxValue);
            GenerateTest<int, uint>(int.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U4_Un;
            GenerateTest<int, uint>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<int, uint>(-1, sourceOp, convOvfUn, DontExpectException, uint.MaxValue);
            GenerateTest<int, uint>(int.MaxValue, sourceOp, convOvfUn, DontExpectException, int.MaxValue);
            GenerateTest<int, uint>(int.MinValue, sourceOp, convOvfUn, DontExpectException, (uint)int.MaxValue + 1);
        }

        static void TestConvertFromInt4ToI8()
        {
            OpCode sourceOp = OpCodes.Ldc_I4;

            OpCode convNoOvf = OpCodes.Conv_I8;
            GenerateTest<int, long>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<int, long>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<int, long>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, int.MaxValue);
            GenerateTest<int, long>(int.MinValue, sourceOp, convNoOvf, DontExpectException, int.MinValue);

            OpCode convOvf = OpCodes.Conv_Ovf_I8;
            GenerateTest<int, long>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<int, long>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<int, long>(int.MaxValue, sourceOp, convOvf, DontExpectException, int.MaxValue);
            GenerateTest<int, long>(int.MinValue, sourceOp, convOvf, DontExpectException, int.MinValue);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I8_Un;
            GenerateTest<int, long>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<int, long>(-1, sourceOp, convOvfUn, DontExpectException, uint.MaxValue);
            GenerateTest<int, long>(int.MaxValue, sourceOp, convOvfUn, DontExpectException, int.MaxValue);
            GenerateTest<int, long>(int.MinValue, sourceOp, convOvfUn, DontExpectException, (long)int.MaxValue + 1);
        }

        static void TestConvertFromInt4ToU8()
        {
            OpCode sourceOp = OpCodes.Ldc_I4;

            OpCode convNoOvf = OpCodes.Conv_U8;
            GenerateTest<int, ulong>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<int, ulong>(-1, sourceOp, convNoOvf, DontExpectException, uint.MaxValue);
            GenerateTest<int, ulong>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, int.MaxValue);
            GenerateTest<int, ulong>(int.MinValue, sourceOp, convNoOvf, DontExpectException, (ulong)int.MaxValue + 1);

            OpCode convOvf = OpCodes.Conv_Ovf_U8;
            GenerateTest<int, ulong>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<int, ulong>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<int, ulong>(int.MaxValue, sourceOp, convOvf, DontExpectException, int.MaxValue);
            GenerateTest<int, ulong>(int.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U8_Un;
            GenerateTest<int, ulong>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<int, ulong>(-1, sourceOp, convOvfUn, DontExpectException, uint.MaxValue);
            GenerateTest<int, ulong>(int.MaxValue, sourceOp, convOvfUn, DontExpectException, int.MaxValue);
            GenerateTest<int, ulong>(int.MinValue, sourceOp, convOvfUn, DontExpectException, (ulong)int.MaxValue + 1);
        }

        static void TestConvertFromInt8()
        {
            TestConvertFromInt8ToI1();
            TestConvertFromInt8ToU1();
            TestConvertFromInt8ToI2();
            TestConvertFromInt8ToU2();
            TestConvertFromInt8ToI4();
            TestConvertFromInt8ToU4();
            TestConvertFromInt8ToI8();
            TestConvertFromInt8ToU8();
        }

        static void TestConvertFromInt8ToI1()
        {
            OpCode sourceOp = OpCodes.Ldc_I8;

            OpCode convNoOvf = OpCodes.Conv_I1;
            GenerateTest<long, sbyte>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<long, sbyte>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, sbyte>(sbyte.MaxValue, sourceOp, convNoOvf, DontExpectException, sbyte.MaxValue);
            GenerateTest<long, sbyte>(sbyte.MinValue, sourceOp, convNoOvf, DontExpectException, sbyte.MinValue);
            GenerateTest<long, sbyte>(byte.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, sbyte>(byte.MinValue, sourceOp, convNoOvf, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<long, sbyte>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, sbyte>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_I1;
            GenerateTest<long, sbyte>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<long, sbyte>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<long, sbyte>(sbyte.MaxValue, sourceOp, convOvf, DontExpectException, sbyte.MaxValue);
            GenerateTest<long, sbyte>(sbyte.MinValue, sourceOp, convOvf, DontExpectException, sbyte.MinValue);
            GenerateTest<long, sbyte>(byte.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, sbyte>(byte.MinValue, sourceOp, convOvf, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<long, sbyte>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, sbyte>(long.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I1_Un;
            GenerateTest<long, sbyte>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<long, sbyte>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, sbyte>(sbyte.MaxValue, sourceOp, convOvfUn, DontExpectException, sbyte.MaxValue);
            GenerateTest<long, sbyte>(sbyte.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, sbyte>(byte.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, sbyte>(byte.MinValue, sourceOp, convOvfUn, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<long, sbyte>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, sbyte>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt8ToU1()
        {
            OpCode sourceOp = OpCodes.Ldc_I8;

            OpCode convNoOvf = OpCodes.Conv_U1;
            GenerateTest<long, byte>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<long, byte>(-1, sourceOp, convNoOvf, DontExpectException, byte.MaxValue);
            GenerateTest<long, byte>(sbyte.MaxValue, sourceOp, convNoOvf, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<long, byte>(sbyte.MinValue, sourceOp, convNoOvf, DontExpectException, (byte)sbyte.MaxValue + 1);
            GenerateTest<long, byte>(byte.MaxValue, sourceOp, convNoOvf, DontExpectException, byte.MaxValue);
            GenerateTest<long, byte>(byte.MinValue, sourceOp, convNoOvf, DontExpectException, byte.MinValue);
            GenerateTest<long, byte>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, byte.MaxValue);
            GenerateTest<long, byte>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_U1;
            GenerateTest<long, byte>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<long, byte>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, byte>(sbyte.MaxValue, sourceOp, convOvf, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<long, byte>(sbyte.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, byte>(byte.MaxValue, sourceOp, convOvf, DontExpectException, byte.MaxValue);
            GenerateTest<long, byte>(byte.MinValue, sourceOp, convOvf, DontExpectException, byte.MinValue);
            GenerateTest<long, byte>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, byte>(long.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U1_Un;
            GenerateTest<long, byte>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<long, byte>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, byte>(sbyte.MaxValue, sourceOp, convOvfUn, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<long, byte>(sbyte.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, byte>(byte.MaxValue, sourceOp, convOvfUn, DontExpectException, byte.MaxValue);
            GenerateTest<long, byte>(byte.MinValue, sourceOp, convOvfUn, DontExpectException, byte.MinValue);
            GenerateTest<long, byte>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, byte>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt8ToI2()
        {
            OpCode sourceOp = OpCodes.Ldc_I8;

            OpCode convNoOvf = OpCodes.Conv_I2;
            GenerateTest<long, short>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<long, short>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, short>(short.MaxValue, sourceOp, convNoOvf, DontExpectException, short.MaxValue);
            GenerateTest<long, short>(short.MinValue, sourceOp, convNoOvf, DontExpectException, short.MinValue);
            GenerateTest<long, short>(ushort.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, short>(ushort.MinValue, sourceOp, convNoOvf, DontExpectException, byte.MinValue);
            GenerateTest<long, short>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, short>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_I2;
            GenerateTest<long, short>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<long, short>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<long, short>(short.MaxValue, sourceOp, convOvf, DontExpectException, short.MaxValue);
            GenerateTest<long, short>(short.MinValue, sourceOp, convOvf, DontExpectException, short.MinValue);
            GenerateTest<long, short>(ushort.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, short>(ushort.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<long, short>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, short>(long.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I2_Un;
            GenerateTest<long, short>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<long, short>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, short>(short.MaxValue, sourceOp, convOvfUn, DontExpectException, short.MaxValue);
            GenerateTest<long, short>(short.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, short>(ushort.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, short>(ushort.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<long, short>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, short>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt8ToU2()
        {
            OpCode sourceOp = OpCodes.Ldc_I8;

            OpCode convNoOvf = OpCodes.Conv_U2;
            GenerateTest<long, ushort>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<long, ushort>(-1, sourceOp, convNoOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<long, ushort>(short.MaxValue, sourceOp, convNoOvf, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<long, ushort>(short.MinValue, sourceOp, convNoOvf, DontExpectException, (ushort)short.MaxValue + 1);
            GenerateTest<long, ushort>(ushort.MaxValue, sourceOp, convNoOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<long, ushort>(ushort.MinValue, sourceOp, convNoOvf, DontExpectException, ushort.MinValue);
            GenerateTest<long, ushort>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<long, ushort>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_U2;
            GenerateTest<long, ushort>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<long, ushort>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, ushort>(short.MaxValue, sourceOp, convOvf, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<long, ushort>(short.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, ushort>(ushort.MaxValue, sourceOp, convOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<long, ushort>(ushort.MinValue, sourceOp, convOvf, DontExpectException, ushort.MinValue);
            GenerateTest<long, ushort>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, ushort>(long.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U2_Un;
            GenerateTest<long, ushort>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<long, ushort>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, ushort>(short.MaxValue, sourceOp, convOvfUn, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<long, ushort>(short.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, ushort>(ushort.MaxValue, sourceOp, convOvfUn, DontExpectException, ushort.MaxValue);
            GenerateTest<long, ushort>(ushort.MinValue, sourceOp, convOvfUn, DontExpectException, ushort.MinValue);
            GenerateTest<long, ushort>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, ushort>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt8ToI4()
        {
            OpCode sourceOp = OpCodes.Ldc_I8;

            OpCode convNoOvf = OpCodes.Conv_I4;
            GenerateTest<long, int>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<long, int>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, int>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, int.MaxValue);
            GenerateTest<long, int>(int.MinValue, sourceOp, convNoOvf, DontExpectException, int.MinValue);
            GenerateTest<long, int>(uint.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, int>(uint.MinValue, sourceOp, convNoOvf, DontExpectException, 0);
            GenerateTest<long, int>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, int>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_I4;
            GenerateTest<long, int>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<long, int>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<long, int>(int.MaxValue, sourceOp, convOvf, DontExpectException, int.MaxValue);
            GenerateTest<long, int>(int.MinValue, sourceOp, convOvf, DontExpectException, int.MinValue);
            GenerateTest<long, int>(uint.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, int>(uint.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<long, int>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, int>(long.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I4_Un;
            GenerateTest<long, int>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<long, int>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, int>(int.MaxValue, sourceOp, convOvfUn, DontExpectException, int.MaxValue);
            GenerateTest<long, int>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, int>(uint.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, int>(uint.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<long, int>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, int>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt8ToU4()
        {
            OpCode sourceOp = OpCodes.Ldc_I8;

            OpCode convNoOvf = OpCodes.Conv_U4;
            GenerateTest<long, uint>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<long, uint>(-1, sourceOp, convNoOvf, DontExpectException, uint.MaxValue);
            GenerateTest<long, uint>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, int.MaxValue);
            GenerateTest<long, uint>(int.MinValue, sourceOp, convNoOvf, DontExpectException, (uint)int.MaxValue + 1);
            GenerateTest<long, uint>(uint.MaxValue, sourceOp, convNoOvf, DontExpectException, uint.MaxValue);
            GenerateTest<long, uint>(uint.MinValue, sourceOp, convNoOvf, DontExpectException, uint.MinValue);
            GenerateTest<long, uint>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, uint.MaxValue);
            GenerateTest<long, uint>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0);

            OpCode convOvf = OpCodes.Conv_Ovf_U4;
            GenerateTest<long, uint>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<long, uint>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, uint>(int.MaxValue, sourceOp, convOvf, DontExpectException, int.MaxValue);
            GenerateTest<long, uint>(int.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, uint>(uint.MaxValue, sourceOp, convOvf, DontExpectException, uint.MaxValue);
            GenerateTest<long, uint>(uint.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<long, uint>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, uint>(long.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U4_Un;
            GenerateTest<long, uint>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<long, uint>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, uint>(int.MaxValue, sourceOp, convOvfUn, DontExpectException, int.MaxValue);
            GenerateTest<long, uint>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, uint>(uint.MaxValue, sourceOp, convOvfUn, DontExpectException, uint.MaxValue);
            GenerateTest<long, uint>(uint.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<long, uint>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, uint>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt8ToI8()
        {
            OpCode sourceOp = OpCodes.Ldc_I8;

            OpCode convNoOvf = OpCodes.Conv_I8;
            GenerateTest<long, long>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<long, long>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<long, long>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, int.MaxValue);
            GenerateTest<long, long>(int.MinValue, sourceOp, convNoOvf, DontExpectException, int.MinValue);
            GenerateTest<long, long>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, long.MaxValue);
            GenerateTest<long, long>(long.MinValue, sourceOp, convNoOvf, DontExpectException, long.MinValue);

            OpCode convOvf = OpCodes.Conv_Ovf_I8;
            GenerateTest<long, long>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<long, long>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<long, long>(int.MaxValue, sourceOp, convOvf, DontExpectException, int.MaxValue);
            GenerateTest<long, long>(int.MinValue, sourceOp, convOvf, DontExpectException, int.MinValue);
            GenerateTest<long, long>(long.MaxValue, sourceOp, convOvf, DontExpectException, long.MaxValue);
            GenerateTest<long, long>(long.MinValue, sourceOp, convOvf, DontExpectException, long.MinValue);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I8_Un;
            GenerateTest<long, long>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<long, long>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, long>(int.MaxValue, sourceOp, convOvfUn, DontExpectException, int.MaxValue);
            GenerateTest<long, long>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<long, long>(long.MaxValue, sourceOp, convOvfUn, DontExpectException, long.MaxValue);
            GenerateTest<long, long>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromInt8ToU8()
        {
            OpCode sourceOp = OpCodes.Ldc_I8;

            OpCode convNoOvf = OpCodes.Conv_U8;
            GenerateTest<long, ulong>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<long, ulong>(-1, sourceOp, convNoOvf, DontExpectException, ulong.MaxValue);
            GenerateTest<long, ulong>(int.MaxValue, sourceOp, convNoOvf, DontExpectException, int.MaxValue);
            GenerateTest<long, ulong>(int.MinValue, sourceOp, convNoOvf, DontExpectException, 0xffffffff80000000UL);
            GenerateTest<long, ulong>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, long.MaxValue);
            GenerateTest<long, ulong>(long.MinValue, sourceOp, convNoOvf, DontExpectException, (ulong)long.MaxValue + 1);

            OpCode convOvf = OpCodes.Conv_Ovf_U8;
            GenerateTest<long, ulong>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<long, ulong>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, ulong>(int.MaxValue, sourceOp, convOvf, DontExpectException, int.MaxValue);
            GenerateTest<long, ulong>(int.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<long, ulong>(long.MaxValue, sourceOp, convOvf, DontExpectException, long.MaxValue);
            GenerateTest<long, ulong>(long.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U8_Un;
            GenerateTest<long, ulong>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<long, ulong>(-1, sourceOp, convOvfUn, DontExpectException, ulong.MaxValue);
            GenerateTest<long, ulong>(int.MaxValue, sourceOp, convOvfUn, DontExpectException, int.MaxValue);
            GenerateTest<long, ulong>(int.MinValue, sourceOp, convOvfUn, DontExpectException, ulong.MaxValue - int.MaxValue);
            GenerateTest<long, ulong>(long.MaxValue, sourceOp, convOvfUn, DontExpectException, long.MaxValue);
            GenerateTest<long, ulong>(long.MinValue, sourceOp, convOvfUn, DontExpectException, (ulong)long.MaxValue + 1);
        }


        static void TestConvertFromFloat()
        {
            TestConvertFromFloatToI1();
            TestConvertFromFloatToU1();
            TestConvertFromFloatToI2();
            TestConvertFromFloatToU2();
            TestConvertFromFloatToI4();
            TestConvertFromFloatToU4();
            TestConvertFromFloatToI8();
            TestConvertFromFloatToU8();
        }

        static void TestConvertFromFloatToI1()
        {
            OpCode sourceOp = OpCodes.Ldc_R4;

            OpCode convNoOvf = OpCodes.Conv_I1;
            GenerateTest<float, sbyte>(1F, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<float, sbyte>(-1F, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<float, sbyte>(1.1F, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<float, sbyte>(-1.1F, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<float, sbyte>(sbyte.MaxValue, sourceOp, convNoOvf, DontExpectException, sbyte.MaxValue);
            GenerateTest<float, sbyte>(sbyte.MinValue, sourceOp, convNoOvf, DontExpectException, sbyte.MinValue);
            GenerateTest<float, sbyte>(byte.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, sbyte>(byte.MinValue, sourceOp, convNoOvf, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<float, sbyte>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, sbyte>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_I1;
            GenerateTest<float, sbyte>(1F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, sbyte>(-1F, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<float, sbyte>(1.1F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, sbyte>(-1.1F, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<float, sbyte>(sbyte.MaxValue, sourceOp, convOvf, DontExpectException, sbyte.MaxValue);
            GenerateTest<float, sbyte>(sbyte.MinValue, sourceOp, convOvf, DontExpectException, sbyte.MinValue);
            GenerateTest<float, sbyte>(byte.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, sbyte>(byte.MinValue, sourceOp, convOvf, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<float, sbyte>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, sbyte>(long.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I1_Un;
            GenerateTest<float, sbyte>(1F, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, sbyte>(-1F, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<float, sbyte>(1.1F, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, sbyte>(-1.1F, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<float, sbyte>(sbyte.MaxValue, sourceOp, convOvfUn, DontExpectException, sbyte.MaxValue);
            GenerateTest<float, sbyte>(sbyte.MinValue, sourceOp, convOvfUn, DontExpectException, sbyte.MinValue);
            GenerateTest<float, sbyte>(byte.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, sbyte>(byte.MinValue, sourceOp, convOvfUn, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<float, sbyte>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, sbyte>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromFloatToU1()
        {
            OpCode sourceOp = OpCodes.Ldc_R4;

            OpCode convNoOvf = OpCodes.Conv_U1;
            GenerateTest<float, byte>(1F, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<float, byte>(-1F, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, byte>(sbyte.MaxValue, sourceOp, convNoOvf, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<float, byte>(sbyte.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, byte>(byte.MaxValue, sourceOp, convNoOvf, DontExpectException, byte.MaxValue);
            GenerateTest<float, byte>(byte.MinValue, sourceOp, convNoOvf, DontExpectException, byte.MinValue);
            GenerateTest<float, byte>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, byte>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_U1;
            GenerateTest<float, byte>(1F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, byte>(1.9F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, byte>(-1F, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, byte>(sbyte.MaxValue, sourceOp, convOvf, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<float, byte>(sbyte.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, byte>(byte.MaxValue, sourceOp, convOvf, DontExpectException, byte.MaxValue);
            GenerateTest<float, byte>(byte.MinValue, sourceOp, convOvf, DontExpectException, byte.MinValue);
            GenerateTest<float, byte>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, byte>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, byte>(Single.NaN, sourceOp, convOvf, ExpectException, 0);


            OpCode convOvfUn = OpCodes.Conv_Ovf_U1_Un;
            GenerateTest<float, byte>(1F, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, byte>(2.2F, sourceOp, convOvfUn, DontExpectException, 2);
            GenerateTest<float, byte>(-1F, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, byte>(sbyte.MaxValue, sourceOp, convOvfUn, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<float, byte>(sbyte.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, byte>(byte.MaxValue, sourceOp, convOvfUn, DontExpectException, byte.MaxValue);
            GenerateTest<float, byte>(byte.MinValue, sourceOp, convOvfUn, DontExpectException, byte.MinValue);
            GenerateTest<float, byte>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, byte>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, byte>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromFloatToI2()
        {
            OpCode sourceOp = OpCodes.Ldc_R4;

            OpCode convNoOvf = OpCodes.Conv_I2;
            GenerateTest<float, short>(1F, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<float, short>(-1F, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<float, short>(short.MaxValue, sourceOp, convNoOvf, DontExpectException, short.MaxValue);
            GenerateTest<float, short>(short.MinValue, sourceOp, convNoOvf, DontExpectException, short.MinValue);
            GenerateTest<float, short>(ushort.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<float, short>(ushort.MinValue, sourceOp, convNoOvf, DontExpectException, byte.MinValue);
            GenerateTest<float, short>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, short>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_I2;
            GenerateTest<float, short>(1F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, short>(1.2F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, short>(-1F, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<float, short>(-1.8F, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<float, short>(short.MaxValue, sourceOp, convOvf, DontExpectException, short.MaxValue);
            GenerateTest<float, short>(short.MinValue, sourceOp, convOvf, DontExpectException, short.MinValue);
            GenerateTest<float, short>(ushort.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, short>(ushort.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<float, short>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, short>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, short>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I2_Un;
            GenerateTest<float, short>(1F, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, short>(1.5F, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, short>(-1.5F, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<float, short>(short.MaxValue, sourceOp, convOvfUn, DontExpectException, short.MaxValue);
            GenerateTest<float, short>(short.MinValue, sourceOp, convOvfUn, DontExpectException, short.MinValue);
            GenerateTest<float, short>(ushort.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, short>(ushort.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<float, short>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, short>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, short>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromFloatToU2()
        {
            OpCode sourceOp = OpCodes.Ldc_R4;

            OpCode convNoOvf = OpCodes.Conv_U2;
            GenerateTest<float, ushort>(1F, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<float, ushort>(3.9F, sourceOp, convNoOvf, DontExpectException, 3);
            GenerateTest<float, ushort>(-1F, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            GenerateTest<float, ushort>(short.MaxValue, sourceOp, convNoOvf, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<float, ushort>(short.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, ushort>(ushort.MaxValue, sourceOp, convNoOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<float, ushort>(ushort.MinValue, sourceOp, convNoOvf, DontExpectException, ushort.MinValue);
            GenerateTest<float, ushort>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, ushort>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_U2;
            GenerateTest<float, ushort>(1F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, ushort>(1.3F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, ushort>(-1F, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, ushort>(short.MaxValue, sourceOp, convOvf, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<float, ushort>(short.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, ushort>(ushort.MaxValue, sourceOp, convOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<float, ushort>(ushort.MinValue, sourceOp, convOvf, DontExpectException, ushort.MinValue);
            GenerateTest<float, ushort>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, ushort>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, ushort>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U2_Un;
            GenerateTest<float, ushort>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, ushort>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, ushort>(short.MaxValue, sourceOp, convOvfUn, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<float, ushort>(short.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, ushort>(ushort.MaxValue, sourceOp, convOvfUn, DontExpectException, ushort.MaxValue);
            GenerateTest<float, ushort>(ushort.MinValue, sourceOp, convOvfUn, DontExpectException, ushort.MinValue);
            GenerateTest<float, ushort>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, ushort>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, ushort>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromFloatToI4()
        {
            OpCode sourceOp = OpCodes.Ldc_R4;

            OpCode convNoOvf = OpCodes.Conv_I4;
            GenerateTest<float, int>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<float, int>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<float, int>(int.MinValue, sourceOp, convNoOvf, DontExpectException, int.MinValue);
            GenerateTest<float, int>(uint.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, int>(uint.MinValue, sourceOp, convNoOvf, DontExpectException, 0);
            GenerateTest<float, int>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, int>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I4_Un;
            GenerateTest<float, int>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, int>(-1, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<float, int>(int.MinValue, sourceOp, convOvfUn, DontExpectException, int.MinValue);
            GenerateTest<float, int>(uint.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, int>(uint.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<float, int>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, int>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, int>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromFloatToU4()
        {
            OpCode sourceOp = OpCodes.Ldc_R4;

            OpCode convNoOvf = OpCodes.Conv_U4;
            GenerateTest<float, uint>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<float, uint>(-1, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, uint>(int.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, uint>(uint.MinValue, sourceOp, convNoOvf, DontExpectException, uint.MinValue);
            GenerateTest<float, uint>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, uint>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_U4;
            GenerateTest<float, uint>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, uint>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, uint>(int.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, uint>(uint.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<float, uint>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, uint>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, uint>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U4_Un;
            GenerateTest<float, uint>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, uint>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, uint>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, uint>(uint.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<float, uint>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, uint>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, uint>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromFloatToI8()
        {
            OpCode sourceOp = OpCodes.Ldc_R4;

            OpCode convNoOvf = OpCodes.Conv_I8;
            GenerateTest<float, long>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<float, long>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<float, long>(int.MinValue, sourceOp, convNoOvf, DontExpectException, int.MinValue);
            GenerateTest<float, long>(long.MinValue, sourceOp, convNoOvf, DontExpectException, long.MinValue);

            OpCode convOvf = OpCodes.Conv_Ovf_I8;
            GenerateTest<float, long>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, long>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<float, long>(int.MinValue, sourceOp, convOvf, DontExpectException, int.MinValue);
            GenerateTest<float, long>(long.MinValue, sourceOp, convOvf, DontExpectException, long.MinValue);
            GenerateTest<float, long>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I8_Un;
            GenerateTest<float, long>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, long>(-1, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<float, long>(int.MinValue, sourceOp, convOvfUn, DontExpectException, int.MinValue);
            GenerateTest<float, long>(long.MinValue, sourceOp, convOvfUn, DontExpectException, long.MinValue);
            GenerateTest<float, long>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromFloatToU8()
        {
            OpCode sourceOp = OpCodes.Ldc_R4;

            OpCode convNoOvf = OpCodes.Conv_U8;
            GenerateTest<float, ulong>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<float, ulong>(-1, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, ulong>(int.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<float, ulong>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_U8;
            GenerateTest<float, ulong>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<float, ulong>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, ulong>(int.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, ulong>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<float, ulong>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U8_Un;
            GenerateTest<float, ulong>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<float, ulong>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, ulong>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, ulong>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<float, ulong>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromDouble()
        {
            TestConvertFromDoubleToI1();
            TestConvertFromDoubleToU1();
            TestConvertFromDoubleToI2();
            TestConvertFromDoubleToU2();
            TestConvertFromDoubleToI4();
            TestConvertFromDoubleToU4();
            TestConvertFromDoubleToI8();
            TestConvertFromDoubleToU8();
        }

        static void TestConvertFromDoubleToI1()
        {
            OpCode sourceOp = OpCodes.Ldc_R8;

            OpCode convNoOvf = OpCodes.Conv_I1;
            GenerateTest<double, sbyte>(1F, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<double, sbyte>(-1F, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<double, sbyte>(1.1F, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<double, sbyte>(-1.1F, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<double, sbyte>(sbyte.MaxValue, sourceOp, convNoOvf, DontExpectException, sbyte.MaxValue);
            GenerateTest<double, sbyte>(sbyte.MinValue, sourceOp, convNoOvf, DontExpectException, sbyte.MinValue);
            GenerateTest<double, sbyte>(byte.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, sbyte>(byte.MinValue, sourceOp, convNoOvf, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<double, sbyte>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, sbyte>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_I1;
            GenerateTest<double, sbyte>(1F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<double, sbyte>(-1F, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<double, sbyte>(1.1F, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<double, sbyte>(-1.1F, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<double, sbyte>(sbyte.MaxValue, sourceOp, convOvf, DontExpectException, sbyte.MaxValue);
            GenerateTest<double, sbyte>(sbyte.MinValue, sourceOp, convOvf, DontExpectException, sbyte.MinValue);
            GenerateTest<double, sbyte>(byte.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, sbyte>(byte.MinValue, sourceOp, convOvf, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<double, sbyte>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, sbyte>(long.MinValue, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I1_Un;
            GenerateTest<double, sbyte>(1F, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<double, sbyte>(-1F, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<double, sbyte>(1.1F, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<double, sbyte>(-1.1F, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<double, sbyte>(sbyte.MaxValue, sourceOp, convOvfUn, DontExpectException, sbyte.MaxValue);
            GenerateTest<double, sbyte>(sbyte.MinValue, sourceOp, convOvfUn, DontExpectException, sbyte.MinValue);
            GenerateTest<double, sbyte>(byte.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, sbyte>(byte.MinValue, sourceOp, convOvfUn, DontExpectException, (sbyte)byte.MinValue);
            GenerateTest<double, sbyte>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, sbyte>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromDoubleToU1()
        {
            OpCode sourceOp = OpCodes.Ldc_R8;

            OpCode convNoOvf = OpCodes.Conv_U1;
            GenerateTest<double, byte>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<double, byte>(-1, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, byte>(sbyte.MaxValue, sourceOp, convNoOvf, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<double, byte>(sbyte.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, byte>(byte.MaxValue, sourceOp, convNoOvf, DontExpectException, byte.MaxValue);
            GenerateTest<double, byte>(byte.MinValue, sourceOp, convNoOvf, DontExpectException, byte.MinValue);
            GenerateTest<double, byte>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, byte>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_U1;
            GenerateTest<double, byte>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<double, byte>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, byte>(sbyte.MaxValue, sourceOp, convOvf, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<double, byte>(sbyte.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, byte>(byte.MaxValue, sourceOp, convOvf, DontExpectException, byte.MaxValue);
            GenerateTest<double, byte>(byte.MinValue, sourceOp, convOvf, DontExpectException, byte.MinValue);
            GenerateTest<double, byte>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, byte>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, byte>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U1_Un;
            GenerateTest<double, byte>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<double, byte>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, byte>(sbyte.MaxValue, sourceOp, convOvfUn, DontExpectException, (byte)sbyte.MaxValue);
            GenerateTest<double, byte>(sbyte.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, byte>(byte.MaxValue, sourceOp, convOvfUn, DontExpectException, byte.MaxValue);
            GenerateTest<double, byte>(byte.MinValue, sourceOp, convOvfUn, DontExpectException, byte.MinValue);
            GenerateTest<double, byte>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, byte>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, byte>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromDoubleToI2()
        {
            OpCode sourceOp = OpCodes.Ldc_R8;

            OpCode convNoOvf = OpCodes.Conv_I2;
            GenerateTest<double, short>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<double, short>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<double, short>(short.MaxValue, sourceOp, convNoOvf, DontExpectException, short.MaxValue);
            GenerateTest<double, short>(short.MinValue, sourceOp, convNoOvf, DontExpectException, short.MinValue);
            GenerateTest<double, short>(ushort.MaxValue, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<double, short>(ushort.MinValue, sourceOp, convNoOvf, DontExpectException, byte.MinValue);
            GenerateTest<double, short>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, short>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_I2;
            GenerateTest<double, short>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<double, short>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<double, short>(short.MaxValue, sourceOp, convOvf, DontExpectException, short.MaxValue);
            GenerateTest<double, short>(short.MinValue, sourceOp, convOvf, DontExpectException, short.MinValue);
            GenerateTest<double, short>(ushort.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, short>(ushort.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<double, short>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, short>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, short>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I2_Un;
            GenerateTest<double, short>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<double, short>(-1, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<double, short>(short.MaxValue, sourceOp, convOvfUn, DontExpectException, short.MaxValue);
            GenerateTest<double, short>(short.MinValue, sourceOp, convOvfUn, DontExpectException, short.MinValue);
            GenerateTest<double, short>(ushort.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, short>(ushort.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<double, short>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, short>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, short>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromDoubleToU2()
        {
            OpCode sourceOp = OpCodes.Ldc_R8;

            OpCode convNoOvf = OpCodes.Conv_U2;
            GenerateTest<double, ushort>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<double, ushort>(-1, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, ushort>(short.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, ushort>(short.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, ushort>(ushort.MaxValue, sourceOp, convNoOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<double, ushort>(ushort.MinValue, sourceOp, convNoOvf, DontExpectException, ushort.MinValue);
            GenerateTest<double, ushort>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, ushort>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_U2;
            GenerateTest<double, ushort>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<double, ushort>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, ushort>(short.MaxValue, sourceOp, convOvf, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<double, ushort>(short.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, ushort>(ushort.MaxValue, sourceOp, convOvf, DontExpectException, ushort.MaxValue);
            GenerateTest<double, ushort>(ushort.MinValue, sourceOp, convOvf, DontExpectException, ushort.MinValue);
            GenerateTest<double, ushort>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, ushort>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, ushort>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U2_Un;
            GenerateTest<double, ushort>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<double, ushort>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, ushort>(short.MaxValue, sourceOp, convOvfUn, DontExpectException, (ushort)short.MaxValue);
            GenerateTest<double, ushort>(short.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, ushort>(ushort.MaxValue, sourceOp, convOvfUn, DontExpectException, ushort.MaxValue);
            GenerateTest<double, ushort>(ushort.MinValue, sourceOp, convOvfUn, DontExpectException, ushort.MinValue);
            GenerateTest<double, ushort>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, ushort>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, ushort>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromDoubleToI4()
        {
            OpCode sourceOp = OpCodes.Ldc_R8;

            OpCode convNoOvf = OpCodes.Conv_I4;
            GenerateTest<double, int>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<double, int>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<double, int>(int.MinValue, sourceOp, convNoOvf, DontExpectException, int.MinValue);
            GenerateTest<double, int>(uint.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, int>(uint.MinValue, sourceOp, convNoOvf, DontExpectException, 0);
            GenerateTest<double, int>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, int>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_I4;
            GenerateTest<double, int>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<double, int>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<double, int>(int.MinValue, sourceOp, convOvf, DontExpectException, int.MinValue);
            GenerateTest<double, int>(uint.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, int>(uint.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<double, int>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, int>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, int>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I4_Un;
            GenerateTest<double, int>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<double, int>(-1, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<double, int>(int.MinValue, sourceOp, convOvfUn, DontExpectException, int.MinValue);
            GenerateTest<double, int>(uint.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, int>(uint.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<double, int>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, int>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, int>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromDoubleToU4()
        {
            OpCode sourceOp = OpCodes.Ldc_R8;

            OpCode convNoOvf = OpCodes.Conv_U4;
            GenerateTest<double, uint>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<double, uint>(-1, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, uint>(int.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, uint>(uint.MinValue, sourceOp, convNoOvf, DontExpectException, uint.MinValue);
            GenerateTest<double, uint>(long.MaxValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, uint>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_U4;
            GenerateTest<double, uint>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<double, uint>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, uint>(int.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, uint>(uint.MinValue, sourceOp, convOvf, DontExpectException, 0);
            GenerateTest<double, uint>(long.MaxValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, uint>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, uint>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U4_Un;
            GenerateTest<double, uint>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<double, uint>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, uint>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, uint>(uint.MinValue, sourceOp, convOvfUn, DontExpectException, 0);
            GenerateTest<double, uint>(long.MaxValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, uint>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, uint>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromDoubleToI8()
        {
            OpCode sourceOp = OpCodes.Ldc_R8;

            OpCode convNoOvf = OpCodes.Conv_I8;
            GenerateTest<double, long>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<double, long>(-1, sourceOp, convNoOvf, DontExpectException, -1);
            GenerateTest<double, long>(int.MinValue, sourceOp, convNoOvf, DontExpectException, int.MinValue);
            GenerateTest<double, long>(long.MinValue, sourceOp, convNoOvf, DontExpectException, long.MinValue);

            OpCode convOvf = OpCodes.Conv_Ovf_I8;
            GenerateTest<double, long>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<double, long>(-1, sourceOp, convOvf, DontExpectException, -1);
            GenerateTest<double, long>(int.MinValue, sourceOp, convOvf, DontExpectException, int.MinValue);
            GenerateTest<double, long>(long.MinValue, sourceOp, convOvf, DontExpectException, long.MinValue);
            GenerateTest<double, long>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_I8_Un;
            GenerateTest<double, long>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<double, long>(-1, sourceOp, convOvfUn, DontExpectException, -1);
            GenerateTest<double, long>(int.MinValue, sourceOp, convOvfUn, DontExpectException, int.MinValue);
            GenerateTest<double, long>(long.MinValue, sourceOp, convOvfUn, DontExpectException, long.MinValue);
            GenerateTest<double, long>(-9E+18, sourceOp, convOvfUn, DontExpectException, (long)-9E+18);
            GenerateTest<double, long>(9E+18, sourceOp, convOvfUn, DontExpectException, (long)9E+18);
            GenerateTest<double, long>(18E+18, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, long>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        static void TestConvertFromDoubleToU8()
        {
            OpCode sourceOp = OpCodes.Ldc_R8;

            OpCode convNoOvf = OpCodes.Conv_U8;
            GenerateTest<double, ulong>(1, sourceOp, convNoOvf, DontExpectException, 1);
            GenerateTest<double, ulong>(-1, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, ulong>(int.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);
            GenerateTest<double, ulong>(long.MinValue, sourceOp, convNoOvf, DontExpectException, 0, UnspecifiedBehaviour);

            OpCode convOvf = OpCodes.Conv_Ovf_U8;
            GenerateTest<double, ulong>(1, sourceOp, convOvf, DontExpectException, 1);
            GenerateTest<double, ulong>(-1, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, ulong>(int.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, ulong>(long.MinValue, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, ulong>(-9E+18, sourceOp, convOvf, ExpectException, 0);
            GenerateTest<double, ulong>(9E+18, sourceOp, convOvf, DontExpectException, (ulong)9E+18);
            GenerateTest<double, ulong>(18E+18, sourceOp, convOvf, DontExpectException, (ulong)18E+18);
            GenerateTest<double, ulong>(Single.NaN, sourceOp, convOvf, ExpectException, 0);

            OpCode convOvfUn = OpCodes.Conv_Ovf_U8_Un;
            GenerateTest<double, ulong>(1, sourceOp, convOvfUn, DontExpectException, 1);
            GenerateTest<double, ulong>(-1, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, ulong>(int.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, ulong>(long.MinValue, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, ulong>(-9E+18, sourceOp, convOvfUn, ExpectException, 0);
            GenerateTest<double, ulong>(9E+18, sourceOp, convOvfUn, DontExpectException, (ulong)9E+18);
            GenerateTest<double, ulong>(18E+18, sourceOp, convOvfUn, DontExpectException, (ulong)18E+18);
            GenerateTest<double, ulong>(Single.NaN, sourceOp, convOvfUn, ExpectException, 0);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            TestConvertFromInt4();
            TestConvertFromInt8();
            TestConvertFromFloat();
            TestConvertFromDouble();
            if (failedCount > 0)
            {
                Console.WriteLine("The number of failed tests: " + failedCount);
                return 101;
            }
            else
            {
                Console.WriteLine("All tests passed");
                return 100;
            }

        }
    }
}
