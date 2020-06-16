
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics.Arm\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        private static void ShiftLeftLogicalAndInsert_Vector128_UInt16()
        {
            var test = new ImmTernaryOpTest__ShiftLeftLogicalAndInsert_Vector128_UInt16();

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

    public sealed unsafe class ImmTernaryOpTest__ShiftLeftLogicalAndInsert_Vector128_UInt16
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

            public DataTable(UInt16[] inArray1, UInt16[] inArray2, Byte immValue, UInt16[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<UInt16>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<UInt16>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<UInt16>();
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
                this.immValue = immValue;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<UInt16, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<UInt16, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);
            public Byte immValue;

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
            public Vector128<UInt16> _fld1;
            public Vector128<UInt16> _fld2;
            public Byte _fldImm;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref testStruct._fld1), ref Unsafe.As<UInt16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref testStruct._fld2), ref Unsafe.As<UInt16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
                testStruct._fldImm = Helpers.Clamp(sizeof(UInt16), TestLibrary.Generator.GetByte(), false);

                return testStruct;
            }

            public void RunStructFldScenario(ImmTernaryOpTest__ShiftLeftLogicalAndInsert_Vector128_UInt16 testClass)
            {
                var result = AdvSimd.ShiftLeftLogicalAndInsert(_fld1, _fld2, _fldImm);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, _fld2, _fldImm, testClass._dataTable.outArrayPtr);
            }

            public void RunStructFldScenario_Load(ImmTernaryOpTest__ShiftLeftLogicalAndInsert_Vector128_UInt16 testClass)
            {
                fixed (Vector128<UInt16>* pFld1 = &_fld1)
                fixed (Vector128<UInt16>* pFld2 = &_fld2)
                {
                    var result = AdvSimd.ShiftLeftLogicalAndInsert(
                        AdvSimd.LoadVector128((UInt16*)(pFld1)),
                        AdvSimd.LoadVector128((UInt16*)(pFld2)),
                        _fldImm
                    );

                    Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                    testClass.ValidateResult(_fld1, _fld2, _fldImm, testClass._dataTable.outArrayPtr);
                }
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);

        private static UInt16[] _data1 = new UInt16[Op1ElementCount];
        private static UInt16[] _data2 = new UInt16[Op2ElementCount];

        private static Vector128<UInt16> _clsVar1;
        private static Vector128<UInt16> _clsVar2;
        private static Byte _clsImm;

        private Vector128<UInt16> _fld1;
        private Vector128<UInt16> _fld2;
        private Byte _fldImm;

        private DataTable _dataTable;

        static ImmTernaryOpTest__ShiftLeftLogicalAndInsert_Vector128_UInt16()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _clsVar1), ref Unsafe.As<UInt16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _clsVar2), ref Unsafe.As<UInt16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
            _clsImm = Helpers.Clamp(sizeof(UInt16), TestLibrary.Generator.GetByte(), false);
        }

        public ImmTernaryOpTest__ShiftLeftLogicalAndInsert_Vector128_UInt16()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _fld1), ref Unsafe.As<UInt16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _fld2), ref Unsafe.As<UInt16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
            _fldImm = Helpers.Clamp(sizeof(UInt16), TestLibrary.Generator.GetByte(), false);
            _dataTable = new DataTable(_data1, _data2, _fldImm, new UInt16[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = AdvSimd.ShiftLeftLogicalAndInsert(
                Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray2Ptr),
                _dataTable.immValue
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.immValue, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = AdvSimd.ShiftLeftLogicalAndInsert(
                AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray1Ptr)),
                AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray2Ptr)),
                _dataTable.immValue
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.immValue, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(AdvSimd).GetMethod(nameof(AdvSimd.ShiftLeftLogicalAndInsert), new Type[] { typeof(Vector128<UInt16>), typeof(Vector128<UInt16>), typeof(Byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray2Ptr),
                                        _dataTable.immValue
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.immValue, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(AdvSimd).GetMethod(nameof(AdvSimd.ShiftLeftLogicalAndInsert), new Type[] { typeof(Vector128<UInt16>), typeof(Vector128<UInt16>), typeof(Byte) })
                                     .Invoke(null, new object[] {
                                        AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray1Ptr)),
                                        AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray2Ptr)),
                                        _dataTable.immValue
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.immValue, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = AdvSimd.ShiftLeftLogicalAndInsert(
                _clsVar1,
                _clsVar2,
                _fldImm
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _fldImm, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector128<UInt16>* pClsVar1 = &_clsVar1)
            fixed (Vector128<UInt16>* pClsVar2 = &_clsVar2)
            {
                var result = AdvSimd.ShiftLeftLogicalAndInsert(
                    AdvSimd.LoadVector128((UInt16*)(pClsVar1)),
                    AdvSimd.LoadVector128((UInt16*)(pClsVar2)),
                    _fldImm
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_clsVar1, _clsVar2, _fldImm, _dataTable.outArrayPtr);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray1Ptr);
            var op2 = Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray2Ptr);
            var result = AdvSimd.ShiftLeftLogicalAndInsert(op1, op2, _dataTable.immValue);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, _dataTable.immValue, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var op1 = AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray1Ptr));
            var op2 = AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray2Ptr));
            var result = AdvSimd.ShiftLeftLogicalAndInsert(op1, op2, _dataTable.immValue);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, _dataTable.immValue, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ImmTernaryOpTest__ShiftLeftLogicalAndInsert_Vector128_UInt16();
            var result = AdvSimd.ShiftLeftLogicalAndInsert(test._fld1, test._fld2, test._fldImm);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fldImm, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new ImmTernaryOpTest__ShiftLeftLogicalAndInsert_Vector128_UInt16();

            fixed (Vector128<UInt16>* pFld1 = &test._fld1)
            fixed (Vector128<UInt16>* pFld2 = &test._fld2)
            {
                var result = AdvSimd.ShiftLeftLogicalAndInsert(
                    AdvSimd.LoadVector128((UInt16*)(pFld1)),
                    AdvSimd.LoadVector128((UInt16*)(pFld2)),
                    test._fldImm
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(test._fld1, test._fld2, test._fldImm, _dataTable.outArrayPtr);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = AdvSimd.ShiftLeftLogicalAndInsert(_fld1, _fld2, _fldImm);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _fldImm, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector128<UInt16>* pFld1 = &_fld1)
            fixed (Vector128<UInt16>* pFld2 = &_fld2)
            {
                var result = AdvSimd.ShiftLeftLogicalAndInsert(
                    AdvSimd.LoadVector128((UInt16*)(pFld1)),
                    AdvSimd.LoadVector128((UInt16*)(pFld2)),
                    _fldImm
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_fld1, _fld2, _fldImm, _dataTable.outArrayPtr);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = AdvSimd.ShiftLeftLogicalAndInsert(test._fld1, test._fld2, test._fldImm);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fldImm, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            var result = AdvSimd.ShiftLeftLogicalAndInsert(
                AdvSimd.LoadVector128((UInt16*)(&test._fld1)),
                AdvSimd.LoadVector128((UInt16*)(&test._fld2)),
                test._fldImm
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fldImm, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector128<UInt16> op1, Vector128<UInt16> op2, Byte imm, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray1 = new UInt16[Op1ElementCount];
            UInt16[] inArray2 = new UInt16[Op2ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray1[0]), op1);
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray2[0]), op2);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            ValidateResult(inArray1, inArray2, imm, outArray, method);
        }

        private void ValidateResult(void* op1, void* op2, Byte imm, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray1 = new UInt16[Op1ElementCount];
            UInt16[] inArray2 = new UInt16[Op2ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(op2), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            ValidateResult(inArray1, inArray2, imm, outArray, method);
        }

        private void ValidateResult(UInt16[] left, UInt16[] right, Byte imm, UInt16[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (var i = 0; i < RetElementCount; i++)
            {
                if (Helpers.ShiftLeftLogicalAndInsert(left[i], right[i], imm) != result[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd)}.{nameof(AdvSimd.ShiftLeftLogicalAndInsert)}<UInt16>(Vector128<UInt16>, Vector128<UInt16>, Byte): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"    left: ({string.Join(", ", left)})");
                TestLibrary.TestFramework.LogInformation($"   right: ({string.Join(", ", right)})");
                TestLibrary.TestFramework.LogInformation($"     imm: ({imm})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
