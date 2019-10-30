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
        private static void BlendVariableInt64()
        {
            var test = new SimpleTernaryOpTest__BlendVariableInt64();

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

                if (Sse2.IsSupported)
                {
                    // Validates passing a static member works, using pinning and Load
                    test.RunClsVarScenario_Load();
                }

                // Validates passing a local works, using Unsafe.Read
                test.RunLclVarScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates passing a local works, using Load
                    test.RunLclVarScenario_Load();

                    // Validates passing a local works, using LoadAligned
                    test.RunLclVarScenario_LoadAligned();
                }

                // Validates passing the field of a local class works
                test.RunClassLclFldScenario();

                if (Sse2.IsSupported)
                {
                    // Validates passing the field of a local class works, using pinning and Load
                    test.RunClassLclFldScenario_Load();
                }

                // Validates passing an instance member of a class works
                test.RunClassFldScenario();

                if (Sse2.IsSupported)
                {
                    // Validates passing an instance member of a class works, using pinning and Load
                    test.RunClassFldScenario_Load();
                }

                // Validates passing the field of a local struct works
                test.RunStructLclFldScenario();

                if (Sse2.IsSupported)
                {
                    // Validates passing the field of a local struct works, using pinning and Load
                    test.RunStructLclFldScenario_Load();
                }

                // Validates passing an instance member of a struct works
                test.RunStructFldScenario();

                if (Sse2.IsSupported)
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

    public sealed unsafe class SimpleTernaryOpTest__BlendVariableInt64
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] inArray2;
            private byte[] inArray3;
            private byte[] outArray;

            private GCHandle inHandle1;
            private GCHandle inHandle2;
            private GCHandle inHandle3;
            private GCHandle outHandle;

            private ulong alignment;

            public DataTable(Int64[] inArray1, Int64[] inArray2, Int64[] inArray3, Int64[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Int64>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<Int64>();
                int sizeOfinArray3 = inArray3.Length * Unsafe.SizeOf<Int64>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<Int64>();
                if ((alignment != 32 && alignment != 16) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2 || (alignment * 2) < sizeOfinArray3 || (alignment * 2) < sizeOfoutArray)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.inArray2 = new byte[alignment * 2];
                this.inArray3 = new byte[alignment * 2];
                this.outArray = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);
                this.inHandle3 = GCHandle.Alloc(this.inArray3, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Int64, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<Int64, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray3Ptr), ref Unsafe.As<Int64, byte>(ref inArray3[0]), (uint)sizeOfinArray3);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray3Ptr => Align((byte*)(inHandle3.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
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
            public Vector128<Int64> _fld1;
            public Vector128<Int64> _fld2;
            public Vector128<Int64> _fld3;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt64(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int64>, byte>(ref testStruct._fld1), ref Unsafe.As<Int64, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Int64>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetInt64(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int64>, byte>(ref testStruct._fld2), ref Unsafe.As<Int64, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Int64>>());
                for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (((i % 2) == 0) ? Convert.ToInt64("0xFFFFFFFFFFFFFFFF", 16) : (long)0); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int64>, byte>(ref testStruct._fld3), ref Unsafe.As<Int64, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<Int64>>());

                return testStruct;
            }

            public void RunStructFldScenario(SimpleTernaryOpTest__BlendVariableInt64 testClass)
            {
                var result = Sse41.BlendVariable(_fld1, _fld2, _fld3);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, _fld2, _fld3, testClass._dataTable.outArrayPtr);
            }

            public void RunStructFldScenario_Load(SimpleTernaryOpTest__BlendVariableInt64 testClass)
            {
                fixed (Vector128<Int64>* pFld1 = &_fld1)
                fixed (Vector128<Int64>* pFld2 = &_fld2)
                fixed (Vector128<Int64>* pFld3 = &_fld3)
                {
                    var result = Sse41.BlendVariable(
                        Sse2.LoadVector128((Int64*)(pFld1)),
                        Sse2.LoadVector128((Int64*)(pFld2)),
                        Sse2.LoadVector128((Int64*)(pFld3))
                    );

                    Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                    testClass.ValidateResult(_fld1, _fld2, _fld3, testClass._dataTable.outArrayPtr);
                }
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<Int64>>() / sizeof(Int64);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<Int64>>() / sizeof(Int64);
        private static readonly int Op3ElementCount = Unsafe.SizeOf<Vector128<Int64>>() / sizeof(Int64);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<Int64>>() / sizeof(Int64);

        private static Int64[] _data1 = new Int64[Op1ElementCount];
        private static Int64[] _data2 = new Int64[Op2ElementCount];
        private static Int64[] _data3 = new Int64[Op3ElementCount];

        private static Vector128<Int64> _clsVar1;
        private static Vector128<Int64> _clsVar2;
        private static Vector128<Int64> _clsVar3;

        private Vector128<Int64> _fld1;
        private Vector128<Int64> _fld2;
        private Vector128<Int64> _fld3;

        private DataTable _dataTable;

        static SimpleTernaryOpTest__BlendVariableInt64()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt64(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int64>, byte>(ref _clsVar1), ref Unsafe.As<Int64, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Int64>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetInt64(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int64>, byte>(ref _clsVar2), ref Unsafe.As<Int64, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Int64>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (((i % 2) == 0) ? Convert.ToInt64("0xFFFFFFFFFFFFFFFF", 16) : (long)0); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int64>, byte>(ref _clsVar3), ref Unsafe.As<Int64, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<Int64>>());
        }

        public SimpleTernaryOpTest__BlendVariableInt64()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt64(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int64>, byte>(ref _fld1), ref Unsafe.As<Int64, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Int64>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetInt64(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int64>, byte>(ref _fld2), ref Unsafe.As<Int64, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Int64>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (((i % 2) == 0) ? Convert.ToInt64("0xFFFFFFFFFFFFFFFF", 16) : (long)0); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int64>, byte>(ref _fld3), ref Unsafe.As<Int64, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<Int64>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt64(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetInt64(); }
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (((i % 2) == 0) ? Convert.ToInt64("0xFFFFFFFFFFFFFFFF", 16) : (long)0); }
            _dataTable = new DataTable(_data1, _data2, _data3, new Int64[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse41.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Sse41.BlendVariable(
                Unsafe.Read<Vector128<Int64>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Int64>>(_dataTable.inArray2Ptr),
                Unsafe.Read<Vector128<Int64>>(_dataTable.inArray3Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = Sse41.BlendVariable(
                Sse2.LoadVector128((Int64*)(_dataTable.inArray1Ptr)),
                Sse2.LoadVector128((Int64*)(_dataTable.inArray2Ptr)),
                Sse2.LoadVector128((Int64*)(_dataTable.inArray3Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_LoadAligned));

            var result = Sse41.BlendVariable(
                Sse2.LoadAlignedVector128((Int64*)(_dataTable.inArray1Ptr)),
                Sse2.LoadAlignedVector128((Int64*)(_dataTable.inArray2Ptr)),
                Sse2.LoadAlignedVector128((Int64*)(_dataTable.inArray3Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Sse41).GetMethod(nameof(Sse41.BlendVariable), new Type[] { typeof(Vector128<Int64>), typeof(Vector128<Int64>), typeof(Vector128<Int64>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Int64>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<Int64>>(_dataTable.inArray2Ptr),
                                        Unsafe.Read<Vector128<Int64>>(_dataTable.inArray3Ptr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int64>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(Sse41).GetMethod(nameof(Sse41.BlendVariable), new Type[] { typeof(Vector128<Int64>), typeof(Vector128<Int64>), typeof(Vector128<Int64>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((Int64*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadVector128((Int64*)(_dataTable.inArray2Ptr)),
                                        Sse2.LoadVector128((Int64*)(_dataTable.inArray3Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int64>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_LoadAligned));

            var result = typeof(Sse41).GetMethod(nameof(Sse41.BlendVariable), new Type[] { typeof(Vector128<Int64>), typeof(Vector128<Int64>), typeof(Vector128<Int64>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((Int64*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadAlignedVector128((Int64*)(_dataTable.inArray2Ptr)),
                                        Sse2.LoadAlignedVector128((Int64*)(_dataTable.inArray3Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int64>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Sse41.BlendVariable(
                _clsVar1,
                _clsVar2,
                _clsVar3
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _clsVar3, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector128<Int64>* pClsVar1 = &_clsVar1)
            fixed (Vector128<Int64>* pClsVar2 = &_clsVar2)
            fixed (Vector128<Int64>* pClsVar3 = &_clsVar3)
            {
                var result = Sse41.BlendVariable(
                    Sse2.LoadVector128((Int64*)(pClsVar1)),
                    Sse2.LoadVector128((Int64*)(pClsVar2)),
                    Sse2.LoadVector128((Int64*)(pClsVar3))
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_clsVar1, _clsVar2, _clsVar3, _dataTable.outArrayPtr);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector128<Int64>>(_dataTable.inArray1Ptr);
            var op2 = Unsafe.Read<Vector128<Int64>>(_dataTable.inArray2Ptr);
            var op3 = Unsafe.Read<Vector128<Int64>>(_dataTable.inArray3Ptr);
            var result = Sse41.BlendVariable(op1, op2, op3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, op3, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var op1 = Sse2.LoadVector128((Int64*)(_dataTable.inArray1Ptr));
            var op2 = Sse2.LoadVector128((Int64*)(_dataTable.inArray2Ptr));
            var op3 = Sse2.LoadVector128((Int64*)(_dataTable.inArray3Ptr));
            var result = Sse41.BlendVariable(op1, op2, op3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, op3, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_LoadAligned));

            var op1 = Sse2.LoadAlignedVector128((Int64*)(_dataTable.inArray1Ptr));
            var op2 = Sse2.LoadAlignedVector128((Int64*)(_dataTable.inArray2Ptr));
            var op3 = Sse2.LoadAlignedVector128((Int64*)(_dataTable.inArray3Ptr));
            var result = Sse41.BlendVariable(op1, op2, op3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, op3, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new SimpleTernaryOpTest__BlendVariableInt64();
            var result = Sse41.BlendVariable(test._fld1, test._fld2, test._fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new SimpleTernaryOpTest__BlendVariableInt64();

            fixed (Vector128<Int64>* pFld1 = &test._fld1)
            fixed (Vector128<Int64>* pFld2 = &test._fld2)
            fixed (Vector128<Int64>* pFld3 = &test._fld3)
            {
                var result = Sse41.BlendVariable(
                    Sse2.LoadVector128((Int64*)(pFld1)),
                    Sse2.LoadVector128((Int64*)(pFld2)),
                    Sse2.LoadVector128((Int64*)(pFld3))
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Sse41.BlendVariable(_fld1, _fld2, _fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _fld3, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector128<Int64>* pFld1 = &_fld1)
            fixed (Vector128<Int64>* pFld2 = &_fld2)
            fixed (Vector128<Int64>* pFld3 = &_fld3)
            {
                var result = Sse41.BlendVariable(
                    Sse2.LoadVector128((Int64*)(pFld1)),
                    Sse2.LoadVector128((Int64*)(pFld2)),
                    Sse2.LoadVector128((Int64*)(pFld3))
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_fld1, _fld2, _fld3, _dataTable.outArrayPtr);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Sse41.BlendVariable(test._fld1, test._fld2, test._fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            var result = Sse41.BlendVariable(
                Sse2.LoadVector128((Int64*)(&test._fld1)),
                Sse2.LoadVector128((Int64*)(&test._fld2)),
                Sse2.LoadVector128((Int64*)(&test._fld3))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector128<Int64> op1, Vector128<Int64> op2, Vector128<Int64> op3, void* result, [CallerMemberName] string method = "")
        {
            Int64[] inArray1 = new Int64[Op1ElementCount];
            Int64[] inArray2 = new Int64[Op2ElementCount];
            Int64[] inArray3 = new Int64[Op3ElementCount];
            Int64[] outArray = new Int64[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref inArray1[0]), op1);
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref inArray2[0]), op2);
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref inArray3[0]), op3);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int64, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Int64>>());

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(void* op1, void* op2, void* op3, void* result, [CallerMemberName] string method = "")
        {
            Int64[] inArray1 = new Int64[Op1ElementCount];
            Int64[] inArray2 = new Int64[Op2ElementCount];
            Int64[] inArray3 = new Int64[Op3ElementCount];
            Int64[] outArray = new Int64[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int64, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector128<Int64>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int64, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(op2), (uint)Unsafe.SizeOf<Vector128<Int64>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int64, byte>(ref inArray3[0]), ref Unsafe.AsRef<byte>(op3), (uint)Unsafe.SizeOf<Vector128<Int64>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int64, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Int64>>());

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(Int64[] firstOp, Int64[] secondOp, Int64[] thirdOp, Int64[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if ((thirdOp[0] != 0) ? secondOp[0] != result[0] : firstOp[0] != result[0])
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if ((thirdOp[i] != 0) ? secondOp[i] != result[i] : firstOp[i] != result[i])
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Sse41)}.{nameof(Sse41.BlendVariable)}<Int64>(Vector128<Int64>, Vector128<Int64>, Vector128<Int64>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($" firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"secondOp: ({string.Join(", ", secondOp)})");
                TestLibrary.TestFramework.LogInformation($" thirdOp: ({string.Join(", ", thirdOp)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
