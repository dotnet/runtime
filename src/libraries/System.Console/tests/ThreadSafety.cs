// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class ThreadSafety
{
    const int NumberOfIterations = 100;

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public static void OpenStandardXXXCanBeCalledConcurrently()
    {
        Parallel.For(0, NumberOfIterations, i =>
        {
            using (Stream s = Console.OpenStandardInput())
            {
                Assert.NotNull(s);
            }
        });

        Parallel.For(0, NumberOfIterations, i =>
        {
            using (Stream s = Console.OpenStandardOutput())
            {
                Assert.NotNull(s);
            }
        });

        Parallel.For(0, NumberOfIterations, i =>
        {
            using (Stream s = Console.OpenStandardError())
            {
                Assert.NotNull(s);
            }
        });
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public static void SetStandardXXXCanBeCalledConcurrently()
    {
        TextReader savedStandardInput = Console.In;
        TextWriter savedStandardOutput = Console.Out;
        TextWriter savedStandardError = Console.Error;

        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (StreamReader sr = new StreamReader(memStream))
                {
                    using (StreamWriter sw = new StreamWriter(memStream))
                    {
                        Parallel.For(0, NumberOfIterations, i =>
                        {
                            Console.SetIn(sr);
                        });

                        Parallel.For(0, NumberOfIterations, i =>
                        {
                            Console.SetOut(sw);
                        });

                        Parallel.For(0, NumberOfIterations, i =>
                        {
                            Console.SetOut(sw);
                        });
                    }

                }
            }
        }
        finally
        {
            Console.SetIn(savedStandardInput);
            Console.SetOut(savedStandardOutput);
            Console.SetError(savedStandardError);
        }
    }

    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "Not supported on Browser, iOS, MacCatalyst, or tvOS.")]
    public static void ReadMayBeCalledConcurrently()
    {
        const char TestChar = '+';

        TextReader savedStandardInput = Console.In;
        try
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(memStream))
                {
                    for (int i = 0; i < NumberOfIterations; i++)
                    {
                        sw.Write(TestChar);
                    }

                    sw.Flush();

                    memStream.Seek(0, SeekOrigin.Begin);

                    using (StreamReader sr = new StreamReader(memStream))
                    {
                        Console.SetIn(sr);

                        Parallel.For(0, NumberOfIterations, i =>
                        {
                            Assert.Equal(TestChar, Console.Read());
                        });

                        // We should be at EOF now.
                        Assert.Equal(-1, Console.Read());
                    }
                }
            }
        }
        finally
        {
            Console.SetIn(savedStandardInput);
        }
    }
}
