// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\General\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace JIT.HardwareIntrinsics.General
{
    public static partial class Program
    {
        private static void AsVector2Single()
        {
            var test = new VectorAs__AsVector2Single();

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

    public sealed unsafe class VectorAs__AsVector2Single
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int VectorElementCount = Unsafe.SizeOf<Vector128<Single>>() / sizeof(Single);

        private static readonly int NumericsElementCount = Unsafe.SizeOf<Vector2>() / sizeof(Single);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));
            Vector128<Single> value;

            value = Vector128.Create(TestLibrary.Generator.GetSingle());
            Vector2 result = value.AsVector2();
            ValidateResult(result, value);

            value = result.AsVector128();
            ValidateResult(value, result);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));
            Vector128<Single> value;

            value = Vector128.Create(TestLibrary.Generator.GetSingle());
            object Result = typeof(Vector128)
                                .GetMethod(nameof(Vector128.AsVector2))
                                .Invoke(null, new object[] { value });
            ValidateResult((Vector2)(Result), value);

            value = (Vector128<Single>)typeof(Vector128)
                                .GetMethod(nameof(Vector128.AsVector128), new Type[] { typeof(Vector2) })
                                .Invoke(null, new object[] { Result });
            ValidateResult(value, (Vector2)(Result));
        }

        private void ValidateResult(Vector2 result, Vector128<Single> value, [CallerMemberName] string method = "")
        {
            Single[] resultElements = new Single[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref resultElements[0]), result);

            Single[] valueElements = new Single[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(Vector128<Single> result, Vector2 value, [CallerMemberName] string method = "")
        {
            Single[] resultElements = new Single[VectorElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref resultElements[0]), result);

            Single[] valueElements = new Single[NumericsElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref valueElements[0]), value);

            ValidateResult(resultElements, valueElements, method);
        }

        private void ValidateResult(Single[] resultElements, Single[] valueElements, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector128<Single>.AsVector2: {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", valueElements)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
