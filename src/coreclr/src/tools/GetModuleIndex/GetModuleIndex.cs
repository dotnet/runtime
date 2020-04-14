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
using Microsoft.FileFormats.MachO;
using Microsoft.FileFormats.PE;

public class GetModuleIndex
{
    public static int Main(string[] args)
    {
        if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
        {
            throw new ArgumentException("Invalid command line arguments");
        }
        string moduleFileName = args[0];
        string outputFileName = args[1];

        using (FileStream stream = File.OpenRead(moduleFileName))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var elfFile = new ELFFile(new StreamAddressSpace(stream));
                byte[] buildId = elfFile.BuildID;
                if (buildId != null)
                {
                    // First byte is the number of bytes total in the build id
                    string outputText = string.Format("0x{0:x2}, {1}", buildId.Length, ToHexString(buildId));
                    File.WriteAllText(outputFileName, outputText);
                }
                else
                {
                    throw new BadInputFormatException($"{moduleFileName} does not have a build id");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var peFile = new PEFile(new StreamAddressSpace(stream));
                // First byte is the number of bytes total in the index
                string outputText = string.Format("0x{0:x2}, {1} {2}", 8, ToHexString(peFile.Timestamp), ToHexString(peFile.SizeOfImage));
                File.WriteAllText(outputFileName, outputText);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var machoFile = new MachOFile(new StreamAddressSpace(stream));
                byte[] uuid = machoFile.Uuid;
                if (uuid != null)
                {
                    // First byte is the number of bytes total in the build id
                    string outputText = string.Format("0x{0:x2}, {1}", uuid.Length, ToHexString(uuid));
                    File.WriteAllText(outputFileName, outputText);
                }
                else
                {
                    throw new BadInputFormatException($"{moduleFileName} does not have a uuid");
                }
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
