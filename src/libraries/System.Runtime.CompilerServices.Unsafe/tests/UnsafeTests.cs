// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.CompilerServices
{
    public class UnsafeTests
    {
        [Fact]
        public static unsafe void ReadInt32()
        {
            int expected = 10;
            void* address = Unsafe.AsPointer(ref expected);
            int ret = Unsafe.Read<int>(address);
            Assert.Equal(expected, ret);
        }

        [Fact]
        public static unsafe void WriteInt32()
        {
            int value = 10;
            int* address = (int*)Unsafe.AsPointer(ref value);
            int expected = 20;
            Unsafe.Write(address, expected);

            Assert.Equal(expected, value);
            Assert.Equal(expected, *address);
            Assert.Equal(expected, Unsafe.Read<int>(address));
        }

        [Fact]
        public static unsafe void WriteBytesIntoInt32()
        {
            int value = 20;
            int* intAddress = (int*)Unsafe.AsPointer(ref value);
            byte* byteAddress = (byte*)intAddress;
            for (int i = 0; i < 4; i++)
            {
                Unsafe.Write(byteAddress + i, (byte)i);
            }

            Assert.Equal(0, Unsafe.Read<byte>(byteAddress));
            Assert.Equal(1, Unsafe.Read<byte>(byteAddress + 1));
            Assert.Equal(2, Unsafe.Read<byte>(byteAddress + 2));
            Assert.Equal(3, Unsafe.Read<byte>(byteAddress + 3));

            Byte4 b4 = Unsafe.Read<Byte4>(byteAddress);
            Assert.Equal(0, b4.B0);
            Assert.Equal(1, b4.B1);
            Assert.Equal(2, b4.B2);
            Assert.Equal(3, b4.B3);

            int expected = (b4.B3 << 24) + (b4.B2 << 16) + (b4.B1 << 8) + (b4.B0);
            Assert.Equal(expected, value);
        }

        [Fact]
        public static unsafe void LongIntoCompoundStruct()
        {
            long value = 1234567891011121314L;
            long* longAddress = (long*)Unsafe.AsPointer(ref value);
            Byte4Short2 b4s2 = Unsafe.Read<Byte4Short2>(longAddress);
            Assert.Equal(162, b4s2.B0);
            Assert.Equal(48, b4s2.B1);
            Assert.Equal(210, b4s2.B2);
            Assert.Equal(178, b4s2.B3);
            Assert.Equal(4340, b4s2.S4);
            Assert.Equal(4386, b4s2.S6);

            b4s2.B0 = 1;
            b4s2.B1 = 1;
            b4s2.B2 = 1;
            b4s2.B3 = 1;
            b4s2.S4 = 1;
            b4s2.S6 = 1;
            Unsafe.Write(longAddress, b4s2);

            long expected = 281479288520961;
            Assert.Equal(expected, value);
            Assert.Equal(expected, Unsafe.Read<long>(longAddress));
        }

        [Fact]
        public static unsafe void ReadWriteDoublePointer()
        {
            int value1 = 10;
            int value2 = 20;
            int* valueAddress = (int*)Unsafe.AsPointer(ref value1);
            int** valueAddressPtr = &valueAddress;
            Unsafe.Write(valueAddressPtr, new IntPtr(&value2));

            Assert.Equal(20, *(*valueAddressPtr));
            Assert.Equal(20, Unsafe.Read<int>(valueAddress));
            Assert.Equal(new IntPtr(valueAddress), Unsafe.Read<IntPtr>(valueAddressPtr));
            Assert.Equal(20, Unsafe.Read<int>(Unsafe.Read<IntPtr>(valueAddressPtr).ToPointer()));
        }

        [Fact]
        public static unsafe void CopyToRef()
        {
            int value = 10;
            int destination = -1;
            Unsafe.Copy(ref destination, Unsafe.AsPointer(ref value));
            Assert.Equal(10, destination);
            Assert.Equal(10, value);

            int destination2 = -1;
            Unsafe.Copy(ref destination2, &value);
            Assert.Equal(10, destination2);
            Assert.Equal(10, value);
        }

        [Fact]
        public static unsafe void CopyToVoidPtr()
        {
            int value = 10;
            int destination = -1;
            Unsafe.Copy(Unsafe.AsPointer(ref destination), ref value);
            Assert.Equal(10, destination);
            Assert.Equal(10, value);

            int destination2 = -1;
            Unsafe.Copy(&destination2, ref value);
            Assert.Equal(10, destination2);
            Assert.Equal(10, value);
        }

        [Fact]
        public static unsafe void SizeOf()
        {
            Assert.Equal(1, Unsafe.SizeOf<sbyte>());
            Assert.Equal(1, Unsafe.SizeOf<byte>());
            Assert.Equal(2, Unsafe.SizeOf<short>());
            Assert.Equal(2, Unsafe.SizeOf<ushort>());
            Assert.Equal(4, Unsafe.SizeOf<int>());
            Assert.Equal(4, Unsafe.SizeOf<uint>());
            Assert.Equal(8, Unsafe.SizeOf<long>());
            Assert.Equal(8, Unsafe.SizeOf<ulong>());
            Assert.Equal(4, Unsafe.SizeOf<float>());
            Assert.Equal(8, Unsafe.SizeOf<double>());
            Assert.Equal(4, Unsafe.SizeOf<Byte4>());
            Assert.Equal(8, Unsafe.SizeOf<Byte4Short2>());
            Assert.Equal(512, Unsafe.SizeOf<Byte512>());
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockStack(int numBytes, byte value)
        {
            byte* stackPtr = stackalloc byte[numBytes];
            Unsafe.InitBlock(stackPtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(stackPtr[i], value);
            }
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockUnmanaged(int numBytes, byte value)
        {
            IntPtr allocatedMemory = Marshal.AllocCoTaskMem(numBytes);
            byte* bytePtr = (byte*)allocatedMemory.ToPointer();
            Unsafe.InitBlock(bytePtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(bytePtr[i], value);
            }
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockRefStack(int numBytes, byte value)
        {
            byte* stackPtr = stackalloc byte[numBytes];
            Unsafe.InitBlock(ref *stackPtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(stackPtr[i], value);
            }
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockRefUnmanaged(int numBytes, byte value)
        {
            IntPtr allocatedMemory = Marshal.AllocCoTaskMem(numBytes);
            byte* bytePtr = (byte*)allocatedMemory.ToPointer();
            Unsafe.InitBlock(ref *bytePtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(bytePtr[i], value);
            }
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockUnalignedStack(int numBytes, byte value)
        {
            byte* stackPtr = stackalloc byte[numBytes + 1];
            stackPtr += 1; // +1 = make unaligned
            Unsafe.InitBlockUnaligned(stackPtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(stackPtr[i], value);
            }
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockUnalignedUnmanaged(int numBytes, byte value)
        {
            IntPtr allocatedMemory = Marshal.AllocCoTaskMem(numBytes + 1);
            byte* bytePtr = (byte*)allocatedMemory.ToPointer() + 1; // +1 = make unaligned
            Unsafe.InitBlockUnaligned(bytePtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(bytePtr[i], value);
            }
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockUnalignedRefStack(int numBytes, byte value)
        {
            byte* stackPtr = stackalloc byte[numBytes + 1];
            stackPtr += 1; // +1 = make unaligned
            Unsafe.InitBlockUnaligned(ref *stackPtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(stackPtr[i], value);
            }
        }

        [Theory]
        [MemberData(nameof(InitBlockData))]
        public static unsafe void InitBlockUnalignedRefUnmanaged(int numBytes, byte value)
        {
            IntPtr allocatedMemory = Marshal.AllocCoTaskMem(numBytes + 1);
            byte* bytePtr = (byte*)allocatedMemory.ToPointer() + 1; // +1 = make unaligned
            Unsafe.InitBlockUnaligned(ref *bytePtr, value, (uint)numBytes);
            for (int i = 0; i < numBytes; i++)
            {
                Assert.Equal(bytePtr[i], value);
            }
        }

        public static IEnumerable<object[]> InitBlockData()
        {
            yield return new object[] { 0, 1 };
            yield return new object[] { 1, 1 };
            yield return new object[] { 10, 0 };
            yield return new object[] { 10, 2 };
            yield return new object[] { 10, 255 };
            yield return new object[] { 10000, 255 };
        }

        [Theory]
        [MemberData(nameof(CopyBlockData))]
        public static unsafe void CopyBlock(int numBytes)
        {
            byte* source = stackalloc byte[numBytes];
            byte* destination = stackalloc byte[numBytes];

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                source[i] = value;
            }

            Unsafe.CopyBlock(destination, source, (uint)numBytes);

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                Assert.Equal(value, destination[i]);
                Assert.Equal(source[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(CopyBlockData))]
        public static unsafe void CopyBlockRef(int numBytes)
        {
            byte* source = stackalloc byte[numBytes];
            byte* destination = stackalloc byte[numBytes];

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                source[i] = value;
            }

            Unsafe.CopyBlock(ref destination[0], ref source[0], (uint)numBytes);

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                Assert.Equal(value, destination[i]);
                Assert.Equal(source[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(CopyBlockData))]
        public static unsafe void CopyBlockUnaligned(int numBytes)
        {
            byte* source = stackalloc byte[numBytes + 1];
            byte* destination = stackalloc byte[numBytes + 1];
            source += 1;      // +1 = make unaligned
            destination += 1; // +1 = make unaligned

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                source[i] = value;
            }

            Unsafe.CopyBlockUnaligned(destination, source, (uint)numBytes);

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                Assert.Equal(value, destination[i]);
                Assert.Equal(source[i], destination[i]);
            }
        }

        [Theory]
        [MemberData(nameof(CopyBlockData))]
        public static unsafe void CopyBlockUnalignedRef(int numBytes)
        {
            byte* source = stackalloc byte[numBytes + 1];
            byte* destination = stackalloc byte[numBytes + 1];
            source += 1;      // +1 = make unaligned
            destination += 1; // +1 = make unaligned

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                source[i] = value;
            }

            Unsafe.CopyBlockUnaligned(ref destination[0], ref source[0], (uint)numBytes);

            for (int i = 0; i < numBytes; i++)
            {
                byte value = (byte)(i % 255);
                Assert.Equal(value, destination[i]);
                Assert.Equal(source[i], destination[i]);
            }
        }

        public static IEnumerable<object[]> CopyBlockData()
        {
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { 10 };
            yield return new object[] { 100 };
            yield return new object[] { 100000 };
        }

        [Fact]
        public static void As()
        {
            object o = "Hello";
            Assert.Equal("Hello", Unsafe.As<string>(o));
        }

        [Fact]
        public static void DangerousAs()
        {
            // Verify that As does not perform type checks
            object o = new object();
            Assert.IsType(typeof(object), Unsafe.As<string>(o));
        }

        [Fact]
        public static void ByteOffsetArray()
        {
            var a = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };

            Assert.Equal(new IntPtr(0), Unsafe.ByteOffset(ref a[0], ref a[0]));
            Assert.Equal(new IntPtr(1), Unsafe.ByteOffset(ref a[0], ref a[1]));
            Assert.Equal(new IntPtr(-1), Unsafe.ByteOffset(ref a[1], ref a[0]));
            Assert.Equal(new IntPtr(2), Unsafe.ByteOffset(ref a[0], ref a[2]));
            Assert.Equal(new IntPtr(-2), Unsafe.ByteOffset(ref a[2], ref a[0]));
            Assert.Equal(new IntPtr(3), Unsafe.ByteOffset(ref a[0], ref a[3]));
            Assert.Equal(new IntPtr(4), Unsafe.ByteOffset(ref a[0], ref a[4]));
            Assert.Equal(new IntPtr(5), Unsafe.ByteOffset(ref a[0], ref a[5]));
            Assert.Equal(new IntPtr(6), Unsafe.ByteOffset(ref a[0], ref a[6]));
            Assert.Equal(new IntPtr(7), Unsafe.ByteOffset(ref a[0], ref a[7]));
        }
        
        [Fact]
        public static void ByteOffsetStackByte4()
        {
            var byte4 = new Byte4();

            Assert.Equal(new IntPtr(0), Unsafe.ByteOffset(ref byte4.B0, ref byte4.B0));
            Assert.Equal(new IntPtr(1), Unsafe.ByteOffset(ref byte4.B0, ref byte4.B1));
            Assert.Equal(new IntPtr(-1), Unsafe.ByteOffset(ref byte4.B1, ref byte4.B0));
            Assert.Equal(new IntPtr(2), Unsafe.ByteOffset(ref byte4.B0, ref byte4.B2));
            Assert.Equal(new IntPtr(-2), Unsafe.ByteOffset(ref byte4.B2, ref byte4.B0));
            Assert.Equal(new IntPtr(3), Unsafe.ByteOffset(ref byte4.B0, ref byte4.B3));
            Assert.Equal(new IntPtr(-3), Unsafe.ByteOffset(ref byte4.B3, ref byte4.B0));
        }

        [Fact]
        public static unsafe void AsRef()
        {
            byte[] b = new byte[4] { 0x42, 0x42, 0x42, 0x42 };
            fixed (byte * p = b)
            {
                ref int r = ref Unsafe.AsRef<int>(p);
                Assert.Equal(0x42424242, r);

                r = 0x0EF00EF0;
                Assert.Equal(0xFE, b[0] | b[1] | b[2] | b[3]);
            }
        }

        [Fact]
        public static void InAsRef()
        {
            int[] a = new int[] { 0x123, 0x234, 0x345, 0x456 };

            ref int r = ref Unsafe.AsRef<int>(a[0]);
            Assert.Equal(0x123, r);

            r = 0x42;
            Assert.Equal(0x42, a[0]);
        }

        [Fact]
        public static void RefAs()
        {
            byte[] b = new byte[4] { 0x42, 0x42, 0x42, 0x42 };

            ref int r = ref Unsafe.As<byte, int>(ref b[0]);
            Assert.Equal(0x42424242, r);

            r = 0x0EF00EF0;
            Assert.Equal(0xFE, b[0] | b[1] | b[2] | b[3]);
        }

        [Fact]
        public static void RefAdd()
        {
            int[] a = new int[] { 0x123, 0x234, 0x345, 0x456 };

            ref int r1 = ref Unsafe.Add(ref a[0], 1);
            Assert.Equal(0x234, r1);

            ref int r2 = ref Unsafe.Add(ref r1, 2);
            Assert.Equal(0x456, r2);

            ref int r3 = ref Unsafe.Add(ref r2, -3);
            Assert.Equal(0x123, r3);
        }

        [Fact]
        public static unsafe void VoidPointerAdd()
        {
            int[] a = new int[] { 0x123, 0x234, 0x345, 0x456 };

            fixed (void* ptr = a)
            {
                void* r1 = Unsafe.Add<int>(ptr, 1);
                Assert.Equal(0x234, *(int*)r1);

                void* r2 = Unsafe.Add<int>(r1, 2);
                Assert.Equal(0x456, *(int*)r2);

                void* r3 = Unsafe.Add<int>(r2, -3);
                Assert.Equal(0x123, *(int*)r3);
            }

            fixed (void* ptr = &a[1])
            {
                void* r0 = Unsafe.Add<int>(ptr, -1);
                Assert.Equal(0x123, *(int*)r0);

                void* r3 = Unsafe.Add<int>(ptr, 2);
                Assert.Equal(0x456, *(int*)r3);
            }
        }

        [Fact]
        public static void RefAddIntPtr()
        {
            int[] a = new int[] { 0x123, 0x234, 0x345, 0x456 };

            ref int r1 = ref Unsafe.Add(ref a[0], (IntPtr)1);
            Assert.Equal(0x234, r1);

            ref int r2 = ref Unsafe.Add(ref r1, (IntPtr)2);
            Assert.Equal(0x456, r2);

            ref int r3 = ref Unsafe.Add(ref r2, (IntPtr)(-3));
            Assert.Equal(0x123, r3);
        }

        [Fact]
        public static void RefAddByteOffset()
        {
            byte[] a = new byte[] { 0x12, 0x34, 0x56, 0x78 };

            ref byte r1 = ref Unsafe.AddByteOffset(ref a[0], (IntPtr)1);
            Assert.Equal(0x34, r1);

            ref byte r2 = ref Unsafe.AddByteOffset(ref r1, (IntPtr)2);
            Assert.Equal(0x78, r2);

            ref byte r3 = ref Unsafe.AddByteOffset(ref r2, (IntPtr)(-3));
            Assert.Equal(0x12, r3);
        }

        [Fact]
        public static void RefSubtract()
        {
            string[] a = new string[] { "abc", "def", "ghi", "jkl" };

            ref string r1 = ref Unsafe.Subtract(ref a[0], -2);
            Assert.Equal("ghi", r1);

            ref string r2 = ref Unsafe.Subtract(ref r1, -1);
            Assert.Equal("jkl", r2);

            ref string r3 = ref Unsafe.Subtract(ref r2, 3);
            Assert.Equal("abc", r3);
        }
        
        [Fact]
        public static unsafe void VoidPointerSubtract()
        {
            int[] a = new int[] { 0x123, 0x234, 0x345, 0x456 };

            fixed (void* ptr = a)
            {
                void* r1 = Unsafe.Subtract<int>(ptr, -2);
                Assert.Equal(0x345, *(int*)r1);

                void* r2 = Unsafe.Subtract<int>(r1, -1);
                Assert.Equal(0x456, *(int*)r2);

                void* r3 = Unsafe.Subtract<int>(r2, 3);
                Assert.Equal(0x123, *(int*)r3);
            }

            fixed (void* ptr = &a[1])
            {
                void* r0 = Unsafe.Subtract<int>(ptr, 1);
                Assert.Equal(0x123, *(int*)r0);

                void* r3 = Unsafe.Subtract<int>(ptr, -2);
                Assert.Equal(0x456, *(int*)r3);
            }
        }

        [Fact]
        public static void RefSubtractIntPtr()
        {
            string[] a = new string[] { "abc", "def", "ghi", "jkl" };

            ref string r1 = ref Unsafe.Subtract(ref a[0], (IntPtr)(-2));
            Assert.Equal("ghi", r1);

            ref string r2 = ref Unsafe.Subtract(ref r1, (IntPtr)(-1));
            Assert.Equal("jkl", r2);

            ref string r3 = ref Unsafe.Subtract(ref r2, (IntPtr)3);
            Assert.Equal("abc", r3);
        }

        [Fact]
        public static void RefSubtractByteOffset()
        {
            byte[] a = new byte[] { 0x12, 0x34, 0x56, 0x78 };

            ref byte r1 = ref Unsafe.SubtractByteOffset(ref a[0], (IntPtr)(-1));
            Assert.Equal(0x34, r1);

            ref byte r2 = ref Unsafe.SubtractByteOffset(ref r1, (IntPtr)(-2));
            Assert.Equal(0x78, r2);

            ref byte r3 = ref Unsafe.SubtractByteOffset(ref r2, (IntPtr)3);
            Assert.Equal(0x12, r3);
        }

        [Fact]
        public static void RefAreSame()
        {
            long[] a = new long[2];

            Assert.True(Unsafe.AreSame(ref a[0], ref a[0]));
            Assert.False(Unsafe.AreSame(ref a[0], ref a[1]));
        }

        [Fact]
        public static unsafe void RefIsAddressGreaterThan()
        {
            int[] a = new int[2];

            Assert.False(Unsafe.IsAddressGreaterThan(ref a[0], ref a[0]));
            Assert.False(Unsafe.IsAddressGreaterThan(ref a[0], ref a[1]));
            Assert.True(Unsafe.IsAddressGreaterThan(ref a[1], ref a[0]));
            Assert.False(Unsafe.IsAddressGreaterThan(ref a[1], ref a[1]));

            // The following tests ensure that we're using unsigned comparison logic

            Assert.False(Unsafe.IsAddressGreaterThan(ref Unsafe.AsRef<byte>((void*)(1)), ref Unsafe.AsRef<byte>((void*)(-1))));
            Assert.True(Unsafe.IsAddressGreaterThan(ref Unsafe.AsRef<byte>((void*)(-1)), ref Unsafe.AsRef<byte>((void*)(1))));
            Assert.True(Unsafe.IsAddressGreaterThan(ref Unsafe.AsRef<byte>((void*)(int.MinValue)), ref Unsafe.AsRef<byte>((void*)(int.MaxValue))));
            Assert.False(Unsafe.IsAddressGreaterThan(ref Unsafe.AsRef<byte>((void*)(int.MaxValue)), ref Unsafe.AsRef<byte>((void*)(int.MinValue))));
            Assert.False(Unsafe.IsAddressGreaterThan(ref Unsafe.AsRef<byte>(null), ref Unsafe.AsRef<byte>(null)));
        }

        [Fact]
        public static unsafe void RefIsAddressLessThan()
        {
            int[] a = new int[2];

            Assert.False(Unsafe.IsAddressLessThan(ref a[0], ref a[0]));
            Assert.True(Unsafe.IsAddressLessThan(ref a[0], ref a[1]));
            Assert.False(Unsafe.IsAddressLessThan(ref a[1], ref a[0]));
            Assert.False(Unsafe.IsAddressLessThan(ref a[1], ref a[1]));

            // The following tests ensure that we're using unsigned comparison logic

            Assert.True(Unsafe.IsAddressLessThan(ref Unsafe.AsRef<byte>((void*)(1)), ref Unsafe.AsRef<byte>((void*)(-1))));
            Assert.False(Unsafe.IsAddressLessThan(ref Unsafe.AsRef<byte>((void*)(-1)), ref Unsafe.AsRef<byte>((void*)(1))));
            Assert.False(Unsafe.IsAddressLessThan(ref Unsafe.AsRef<byte>((void*)(int.MinValue)), ref Unsafe.AsRef<byte>((void*)(int.MaxValue))));
            Assert.True(Unsafe.IsAddressLessThan(ref Unsafe.AsRef<byte>((void*)(int.MaxValue)), ref Unsafe.AsRef<byte>((void*)(int.MinValue))));
            Assert.False(Unsafe.IsAddressLessThan(ref Unsafe.AsRef<byte>(null), ref Unsafe.AsRef<byte>(null)));
        }

        [Fact]
        public static unsafe void ReadUnaligned_ByRef_Int32()
        {
            byte[] unaligned = Int32Double.Unaligned(123456789, 3.42);

            int actual = Unsafe.ReadUnaligned<int>(ref unaligned[1]);

            Assert.Equal(123456789, actual);
        }

        [Fact]
        public static unsafe void ReadUnaligned_ByRef_Double()
        {
            byte[] unaligned = Int32Double.Unaligned(123456789, 3.42);

            double actual = Unsafe.ReadUnaligned<double>(ref unaligned[9]);

            Assert.Equal(3.42, actual);
        }

        [Fact]
        public static unsafe void ReadUnaligned_ByRef_Struct()
        {
            byte[] unaligned = Int32Double.Unaligned(123456789, 3.42);

            Int32Double actual = Unsafe.ReadUnaligned<Int32Double>(ref unaligned[1]);

            Assert.Equal(123456789, actual.Int32);
            Assert.Equal(3.42, actual.Double);
        }

        [Fact]
        public static unsafe void ReadUnaligned_Ptr_Int32()
        {
            byte[] unaligned = Int32Double.Unaligned(123456789, 3.42);

            fixed (byte* p = unaligned)
            {
                int actual = Unsafe.ReadUnaligned<int>(p + 1);

                Assert.Equal(123456789, actual);
            }
        }

        [Fact]
        public static unsafe void ReadUnaligned_Ptr_Double()
        {
            byte[] unaligned = Int32Double.Unaligned(123456789, 3.42);

            fixed (byte* p = unaligned)
            {
                double actual = Unsafe.ReadUnaligned<double>(p + 9);

                Assert.Equal(3.42, actual);
            }
        }

        [Fact]
        public static unsafe void ReadUnaligned_Ptr_Struct()
        {
            byte[] unaligned = Int32Double.Unaligned(123456789, 3.42);

            fixed (byte* p = unaligned)
            {
                Int32Double actual = Unsafe.ReadUnaligned<Int32Double>(p + 1);

                Assert.Equal(123456789, actual.Int32);
                Assert.Equal(3.42, actual.Double);
            }
        }

        [Fact]
        public static unsafe void WriteUnaligned_ByRef_Int32()
        {
            byte[] unaligned = new byte[sizeof(Int32Double) + 1];

            Unsafe.WriteUnaligned(ref unaligned[1], 123456789);

            int actual = Int32Double.Aligned(unaligned).Int32;
            Assert.Equal(123456789, actual);
        }

        [Fact]
        public static unsafe void WriteUnaligned_ByRef_Double()
        {
            byte[] unaligned = new byte[sizeof(Int32Double) + 1];

            Unsafe.WriteUnaligned(ref unaligned[9], 3.42);

            double actual = Int32Double.Aligned(unaligned).Double;
            Assert.Equal(3.42, actual);
        }

        [Fact]
        public static unsafe void WriteUnaligned_ByRef_Struct()
        {
            byte[] unaligned = new byte[sizeof(Int32Double) + 1];

            Unsafe.WriteUnaligned(ref unaligned[1], new Int32Double { Int32 = 123456789, Double = 3.42 });

            Int32Double actual = Int32Double.Aligned(unaligned);
            Assert.Equal(123456789, actual.Int32);
            Assert.Equal(3.42, actual.Double);
        }

        [Fact]
        public static unsafe void WriteUnaligned_Ptr_Int32()
        {
            byte[] unaligned = new byte[sizeof(Int32Double) + 1];

            fixed (byte* p = unaligned)
            {
                Unsafe.WriteUnaligned(p + 1, 123456789);
            }

            int actual = Int32Double.Aligned(unaligned).Int32;
            Assert.Equal(123456789, actual);
        }

        [Fact]
        public static unsafe void WriteUnaligned_Ptr_Double()
        {
            byte[] unaligned = new byte[sizeof(Int32Double) + 1];

            fixed (byte* p = unaligned)
            {
                Unsafe.WriteUnaligned(p + 9, 3.42);
            }

            double actual = Int32Double.Aligned(unaligned).Double;
            Assert.Equal(3.42, actual);
        }

        [Fact]
        public static unsafe void WriteUnaligned_Ptr_Struct()
        {
            byte[] unaligned = new byte[sizeof(Int32Double) + 1];

            fixed (byte* p = unaligned)
            {
                Unsafe.WriteUnaligned(p + 1, new Int32Double { Int32 = 123456789, Double = 3.42 });
            }

            Int32Double actual = Int32Double.Aligned(unaligned);
            Assert.Equal(123456789, actual.Int32);
            Assert.Equal(3.42, actual.Double);
        }

        [Fact]
        public static void Unbox_Int32()
        {
            object box = 42;

            Assert.True(Unsafe.AreSame(ref Unsafe.Unbox<int>(box), ref Unsafe.Unbox<int>(box)));

            Assert.Equal(42, (int)box);
            Assert.Equal(42, Unsafe.Unbox<int>(box));

            ref int value = ref Unsafe.Unbox<int>(box);
            value = 84;
            Assert.Equal(84, (int)box);
            Assert.Equal(84, Unsafe.Unbox<int>(box));

            Assert.Throws<InvalidCastException>(() => Unsafe.Unbox<Byte4>(box));
        }

        [Fact]
        public static void Unbox_CustomValueType()
        {
            object box = new Int32Double();

            Assert.Equal(0, ((Int32Double)box).Double);
            Assert.Equal(0, ((Int32Double)box).Int32);

            ref Int32Double value = ref Unsafe.Unbox<Int32Double>(box);
            value.Double = 42;
            value.Int32 = 84;

            Assert.Equal(42, ((Int32Double)box).Double);
            Assert.Equal(84, ((Int32Double)box).Int32);

            Assert.Throws<InvalidCastException>(() => Unsafe.Unbox<bool>(box));
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Byte4
    {
        [FieldOffset(0)]
        public byte B0;
        [FieldOffset(1)]
        public byte B1;
        [FieldOffset(2)]
        public byte B2;
        [FieldOffset(3)]
        public byte B3;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Byte4Short2
    {
        [FieldOffset(0)]
        public byte B0;
        [FieldOffset(1)]
        public byte B1;
        [FieldOffset(2)]
        public byte B2;
        [FieldOffset(3)]
        public byte B3;
        [FieldOffset(4)]
        public short S4;
        [FieldOffset(6)]
        public short S6;
    }

    public unsafe struct Byte512
    {
        public fixed byte Bytes[512];
    }

    [StructLayout(LayoutKind.Explicit, Size=16)]
    public unsafe struct Int32Double
    {
        [FieldOffset(0)]
        public int Int32;
        [FieldOffset(8)]
        public double Double;

        public static unsafe byte[] Unaligned(int i, double d)
        {
            var aligned = new Int32Double { Int32 = i, Double = d };
            var unaligned = new byte[sizeof(Int32Double) + 1];

            fixed (byte* p = unaligned)
            {
                Buffer.MemoryCopy(&aligned, p + 1, sizeof(Int32Double), sizeof(Int32Double));
            }

            return unaligned;
        }

        public static unsafe Int32Double Aligned(byte[] unaligned)
        {
            var aligned = new Int32Double();

            fixed (byte* p = unaligned)
            {
                Buffer.MemoryCopy(p + 1, &aligned, sizeof(Int32Double), sizeof(Int32Double));
            }

            return aligned;
        }
    }
}
