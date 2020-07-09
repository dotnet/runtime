// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\General\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace JIT.HardwareIntrinsics.General
{
    public static partial class Program
    {
        private static void AsVectorUInt64()
        {
            var test = new VectorAs__AsVectorUInt64();

            // Validates basic functionality works
            test.RunBasicScenario();

            // Validates calling via reflection works
            test.RunReflectionScenario();

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class VectorAs__AsVectorUInt64
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int VectorElementCount = Unsafe.SizeOf<Vector256<UInt64>>() / sizeof(UInt64);

        private static readonly int NumericsElementCount = Unsafe.SizeOf<Vector<UInt64>>() / sizeof(UInt64);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector256<UInt64> value;

            value = Vector256.Create((ulong)TestLibrary.Generator.GetUInt64());
            Vector<UInt64> result = value.AsVector();
            ValidateResult(result, value);

            value = result.AsVector256();
            ValidateResult(value, result);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector256<UInt64> value;

            value = Vector256.Create((ulong)TestLibrary.Generator.GetUInt64());
            object Result = typeof(Vector256)
                                .GetMethod(nameof(Vector256.AsVector))
                                .MakeGenericMethod(typeof(UInt64))
                                .Invoke(null, new object[] { value });
            ValidateResult((Vector<UInt64>)(Result), value);

            value = (Vector256<UInt64>)typeof(Vector256)
                                .GetMethods()
                                .Where((methodInfo) => {
                                    if (methodInfo.Name == nameof(Vector256.AsVector256))
                                    {
                                        var parameters = methodInfo.GetParameters();
                                        return (parameters.Length == 1) &&
                                               (parameters[0].ParameterType.IsGenericType) &&
                                               (parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(Vector<>));
                                    }
                                    return false;
                                })
                                .Single()
                                .MakeGenericMethod(typeof(UInt64))
                                .Invoke(null, new object[] { Result });
            ValidateResult(value, (Vector<UInt64>)(Result));
        }

        private void ValidateResult(Vector<UInt64> result, Vector256<UInt64> value, [CallerMemberName] string method = "")
        {
            UInt64[] resultElements = new UInt64[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt64, byte>(ref resultElements[0]), result);

            UInt64[] valueElements = new UInt64[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt64, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(Vector256<UInt64> result, Vector<UInt64> value, [CallerMemberName] string method = "")
        {
            UInt64[] resultElements = new UInt64[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt64, byte>(ref resultElements[0]), result);

            UInt64[] valueElements = new UInt64[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt64, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(UInt64[] resultElements, UInt64[] valueElements, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (resultElements.Length <= valueElements.Length)
            {
                for (var i = 0; i < resultElements.Length; i++)
                {
                    if (resultElements[i] != valueElements[i])
                    {
                        succeeded = false;
                        break;
                    }
                }
            }
            else
            {
                for (var i = 0; i < valueElements.Length; i++)
                {
                    if (resultElements[i] != valueElements[i])
                    {
                        succeeded = false;
                        break;
                    }
                }

                for (var i = valueElements.Length; i < resultElements.Length; i++)
                {
                    if (resultElements[i] != default)
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256<UInt64>.AsVector: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
