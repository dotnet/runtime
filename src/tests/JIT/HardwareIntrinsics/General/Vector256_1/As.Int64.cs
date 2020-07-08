// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\General\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace JIT.HardwareIntrinsics.General
{
    public static partial class Program
    {
        private static void AsInt64()
        {
            var test = new VectorAs__AsInt64();

            // Validates basic functionality works
            test.RunBasicScenario();

            // Validates basic functionality works using the generic form, rather than the type-specific form of the method
            test.RunGenericScenario();

            // Validates calling via reflection works
            test.RunReflectionScenario();

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class VectorAs__AsInt64
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<Int64>>() / sizeof(Int64);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector256<Int64> value;

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<byte> byteResult = value.AsByte();
            ValidateResult(byteResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<double> doubleResult = value.AsDouble();
            ValidateResult(doubleResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<short> shortResult = value.AsInt16();
            ValidateResult(shortResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<int> intResult = value.AsInt32();
            ValidateResult(intResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<long> longResult = value.AsInt64();
            ValidateResult(longResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<sbyte> sbyteResult = value.AsSByte();
            ValidateResult(sbyteResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<float> floatResult = value.AsSingle();
            ValidateResult(floatResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<ushort> ushortResult = value.AsUInt16();
            ValidateResult(ushortResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<uint> uintResult = value.AsUInt32();
            ValidateResult(uintResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<ulong> ulongResult = value.AsUInt64();
            ValidateResult(ulongResult, value);
        }

        public void RunGenericScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunGenericScenario));
            Vector256<Int64> value;

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<byte> byteResult = value.As<Int64, byte>();
            ValidateResult(byteResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<double> doubleResult = value.As<Int64, double>();
            ValidateResult(doubleResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<short> shortResult = value.As<Int64, short>();
            ValidateResult(shortResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<int> intResult = value.As<Int64, int>();
            ValidateResult(intResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<long> longResult = value.As<Int64, long>();
            ValidateResult(longResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<sbyte> sbyteResult = value.As<Int64, sbyte>();
            ValidateResult(sbyteResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<float> floatResult = value.As<Int64, float>();
            ValidateResult(floatResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<ushort> ushortResult = value.As<Int64, ushort>();
            ValidateResult(ushortResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<uint> uintResult = value.As<Int64, uint>();
            ValidateResult(uintResult, value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            Vector256<ulong> ulongResult = value.As<Int64, ulong>();
            ValidateResult(ulongResult, value);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector256<Int64> value;

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object byteResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsByte))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<byte>)(byteResult), value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object doubleResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsDouble))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<double>)(doubleResult), value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object shortResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsInt16))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<short>)(shortResult), value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object intResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsInt32))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<int>)(intResult), value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object longResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsInt64))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<long>)(longResult), value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object sbyteResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsSByte))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<sbyte>)(sbyteResult), value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object floatResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsSingle))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<float>)(floatResult), value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object ushortResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsUInt16))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<ushort>)(ushortResult), value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object uintResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsUInt32))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<uint>)(uintResult), value);

            value = Vector256.Create((long)TestLibrary.Generator.GetInt64());
            object ulongResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsUInt64))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<ulong>)(ulongResult), value);
        }

        private void ValidateResult<T>(Vector256<T> result, Vector256<Int64> value, [CallerMemberName] string method = "")
            where T : struct
        {
            Int64[] resultElements = new Int64[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref resultElements[0]), result);

            Int64[] valueElements = new Int64[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, typeof(T), method);
        }

        private void ValidateResult(Int64[] resultElements, Int64[] valueElements, Type targetType, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (var i = 0; i < ElementCount; i++)
            {
                if (resultElements[i] != valueElements[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256<Int64>.As{targetType.Name}: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
