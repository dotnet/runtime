// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

#if HOST_MODEL
namespace Microsoft.NET.HostModel.MachO;
#else
namespace ILCompiler.Reflection.ReadyToRun.MachO;
#endif

/// <summary>
/// A managed object containing relevant information for AdHoc signing a Mach-O file.
/// The object is created from a memory mapped file, and a signature can be calculated from the memory mapped file.
/// However, since a memory mapped file cannot be extended, the signature is written to a file stream.
/// </summary>
internal unsafe partial class MachObjectFile
{
    public static bool IsMachOImage(string filePath)
    {
        using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
        {
            if (reader.BaseStream.Length < 256) // Header size
            {
                return false;
            }
            uint magic = reader.ReadUInt32();
            return Enum.IsDefined(typeof(MachMagic), magic);
        }
    }
}
