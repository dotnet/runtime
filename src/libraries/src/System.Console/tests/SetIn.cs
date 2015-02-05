// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class SetIn
{
    [Fact]
    public static void SetInThrowsOnNull()
    {
        TextReader savedIn = Console.In;
        try
        {
            Assert.Throws<ArgumentNullException>(() => Console.SetIn(null));
        }
        finally
        {
            Console.SetIn(savedIn);
        }
    }

    [Fact]
    public static void SetInReadLine()
    {
        const string TextStringFormat = "Test {0}";

        TextReader oldInToRestore = Console.In;
        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(memStream))
                {
                    for (int i = 0; i < 20; i++)
                    {
                        sw.WriteLine(string.Format(TextStringFormat, i));
                    }

                    sw.Flush();
                    memStream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader sr = new StreamReader(memStream))
                    {
                        Console.SetIn(sr);
                        Assert.NotNull(Console.In);

                        for (int i = 0; i < 20; i++)
                        {
                            Assert.Equal(string.Format(TextStringFormat, i), Console.ReadLine());
                        }
                    }
                }
            }
        }
        finally
        {
            Console.SetIn(oldInToRestore);
        }
    }
}
