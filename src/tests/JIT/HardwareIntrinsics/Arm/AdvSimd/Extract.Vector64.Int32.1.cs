// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\Arm\Shared. In order to make    *
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
        private static void Extract_Vector64_Int32_1()
        {
            var test = new ExtractTest__Extract_Vector64_Int32_1();

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

    public sealed unsafe class ExtractTest__Extract_Vector64_Int32_1
    {
        private struct DataTable
        {
            private byte[] inArray1;

            private GCHandle inHandle1;

            private ulong alignment;

            public DataTable(Int32[] inArray1, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Int32>();
                if ((alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Int32, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private struct TestStruct
        {
            public Vector64<Int32> _fld1;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Int32>, byte>(ref testStruct._fld1), ref Unsafe.As<Int32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Int32>>());

                return testStruct;
            }

            public void RunStructFldScenario(ExtractTest__Extract_Vector64_Int32_1 testClass)
            {
                var result = AdvSimd.Extract(_fld1, ElementIndex);

                testClass.ValidateResult(_fld1, result);
            }

            public void RunStructFldScenario_Load(ExtractTest__Extract_Vector64_Int32_1 testClass)
            {
                fixed (Vector64<Int32>* pFld1 = &_fld1)
                {
                    var result = AdvSimd.Extract(AdvSimd.LoadVector64((Int32*)pFld1), ElementIndex);

                    testClass.ValidateResult(_fld1, result);
                }
            }
        }

        private static readonly int LargestVectorSize = 8;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector64<Int32>>() / sizeof(Int32);

        private static readonly byte ElementIndex = 1;

        private static Int32[] _data1 = new Int32[Op1ElementCount];

        private static Vector64<Int32> _clsVar1;

        private Vector64<Int32> _fld1;

        private DataTable _dataTable;

        static ExtractTest__Extract_Vector64_Int32_1()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Int32>, byte>(ref _clsVar1), ref Unsafe.As<Int32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Int32>>());
        }

        public ExtractTest__Extract_Vector64_Int32_1()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Int32>, byte>(ref _fld1), ref Unsafe.As<Int32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Int32>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
            _dataTable = new DataTable(_data1, LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = AdvSimd.Extract(
                Unsafe.Read<Vector64<Int32>>(_dataTable.inArray1Ptr),
                ElementIndex
            );

            ValidateResult(_dataTable.inArray1Ptr, result);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = AdvSimd.Extract(
                AdvSimd.LoadVector64((Int32*)(_dataTable.inArray1Ptr)),
                ElementIndex
            );

            ValidateResult(_dataTable.inArray1Ptr, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(AdvSimd).GetMethod(nameof(AdvSimd.Extract), new Type[] { typeof(Vector64<Int32>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector64<Int32>>(_dataTable.inArray1Ptr),
                                        ElementIndex
                                     });

            ValidateResult(_dataTable.inArray1Ptr, (Int32)result);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(AdvSimd).GetMethod(nameof(AdvSimd.Extract), new Type[] { typeof(Vector64<Int32>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        AdvSimd.LoadVector64((Int32*)(_dataTable.inArray1Ptr)),
                                        ElementIndex
                                     });

            ValidateResult(_dataTable.inArray1Ptr, (Int32)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = AdvSimd.Extract(_clsVar1, ElementIndex);

            ValidateResult(_clsVar1, result);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector64<Int32>* pClsVar1 = &_clsVar1)
            {
                var result = AdvSimd.Extract(AdvSimd.LoadVector64((Int32*)pClsVar1), ElementIndex);

                ValidateResult(_clsVar1, result);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector64<Int32>>(_dataTable.inArray1Ptr);
            var result = AdvSimd.Extract(op1, ElementIndex);

            ValidateResult(op1, result);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var op1 = AdvSimd.LoadVector64((Int32*)(_dataTable.inArray1Ptr));
            var result = AdvSimd.Extract(op1, ElementIndex);

            ValidateResult(op1, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ExtractTest__Extract_Vector64_Int32_1();
            var result = AdvSimd.Extract(test._fld1, ElementIndex);

            ValidateResult(test._fld1, result);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new ExtractTest__Extract_Vector64_Int32_1();

            fixed (Vector64<Int32>* pFld1 = &test._fld1)
            {
                var result = AdvSimd.Extract(AdvSimd.LoadVector64((Int32*)(pFld1)), ElementIndex);

                ValidateResult(test._fld1, result);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = AdvSimd.Extract(_fld1, ElementIndex);

            ValidateResult(_fld1, result);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector64<Int32>* pFld1 = &_fld1)
            {
                var result = AdvSimd.Extract(AdvSimd.LoadVector64((Int32*)(pFld1)), ElementIndex);

                ValidateResult(_fld1, result);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = AdvSimd.Extract(test._fld1, ElementIndex);

            ValidateResult(test._fld1, result);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            var result = AdvSimd.Extract(
                AdvSimd.LoadVector64((Int32*)&test._fld1),
                ElementIndex
            );

            ValidateResult(test._fld1, result);
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

        private void ValidateResult(Vector64<Int32> op1, Int32 result, [CallerMemberName] string method = "")
        {
            Int32[] inArray1 = new Int32[Op1ElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Int32, byte>(ref inArray1[0]), op1);

            ValidateResult(inArray1, result, method);
        }

        private void ValidateResult(void* op1, Int32 result, [CallerMemberName] string method = "")
        {
            Int32[] inArray1 = new Int32[Op1ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector64<Int32>>());

            ValidateResult(inArray1, result, method);
        }

        private void ValidateResult(Int32[] firstOp, Int32 result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (firstOp[ElementIndex] != result)
            {
                succeeded = false;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd)}.{nameof(AdvSimd.Extract)}(Vector64<Int32>, 1): {method} failed:");
                TestLibrary.TestFramework.LogInformation($" firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
