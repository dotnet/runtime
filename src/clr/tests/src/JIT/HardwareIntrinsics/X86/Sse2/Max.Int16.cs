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
        private static void MaxInt16()
        {
            var test = new SimpleBinaryOpTest__MaxInt16();

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

    public sealed unsafe class SimpleBinaryOpTest__MaxInt16
    {
        private const int VectorSize = 16;
        private const int ElementCount = VectorSize / sizeof(Int16);

        private static Int16[] _data1 = new Int16[ElementCount];
        private static Int16[] _data2 = new Int16[ElementCount];

        private static Vector128<Int16> _clsVar1;
        private static Vector128<Int16> _clsVar2;

        private Vector128<Int16> _fld1;
        private Vector128<Int16> _fld2;

        private SimpleBinaryOpTest__DataTable<Int16> _dataTable;

        static SimpleBinaryOpTest__MaxInt16()
        {
            var random = new Random();

            for (var i = 0; i < ElementCount; i++) { _data1[i] = (short)(random.Next(short.MinValue, short.MaxValue)); _data2[i] = (short)(random.Next(short.MinValue, short.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref _clsVar1), ref Unsafe.As<Int16, byte>(ref _data2[0]), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref _clsVar2), ref Unsafe.As<Int16, byte>(ref _data1[0]), VectorSize);
        }

        public SimpleBinaryOpTest__MaxInt16()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < ElementCount; i++) { _data1[i] = (short)(random.Next(short.MinValue, short.MaxValue)); _data2[i] = (short)(random.Next(short.MinValue, short.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref _fld1), ref Unsafe.As<Int16, byte>(ref _data1[0]), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref _fld2), ref Unsafe.As<Int16, byte>(ref _data2[0]), VectorSize);

            for (var i = 0; i < ElementCount; i++) { _data1[i] = (short)(random.Next(short.MinValue, short.MaxValue)); _data2[i] = (short)(random.Next(short.MinValue, short.MaxValue)); }
            _dataTable = new SimpleBinaryOpTest__DataTable<Int16>(_data1, _data2, new Int16[ElementCount], VectorSize);
        }

        public bool IsSupported => Sse2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Sse2.Max(
                Unsafe.Read<Vector128<Int16>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Int16>>(_dataTable.inArray2Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Sse2.Max(
                Sse2.LoadVector128((Int16*)(_dataTable.inArray1Ptr)),
                Sse2.LoadVector128((Int16*)(_dataTable.inArray2Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Sse2.Max(
                Sse2.LoadAlignedVector128((Int16*)(_dataTable.inArray1Ptr)),
                Sse2.LoadAlignedVector128((Int16*)(_dataTable.inArray2Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.Max), new Type[] { typeof(Vector128<Int16>), typeof(Vector128<Int16>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Int16>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<Int16>>(_dataTable.inArray2Ptr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.Max), new Type[] { typeof(Vector128<Int16>), typeof(Vector128<Int16>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((Int16*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadVector128((Int16*)(_dataTable.inArray2Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.Max), new Type[] { typeof(Vector128<Int16>), typeof(Vector128<Int16>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((Int16*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadAlignedVector128((Int16*)(_dataTable.inArray2Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Sse2.Max(
                _clsVar1,
                _clsVar2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var left = Unsafe.Read<Vector128<Int16>>(_dataTable.inArray1Ptr);
            var right = Unsafe.Read<Vector128<Int16>>(_dataTable.inArray2Ptr);
            var result = Sse2.Max(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var left = Sse2.LoadVector128((Int16*)(_dataTable.inArray1Ptr));
            var right = Sse2.LoadVector128((Int16*)(_dataTable.inArray2Ptr));
            var result = Sse2.Max(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var left = Sse2.LoadAlignedVector128((Int16*)(_dataTable.inArray1Ptr));
            var right = Sse2.LoadAlignedVector128((Int16*)(_dataTable.inArray2Ptr));
            var result = Sse2.Max(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleBinaryOpTest__MaxInt16();
            var result = Sse2.Max(test._fld1, test._fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Sse2.Max(_fld1, _fld2);

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

        private void ValidateResult(Vector128<Int16> left, Vector128<Int16> right, void* result, [CallerMemberName] string method = "")
        {
            Int16[] inArray1 = new Int16[ElementCount];
            Int16[] inArray2 = new Int16[ElementCount];
            Int16[] outArray = new Int16[ElementCount];

            Unsafe.Write(Unsafe.AsPointer(ref inArray1[0]), left);
            Unsafe.Write(Unsafe.AsPointer(ref inArray2[0]), right);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* left, void* right, void* result, [CallerMemberName] string method = "")
        {
            Int16[] inArray1 = new Int16[ElementCount];
            Int16[] inArray2 = new Int16[ElementCount];
            Int16[] outArray = new Int16[ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(left), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(right), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(Int16[] left, Int16[] right, Int16[] result, [CallerMemberName] string method = "")
        {
            if (Math.Max(left[0], right[0]) != result[0])
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < left.Length; i++)
                {
                    if (Math.Max(left[i], right[i]) != result[i])
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.Max)}<Int16>: {method} failed:");
                Console.WriteLine($"    left: ({string.Join(", ", left)})");
                Console.WriteLine($"   right: ({string.Join(", ", right)})");
                Console.WriteLine($"  result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
