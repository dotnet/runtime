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

namespace JIT.HardwareIntrinsics.General._Vector128
{
    public static partial class Program
    {
        [Fact]
        public static void LessThanInt16()
        {
            var test = new VectorBinaryOpTest__LessThanInt16();

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

    public sealed unsafe class VectorBinaryOpTest__LessThanInt16
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

            public DataTable(Int16[] inArray1, Int16[] inArray2, Int16[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Int16>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<Int16>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<Int16>();
                if ((alignment != 32 && alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2 || (alignment * 2) < sizeOfoutArray)
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

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Int16, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<Int16, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
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
            public Vector128<Int16> _fld1;
            public Vector128<Int16> _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref testStruct._fld1), ref Unsafe.As<Int16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Int16>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref testStruct._fld2), ref Unsafe.As<Int16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Int16>>());

                return testStruct;
            }

            public void RunStructFldScenario(VectorBinaryOpTest__LessThanInt16 testClass)
            {
                var result = Vector128.LessThan(_fld1, _fld2);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, _fld2, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<Int16>>() / sizeof(Int16);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<Int16>>() / sizeof(Int16);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<Int16>>() / sizeof(Int16);

        private static Int16[] _data1 = new Int16[Op1ElementCount];
        private static Int16[] _data2 = new Int16[Op2ElementCount];

        private static Vector128<Int16> _clsVar1;
        private static Vector128<Int16> _clsVar2;

        private Vector128<Int16> _fld1;
        private Vector128<Int16> _fld2;

        private DataTable _dataTable;

        static VectorBinaryOpTest__LessThanInt16()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref _clsVar1), ref Unsafe.As<Int16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Int16>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref _clsVar2), ref Unsafe.As<Int16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Int16>>());
        }

        public VectorBinaryOpTest__LessThanInt16()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref _fld1), ref Unsafe.As<Int16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Int16>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Int16>, byte>(ref _fld2), ref Unsafe.As<Int16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Int16>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetInt16(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetInt16(); }
            _dataTable = new DataTable(_data1, _data2, new Int16[RetElementCount], LargestVectorSize);
        }

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Vector128.LessThan(
                Unsafe.Read<Vector128<Int16>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Int16>>(_dataTable.inArray2Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var method = typeof(Vector128).GetMethod(nameof(Vector128.LessThan), new Type[] {
                typeof(Vector128<Int16>),
                typeof(Vector128<Int16>)
            });

            if (method is null)
            {
                method = typeof(Vector128).GetMethod(nameof(Vector128.LessThan), 1, new Type[] {
                    typeof(Vector128<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
                    typeof(Vector128<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
                });
            }

            if (method.IsGenericMethodDefinition)
            {
                method = method.MakeGenericMethod(typeof(Int16));
            }

            var result = method.Invoke(null, new object[] {
                Unsafe.Read<Vector128<Int16>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Int16>>(_dataTable.inArray2Ptr)
            });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Vector128.LessThan(
                _clsVar1,
                _clsVar2
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector128<Int16>>(_dataTable.inArray1Ptr);
            var op2 = Unsafe.Read<Vector128<Int16>>(_dataTable.inArray2Ptr);
            var result = Vector128.LessThan(op1, op2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new VectorBinaryOpTest__LessThanInt16();
            var result = Vector128.LessThan(test._fld1, test._fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Vector128.LessThan(_fld1, _fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Vector128.LessThan(test._fld1, test._fld2);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunStructFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario));

            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        private void ValidateResult(Vector128<Int16> op1, Vector128<Int16> op2, void* result, [CallerMemberName] string method = "")
        {
            Int16[] inArray1 = new Int16[Op1ElementCount];
            Int16[] inArray2 = new Int16[Op2ElementCount];
            Int16[] outArray = new Int16[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Int16, byte>(ref inArray1[0]), op1);
            Unsafe.WriteUnaligned(ref Unsafe.As<Int16, byte>(ref inArray2[0]), op2);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Int16>>());

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* op1, void* op2, void* result, [CallerMemberName] string method = "")
        {
            Int16[] inArray1 = new Int16[Op1ElementCount];
            Int16[] inArray2 = new Int16[Op2ElementCount];
            Int16[] outArray = new Int16[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector128<Int16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(op2), (uint)Unsafe.SizeOf<Vector128<Int16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Int16>>());

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(Int16[] left, Int16[] right, Int16[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (result[0] != ((left[0] < right[0]) ? -1 : 0))
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (result[i] != ((left[i] < right[i]) ? -1 : 0))
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Vector128)}.{nameof(Vector128.LessThan)}<Int16>(Vector128<Int16>, Vector128<Int16>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"    left: ({string.Join(", ", left)})");
                TestLibrary.TestFramework.LogInformation($"   right: ({string.Join(", ", right)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
