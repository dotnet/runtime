// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\Arm\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        private static void MultiplyHigh_Int64()
        {
            var test = new ScalarBinaryOpTest__MultiplyHigh_Int64();

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

    public sealed unsafe class ScalarBinaryOpTest__MultiplyHigh_Int64
    {
        private struct TestStruct
        {
            public Int64 _fld1;
            public Int64 _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld1 = -TestLibrary.Generator.GetInt64();
                testStruct._fld2 = -TestLibrary.Generator.GetInt64();

                return testStruct;
            }

            public void RunStructFldScenario(ScalarBinaryOpTest__MultiplyHigh_Int64 testClass)
            {
                var result = ArmBase.Arm64.MultiplyHigh(_fld1, _fld2);
                testClass.ValidateResult(_fld1, _fld2, result);
            }
        }

        private static Int64 _data1;
        private static Int64 _data2;

        private static Int64 _clsVar1;
        private static Int64 _clsVar2;

        private Int64 _fld1;
        private Int64 _fld2;

        static ScalarBinaryOpTest__MultiplyHigh_Int64()
        {
            _clsVar1 = -TestLibrary.Generator.GetInt64();
            _clsVar2 = -TestLibrary.Generator.GetInt64();
        }

        public ScalarBinaryOpTest__MultiplyHigh_Int64()
        {
            Succeeded = true;

            _fld1 = -TestLibrary.Generator.GetInt64();
            _fld2 = -TestLibrary.Generator.GetInt64();

            _data1 = -TestLibrary.Generator.GetInt64();
            _data2 = -TestLibrary.Generator.GetInt64();
        }

        public bool IsSupported => ArmBase.Arm64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = ArmBase.Arm64.MultiplyHigh(
                Unsafe.ReadUnaligned<Int64>(ref Unsafe.As<Int64, byte>(ref _data1)),
                Unsafe.ReadUnaligned<Int64>(ref Unsafe.As<Int64, byte>(ref _data2))
            );

            ValidateResult(_data1, _data2, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(ArmBase.Arm64).GetMethod(nameof(ArmBase.Arm64.MultiplyHigh), new Type[] { typeof(Int64), typeof(Int64) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<Int64>(ref Unsafe.As<Int64, byte>(ref _data1)),
                                        Unsafe.ReadUnaligned<Int64>(ref Unsafe.As<Int64, byte>(ref _data2))
                                     });

            ValidateResult(_data1, _data2, (Int64)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = ArmBase.Arm64.MultiplyHigh(
                _clsVar1,
                _clsVar2
            );

            ValidateResult(_clsVar1, _clsVar2, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data1 = Unsafe.ReadUnaligned<Int64>(ref Unsafe.As<Int64, byte>(ref _data1));
            var data2 = Unsafe.ReadUnaligned<Int64>(ref Unsafe.As<Int64, byte>(ref _data2));
            var result = ArmBase.Arm64.MultiplyHigh(data1, data2);

            ValidateResult(data1, data2, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarBinaryOpTest__MultiplyHigh_Int64();
            var result = ArmBase.Arm64.MultiplyHigh(test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = ArmBase.Arm64.MultiplyHigh(_fld1, _fld2);
            ValidateResult(_fld1, _fld2, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = ArmBase.Arm64.MultiplyHigh(test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, result);
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

        private void ValidateResult(Int64 left, Int64 right, Int64 result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            isUnexpectedResult = Helpers.MultiplyHigh(left, right) != result;

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(ArmBase.Arm64)}.{nameof(ArmBase.Arm64.MultiplyHigh)}<Int64>(Int64, Int64): MultiplyHigh failed:");
                TestLibrary.TestFramework.LogInformation($"    left: {left}");
                TestLibrary.TestFramework.LogInformation($"   right: {right}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
