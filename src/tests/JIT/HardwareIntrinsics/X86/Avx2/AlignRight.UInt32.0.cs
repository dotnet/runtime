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
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace JIT.HardwareIntrinsics.X86._Avx2
{
    public static partial class Program
    {
        [Fact]
        public static void AlignRightUInt320()
        {
            var test = new ImmBinaryOpTest__AlignRightUInt320();

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

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

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
                // Validates we throw on unsupported hardware
                test.RunUnsupportedScenario();
            }

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class ImmBinaryOpTest__AlignRightUInt320
    {
        private struct TestStruct
        {
            public Vector256<UInt32> _fld1;
            public Vector256<UInt32> _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt32(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref testStruct._fld1), ref Unsafe.As<UInt32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector256<UInt32>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt32(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref testStruct._fld2), ref Unsafe.As<UInt32, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector256<UInt32>>());

                return testStruct;
            }

            public void RunStructFldScenario(ImmBinaryOpTest__AlignRightUInt320 testClass)
            {
                var result = Avx2.AlignRight(_fld1, _fld2, 0);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, _fld2, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 32;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector256<UInt32>>() / sizeof(UInt32);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector256<UInt32>>() / sizeof(UInt32);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector256<UInt32>>() / sizeof(UInt32);

        private static UInt32[] _data1 = new UInt32[Op1ElementCount];
        private static UInt32[] _data2 = new UInt32[Op2ElementCount];

        private static Vector256<UInt32> _clsVar1;
        private static Vector256<UInt32> _clsVar2;

        private Vector256<UInt32> _fld1;
        private Vector256<UInt32> _fld2;

        private SimpleBinaryOpTest__DataTable<UInt32, UInt32, UInt32> _dataTable;

        static ImmBinaryOpTest__AlignRightUInt320()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref _clsVar1), ref Unsafe.As<UInt32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector256<UInt32>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref _clsVar2), ref Unsafe.As<UInt32, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector256<UInt32>>());
        }

        public ImmBinaryOpTest__AlignRightUInt320()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref _fld1), ref Unsafe.As<UInt32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector256<UInt32>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt32>, byte>(ref _fld2), ref Unsafe.As<UInt32, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector256<UInt32>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt32(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt32(); }
            _dataTable = new SimpleBinaryOpTest__DataTable<UInt32, UInt32, UInt32>(_data1, _data2, new UInt32[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Avx2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Avx2.AlignRight(
                Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray2Ptr),
                0
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = Avx2.AlignRight(
                Avx.LoadVector256((UInt32*)(_dataTable.inArray1Ptr)),
                Avx.LoadVector256((UInt32*)(_dataTable.inArray2Ptr)),
                0
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_LoadAligned));

            var result = Avx2.AlignRight(
                Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray1Ptr)),
                Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray2Ptr)),
                0
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Avx2).GetMethod(nameof(Avx2.AlignRight), new Type[] { typeof(Vector256<UInt32>), typeof(Vector256<UInt32>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray2Ptr),
                                        (byte)0
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<UInt32>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(Avx2).GetMethod(nameof(Avx2.AlignRight), new Type[] { typeof(Vector256<UInt32>), typeof(Vector256<UInt32>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadVector256((UInt32*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadVector256((UInt32*)(_dataTable.inArray2Ptr)),
                                        (byte)0
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<UInt32>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_LoadAligned));

            var result = typeof(Avx2).GetMethod(nameof(Avx2.AlignRight), new Type[] { typeof(Vector256<UInt32>), typeof(Vector256<UInt32>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray1Ptr)),
                                        Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray2Ptr)),
                                        (byte)0
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<UInt32>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Avx2.AlignRight(
                _clsVar1,
                _clsVar2,
                0
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var left = Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray1Ptr);
            var right = Unsafe.Read<Vector256<UInt32>>(_dataTable.inArray2Ptr);
            var result = Avx2.AlignRight(left, right, 0);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var left = Avx.LoadVector256((UInt32*)(_dataTable.inArray1Ptr));
            var right = Avx.LoadVector256((UInt32*)(_dataTable.inArray2Ptr));
            var result = Avx2.AlignRight(left, right, 0);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_LoadAligned));

            var left = Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray1Ptr));
            var right = Avx.LoadAlignedVector256((UInt32*)(_dataTable.inArray2Ptr));
            var result = Avx2.AlignRight(left, right, 0);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(left, right, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ImmBinaryOpTest__AlignRightUInt320();
            var result = Avx2.AlignRight(test._fld1, test._fld2, 0);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Avx2.AlignRight(_fld1, _fld2, 0);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Avx2.AlignRight(test._fld1, test._fld2, 0);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector256<UInt32> left, Vector256<UInt32> right, void* result, [CallerMemberName] string method = "")
        {
            UInt32[] inArray1 = new UInt32[Op1ElementCount];
            UInt32[] inArray2 = new UInt32[Op2ElementCount];
            UInt32[] outArray = new UInt32[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt32, byte>(ref inArray1[0]), left);
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt32, byte>(ref inArray2[0]), right);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<UInt32>>());

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(void* left, void* right, void* result, [CallerMemberName] string method = "")
        {
            UInt32[] inArray1 = new UInt32[Op1ElementCount];
            UInt32[] inArray2 = new UInt32[Op2ElementCount];
            UInt32[] outArray = new UInt32[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(left), (uint)Unsafe.SizeOf<Vector256<UInt32>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(right), (uint)Unsafe.SizeOf<Vector256<UInt32>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<UInt32>>());

            ValidateResult(inArray1, inArray2, outArray, method);
        }

        private void ValidateResult(UInt32[] left, UInt32[] right, UInt32[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (result[0] != right[0])
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (result[i] != right[i])
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Avx2)}.{nameof(Avx2.AlignRight)}<UInt32>(Vector256<UInt32>.0, Vector256<UInt32>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"    left: ({string.Join(", ", left)})");
                TestLibrary.TestFramework.LogInformation($"   right: ({string.Join(", ", right)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
