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

namespace JIT.HardwareIntrinsics.General._Vector128_1
{
    public static partial class Program
    {
        [Fact]
        public static void AsVectorByte()
        {
            var test = new VectorAs__AsVectorByte();

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

    public sealed unsafe class VectorAs__AsVectorByte
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int VectorElementCount = Unsafe.SizeOf<Vector128<Byte>>() / sizeof(Byte);

        private static readonly int NumericsElementCount = Unsafe.SizeOf<Vector<Byte>>() / sizeof(Byte);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector128<Byte> value;

            value = Vector128.Create((byte)TestLibrary.Generator.GetByte());
            Vector<Byte> result = value.AsVector();
            ValidateResult(result, value);

            value = result.AsVector128();
            ValidateResult(value, result);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector128<Byte> value;

            value = Vector128.Create((byte)TestLibrary.Generator.GetByte());
            object Result = typeof(Vector128)
                                .GetMethod(nameof(Vector128.AsVector))
                                .MakeGenericMethod(typeof(Byte))
                                .Invoke(null, new object[] { value });
            ValidateResult((Vector<Byte>)(Result), value);

            value = (Vector128<Byte>)typeof(Vector128)
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
                                .MakeGenericMethod(typeof(Byte))
                                .Invoke(null, new object[] { Result });
            ValidateResult(value, (Vector<Byte>)(Result));
        }

        private void ValidateResult(Vector<Byte> result, Vector128<Byte> value, [CallerMemberName] string method = "")
        {
            Byte[] resultElements = new Byte[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref resultElements[0]), result);

            Byte[] valueElements = new Byte[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(Vector128<Byte> result, Vector<Byte> value, [CallerMemberName] string method = "")
        {
            Byte[] resultElements = new Byte[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref resultElements[0]), result);

            Byte[] valueElements = new Byte[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(Byte[] resultElements, Byte[] valueElements, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector128<Byte>.AsVector: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
