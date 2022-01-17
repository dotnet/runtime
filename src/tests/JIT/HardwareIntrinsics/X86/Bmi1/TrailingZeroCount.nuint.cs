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

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void TrailingZeroCountnuint()
        {
            var test = new ScalarUnaryOpTest__TrailingZeroCountnuint();

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

    public sealed unsafe class ScalarUnaryOpTest__TrailingZeroCountnuint
    {
        private struct TestStruct
        {
            public nuint _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld = TestLibrary.Generator.GetUIntPtr();
                return testStruct;
            }

            public void RunStructFldScenario(ScalarUnaryOpTest__TrailingZeroCountnuint testClass)
            {
                var result = Bmi1.TrailingZeroCount(_fld);
                testClass.ValidateResult(_fld, result);
            }
        }

        private static nuint _data;

        private static nuint _clsVar;

        private nuint _fld;

        static ScalarUnaryOpTest__TrailingZeroCountnuint()
        {
            _clsVar = TestLibrary.Generator.GetUIntPtr();
        }

        public ScalarUnaryOpTest__TrailingZeroCountnuint()
        {
            Succeeded = true;

            
            _fld = TestLibrary.Generator.GetUIntPtr();
            _data = TestLibrary.Generator.GetUIntPtr();
        }

        public bool IsSupported => Bmi1.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Bmi1.TrailingZeroCount(
                Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<nuint, byte>(ref _data))
            );

            ValidateResult(_data, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Bmi1).GetMethod(nameof(Bmi1.TrailingZeroCount), new Type[] { typeof(nuint) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<nuint, byte>(ref _data))
                                     });

            ValidateResult(_data, (nuint)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Bmi1.TrailingZeroCount(
                _clsVar
            );

            ValidateResult(_clsVar, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data = Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<nuint, byte>(ref _data));
            var result = Bmi1.TrailingZeroCount(data);

            ValidateResult(data, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarUnaryOpTest__TrailingZeroCountnuint();
            var result = Bmi1.TrailingZeroCount(test._fld);

            ValidateResult(test._fld, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Bmi1.TrailingZeroCount(_fld);
            ValidateResult(_fld, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Bmi1.TrailingZeroCount(test._fld);

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

        private void ValidateResult(nuint data, nuint result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            nuint expectedResult = 0; for (int index = 0; ((data >> index) & 1) == 0; index++) { expectedResult++; } isUnexpectedResult = (expectedResult != result);

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Bmi1)}.{nameof(Bmi1.TrailingZeroCount)}<nuint>(nuint): TrailingZeroCount failed:");
                TestLibrary.TestFramework.LogInformation($"    data: {data}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
