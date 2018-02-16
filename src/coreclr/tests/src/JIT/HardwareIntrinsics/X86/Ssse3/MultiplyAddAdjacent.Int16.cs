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
        private static void MultiplyAddAdjacentInt16()
        {
            var test = new SimpleBinaryOpTest__MultiplyAddAdjacentInt16();

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

    public sealed unsafe class SimpleBinaryOpTest__MultiplyAddAdjacentInt16
    {
        private const int VectorSize = 16;

        private const int Op1ElementCount = VectorSize / sizeof(Byte);
        private const int Op2ElementCount = VectorSize / sizeof(SByte);
        private const int RetElementCount = VectorSize / sizeof(Int16);

        private static Byte[] _data1 = new Byte[Op1ElementCount];
        private static SByte[] _data2 = new SByte[Op2ElementCount];

        private static Vector128<Byte> _clsVar1;
        private static Vector128<SByte> _clsVar2;

        private Vector128<Byte> _fld1;
        private Vector128<SByte> _fld2;

        private SimpleBinaryOpTest__DataTable<Int16, Byte, SByte> _dataTable;

        static SimpleBinaryOpTest__MultiplyAddAdjacentInt16()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (byte)(random.Next(0, byte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _clsVar1), ref Unsafe.As<Byte, byte>(ref _data1[0]), VectorSize);
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (sbyte)(random.Next(sbyte.MinValue, sbyte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _clsVar2), ref Unsafe.As<SByte, byte>(ref _data2[0]), VectorSize);
        }

        public SimpleBinaryOpTest__MultiplyAddAdjacentInt16()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (byte)(random.Next(0, byte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _fld1), ref Unsafe.As<Byte, byte>(ref _data1[0]), VectorSize);
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (sbyte)(random.Next(sbyte.MinValue, sbyte.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _fld2), ref Unsafe.As<SByte, byte>(ref _data2[0]), VectorSize);

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = (byte)(random.Next(0, byte.MaxValue)); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (sbyte)(random.Next(sbyte.MinValue, sbyte.MaxValue)); }
            _dataTable = new SimpleBinaryOpTest__DataTable<Int16, Byte, SByte>(_data1, _data2, new Int16[RetElementCount], VectorSize);
        }

        public bool IsSupported => Ssse3.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Ssse3.MultiplyAddAdjacent(
                Unsafe.Read<Vector128<Byte>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<SByte>>(_dataTable.inArray2Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Ssse3.MultiplyAddAdjacent(
                Sse2.LoadVector128((Byte*)(_dataTable.inArray1Ptr)),
                Sse2.LoadVector128((SByte*)(_dataTable.inArray2Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Ssse3.MultiplyAddAdjacent(
                Sse2.LoadAlignedVector128((Byte*)(_dataTable.inArray1Ptr)),
                Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray2Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Ssse3).GetMethod(nameof(Ssse3.MultiplyAddAdjacent), new Type[] { typeof(Vector128<Byte>), typeof(Vector128<SByte>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Byte>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<SByte>>(_dataTable.inArray2Ptr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Ssse3).GetMethod(nameof(Ssse3.MultiplyAddAdjacent), new Type[] { typeof(Vector128<Byte>), typeof(Vector128<SByte>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((Byte*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadVector128((SByte*)(_dataTable.inArray2Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Ssse3).GetMethod(nameof(Ssse3.MultiplyAddAdjacent), new Type[] { typeof(Vector128<Byte>), typeof(Vector128<SByte>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((Byte*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray2Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Ssse3.MultiplyAddAdjacent(
                _clsVar1,
                _clsVar2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var left = Unsafe.Read<Vector128<Byte>>(_dataTable.inArray1Ptr);
            var right = Unsafe.Read<Vector128<SByte>>(_dataTable.inArray2Ptr);
            var result = Ssse3.MultiplyAddAdjacent(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var left = Sse2.LoadVector128((Byte*)(_dataTable.inArray1Ptr));
            var right = Sse2.LoadVector128((SByte*)(_dataTable.inArray2Ptr));
            var result = Ssse3.MultiplyAddAdjacent(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var left = Sse2.LoadAlignedVector128((Byte*)(_dataTable.inArray1Ptr));
            var right = Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray2Ptr));
            var result = Ssse3.MultiplyAddAdjacent(left, right);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclFldScenario()
        {
            var test = new SimpleBinaryOpTest__MultiplyAddAdjacentInt16();
            var result = Ssse3.MultiplyAddAdjacent(test._fld1, test._fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunFldScenario()
        {
            var result = Ssse3.MultiplyAddAdjacent(_fld1, _fld2);

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

        private void ValidateResult(Vector128<Byte> left, Vector128<SByte> right, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray1 = new Byte[Op1ElementCount];
            SByte[] inArray2 = new SByte[Op2ElementCount];
            Int16[] outArray = new Int16[RetElementCount];

            Unsafe.Write(Unsafe.AsPointer(ref inArray1[0]), left);
            Unsafe.Write(Unsafe.AsPointer(ref inArray2[0]), right);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* left, void* right, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray1 = new Byte[Op1ElementCount];
            SByte[] inArray2 = new SByte[Op2ElementCount];
            Int16[] outArray = new Int16[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(left), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(right), VectorSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), VectorSize);

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(Byte[] left, SByte[] right, Int16[] result, [CallerMemberName] string method = "")
        {
            if (result[0] != Math.Clamp(((right[1] * left[1]) + (right[0] * left[0])), short.MinValue, short.MaxValue))
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (result[i] != Math.Clamp(((right[(i * 2) + 1] * left[(i * 2) + 1]) + (right[i * 2] * left[i * 2])), short.MinValue, short.MaxValue))
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                Console.WriteLine($"{nameof(Ssse3)}.{nameof(Ssse3.MultiplyAddAdjacent)}<Int16>(Vector128<Byte>, Vector128<SByte>): {method} failed:");
                Console.WriteLine($"    left: ({string.Join(", ", left)})");
                Console.WriteLine($"   right: ({string.Join(", ", right)})");
                Console.WriteLine($"  result: ({string.Join(", ", result)})");
                Console.WriteLine();
            }
        }
    }
}
