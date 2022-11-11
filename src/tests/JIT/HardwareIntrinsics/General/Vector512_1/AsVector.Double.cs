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
using Xunit;

namespace JIT.HardwareIntrinsics.General._Vector512_1
{
    public static partial class Program
    {
        [Fact]
        public static void AsVectorDouble()
        {
            var test = new VectorAs__AsVectorDouble();

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

    public sealed unsafe class VectorAs__AsVectorDouble
    {
        private static readonly int LargestVectorSize = 64;

        private static readonly int VectorElementCount = Unsafe.SizeOf<Vector512<Double>>() / sizeof(Double);

        private static readonly int NumericsElementCount = Unsafe.SizeOf<Vector<Double>>() / sizeof(Double);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector512<Double> value;

            value = Vector512.Create((double)TestLibrary.Generator.GetDouble());
            Vector<Double> result = value.AsVector();
            ValidateResult(result, value);

            value = result.AsVector512();
            ValidateResult(value, result);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector512<Double> value;

            value = Vector512.Create((double)TestLibrary.Generator.GetDouble());
            object Result = typeof(Vector512)
                                .GetMethod(nameof(Vector512.AsVector))
                                .MakeGenericMethod(typeof(Double))
                                .Invoke(null, new object[] { value });
            ValidateResult((Vector<Double>)(Result), value);

            value = (Vector512<Double>)typeof(Vector512)
                                .GetMethods()
                                .Where((methodInfo) => {
                                    if (methodInfo.Name == nameof(Vector512.AsVector512))
                                    {
                                        var parameters = methodInfo.GetParameters();
                                        return (parameters.Length == 1) &&
                                               (parameters[0].ParameterType.IsGenericType) &&
                                               (parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(Vector<>));
                                    }
                                    return false;
                                })
                                .Single()
                                .MakeGenericMethod(typeof(Double))
                                .Invoke(null, new object[] { Result });
            ValidateResult(value, (Vector<Double>)(Result));
        }

        private void ValidateResult(Vector<Double> result, Vector512<Double> value, [CallerMemberName] string method = "")
        {
            Double[] resultElements = new Double[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref resultElements[0]), result);

            Double[] valueElements = new Double[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(Vector512<Double> result, Vector<Double> value, [CallerMemberName] string method = "")
        {
            Double[] resultElements = new Double[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref resultElements[0]), result);

            Double[] valueElements = new Double[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(Double[] resultElements, Double[] valueElements, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector512<Double>.AsVector: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
