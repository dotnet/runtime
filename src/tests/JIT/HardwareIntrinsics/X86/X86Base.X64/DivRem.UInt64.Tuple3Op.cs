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

namespace JIT.HardwareIntrinsics.X86._X86Base.X64
{
    public static partial class Program
    {
        [Fact]
        public static void DivRemUInt64Tuple3Op()
        {
            var test = new ScalarTernOpTupleTest__DivRemUInt64();

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

    public sealed unsafe class ScalarTernOpTupleTest__DivRemUInt64
    {
        private struct TestStruct
        {
            public UInt64 _fld1;
            public UInt64 _fld2;
            public UInt64 _fld3;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld1 = 1;
                testStruct._fld2 = 1;
                testStruct._fld3 = 0x8000000000000000;

                return testStruct;
            }

            public void RunStructFldScenario(ScalarTernOpTupleTest__DivRemUInt64 testClass)
            {
                var result = X86Base.X64.DivRem(_fld1, _fld2, _fld3);
                testClass.ValidateResult(_fld1, _fld2, _fld3, result);
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

        static ScalarTernOpTupleTest__DivRemUInt64()
        {
            _clsVar1 = 1;
            _clsVar2 = 1;
            _clsVar3 = 0x8000000000000000;
        }

        public ScalarTernOpTupleTest__DivRemUInt64()
        {
            Succeeded = true;

            _fld1 = 1;
            _fld2 = 1;
            _fld3 = 0x8000000000000000;

            _data1 = 1;
            _data2 = 1;
            _data3 = 0x8000000000000000;
        }

        public bool IsSupported => X86Base.X64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = X86Base.X64.DivRem(
                Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1)),
                Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data2)),
                Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data3))
            );

            ValidateResult(_data1, _data2, _data3, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(X86Base.X64).GetMethod(nameof(X86Base.X64.DivRem), new Type[] { typeof(UInt64), typeof(UInt64), typeof(UInt64) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1)),
                                        Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data2)),
                                        Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data3))
                                     });

            ValidateResult(_data1, _data2, _data3, ((UInt64, UInt64))result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = X86Base.X64.DivRem(
                _clsVar1,
                _clsVar2,
                _clsVar3
            );

            ValidateResult(_clsVar1, _clsVar2, _clsVar3, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data1 = Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1));
            var data2 = Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data2));
            var data3 = Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data3));
            var result = X86Base.X64.DivRem(data1, data2, data3);

            ValidateResult(data1, data2, data3, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarTernOpTupleTest__DivRemUInt64();
            var result = X86Base.X64.DivRem(test._fld1, test._fld2, test._fld3);

            ValidateResult(test._fld1, test._fld2, test._fld3, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = X86Base.X64.DivRem(_fld1, _fld2, _fld3);
            ValidateResult(_fld1, _fld2, _fld3, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = X86Base.X64.DivRem(test._fld1, test._fld2, test._fld3);

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

        private void ValidateResult(UInt64 op1, UInt64 op2, UInt64 op3, (UInt64, UInt64) result, [CallerMemberName] string method = "")
        {
            (var ret1, var ret2) = result;
            var isUnexpectedResult = false;

            ulong expectedQuotient = 2;          ulong expectedReminder = 1;  isUnexpectedResult = (expectedQuotient != ret1) || (expectedReminder != ret2);

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(X86Base.X64)}.{nameof(X86Base.X64.DivRem)}<(UInt64, UInt64)>(UInt64, UInt64, UInt64): DivRem failed:");
                TestLibrary.TestFramework.LogInformation($"   op1: {op1}");
                TestLibrary.TestFramework.LogInformation($"   op2: {op2}");
                TestLibrary.TestFramework.LogInformation($"   op3: {op3}");
                TestLibrary.TestFramework.LogInformation($"  result1: {ret1}");
                TestLibrary.TestFramework.LogInformation($"  result2: {ret2}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
