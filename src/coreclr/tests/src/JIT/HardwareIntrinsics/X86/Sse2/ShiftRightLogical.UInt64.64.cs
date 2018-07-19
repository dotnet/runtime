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
        private static void ShiftRightLogicalUInt6464()
        {
            var test = new ImmUnaryOpTest__ShiftRightLogicalUInt6464();

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

    public sealed unsafe class ImmUnaryOpTest__ShiftRightLogicalUInt6464
    {
        private struct TestStruct
        {
            public Vector128<UInt64> _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();
                var random = new Random();

                for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ulong)(random.Next(0, int.MaxValue)); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt64>, byte>(ref testStruct._fld), ref Unsafe.As<UInt64, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<UInt64>>());

                return testStruct;
            }

            public void RunStructFldScenario(ImmUnaryOpTest__ShiftRightLogicalUInt6464 testClass)
            {
                var result = Sse2.ShiftRightLogical(_fld, 64);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<UInt64>>() / sizeof(UInt64);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<UInt64>>() / sizeof(UInt64);

        private static UInt64[] _data = new UInt64[Op1ElementCount];

        private static Vector128<UInt64> _clsVar;

        private Vector128<UInt64> _fld;

        private SimpleUnaryOpTest__DataTable<UInt64, UInt64> _dataTable;

        static ImmUnaryOpTest__ShiftRightLogicalUInt6464()
        {
            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ulong)(random.Next(0, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt64>, byte>(ref _clsVar), ref Unsafe.As<UInt64, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<UInt64>>());
        }

        public ImmUnaryOpTest__ShiftRightLogicalUInt6464()
        {
            Succeeded = true;

            var random = new Random();

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ulong)(random.Next(0, int.MaxValue)); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt64>, byte>(ref _fld), ref Unsafe.As<UInt64, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<UInt64>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = (ulong)(random.Next(0, int.MaxValue)); }
            _dataTable = new SimpleUnaryOpTest__DataTable<UInt64, UInt64>(_data, new UInt64[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Sse2.ShiftRightLogical(
                Unsafe.Read<Vector128<UInt64>>(_dataTable.inArrayPtr),
                64
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            var result = Sse2.ShiftRightLogical(
                Sse2.LoadVector128((UInt64*)(_dataTable.inArrayPtr)),
                64
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Sse2.ShiftRightLogical(
                Sse2.LoadAlignedVector128((UInt64*)(_dataTable.inArrayPtr)),
                64
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical), new Type[] { typeof(Vector128<UInt64>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<UInt64>>(_dataTable.inArrayPtr),
                                        (byte)64
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt64>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical), new Type[] { typeof(Vector128<UInt64>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((UInt64*)(_dataTable.inArrayPtr)),
                                        (byte)64
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt64>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.ShiftRightLogical), new Type[] { typeof(Vector128<UInt64>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((UInt64*)(_dataTable.inArrayPtr)),
                                        (byte)64
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<UInt64>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Sse2.ShiftRightLogical(
                _clsVar,
                64
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var firstOp = Unsafe.Read<Vector128<UInt64>>(_dataTable.inArrayPtr);
            var result = Sse2.ShiftRightLogical(firstOp, 64);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            var firstOp = Sse2.LoadVector128((UInt64*)(_dataTable.inArrayPtr));
            var result = Sse2.ShiftRightLogical(firstOp, 64);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var firstOp = Sse2.LoadAlignedVector128((UInt64*)(_dataTable.inArrayPtr));
            var result = Sse2.ShiftRightLogical(firstOp, 64);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            var test = new ImmUnaryOpTest__ShiftRightLogicalUInt6464();
            var result = Sse2.ShiftRightLogical(test._fld, 64);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            var result = Sse2.ShiftRightLogical(_fld, 64);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            var test = TestStruct.Create();
            var result = Sse2.ShiftRightLogical(test._fld, 64);

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

        private void ValidateResult(Vector128<UInt64> firstOp, void* result, [CallerMemberName] string method = "")
        {
            UInt64[] inArray = new UInt64[Op1ElementCount];
            UInt64[] outArray = new UInt64[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt64, byte>(ref inArray[0]), firstOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt64, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt64>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* result, [CallerMemberName] string method = "")
        {
            UInt64[] inArray = new UInt64[Op1ElementCount];
            UInt64[] outArray = new UInt64[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt64, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector128<UInt64>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt64, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt64>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(UInt64[] firstOp, UInt64[] result, [CallerMemberName] string method = "")
        {
            if (0 != result[0])
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (0 != result[i])
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Sse2)}.{nameof(Sse2.ShiftRightLogical)}<UInt64>(Vector128<UInt64><9>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"   result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }
    }
}
