// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;

internal static class BinaryReaderExtensions
{
    /// <summary>
    /// Reads a C-style, zero-terminated string from memory.
    /// </summary>
    public static string ReadZString(this BinaryReader reader)
    {
        var sb = new StringBuilder();
        byte nextByte = reader.ReadByte();
        while (nextByte != 0)
        {
            sb.Append((char)nextByte);
            nextByte = reader.ReadByte();
        }
        return sb.ToString();
    }
}
