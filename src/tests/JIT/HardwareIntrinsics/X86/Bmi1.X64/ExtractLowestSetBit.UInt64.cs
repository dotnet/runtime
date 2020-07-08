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
        private static void ExtractLowestSetBitUInt64()
        {
            var test = new ScalarUnaryOpTest__ExtractLowestSetBitUInt64();

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

    public sealed unsafe class ScalarUnaryOpTest__ExtractLowestSetBitUInt64
    {
        private struct TestStruct
        {
            public UInt64 _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld = TestLibrary.Generator.GetUInt64();
                return testStruct;
            }

            public void RunStructFldScenario(ScalarUnaryOpTest__ExtractLowestSetBitUInt64 testClass)
            {
                var result = Bmi1.X64.ExtractLowestSetBit(_fld);
                testClass.ValidateResult(_fld, result);
            }
        }

        private static UInt64 _data;

        private static UInt64 _clsVar;

        private UInt64 _fld;

        static ScalarUnaryOpTest__ExtractLowestSetBitUInt64()
        {
            _clsVar = TestLibrary.Generator.GetUInt64();
        }

        public ScalarUnaryOpTest__ExtractLowestSetBitUInt64()
        {
            Succeeded = true;

            
            _fld = TestLibrary.Generator.GetUInt64();
            _data = TestLibrary.Generator.GetUInt64();
        }

        public bool IsSupported => Bmi1.X64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Bmi1.X64.ExtractLowestSetBit(
                Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data))
            );

            ValidateResult(_data, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Bmi1.X64).GetMethod(nameof(Bmi1.X64.ExtractLowestSetBit), new Type[] { typeof(UInt64) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data))
                                     });

            ValidateResult(_data, (UInt64)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Bmi1.X64.ExtractLowestSetBit(
                _clsVar
            );

            ValidateResult(_clsVar, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data = Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data));
            var result = Bmi1.X64.ExtractLowestSetBit(data);

            ValidateResult(data, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarUnaryOpTest__ExtractLowestSetBitUInt64();
            var result = Bmi1.X64.ExtractLowestSetBit(test._fld);

            ValidateResult(test._fld, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Bmi1.X64.ExtractLowestSetBit(_fld);
            ValidateResult(_fld, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Bmi1.X64.ExtractLowestSetBit(test._fld);

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

        private void ValidateResult(UInt64 data, UInt64 result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            isUnexpectedResult = ((unchecked((ulong)(-(long)data)) & data) != result);

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Bmi1.X64)}.{nameof(Bmi1.X64.ExtractLowestSetBit)}<UInt64>(UInt64): ExtractLowestSetBit failed:");
                TestLibrary.TestFramework.LogInformation($"    data: {data}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
