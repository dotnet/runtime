// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace VectorWiden
{
    public class Program
    {
        private static byte[] _asciiBytes = Encoding.ASCII.GetBytes("text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7");
        private static string _asciiString = new string('\0', _asciiBytes.Length);
        private static string compareString = "text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7";

        [Fact]
        public static unsafe int TestEntryPoint()
        {
            fixed (byte* pBytes = &_asciiBytes[0])
            fixed (char* pString = _asciiString)
            {
                TryGetAsciiStringVectorWiden(pBytes, pString, _asciiBytes.Length);
            }
            if (String.Compare(_asciiString, compareString) != 0)
            {
                Console.WriteLine("Test Failed. Expected:\n");
                Console.WriteLine(compareString);
                Console.WriteLine("Test Failed. Got:\n");
                Console.WriteLine(_asciiString);
                return -1;
            }
            return 100;
        }

        public static unsafe bool TryGetAsciiStringVectorWiden(byte* input, char* output, int count)

        {
            // Calcuate end position
            var end = input + count;

            // Start as valid
            var isValid = true;

            do
            {
                // If Vector not-accelerated or remaining less than vector size
                if (!Vector.IsHardwareAccelerated || input > end - Vector<sbyte>.Count)
                {
                    if (IntPtr.Size == 8) // Use Intrinsic switch for branch elimination
                    {
                        // 64-bit: Loop longs by default
                        while (input <= end - sizeof(long))
                        {
                            isValid &= CheckBytesInAsciiRange(((long*)input)[0]);

                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                            output[2] = (char)input[2];
                            output[3] = (char)input[3];
                            output[4] = (char)input[4];
                            output[5] = (char)input[5];
                            output[6] = (char)input[6];
                            output[7] = (char)input[7];

                            input += sizeof(long);
                            output += sizeof(long);
                        }
                        if (input <= end - sizeof(int))
                        {
                            isValid &= CheckBytesInAsciiRange(((int*)input)[0]);

                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                            output[2] = (char)input[2];
                            output[3] = (char)input[3];

                            input += sizeof(int);
                            output += sizeof(int);
                        }
                    }
                    else
                    {
                        // 32-bit: Loop ints by default
                        while (input <= end - sizeof(int))
                        {
                            isValid &= CheckBytesInAsciiRange(((int*)input)[0]);

                            output[0] = (char)input[0];
                            output[1] = (char)input[1];
                            output[2] = (char)input[2];
                            output[3] = (char)input[3];

                            input += sizeof(int);
                            output += sizeof(int);
                        }
                    }
                    if (input <= end - sizeof(short))
                    {
                        isValid &= CheckBytesInAsciiRange(((short*)input)[0]);

                        output[0] = (char)input[0];
                        output[1] = (char)input[1];

                        input += sizeof(short);
                        output += sizeof(short);
                    }
                    if (input < end)
                    {
                        isValid &= CheckBytesInAsciiRange(((sbyte*)input)[0]);
                        output[0] = (char)input[0];
                    }

                    return isValid;
                }

                // do/while as entry condition already checked
                do
                {
                    var vector = Unsafe.AsRef<Vector<sbyte>>(input);
                    isValid &= CheckBytesInAsciiRange(vector);
                    // Vectorized widen, byte vector to two short vectors
                    Vector.Widen(vector, out Unsafe.AsRef<Vector<short>>(output),
                                 out Unsafe.AsRef<Vector<short>>(output + Vector<short>.Count));
                    input += Vector<sbyte>.Count;
                    output += Vector<sbyte>.Count;
                } while (input <= end - Vector<sbyte>.Count);

                // Vector path done, loop back to do non-Vector
                // If is a exact multiple of vector size, bail now
            } while (input > end - Vector<sbyte>.Count);

            return isValid;
        }

        private static bool CheckBytesInAsciiRange(Vector<sbyte> check)
        {
            // Vectorized byte range check, signed byte > 0 for 1-127
            return Vector.GreaterThanAll(check, Vector<sbyte>.Zero);
        }

        // Validate: bytes != 0 && bytes <= 127
        //  Subtract 1 from all bytes to move 0 to high bits
        //  bitwise or with self to catch all > 127 bytes
        //  mask off high bits and check if 0

        private static bool CheckBytesInAsciiRange(long check)
        {
            const long HighBits = unchecked((long)0x8080808080808080L);
            return (((check - 0x0101010101010101L) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(int check)
        {
            const int HighBits = unchecked((int)0x80808080);
            return (((check - 0x01010101) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(short check)
        {
            const short HighBits = unchecked((short)0x8080);
            return (((short)(check - 0x0101) | check) & HighBits) == 0;
        }

        private static bool CheckBytesInAsciiRange(sbyte check)
            => check > 0;
    }
}
