// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\X86\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["MultipleSumAbsoluteDifferences"] = MultipleSumAbsoluteDifferences
            };
        }

        private static void MultipleSumAbsoluteDifferences()
        {
            var test = new SimpleBinaryOpTest__MultipleSumAbsoluteDifferences();

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

    public sealed unsafe class SimpleBinaryOpTest__MultipleSumAbsoluteDifferences
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<Byte>>() / sizeof(Byte);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<Byte>>() / sizeof(Byte);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);

        private static Byte[] _data1 = new Byte[Op1ElementCount];
        private static Byte[] _data2 = new Byte[Op2ElementCount];

        private static Vector128<Byte> _clsVar1;
        private static Vector128<Byte> _clsVar2;

        private Vector128<Byte> _fld1;
        private Vector128<Byte> _fld2;

        private SimpleBinaryOpTest__DataTable<UInt16, Byte, Byte> _dataTable;

        static SimpleBinaryOpTest__MultipleSumAbsoluteDifferences()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (byte)(random.Next(0, byte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _clsVar1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (byte)(random.Next(0, byte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _clsVar2), ref Unsafe.As<Byte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());
        }

        public SimpleBinaryOpTest__MultipleSumAbsoluteDifferences()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (byte)(random.Next(0, byte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _fld1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (byte)(random.Next(0, byte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _fld2), ref Unsafe.As<Byte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (byte)(random.Next(0, byte.MaxValue)); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (byte)(random.Next(0, byte.MaxValue)); }
            _dataTable = new SimpleBinaryOpTest__DataTable<UInt16, Byte, Byte>(_data1, _data2, new UInt16[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse41.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            const byte imm8 = 0;
            
            var result = Sse41.MultipleSumAbsoluteDifferences(
                Unsafe.Read<Vector128<Byte>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Byte>>(_dataTable.inArray2Ptr),
                imm8
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, imm8, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            const byte imm8 = 1;
            
            var result = Sse41.MultipleSumAbsoluteDifferences(
                Sse2.LoadVector128((Byte*)(_dataTable.inArray1Ptr)),
                Sse2.LoadVector128((Byte*)(_dataTable.inArray2Ptr)),
                imm8
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, imm8, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            const byte imm8 = 2;

            var result = Sse41.MultipleSumAbsoluteDifferences(
                Sse2.LoadAlignedVector128((Byte*)(_dataTable.inArray1Ptr)),
                Sse2.LoadAlignedVector128((Byte*)(_dataTable.inArray2Ptr)),
                imm8
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, imm8, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            const byte imm8 = 3;

            var result = typeof(Sse41).GetMethod(nameof(Sse41.MultipleSumAbsoluteDifferences), new Type[] { typeof(Vector128<Byte>), typeof(Vector128<Byte>), typeof(Byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Byte>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<Byte>>(_dataTable.inArray2Ptr),
                                        imm8
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, imm8, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            const byte imm8 = 4;

            var result = typeof(Sse41).GetMethod(nameof(Sse41.MultipleSumAbsoluteDifferences), new Type[] { typeof(Vector128<Byte>), typeof(Vector128<Byte>), typeof(Byte) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((Byte*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadVector128((Byte*)(_dataTable.inArray2Ptr)),
                                        imm8
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, imm8, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            const byte imm8 = 5;

            var result = typeof(Sse41).GetMethod(nameof(Sse41.MultipleSumAbsoluteDifferences), new Type[] { typeof(Vector128<Byte>), typeof(Vector128<Byte>), typeof(Byte) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((Byte*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadAlignedVector128((Byte*)(_dataTable.inArray2Ptr)),
                                        imm8
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, imm8, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            const byte imm8 = 6;

            var result = Sse41.MultipleSumAbsoluteDifferences(
                _clsVar1,
                _clsVar2,
                imm8
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, imm8, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            const byte imm8 = 7;

            var left = Unsafe.Read<Vector128<Byte>>(_dataTable.inArray1Ptr);
            var right = Unsafe.Read<Vector128<Byte>>(_dataTable.inArray2Ptr);
            var result = Sse41.MultipleSumAbsoluteDifferences(left, right, imm8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, imm8, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            const byte imm8 = 8;

            var left = Sse2.LoadVector128((Byte*)(_dataTable.inArray1Ptr));
            var right = Sse2.LoadVector128((Byte*)(_dataTable.inArray2Ptr));
            var result = Sse41.MultipleSumAbsoluteDifferences(left, right, imm8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, imm8, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            const byte imm8 = 9;

            var left = Sse2.LoadAlignedVector128((Byte*)(_dataTable.inArray1Ptr));
            var right = Sse2.LoadAlignedVector128((Byte*)(_dataTable.inArray2Ptr));
            var result = Sse41.MultipleSumAbsoluteDifferences(left, right, imm8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, imm8, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            const byte imm8 = 10;

            var test = new SimpleBinaryOpTest__MultipleSumAbsoluteDifferences();
            var result = Sse41.MultipleSumAbsoluteDifferences(test._fld1, test._fld2, imm8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, imm8, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            const byte imm8 = 11;

            var result = Sse41.MultipleSumAbsoluteDifferences(_fld1, _fld2, imm8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, imm8, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector128<Byte> left, Vector128<Byte> right, byte imm8, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray1 = new Byte[Op1ElementCount];
            Byte[] inArray2 = new Byte[Op2ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref inArray1[0]), left);
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref inArray2[0]), right);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            ValidateResult(inArray1, inArray2, imm8, outArray, method);
        }

        private void ValidateResult(void* left, void* right, byte imm8, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray1 = new Byte[Op1ElementCount];
            Byte[] inArray2 = new Byte[Op2ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(left), (uint)Unsafe.SizeOf<Vector128<Byte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(right), (uint)Unsafe.SizeOf<Vector128<Byte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            ValidateResult(inArray1, inArray2, imm8, outArray, method);
        }

        private void ValidateResult(Byte[] left, Byte[] right, byte imm8, UInt16[] result, [CallerMemberName] string method = "")
        {
            var srcOffset = ((imm8 & 0x3) * 32) / 8;
            var dstOffset = (((imm8 & 0x4) >> 2) * 32) / 8;

            if (result[0] != Math.Abs(left[dstOffset + 0] - right[srcOffset + 0]) +
                             Math.Abs(left[dstOffset + 1] - right[srcOffset + 1]) +
                             Math.Abs(left[dstOffset + 2] - right[srcOffset + 2]) +
                             Math.Abs(left[dstOffset + 3] - right[srcOffset + 3]))
            {
                Succeeded = false;
            }
            else if (result[1] != Math.Abs(left[dstOffset + 1] - right[srcOffset + 0]) +
                                  Math.Abs(left[dstOffset + 2] - right[srcOffset + 1]) +
                                  Math.Abs(left[dstOffset + 3] - right[srcOffset + 2]) +
                                  Math.Abs(left[dstOffset + 4] - right[srcOffset + 3]))
            {
                Succeeded = false;
            }
            else if (result[2] != Math.Abs(left[dstOffset + 2] - right[srcOffset + 0]) +
                                  Math.Abs(left[dstOffset + 3] - right[srcOffset + 1]) +
                                  Math.Abs(left[dstOffset + 4] - right[srcOffset + 2]) +
                                  Math.Abs(left[dstOffset + 5] - right[srcOffset + 3]))
            {
                Succeeded = false;
            }
            else if (result[3] != Math.Abs(left[dstOffset + 3] - right[srcOffset + 0]) +
                                  Math.Abs(left[dstOffset + 4] - right[srcOffset + 1]) +
                                  Math.Abs(left[dstOffset + 5] - right[srcOffset + 2]) +
                                  Math.Abs(left[dstOffset + 6] - right[srcOffset + 3]))
            {
                Succeeded = false;
            }
            else if (result[4] != Math.Abs(left[dstOffset + 4] - right[srcOffset + 0]) +
                                  Math.Abs(left[dstOffset + 5] - right[srcOffset + 1]) +
                                  Math.Abs(left[dstOffset + 6] - right[srcOffset + 2]) +
                                  Math.Abs(left[dstOffset + 7] - right[srcOffset + 3]))
            {
                Succeeded = false;
            }
            else if (result[5] != Math.Abs(left[dstOffset + 5] - right[srcOffset + 0]) +
                                  Math.Abs(left[dstOffset + 6] - right[srcOffset + 1]) +
                                  Math.Abs(left[dstOffset + 7] - right[srcOffset + 2]) +
                                  Math.Abs(left[dstOffset + 8] - right[srcOffset + 3]))
            {
                Succeeded = false;
            }
            else if (result[6] != Math.Abs(left[dstOffset + 6] - right[srcOffset + 0]) +
                                  Math.Abs(left[dstOffset + 7] - right[srcOffset + 1]) +
                                  Math.Abs(left[dstOffset + 8] - right[srcOffset + 2]) +
                                  Math.Abs(left[dstOffset + 9] - right[srcOffset + 3]))
            {
                Succeeded = false;
            }
            else if (result[7] != Math.Abs(left[dstOffset + 7] - right[srcOffset + 0]) +
                                  Math.Abs(left[dstOffset + 8] - right[srcOffset + 1]) +
                                  Math.Abs(left[dstOffset + 9] - right[srcOffset + 2]) +
                                  Math.Abs(left[dstOffset + 10] - right[srcOffset + 3]))
            {
                Succeeded = false;
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Sse41)}.{nameof(Sse41.MultipleSumAbsoluteDifferences)}Vector128<UInt16>(Vector128<Byte>, Vector128<Byte>, Byte): {method} failed:");
                Console.WriteLine($"    left: ({string.Join(", ", left)})");
                Console.WriteLine($"   right: ({string.Join(", ", right)})");
                Console.WriteLine($"    imm8: ({imm8})");
                Console.WriteLine($"  result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
