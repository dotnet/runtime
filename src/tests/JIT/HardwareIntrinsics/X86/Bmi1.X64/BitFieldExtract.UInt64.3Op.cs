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
        private static void BitFieldExtractUInt643Op()
        {
            var test = new ScalarTernOpTest__BitFieldExtractUInt64();

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

    public sealed unsafe class ScalarTernOpTest__BitFieldExtractUInt64
    {
        private struct TestStruct
        {
            public UInt64 _fld1;
            public Byte _fld2;
            public Byte _fld3;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld1 = 0x1E00000000000000;
                testStruct._fld2 = 57;
                testStruct._fld3 = 4;

                return testStruct;
            }

            public void RunStructFldScenario(ScalarTernOpTest__BitFieldExtractUInt64 testClass)
            {
                var result = Bmi1.X64.BitFieldExtract(_fld1, _fld2, _fld3);
                testClass.ValidateResult(_fld1, _fld2, _fld3, result);
            }
        }

        private static UInt64 _data1;
        private static Byte _data2;
        private static Byte _data3;

        private static UInt64 _clsVar1;
        private static Byte _clsVar2;
        private static Byte _clsVar3;

        private UInt64 _fld1;
        private Byte _fld2;
        private Byte _fld3;

        static ScalarTernOpTest__BitFieldExtractUInt64()
        {
            _clsVar1 = 0x1E00000000000000;
            _clsVar2 = 57;
            _clsVar3 = 4;
        }

        public ScalarTernOpTest__BitFieldExtractUInt64()
        {
            Succeeded = true;

            _fld1 = 0x1E00000000000000;
            _fld2 = 57;
            _fld3 = 4;

            _data1 = 0x1E00000000000000;
            _data2 = 57;
            _data3 = 4;
        }

        public bool IsSupported => Bmi1.X64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Bmi1.X64.BitFieldExtract(
                Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1)),
                Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data2)),
                Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data3))
            );

            ValidateResult(_data1, _data2, _data3, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Bmi1.X64).GetMethod(nameof(Bmi1.X64.BitFieldExtract), new Type[] { typeof(UInt64), typeof(Byte), typeof(Byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<UInt64>(ref Unsafe.As<UInt64, byte>(ref _data1)),
                                        Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data2)),
                                        Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data3))
                                     });

            ValidateResult(_data1, _data2, _data3, (UInt64)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Bmi1.X64.BitFieldExtract(
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
            var data2 = Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data2));
            var data3 = Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data3));
            var result = Bmi1.X64.BitFieldExtract(data1, data2, data3);

            ValidateResult(data1, data2, data3, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarTernOpTest__BitFieldExtractUInt64();
            var result = Bmi1.X64.BitFieldExtract(test._fld1, test._fld2, test._fld3);

            ValidateResult(test._fld1, test._fld2, test._fld3, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Bmi1.X64.BitFieldExtract(_fld1, _fld2, _fld3);
            ValidateResult(_fld1, _fld2, _fld3, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Bmi1.X64.BitFieldExtract(test._fld1, test._fld2, test._fld3);

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

        private void ValidateResult(UInt64 op1, Byte op2, Byte op3, UInt64 result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            ulong expectedResult = 15; isUnexpectedResult = (expectedResult != result);

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Bmi1.X64)}.{nameof(Bmi1.X64.BitFieldExtract)}<UInt64>(UInt64, Byte, Byte): BitFieldExtract failed:");
                TestLibrary.TestFramework.LogInformation($"   op1: {op1}");
                TestLibrary.TestFramework.LogInformation($"   op2: {op2}");
                TestLibrary.TestFramework.LogInformation($"   op3: {op3}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
