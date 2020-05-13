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
using System.Runtime.Intrinsics.Arm;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        private static void DuplicateSelectedScalarToVector64_V128_SByte_8()
        {
            var test = new ImmUnaryOpTest__DuplicateSelectedScalarToVector64_V128_SByte_8();

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

    public sealed unsafe class ImmUnaryOpTest__DuplicateSelectedScalarToVector64_V128_SByte_8
    {
        private struct DataTable
        {
            private byte[] inArray;
            private byte[] outArray;

            private GCHandle inHandle;
            private GCHandle outHandle;

            private ulong alignment;

            public DataTable(SByte[] inArray, SByte[] outArray, int alignment)
            {
                int sizeOfinArray = inArray.Length * Unsafe.SizeOf<SByte>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<SByte>();
                if ((alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray || (alignment * 2) < sizeOfoutArray)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray = new byte[alignment * 2];
                this.outArray = new byte[alignment * 2];

                this.inHandle = GCHandle.Alloc(this.inArray, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArrayPtr), ref Unsafe.As<SByte, byte>(ref inArray[0]), (uint)sizeOfinArray);
            }

            public void* inArrayPtr => Align((byte*)(inHandle.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle.Free();
                outHandle.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private struct TestStruct
        {
            public Vector128<SByte> _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetSByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref testStruct._fld), ref Unsafe.As<SByte, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());

                return testStruct;
            }

            public void RunStructFldScenario(ImmUnaryOpTest__DuplicateSelectedScalarToVector64_V128_SByte_8 testClass)
            {
                var result = AdvSimd.DuplicateSelectedScalarToVector64(_fld, 8);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld, testClass._dataTable.outArrayPtr);
            }

            public void RunStructFldScenario_Load(ImmUnaryOpTest__DuplicateSelectedScalarToVector64_V128_SByte_8 testClass)
            {
                fixed (Vector128<SByte>* pFld = &_fld)
                {
                    var result = AdvSimd.DuplicateSelectedScalarToVector64(
                        AdvSimd.LoadVector128((SByte*)(pFld)),
                        8
                    );

                    Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                    testClass.ValidateResult(_fld, testClass._dataTable.outArrayPtr);
                }
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<SByte>>() / sizeof(SByte);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector64<SByte>>() / sizeof(SByte);

        private static SByte[] _data = new SByte[Op1ElementCount];

        private static Vector128<SByte> _clsVar;

        private Vector128<SByte> _fld;

        private DataTable _dataTable;

        static ImmUnaryOpTest__DuplicateSelectedScalarToVector64_V128_SByte_8()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _clsVar), ref Unsafe.As<SByte, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());
        }

        public ImmUnaryOpTest__DuplicateSelectedScalarToVector64_V128_SByte_8()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _fld), ref Unsafe.As<SByte, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetSByte(); }
            _dataTable = new DataTable(_data, new SByte[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = AdvSimd.DuplicateSelectedScalarToVector64(
                Unsafe.Read<Vector128<SByte>>(_dataTable.inArrayPtr),
                8
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = AdvSimd.DuplicateSelectedScalarToVector64(
                AdvSimd.LoadVector128((SByte*)(_dataTable.inArrayPtr)),
                8
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(AdvSimd).GetMethod(nameof(AdvSimd.DuplicateSelectedScalarToVector64), new Type[] { typeof(Vector128<SByte>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<SByte>>(_dataTable.inArrayPtr),
                                        (byte)8
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector64<SByte>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(AdvSimd).GetMethod(nameof(AdvSimd.DuplicateSelectedScalarToVector64), new Type[] { typeof(Vector128<SByte>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        AdvSimd.LoadVector128((SByte*)(_dataTable.inArrayPtr)),
                                        (byte)8
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector64<SByte>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = AdvSimd.DuplicateSelectedScalarToVector64(
                _clsVar,
                8
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector128<SByte>* pClsVar = &_clsVar)
            {
                var result = AdvSimd.DuplicateSelectedScalarToVector64(
                    AdvSimd.LoadVector128((SByte*)(pClsVar)),
                    8
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_clsVar, _dataTable.outArrayPtr);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var firstOp = Unsafe.Read<Vector128<SByte>>(_dataTable.inArrayPtr);
            var result = AdvSimd.DuplicateSelectedScalarToVector64(firstOp, 8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var firstOp = AdvSimd.LoadVector128((SByte*)(_dataTable.inArrayPtr));
            var result = AdvSimd.DuplicateSelectedScalarToVector64(firstOp, 8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ImmUnaryOpTest__DuplicateSelectedScalarToVector64_V128_SByte_8();
            var result = AdvSimd.DuplicateSelectedScalarToVector64(test._fld, 8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new ImmUnaryOpTest__DuplicateSelectedScalarToVector64_V128_SByte_8();

            fixed (Vector128<SByte>* pFld = &test._fld)
            {
                var result = AdvSimd.DuplicateSelectedScalarToVector64(
                    AdvSimd.LoadVector128((SByte*)(pFld)),
                    8
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(test._fld, _dataTable.outArrayPtr);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = AdvSimd.DuplicateSelectedScalarToVector64(_fld, 8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector128<SByte>* pFld = &_fld)
            {
                var result = AdvSimd.DuplicateSelectedScalarToVector64(
                    AdvSimd.LoadVector128((SByte*)(pFld)),
                    8
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_fld, _dataTable.outArrayPtr);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = AdvSimd.DuplicateSelectedScalarToVector64(test._fld, 8);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            var result = AdvSimd.DuplicateSelectedScalarToVector64(
                AdvSimd.LoadVector128((SByte*)(&test._fld)),
                8
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector128<SByte> firstOp, void* result, [CallerMemberName] string method = "")
        {
            SByte[] inArray = new SByte[Op1ElementCount];
            SByte[] outArray = new SByte[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref inArray[0]), firstOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector64<SByte>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* result, [CallerMemberName] string method = "")
        {
            SByte[] inArray = new SByte[Op1ElementCount];
            SByte[] outArray = new SByte[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector128<SByte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector64<SByte>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(SByte[] firstOp, SByte[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (result[0] != firstOp[8])
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (result[i] != firstOp[8])
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd)}.{nameof(AdvSimd.DuplicateSelectedScalarToVector64)}<SByte>(Vector128<SByte>, 8): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"   result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
