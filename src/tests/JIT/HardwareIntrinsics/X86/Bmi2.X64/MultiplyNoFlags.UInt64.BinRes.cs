// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\X86\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace JIT.HardwareIntrinsics.X86._Bmi2.X64
{
    public static partial class Program
    {
        [Fact]
        public static void MultiplyNoFlagsUInt64BinRes()
        {
            var test = new ScalarTernOpBinResTest__MultiplyNoFlagsUInt64();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.ReadUnaligned
                test.RunBasicScenario_UnsafeRead();

                // Validates calling via reflection works, using Unsafe.ReadUnaligned
                test.RunReflectionScenario_UnsafeRead();

                // Validates passing a static member works
                test.RunClsVarScenario();

                // Validates passing a local works, using Unsafe.ReadUnaligned
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

    public sealed unsafe class ScalarTernOpBinResTest__MultiplyNoFlagsUInt64
    {
        private struct TestStruct
        {
            public UInt64 _fld1;
            public UInt64 _fld2;
            public UInt64 _fld3;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld1 = UInt64.MaxValue;
                testStruct._fld2 = UInt64.MaxValue;
                testStruct._fld3 = 0;

                return testStruct;
            }

            public void RunStructFldScenario(ScalarTernOpBinResTest__MultiplyNoFlagsUInt64 testClass)
            {
                UInt64 buffer = 0;
                var result = Bmi2.X64.MultiplyNoFlags(_fld1, _fld2, &buffer);
                testClass.ValidateResult(_fld1, _fld2, buffer, result);
            }
        }

        private static UInt64 _data1;
        private static UInt64 _data2;
        private static UInt64 _data3;

        private static UInt64 _clsVar1;
        private static UInt64 _clsVar2;
        private static UInt64 _clsVar3;

        private UInt64 _fld1;
        private UInt64 _fld2;
        private UInt64 _fld3;

        static ScalarTernOpBinResTest__MultiplyNoFlagsUInt64()
        {
            _clsVar1 = UInt64.MaxValue;
            _clsVar2 = UInt64.MaxValue;
            _clsVar3 = 0;
        }

        public ScalarTernOpBinResTest__MultiplyNoFlagsUInt64()
        {
            Succeeded = true;

            _fld1 = UInt64.MaxValue;
            _fld2 = UInt64.MaxValue;
            _fld3 = 0;

            _data1 = UInt64.MaxValue;
            _data2 = UInt64.MaxValue;
            _data3 = 0;
        }

        public bool IsSupported => Bmi2.X64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            UInt64 buffer = 0;

            var result = Bmi2.X64.MultiplyNoFlags(
                Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1)),
                Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data2)),
                &buffer
            );

            ValidateResult(_data1, _data2, buffer, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            UInt64 buffer = 0;

            var result = typeof(Bmi2.X64).GetMethod(nameof(Bmi2.X64.MultiplyNoFlags), new Type[] { typeof(UInt64), typeof(UInt64), typeof(UInt64*) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1)),
                                        Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data2)),
                                        Pointer.Box(&buffer, typeof(UInt64*))
                                     });

            ValidateResult(_data1, _data2, buffer, (UInt64)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));
            UInt64 buffer = 0;
            var result = Bmi2.X64.MultiplyNoFlags(
                _clsVar1,
                _clsVar2,
                &buffer
            );

            ValidateResult(_clsVar1, _clsVar2, buffer, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data1 = Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1));
            var data2 = Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data2));
            var data3 = Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data3));
            var result = Bmi2.X64.MultiplyNoFlags(data1, data2, &data3);

            ValidateResult(data1, data2, data3, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            UInt64 buffer = 0;

            var test = new ScalarTernOpBinResTest__MultiplyNoFlagsUInt64();
            var result = Bmi2.X64.MultiplyNoFlags(test._fld1, test._fld2, &buffer);

            ValidateResult(test._fld1, test._fld2, buffer, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            UInt64 buffer = 0;

            var result = Bmi2.X64.MultiplyNoFlags(_fld1, _fld2, &buffer);
            ValidateResult(_fld1, _fld2, buffer, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Bmi2.X64.MultiplyNoFlags(test._fld1, test._fld2, &test._fld3);

            ValidateResult(test._fld1, test._fld2, test._fld3, result);
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

        private void ValidateResult(UInt64 op1, UInt64 op2, UInt64 lower, UInt64 higher, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            ulong expectedHigher = 18446744073709551614, expectedLower = 1; isUnexpectedResult = (expectedHigher != higher) || (expectedLower != lower);

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Bmi2.X64)}.{nameof(Bmi2.X64.MultiplyNoFlags)}<UInt64>(UInt64, UInt64, UInt64): MultiplyNoFlags failed:");
                TestLibrary.TestFramework.LogInformation($"   op1: {op1}");
                TestLibrary.TestFramework.LogInformation($"   op2: {op2}");
                TestLibrary.TestFramework.LogInformation($" lower: {lower}");
                TestLibrary.TestFramework.LogInformation($"higher: {higher}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
