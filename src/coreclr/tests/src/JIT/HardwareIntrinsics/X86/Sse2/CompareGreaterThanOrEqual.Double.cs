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
        private static void CompareGreaterThanOrEqualDouble()
        {
            var test = new SimpleBinaryOpTest__CompareGreaterThanOrEqualDouble();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                // Validates basic functionality works, using Load
                test.RunBasicScenario_Load();

                // Validates basic functionality works, using LoadAligned
                test.RunBasicScenario_LoadAligned();

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                // Validates calling via reflection works, using Load
                test.RunReflectionScenario_Load();

                // Validates calling via reflection works, using LoadAligned
                test.RunReflectionScenario_LoadAligned();

                // Validates passing a static member works
                test.RunClsVarScenario();

                // Validates passing a local works, using Unsafe.Read
                test.RunLclVarScenario_UnsafeRead();

                // Validates passing a local works, using Load
                test.RunLclVarScenario_Load();

                // Validates passing a local works, using LoadAligned
                test.RunLclVarScenario_LoadAligned();

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

    public sealed unsafe class SimpleBinaryOpTest__CompareGreaterThanOrEqualDouble
    {
        private const int VectorSize = 16;
        private const int ElementCount = VectorSize / sizeof(Double);

        private static Double[] _data1 = new Double[ElementCount];
        private static Double[] _data2 = new Double[ElementCount];

        private static Vector128<Double> _clsVar1;
        private static Vector128<Double> _clsVar2;

        private Vector128<Double> _fld1;
        private Vector128<Double> _fld2;

        private SimpleBinaryOpTest__DataTable<Double> _dataTable;

        static SimpleBinaryOpTest__CompareGreaterThanOrEqualDouble()
        {
            var random = new Random();

            for (var i = 0; i < ElementCount; i++) { _data1[i] = (double)(random.NextDouble()); _data2[i] = (double)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _clsVar1), ref Unsafe.As<Double, byte>(ref _data2[0]), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _clsVar2), ref Unsafe.As<Double, byte>(ref _data1[0]), VectorSize);
        }

        public SimpleBinaryOpTest__CompareGreaterThanOrEqualDouble()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < ElementCount; i++) { _data1[i] = (double)(random.NextDouble()); _data2[i] = (double)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _fld1), ref Unsafe.As<Double, byte>(ref _data1[0]), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _fld2), ref Unsafe.As<Double, byte>(ref _data2[0]), VectorSize);

            for (var i = 0; i < ElementCount; i++) { _data1[i] = (double)(random.NextDouble()); _data2[i] = (double)(random.NextDouble()); }
            _dataTable = new SimpleBinaryOpTest__DataTable<Double>(_data1, _data2, new Double[ElementCount], VectorSize);
        }

        public bool IsSupported => Sse2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Sse2.CompareGreaterThanOrEqual(
                Unsafe.Read<Vector128<Double>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Double>>(_dataTable.inArray2Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Sse2.CompareGreaterThanOrEqual(
                Sse2.LoadVector128((Double*)(_dataTable.inArray1Ptr)),
                Sse2.LoadVector128((Double*)(_dataTable.inArray2Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Sse2.CompareGreaterThanOrEqual(
                Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray1Ptr)),
                Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray2Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.CompareGreaterThanOrEqual), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Double>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<Double>>(_dataTable.inArray2Ptr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Double>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.CompareGreaterThanOrEqual), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((Double*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadVector128((Double*)(_dataTable.inArray2Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Double>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.CompareGreaterThanOrEqual), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray2Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Double>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Sse2.CompareGreaterThanOrEqual(
                _clsVar1,
                _clsVar2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var left = Unsafe.Read<Vector128<Double>>(_dataTable.inArray1Ptr);
            var right = Unsafe.Read<Vector128<Double>>(_dataTable.inArray2Ptr);
            var result = Sse2.CompareGreaterThanOrEqual(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var left = Sse2.LoadVector128((Double*)(_dataTable.inArray1Ptr));
            var right = Sse2.LoadVector128((Double*)(_dataTable.inArray2Ptr));
            var result = Sse2.CompareGreaterThanOrEqual(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var left = Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray1Ptr));
            var right = Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray2Ptr));
            var result = Sse2.CompareGreaterThanOrEqual(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleBinaryOpTest__CompareGreaterThanOrEqualDouble();
            var result = Sse2.CompareGreaterThanOrEqual(test._fld1, test._fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Sse2.CompareGreaterThanOrEqual(_fld1, _fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector128<Double> left, Vector128<Double> right, void* result, [CallerMemberName] string method = "")
        {
            Double[] inArray1 = new Double[ElementCount];
            Double[] inArray2 = new Double[ElementCount];
            Double[] outArray = new Double[ElementCount];

            Unsafe.Write(Unsafe.AsPointer(ref inArray1[0]), left);
            Unsafe.Write(Unsafe.AsPointer(ref inArray2[0]), right);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* left, void* right, void* result, [CallerMemberName] string method = "")
        {
            Double[] inArray1 = new Double[ElementCount];
            Double[] inArray2 = new Double[ElementCount];
            Double[] outArray = new Double[ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(left), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(right), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(Double[] left, Double[] right, Double[] result, [CallerMemberName] string method = "")
        {
            if (BitConverter.DoubleToInt64Bits(result[0]) != ((left[0] >= right[0]) ? -1 : 0))
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < left.Length; i++)
                {
                    if (BitConverter.DoubleToInt64Bits(result[i]) != ((left[i] >= right[i]) ? -1 : 0))
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.CompareGreaterThanOrEqual)}<Double>: {method} failed:");
                Console.WriteLine($"    left: ({string.Join(", ", left)})");
                Console.WriteLine($"   right: ({string.Join(", ", right)})");
                Console.WriteLine($"  result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
