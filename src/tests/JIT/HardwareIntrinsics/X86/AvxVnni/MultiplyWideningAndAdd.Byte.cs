// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void MultiplyWideningAndAddByte()
        {
            var test = new SimpleTernaryOpTest__MultiplyWideningAndAddByte();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Avx.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                else
                {
                    Console.WriteLine("Avx Is Not Supported");
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();  //TODO: this one does not work. Fix it.

                if (Avx.IsSupported)
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

                if (Avx.IsSupported)
                {
                    // Validates passing a local works, using Load
                    test.RunLclVarScenario_Load();

                    // Validates passing a local works, using LoadAligned
                    test.RunLclVarScenario_LoadAligned();
                }

                // Validates passing the field of a local class works
                test.RunClassLclFldScenario();

                // Validates passing an instance member of a class works
                test.RunClassFldScenario();

                // Validates passing the field of a local struct works
                test.RunStructLclFldScenario();

                // Validates passing an instance member of a struct works
                test.RunStructFldScenario();
            }
            else
            {
                Console.WriteLine("Test Is Not Supported");
                // Validates we throw on unsupported hardware
                test.RunUnsupportedScenario();
            }

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class SimpleTernaryOpTest__MultiplyWideningAndAddByte
    {
        private struct DataTable
        {
            private byte[] inArray0;
            private byte[] inArray1;
            private byte[] inArray2;
            private byte[] outArray;

            private GCHandle inHandle0;
            private GCHandle inHandle1;
            private GCHandle inHandle2;
            private GCHandle outHandle;

            private ulong alignment;

            public DataTable(Int32[] inArray0, Byte[] inArray1, SByte[] inArray2, Int32[] outArray, int alignment)
            {
                int sizeOfinArray0 = inArray0.Length * Unsafe.SizeOf<Int32>();
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Byte>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<SByte>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<Int32>();

                if((alignment != 32 && alignment != 16) || (alignment *2) < sizeOfinArray0 || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2 || (alignment * 2) < sizeOfoutArray)        
                {
                    throw new ArgumentException("Invalid value of alighment");
                }

                this.inArray0 = new byte[alignment * 2];
                this.inArray1 = new byte[alignment * 2];
                this.inArray2 = new byte[alignment * 2];
                this.outArray = new byte[alignment * 2];

                this.inHandle0 = GCHandle.Alloc(this.inArray0, GCHandleType.Pinned);
                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray0Ptr), ref Unsafe.As<Int32, byte>(ref inArray0[0]), (uint)sizeOfinArray0);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Byte, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<SByte, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
            }

            public void* inArray0Ptr => Align((byte*)(inHandle0.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle0.Free();
                inHandle1.Free();
                inHandle2.Free();
                outHandle.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlighment)
            {
                return (void*)(((ulong)buffer + expectedAlighment -1) & ~(expectedAlighment - 1));
            }
        }
        private struct TestStruct
        {
            public Vector256<Int32> _fld0;
            public Vector256<Byte> _fld1;
            public Vector256<SByte> _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op0ElementCount; i++) { _data0[i] = TestLibrary.Generator.GetByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Int32>, byte>(ref testStruct._fld0), ref Unsafe.As<Int32, byte>(ref _data0[0]), (uint)Unsafe.SizeOf<Vector256<Int32>>());
                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Byte>, byte>(ref testStruct._fld1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector256<Byte>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (sbyte)TestLibrary.Generator.GetSByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<SByte>, byte>(ref testStruct._fld2), ref Unsafe.As<SByte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector256<SByte>>());

                return testStruct;
            }

            public void RunStructFldScenario(SimpleTernaryOpTest__MultiplyWideningAndAddByte testClass)
            {
                var result = AvxVnni.MultiplyWideningAndAdd(_fld0, _fld1, _fld2);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld0, _fld1, _fld2, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 32;

        private static readonly int Op0ElementCount = Unsafe.SizeOf<Vector256<Int32>>() / sizeof(Int32);
        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector256<Byte>>() / sizeof(Byte);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector256<SByte>>() / sizeof(SByte);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector256<Int32>>() / sizeof(Int32);

        private static Int32[] _data0 = new Int32[Op0ElementCount];
        private static Byte[] _data1 = new Byte[Op1ElementCount];
        private static SByte[] _data2 = new SByte[Op2ElementCount];

        private static Vector256<Int32> _clsVar0;
        private static Vector256<Byte> _clsVar1;
        private static Vector256<SByte> _clsVar2;

        private Vector256<Int32> _fld0;
        private Vector256<Byte> _fld1;
        private Vector256<SByte> _fld2;

        private DataTable _dataTable;

        static SimpleTernaryOpTest__MultiplyWideningAndAddByte()
        {
            for (var i = 0; i < Op0ElementCount; i++) { _data0[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Int32>, byte>(ref _clsVar0), ref Unsafe.As<Int32, byte>(ref _data0[0]), (uint)Unsafe.SizeOf<Vector256<Int32>>());
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Byte>, byte>(ref _clsVar1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector256<Byte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (sbyte)TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<SByte>, byte>(ref _clsVar2), ref Unsafe.As<SByte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector256<SByte>>());
        }

        public SimpleTernaryOpTest__MultiplyWideningAndAddByte()
        {
            Succeeded = true;

            for (var i = 0; i < Op0ElementCount; i++) { _data0[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Int32>, byte>(ref _fld0), ref Unsafe.As<Int32, byte>(ref _data0[0]), (uint)Unsafe.SizeOf<Vector256<Int32>>());
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<Byte>, byte>(ref _fld1), ref Unsafe.As<Byte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector256<Byte>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = (sbyte)TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<SByte>, byte>(ref _fld2), ref Unsafe.As<SByte, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector256<SByte>>());

            for (var i = 0; i < Op0ElementCount; i++) { _data0[i] = TestLibrary.Generator.GetInt32(); }
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetByte(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetSByte(); }
            _dataTable = new DataTable(_data0, _data1, _data2, new Int32[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => AvxVnni.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = AvxVnni.MultiplyWideningAndAdd(
                Unsafe.Read<Vector256<Int32>>(_dataTable.inArray0Ptr),
                Unsafe.Read<Vector256<Byte>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector256<SByte>>(_dataTable.inArray2Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray0Ptr, _dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = AvxVnni.MultiplyWideningAndAdd(
                Avx.LoadVector256((Int32*)(_dataTable.inArray0Ptr)),
                Avx.LoadVector256((Byte*)(_dataTable.inArray1Ptr)),
                Avx.LoadVector256((SByte*)(_dataTable.inArray2Ptr)));

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray0Ptr, _dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
            
        }

        public void RunBasicScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_LoadAligned));

            var result = AvxVnni.MultiplyWideningAndAdd(
                Avx.LoadAlignedVector256((Int32*)(_dataTable.inArray0Ptr)),
                Avx.LoadAlignedVector256((Byte*)(_dataTable.inArray1Ptr)),
                Avx.LoadAlignedVector256((SByte*)(_dataTable.inArray2Ptr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray0Ptr, _dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(AvxVnni).GetMethod(nameof(AvxVnni.MultiplyWideningAndAdd), new Type[] { typeof(Vector256<Int32>), typeof(Vector256<Byte>), typeof(Vector256<SByte>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<Int32>>(_dataTable.inArray0Ptr),
                                        Unsafe.Read<Vector256<Byte>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector256<SByte>>(_dataTable.inArray2Ptr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Int32>)(result));
            ValidateResult(_dataTable.inArray0Ptr, _dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(AvxVnni).GetMethod(nameof(AvxVnni.MultiplyWideningAndAdd), new Type[] { typeof(Vector256<Int32>), typeof(Vector256<Byte>), typeof(Vector256<SByte>) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadVector256((Int32*)(_dataTable.inArray0Ptr)),
                                        Avx.LoadVector256((Byte*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadVector256((SByte*)(_dataTable.inArray2Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Int32>)(result));
            ValidateResult(_dataTable.inArray0Ptr, _dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_LoadAligned));

            var result = typeof(AvxVnni).GetMethod(nameof(AvxVnni.MultiplyWideningAndAdd), new Type[] { typeof(Vector256<Int32>), typeof(Vector256<Byte>), typeof(Vector256<SByte>) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadAlignedVector256((Int32*)(_dataTable.inArray0Ptr)),
                                        Avx.LoadAlignedVector256((Byte*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadAlignedVector256((SByte*)(_dataTable.inArray2Ptr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<Int32>)(result));
            ValidateResult(_dataTable.inArray0Ptr, _dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = AvxVnni.MultiplyWideningAndAdd(
                _clsVar0,
                _clsVar1,
                _clsVar2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar0, _clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var first = Unsafe.Read<Vector256<Int32>>(_dataTable.inArray0Ptr);
            var second = Unsafe.Read<Vector256<Byte>>(_dataTable.inArray1Ptr);
            var third = Unsafe.Read<Vector256<SByte>>(_dataTable.inArray2Ptr);
            var result = AvxVnni.MultiplyWideningAndAdd(first, second, third);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(first, second, third, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var first= Avx.LoadVector256((Int32*)(_dataTable.inArray0Ptr));
            var second = Avx.LoadVector256((Byte*)(_dataTable.inArray1Ptr));
            var third = Avx.LoadVector256((SByte*)(_dataTable.inArray2Ptr));
            var result = AvxVnni.MultiplyWideningAndAdd(first, second, third);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(first, second, third, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_LoadAligned));

            var first = Avx.LoadAlignedVector256((Int32*)(_dataTable.inArray0Ptr));
            var second = Avx.LoadAlignedVector256((Byte*)(_dataTable.inArray1Ptr));
            var third = Avx.LoadAlignedVector256((SByte*)(_dataTable.inArray2Ptr));
            var result = AvxVnni.MultiplyWideningAndAdd(first, second, third);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(first, second, third, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new SimpleTernaryOpTest__MultiplyWideningAndAddByte();
            var result = AvxVnni.MultiplyWideningAndAdd(test._fld0, test._fld1, test._fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld0, test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = AvxVnni.MultiplyWideningAndAdd(_fld0, _fld1, _fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld0, _fld1, _fld2, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = AvxVnni.MultiplyWideningAndAdd(test._fld0, test._fld1, test._fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld0, test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunStructFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario));

            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
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

        private void ValidateResult(Vector256<Int32> addend, Vector256<Byte> left, Vector256<SByte> right, void* result, [CallerMemberName] string method = "")
        {
            Int32[] inArray0 = new Int32[Op0ElementCount];
            Byte[] inArray1 = new Byte[Op1ElementCount];
            SByte[] inArray2 = new SByte[Op2ElementCount];
            Int32[] outArray = new Int32[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Int32, byte>(ref inArray0[0]), addend);
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref inArray1[0]), left);
            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref inArray2[0]), right);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<Int32>>());

            ValidateResult(inArray0, inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* addend, void* left, void* right, void* result, [CallerMemberName] string method = "")
        {
            Int32[] inArray0 = new Int32[Op0ElementCount];
            Byte[] inArray1 = new Byte[Op1ElementCount];
            SByte[] inArray2 = new SByte[Op2ElementCount];
            Int32[] outArray = new Int32[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref inArray0[0]), ref Unsafe.AsRef<byte>(addend), (uint)Unsafe.SizeOf<Vector256<Int32>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(left), (uint)Unsafe.SizeOf<Vector256<Byte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(right), (uint)Unsafe.SizeOf<Vector256<SByte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<Int32>>());

            ValidateResult(inArray0, inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(Int32[] addend, Byte[] left, SByte[] right, Int32[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            Int32[] outArray = new Int32[RetElementCount];

            for (var i = 0; i < RetElementCount; i++)
            {
                outArray[i] = Math.Clamp((addend[i] + (right[i * 4 + 3] * left[i * 4 + 3] + right[i * 4 + 2] * left[i * 4 + 2])
                                                    + (right[i * 4 + 1] * left[i * 4 + 1] + right[i * 4] * left[i * 4])), int.MinValue, int.MaxValue);
            }

            for (var i = 0; i < RetElementCount; i++)
            {
                if (result[i] != outArray[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AvxVnni)}.{nameof(AvxVnni.MultiplyWideningAndAdd)}<Int32>(Vector256<Int32>, Vector256<Int32>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  addend: ({string.Join(", ", addend)})");
                TestLibrary.TestFramework.LogInformation($"  left: ({string.Join(", ", left)})");
                TestLibrary.TestFramework.LogInformation($"  right: ({string.Join(", ", right)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation($"  valid: ({string.Join(", ", outArray)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
