using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace GitHub_21666
{
    // CRC32 is a special instruction that has 4-byte opcode but does not use SSE38 or SSE3A encoding,
    // so the compiler backend needs to specially check its code size.
    // Test LZCNT as well to ensure that future changes do not impact 3-byte opcode instructions.
    class GitHub_21666
    {
        const int Pass = 100;
        const int Fail = 0;

        static byte byteSF = 1;
        static ushort ushortSF = 1;
        static uint uintSF = 1;
        static ulong ulongSF = 1;

        readonly static byte[] byteArray = new byte[10];
        readonly static ushort[] ushortArray = new ushort[10];
        readonly static uint[] uintArray = new uint[10];
        readonly static ulong[] ulongArray = new ulong[10];

        static int Main(string[] args)
        {
            bool success = true;
            byteSF = 0;
            ushortSF = 0;
            uintSF = 0;
            ulongSF = 0;
            success = success && TestByteContainment();
            success = success && TestUInt16Containment();
            success = success && TestUInt32Containment();
            success = success && TestUInt64Containment();

            return success ? Pass : Fail;
        }

        static unsafe bool TestByteContainment()
        {
            byte value = (byte)0;
            byte* ptr = &value;
            if (Sse42.IsSupported)
            {
                if (Sse42.Crc32(0xffffffffU, (byte)0) != 0xad82acaeU)
                {
                    Console.WriteLine("TestByteContainment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, value) != 0xad82acaeU)
                {
                    Console.WriteLine("TestByteContainment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, *ptr) != 0xad82acaeU)
                {
                    Console.WriteLine("TestByteContainment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, byteArray[1]) != 0xad82acaeU)
                {
                    Console.WriteLine("TestByteContainment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, byteArray[*ptr + 1]) != 0xad82acaeU)
                {
                    Console.WriteLine("TestByteContainment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, byteSF) != 0xad82acaeU)
                {
                    Console.WriteLine("TestByteContainment failed on Crc32");
                    return false;
                }
            }

            return true;
        }

        static unsafe bool TestUInt16Containment()
        {
            ushort value = (ushort)0;
            ushort* ptr = &value;
            if (Sse42.IsSupported)
            {
                if (Sse42.Crc32(0xffffffffU, (ushort)0) != 0xe9e882dU)
                {
                    Console.WriteLine("TestUInt16Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, value) != 0xe9e882dU)
                {
                    Console.WriteLine("TestUInt16Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, *ptr) != 0xe9e882dU)
                {
                    Console.WriteLine("TestUInt16Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, ushortArray[1]) != 0xe9e882dU)
                {
                    Console.WriteLine("TestUInt16Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, ushortArray[*ptr + 1]) != 0xe9e882dU)
                {
                    Console.WriteLine("TestUInt16Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, ushortSF) != 0xe9e882dU)
                {
                    Console.WriteLine("TestUInt16Containment failed on Crc32");
                    return false;
                }
            }

            return true;
        }

        static unsafe bool TestUInt32Containment()
        {
            uint value = (uint)0;
            uint* ptr = &value;
            if (Lzcnt.IsSupported)
            {
                if (Lzcnt.LeadingZeroCount(*ptr) != 32)
                {
                    Console.WriteLine("TestUInt32Containment failed on LeadingZeroCount");
                    return false;
                }

                if (Lzcnt.LeadingZeroCount(uintArray[2]) != 32)
                {
                    Console.WriteLine("TestUInt32Containment failed on LeadingZeroCount");
                    return false;
                }

                if (Lzcnt.LeadingZeroCount(uintArray[*ptr + 2]) != 32)
                {
                    Console.WriteLine("TestUInt32Containment failed on LeadingZeroCount");
                    return false;
                }

            }

            uint* ptr1 = &value;
            if (Sse42.IsSupported)
            {
                if (Sse42.Crc32(0xffffffffU, (uint)0) != 0xb798b438U)
                {
                    Console.WriteLine("TestUInt32Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, value) != 0xb798b438U)
                {
                    Console.WriteLine("TestUInt32Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, *ptr1) != 0xb798b438U)
                {
                    Console.WriteLine("TestUInt32Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, uintArray[1]) != 0xb798b438U)
                {
                    Console.WriteLine("TestUInt32Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, uintArray[*ptr + 1]) != 0xb798b438U)
                {
                    Console.WriteLine("TestUInt32Containment failed on Crc32");
                    return false;
                }

                if (Sse42.Crc32(0xffffffffU, uintSF) != 0xb798b438U)
                {
                    Console.WriteLine("TestUInt32Containment failed on Crc32");
                    return false;
                }
            }

            return true;
        }

        static unsafe bool TestUInt64Containment()
        {
            ulong value = (ulong)0;
            ulong* ptr = &value;
            if (Lzcnt.X64.IsSupported)
            {
                if (Lzcnt.X64.LeadingZeroCount(*ptr) != 64)
                {
                    Console.WriteLine("TestUInt64Containment failed on LeadingZeroCount");
                    return false;
                }

                if (Lzcnt.X64.LeadingZeroCount(ulongArray[2]) != 64)
                {
                    Console.WriteLine("TestUInt64Containment failed on LeadingZeroCount");
                    return false;
                }

                if (Lzcnt.X64.LeadingZeroCount(ulongArray[*ptr + 2]) != 64)
                {
                    Console.WriteLine("TestUInt64Containment failed on LeadingZeroCount");
                    return false;
                }

            }

            ulong* ptr1 = &value;

            if (Sse42.X64.IsSupported)
            {
                if (Sse42.X64.Crc32(0xffffffffffffffffUL, 0) != 0x0000000073d74d75UL)
                {
                    Console.WriteLine("TestUInt64Containment failed on Crc32");
                    return false;
                }

                if (Sse42.X64.Crc32(0xffffffffffffffffUL, value) != 0x0000000073d74d75UL)
                {
                    Console.WriteLine("TestUInt64Containment failed on Crc32");
                    return false;
                }

                if (Sse42.X64.Crc32(0xffffffffffffffffUL, *ptr1) != 0x0000000073d74d75UL)
                {
                    Console.WriteLine("TestUInt64Containment failed on Crc32");
                    return false;
                }

                if (Sse42.X64.Crc32(0xffffffffffffffffUL, ulongArray[1]) != 0x0000000073d74d75UL)
                {
                    Console.WriteLine("TestUInt64Containment failed on Crc32");
                    return false;
                }

                if (Sse42.X64.Crc32(0xffffffffffffffffUL, ulongArray[*ptr + 1]) != 0x0000000073d74d75UL)
                {
                    Console.WriteLine("TestUInt64Containment failed on Crc32");
                    return false;
                }

                if (Sse42.X64.Crc32(0xffffffffffffffffUL, ulongSF) != 0x0000000073d74d75UL)
                {
                    Console.WriteLine("TestUInt64Containment failed on Crc32");
                    return false;
                }
            }

            return true;
        }
    }
}
