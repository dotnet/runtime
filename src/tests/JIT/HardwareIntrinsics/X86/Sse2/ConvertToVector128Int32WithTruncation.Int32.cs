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
        private static void ConvertToVector128Int32WithTruncationInt32Vector128Single()
        {
            var test = new SimpleUnaryOpConvTest__ConvertToVector128Int32WithTruncationInt32Vector128Single();

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

    public sealed unsafe class SimpleUnaryOpConvTest__ConvertToVector128Int32WithTruncationInt32Vector128Single
    {
        private struct TestStruct
        {
            public Vector128<Single> _fld;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetSingle(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Single>, byte>(ref testStruct._fld), ref Unsafe.As<Single, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<Single>>());

                return testStruct;
            }

            public void RunStructFldScenario(SimpleUnaryOpConvTest__ConvertToVector128Int32WithTruncationInt32Vector128Single testClass)
            {
                var result = Sse2.ConvertToVector128Int32WithTruncation(_fld);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<Single>>() / sizeof(Single);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector128<Int32>>() / sizeof(Int32);

        private static Single[] _data = new Single[Op1ElementCount];

        private static Vector128<Single> _clsVar;

        private Vector128<Single> _fld;

        private SimpleUnaryOpTest__DataTable<Int32, Single> _dataTable;

        static SimpleUnaryOpConvTest__ConvertToVector128Int32WithTruncationInt32Vector128Single()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetSingle(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Single>, byte>(ref _clsVar), ref Unsafe.As<Single, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<Single>>());
        }

        public SimpleUnaryOpConvTest__ConvertToVector128Int32WithTruncationInt32Vector128Single()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetSingle(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Single>, byte>(ref _fld), ref Unsafe.As<Single, byte>(ref _data[0]), (uint)Unsafe.SizeOf<Vector128<Single>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetSingle(); }
            _dataTable = new SimpleUnaryOpTest__DataTable<Int32, Single>(_data, new Int32[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => Sse2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Sse2.ConvertToVector128Int32WithTruncation(
                Unsafe.Read<Vector128<Single>>(_dataTable.inArrayPtr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = Sse2.ConvertToVector128Int32WithTruncation(
                Sse2.LoadVector128((Single*)(_dataTable.inArrayPtr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunBasicScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_LoadAligned));

            var result = Sse2.ConvertToVector128Int32WithTruncation(
                Sse2.LoadAlignedVector128((Single*)(_dataTable.inArrayPtr))
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Sse2).GetMethod(nameof(Sse2.ConvertToVector128Int32WithTruncation), new Type[] { typeof(Vector128<Single>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Single>>(_dataTable.inArrayPtr)
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int32>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(Sse2).GetMethod(nameof(Sse2.ConvertToVector128Int32WithTruncation), new Type[] { typeof(Vector128<Single>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((Single*)(_dataTable.inArrayPtr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int32>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_LoadAligned));

            var result = typeof(Sse2).GetMethod(nameof(Sse2.ConvertToVector128Int32WithTruncation), new Type[] { typeof(Vector128<Single>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((Single*)(_dataTable.inArrayPtr))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector128<Int32>)(result));
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Sse2.ConvertToVector128Int32WithTruncation(
                _clsVar
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var firstOp = Unsafe.Read<Vector128<Single>>(_dataTable.inArrayPtr);
            var result = Sse2.ConvertToVector128Int32WithTruncation(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var firstOp = Sse2.LoadVector128((Single*)(_dataTable.inArrayPtr));
            var result = Sse2.ConvertToVector128Int32WithTruncation(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_LoadAligned));

            var firstOp = Sse2.LoadAlignedVector128((Single*)(_dataTable.inArrayPtr));
            var result = Sse2.ConvertToVector128Int32WithTruncation(firstOp);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(firstOp, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new SimpleUnaryOpConvTest__ConvertToVector128Int32WithTruncationInt32Vector128Single();
            var result = Sse2.ConvertToVector128Int32WithTruncation(test._fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Sse2.ConvertToVector128Int32WithTruncation(_fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Sse2.ConvertToVector128Int32WithTruncation(test._fld);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld, _dataTable.outArrayPtr);
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

        private void ValidateResult(Vector128<Single> firstOp, void* result, [CallerMemberName] string method = "")
        {
            Single[] inArray = new Single[Op1ElementCount];
            Int32[] outArray = new Int32[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref inArray[0]), firstOp);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Int32>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(void* firstOp, void* result, [CallerMemberName] string method = "")
        {
            Single[] inArray = new Single[Op1ElementCount];
            Int32[] outArray = new Int32[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)Unsafe.SizeOf<Vector128<Single>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Int32, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector128<Int32>>());

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(Single[] firstOp, Int32[] result, [CallerMemberName] string method = "")
        {
            if (Sse2Verify.ConvertToVector128Int32WithTruncation(result, firstOp))
            {
                Succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (Sse2Verify.ConvertToVector128Int32WithTruncation(result, firstOp))
                    {
                        Succeeded = false;
                        break;
                    }
                }
            }

            if (!Succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Sse2)}.{nameof(Sse2.ConvertToVector128Int32WithTruncation)}<Int32>(Vector128<Single>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"   result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }
    }
}
