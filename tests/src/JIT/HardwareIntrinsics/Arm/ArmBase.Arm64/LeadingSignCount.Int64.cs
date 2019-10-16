// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static void LeadingSignCount_Int64()
        {
            var test = new ScalarUnaryOpTest__LeadingSignCount_Int64();

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

    public sealed unsafe class ScalarUnaryOpTest__LeadingSignCount_Int64
    {
        private struct TestStruct
        {
            public Int64 _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld = TestLibrary.Generator.GetInt64();
                return testStruct;
            }

            public void RunStructFldScenario(ScalarUnaryOpTest__LeadingSignCount_Int64 testClass)
            {
                var result = ArmBase.Arm64.LeadingSignCount(_fld);
                testClass.ValidateResult(_fld, result);
            }
        }

        private static Int64 _data;

        private static Int64 _clsVar;

        private Int64 _fld;

        static ScalarUnaryOpTest__LeadingSignCount_Int64()
        {
            _clsVar = TestLibrary.Generator.GetInt64();
        }

        public ScalarUnaryOpTest__LeadingSignCount_Int64()
        {
            Succeeded = true;

            
            _fld = TestLibrary.Generator.GetInt64();
            _data = TestLibrary.Generator.GetInt64();
        }

        public bool IsSupported => ArmBase.Arm64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = ArmBase.Arm64.LeadingSignCount(
                Unsafe.ReadUnaligned<Int64>(ref Unsafe.As<Int64, byte>(ref _data))
            );

            ValidateResult(_data, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(ArmBase.Arm64).GetMethod(nameof(ArmBase.Arm64.LeadingSignCount), new Type[] { typeof(Int64) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<Int64>(ref Unsafe.As<Int64, byte>(ref _data))
                                     });

            ValidateResult(_data, (Int32)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = ArmBase.Arm64.LeadingSignCount(
                _clsVar
            );

            ValidateResult(_clsVar, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data = Unsafe.ReadUnaligned<Int64>(ref Unsafe.As<Int64, byte>(ref _data));
            var result = ArmBase.Arm64.LeadingSignCount(data);

            ValidateResult(data, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarUnaryOpTest__LeadingSignCount_Int64();
            var result = ArmBase.Arm64.LeadingSignCount(test._fld);

            ValidateResult(test._fld, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = ArmBase.Arm64.LeadingSignCount(_fld);
            ValidateResult(_fld, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = ArmBase.Arm64.LeadingSignCount(test._fld);

            ValidateResult(test._fld, result);
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

        private void ValidateResult(Int64 data, Int32 result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            int expectedResult = 0; for (int index = 62; (((ulong)data >> index) & 1) == (((ulong)data >> 63) & 1); index--) { expectedResult++; } isUnexpectedResult = (expectedResult != result);

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(ArmBase.Arm64)}.{nameof(ArmBase.Arm64.LeadingSignCount)}<Int32>(Int64): LeadingSignCount failed:");
                TestLibrary.TestFramework.LogInformation($"    data: {data}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
