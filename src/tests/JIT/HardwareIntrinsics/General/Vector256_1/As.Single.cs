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

namespace JIT.HardwareIntrinsics.General._Vector256_1
{
    public static partial class Program
    {
        [Fact]
        public static void AsSingle()
        {
            var test = new VectorAs__AsSingle();

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

    public sealed unsafe class VectorAs__AsSingle
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<Single>>() / sizeof(Single);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector256<Single> value;

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<byte> byteResult = value.AsByte();
            ValidateResult(byteResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<double> doubleResult = value.AsDouble();
            ValidateResult(doubleResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<short> shortResult = value.AsInt16();
            ValidateResult(shortResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<int> intResult = value.AsInt32();
            ValidateResult(intResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<long> longResult = value.AsInt64();
            ValidateResult(longResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<sbyte> sbyteResult = value.AsSByte();
            ValidateResult(sbyteResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<float> floatResult = value.AsSingle();
            ValidateResult(floatResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<ushort> ushortResult = value.AsUInt16();
            ValidateResult(ushortResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<uint> uintResult = value.AsUInt32();
            ValidateResult(uintResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<ulong> ulongResult = value.AsUInt64();
            ValidateResult(ulongResult, value);
        }

        public void RunGenericScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunGenericScenario));
            Vector256<Single> value;

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<byte> byteResult = value.As<Single, byte>();
            ValidateResult(byteResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<double> doubleResult = value.As<Single, double>();
            ValidateResult(doubleResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<short> shortResult = value.As<Single, short>();
            ValidateResult(shortResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<int> intResult = value.As<Single, int>();
            ValidateResult(intResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<long> longResult = value.As<Single, long>();
            ValidateResult(longResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<sbyte> sbyteResult = value.As<Single, sbyte>();
            ValidateResult(sbyteResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<float> floatResult = value.As<Single, float>();
            ValidateResult(floatResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<ushort> ushortResult = value.As<Single, ushort>();
            ValidateResult(ushortResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<uint> uintResult = value.As<Single, uint>();
            ValidateResult(uintResult, value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            Vector256<ulong> ulongResult = value.As<Single, ulong>();
            ValidateResult(ulongResult, value);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector256<Single> value;

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object byteResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsByte))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<byte>)(byteResult), value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object doubleResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsDouble))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<double>)(doubleResult), value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object shortResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsInt16))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<short>)(shortResult), value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object intResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsInt32))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<int>)(intResult), value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object longResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsInt64))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<long>)(longResult), value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object sbyteResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsSByte))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<sbyte>)(sbyteResult), value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object floatResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsSingle))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<float>)(floatResult), value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object ushortResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsUInt16))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<ushort>)(ushortResult), value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object uintResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsUInt32))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<uint>)(uintResult), value);

            value = Vector256.Create((float)TestLibrary.Generator.GetSingle());
            object ulongResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.AsUInt64))
                                    .MakeGenericMethod(typeof(Single))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<ulong>)(ulongResult), value);
        }

        private void ValidateResult<T>(Vector256<T> result, Vector256<Single> value, [CallerMemberName] string method = "")
            where T : struct
        {
            Single[] resultElements = new Single[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref resultElements[0]), result);

            Single[] valueElements = new Single[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, typeof(T), method);
        }

        private void ValidateResult(Single[] resultElements, Single[] valueElements, Type targetType, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector256<Single>.As{targetType.Name}: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
