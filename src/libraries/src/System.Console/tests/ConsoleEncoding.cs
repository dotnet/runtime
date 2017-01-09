// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Xunit;

public class ConsoleEncoding
{
    public static IEnumerable<object[]> InputData()
    {
        yield return new object[] { "This is Ascii string" };
        yield return new object[] { "This is string have surrogates \uD800\uDC00" };
        yield return new object[] { "This string has non ascii charaters \u03b1\u00df\u0393\u03c0\u03a3\u03c3\u00b5" };
        yield return new object[] { "This string has invalid surrogates \uD800\uD800\uD800\uD800\uD800\uD800" };
        yield return new object[] { "\uD800" };
    }

    [Theory]
    [PlatformSpecific(TestPlatforms.Windows)]
    [MemberData(nameof(InputData))]
    public void TestEncoding(string inputString)
    {
        var outConsoleStream = Console.Out;
        var inConsoleStream = Console.In;

        try
        {
            byte [] inputBytes;
            byte [] inputBytesNoBom = Console.OutputEncoding.GetBytes(inputString);
            byte [] bom = Console.OutputEncoding.GetPreamble();

            if (bom.Length > 0)
            {
                inputBytes = new byte[inputBytesNoBom.Length + bom.Length];
                Array.Copy(bom, inputBytes, bom.Length);
                Array.Copy(inputBytesNoBom, 0, inputBytes, bom.Length, inputBytesNoBom.Length);
            }
            else
            {
                inputBytes = inputBytesNoBom;
            }

            byte[] outBytes = new byte[inputBytes.Length];
            using (MemoryStream ms = new MemoryStream(outBytes, true))
            {
                using (StreamWriter sw = new StreamWriter(ms, Console.OutputEncoding))
                {
                    Console.SetOut(sw);
                    Console.Write(inputString);
                }
            }

            Assert.Equal(inputBytes, outBytes);

            string inString = new String(Console.InputEncoding.GetChars(inputBytesNoBom));

            string outString;
            using (MemoryStream ms = new MemoryStream(inputBytes, false))
            {
                using (StreamReader sr = new StreamReader(ms, Console.InputEncoding))
                {
                    Console.SetIn(sr);
                    outString = Console.In.ReadToEnd();
                }
            }

            Assert.Equal(inString, outString);
        }
        finally
        {
            Console.SetOut(outConsoleStream);
            Console.SetIn(inConsoleStream);
        }
    }
}
