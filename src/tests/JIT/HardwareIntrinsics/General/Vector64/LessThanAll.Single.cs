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

namespace JIT.HardwareIntrinsics.General
{
    public static partial class Program
    {
        private static void LessThanAllSingle()
        {
            var test = new VectorBooleanBinaryOpTest__LessThanAllSingle();

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

    public sealed unsafe class VectorBooleanBinaryOpTest__LessThanAllSingle
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] inArray2;

            private GCHandle inHandle1;
            private GCHandle inHandle2;

            private ulong alignment;

            public DataTable(Single[] inArray1, Single[] inArray2, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Single>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<Single>();
                if ((alignment != 32 && alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.inArray2 = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Single, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<Single, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private struct TestStruct
        {
            public Vector64<Single> _fld1;
            public Vector64<Single> _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSingle(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Single>, byte>(ref testStruct._fld1), ref Unsafe.As<Single, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Single>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetSingle(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Single>, byte>(ref testStruct._fld2), ref Unsafe.As<Single, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector64<Single>>());

                return testStruct;
            }

            public void RunStructFldScenario(VectorBooleanBinaryOpTest__LessThanAllSingle testClass)
            {
                var result = Vector64.LessThanAll(_fld1, _fld2);
                testClass.ValidateResult(_fld1, _fld2, result);
            }
        }

        private static readonly int LargestVectorSize = 8;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector64<Single>>() / sizeof(Single);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector64<Single>>() / sizeof(Single);

        private static Single[] _data1 = new Single[Op1ElementCount];
        private static Single[] _data2 = new Single[Op2ElementCount];

        private static Vector64<Single> _clsVar1;
        private static Vector64<Single> _clsVar2;

        private Vector64<Single> _fld1;
        private Vector64<Single> _fld2;

        private DataTable _dataTable;

        static VectorBooleanBinaryOpTest__LessThanAllSingle()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSingle(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Single>, byte>(ref _clsVar1), ref Unsafe.As<Single, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Single>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetSingle(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Single>, byte>(ref _clsVar2), ref Unsafe.As<Single, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector64<Single>>());
        }

        public VectorBooleanBinaryOpTest__LessThanAllSingle()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSingle(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Single>, byte>(ref _fld1), ref Unsafe.As<Single, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<Single>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetSingle(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<Single>, byte>(ref _fld2), ref Unsafe.As<Single, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector64<Single>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSingle(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetSingle(); }
            _dataTable = new DataTable(_data1, _data2, LargestVectorSize);
        }

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Vector64.LessThanAll(
                Unsafe.Read<Vector64<Single>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector64<Single>>(_dataTable.inArray2Ptr)
            );

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var method = typeof(Vector64).GetMethod(nameof(Vector64.LessThanAll), new Type[] {
                typeof(Vector64<Single>),
                typeof(Vector64<Single>)
            });

            if (method is null)
            {
                method = typeof(Vector64).GetMethod(nameof(Vector64.LessThanAll), 1, new Type[] {
                    typeof(Vector64<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
                    typeof(Vector64<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
                });
            }

            if (method.IsGenericMethodDefinition)
            {
                method = method.MakeGenericMethod(typeof(Single));
            }

            var result = method.Invoke(null, new object[] {
                Unsafe.Read<Vector64<Single>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector64<Single>>(_dataTable.inArray2Ptr)
            });

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, (bool)(result));
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Vector64.LessThanAll(
                _clsVar1,
                _clsVar2
            );

            ValidateResult(_clsVar1, _clsVar2, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector64<Single>>(_dataTable.inArray1Ptr);
            var op2 = Unsafe.Read<Vector64<Single>>(_dataTable.inArray2Ptr);
            var result = Vector64.LessThanAll(op1, op2);

            ValidateResult(op1, op2, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new VectorBooleanBinaryOpTest__LessThanAllSingle();
            var result = Vector64.LessThanAll(test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Vector64.LessThanAll(_fld1, _fld2);

            ValidateResult(_fld1, _fld2, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Vector64.LessThanAll(test._fld1, test._fld2);
            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunStructFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario));

            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        private void ValidateResult(Vector64<Single> op1, Vector64<Single> op2, bool result, [CallerMemberName] string method = "")
        {
            Single[] inArray1 = new Single[Op1ElementCount];
            Single[] inArray2 = new Single[Op2ElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref inArray1[0]), op1);
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref inArray2[0]), op2);

            ValidateResult(inArray1, inArray2, result, method);
        }

        private void ValidateResult(void* op1, void* op2, bool result, [CallerMemberName] string method = "")
        {
            Single[] inArray1 = new Single[Op1ElementCount];
            Single[] inArray2 = new Single[Op2ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector64<Single>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(op2), (uint)Unsafe.SizeOf<Vector64<Single>>());

            ValidateResult(inArray1, inArray2, result, method);
        }

        private void ValidateResult(Single[] left, Single[] right, bool result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            var expectedResult = true;

            for (var i = 0; i < Op1ElementCount; i++)
            {
                expectedResult &= (left[i] < right[i]);
            }

            succeeded = (expectedResult == result);

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Vector64)}.{nameof(Vector64.LessThanAll)}<Single>(Vector64<Single>, Vector64<Single>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"    left: ({string.Join(", ", left)})");
                TestLibrary.TestFramework.LogInformation($"   right: ({string.Join(", ", right)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({result})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
