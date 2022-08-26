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
        public static void AsByte()
        {
            var test = new VectorAs__AsByte();

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

    public sealed unsafe class VectorAs__AsByte
    {
        private static readonly int LargestVectorSize = 8;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector64<Byte>>() / sizeof(Byte);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector64<Byte> value;

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<byte> byteResult = value.AsByte();
            ValidateResult(byteResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<double> doubleResult = value.AsDouble();
            ValidateResult(doubleResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<short> shortResult = value.AsInt16();
            ValidateResult(shortResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<int> intResult = value.AsInt32();
            ValidateResult(intResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<long> longResult = value.AsInt64();
            ValidateResult(longResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<sbyte> sbyteResult = value.AsSByte();
            ValidateResult(sbyteResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<float> floatResult = value.AsSingle();
            ValidateResult(floatResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<ushort> ushortResult = value.AsUInt16();
            ValidateResult(ushortResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<uint> uintResult = value.AsUInt32();
            ValidateResult(uintResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<ulong> ulongResult = value.AsUInt64();
            ValidateResult(ulongResult, value);
        }

        public void RunGenericScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunGenericScenario));
            Vector64<Byte> value;

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<byte> byteResult = value.As<Byte, byte>();
            ValidateResult(byteResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<double> doubleResult = value.As<Byte, double>();
            ValidateResult(doubleResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<short> shortResult = value.As<Byte, short>();
            ValidateResult(shortResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<int> intResult = value.As<Byte, int>();
            ValidateResult(intResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<long> longResult = value.As<Byte, long>();
            ValidateResult(longResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<sbyte> sbyteResult = value.As<Byte, sbyte>();
            ValidateResult(sbyteResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<float> floatResult = value.As<Byte, float>();
            ValidateResult(floatResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<ushort> ushortResult = value.As<Byte, ushort>();
            ValidateResult(ushortResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<uint> uintResult = value.As<Byte, uint>();
            ValidateResult(uintResult, value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            Vector64<ulong> ulongResult = value.As<Byte, ulong>();
            ValidateResult(ulongResult, value);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector64<Byte> value;

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object byteResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsByte))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<byte>)(byteResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object doubleResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsDouble))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<double>)(doubleResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object shortResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsInt16))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<short>)(shortResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object intResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsInt32))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<int>)(intResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object longResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsInt64))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<long>)(longResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object sbyteResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsSByte))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<sbyte>)(sbyteResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object floatResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsSingle))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<float>)(floatResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object ushortResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsUInt16))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<ushort>)(ushortResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object uintResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsUInt32))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<uint>)(uintResult), value);

            value = Vector64.Create(TestLibrary.Generator.GetByte());
            object ulongResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.AsUInt64))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector64<ulong>)(ulongResult), value);
        }

        private void ValidateResult<T>(Vector64<T> result, Vector64<Byte> value, [CallerMemberName] string method = "")
            where T : struct
        {
            Byte[] resultElements = new Byte[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref resultElements[0]), result);

            Byte[] valueElements = new Byte[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, typeof(T), method);
        }

        private void ValidateResult(Byte[] resultElements, Byte[] valueElements, Type targetType, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector64<Byte>.As{targetType.Name}: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
