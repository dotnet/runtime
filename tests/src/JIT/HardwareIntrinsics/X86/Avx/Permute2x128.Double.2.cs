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
        private static void Permute2x128Double2()
        {
            var test = new SimpleBinaryOpTest__Permute2x128Double2();

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

    public sealed unsafe class SimpleBinaryOpTest__Permute2x128Double2
    {
        private const int VectorSize = 32;

        private const int Op1ElementCount = VectorSize / sizeof(Double);
        private const int Op2ElementCount = VectorSize / sizeof(Double);
        private const int RetElementCount = VectorSize / sizeof(Double);

        private static Double[] _data1 = new Double[Op1ElementCount];
        private static Double[] _data2 = new Double[Op2ElementCount];

        private static Vector256<Double> _clsVar1;
        private static Vector256<Double> _clsVar2;

        private Vector256<Double> _fld1;
        private Vector256<Double> _fld2;

        private SimpleBinaryOpTest__DataTable<Double, Double, Double> _dataTable;

        static SimpleBinaryOpTest__Permute2x128Double2()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (double)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Double>, byte>(ref _clsVar1), ref Unsafe.As<Double, byte>(ref _data1[0]), VectorSize);
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (double)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Double>, byte>(ref _clsVar2), ref Unsafe.As<Double, byte>(ref _data2[0]), VectorSize);
        }

        public SimpleBinaryOpTest__Permute2x128Double2()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (double)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Double>, byte>(ref _fld1), ref Unsafe.As<Double, byte>(ref _data1[0]), VectorSize);
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (double)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Double>, byte>(ref _fld2), ref Unsafe.As<Double, byte>(ref _data2[0]), VectorSize);

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (double)(random.NextDouble()); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (double)(random.NextDouble()); }
            _dataTable = new SimpleBinaryOpTest__DataTable<Double, Double, Double>(_data1, _data2, new Double[RetElementCount], VectorSize);
        }

        public bool IsSupported => Avx.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Avx.Permute2x128(
                Unsafe.Read<Vector256<Double>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector256<Double>>(_dataTable.inArray2Ptr),
                2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Avx.Permute2x128(
                Avx.LoadVector256((Double*)(_dataTable.inArray1Ptr)),
                Avx.LoadVector256((Double*)(_dataTable.inArray2Ptr)),
                2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Avx.Permute2x128(
                Avx.LoadAlignedVector256((Double*)(_dataTable.inArray1Ptr)),
                Avx.LoadAlignedVector256((Double*)(_dataTable.inArray2Ptr)),
                2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Avx).GetMethod(nameof(Avx.Permute2x128)).MakeGenericMethod( new Type[] { typeof(Double) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<Double>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector256<Double>>(_dataTable.inArray2Ptr),
                                        (byte)2
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Double>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Avx).GetMethod(nameof(Avx.Permute2x128)).MakeGenericMethod( new Type[] { typeof(Double) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadVector256((Double*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadVector256((Double*)(_dataTable.inArray2Ptr)),
                                        (byte)2
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Double>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Avx).GetMethod(nameof(Avx.Permute2x128)).MakeGenericMethod( new Type[] { typeof(Double) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadAlignedVector256((Double*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadAlignedVector256((Double*)(_dataTable.inArray2Ptr)),
                                        (byte)2
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Double>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Avx.Permute2x128(
                _clsVar1,
                _clsVar2,
                2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var left = Unsafe.Read<Vector256<Double>>(_dataTable.inArray1Ptr);
            var right = Unsafe.Read<Vector256<Double>>(_dataTable.inArray2Ptr);
            var result = Avx.Permute2x128(left, right, 2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var left = Avx.LoadVector256((Double*)(_dataTable.inArray1Ptr));
            var right = Avx.LoadVector256((Double*)(_dataTable.inArray2Ptr));
            var result = Avx.Permute2x128(left, right, 2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var left = Avx.LoadAlignedVector256((Double*)(_dataTable.inArray1Ptr));
            var right = Avx.LoadAlignedVector256((Double*)(_dataTable.inArray2Ptr));
            var result = Avx.Permute2x128(left, right, 2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleBinaryOpTest__Permute2x128Double2();
            var result = Avx.Permute2x128(test._fld1, test._fld2, 2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Avx.Permute2x128(_fld1, _fld2, 2);

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

        private void ValidateResult(Vector256<Double> left, Vector256<Double> right, void* result, [CallerMemberName] string method = "")
        {
            Double[] inArray1 = new Double[Op1ElementCount];
            Double[] inArray2 = new Double[Op2ElementCount];
            Double[] outArray = new Double[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref inArray1[0]), left);
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref inArray2[0]), right);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* left, void* right, void* result, [CallerMemberName] string method = "")
        {
            Double[] inArray1 = new Double[Op1ElementCount];
            Double[] inArray2 = new Double[Op2ElementCount];
            Double[] outArray = new Double[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(left), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(right), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(Double[] left, Double[] right, Double[] result, [CallerMemberName] string method = "")
        {
            if (BitConverter.DoubleToInt64Bits(result[0]) != BitConverter.DoubleToInt64Bits(right[0]))
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (i > 1 ? (BitConverter.DoubleToInt64Bits(result[i]) != BitConverter.DoubleToInt64Bits(left[i - 2])) : (BitConverter.DoubleToInt64Bits(result[i]) != BitConverter.DoubleToInt64Bits(right[i])))
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.Permute2x128)}<Double>(Vector256<Double>.2, Vector256<Double>): {method} failed:");
                Console.WriteLine($"    left: ({string.Join(", ", left)})");
                Console.WriteLine($"   right: ({string.Join(", ", right)})");
                Console.WriteLine($"  result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
