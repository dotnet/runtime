// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* 
 * Bug info:
 * Asserstion prop was comparing 2 range assertions incorrectly and keeping the wrong one as a result.
 * Thus assuming it didn't need the narrowing convert to byte. It thus returned the full 32 bit value instead of the lower 8 bits  alone.
 * 
 * Repro Steps:
 * Compile this program with option /optimize and execute it from a console window:
 * 
 * Actual Results:
 * value : 256, lowByte : 0, lowByteInt : 256
 *
 * Expected Results:
 * value : 256, lowByte : 0, lowByteInt : 0
*/

using System;

internal class Program
{
    private struct MyStruct
    {
        public ushort value;
        public MyStruct(ushort value)
        {
            this.value = value;
        }
    };

    static private MyStruct[] s_myObjects = { new MyStruct(0x0100) };

    private static int Main(string[] args)
    {
        MyStruct obj = s_myObjects[0];
        ushort value = obj.value;
        byte lowByte = (byte)(value & 0xff);
        int lowByteInt = lowByte; // here is the bug !
        Console.WriteLine(String.Format("value : {0}, lowByte : {1}, lowByteInt : {2}", value, lowByte, lowByteInt));
        return (lowByteInt == 0) ? 100 : 101;
    }
}
