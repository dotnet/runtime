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
        private static void AndNotnuint()
        {
            var test = new ScalarBinaryOpTest__AndNotnuint();

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

    public sealed unsafe class ScalarBinaryOpTest__AndNotnuint
    {
        private struct TestStruct
        {
            public nuint _fld1;
            public nuint _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld1 = TestLibrary.Generator.GetUIntPtr();
                testStruct._fld2 = TestLibrary.Generator.GetUIntPtr();

                return testStruct;
            }

            public void RunStructFldScenario(ScalarBinaryOpTest__AndNotnuint testClass)
            {
                var result = Bmi1.AndNot(_fld1, _fld2);
                testClass.ValidateResult(_fld1, _fld2, result);
            }
        }

        private static nuint _data1;
        private static nuint _data2;

        private static nuint _clsVar1;
        private static nuint _clsVar2;

        private nuint _fld1;
        private nuint _fld2;

        static ScalarBinaryOpTest__AndNotnuint()
        {
            _clsVar1 = TestLibrary.Generator.GetUIntPtr();
            _clsVar2 = TestLibrary.Generator.GetUIntPtr();
        }

        public ScalarBinaryOpTest__AndNotnuint()
        {
            Succeeded = true;

            _fld1 = TestLibrary.Generator.GetUIntPtr();
            _fld2 = TestLibrary.Generator.GetUIntPtr();

            _data1 = TestLibrary.Generator.GetUIntPtr();
            _data2 = TestLibrary.Generator.GetUIntPtr();
        }

        public bool IsSupported => Bmi1.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Bmi1.AndNot(
                Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<nuint, byte>(ref _data1)),
                Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<nuint, byte>(ref _data2))
            );

            ValidateResult(_data1, _data2, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Bmi1).GetMethod(nameof(Bmi1.AndNot), new Type[] { typeof(nuint), typeof(nuint) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<nuint, byte>(ref _data1)),
                                        Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<nuint, byte>(ref _data2))
                                     });

            ValidateResult(_data1, _data2, (nuint)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Bmi1.AndNot(
                _clsVar1,
                _clsVar2
            );

            ValidateResult(_clsVar1, _clsVar2, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data1 = Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<nuint, byte>(ref _data1));
            var data2 = Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<nuint, byte>(ref _data2));
            var result = Bmi1.AndNot(data1, data2);

            ValidateResult(data1, data2, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarBinaryOpTest__AndNotnuint();
            var result = Bmi1.AndNot(test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Bmi1.AndNot(_fld1, _fld2);
            ValidateResult(_fld1, _fld2, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Bmi1.AndNot(test._fld1, test._fld2);

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

        private void ValidateResult(nuint left, nuint right, nuint result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            isUnexpectedResult = ((~left & right) != result);

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Bmi1)}.{nameof(Bmi1.AndNot)}<nuint>(nuint, nuint): AndNot failed:");
                TestLibrary.TestFramework.LogInformation($"    left: {left}");
                TestLibrary.TestFramework.LogInformation($"   right: {right}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
