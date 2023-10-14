// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace JIT.HardwareIntrinsics.General._Vector128
{
    public static partial class Program
    {
        [Fact]
        public static void Test()
        {
            var test = new VectorBooleanBinaryOpTest__LessThanOrEqualAnySingle();

            // Validates basic functionality works, using Unsafe.Read
            test.RunBasicScenario_UnsafeRead();

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class VectorBooleanBinaryOpTest__LessThanOrEqualAnySingle
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] inArray2;

            private GCHandle inHandle1;
            private GCHandle inHandle2;

            private ulong alignment;

            public DataTable(Single[] inArray1, Single[] inArray2, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Single>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<Single>();
                if (!int.IsPow2(alignment) || (alignment > 16) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.inArray2 = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Single, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<Single, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<Single>>() / sizeof(Single);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<Single>>() / sizeof(Single);

        private static Single[] _data1 = new Single[Op1ElementCount];
        private static Single[] _data2 = new Single[Op2ElementCount];

        private DataTable _dataTable;

        public VectorBooleanBinaryOpTest__LessThanOrEqualAnySingle()
        {
            Succeeded = true;

            _data1[0] = 0.168625f;
            _data1[1] = 0.5899811f;
            _data1[2] = 0.8042229f;
            _data1[3] = 0.8173325f;

            _data2[0] = 0.059660614f;
            _data2[1] = 0.13952714f;
            _data2[2] = 0.23523656f;
            _data2[3] = 0.48773053f;

            _dataTable = new DataTable(_data1, _data2, LargestVectorSize);
        }

        public bool Succeeded { get; set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool LessThanOrEqualAnyProblem()
        {
            return Vector128.LessThanOrEqualAny(
                Unsafe.Read<Vector128<Single>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Single>>(_dataTable.inArray2Ptr)
            );
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void* GetPtr1()
        {
            return _dataTable.inArray1Ptr;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void* GetPtr2()
        {
            return _dataTable.inArray2Ptr;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CheckResult(bool result)
        {
            ValidateResult(GetPtr1(), GetPtr2(), result);
        }

        public void RunBasicScenario_UnsafeRead()
        {
            var result = Vector128.LessThanOrEqualAny(
                Unsafe.Read<Vector128<Single>>(GetPtr1()),
                Unsafe.Read<Vector128<Single>>(GetPtr2())
            );

            CheckResult(result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ValidateResult(void* op1, void* op2, bool result, [CallerMemberName] string method = "")
        {
            Single[] inArray1 = new Single[Op1ElementCount];
            Single[] inArray2 = new Single[Op2ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector128<Single>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Single, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(op2), (uint)Unsafe.SizeOf<Vector128<Single>>());

            ValidateResult(inArray1, inArray2, result, method);
        }

        private void ValidateResult(Single[] left, Single[] right, bool result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            var expectedResult = false;

            for (var i = 0; i < Op1ElementCount; i++)
            {
                expectedResult |= (left[i] <= right[i]);
            }

            succeeded = (expectedResult == result);

            if (!succeeded)
            {
                Succeeded = false;
            }
        }
    }
}
