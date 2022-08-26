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
using Xunit;

namespace JIT.HardwareIntrinsics.Arm._ArmBase
{
    public static partial class Program
    {
        [Fact]
        public static void ReverseElementBits_Int32()
        {
            var test = new ScalarUnaryOpTest__ReverseElementBits_Int32();

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

    public sealed unsafe class ScalarUnaryOpTest__ReverseElementBits_Int32
    {
        private struct TestStruct
        {
            public Int32 _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld = TestLibrary.Generator.GetInt32();
                return testStruct;
            }

            public void RunStructFldScenario(ScalarUnaryOpTest__ReverseElementBits_Int32 testClass)
            {
                var result = ArmBase.ReverseElementBits(_fld);
                testClass.ValidateResult(_fld, result);
            }
        }

        private static Int32 _data;

        private static Int32 _clsVar;

        private Int32 _fld;

        static ScalarUnaryOpTest__ReverseElementBits_Int32()
        {
            _clsVar = TestLibrary.Generator.GetInt32();
        }

        public ScalarUnaryOpTest__ReverseElementBits_Int32()
        {
            Succeeded = true;

            
            _fld = TestLibrary.Generator.GetInt32();
            _data = TestLibrary.Generator.GetInt32();
        }

        public bool IsSupported => ArmBase.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = ArmBase.ReverseElementBits(
                Unsafe.ReadUnaligned<Int32>(ref Unsafe.As<Int32, byte>(ref _data))
            );

            ValidateResult(_data, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(ArmBase).GetMethod(nameof(ArmBase.ReverseElementBits), new Type[] { typeof(Int32) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<Int32>(ref Unsafe.As<Int32, byte>(ref _data))
                                     });

            ValidateResult(_data, (Int32)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = ArmBase.ReverseElementBits(
                _clsVar
            );

            ValidateResult(_clsVar, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data = Unsafe.ReadUnaligned<Int32>(ref Unsafe.As<Int32, byte>(ref _data));
            var result = ArmBase.ReverseElementBits(data);

            ValidateResult(data, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarUnaryOpTest__ReverseElementBits_Int32();
            var result = ArmBase.ReverseElementBits(test._fld);

            ValidateResult(test._fld, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = ArmBase.ReverseElementBits(_fld);
            ValidateResult(_fld, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = ArmBase.ReverseElementBits(test._fld);

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

        private void ValidateResult(Int32 data, Int32 result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            isUnexpectedResult = Helpers.ReverseElementBits(data) != result;

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(ArmBase)}.{nameof(ArmBase.ReverseElementBits)}<Int32>(Int32): ReverseElementBits failed:");
                TestLibrary.TestFramework.LogInformation($"    data: {data}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
