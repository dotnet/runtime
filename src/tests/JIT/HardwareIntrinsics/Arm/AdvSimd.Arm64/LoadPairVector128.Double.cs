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
        private static void LoadPairVector128_Double()
        {
            var test = new LoadPairVector128_Double();

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

    public sealed unsafe class LoadPairVector128_Double
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

            public DataTable(Double[] inArray1, Double[] outArray1, Double[] outArray2, int alignment)
            {
                int sizeOfinArray1  = inArray1.Length * Unsafe.SizeOf<Double>();
                int sizeOfoutArray1 = outArray1.Length * Unsafe.SizeOf<Double>();
                int sizeOfoutArray2 = outArray2.Length * Unsafe.SizeOf<Double>();
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

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Double, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
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

        private static readonly int LargestVectorSize = 32;

        private static readonly int Ret1ElementCount = Unsafe.SizeOf<Vector128<Double>>() / sizeof(Double);
        private static readonly int Ret2ElementCount = Unsafe.SizeOf<Vector128<Double>>() / sizeof(Double);
        private static readonly int Op1ElementCount  = Ret1ElementCount + Ret2ElementCount;

        private static Double[] _data = new Double[Op1ElementCount];

        private DataTable _dataTable;

        public LoadPairVector128_Double()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data[i] = TestLibrary.Generator.GetDouble(); }
            _dataTable = new DataTable(_data, new Double[Ret1ElementCount], new Double[Ret2ElementCount], LargestVectorSize);
        }

        public bool IsSupported => AdvSimd.Arm64.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var (value1, value2) = AdvSimd.Arm64.LoadPairVector128((Double*)(_dataTable.inArray1Ptr));
            Unsafe.Write(_dataTable.outArray1Ptr, value1);
            Unsafe.Write(_dataTable.outArray2Ptr, value2);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outArray1Ptr, _dataTable.outArray2Ptr);
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(AdvSimd.Arm64).GetMethod(nameof(AdvSimd.Arm64.LoadPairVector128), new Type[] { typeof(Double*) })
                                     .Invoke(null, new object[] {
                                        Pointer.Box(_dataTable.inArray1Ptr, typeof(Double*))
                                     });

            var (value1, value2) = ((Vector128<Double>, Vector128<Double>))result;

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
            Double[] inArray = new Double[Op1ElementCount];
            Double[] outArray1 = new Double[Ret1ElementCount];
            Double[] outArray2 = new Double[Ret2ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray[0]), ref Unsafe.AsRef<byte>(firstOp), (uint)(Unsafe.SizeOf<Double>() * Op1ElementCount));
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref outArray1[0]), ref Unsafe.AsRef<byte>(result1), (uint)Unsafe.SizeOf<Vector128<Double>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref outArray2[0]), ref Unsafe.AsRef<byte>(result2), (uint)Unsafe.SizeOf<Vector128<Double>>());

            ValidateResult(inArray, outArray1, outArray2, method);
        }

        private void ValidateResult(Double[] firstOp, Double[] firstResult, Double[] secondResult, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < Op1ElementCount; i++)
            {
                if (BitConverter.DoubleToInt64Bits(firstOp[i]) != BitConverter.DoubleToInt64Bits(Helpers.Concat(firstResult, secondResult, i)))
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(AdvSimd.Arm64)}.{nameof(AdvSimd.Arm64.LoadPairVector128)}<Double>(Vector128<Double>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"     firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($" firstResult: ({string.Join(", ", firstResult)})");
                TestLibrary.TestFramework.LogInformation($"secondResult: ({string.Join(", ", secondResult)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
