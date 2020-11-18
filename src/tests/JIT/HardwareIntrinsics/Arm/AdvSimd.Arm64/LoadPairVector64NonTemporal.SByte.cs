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
        private static void LoadPairVector64NonTemporal_SByte()
        {
            var test = new LoadPairVector64NonTemporal_SByte();

            if (test.IsSupported)
            {
                // Validates basic functionality works
                test.RunBasicScenario_Load();

                // Validates calling via reflection works
                test.RunReflectionScenario_Load();
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

    public sealed unsafe class LoadPairVector64NonTemporal_SByte
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] outArray1;
            private byte[] outArray2;

            private GCHandle inHandle1;
            private GCHandle outHandle1;
            private GCHandle outHandle2;

            private ulong alignment;

            public DataTable(SByte[] inArray1, SByte[] outArray1, SByte[] outArray2, int alignment)
            {
                int sizeOfinArray1  = inArray1.Length * Unsafe.SizeOf<SByte>();
                int sizeOfoutArray1 = outArray1.Length * Unsafe.SizeOf<SByte>();
                int sizeOfoutArray2 = outArray2.Length * Unsafe.SizeOf<SByte>();
                if ((alignment != 16 && alignment != 32) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfoutArray1 || (alignment * 2) < sizeOfoutArray2)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1  = new byte[alignment * 2];
                this.outArray1 = new byte[alignment * 2];
                this.outArray2 = new byte[alignment * 2];

                this.inHandle1  = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.outHandle1 = GCHandle.Alloc(this.outArray1, GCHandleType.Pinned);
                this.outHandle2 = GCHandle.Alloc(this.outArray2, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<SByte, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
            }

            public void* inArray1Ptr  => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArray1Ptr => Align((byte*)(outHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArray2Ptr => Align((byte*)(outHandle2.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                outHandle1.Free();
                outHandle2.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Ret1ElementCount = Unsafe.SizeOf<Vector64<SByte>>() / sizeof(SByte);
        private static readonly int Ret2ElementCount = Unsafe.SizeOf<Vector64<SByte>>() / sizeof(SByte);
        private static readonly int Op1ElementCount  = Ret1ElementCount + Ret2ElementCount;

        private static SByte[] _data = new SByte[Op1ElementCount];

        private DataTable _dataTable;

        public LoadPairVector64NonTemporal_SByte()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetSByte(); }
            _dataTable = new DataTable(_data, new SByte[Ret1ElementCount], new SByte[Ret2ElementCount], LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.Arm64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var (value1, value2) = AdvSimd.Arm64.LoadPairVector64NonTemporal((SByte*)(_dataTable.inArray1Ptr));
            Unsafe.Write(_dataTable.outArray1Ptr, value1);
            Unsafe.Write(_dataTable.outArray2Ptr, value2);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outArray1Ptr, _dataTable.outArray2Ptr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(AdvSimd.Arm64).GetMethod(nameof(AdvSimd.Arm64.LoadPairVector64NonTemporal), new Type[] { typeof(SByte*) })
                                     .Invoke(null, new object[] {
                                        Pointer.Box(_dataTable.inArray1Ptr, typeof(SByte*))
                                     });

            var (value1, value2) = ((Vector64<SByte>, Vector64<SByte>))result;

            Unsafe.Write(_dataTable.outArray1Ptr, value1);
            Unsafe.Write(_dataTable.outArray2Ptr, value2);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outArray1Ptr, _dataTable.outArray2Ptr);
        }

        public void RunUnsupportedScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunUnsupportedScenario));

            Succeeded = false;

            try
            {
                RunBasicScenario_Load();
            }
            catch (PlatformNotSupportedException)
            {
                Succeeded = true;
            }
        }

        private void ValidateResult(void* firstOp, void* result1, void* result2, [CallerMemberName] string method = "")
        {
            SByte[] inArray = new SByte[Op1ElementCount];
            SByte[] outArray1 = new SByte[Ret1ElementCount];
            SByte[] outArray2 = new SByte[Ret2ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)(Unsafe.SizeOf<SByte>() * Op1ElementCount));
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref outArray1[0]), ref Unsafe.AsRef<byte>(result1), (uint)Unsafe.SizeOf<Vector64<SByte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref outArray2[0]), ref Unsafe.AsRef<byte>(result2), (uint)Unsafe.SizeOf<Vector64<SByte>>());

            ValidateResult(inArray, outArray1, outArray2, method);
        }

        private void ValidateResult(SByte[] firstOp, SByte[] firstResult, SByte[] secondResult, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < Op1ElementCount; i++)
            {
                if (firstOp[i] != Helpers.Concat(firstResult, secondResult, i))
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd.Arm64)}.{nameof(AdvSimd.Arm64.LoadPairVector64NonTemporal)}<SByte>(Vector64<SByte>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"     firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($" firstResult: ({string.Join(", ", firstResult)})");
                TestLibrary.TestFramework.LogInformation($"secondResult: ({string.Join(", ", secondResult)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
