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
        private static void SetAllVector128SByte()
        {
            var test = new ScalarSimdUnaryOpTest__SetAllVector128SByte();

            if (test.IsSupported)
            {
                // Validates basic functionality works
                test.RunBasicScenario_UnsafeRead();

                // Validates calling via reflection works
                test.RunReflectionScenario_UnsafeRead();

                // Validates passing a static member works
                test.RunClsVarScenario();

                // Validates passing a local works
                test.RunLclVarScenario_UnsafeRead();

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

    public sealed unsafe class ScalarSimdUnaryOpTest__SetAllVector128SByte
    {
        private struct TestStruct
        {
            public SByte _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld = TestLibrary.Generator.GetSByte();
                return testStruct;
            }

            public void RunStructFldScenario(ScalarSimdUnaryOpTest__SetAllVector128SByte testClass)
            {
                var result = Sse2.SetAllVector128(_fld);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<SByte>>() / sizeof(SByte);

        private static SByte _data;

        private static SByte _clsVar;

        private SByte _fld;

        private ScalarSimdUnaryOpTest__DataTable<SByte> _dataTable;

        static ScalarSimdUnaryOpTest__SetAllVector128SByte()
        {
            _clsVar = TestLibrary.Generator.GetSByte();
        }

        public ScalarSimdUnaryOpTest__SetAllVector128SByte()
        {
            Succeeded = true;

            _fld = TestLibrary.Generator.GetSByte();
            _data = TestLibrary.Generator.GetSByte();
            _dataTable = new ScalarSimdUnaryOpTest__DataTable<SByte>(new SByte[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse2.IsSupported && (Environment.Is64BitProcess || ((typeof(SByte) != typeof(long)) && (typeof(SByte) != typeof(ulong))));

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Sse2.SetAllVector128(
                Unsafe.ReadUnaligned<SByte>(ref Unsafe.As<SByte, byte>(ref _data))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_data, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), new Type[] { typeof(SByte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<SByte>(ref Unsafe.As<SByte, byte>(ref _data))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<SByte>)(result));
            ValidateResult(_data, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            var result = Sse2.SetAllVector128(
                _clsVar
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var data = Unsafe.ReadUnaligned<SByte>(ref Unsafe.As<SByte, byte>(ref _data));
            var result = Sse2.SetAllVector128(data);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(data, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            var test = new ScalarSimdUnaryOpTest__SetAllVector128SByte();
            var result = Sse2.SetAllVector128(test._fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            var result = Sse2.SetAllVector128(_fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            var test = TestStruct.Create();
            var result = Sse2.SetAllVector128(test._fld);

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

        private void ValidateResult(SByte firstOp, void* result, [CallerMemberName] string method = "")
        {
            SByte[] outArray = new SByte[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<SByte>>());

            ValidateResult(firstOp, outArray, method);
        }

        private void ValidateResult(SByte firstOp, SByte[] result, [CallerMemberName] string method = "")
        {
            if (result[0] != firstOp)
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (result[i] != firstOp)
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Sse2)}.{nameof(Sse2.SetAllVector128)}<SByte>(Vector128<SByte>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"   result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }
    }
}
