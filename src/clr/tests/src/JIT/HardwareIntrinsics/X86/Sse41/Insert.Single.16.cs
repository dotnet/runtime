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
using static System.Runtime.Intrinsics.X86.Sse;
using static System.Runtime.Intrinsics.X86.Sse2;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void InsertSingle16()
        {
            var test = new SimpleBinaryOpTest__InsertSingle16();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Sse.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (Sse.IsSupported)
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

                if (Sse.IsSupported)
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

    public sealed unsafe class SimpleBinaryOpTest__InsertSingle16
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<Single>>() / sizeof(Single);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<Single>>() / sizeof(Single);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<Single>>() / sizeof(Single);

        private static Single[] _data1 = new Single[Op1ElementCount];
        private static Single[] _data2 = new Single[Op2ElementCount];

        private static Vector128<Single> _clsVar1;
        private static Vector128<Single> _clsVar2;

        private Vector128<Single> _fld1;
        private Vector128<Single> _fld2;

        private SimpleBinaryOpTest__DataTable<Single, Single, Single> _dataTable;

        static SimpleBinaryOpTest__InsertSingle16()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (float)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Single>, byte>(ref _clsVar1), ref Unsafe.As<Single, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Single>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (float)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Single>, byte>(ref _clsVar2), ref Unsafe.As<Single, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Single>>());
        }

        public SimpleBinaryOpTest__InsertSingle16()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (float)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Single>, byte>(ref _fld1), ref Unsafe.As<Single, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Single>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (float)(random.NextDouble()); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Single>, byte>(ref _fld2), ref Unsafe.As<Single, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Single>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (float)(random.NextDouble()); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (float)(random.NextDouble()); }
            _dataTable = new SimpleBinaryOpTest__DataTable<Single, Single, Single>(_data1, _data2, new Single[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse41.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Sse41.Insert(
                Unsafe.Read<Vector128<Single>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Single>>(_dataTable.inArray2Ptr),
                16
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Sse41.Insert(
                Sse.LoadVector128((Single*)(_dataTable.inArray1Ptr)),
                LoadVector128((Single*)(_dataTable.inArray2Ptr)),
                16
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Sse41.Insert(
                Sse.LoadAlignedVector128((Single*)(_dataTable.inArray1Ptr)),
                LoadAlignedVector128((Single*)(_dataTable.inArray2Ptr)),
                16
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.Insert), new Type[] { typeof(Vector128<Single>), typeof(Vector128<Single>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Single>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<Single>>(_dataTable.inArray2Ptr),
                                        (byte)16
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Single>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.Insert), new Type[] { typeof(Vector128<Single>), typeof(Vector128<Single>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Sse.LoadVector128((Single*)(_dataTable.inArray1Ptr)),
                                        LoadVector128((Single*)(_dataTable.inArray2Ptr)),
                                        (byte)16
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Single>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.Insert), new Type[] { typeof(Vector128<Single>), typeof(Vector128<Single>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Sse.LoadAlignedVector128((Single*)(_dataTable.inArray1Ptr)),
                                        LoadAlignedVector128((Single*)(_dataTable.inArray2Ptr)),
                                        (byte)16
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Single>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Sse41.Insert(
                _clsVar1,
                _clsVar2,
                16
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var left = Unsafe.Read<Vector128<Single>>(_dataTable.inArray1Ptr);
            var right = Unsafe.Read<Vector128<Single>>(_dataTable.inArray2Ptr);
            var result = Sse41.Insert(left, right, 16);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var left = Sse.LoadVector128((Single*)(_dataTable.inArray1Ptr));
            var right = LoadVector128((Single*)(_dataTable.inArray2Ptr));
            var result = Sse41.Insert(left, right, 16);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var left = Sse.LoadAlignedVector128((Single*)(_dataTable.inArray1Ptr));
            var right = LoadAlignedVector128((Single*)(_dataTable.inArray2Ptr));
            var result = Sse41.Insert(left, right, 16);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleBinaryOpTest__InsertSingle16();
            var result = Sse41.Insert(test._fld1, test._fld2, 16);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Sse41.Insert(_fld1, _fld2, 16);

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

        private void ValidateResult(Vector128<Single> left, Vector128<Single> right, void* result, [CallerMemberName] string method = "")
        {
            Single[] inArray1 = new Single[Op1ElementCount];
            Single[] inArray2 = new Single[Op2ElementCount];
            Single[] outArray = new Single[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref inArray1[0]), left);
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref inArray2[0]), right);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Single>>());

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* left, void* right, void* result, [CallerMemberName] string method = "")
        {
            Single[] inArray1 = new Single[Op1ElementCount];
            Single[] inArray2 = new Single[Op2ElementCount];
            Single[] outArray = new Single[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(left), (uint)Unsafe.SizeOf<Vector128<Single>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(right), (uint)Unsafe.SizeOf<Vector128<Single>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Single>>());

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(Single[] left, Single[] right, Single[] result, [CallerMemberName] string method = "")
        {
            if (BitConverter.SingleToInt32Bits(result[0]) != BitConverter.SingleToInt32Bits(left[0]))
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (i == 1 ? BitConverter.SingleToInt32Bits(result[i]) != BitConverter.SingleToInt32Bits(right[0]) : BitConverter.SingleToInt32Bits(result[i]) != BitConverter.SingleToInt32Bits(left[i]))
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Sse41)}.{nameof(Sse41.Insert)}<Single>(Vector128<Single>, Vector128<Single>.16): {method} failed:");
                Console.WriteLine($"    left: ({string.Join(", ", left)})");
                Console.WriteLine($"   right: ({string.Join(", ", right)})");
                Console.WriteLine($"  result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
