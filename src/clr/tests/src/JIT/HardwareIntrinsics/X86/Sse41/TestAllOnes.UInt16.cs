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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        private static void TestAllOnesUInt16()
        {
            var test = new BooleanUnaryOpTest__TestAllOnesUInt16();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates calling via reflection works, using Load
                    test.RunReflectionScenario_Load();

                    // Validates calling via reflection works, using LoadAligned
                    test.RunReflectionScenario_LoadAligned();
                }

                // Validates passing a static member works
                test.RunClsVarScenario();

                // Validates passing a local works, using Unsafe.Read
                test.RunLclVarScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates passing a local works, using Load
                    test.RunLclVarScenario_Load();

                    // Validates passing a local works, using LoadAligned
                    test.RunLclVarScenario_LoadAligned();
                }

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

    public sealed unsafe class BooleanUnaryOpTest__TestAllOnesUInt16
    {
        private struct TestStruct
        {
            public Vector128<UInt16> _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref testStruct._fld), ref Unsafe.As<UInt16, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

                return testStruct;
            }

            public void RunStructFldScenario(BooleanUnaryOpTest__TestAllOnesUInt16 testClass)
            {
                var result = Sse41.TestAllOnes(_fld);
                testClass.ValidateResult(_fld, result);
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);

        private static UInt16[] _data = new UInt16[Op1ElementCount];

        private static Vector128<UInt16> _clsVar;

        private Vector128<UInt16> _fld;

        private BooleanUnaryOpTest__DataTable<UInt16> _dataTable;

        static BooleanUnaryOpTest__TestAllOnesUInt16()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _clsVar), ref Unsafe.As<UInt16, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());
        }

        public BooleanUnaryOpTest__TestAllOnesUInt16()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt16>, byte>(ref _fld), ref Unsafe.As<UInt16, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetUInt16(); }
            _dataTable = new BooleanUnaryOpTest__DataTable<UInt16>(_data, LargestVectorSize);
        }

        public bool IsSupported => Sse41.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Sse41.TestAllOnes(
                Unsafe.Read<Vector128<UInt16>>(_dataTable.inArrayPtr)
            );

            ValidateResult(_dataTable.inArrayPtr, result);
        }

        public void RunBasicScenario_Load()
        {
            var result = Sse41.TestAllOnes(
                Sse2.LoadVector128((UInt16*)(_dataTable.inArrayPtr))
            );

            ValidateResult(_dataTable.inArrayPtr, result);
        }

        public void RunBasicScenario_LoadAligned()
        {
            var result = Sse41.TestAllOnes(
                Sse2.LoadAlignedVector128((UInt16*)(_dataTable.inArrayPtr))
            );

            ValidateResult(_dataTable.inArrayPtr, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.TestAllOnes), new Type[] { typeof(Vector128<UInt16>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<UInt16>>(_dataTable.inArrayPtr)
                                     });

            ValidateResult(_dataTable.inArrayPtr, (bool)(result));
        }

        public void RunReflectionScenario_Load()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.TestAllOnes), new Type[] { typeof(Vector128<UInt16>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((UInt16*)(_dataTable.inArrayPtr))
                                     });

            ValidateResult(_dataTable.inArrayPtr, (bool)(result));
        }

        public void RunReflectionScenario_LoadAligned()
        {
            var result = typeof(Sse41).GetMethod(nameof(Sse41.TestAllOnes), new Type[] { typeof(Vector128<UInt16>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((UInt16*)(_dataTable.inArrayPtr))
                                     });

            ValidateResult(_dataTable.inArrayPtr, (bool)(result));
        }

        public void RunClsVarScenario()
        {
            var result = Sse41.TestAllOnes(
                _clsVar
            );

            ValidateResult(_clsVar, result);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            var value = Unsafe.Read<Vector128<UInt16>>(_dataTable.inArrayPtr);
            var result = Sse41.TestAllOnes(value);

            ValidateResult(value, result);
        }

        public void RunLclVarScenario_Load()
        {
            var value = Sse2.LoadVector128((UInt16*)(_dataTable.inArrayPtr));
            var result = Sse41.TestAllOnes(value);

            ValidateResult(value, result);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            var value = Sse2.LoadAlignedVector128((UInt16*)(_dataTable.inArrayPtr));
            var result = Sse41.TestAllOnes(value);

            ValidateResult(value, result);
        }

        public void RunClassLclFldScenario()
        {
            var test = new BooleanUnaryOpTest__TestAllOnesUInt16();
            var result = Sse41.TestAllOnes(test._fld);

            ValidateResult(test._fld, result);
        }

        public void RunClassFldScenario()
        {
            var result = Sse41.TestAllOnes(_fld);

            ValidateResult(_fld, result);
        }

        public void RunStructLclFldScenario()
        {
            var test = TestStruct.Create();
            var result = Sse41.TestAllOnes(test._fld);
            ValidateResult(test._fld, result);
        }

        public void RunStructFldScenario()
        {
            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        public void RunUnsupportedScenario()
        {
            Succeeded = false;

            try
            {
                RunBasicScenario_UnsafeRead();
            }
            catch (PlatformNotSupportedException)
            {
                Succeeded = true;
            }
        }

        private void ValidateResult(Vector128<UInt16> value, bool result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray = new UInt16[Op1ElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray[0]), value);

            ValidateResult(inArray, result, method);
        }

        private void ValidateResult(void* value, bool result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray = new UInt16[Op1ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(value), (uint)Unsafe.SizeOf<Vector128<UInt16>>());

            ValidateResult(inArray, result, method);
        }

        private void ValidateResult(UInt16[] value, bool result, [CallerMemberName] string method = "")
        {
            var expectedResult = true;

            for (var i = 0; i < Op1ElementCount; i++)
            {
                expectedResult &= ((~value[i] & ushort.MaxValue) == 0);
            }

            if (expectedResult != result)
            {
                Succeeded = false;

                TestLibrary.TestFramework.LogInformation($"{nameof(Sse41)}.{nameof(Sse41.TestAllOnes)}<UInt16>(Vector128<UInt16>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"    value: ({string.Join(", ", value)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }
    }
}
