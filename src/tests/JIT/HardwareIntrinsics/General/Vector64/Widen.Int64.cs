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
using Xunit;

namespace JIT.HardwareIntrinsics.General._Vector64
{
    public static partial class Program
    {
        [Fact]
        public static void WidenInt64()
        {
            var test = new VectorWidenTest__WidenInt64();

            // Validates basic functionality works, using Unsafe.Read
            test.RunBasicScenario_UnsafeRead();

            // Validates calling via reflection works, using Unsafe.Read
            test.RunReflectionScenario_UnsafeRead();

            // Validates passing a static member works
            test.RunClsVarScenario();

            // Validates passing a local works, using Unsafe.Read
            test.RunLclVarScenario_UnsafeRead();

            // Validates passing the field of a local class works
            test.RunClassLclFldScenario();

            // Validates passing an instance member of a class works
            test.RunClassFldScenario();

            // Validates passing the field of a local struct works
            test.RunStructLclFldScenario();

            // Validates passing an instance member of a struct works
            test.RunStructFldScenario();

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class VectorWidenTest__WidenInt64
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] outLowerArray;
            private byte[] outUpperArray;

            private GCHandle inHandle1;
            private GCHandle outLowerHandle;
            private GCHandle outUpperHandle;

            private ulong alignment;

            public DataTable(Int32[] inArray1, Int64[] outLowerArray, Int64[] outUpperArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Int32>();
                int sizeOfoutLowerArray = outLowerArray.Length * Unsafe.SizeOf<Int64>();
                int sizeOfoutUpperArray = outUpperArray.Length * Unsafe.SizeOf<Int64>();
                if ((alignment != 32 && alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfoutLowerArray|| (alignment * 2) < sizeOfoutUpperArray)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.outLowerArray = new byte[alignment * 2];
                this.outUpperArray = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.outLowerHandle = GCHandle.Alloc(this.outLowerArray, GCHandleType.Pinned);
                this.outUpperHandle = GCHandle.Alloc(this.outUpperArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Int32, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outLowerArrayPtr => Align((byte*)(outLowerHandle.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outUpperArrayPtr => Align((byte*)(outUpperHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                outLowerHandle.Free();
                outUpperHandle.Free();
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

            public void RunStructFldScenario(VectorWidenTest__WidenInt64 testClass)
            {
                var result = Vector64.Widen(_fld1);

                Unsafe.Write(testClass._dataTable.outLowerArrayPtr, result.Lower);
                Unsafe.Write(testClass._dataTable.outUpperArrayPtr, result.Upper);
                testClass.ValidateResult(_fld1, testClass._dataTable.outLowerArrayPtr, testClass._dataTable.outUpperArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 8;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector64<Int32>>() / sizeof(Int32);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector64<Int64>>() / sizeof(Int64);

        private static Int32[] _data1 = new Int32[Op1ElementCount];

        private static Vector64<Int32> _clsVar1;

        private Vector64<Int32> _fld1;

        private DataTable _dataTable;

        static VectorWidenTest__WidenInt64()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Int32>, byte>(ref _clsVar1), ref Unsafe.As<Int32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Int32>>());
        }

        public VectorWidenTest__WidenInt64()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Int32>, byte>(ref _fld1), ref Unsafe.As<Int32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Int32>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt32(); }
            _dataTable = new DataTable(_data1, new Int64[RetElementCount], new Int64[RetElementCount], LargestVectorSize);
        }

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Vector64.Widen(
                Unsafe.Read<Vector64<Int32>>(_dataTable.inArray1Ptr)
            );

            Unsafe.Write(_dataTable.outLowerArrayPtr, result.Lower);
            Unsafe.Write(_dataTable.outUpperArrayPtr, result.Upper);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outLowerArrayPtr, _dataTable.outUpperArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var method = typeof(Vector64).GetMethod(nameof(Vector64.Widen), new Type[] {
                typeof(Vector64<Int32>)
            });

            if (method is null)
            {
                method = typeof(Vector64).GetMethod(nameof(Vector64.Widen), 1, new Type[] {
                    typeof(Vector64<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
                });
            }

            if (method.IsGenericMethodDefinition)
            {
                method = method.MakeGenericMethod(typeof(Int64));
            }

            var result = method.Invoke(null, new object[] {
                Unsafe.Read<Vector64<Int32>>(_dataTable.inArray1Ptr)
            });

            Unsafe.Write(_dataTable.outLowerArrayPtr, (((Vector64<Int64> Lower, Vector64<Int64> Upper))(result)).Lower);
            Unsafe.Write(_dataTable.outUpperArrayPtr, (((Vector64<Int64> Lower, Vector64<Int64> Upper))(result)).Upper);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outLowerArrayPtr, _dataTable.outUpperArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Vector64.Widen(
                _clsVar1
            );

            Unsafe.Write(_dataTable.outLowerArrayPtr, result.Lower);
            Unsafe.Write(_dataTable.outUpperArrayPtr, result.Upper);
            ValidateResult(_clsVar1, _dataTable.outLowerArrayPtr, _dataTable.outUpperArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector64<Int32>>(_dataTable.inArray1Ptr);
            var result = Vector64.Widen(op1);

            Unsafe.Write(_dataTable.outLowerArrayPtr, result.Lower);
            Unsafe.Write(_dataTable.outUpperArrayPtr, result.Upper);
            ValidateResult(op1, _dataTable.outLowerArrayPtr, _dataTable.outUpperArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new VectorWidenTest__WidenInt64();
            var result = Vector64.Widen(test._fld1);

            Unsafe.Write(_dataTable.outLowerArrayPtr, result.Lower);
            Unsafe.Write(_dataTable.outUpperArrayPtr, result.Upper);
            ValidateResult(test._fld1, _dataTable.outLowerArrayPtr, _dataTable.outUpperArrayPtr);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Vector64.Widen(_fld1);

            Unsafe.Write(_dataTable.outLowerArrayPtr, result.Lower);
            Unsafe.Write(_dataTable.outUpperArrayPtr, result.Upper);
            ValidateResult(_fld1, _dataTable.outLowerArrayPtr, _dataTable.outUpperArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Vector64.Widen(test._fld1);

            Unsafe.Write(_dataTable.outLowerArrayPtr, result.Lower);
            Unsafe.Write(_dataTable.outUpperArrayPtr, result.Upper);
            ValidateResult(test._fld1, _dataTable.outLowerArrayPtr, _dataTable.outUpperArrayPtr);
        }

        public void RunStructFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario));

            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        private void ValidateResult(Vector64<Int32> op1, void* lowerResult, void* upperResult, [CallerMemberName] string method = "")
        {
            Int32[] inArray1 = new Int32[Op1ElementCount];
            Int64[] outLowerArray = new Int64[RetElementCount];
            Int64[] outUpperArray = new Int64[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Int32, byte>(ref inArray1[0]), op1);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int64, byte>(ref outLowerArray[0]), ref Unsafe.AsRef<byte>(lowerResult), (uint)Unsafe.SizeOf<Vector64<Int64>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int64, byte>(ref outUpperArray[0]), ref Unsafe.AsRef<byte>(upperResult), (uint)Unsafe.SizeOf<Vector64<Int64>>());

            ValidateResult(inArray1, outLowerArray, outUpperArray, method);
        }

        private void ValidateResult(void* op1, void* lowerResult, void* upperResult, [CallerMemberName] string method = "")
        {
            Int32[] inArray1 = new Int32[Op1ElementCount];
            Int64[] outLowerArray = new Int64[RetElementCount];
            Int64[] outUpperArray = new Int64[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector64<Int32>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int64, byte>(ref outLowerArray[0]), ref Unsafe.AsRef<byte>(lowerResult), (uint)Unsafe.SizeOf<Vector64<Int64>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int64, byte>(ref outUpperArray[0]), ref Unsafe.AsRef<byte>(upperResult), (uint)Unsafe.SizeOf<Vector64<Int64>>());

            ValidateResult(inArray1, outLowerArray, outUpperArray, method);
        }

        private void ValidateResult(Int32[] firstOp, Int64[] lowerResult, Int64[] upperResult, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (var i = 0; i < RetElementCount; i++)
            {
                if (lowerResult[i] != (long)(firstOp[i]))
                {
                    succeeded = false;
                    break;
                }
            }

            for (var i = 0; i < RetElementCount; i++)
            {
                if (upperResult[i] != (long)(firstOp[i + RetElementCount]))
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Vector64)}.{nameof(Vector64.Widen)}<Int64>(Vector64<Int32>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"      firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"  lowerResult: ({string.Join(", ", lowerResult)})");
				TestLibrary.TestFramework.LogInformation($"  upperResult: ({string.Join(", ", upperResult)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
