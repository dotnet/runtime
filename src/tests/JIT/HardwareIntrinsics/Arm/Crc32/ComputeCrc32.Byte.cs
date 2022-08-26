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

namespace JIT.HardwareIntrinsics.Arm._Crc32
{
    public static partial class Program
    {
        [Fact]
        public static void ComputeCrc32_Byte()
        {
            var test = new ScalarBinaryOpTest__ComputeCrc32_Byte();

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

    public sealed unsafe class ScalarBinaryOpTest__ComputeCrc32_Byte
    {
        private struct TestStruct
        {
            public UInt32 _fld1;
            public Byte _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                testStruct._fld1 = 0xFFFFFFFF;
                testStruct._fld2 = 0x20;

                return testStruct;
            }

            public void RunStructFldScenario(ScalarBinaryOpTest__ComputeCrc32_Byte testClass)
            {
                var result = Crc32.ComputeCrc32(_fld1, _fld2);
                testClass.ValidateResult(_fld1, _fld2, result);
            }
        }

        private static UInt32 _data1;
        private static Byte _data2;

        private static UInt32 _clsVar1;
        private static Byte _clsVar2;

        private UInt32 _fld1;
        private Byte _fld2;

        static ScalarBinaryOpTest__ComputeCrc32_Byte()
        {
            _clsVar1 = 0xFFFFFFFF;
            _clsVar2 = 0x20;
        }

        public ScalarBinaryOpTest__ComputeCrc32_Byte()
        {
            Succeeded = true;

            _fld1 = 0xFFFFFFFF;
            _fld2 = 0x20;

            _data1 = 0xFFFFFFFF;
            _data2 = 0x20;
        }

        public bool IsSupported => Crc32.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Crc32.ComputeCrc32(
                Unsafe.ReadUnaligned<UInt32>(ref Unsafe.As<UInt32, byte>(ref _data1)),
                Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data2))
            );

            ValidateResult(_data1, _data2, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Crc32).GetMethod(nameof(Crc32.ComputeCrc32), new Type[] { typeof(UInt32), typeof(Byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.ReadUnaligned<UInt32>(ref Unsafe.As<UInt32, byte>(ref _data1)),
                                        Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data2))
                                     });

            ValidateResult(_data1, _data2, (UInt32)result);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Crc32.ComputeCrc32(
                _clsVar1,
                _clsVar2
            );

            ValidateResult(_clsVar1, _clsVar2, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var data1 = Unsafe.ReadUnaligned<UInt32>(ref Unsafe.As<UInt32, byte>(ref _data1));
            var data2 = Unsafe.ReadUnaligned<Byte>(ref Unsafe.As<Byte, byte>(ref _data2));
            var result = Crc32.ComputeCrc32(data1, data2);

            ValidateResult(data1, data2, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new ScalarBinaryOpTest__ComputeCrc32_Byte();
            var result = Crc32.ComputeCrc32(test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Crc32.ComputeCrc32(_fld1, _fld2);
            ValidateResult(_fld1, _fld2, result);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Crc32.ComputeCrc32(test._fld1, test._fld2);

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

        private void ValidateResult(UInt32 left, Byte right, UInt32 result, [CallerMemberName] string method = "")
        {
            var isUnexpectedResult = false;

            uint expectedResult = 0x169330BA; isUnexpectedResult = (expectedResult != result);

            if (isUnexpectedResult)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Crc32)}.{nameof(Crc32.ComputeCrc32)}<UInt32>(UInt32, Byte): ComputeCrc32 failed:");
                TestLibrary.TestFramework.LogInformation($"    left: {left}");
                TestLibrary.TestFramework.LogInformation($"   right: {right}");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
