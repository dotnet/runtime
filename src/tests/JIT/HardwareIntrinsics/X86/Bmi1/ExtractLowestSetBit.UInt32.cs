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
        private static void ExtractLowestSetBitUInt32()
        {
            var test = new ScalarUnaryOpTest__ExtractLowestSetBitUInt32();

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

    public sealed unsafe class ScalarUnaryOpTest__ExtractLowestSetBitUInt32
    {
        private struct TestStruct
        {
            public UInt32 _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld = TestLibrary.Generator.GetUInt32();
                return testStruct;
            }

            public void RunStructFldScenario(ScalarUnaryOpTest__ExtractLowestSetBitUInt32 testClass)
            {
                var result = Bmi1.ExtractLowestSetBit(_fld);
                testClass.ValidateResult(_fld, result);
            }
        }

        private static UInt32 _data;

        private static UInt32 _clsVar;

        private UInt32 _fld;

        static ScalarUnaryOpTest__ExtractLowestSetBitUInt32()
        {
            _clsVar = TestLibrary.Generator.GetUInt32();
        }

        public ScalarUnaryOpTest__ExtractLowestSetBitUInt32()
        {
            Succeeded = true;

            
            _fld = TestLibrary.Generator.GetUInt32();
            _data = TestLibrary.Generator.GetUInt32();
        }

        public bool IsSupported => Bmi1.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Bmi1.ExtractLowestSetBit(
                Unsafe.ReadUnaligned<UInt32>(ref Unsafe.As<UInt32, byte>(ref _data))
            );

            ValidateResult(_data, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Bmi1).GetMethod(nameof(Bmi1.ExtractLowestSetBit), new Type[] { typeof(UInt32) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<UInt32>(ref Unsafe.As<UInt32, byte>(ref _data))
                                     });

            ValidateResult(_data, (UInt32)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Bmi1.ExtractLowestSetBit(
                _clsVar
            );

            ValidateResult(_clsVar, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data = Unsafe.ReadUnaligned<UInt32>(ref Unsafe.As<UInt32, byte>(ref _data));
            var result = Bmi1.ExtractLowestSetBit(data);

            ValidateResult(data, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarUnaryOpTest__ExtractLowestSetBitUInt32();
            var result = Bmi1.ExtractLowestSetBit(test._fld);

            ValidateResult(test._fld, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Bmi1.ExtractLowestSetBit(_fld);
            ValidateResult(_fld, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Bmi1.ExtractLowestSetBit(test._fld);

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

        private void ValidateResult(UInt32 data, UInt32 result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            isUnexpectedResult = ((unchecked((uint)(-(int)data)) & data) != result);

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Bmi1)}.{nameof(Bmi1.ExtractLowestSetBit)}<UInt32>(UInt32): ExtractLowestSetBit failed:");
                TestLibrary.TestFramework.LogInformation($"    data: {data}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
