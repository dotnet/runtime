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
        private static void BlendVariableSByte()
        {
            var test = new SimpleTernaryOpTest__BlendVariableSByte();

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

    public sealed unsafe class SimpleTernaryOpTest__BlendVariableSByte
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

            public DataTable(SByte[] inArray1, SByte[] inArray2, SByte[] inArray3, SByte[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<SByte>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<SByte>();
                int sizeOfinArray3 = inArray3.Length * Unsafe.SizeOf<SByte>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<SByte>();
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

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<SByte, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<SByte, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray3Ptr), ref Unsafe.As<SByte, byte>(ref inArray3[0]), (uint)sizeOfinArray3);
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
            public Vector128<SByte> _fld1;
            public Vector128<SByte> _fld2;
            public Vector128<SByte> _fld3;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref testStruct._fld1), ref Unsafe.As<SByte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetSByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref testStruct._fld2), ref Unsafe.As<SByte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());
                for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (sbyte)(((i % 2) == 0) ? -128 : 1); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref testStruct._fld3), ref Unsafe.As<SByte, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());

                return testStruct;
            }

            public void RunStructFldScenario(SimpleTernaryOpTest__BlendVariableSByte testClass)
            {
                var result = Sse41.BlendVariable(_fld1, _fld2, _fld3);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, _fld2, _fld3, testClass._dataTable.outArrayPtr);
            }

            public void RunStructFldScenario_Load(SimpleTernaryOpTest__BlendVariableSByte testClass)
            {
                fixed (Vector128<SByte>* pFld1 = &_fld1)
                fixed (Vector128<SByte>* pFld2 = &_fld2)
                fixed (Vector128<SByte>* pFld3 = &_fld3)
                {
                    var result = Sse41.BlendVariable(
                        Sse2.LoadVector128((SByte*)(pFld1)),
                        Sse2.LoadVector128((SByte*)(pFld2)),
                        Sse2.LoadVector128((SByte*)(pFld3))
                    );

                    Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                    testClass.ValidateResult(_fld1, _fld2, _fld3, testClass._dataTable.outArrayPtr);
                }
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<SByte>>() / sizeof(SByte);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<SByte>>() / sizeof(SByte);
        private static readonly int Op3ElementCount = Unsafe.SizeOf<Vector128<SByte>>() / sizeof(SByte);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<SByte>>() / sizeof(SByte);

        private static SByte[] _data1 = new SByte[Op1ElementCount];
        private static SByte[] _data2 = new SByte[Op2ElementCount];
        private static SByte[] _data3 = new SByte[Op3ElementCount];

        private static Vector128<SByte> _clsVar1;
        private static Vector128<SByte> _clsVar2;
        private static Vector128<SByte> _clsVar3;

        private Vector128<SByte> _fld1;
        private Vector128<SByte> _fld2;
        private Vector128<SByte> _fld3;

        private DataTable _dataTable;

        static SimpleTernaryOpTest__BlendVariableSByte()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _clsVar1), ref Unsafe.As<SByte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _clsVar2), ref Unsafe.As<SByte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (sbyte)(((i % 2) == 0) ? -128 : 1); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _clsVar3), ref Unsafe.As<SByte, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());
        }

        public SimpleTernaryOpTest__BlendVariableSByte()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _fld1), ref Unsafe.As<SByte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _fld2), ref Unsafe.As<SByte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (sbyte)(((i % 2) == 0) ? -128 : 1); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<SByte>, byte>(ref _fld3), ref Unsafe.As<SByte, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector128<SByte>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSByte(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetSByte(); }
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = (sbyte)(((i % 2) == 0) ? -128 : 1); }
            _dataTable = new DataTable(_data1, _data2, _data3, new SByte[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse41.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Sse41.BlendVariable(
                Unsafe.Read<Vector128<SByte>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<SByte>>(_dataTable.inArray2Ptr),
                Unsafe.Read<Vector128<SByte>>(_dataTable.inArray3Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = Sse41.BlendVariable(
                Sse2.LoadVector128((SByte*)(_dataTable.inArray1Ptr)),
                Sse2.LoadVector128((SByte*)(_dataTable.inArray2Ptr)),
                Sse2.LoadVector128((SByte*)(_dataTable.inArray3Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_LoadAligned));

            var result = Sse41.BlendVariable(
                Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray1Ptr)),
                Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray2Ptr)),
                Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray3Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Sse41).GetMethod(nameof(Sse41.BlendVariable), new Type[] { typeof(Vector128<SByte>), typeof(Vector128<SByte>), typeof(Vector128<SByte>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<SByte>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<SByte>>(_dataTable.inArray2Ptr),
                                        Unsafe.Read<Vector128<SByte>>(_dataTable.inArray3Ptr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<SByte>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(Sse41).GetMethod(nameof(Sse41.BlendVariable), new Type[] { typeof(Vector128<SByte>), typeof(Vector128<SByte>), typeof(Vector128<SByte>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((SByte*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadVector128((SByte*)(_dataTable.inArray2Ptr)),
                                        Sse2.LoadVector128((SByte*)(_dataTable.inArray3Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<SByte>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_LoadAligned));

            var result = typeof(Sse41).GetMethod(nameof(Sse41.BlendVariable), new Type[] { typeof(Vector128<SByte>), typeof(Vector128<SByte>), typeof(Vector128<SByte>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray2Ptr)),
                                        Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray3Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<SByte>)(result));
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

            fixed (Vector128<SByte>* pClsVar1 = &_clsVar1)
            fixed (Vector128<SByte>* pClsVar2 = &_clsVar2)
            fixed (Vector128<SByte>* pClsVar3 = &_clsVar3)
            {
                var result = Sse41.BlendVariable(
                    Sse2.LoadVector128((SByte*)(pClsVar1)),
                    Sse2.LoadVector128((SByte*)(pClsVar2)),
                    Sse2.LoadVector128((SByte*)(pClsVar3))
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_clsVar1, _clsVar2, _clsVar3, _dataTable.outArrayPtr);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector128<SByte>>(_dataTable.inArray1Ptr);
            var op2 = Unsafe.Read<Vector128<SByte>>(_dataTable.inArray2Ptr);
            var op3 = Unsafe.Read<Vector128<SByte>>(_dataTable.inArray3Ptr);
            var result = Sse41.BlendVariable(op1, op2, op3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, op3, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var op1 = Sse2.LoadVector128((SByte*)(_dataTable.inArray1Ptr));
            var op2 = Sse2.LoadVector128((SByte*)(_dataTable.inArray2Ptr));
            var op3 = Sse2.LoadVector128((SByte*)(_dataTable.inArray3Ptr));
            var result = Sse41.BlendVariable(op1, op2, op3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, op3, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_LoadAligned));

            var op1 = Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray1Ptr));
            var op2 = Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray2Ptr));
            var op3 = Sse2.LoadAlignedVector128((SByte*)(_dataTable.inArray3Ptr));
            var result = Sse41.BlendVariable(op1, op2, op3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, op3, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new SimpleTernaryOpTest__BlendVariableSByte();
            var result = Sse41.BlendVariable(test._fld1, test._fld2, test._fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new SimpleTernaryOpTest__BlendVariableSByte();

            fixed (Vector128<SByte>* pFld1 = &test._fld1)
            fixed (Vector128<SByte>* pFld2 = &test._fld2)
            fixed (Vector128<SByte>* pFld3 = &test._fld3)
            {
                var result = Sse41.BlendVariable(
                    Sse2.LoadVector128((SByte*)(pFld1)),
                    Sse2.LoadVector128((SByte*)(pFld2)),
                    Sse2.LoadVector128((SByte*)(pFld3))
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

            fixed (Vector128<SByte>* pFld1 = &_fld1)
            fixed (Vector128<SByte>* pFld2 = &_fld2)
            fixed (Vector128<SByte>* pFld3 = &_fld3)
            {
                var result = Sse41.BlendVariable(
                    Sse2.LoadVector128((SByte*)(pFld1)),
                    Sse2.LoadVector128((SByte*)(pFld2)),
                    Sse2.LoadVector128((SByte*)(pFld3))
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
                Sse2.LoadVector128((SByte*)(&test._fld1)),
                Sse2.LoadVector128((SByte*)(&test._fld2)),
                Sse2.LoadVector128((SByte*)(&test._fld3))
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

        private void ValidateResult(Vector128<SByte> op1, Vector128<SByte> op2, Vector128<SByte> op3, void* result, [CallerMemberName] string method = "")
        {
            SByte[] inArray1 = new SByte[Op1ElementCount];
            SByte[] inArray2 = new SByte[Op2ElementCount];
            SByte[] inArray3 = new SByte[Op3ElementCount];
            SByte[] outArray = new SByte[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref inArray1[0]), op1);
            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref inArray2[0]), op2);
            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref inArray3[0]), op3);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<SByte>>());

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(void* op1, void* op2, void* op3, void* result, [CallerMemberName] string method = "")
        {
            SByte[] inArray1 = new SByte[Op1ElementCount];
            SByte[] inArray2 = new SByte[Op2ElementCount];
            SByte[] inArray3 = new SByte[Op3ElementCount];
            SByte[] outArray = new SByte[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector128<SByte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(op2), (uint)Unsafe.SizeOf<Vector128<SByte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref inArray3[0]), ref Unsafe.AsRef<byte>(op3), (uint)Unsafe.SizeOf<Vector128<SByte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<SByte>>());

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(SByte[] firstOp, SByte[] secondOp, SByte[] thirdOp, SByte[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (((thirdOp[0] >> 7) & 1) == 1 ? secondOp[0] != result[0] : firstOp[0] != result[0])
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (((thirdOp[i] >> 7) & 1) == 1 ? secondOp[i] != result[i] : firstOp[i] != result[i])
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Sse41)}.{nameof(Sse41.BlendVariable)}<SByte>(Vector128<SByte>, Vector128<SByte>, Vector128<SByte>): {method} failed:");
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
