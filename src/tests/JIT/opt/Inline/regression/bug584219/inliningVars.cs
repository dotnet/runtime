// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace ConsoleApplication1
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int result = 100;
            int byteCount = 0;
            int index = 0;
            byte[] buffer = new byte[100];
            buffer[0] = 0x12;
            buffer[1] = 0x34;
            buffer[2] = 0x56;
            buffer[3] = 0x78;
            short shrt = SerializerShort.Deserialize(buffer, index + byteCount, out byteCount);
            if (shrt != 0x3412) result = 101;
            Console.WriteLine(shrt.ToString("x")); // Prints "3412"    
            byteCount = 0;
            int i1 = SerializerInt.Deserialize(buffer, index + byteCount, out byteCount);
            if ((i1 != 0x78563412) || (index != 0) || (byteCount != 1)) result = 101;
            Console.WriteLine(i1.ToString("x")); // Prints "78563412"      
            Console.WriteLine(index); // Prints "0"      
            Console.WriteLine(byteCount); // Should be Prints 1 !! Prints "2"      
            return result;
        }

        public static class SerializerShort
        {
            public static short Deserialize(byte[] buffer, int index, out int byteCount)
            {
                byteCount = sizeof(short);
                return BitConverter.ToInt16(buffer, index);
            }
        }
        public static class SerializerInt
        {
            public static int Deserialize(byte[] buffer, int index, out int byteCount)
            {
                byteCount = sizeof(byte);
                return BitConverter.ToInt32(buffer, index);
            }
        }
    }
}
