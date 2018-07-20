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
        private static void SetAllVector128Byte()
        {
            var test = new ScalarSimdUnaryOpTest__SetAllVector128Byte();

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

    public sealed unsafe class ScalarSimdUnaryOpTest__SetAllVector128Byte
    {
        private struct TestStruct
        {
            public Byte _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld = TestLibrary.Generator.GetByte();
                return testStruct;
            }

            public void RunStructFldScenario(ScalarSimdUnaryOpTest__SetAllVector128Byte testClass)
            {
                var result = Sse2.SetAllVector128(_fld);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<Byte>>() / sizeof(Byte);

        private static Byte _data;

        private static Byte _clsVar;

        private Byte _fld;

        private ScalarSimdUnaryOpTest__DataTable<Byte> _dataTable;

        static ScalarSimdUnaryOpTest__SetAllVector128Byte()
        {
            _clsVar = TestLibrary.Generator.GetByte();
        }

        public ScalarSimdUnaryOpTest__SetAllVector128Byte()
        {
            Succeeded = true;

            _fld = TestLibrary.Generator.GetByte();
            _data = TestLibrary.Generator.GetByte();
            _dataTable = new ScalarSimdUnaryOpTest__DataTable<Byte>(new Byte[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse2.IsSupported && (Environment.Is64BitProcess || ((typeof(Byte) != typeof(long)) && (typeof(Byte) != typeof(ulong))));

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Sse2.SetAllVector128(
                Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_data, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Sse2).GetMethod(nameof(Sse2.SetAllVector128), new Type[] { typeof(Byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Byte>)(result));
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
            var data = Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data));
            var result = Sse2.SetAllVector128(data);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(data, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            var test = new ScalarSimdUnaryOpTest__SetAllVector128Byte();
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

        private void ValidateResult(Byte firstOp, void* result, [CallerMemberName] string method = "")
        {
            Byte[] outArray = new Byte[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Byte>>());

            ValidateResult(firstOp, outArray, method);
        }

        private void ValidateResult(Byte firstOp, Byte[] result, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"{nameof(Sse2)}.{nameof(Sse2.SetAllVector128)}<Byte>(Vector128<Byte>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"   result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }
    }
}
