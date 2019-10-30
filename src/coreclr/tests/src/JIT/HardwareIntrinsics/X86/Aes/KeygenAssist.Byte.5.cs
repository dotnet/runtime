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
        private static void KeygenAssistByte5()
        {
            var test = new AesImmOpTest__KeygenAssistByte5();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Aes.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (Aes.IsSupported)
                {
                    // Validates calling via reflection works, using Load
                    test.RunReflectionScenario_Load();

                    // Validates calling via reflection works, using LoadAligned
                    test.RunReflectionScenario_LoadAligned();
                }

                // Validates passing a static member works
                test.RunClsVarScenario();

                if (Aes.IsSupported)
                {
                    // Validates passing a static member works, using pinning and Load
                    test.RunClsVarScenario_Load();
                }

                // Validates passing a local works, using Unsafe.Read
                test.RunLclVarScenario_UnsafeRead();

                if (Aes.IsSupported)
                {
                    // Validates passing a local works, using Load
                    test.RunLclVarScenario_Load();

                    // Validates passing a local works, using LoadAligned
                    test.RunLclVarScenario_LoadAligned();
                }

                // Validates passing the field of a local class works
                test.RunClassLclFldScenario();

                if (Aes.IsSupported)
                {
                    // Validates passing the field of a local class works, using pinning and Load
                    test.RunClassLclFldScenario_Load();
                }

                // Validates passing an instance member of a class works
                test.RunClassFldScenario();

                if (Aes.IsSupported)
                {
                    // Validates passing an instance member of a class works, using pinning and Load
                    test.RunClassFldScenario_Load();
                }

                // Validates passing the field of a local struct works
                test.RunStructLclFldScenario();

                if (Aes.IsSupported)
                {
                    // Validates passing the field of a local struct works, using pinning and Load
                    test.RunStructLclFldScenario_Load();
                }

                // Validates passing an instance member of a struct works
                test.RunStructFldScenario();

                if (Aes.IsSupported)
                {
                    // Validates passing an instance member of a struct works, using pinning and Load
                    test.RunStructFldScenario_Load();
                }
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

    public sealed unsafe class AesImmOpTest__KeygenAssistByte5
    {
        private struct TestStruct
        {
            public Vector128<Byte> _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref testStruct._fld), ref Unsafe.As<Byte, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());

                return testStruct;
            }

            public void RunStructFldScenario(AesImmOpTest__KeygenAssistByte5 testClass)
            {
                var result = Aes.KeygenAssist(_fld, 5);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(testClass._dataTable.outArrayPtr);
            }

            public void RunStructFldScenario_Load(AesImmOpTest__KeygenAssistByte5 testClass)
            {
                fixed (Vector128<Byte>* pFld = &_fld)
                {
                    var result = Aes.KeygenAssist(
                        Aes.LoadVector128((Byte*)(pFld)),
                        5
                    );

                    Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                    testClass.ValidateResult(testClass._dataTable.outArrayPtr);
                }
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<Byte>>() / sizeof(Byte);

        private static Byte[] _data = new Byte[16] {0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01, 0xff, 0xee, 0xdd, 0xcc, 0xbb, 0xaa, 0x99, 0x88};
        private static Byte[] _expectedRet = new Byte[16] {0x85, 0x6e, 0x26, 0x7c, 0x6b, 0x26, 0x7c, 0x85, 0xea, 0xac, 0xee, 0xc4, 0xa9, 0xee, 0xc4, 0xea};

        private static Vector128<Byte> _clsVar;

        private Vector128<Byte> _fld;

        private SimpleUnaryOpTest__DataTable<Byte, Byte> _dataTable;

        static AesImmOpTest__KeygenAssistByte5()
        {

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _clsVar), ref Unsafe.As<Byte, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());
        }

        public AesImmOpTest__KeygenAssistByte5()
        {
            Succeeded = true;

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Byte>, byte>(ref _fld), ref Unsafe.As<Byte, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<Byte>>());


            _dataTable = new SimpleUnaryOpTest__DataTable<Byte, Byte>(_data, new Byte[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Aes.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Aes.KeygenAssist(
                Unsafe.Read<Vector128<Byte>>(_dataTable.inArrayPtr),
                5
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = Aes.KeygenAssist(
                Aes.LoadVector128((Byte*)(_dataTable.inArrayPtr)),
                5
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_LoadAligned));

            var result = Aes.KeygenAssist(
                Aes.LoadAlignedVector128((Byte*)(_dataTable.inArrayPtr)),
                5
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Aes).GetMethod(nameof(Aes.KeygenAssist), new Type[] { typeof(Vector128<Byte>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Byte>>(_dataTable.inArrayPtr),
                                        (byte)5
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Byte>)(result));
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(Aes).GetMethod(nameof(Aes.KeygenAssist), new Type[] { typeof(Vector128<Byte>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Aes.LoadVector128((Byte*)(_dataTable.inArrayPtr)),
                                        (byte)5
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Byte>)(result));
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_LoadAligned));

            var result = typeof(Aes).GetMethod(nameof(Aes.KeygenAssist), new Type[] { typeof(Vector128<Byte>), typeof(byte) })
                                     .Invoke(null, new object[] {
                                        Aes.LoadAlignedVector128((Byte*)(_dataTable.inArrayPtr)),
                                        (byte)5
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Byte>)(result));
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Aes.KeygenAssist(
                _clsVar,
                5
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector128<Byte>* pClsVar = &_clsVar)
            {
                var result = Aes.KeygenAssist(
                    Aes.LoadVector128((Byte*)(pClsVar)),
                    5
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_dataTable.outArrayPtr);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var firstOp = Unsafe.Read<Vector128<Byte>>(_dataTable.inArrayPtr);
            var result = Aes.KeygenAssist(firstOp, 5);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var firstOp = Aes.LoadVector128((Byte*)(_dataTable.inArrayPtr));
            var result = Aes.KeygenAssist(firstOp, 5);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_LoadAligned));

            var firstOp = Aes.LoadAlignedVector128((Byte*)(_dataTable.inArrayPtr));
            var result = Aes.KeygenAssist(firstOp, 5);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new AesImmOpTest__KeygenAssistByte5();
            var result = Aes.KeygenAssist(test._fld, 5);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new AesImmOpTest__KeygenAssistByte5();

            fixed (Vector128<Byte>* pFld = &test._fld)
            {
                var result = Aes.KeygenAssist(
                    Aes.LoadVector128((Byte*)(pFld)),
                    5
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_dataTable.outArrayPtr);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Aes.KeygenAssist(_fld, 5);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector128<Byte>* pFld = &_fld)
            {
                var result = Aes.KeygenAssist(
                    Aes.LoadVector128((Byte*)(pFld)),
                    5
                );

                Unsafe.Write(_dataTable.outArrayPtr, result);
                ValidateResult(_dataTable.outArrayPtr);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Aes.KeygenAssist(test._fld, 5);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            var result = Aes.KeygenAssist(
                Aes.LoadVector128((Byte*)(&test._fld)),
                5
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.outArrayPtr);
        }

        public void RunStructFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario));

            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        public void RunStructFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario_Load));

            var test = TestStruct.Create();
            test.RunStructFldScenario_Load(this);
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

        private void ValidateResult(void* result, [CallerMemberName] string method = "")
        {

            Byte[] outArray = new Byte[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Byte>>());

            ValidateResult(outArray, method);
        }

        private void ValidateResult(Byte[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != _expectedRet[i] )
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Aes)}.{nameof(Aes.KeygenAssist)}<Byte>(Vector128<Byte>, 5): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  expectedRet: ({string.Join(", ", _expectedRet)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
