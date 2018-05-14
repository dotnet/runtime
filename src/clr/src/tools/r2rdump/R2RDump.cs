// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace R2RDump
{
    class R2RDump
    {
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    throw new System.ArgumentException("File name must be passed as argument");
                }

                R2RReader r2r = new R2RReader(args[0]);

                if (r2r.IsR2R)
                {
                    Console.WriteLine($"Filename: {r2r.Filename}");
                    Console.WriteLine($"Machine: {r2r.Machine}");
                    Console.WriteLine($"ImageBase: 0x{r2r.ImageBase:X8}");

                    Console.WriteLine("============== R2R Header ==============");
                    Console.WriteLine(r2r.R2RHeader.ToString());
                    for (int i = 0; i < r2r.R2RHeader.NumberOfSections; i++)
                    {
                        Console.WriteLine("------------------");
                        Console.WriteLine(r2r.R2RHeader.Sections[i].ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
                return 1;
            }
            return 0;
        }
    }
}
