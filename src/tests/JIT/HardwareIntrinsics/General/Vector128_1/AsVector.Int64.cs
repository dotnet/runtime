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
        private static void AsVectorInt64()
        {
            var test = new VectorAs__AsVectorInt64();

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

    public sealed unsafe class VectorAs__AsVectorInt64
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int VectorElementCount = Unsafe.SizeOf<Vector128<Int64>>() / sizeof(Int64);

        private static readonly int NumericsElementCount = Unsafe.SizeOf<Vector<Int64>>() / sizeof(Int64);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector128<Int64> value;

            value = Vector128.Create((long)TestLibrary.Generator.GetInt64());
            Vector<Int64> result = value.AsVector();
            ValidateResult(result, value);

            value = result.AsVector128();
            ValidateResult(value, result);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector128<Int64> value;

            value = Vector128.Create((long)TestLibrary.Generator.GetInt64());
            object Result = typeof(Vector128)
                                .GetMethod(nameof(Vector128.AsVector))
                                .MakeGenericMethod(typeof(Int64))
                                .Invoke(null, new object[] { value });
            ValidateResult((Vector<Int64>)(Result), value);

            value = (Vector128<Int64>)typeof(Vector128)
                                .GetMethods()
                                .Where((methodInfo) => {
                                    if (methodInfo.Name == nameof(Vector128.AsVector128))
                                    {
                                        var parameters = methodInfo.GetParameters();
                                        return (parameters.Length == 1) &&
                                               (parameters[0].ParameterType.IsGenericType) &&
                                               (parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(Vector<>));
                                    }
                                    return false;
                                })
                                .Single()
                                .MakeGenericMethod(typeof(Int64))
                                .Invoke(null, new object[] { Result });
            ValidateResult(value, (Vector<Int64>)(Result));
        }

        private void ValidateResult(Vector<Int64> result, Vector128<Int64> value, [CallerMemberName] string method = "")
        {
            Int64[] resultElements = new Int64[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref resultElements[0]), result);

            Int64[] valueElements = new Int64[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(Vector128<Int64> result, Vector<Int64> value, [CallerMemberName] string method = "")
        {
            Int64[] resultElements = new Int64[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref resultElements[0]), result);

            Int64[] valueElements = new Int64[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(Int64[] resultElements, Int64[] valueElements, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector128<Int64>.AsVector: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
