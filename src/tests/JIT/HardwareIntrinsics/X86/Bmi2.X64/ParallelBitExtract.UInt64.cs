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
        private static void ParallelBitExtractUInt64()
        {
            var test = new ScalarBinaryOpTest__ParallelBitExtractUInt64();

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

    public sealed unsafe class ScalarBinaryOpTest__ParallelBitExtractUInt64
    {
        private struct TestStruct
        {
            public UInt64 _fld1;
            public UInt64 _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld1 = TestLibrary.Generator.GetUInt64();
                testStruct._fld2 = TestLibrary.Generator.GetUInt64();

                return testStruct;
            }

            public void RunStructFldScenario(ScalarBinaryOpTest__ParallelBitExtractUInt64 testClass)
            {
                var result = Bmi2.X64.ParallelBitExtract(_fld1, _fld2);
                testClass.ValidateResult(_fld1, _fld2, result);
            }
        }

        private static UInt64 _data1;
        private static UInt64 _data2;

        private static UInt64 _clsVar1;
        private static UInt64 _clsVar2;

        private UInt64 _fld1;
        private UInt64 _fld2;

        static ScalarBinaryOpTest__ParallelBitExtractUInt64()
        {
            _clsVar1 = TestLibrary.Generator.GetUInt64();
            _clsVar2 = TestLibrary.Generator.GetUInt64();
        }

        public ScalarBinaryOpTest__ParallelBitExtractUInt64()
        {
            Succeeded = true;

            _fld1 = TestLibrary.Generator.GetUInt64();
            _fld2 = TestLibrary.Generator.GetUInt64();

            _data1 = TestLibrary.Generator.GetUInt64();
            _data2 = TestLibrary.Generator.GetUInt64();
        }

        public bool IsSupported => Bmi2.X64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Bmi2.X64.ParallelBitExtract(
                Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1)),
                Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data2))
            );

            ValidateResult(_data1, _data2, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Bmi2.X64).GetMethod(nameof(Bmi2.X64.ParallelBitExtract), new Type[] { typeof(UInt64), typeof(UInt64) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1)),
                                        Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data2))
                                     });

            ValidateResult(_data1, _data2, (UInt64)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Bmi2.X64.ParallelBitExtract(
                _clsVar1,
                _clsVar2
            );

            ValidateResult(_clsVar1, _clsVar2, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data1 = Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1));
            var data2 = Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data2));
            var result = Bmi2.X64.ParallelBitExtract(data1, data2);

            ValidateResult(data1, data2, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarBinaryOpTest__ParallelBitExtractUInt64();
            var result = Bmi2.X64.ParallelBitExtract(test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Bmi2.X64.ParallelBitExtract(_fld1, _fld2);
            ValidateResult(_fld1, _fld2, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Bmi2.X64.ParallelBitExtract(test._fld1, test._fld2);

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

        private void ValidateResult(UInt64 left, UInt64 right, UInt64 result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            
// The validation logic defined here for Bmi2.ParallelBitDeposit and Bmi2.ParallelBitExtract is
// based on the 'Operation' pseudo-code defined for the pdep and pext instruction in the 'Intel®
// 64 and IA-32 Architectures Software Developer’s Manual; Volume 2 (2A, 2B, 2C & 2D): Instruction
// Set Reference, A-Z'

ulong temp = left;
ulong mask = right;
ulong dest = 0;
byte m = 0, k = 0;

while (m < 64)
{
    if (((mask >> m) & 1) == 1) // Extract bit at index m of mask
    {
        dest |= (((temp >> m) & 1) << k); // Extract bit at index m of temp and insert to index k of dest
        k++;
    }
    m++;
}

isUnexpectedResult = (dest != result);


            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Bmi2.X64)}.{nameof(Bmi2.X64.ParallelBitExtract)}<UInt64>(UInt64, UInt64): ParallelBitExtract failed:");
                TestLibrary.TestFramework.LogInformation($"    left: {left}");
                TestLibrary.TestFramework.LogInformation($"   right: {right}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
