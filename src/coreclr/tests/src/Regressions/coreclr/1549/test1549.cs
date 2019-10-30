// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

public class Test
{
    public static int Main()
    {
        try
        {
            Encoding enc = Encoding.GetEncoding("utf-16");
            Decoder dec = enc.GetDecoder();
            int charPos = 0;
            byte[] inBytes1 = new byte[] { 0x87 };
            byte[] inBytes2 = new byte[] { 0x90 };
            char[] outChars = new char[4];

            int i = dec.GetChars(inBytes1, 0, inBytes1.Length, outChars, charPos, false);
            charPos += i;
            if (i != 0)
            {
                Console.WriteLine("!!!ERROR-001: Incorrect number of characters decoded. Expected: 0, Actual: " + i.ToString());
                Console.WriteLine("FAIL");
                return 91;
            }

            i = dec.GetChars(inBytes2, 0, inBytes2.Length, outChars, charPos, false);
            charPos += i;
            if (i != 1)
            {
                Console.WriteLine("!!!ERROR-002: Incorrect number of characters decoded. Expected: 1, Actual: " + i.ToString());
                Console.WriteLine("FAIL");
                return 92;
            }


            if (outChars[0] != '\u9087')
            {
                Console.WriteLine("!!!ERROR-003: Incorrect character decoded. Expected: 9087, Actual: " + ((int)outChars[0]).ToString("x"));
                Console.WriteLine("FAIL");
                return 93;
            }

            for (int j = 1; j < 4; j++)
            {
                if (outChars[j] != '\u0000')
                {
                    Console.WriteLine("!!!ERROR-004: Incorrect character decoded at index " + j.ToString() + ". Expected: 0000, Actual: " + ((int)outChars[j]).ToString("x"));
                    Console.WriteLine("FAIL");
                    return 94;
                } 
            }

            Console.WriteLine("Pass");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("!!!ERROR-002: Unexpected exception : " + e);
            Console.WriteLine("FAIL");
            return 101;
        }
    }
}