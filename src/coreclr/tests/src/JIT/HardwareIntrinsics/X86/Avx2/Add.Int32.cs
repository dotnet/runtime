// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void AddInt32()
        {
            var test = new SimpleBinaryOpTest__AddInt32();

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

    public sealed unsafe class SimpleBinaryOpTest__AddInt32
    {
        private const int VectorSize = 32;
        private const int ElementCount = VectorSize / sizeof(Int32);

        private static Int32[] _data1 = new Int32[ElementCount];
        private static Int32[] _data2 = new Int32[ElementCount];

        private static Vector256<Int32> _clsVar1;
        private static Vector256<Int32> _clsVar2;

        private Vector256<Int32> _fld1;
        private Vector256<Int32> _fld2;

        private SimpleBinaryOpTest__DataTable<Int32> _dataTable;

        static SimpleBinaryOpTest__AddInt32()
        {
            var random = new Random();

            for (var i = 0; i < ElementCount; i++) { _data1[i] = (int)(random.Next(int.MinValue, int.MaxValue)); _data2[i] = (int)(random.Next(int.MinValue, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Int32>, byte>(ref _clsVar1), ref Unsafe.As<Int32, byte>(ref _data2[0]), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Int32>, byte>(ref _clsVar2), ref Unsafe.As<Int32, byte>(ref _data1[0]), VectorSize);
        }

        public SimpleBinaryOpTest__AddInt32()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < ElementCount; i++) { _data1[i] = (int)(random.Next(int.MinValue, int.MaxValue)); _data2[i] = (int)(random.Next(int.MinValue, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Int32>, byte>(ref _fld1), ref Unsafe.As<Int32, byte>(ref _data1[0]), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Int32>, byte>(ref _fld2), ref Unsafe.As<Int32, byte>(ref _data2[0]), VectorSize);

            for (var i = 0; i < ElementCount; i++) { _data1[i] = (int)(random.Next(int.MinValue, int.MaxValue)); _data2[i] = (int)(random.Next(int.MinValue, int.MaxValue)); }
            _dataTable = new SimpleBinaryOpTest__DataTable<Int32>(_data1, _data2, new Int32[ElementCount]);
        }

        public bool IsSupported => Avx2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario()
        {
            var result = Avx2.Add(
                Unsafe.Read<Vector256<Int32>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector256<Int32>>(_dataTable.inArray2Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1, _dataTable.inArray2, _dataTable.outArray);
        }

        public void RunReflectionScenario()
        {
            var result = typeof(Avx2).GetMethod(nameof(Avx2.Add), new Type[] { typeof(Vector256<Int32>), typeof(Vector256<Int32>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<Int32>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector256<Int32>>(_dataTable.inArray2Ptr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Int32>)(result));
            ValidateResult(_dataTable.inArray1, _dataTable.inArray2, _dataTable.outArray);
        }

        public void RunClsVarScenario()
        {
            var result = Avx2.Add(
                _clsVar1,
                _clsVar2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArray);
        }

        public void RunLclVarScenario()
        {
            var left = Unsafe.Read<Vector256<Int32>>(_dataTable.inArray1Ptr);
            var right = Unsafe.Read<Vector256<Int32>>(_dataTable.inArray2Ptr);
            var result = Avx2.Add(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArray);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleBinaryOpTest__AddInt32();
            var result = Avx2.Add(test._fld1, test._fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArray);
        }

        public void RunFldScenario()
        {
            var result = Avx2.Add(_fld1, _fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _dataTable.outArray);
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

        private void ValidateResult(Vector256<Int32> left, Vector256<Int32> right, Int32[] result, [CallerMemberName] string method = "")
        {
            Int32[] inArray1 = new Int32[ElementCount];
            Int32[] inArray2 = new Int32[ElementCount];

            Unsafe.Write(Unsafe.AsPointer(ref inArray1[0]), left);
            Unsafe.Write(Unsafe.AsPointer(ref inArray2[0]), right);

            ValidateResult(inArray1, inArray2, result, method);
        }

        private void ValidateResult(Int32[] left, Int32[] right, Int32[] result, [CallerMemberName] string method = "")
        {
            if ((int)(left[0] + right[0]) != result[0])
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < left.Length; i++)
                {
                    if ((int)(left[i] + right[i]) != result[i])
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Avx2)}.{nameof(Avx2.Add)}<Int32>: {method} failed:");
                Console.WriteLine($"    left: ({string.Join(", ", left)})");
                Console.WriteLine($"   right: ({string.Join(", ", right)})");
                Console.WriteLine($"  result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
