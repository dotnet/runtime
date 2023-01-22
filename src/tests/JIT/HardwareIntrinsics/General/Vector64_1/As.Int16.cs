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
using Xunit;

namespace JIT.HardwareIntrinsics.General._Vector64_1
{
    public static partial class Program
    {
        [Fact]
        public static void AsInt16()
        {
            var test = new VectorAs__AsInt16();

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

    public sealed unsafe class VectorAs__AsInt16
    {
        private static readonly int LargestVectorSize = 8;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector64<Int16>>() / sizeof(Int16);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector64<Int16> value;

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<byte> byteResult = value.AsByte();
            ValidateResult(byteResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<double> doubleResult = value.AsDouble();
            ValidateResult(doubleResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<short> shortResult = value.AsInt16();
            ValidateResult(shortResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<int> intResult = value.AsInt32();
            ValidateResult(intResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<long> longResult = value.AsInt64();
            ValidateResult(longResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<sbyte> sbyteResult = value.AsSByte();
            ValidateResult(sbyteResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<float> floatResult = value.AsSingle();
            ValidateResult(floatResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<ushort> ushortResult = value.AsUInt16();
            ValidateResult(ushortResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<uint> uintResult = value.AsUInt32();
            ValidateResult(uintResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<ulong> ulongResult = value.AsUInt64();
            ValidateResult(ulongResult, value);
        }

        public void RunGenericScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunGenericScenario));
            Vector64<Int16> value;

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<byte> byteResult = value.As<Int16, byte>();
            ValidateResult(byteResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<double> doubleResult = value.As<Int16, double>();
            ValidateResult(doubleResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<short> shortResult = value.As<Int16, short>();
            ValidateResult(shortResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<int> intResult = value.As<Int16, int>();
            ValidateResult(intResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<long> longResult = value.As<Int16, long>();
            ValidateResult(longResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<sbyte> sbyteResult = value.As<Int16, sbyte>();
            ValidateResult(sbyteResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<float> floatResult = value.As<Int16, float>();
            ValidateResult(floatResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<ushort> ushortResult = value.As<Int16, ushort>();
            ValidateResult(ushortResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<uint> uintResult = value.As<Int16, uint>();
            ValidateResult(uintResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            Vector64<ulong> ulongResult = value.As<Int16, ulong>();
            ValidateResult(ulongResult, value);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector64<Int16> value;

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object byteResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsByte))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<byte>)(byteResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object doubleResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsDouble))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<double>)(doubleResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object shortResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsInt16))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<short>)(shortResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object intResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsInt32))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<int>)(intResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object longResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsInt64))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<long>)(longResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object sbyteResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsSByte))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<sbyte>)(sbyteResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object floatResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsSingle))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<float>)(floatResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object ushortResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsUInt16))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<ushort>)(ushortResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object uintResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsUInt32))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<uint>)(uintResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetInt16());
            object ulongResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsUInt64))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<ulong>)(ulongResult), value);
        }

        private void ValidateResult<T>(Vector64<T> result, Vector64<Int16> value, [CallerMemberName] string method = "")
            where T : struct
        {
            Int16[] resultElements = new Int16[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int16, byte>(ref resultElements[0]), result);

            Int16[] valueElements = new Int16[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int16, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, typeof(T), method);
        }

        private void ValidateResult(Int16[] resultElements, Int16[] valueElements, Type targetType, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector64<Int16>.As{targetType.Name}: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
