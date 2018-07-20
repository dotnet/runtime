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
        private static void ExtractUInt161()
        {
            var test = new ExtractScalarTest__ExtractUInt161();

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

    public sealed unsafe class ExtractScalarTest__ExtractUInt161
    {
        private struct TestStruct
        {
            public Vector256<UInt16> _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt16>, byte>(ref testStruct._fld), ref Unsafe.As<UInt16, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector256<UInt16>>());

                return testStruct;
            }

            public void RunStructFldScenario(ExtractScalarTest__ExtractUInt161 testClass)
            {
                var result = Avx.Extract(_fld, 1);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 32;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector256<UInt16>>() / sizeof(UInt16);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector256<UInt16>>() / sizeof(UInt16);

        private static UInt16[] _data = new UInt16[Op1ElementCount];

        private static Vector256<UInt16> _clsVar;

        private Vector256<UInt16> _fld;

        private SimpleUnaryOpTest__DataTable<UInt16, UInt16> _dataTable;

        static ExtractScalarTest__ExtractUInt161()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt16>, byte>(ref _clsVar), ref Unsafe.As<UInt16, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector256<UInt16>>());
        }

        public ExtractScalarTest__ExtractUInt161()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector256<UInt16>, byte>(ref _fld), ref Unsafe.As<UInt16, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector256<UInt16>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt16(); }
            _dataTable = new SimpleUnaryOpTest__DataTable<UInt16, UInt16>(_data, new UInt16[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Avx.IsSupported && (Environment.Is64BitProcess || ((typeof(UInt16) != typeof(long)) && (typeof(UInt16) != typeof(ulong))));

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Avx.Extract(
                Unsafe.Read<Vector256<UInt16>>(_dataTable.inArrayPtr),
                1
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Avx.Extract(
                Avx.LoadVector256((UInt16*)(_dataTable.inArrayPtr)),
                1
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Avx.Extract(
                Avx.LoadAlignedVector256((UInt16*)(_dataTable.inArrayPtr)),
                1
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Avx).GetMethod(nameof(Avx.Extract), new Type[] { typeof(Vector256<UInt16>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector256<UInt16>>(_dataTable.inArrayPtr),
                                        (byte)1
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (UInt16)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Avx).GetMethod(nameof(Avx.Extract), new Type[] { typeof(Vector256<UInt16>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadVector256((UInt16*)(_dataTable.inArrayPtr)),
                                        (byte)1
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (UInt16)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Avx).GetMethod(nameof(Avx.Extract), new Type[] { typeof(Vector256<UInt16>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Avx.LoadAlignedVector256((UInt16*)(_dataTable.inArrayPtr)),
                                        (byte)1
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (UInt16)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Avx.Extract(
                _clsVar,
                1
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var firstOp = Unsafe.Read<Vector256<UInt16>>(_dataTable.inArrayPtr);
            var result = Avx.Extract(firstOp, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var firstOp = Avx.LoadVector256((UInt16*)(_dataTable.inArrayPtr));
            var result = Avx.Extract(firstOp, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var firstOp = Avx.LoadAlignedVector256((UInt16*)(_dataTable.inArrayPtr));
            var result = Avx.Extract(firstOp, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            var test = new ExtractScalarTest__ExtractUInt161();
            var result = Avx.Extract(test._fld, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            var result = Avx.Extract(_fld, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            var test = TestStruct.Create();
            var result = Avx.Extract(test._fld, 1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunStructFldScenario()
        {
            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        public void RunUnsupportedScenario()
        {
            Succeeded = false;

            try
            {
                RunBasicScenario_UnsafeRead();
            }
            catch (PlatformNotSupportedException)
            {
                Succeeded = true;
            }
        }

        private void ValidateResult(Vector256<UInt16> firstOp, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray = new UInt16[Op1ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray[0]), firstOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<UInt16>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray = new UInt16[Op1ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector256<UInt16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector256<UInt16>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(UInt16[] firstOp, UInt16[] result, [CallerMemberName] string method = "")
        {
            if ((result[0] != firstOp[1]))
            {
                Succeeded = false;
            }

            if (!Succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Avx)}.{nameof(Avx.Extract)}<UInt16>(Vector256<UInt16><9>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"   result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }
    }
}
