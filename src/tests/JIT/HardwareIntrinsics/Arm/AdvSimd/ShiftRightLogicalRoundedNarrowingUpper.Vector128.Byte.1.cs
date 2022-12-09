// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using System.Runtime.Intrinsics.Arm;
using Xunit;

namespace JIT.HardwareIntrinsics.Arm._AdvSimd
{
    public static partial class Program
    {
        [Fact]
        public static void ShiftRightLogicalRoundedNarrowingUpper_Vector128_Byte_1()
        {
            var test = new ImmBinaryOpTest__ShiftRightLogicalRoundedNarrowingUpper_Vector128_Byte_1();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (AdvSimd.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (AdvSimd.IsSupported)
                {
                    // Validates calling via reflection works, using Load
                    test.RunReflectionScenario_Load();
                }

                // Validates passing a static member works
                test.RunClsVarScenario();

                if (AdvSimd.IsSupported)
                {
                    // Validates passing a static member works, using pinning and Load
                    test.RunClsVarScenario_Load();
                }

                // Validates passing a local works, using Unsafe.Read
                test.RunLclVarScenario_UnsafeRead();

                if (AdvSimd.IsSupported)
                {
                    // Validates passing a local works, using Load
                    test.RunLclVarScenario_Load();
                }

                // Validates passing the field of a local class works
                test.RunClassLclFldScenario();

                if (AdvSimd.IsSupported)
                {
                    // Validates passing the field of a local class works, using pinning and Load
                    test.RunClassLclFldScenario_Load();
                }

                // Validates passing an instance member of a class works
                test.RunClassFldScenario();

                if (AdvSimd.IsSupported)
                {
                    // Validates passing an instance member of a class works, using pinning and Load
                    test.RunClassFldScenario_Load();
                }

                // Validates passing the field of a local struct works
                test.RunStructLclFldScenario();

                if (AdvSimd.IsSupported)
                {
                    // Validates passing the field of a local struct works, using pinning and Load
                    test.RunStructLclFldScenario_Load();
                }

                // Validates passing an instance member of a struct works
                test.RunStructFldScenario();

                if (AdvSimd.IsSupported)
                {
                    // Validates passing an instance member of a struct works, using pinning and Load
                    test.RunStructFldScenario_Load();
                }
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

    public sealed unsafe class ImmBinaryOpTest__ShiftRightLogicalRoundedNarrowingUpper_Vector128_Byte_1
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] inArray2;
            private byte[] outArray;

            private GCHandle inHandle1;
            private GCHandle inHandle2;
            private GCHandle outHandle;

            private ulong alignment;

            public DataTable(Byte[] inArray1, UInt16[] inArray2, Byte[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Byte>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<UInt16>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<Byte>();
                if ((alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2 || (alignment * 2) < sizeOfoutArray)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.inArray2 = new byte[alignment * 2];
                this.outArray = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Byte, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<UInt16, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
                outHandle.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private struct TestStruct
        {
            public Vector64<Byte> _fld1;
            public Vector128<UInt16> _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Byte>, byte>(ref testStruct._fld1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Byte>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref testStruct._fld2), ref Unsafe.As<UInt16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

                return testStruct;
            }

            public void RunStructFldScenario(ImmBinaryOpTest__ShiftRightLogicalRoundedNarrowingUpper_Vector128_Byte_1 testClass)
            {
                var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(_fld1, _fld2, 1);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, _fld2, testClass._dataTable.outArrayPtr);
            }

            public void RunStructFldScenario_Load(ImmBinaryOpTest__ShiftRightLogicalRoundedNarrowingUpper_Vector128_Byte_1 testClass)
            {
                fixed (Vector64<Byte>* pFld1 = &_fld1)
                fixed (Vector128<UInt16>* pFld2 = &_fld2)
                {
                    var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                        AdvSimd.LoadVector64((Byte*)(pFld1)),
                        AdvSimd.LoadVector128((UInt16*)(pFld2)),
                        1
                    );

                    Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                    testClass.ValidateResult(_fld1, _fld2, testClass._dataTable.outArrayPtr);
                }
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector64<Byte>>() / sizeof(Byte);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<Byte>>() / sizeof(Byte);
        private static readonly byte Imm = 1;

        private static Byte[] _data1 = new Byte[Op1ElementCount];
        private static UInt16[] _data2 = new UInt16[Op2ElementCount];

        private static Vector64<Byte> _clsVar1;
        private static Vector128<UInt16> _clsVar2;

        private Vector64<Byte> _fld1;
        private Vector128<UInt16> _fld2;

        private DataTable _dataTable;

        static ImmBinaryOpTest__ShiftRightLogicalRoundedNarrowingUpper_Vector128_Byte_1()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Byte>, byte>(ref _clsVar1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Byte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _clsVar2), ref Unsafe.As<UInt16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
        }

        public ImmBinaryOpTest__ShiftRightLogicalRoundedNarrowingUpper_Vector128_Byte_1()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Byte>, byte>(ref _fld1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Byte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _fld2), ref Unsafe.As<UInt16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
            _dataTable = new DataTable(_data1, _data2, new Byte[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                Unsafe.Read<Vector64<Byte>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray2Ptr),
                1
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                AdvSimd.LoadVector64((Byte*)(_dataTable.inArray1Ptr)),
                AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray2Ptr)),
                1
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(AdvSimd).GetMethod(nameof(AdvSimd.ShiftRightLogicalRoundedNarrowingUpper), new Type[] { typeof(Vector64<Byte>), typeof(Vector128<UInt16>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector64<Byte>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray2Ptr),
                                        (byte)1
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Byte>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(AdvSimd).GetMethod(nameof(AdvSimd.ShiftRightLogicalRoundedNarrowingUpper), new Type[] { typeof(Vector64<Byte>), typeof(Vector128<UInt16>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        AdvSimd.LoadVector64((Byte*)(_dataTable.inArray1Ptr)),
                                        AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray2Ptr)),
                                        (byte)1
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Byte>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                _clsVar1,
                _clsVar2,
                1
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector64<Byte>* pClsVar1 = &_clsVar1)
            fixed (Vector128<UInt16>* pClsVar2 = &_clsVar2)
            {
                var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                    AdvSimd.LoadVector64((Byte*)(pClsVar1)),
                    AdvSimd.LoadVector128((UInt16*)(pClsVar2)),
                    1
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector64<Byte>>(_dataTable.inArray1Ptr);
            var op2 = Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray2Ptr);
            var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(op1, op2, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var op1 = AdvSimd.LoadVector64((Byte*)(_dataTable.inArray1Ptr));
            var op2 = AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray2Ptr));
            var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(op1, op2, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ImmBinaryOpTest__ShiftRightLogicalRoundedNarrowingUpper_Vector128_Byte_1();
            var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(test._fld1, test._fld2, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));
            var test = new ImmBinaryOpTest__ShiftRightLogicalRoundedNarrowingUpper_Vector128_Byte_1();

            fixed (Vector64<Byte>* pFld1 = &test._fld1)
            fixed (Vector128<UInt16>* pFld2 = &test._fld2)
            {
                var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                    AdvSimd.LoadVector64((Byte*)(pFld1)),
                    AdvSimd.LoadVector128((UInt16*)(pFld2)),
                    1
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(_fld1, _fld2, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector64<Byte>* pFld1 = &_fld1)
            fixed (Vector128<UInt16>* pFld2 = &_fld2)
            {
                var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                    AdvSimd.LoadVector64((Byte*)(pFld1)),
                    AdvSimd.LoadVector128((UInt16*)(pFld2)),
                    1
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_fld1, _fld2, _dataTable.outArrayPtr);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(test._fld1, test._fld2, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            var result = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                AdvSimd.LoadVector64((Byte*)(&test._fld1)),
                AdvSimd.LoadVector128((UInt16*)(&test._fld2)),
                1
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunStructFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario));

            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        public void RunStructFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario_Load));

            var test = TestStruct.Create();
            test.RunStructFldScenario_Load(this);
        }

        public void RunUnsupportedScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunUnsupportedScenario));

            bool succeeded = false;

            try
            {
                RunBasicScenario_UnsafeRead();
            }
            catch (PlatformNotSupportedException)
            {
                succeeded = true;
            }

            if (!succeeded)
            {
                Succeeded = false;
            }
        }

        private void ValidateResult(Vector64<Byte> firstOp, Vector128<UInt16> secondOp, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray1 = new Byte[Op1ElementCount];
            UInt16[] inArray2 = new UInt16[Op2ElementCount];
            Byte[] outArray = new Byte[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref inArray1[0]), firstOp);
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray2[0]), secondOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Byte>>());

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* secondOp, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray1 = new Byte[Op1ElementCount];
            UInt16[] inArray2 = new UInt16[Op2ElementCount];
            Byte[] outArray = new Byte[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector64<Byte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(secondOp), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Byte>>());

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(Byte[] firstOp, UInt16[] secondOp, Byte[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (var i = 0; i < RetElementCount; i++)
            {
                if (Helpers.ShiftRightLogicalRoundedNarrowingUpper(firstOp, secondOp, Imm, i) != result[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd)}.{nameof(AdvSimd.ShiftRightLogicalRoundedNarrowingUpper)}<Byte>(Vector64<Byte>, Vector128<UInt16>, 1): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"    firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"   secondOp: ({string.Join(", ", secondOp)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
