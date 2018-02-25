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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void TestAllOnesUInt64()
        {
            var test = new BooleanComparisonOpTest__TestAllOnesUInt64();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates calling via reflection works, using Load
                    test.RunReflectionScenario_Load();

                    // Validates calling via reflection works, using LoadAligned
                    test.RunReflectionScenario_LoadAligned();
                }

                // Validates passing a static member works
                test.RunClsVarScenario();

                // Validates passing a local works, using Unsafe.Read
                test.RunLclVarScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates passing a local works, using Load
                    test.RunLclVarScenario_Load();

                    // Validates passing a local works, using LoadAligned
                    test.RunLclVarScenario_LoadAligned();
                }

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

    public sealed unsafe class BooleanComparisonOpTest__TestAllOnesUInt64
    {
        private const int VectorSize = 16;

        private const int Op1ElementCount = VectorSize / sizeof(UInt64);

        private static UInt64[] _data = new UInt64[Op1ElementCount];

        private static Vector128<UInt64> _clsVar;

        private Vector128<UInt64> _fld;

        private BooleanUnaryOpTest__DataTable<UInt64> _dataTable;

        static BooleanComparisonOpTest__TestAllOnesUInt64()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ulong)(random.Next(0, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt64>, byte>(ref _clsVar), ref Unsafe.As<UInt64, byte>(ref _data[0]), VectorSize);
        }

        public BooleanComparisonOpTest__TestAllOnesUInt64()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ulong)(random.Next(0, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt64>, byte>(ref _fld), ref Unsafe.As<UInt64, byte>(ref _data[0]), VectorSize);

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ulong)(random.Next(0, int.MaxValue)); }
            _dataTable = new BooleanUnaryOpTest__DataTable<UInt64>(_data, VectorSize);
        }

        public bool IsSupported => Sse41.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Sse41.TestAllOnes(
                Unsafe.Read<Vector128<UInt64>>(_dataTable.inArrayPtr)
            );

            ValidateResult(_dataTable.inArrayPtr, result);
        }

        public void RunBasicScenario_Load()
        {
            var result = Sse41.TestAllOnes(
                Sse2.LoadVector128((UInt64*)(_dataTable.inArrayPtr))
            );

            ValidateResult(_dataTable.inArrayPtr, result);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Sse41.TestAllOnes(
                Sse2.LoadAlignedVector128((UInt64*)(_dataTable.inArrayPtr))
            );

            ValidateResult(_dataTable.inArrayPtr, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.TestAllOnes), new Type[] { typeof(Vector128<UInt64>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<UInt64>>(_dataTable.inArrayPtr)
                                     });

            ValidateResult(_dataTable.inArrayPtr, (bool)(result));
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.TestAllOnes), new Type[] { typeof(Vector128<UInt64>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((UInt64*)(_dataTable.inArrayPtr))
                                     });

            ValidateResult(_dataTable.inArrayPtr, (bool)(result));
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.TestAllOnes), new Type[] { typeof(Vector128<UInt64>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((UInt64*)(_dataTable.inArrayPtr))
                                     });

            ValidateResult(_dataTable.inArrayPtr, (bool)(result));
        }

        public void RunClsVarScenario()
        {
            var result = Sse41.TestAllOnes(
                _clsVar
            );

            ValidateResult(_clsVar, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var value = Unsafe.Read<Vector128<UInt64>>(_dataTable.inArrayPtr);
            var result = Sse41.TestAllOnes(value);

            ValidateResult(value, result);
        }

        public void RunLclVarScenario_Load()
        {
            var value = Sse2.LoadVector128((UInt64*)(_dataTable.inArrayPtr));
            var result = Sse41.TestAllOnes(value);

            ValidateResult(value, result);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var value = Sse2.LoadAlignedVector128((UInt64*)(_dataTable.inArrayPtr));
            var result = Sse41.TestAllOnes(value);

            ValidateResult(value, result);
        }

        public void RunLclFldScenario()
        {
            var test = new BooleanComparisonOpTest__TestAllOnesUInt64();
            var result = Sse41.TestAllOnes(test._fld);

            ValidateResult(test._fld, result);
        }

        public void RunFldScenario()
        {
            var result = Sse41.TestAllOnes(_fld);

            ValidateResult(_fld, result);
        }

        public void RunUnsupportedScenario()
        {
            Succeeded = false;

            try
            {
                RunBasicScenario_UnsafeRead();
            }
            catch (PlatformNotSupportedException)
            {
                Succeeded = true;
            }
        }

        private void ValidateResult(Vector128<UInt64> value, bool result, [CallerMemberName] string method = "")
        {
            UInt64[] inArray = new UInt64[Op1ElementCount];

            Unsafe.Write(Unsafe.AsPointer(ref inArray[0]), value);

            ValidateResult(inArray, result, method);
        }

        private void ValidateResult(void* value, bool result, [CallerMemberName] string method = "")
        {
            UInt64[] inArray = new UInt64[Op1ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt64, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(value), VectorSize);

            ValidateResult(inArray, result, method);
        }

        private void ValidateResult(UInt64[] value, bool result, [CallerMemberName] string method = "")
        {
            var expectedResult = true;

            for (var i = 0; i < Op1ElementCount; i++)
            {
                expectedResult &= ((~value[i] & ulong.MaxValue) == 0);
            }

            if (expectedResult != result)
            {
                Succeeded = false;

                Console.WriteLine($"{nameof(Sse41)}.{nameof(Sse41.TestAllOnes)}<UInt64>(Vector128<UInt64>): {method} failed:");
                Console.WriteLine($"    value: ({string.Join(", ", value)})");
                Console.WriteLine($"  result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
