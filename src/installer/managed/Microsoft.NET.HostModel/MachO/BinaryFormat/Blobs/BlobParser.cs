// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Class that provides methods for reading blob instances from a Mach-O file.
/// </summary>
internal static class BlobParser
{
    /// <summary>
    /// Reads a blob from a file at the specified offset.
    /// </summary>
    /// <param name="reader">The file to read from.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <returns>The created blob.</returns>
    public static IBlob ParseBlob(IMachOFileReader reader, long offset)
    {
        var magic = (BlobMagic)reader.ReadUInt32BigEndian(offset);
        return magic switch
        {
            BlobMagic.CodeDirectory => new CodeDirectoryBlob(SimpleBlob.Read(reader, offset)),
            BlobMagic.Requirements => new RequirementsBlob(SuperBlob.Read(reader, offset)),
            BlobMagic.CmsWrapper => new CmsWrapperBlob(SimpleBlob.Read(reader, offset)),
            BlobMagic.EmbeddedSignature => new EmbeddedSignatureBlob(SuperBlob.Read(reader, offset)),
            _ => SimpleBlob.Read(reader, offset)
        };
    }
}
