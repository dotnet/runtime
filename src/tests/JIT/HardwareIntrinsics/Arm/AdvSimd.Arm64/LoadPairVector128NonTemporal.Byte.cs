// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\Arm\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        private static void LoadPairVector128NonTemporal_Byte()
        {
            var test = new LoadPairVector128NonTemporal_Byte();

            if (test.IsSupported)
            {
                // Validates basic functionality works
                test.RunBasicScenario();

                // Validates calling via reflection works
                test.RunReflectionScenario();

                // Validates loading to a static member works
                test.RunClsVarScenario();

                // Validates loading to the field of a local class works
                test.RunClassLclFldScenario();

                // Validates loading to the field of a local struct works
                test.RunStructLclFldScenario();

                // Validates loading to an instance member of a struct works
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

    public sealed unsafe class LoadPairVector128NonTemporal_Byte
    {
        private struct DataTable
        {
            private byte[] inArray;
            private byte[] outArray;

            private GCHandle inHandle;
            private GCHandle outHandle;

            private ulong alignment;

            public DataTable(Byte[] inArray, Byte[] outArray, int alignment)
            {
                int sizeOfinArray  = inArray.Length * Unsafe.SizeOf<Byte>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<Byte>();

                if ((alignment != 16 && alignment != 32) || (alignment * 2) < sizeOfinArray || (alignment * 2) < sizeOfoutArray)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray  = new byte[alignment * 2];
                this.outArray = new byte[alignment * 2];

                this.inHandle  = GCHandle.Alloc(this.inArray, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArrayPtr), ref Unsafe.As<Byte, byte>(ref inArray[0]), (uint)sizeOfinArray);
            }

            public void* inArrayPtr  => Align((byte*)(inHandle.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle.Free();
                outHandle.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private struct TestStruct
        {
            public (Vector128<Byte>,Vector128<Byte>) _fld;

            public static TestStruct Create()
            {
                return new TestStruct();
            }

            public void RunStructFldScenario(LoadPairVector128NonTemporal_Byte testClass)
            {
                _fld = AdvSimd.Arm64.LoadPairVector128NonTemporal((Byte*)(testClass._dataTable.inArrayPtr));

                Unsafe.Write(testClass._dataTable.outArrayPtr, _fld);
                testClass.ValidateResult(testClass._dataTable.inArrayPtr, testClass._dataTable.outArrayPtr);
            }
        }


        private static readonly int LargestVectorSize = 32;
        private static readonly int RetElementCount = Unsafe.SizeOf<(Vector128<Byte>,Vector128<Byte>)>() / sizeof(Byte);
        private static readonly int Op1ElementCount = RetElementCount;

        private static Byte[] _data = new Byte[Op1ElementCount];

        private static (Vector128<Byte>,Vector128<Byte>) _clsVar;

        private (Vector128<Byte>,Vector128<Byte>) _fld;

        private DataTable _dataTable;

        public LoadPairVector128NonTemporal_Byte()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetByte(); }
            _dataTable = new DataTable(_data, new Byte[RetElementCount], LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.Arm64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            var result = AdvSimd.Arm64.LoadPairVector128NonTemporal((Byte*)(_dataTable.inArrayPtr));
            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            var result = typeof(AdvSimd.Arm64).GetMethod(nameof(AdvSimd.Arm64.LoadPairVector128NonTemporal), new Type[] { typeof(Byte*) })
                                     .Invoke(null, new object[] {
                                        Pointer.Box(_dataTable.inArrayPtr, typeof(Byte*))
                                     });

            Unsafe.Write(_dataTable.outArrayPtr, ((Vector128<Byte>,Vector128<Byte>))result);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            _clsVar = AdvSimd.Arm64.LoadPairVector128NonTemporal((Byte*)(_dataTable.inArrayPtr));

            Unsafe.Write(_dataTable.outArrayPtr, _clsVar);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new LoadPairVector128NonTemporal_Byte();
            test._fld = AdvSimd.Arm64.LoadPairVector128NonTemporal((Byte*)(_dataTable.inArrayPtr));

            Unsafe.Write(_dataTable.outArrayPtr, test._fld);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            test._fld = AdvSimd.Arm64.LoadPairVector128NonTemporal((Byte*)(_dataTable.inArrayPtr));

            Unsafe.Write(_dataTable.outArrayPtr, test._fld);
            ValidateResult(_dataTable.inArrayPtr, _dataTable.outArrayPtr);
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
                RunBasicScenario();
            }
            catch (PlatformNotSupportedException)
            {
                Succeeded = true;
            }
        }

        private void ValidateResult(void* firstOp, void* result, [CallerMemberName] string method = "")
        {
            Byte[] inArray = new Byte[Op1ElementCount];
            Byte[] outArray = new Byte[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)(Unsafe.SizeOf<Byte>() * Op1ElementCount));
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Byte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)(Unsafe.SizeOf<Byte>() * RetElementCount));

            ValidateResult(inArray, outArray, method);
        }

        private void ValidateResult(Byte[] firstOp, Byte[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < Op1ElementCount; i++)
            {
                if (firstOp[i] != result[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd.Arm64)}.{nameof(AdvSimd.Arm64.LoadPairVector128NonTemporal)}<Byte>(Vector128<Byte>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($" result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
