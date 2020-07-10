// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static void InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3()
        {
            var test = new InsertSelectedScalarTest__InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3();

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

    public sealed unsafe class InsertSelectedScalarTest__InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] inArray3;
            private byte[] outArray;

            private GCHandle inHandle1;
            private GCHandle inHandle3;
            private GCHandle outHandle;

            private ulong alignment;

            public DataTable(Int32[] inArray1, Int32[] inArray3, Int32[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Int32>();
                int sizeOfinArray3 = inArray3.Length * Unsafe.SizeOf<Int32>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<Int32>();
                if ((alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray3 || (alignment * 2) < sizeOfoutArray)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.inArray3 = new byte[alignment * 2];
                this.outArray = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.inHandle3 = GCHandle.Alloc(this.inArray3, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Int32, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray3Ptr), ref Unsafe.As<Int32, byte>(ref inArray3[0]), (uint)sizeOfinArray3);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray3Ptr => Align((byte*)(inHandle3.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                inHandle3.Free();
                outHandle.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private struct TestStruct
        {
            public Vector64<Int32> _fld1;
            public Vector128<Int32> _fld3;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Int32>, byte>(ref testStruct._fld1), ref Unsafe.As<Int32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Int32>>());
                for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetInt32(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int32>, byte>(ref testStruct._fld3), ref Unsafe.As<Int32, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<Int32>>());

                return testStruct;
            }

            public void RunStructFldScenario(InsertSelectedScalarTest__InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3 testClass)
            {
                var result = AdvSimd.Arm64.InsertSelectedScalar(_fld1, 1, _fld3, 3);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, _fld3, testClass._dataTable.outArrayPtr);
            }

            public void RunStructFldScenario_Load(InsertSelectedScalarTest__InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3 testClass)
            {
                fixed (Vector64<Int32>* pFld1 = &_fld1)
                fixed (Vector128<Int32>* pFld2 = &_fld3)
                {
                    var result = AdvSimd.Arm64.InsertSelectedScalar(
                        AdvSimd.LoadVector64((Int32*)pFld1),
                        1,
                        AdvSimd.LoadVector128((Int32*)pFld2),
                        3
                    );

                    Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                    testClass.ValidateResult(_fld1, _fld3, testClass._dataTable.outArrayPtr);
                }
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector64<Int32>>() / sizeof(Int32);
        private static readonly int Op3ElementCount = Unsafe.SizeOf<Vector128<Int32>>() / sizeof(Int32);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector64<Int32>>() / sizeof(Int32);
        private static readonly byte ElementIndex1 = 1;
        private static readonly byte ElementIndex2 = 3;

        private static Int32[] _data1 = new Int32[Op1ElementCount];
        private static Int32[] _data3 = new Int32[Op3ElementCount];

        private static Vector64<Int32> _clsVar1;
        private static Vector128<Int32> _clsVar3;

        private Vector64<Int32> _fld1;
        private Vector128<Int32> _fld3;

        private DataTable _dataTable;

        static InsertSelectedScalarTest__InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Int32>, byte>(ref _clsVar1), ref Unsafe.As<Int32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Int32>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int32>, byte>(ref _clsVar3), ref Unsafe.As<Int32, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<Int32>>());
        }

        public InsertSelectedScalarTest__InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Int32>, byte>(ref _fld1), ref Unsafe.As<Int32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Int32>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int32>, byte>(ref _fld3), ref Unsafe.As<Int32, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<Int32>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetInt32(); }
            _dataTable = new DataTable(_data1, _data3, new Int32[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.Arm64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = AdvSimd.Arm64.InsertSelectedScalar(
                Unsafe.Read<Vector64<Int32>>(_dataTable.inArray1Ptr),
                1,
                Unsafe.Read<Vector128<Int32>>(_dataTable.inArray3Ptr),
                3
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = AdvSimd.Arm64.InsertSelectedScalar(
                AdvSimd.LoadVector64((Int32*)(_dataTable.inArray1Ptr)),
                1,
                AdvSimd.LoadVector128((Int32*)(_dataTable.inArray3Ptr)),
                3
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(AdvSimd.Arm64).GetMethod(nameof(AdvSimd.Arm64.InsertSelectedScalar), new Type[] { typeof(Vector64<Int32>), typeof(byte), typeof(Vector128<Int32>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector64<Int32>>(_dataTable.inArray1Ptr),
                                        ElementIndex1,
                                        Unsafe.Read<Vector128<Int32>>(_dataTable.inArray3Ptr),
                                        ElementIndex2
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector64<Int32>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(AdvSimd.Arm64).GetMethod(nameof(AdvSimd.Arm64.InsertSelectedScalar), new Type[] { typeof(Vector64<Int32>), typeof(byte), typeof(Vector128<Int32>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        AdvSimd.LoadVector64((Int32*)(_dataTable.inArray1Ptr)),
                                        ElementIndex1,
                                        AdvSimd.LoadVector128((Int32*)(_dataTable.inArray3Ptr)),
                                        ElementIndex2
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector64<Int32>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = AdvSimd.Arm64.InsertSelectedScalar(
                _clsVar1,
                1,
                _clsVar3,
                3
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar3, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector64<Int32>* pClsVar1 = &_clsVar1)
            fixed (Vector128<Int32>* pClsVar3 = &_clsVar3)
            {
                var result = AdvSimd.Arm64.InsertSelectedScalar(
                    AdvSimd.LoadVector64((Int32*)(pClsVar1)),
                    1,
                    AdvSimd.LoadVector128((Int32*)(pClsVar3)),
                    3
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_clsVar1, _clsVar3, _dataTable.outArrayPtr);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector64<Int32>>(_dataTable.inArray1Ptr);
            var op3 = Unsafe.Read<Vector128<Int32>>(_dataTable.inArray3Ptr);
            var result = AdvSimd.Arm64.InsertSelectedScalar(op1, 1, op3, 3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op3, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var op1 = AdvSimd.LoadVector64((Int32*)(_dataTable.inArray1Ptr));
            var op3 = AdvSimd.LoadVector128((Int32*)(_dataTable.inArray3Ptr));
            var result = AdvSimd.Arm64.InsertSelectedScalar(op1, 1, op3, 3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op3, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new InsertSelectedScalarTest__InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3();
            var result = AdvSimd.Arm64.InsertSelectedScalar(test._fld1, 1, test._fld3, 3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld3, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new InsertSelectedScalarTest__InsertSelectedScalar_Vector64_Int32_1_Vector128_Int32_3();

            fixed (Vector64<Int32>* pFld1 = &test._fld1)
            fixed (Vector128<Int32>* pFld2 = &test._fld3)
            {
                var result = AdvSimd.Arm64.InsertSelectedScalar(
                    AdvSimd.LoadVector64((Int32*)pFld1),
                    1,
                    AdvSimd.LoadVector128((Int32*)pFld2),
                    3
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(test._fld1, test._fld3, _dataTable.outArrayPtr);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = AdvSimd.Arm64.InsertSelectedScalar(_fld1, 1, _fld3, 3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld3, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector64<Int32>* pFld1 = &_fld1)
            fixed (Vector128<Int32>* pFld2 = &_fld3)
            {
                var result = AdvSimd.Arm64.InsertSelectedScalar(
                    AdvSimd.LoadVector64((Int32*)pFld1),
                    1,
                    AdvSimd.LoadVector128((Int32*)pFld2),
                    3
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_fld1, _fld3, _dataTable.outArrayPtr);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = AdvSimd.Arm64.InsertSelectedScalar(test._fld1, 1, test._fld3, 3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld3, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            var result = AdvSimd.Arm64.InsertSelectedScalar(
                AdvSimd.LoadVector64((Int32*)(&test._fld1)),
                1,
                AdvSimd.LoadVector128((Int32*)(&test._fld3)),
                3
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld3, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector64<Int32> op1, Vector128<Int32> op3, void* result, [CallerMemberName] string method = "")
        {
            Int32[] inArray1 = new Int32[Op1ElementCount];
            Int32[] inArray3 = new Int32[Op3ElementCount];
            Int32[] outArray = new Int32[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Int32, byte>(ref inArray1[0]), op1);
            Unsafe.WriteUnaligned(ref Unsafe.As<Int32, byte>(ref inArray3[0]), op3);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector64<Int32>>());

            ValidateResult(inArray1, inArray3, outArray, method);
        }

        private void ValidateResult(void* op1, void* op3, void* result, [CallerMemberName] string method = "")
        {
            Int32[] inArray1 = new Int32[Op1ElementCount];
            Int32[] inArray3 = new Int32[Op3ElementCount];
            Int32[] outArray = new Int32[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector64<Int32>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref inArray3[0]), ref Unsafe.AsRef<byte>(op3), (uint)Unsafe.SizeOf<Vector128<Int32>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector64<Int32>>());

            ValidateResult(inArray1, inArray3, outArray, method);
        }

        private void ValidateResult(Int32[] firstOp, Int32[] thirdOp, Int32[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (var i = 0; i < RetElementCount; i++)
            {
                if (Helpers.Insert(firstOp, ElementIndex1, thirdOp[ElementIndex2], i) != result[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd.Arm64)}.{nameof(AdvSimd.Arm64.InsertSelectedScalar)}<Int32>(Vector64<Int32>, {1}, Vector128<Int32>, {3}): {method} failed:");
                TestLibrary.TestFramework.LogInformation($" firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($" thirdOp: ({string.Join(", ", thirdOp)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
