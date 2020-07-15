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
        private static void StorePairNonTemporal_Vector128_Byte()
        {
            var test = new StoreBinaryOpTest__StorePairNonTemporal_Vector128_Byte();

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

    public sealed unsafe class StoreBinaryOpTest__StorePairNonTemporal_Vector128_Byte
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

            public DataTable(Byte[] inArray1, Byte[] inArray2, Byte[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Byte>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<Byte>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<Byte>();
                if ((alignment != 16 && alignment != 32) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2 || (alignment * 2) < sizeOfoutArray)
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
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<Byte, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
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
            public Vector128<Byte> _fld1;
            public Vector128<Byte> _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref testStruct._fld1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref testStruct._fld2), ref Unsafe.As<Byte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());

                return testStruct;
            }

            public void RunStructFldScenario(StoreBinaryOpTest__StorePairNonTemporal_Vector128_Byte testClass)
            {
                AdvSimd.Arm64.StorePairNonTemporal((Byte*)testClass._dataTable.outArrayPtr, _fld1, _fld2);

                testClass.ValidateResult(_fld1, _fld2, testClass._dataTable.outArrayPtr);
            }

            public void RunStructFldScenario_Load(StoreBinaryOpTest__StorePairNonTemporal_Vector128_Byte testClass)
            {
                fixed (Vector128<Byte>* pFld1 = &_fld1)
                fixed (Vector128<Byte>* pFld2 = &_fld2)
                {
                    AdvSimd.Arm64.StorePairNonTemporal(
                        (Byte*)testClass._dataTable.outArrayPtr,
                        AdvSimd.LoadVector128((Byte*)(pFld1)),
                        AdvSimd.LoadVector128((Byte*)(pFld2))
                    );

                    testClass.ValidateResult(_fld1, _fld2, testClass._dataTable.outArrayPtr);
                }
            }
        }

        private static readonly int LargestVectorSize = 32;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<Byte>>() / sizeof(Byte);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<Byte>>() / sizeof(Byte);
        private static readonly int RetElementCount = Op1ElementCount + Op2ElementCount;

        private static Byte[] _data1 = new Byte[Op1ElementCount];
        private static Byte[] _data2 = new Byte[Op2ElementCount];

        private static Vector128<Byte> _clsVar1;
        private static Vector128<Byte> _clsVar2;

        private Vector128<Byte> _fld1;
        private Vector128<Byte> _fld2;

        private DataTable _dataTable;

        static StoreBinaryOpTest__StorePairNonTemporal_Vector128_Byte()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _clsVar1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _clsVar2), ref Unsafe.As<Byte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());
        }

        public StoreBinaryOpTest__StorePairNonTemporal_Vector128_Byte()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _fld1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _fld2), ref Unsafe.As<Byte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
            _dataTable = new DataTable(_data1, _data2, new Byte[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.Arm64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            AdvSimd.Arm64.StorePairNonTemporal(
                (Byte*)_dataTable.outArrayPtr,
                Unsafe.Read<Vector128<Byte>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Byte>>(_dataTable.inArray2Ptr)
            );

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            AdvSimd.Arm64.StorePairNonTemporal(
                (Byte*)_dataTable.outArrayPtr,
                AdvSimd.LoadVector128((Byte*)(_dataTable.inArray1Ptr)),
                AdvSimd.LoadVector128((Byte*)(_dataTable.inArray2Ptr))
            );

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            typeof(AdvSimd.Arm64).GetMethod(nameof(AdvSimd.Arm64.StorePairNonTemporal), new Type[] { typeof(Byte*), typeof(Vector128<Byte>), typeof(Vector128<Byte>) })
                         .Invoke(null, new object[] {
                                 Pointer.Box(_dataTable.outArrayPtr, typeof(Byte*)),
                                 Unsafe.Read<Vector128<Byte>>(_dataTable.inArray1Ptr),
                                 Unsafe.Read<Vector128<Byte>>(_dataTable.inArray2Ptr) });

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            typeof(AdvSimd.Arm64).GetMethod(nameof(AdvSimd.Arm64.StorePairNonTemporal), new Type[] { typeof(Byte*), typeof(Vector128<Byte>), typeof(Vector128<Byte>) })
                                     .Invoke(null, new object[] {
                                        Pointer.Box(_dataTable.outArrayPtr, typeof(Byte*)),
                                        AdvSimd.LoadVector128((Byte*)(_dataTable.inArray1Ptr)),
                                        AdvSimd.LoadVector128((Byte*)(_dataTable.inArray2Ptr))
                                     });

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            AdvSimd.Arm64.StorePairNonTemporal((Byte*)_dataTable.outArrayPtr, _clsVar1, _clsVar2);

            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector128<Byte>* pClsVar1 = &_clsVar1)
            fixed (Vector128<Byte>* pClsVar2 = &_clsVar2)
            {
                AdvSimd.Arm64.StorePairNonTemporal(
                    (Byte*)_dataTable.outArrayPtr,
                    AdvSimd.LoadVector128((Byte*)(pClsVar1)),
                    AdvSimd.LoadVector128((Byte*)(pClsVar2))
                );

                ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector128<Byte>>(_dataTable.inArray1Ptr);
            var op2 = Unsafe.Read<Vector128<Byte>>(_dataTable.inArray2Ptr);
            AdvSimd.Arm64.StorePairNonTemporal((Byte*)_dataTable.outArrayPtr, op1, op2);

            ValidateResult(op1, op2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var op1 = AdvSimd.LoadVector128((Byte*)(_dataTable.inArray1Ptr));
            var op2 = AdvSimd.LoadVector128((Byte*)(_dataTable.inArray2Ptr));
            AdvSimd.Arm64.StorePairNonTemporal((Byte*)_dataTable.outArrayPtr, op1, op2);

            ValidateResult(op1, op2, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new StoreBinaryOpTest__StorePairNonTemporal_Vector128_Byte();
            AdvSimd.Arm64.StorePairNonTemporal((Byte*)_dataTable.outArrayPtr, test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new StoreBinaryOpTest__StorePairNonTemporal_Vector128_Byte();

            fixed (Vector128<Byte>* pFld1 = &test._fld1)
            fixed (Vector128<Byte>* pFld2 = &test._fld2)
            {
                AdvSimd.Arm64.StorePairNonTemporal(
                    (Byte*)_dataTable.outArrayPtr,
                    AdvSimd.LoadVector128((Byte*)(pFld1)),
                    AdvSimd.LoadVector128((Byte*)(pFld2))
                );

                ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            AdvSimd.Arm64.StorePairNonTemporal((Byte*)_dataTable.outArrayPtr, _fld1, _fld2);

            ValidateResult(_fld1, _fld2, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector128<Byte>* pFld1 = &_fld1)
            fixed (Vector128<Byte>* pFld2 = &_fld2)
            {
                AdvSimd.Arm64.StorePairNonTemporal(
                    (Byte*)_dataTable.outArrayPtr,
                    AdvSimd.LoadVector128((Byte*)(pFld1)),
                    AdvSimd.LoadVector128((Byte*)(pFld2))
                );

                ValidateResult(_fld1, _fld2, _dataTable.outArrayPtr);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            AdvSimd.Arm64.StorePairNonTemporal((Byte*)_dataTable.outArrayPtr, test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            AdvSimd.Arm64.StorePairNonTemporal(
                (Byte*)_dataTable.outArrayPtr,
                AdvSimd.LoadVector128((Byte*)(&test._fld1)),
                AdvSimd.LoadVector128((Byte*)(&test._fld2))
            );

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

        private void ValidateResult(Vector128<Byte> op1, Vector128<Byte> op2, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray1 = new Byte[Op1ElementCount];
            Byte[] inArray2 = new Byte[Op2ElementCount];
            Byte[] outArray = new Byte[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref inArray1[0]), op1);
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref inArray2[0]), op2);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)(RetElementCount * Unsafe.SizeOf<Byte>()));

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* op1, void* op2, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray1 = new Byte[Op1ElementCount];
            Byte[] inArray2 = new Byte[Op2ElementCount];
            Byte[] outArray = new Byte[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector128<Byte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(op2), (uint)Unsafe.SizeOf<Vector128<Byte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)(RetElementCount * Unsafe.SizeOf<Byte>()));

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(Byte[] firstOp, Byte[] secondOp, Byte[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < RetElementCount; i++)
            {
                if (Helpers.Concat(firstOp, secondOp, i) != result[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd.Arm64)}.{nameof(AdvSimd.Arm64.StorePairNonTemporal)}<Byte>(Vector128<Byte>, Vector128<Byte>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($" firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"secondOp: ({string.Join(", ", secondOp)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
