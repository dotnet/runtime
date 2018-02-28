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
        private static void TestNotZAndNotCUInt32()
        {
            var test = new BooleanTwoComparisonOpTest__TestNotZAndNotCUInt32();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Avx.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (Avx.IsSupported)
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

                if (Avx.IsSupported)
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

    public sealed unsafe class BooleanTwoComparisonOpTest__TestNotZAndNotCUInt32
    {
        private const int VectorSize = 32;

        private const int Op1ElementCount = VectorSize / sizeof(UInt32);
        private const int Op2ElementCount = VectorSize / sizeof(UInt32);

        private static UInt32[] _data1 = new UInt32[Op1ElementCount];
        private static UInt32[] _data2 = new UInt32[Op2ElementCount];

        private static Vector256<UInt32> _clsVar1;
        private static Vector256<UInt32> _clsVar2;

        private Vector256<UInt32> _fld1;
        private Vector256<UInt32> _fld2;

        private BooleanTwoComparisonOpTest__DataTable<UInt32, UInt32> _dataTable;

        static BooleanTwoComparisonOpTest__TestNotZAndNotCUInt32()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (uint)(random.Next(0, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref _clsVar1), ref Unsafe.As<UInt32, byte>(ref _data1[0]), VectorSize);
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (uint)(random.Next(0, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref _clsVar2), ref Unsafe.As<UInt32, byte>(ref _data2[0]), VectorSize);
        }

        public BooleanTwoComparisonOpTest__TestNotZAndNotCUInt32()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (uint)(random.Next(0, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref _fld1), ref Unsafe.As<UInt32, byte>(ref _data1[0]), VectorSize);
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (uint)(random.Next(0, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref _fld2), ref Unsafe.As<UInt32, byte>(ref _data2[0]), VectorSize);

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (uint)(random.Next(0, int.MaxValue)); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (uint)(random.Next(0, int.MaxValue)); }
            _dataTable = new BooleanTwoComparisonOpTest__DataTable<UInt32, UInt32>(_data1, _data2, VectorSize);
        }

        public bool IsSupported => Avx.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Avx.TestNotZAndNotC(
                Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray2Ptr)
            );

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, result);
        }

        public void RunBasicScenario_Load()
        {
            var result = Avx.TestNotZAndNotC(
                Avx.LoadVector256((UInt32*)(_dataTable.inArray1Ptr)),
                Avx.LoadVector256((UInt32*)(_dataTable.inArray2Ptr))
            );

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, result);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Avx.TestNotZAndNotC(
                Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray1Ptr)),
                Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray2Ptr))
            );

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var method = typeof(Avx).GetMethod(nameof(Avx.TestNotZAndNotC), new Type[] { typeof(Vector256<UInt32>), typeof(Vector256<UInt32>) });
            
            if (method != null)
            {
                var result = method.Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray2Ptr)
                                     });

                ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, (bool)(result));
            }
        }

        public void RunReflectionScenario_Load()
        {
            var method = typeof(Avx).GetMethod(nameof(Avx.TestNotZAndNotC), new Type[] { typeof(Vector256<UInt32>), typeof(Vector256<UInt32>) });
            
            if (method != null)
            {
                var result = method.Invoke(null, new object[] {
                                        Avx.LoadVector256((UInt32*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadVector256((UInt32*)(_dataTable.inArray2Ptr))
                                     });

                ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, (bool)(result));
            }
        }

        public void RunReflectionScenario_LoadAligned()
        {var method = typeof(Avx).GetMethod(nameof(Avx.TestNotZAndNotC), new Type[] { typeof(Vector256<UInt32>), typeof(Vector256<UInt32>) });
            
            if (method != null)
            {
                var result = method.Invoke(null, new object[] {
                                        Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray2Ptr))
                                     });

                ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, (bool)(result));
            }
        }

        public void RunClsVarScenario()
        {
            var result = Avx.TestNotZAndNotC(
                _clsVar1,
                _clsVar2
            );

            ValidateResult(_clsVar1, _clsVar2, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var left = Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray1Ptr);
            var right = Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray2Ptr);
            var result = Avx.TestNotZAndNotC(left, right);

            ValidateResult(left, right, result);
        }

        public void RunLclVarScenario_Load()
        {
            var left = Avx.LoadVector256((UInt32*)(_dataTable.inArray1Ptr));
            var right = Avx.LoadVector256((UInt32*)(_dataTable.inArray2Ptr));
            var result = Avx.TestNotZAndNotC(left, right);

            ValidateResult(left, right, result);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var left = Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray1Ptr));
            var right = Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray2Ptr));
            var result = Avx.TestNotZAndNotC(left, right);

            ValidateResult(left, right, result);
        }

        public void RunLclFldScenario()
        {
            var test = new BooleanTwoComparisonOpTest__TestNotZAndNotCUInt32();
            var result = Avx.TestNotZAndNotC(test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunFldScenario()
        {
            var result = Avx.TestNotZAndNotC(_fld1, _fld2);

            ValidateResult(_fld1, _fld2, result);
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

        private void ValidateResult(Vector256<UInt32> left, Vector256<UInt32> right, bool result, [CallerMemberName] string method = "")
        {
            UInt32[] inArray1 = new UInt32[Op1ElementCount];
            UInt32[] inArray2 = new UInt32[Op2ElementCount];

            Unsafe.Write(Unsafe.AsPointer(ref inArray1[0]), left);
            Unsafe.Write(Unsafe.AsPointer(ref inArray2[0]), right);

            ValidateResult(inArray1, inArray2, result, method);
        }

        private void ValidateResult(void* left, void* right, bool result, [CallerMemberName] string method = "")
        {
            UInt32[] inArray1 = new UInt32[Op1ElementCount];
            UInt32[] inArray2 = new UInt32[Op2ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(left), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(right), VectorSize);

            ValidateResult(inArray1, inArray2, result, method);
        }

        private void ValidateResult(UInt32[] left, UInt32[] right, bool result, [CallerMemberName] string method = "")
        {
            var expectedResult1 = true;

            for (var i = 0; i < Op1ElementCount; i++)
            {
                expectedResult1 &= (((left[i] & right[i]) == 0));
            }

            var expectedResult2 = true;

            for (var i = 0; i < Op1ElementCount; i++)
            {
                expectedResult2 &= (((~left[i] & right[i]) == 0));
            }

            if (((expectedResult1 == false) && (expectedResult2 == false)) != result)
            {
                Succeeded = false;

                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.TestNotZAndNotC)}<UInt32>(Vector256<UInt32>, Vector256<UInt32>): {method} failed:");
                Console.WriteLine($"    left: ({string.Join(", ", left)})");
                Console.WriteLine($"   right: ({string.Join(", ", right)})");
                Console.WriteLine($"  result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
