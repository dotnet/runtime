// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static void AsDouble()
        {
            var test = new VectorAs__AsDouble();

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

    public sealed unsafe class VectorAs__AsDouble
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<Double>>() / sizeof(Double);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector256<Double> value;

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<byte> byteResult = value.AsByte();
            ValidateResult(byteResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<double> doubleResult = value.AsDouble();
            ValidateResult(doubleResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<short> shortResult = value.AsInt16();
            ValidateResult(shortResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<int> intResult = value.AsInt32();
            ValidateResult(intResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<long> longResult = value.AsInt64();
            ValidateResult(longResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<sbyte> sbyteResult = value.AsSByte();
            ValidateResult(sbyteResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<float> floatResult = value.AsSingle();
            ValidateResult(floatResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<ushort> ushortResult = value.AsUInt16();
            ValidateResult(ushortResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<uint> uintResult = value.AsUInt32();
            ValidateResult(uintResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<ulong> ulongResult = value.AsUInt64();
            ValidateResult(ulongResult, value);
        }

        public void RunGenericScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunGenericScenario));
            Vector256<Double> value;

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<byte> byteResult = value.As<Double, byte>();
            ValidateResult(byteResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<double> doubleResult = value.As<Double, double>();
            ValidateResult(doubleResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<short> shortResult = value.As<Double, short>();
            ValidateResult(shortResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<int> intResult = value.As<Double, int>();
            ValidateResult(intResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<long> longResult = value.As<Double, long>();
            ValidateResult(longResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<sbyte> sbyteResult = value.As<Double, sbyte>();
            ValidateResult(sbyteResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<float> floatResult = value.As<Double, float>();
            ValidateResult(floatResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<ushort> ushortResult = value.As<Double, ushort>();
            ValidateResult(ushortResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<uint> uintResult = value.As<Double, uint>();
            ValidateResult(uintResult, value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            Vector256<ulong> ulongResult = value.As<Double, ulong>();
            ValidateResult(ulongResult, value);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector256<Double> value;

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object byteResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsByte))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<byte>)(byteResult), value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object doubleResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsDouble))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<double>)(doubleResult), value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object shortResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsInt16))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<short>)(shortResult), value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object intResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsInt32))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<int>)(intResult), value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object longResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsInt64))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<long>)(longResult), value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object sbyteResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsSByte))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<sbyte>)(sbyteResult), value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object floatResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsSingle))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<float>)(floatResult), value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object ushortResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsUInt16))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<ushort>)(ushortResult), value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object uintResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsUInt32))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<uint>)(uintResult), value);

            value = Vector256.Create(TestLibrary.Generator.GetDouble());
            object ulongResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsUInt64))
                                    .MakeGenericMethod(typeof(Double))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<ulong>)(ulongResult), value);
        }

        private void ValidateResult<T>(Vector256<T> result, Vector256<Double> value, [CallerMemberName] string method = "")
            where T : struct
        {
            Double[] resultElements = new Double[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref resultElements[0]), result);

            Double[] valueElements = new Double[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, typeof(T), method);
        }

        private void ValidateResult(Double[] resultElements, Double[] valueElements, Type targetType, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector256<Double>.As{targetType.Name}: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
