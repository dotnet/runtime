// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\X86\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace JIT.HardwareIntrinsics.X86._Sse2
{
    public static partial class Program
    {
        [Fact]
        public static void StoreScalarUInt32()
        {
            var test = new StoreUnaryOpTest__StoreScalarUInt32();

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

    public sealed unsafe class StoreUnaryOpTest__StoreScalarUInt32
    {
        private struct TestStruct
        {
            public Vector128<UInt32> _fld1;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt32(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt32>, byte>(ref testStruct._fld1), ref Unsafe.As<UInt32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<UInt32>>());

                return testStruct;
            }

            public void RunStructFldScenario(StoreUnaryOpTest__StoreScalarUInt32 testClass)
            {
                Sse2.StoreScalar((UInt32*)testClass._dataTable.outArrayPtr, _fld1);
                testClass.ValidateResult(_fld1, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<UInt32>>() / sizeof(UInt32);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<UInt32>>() / sizeof(UInt32);

        private static UInt32[] _data1 = new UInt32[Op1ElementCount];

        private static Vector128<UInt32> _clsVar1;

        private Vector128<UInt32> _fld1;

        private SimpleUnaryOpTest__DataTable<UInt32, UInt32> _dataTable;

        static StoreUnaryOpTest__StoreScalarUInt32()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt32>, byte>(ref _clsVar1), ref Unsafe.As<UInt32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<UInt32>>());
        }

        public StoreUnaryOpTest__StoreScalarUInt32()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt32(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<UInt32>, byte>(ref _fld1), ref Unsafe.As<UInt32, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<UInt32>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt32(); }
            _dataTable = new SimpleUnaryOpTest__DataTable<UInt32, UInt32>(_data1, new UInt32[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            Sse2.StoreScalar(
                (UInt32*)_dataTable.outArrayPtr,
                Unsafe.Read<Vector128<UInt32>>(_dataTable.inArrayPtr)
            );

            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            Sse2.StoreScalar(
                (UInt32*)_dataTable.outArrayPtr,
                Sse2.LoadVector128((UInt32*)(_dataTable.inArrayPtr))
            );

            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_LoadAligned));

            Sse2.StoreScalar(
                (UInt32*)_dataTable.outArrayPtr,
                Sse2.LoadAlignedVector128((UInt32*)(_dataTable.inArrayPtr))
            );

            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            typeof(Sse2).GetMethod(nameof(Sse2.StoreScalar), new Type[] { typeof(UInt32*), typeof(Vector128<UInt32>) })
                         .Invoke(null, new object[] {
                            Pointer.Box(_dataTable.outArrayPtr, typeof(UInt32*)),
                            Unsafe.Read<Vector128<UInt32>>(_dataTable.inArrayPtr)
                         });

            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            typeof(Sse2).GetMethod(nameof(Sse2.StoreScalar), new Type[] { typeof(UInt32*), typeof(Vector128<UInt32>) })
                         .Invoke(null, new object[] {
                            Pointer.Box(_dataTable.outArrayPtr, typeof(UInt32*)),
                            Sse2.LoadVector128((UInt32*)(_dataTable.inArrayPtr))
                         });

            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_LoadAligned));

            typeof(Sse2).GetMethod(nameof(Sse2.StoreScalar), new Type[] { typeof(UInt32*), typeof(Vector128<UInt32>) })
                         .Invoke(null, new object[] {
                            Pointer.Box(_dataTable.outArrayPtr, typeof(UInt32*)),
                            Sse2.LoadAlignedVector128((UInt32*)(_dataTable.inArrayPtr))
                         });

            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            Sse2.StoreScalar(
                (UInt32*)_dataTable.outArrayPtr,
                _clsVar1
            );

            ValidateResult(_clsVar1, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var value = Unsafe.Read<Vector128<UInt32>>(_dataTable.inArrayPtr);
            Sse2.StoreScalar((UInt32*)_dataTable.outArrayPtr, value);

            ValidateResult(value, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var value = Sse2.LoadVector128((UInt32*)(_dataTable.inArrayPtr));
            Sse2.StoreScalar((UInt32*)_dataTable.outArrayPtr, value);

            ValidateResult(value, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_LoadAligned));

            var value = Sse2.LoadAlignedVector128((UInt32*)(_dataTable.inArrayPtr));
            Sse2.StoreScalar((UInt32*)_dataTable.outArrayPtr, value);

            ValidateResult(value, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new StoreUnaryOpTest__StoreScalarUInt32();
            Sse2.StoreScalar((UInt32*)_dataTable.outArrayPtr, test._fld1);

            ValidateResult(test._fld1, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            Sse2.StoreScalar((UInt32*)_dataTable.outArrayPtr, _fld1);
            ValidateResult(_fld1, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            Sse2.StoreScalar((UInt32*)_dataTable.outArrayPtr, test._fld1);

            ValidateResult(test._fld1, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector128<UInt32> value, void* result, [CallerMemberName] string method = "")
        {
            UInt32[] inArray = new UInt32[Op1ElementCount];
            UInt32[] outArray = new UInt32[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt32, byte>(ref inArray[0]), value);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt32>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(void* value, void* result, [CallerMemberName] string method = "")
        {
            UInt32[] inArray = new UInt32[Op1ElementCount];
            UInt32[] outArray = new UInt32[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(value), (uint)Unsafe.SizeOf<Vector128<UInt32>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<UInt32>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(UInt32[] value, UInt32[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (value[0] != result[0])
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (0 != result[i])
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Sse2)}.{nameof(Sse2.StoreScalar)}<UInt32>(Vector128<UInt32>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"    value: ({string.Join(", ", value)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
