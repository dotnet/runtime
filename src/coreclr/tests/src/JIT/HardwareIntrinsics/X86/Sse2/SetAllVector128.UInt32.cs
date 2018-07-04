// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\X86\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void SetAllVector128UInt32()
        {
            bool skipIf32Bit = (typeof(UInt32) == typeof(Int64)) ||
                               (typeof(UInt32) == typeof(UInt64));

            if (skipIf32Bit && !Environment.Is64BitProcess)
            {
                return;
            }

            var test = new ScalarSimdUnaryOpTest__SetAllVector128UInt32();

            if (test.IsSupported)
            {
                // Validates basic functionality works
                test.RunBasicScenario();

                // Validates calling via reflection works
                test.RunReflectionScenario();

                // Validates passing a static member works
                test.RunClsVarScenario();

                // Validates passing a local works
                test.RunLclVarScenario();

                // Validates passing the field of a local works
                test.RunLclFldScenario();

                // Validates passing an instance member works
                test.RunFldScenario();
            }
            else
            {
                // Validates we throw on unsupported hardware
                test.RunUnsupportedScenario();
            }

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class ScalarSimdUnaryOpTest__SetAllVector128UInt32
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<UInt32>>() / sizeof(UInt32);

        private static readonly Random Random = new Random();

        private static UInt32 _clsVar;

        private UInt32 _fld;

        private ScalarSimdUnaryOpTest__DataTable<UInt32> _dataTable;

        static ScalarSimdUnaryOpTest__SetAllVector128UInt32()
        {
            _clsVar = (uint)(Random.Next(0, int.MaxValue));
        }

        public ScalarSimdUnaryOpTest__SetAllVector128UInt32()
        {
            Succeeded = true;

            _fld = (uint)(Random.Next(0, int.MaxValue));
            _dataTable = new ScalarSimdUnaryOpTest__DataTable<UInt32>(new UInt32[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario()
        {
            var firstOp = (uint)(Random.Next(0, int.MaxValue));
            var result = Sse2.SetAllVector128(
                firstOp
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario()
        {
            var firstOp = (uint)(Random.Next(0, int.MaxValue));
            var method = typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), new Type[] { typeof(UInt32) });
            var result = method.Invoke(null, new object[] { firstOp });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt32>)(result));
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Sse2.SetAllVector128(
                _clsVar
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario()
        {
            var firstOp = (uint)(Random.Next(0, int.MaxValue));
            var result = Sse2.SetAllVector128(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new ScalarSimdUnaryOpTest__SetAllVector128UInt32();
            var result = Sse2.SetAllVector128(test._fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Sse2.SetAllVector128(_fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld, _dataTable.outArrayPtr);
        }

        public void RunUnsupportedScenario()
        {
            Succeeded = false;

            try
            {
                RunBasicScenario();
            }
            catch (PlatformNotSupportedException)
            {
                Succeeded = true;
            }
        }

        private void ValidateResult(UInt32 firstOp, void* result, [CallerMemberName] string method = "")
        {
            UInt32[] outArray = new UInt32[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt32>>());

            ValidateResult(firstOp, outArray, method);
        }

        private void ValidateResult(UInt32 firstOp, UInt32[] result, [CallerMemberName] string method = "")
        {
            if (result[0] != firstOp)
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (result[i] != firstOp)
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.SetAllVector128)}<UInt32>(Vector128<UInt32>): {method} failed:");
                Console.WriteLine($"  firstOp: ({string.Join(", ", firstOp)})");
                Console.WriteLine($"   result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
