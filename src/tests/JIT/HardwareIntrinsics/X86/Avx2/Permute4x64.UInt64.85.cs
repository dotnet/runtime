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
        public static void Permute4x64UInt6485()
        {
            var test = new ImmUnaryOpTest__Permute4x64UInt6485();

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

    public sealed unsafe class ImmUnaryOpTest__Permute4x64UInt6485
    {
        private struct TestStruct
        {
            public Vector256<UInt64> _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt64(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt64>, byte>(ref testStruct._fld), ref Unsafe.As<UInt64, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector256<UInt64>>());

                return testStruct;
            }

            public void RunStructFldScenario(ImmUnaryOpTest__Permute4x64UInt6485 testClass)
            {
                var result = Avx2.Permute4x64(_fld, 85);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 32;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector256<UInt64>>() / sizeof(UInt64);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector256<UInt64>>() / sizeof(UInt64);

        private static UInt64[] _data = new UInt64[Op1ElementCount];

        private static Vector256<UInt64> _clsVar;

        private Vector256<UInt64> _fld;

        private SimpleUnaryOpTest__DataTable<UInt64, UInt64> _dataTable;

        static ImmUnaryOpTest__Permute4x64UInt6485()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt64(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt64>, byte>(ref _clsVar), ref Unsafe.As<UInt64, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector256<UInt64>>());
        }

        public ImmUnaryOpTest__Permute4x64UInt6485()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt64(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt64>, byte>(ref _fld), ref Unsafe.As<UInt64, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector256<UInt64>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt64(); }
            _dataTable = new SimpleUnaryOpTest__DataTable<UInt64, UInt64>(_data, new UInt64[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Avx2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Avx2.Permute4x64(
                Unsafe.Read<Vector256<UInt64>>(_dataTable.inArrayPtr),
                85
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = Avx2.Permute4x64(
                Avx.LoadVector256((UInt64*)(_dataTable.inArrayPtr)),
                85
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_LoadAligned));

            var result = Avx2.Permute4x64(
                Avx.LoadAlignedVector256((UInt64*)(_dataTable.inArrayPtr)),
                85
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Avx2).GetMethod(nameof(Avx2.Permute4x64), new Type[] { typeof(Vector256<UInt64>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<UInt64>>(_dataTable.inArrayPtr),
                                        (byte)85
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<UInt64>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(Avx2).GetMethod(nameof(Avx2.Permute4x64), new Type[] { typeof(Vector256<UInt64>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadVector256((UInt64*)(_dataTable.inArrayPtr)),
                                        (byte)85
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<UInt64>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_LoadAligned));

            var result = typeof(Avx2).GetMethod(nameof(Avx2.Permute4x64), new Type[] { typeof(Vector256<UInt64>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadAlignedVector256((UInt64*)(_dataTable.inArrayPtr)),
                                        (byte)85
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector256<UInt64>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Avx2.Permute4x64(
                _clsVar,
                85
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var firstOp = Unsafe.Read<Vector256<UInt64>>(_dataTable.inArrayPtr);
            var result = Avx2.Permute4x64(firstOp, 85);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var firstOp = Avx.LoadVector256((UInt64*)(_dataTable.inArrayPtr));
            var result = Avx2.Permute4x64(firstOp, 85);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_LoadAligned));

            var firstOp = Avx.LoadAlignedVector256((UInt64*)(_dataTable.inArrayPtr));
            var result = Avx2.Permute4x64(firstOp, 85);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ImmUnaryOpTest__Permute4x64UInt6485();
            var result = Avx2.Permute4x64(test._fld, 85);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Avx2.Permute4x64(_fld, 85);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Avx2.Permute4x64(test._fld, 85);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector256<UInt64> firstOp, void* result, [CallerMemberName] string method = "")
        {
            UInt64[] inArray = new UInt64[Op1ElementCount];
            UInt64[] outArray = new UInt64[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt64, byte>(ref inArray[0]), firstOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt64, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<UInt64>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* result, [CallerMemberName] string method = "")
        {
            UInt64[] inArray = new UInt64[Op1ElementCount];
            UInt64[] outArray = new UInt64[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt64, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector256<UInt64>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt64, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<UInt64>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(UInt64[] firstOp, UInt64[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (firstOp[1] != result[0])
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (firstOp[1] != result[i])
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Avx2)}.{nameof(Avx2.Permute4x64)}<UInt64>(Vector256<UInt64><9>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"   result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
