// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics.Arm\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        private static void StoreSelectedScalar_Vector128_UInt16_7()
        {
            var test = new StoreSelectedScalarTest__StoreSelectedScalar_Vector128_UInt16_7();

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

    public sealed unsafe class StoreSelectedScalarTest__StoreSelectedScalar_Vector128_UInt16_7
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] outArray;

            private GCHandle inHandle1;
            private GCHandle outHandle;

            private ulong alignment;

            public DataTable(UInt16[] inArray1, UInt16[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<UInt16>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<UInt16>();
                if ((alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfoutArray)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.outArray = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<UInt16, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
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

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref testStruct._fld1), ref Unsafe.As<UInt16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

                return testStruct;
            }

            public void RunStructFldScenario(StoreSelectedScalarTest__StoreSelectedScalar_Vector128_UInt16_7 testClass)
            {
                AdvSimd.StoreSelectedScalar((UInt16*)testClass._dataTable.outArrayPtr, _fld1, 7);

                testClass.ValidateResult(_fld1, testClass._dataTable.outArrayPtr);
            }

            public void RunStructFldScenario_Load(StoreSelectedScalarTest__StoreSelectedScalar_Vector128_UInt16_7 testClass)
            {
                fixed (Vector128<UInt16>* pFld1 = &_fld1)
                {
                    AdvSimd.StoreSelectedScalar((UInt16*)testClass._dataTable.outArrayPtr, AdvSimd.LoadVector128((UInt16*)(pFld1)), 7);

                    testClass.ValidateResult(_fld1, testClass._dataTable.outArrayPtr);
                }
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);
        private static readonly int RetElementCount = 1;
        private static readonly byte ElementIndex = 7;

        private static UInt16[] _data1 = new UInt16[Op1ElementCount];

        private static Vector128<UInt16> _clsVar1;

        private Vector128<UInt16> _fld1;

        private DataTable _dataTable;

        static StoreSelectedScalarTest__StoreSelectedScalar_Vector128_UInt16_7()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _clsVar1), ref Unsafe.As<UInt16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
        }

        public StoreSelectedScalarTest__StoreSelectedScalar_Vector128_UInt16_7()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _fld1), ref Unsafe.As<UInt16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
            _dataTable = new DataTable(_data1, new UInt16[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray1Ptr), 7);

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray1Ptr)), 7);

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            typeof(AdvSimd).GetMethod(nameof(AdvSimd.StoreSelectedScalar), new Type[] { typeof(UInt16*), typeof(Vector128<UInt16>), typeof(byte) })
                         .Invoke(null, new object[] {
                                 Pointer.Box(_dataTable.outArrayPtr, typeof(UInt16*)),
                                 Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray1Ptr),
                                 ElementIndex });

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            typeof(AdvSimd).GetMethod(nameof(AdvSimd.StoreSelectedScalar), new Type[] { typeof(UInt16*), typeof(Vector128<UInt16>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Pointer.Box(_dataTable.outArrayPtr, typeof(UInt16*)),
                                        AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray1Ptr)),
                                        ElementIndex });

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, _clsVar1, 7);

            ValidateResult(_clsVar1, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector128<UInt16>* pClsVar1 = &_clsVar1)
            {
                AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, AdvSimd.LoadVector128((UInt16*)(pClsVar1)), 7);

                ValidateResult(_clsVar1, _dataTable.outArrayPtr);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector128<UInt16>>(_dataTable.inArray1Ptr);
            AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, op1, 7);

            ValidateResult(op1, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var op1 = AdvSimd.LoadVector128((UInt16*)(_dataTable.inArray1Ptr));
            AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, op1, 7);

            ValidateResult(op1, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new StoreSelectedScalarTest__StoreSelectedScalar_Vector128_UInt16_7();
            AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, test._fld1, 7);

            ValidateResult(test._fld1, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new StoreSelectedScalarTest__StoreSelectedScalar_Vector128_UInt16_7();

            fixed (Vector128<UInt16>* pFld1 = &test._fld1)
            {
                AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, AdvSimd.LoadVector128((UInt16*)(pFld1)), 7);

                ValidateResult(test._fld1, _dataTable.outArrayPtr);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, _fld1, 7);

            ValidateResult(_fld1, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector128<UInt16>* pFld1 = &_fld1)
            {
                AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, AdvSimd.LoadVector128((UInt16*)(pFld1)), 7);

                ValidateResult(_fld1, _dataTable.outArrayPtr);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, test._fld1, 7);

            ValidateResult(test._fld1, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            AdvSimd.StoreSelectedScalar((UInt16*)_dataTable.outArrayPtr, AdvSimd.LoadVector128((UInt16*)(&test._fld1)), 7);

            ValidateResult(test._fld1, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector128<UInt16> op1, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray1 = new UInt16[Op1ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray1[0]), op1);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result),(uint)(Unsafe.SizeOf<UInt16>() * RetElementCount));

            ValidateResult(inArray1, outArray[0], method);
        }

        private void ValidateResult(void* op1, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray1 = new UInt16[Op1ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)(Unsafe.SizeOf<UInt16>() * RetElementCount));

            ValidateResult(inArray1, outArray[0], method);
        }

        private void ValidateResult(UInt16[] firstOp, UInt16 result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (firstOp[ElementIndex] != result)
            {
                 succeeded = false;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd)}.{nameof(AdvSimd.StoreSelectedScalar)}<UInt16>(UInt16*, Vector128<UInt16>, 7): {method} failed:");
                TestLibrary.TestFramework.LogInformation($" firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
