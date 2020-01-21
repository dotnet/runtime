// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.FileFormats;
using Microsoft.FileFormats.ELF;
using Microsoft.FileFormats.PE;

public class GetModuleIndex
{
    public static int Main(string[] args)
    {
        if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
        {
            throw new ArgumentException("No file name");
        }
        string fileName = args[0];

        using (FileStream stream = File.OpenRead(fileName))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var elfFile = new ELFFile(new StreamAddressSpace(stream));
                byte[] buildId = elfFile.BuildID;
                if (buildId != null)
                {
                    // First byte is the number of bytes total in the build id
                    Console.WriteLine("0x{0:x2}, {1}", buildId.Length, ToHexString(buildId));
                }
                else
                {
                    throw new BadInputFormatException($"{fileName} does not have a build id");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var peFile = new PEFile(new StreamAddressSpace(stream));
                // First byte is the number of bytes total in the index
                Console.WriteLine("0x{0:x2}, {1} {2}", 8, ToHexString(peFile.Timestamp), ToHexString(peFile.SizeOfImage));
            }
            else
            {
                throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
            }
        }
        return 0;
    }

    private static string ToHexString(uint value)
    {
        return ToHexString(BitConverter.GetBytes(value));
    }

    private static string ToHexString(byte[] bytes)
    {
        return string.Concat(bytes.Select(b => string.Format("0x{0:x2}, ", b)));
    }
}
