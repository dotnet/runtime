// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// FileEntry: Records information about embedded files.
    ///
    /// The bundle manifest records the following meta-data for each
    /// file embedded in the bundle:
    /// * Type       (1 byte)
    /// * NameLength (7-bit extension encoding, typically 1 byte)
    /// * Name       ("NameLength" Bytes)
    /// * Offset     (Int64)
    /// * Size       (Int64)
    /// === present only in bundle version 3+
    /// * CompressedSize   (Int64)  0 indicates No Compression
    /// </summary>
    public class FileEntry
    {
        public readonly uint BundleMajorVersion;

        public readonly long Offset;
        public readonly long Size;
        public readonly long CompressedSize;
        public readonly FileType Type;
        public readonly string RelativePath; // Path of an embedded file, relative to the Bundle source-directory.

        public const char DirectorySeparatorChar = '/';

        public FileEntry(FileType fileType, string relativePath, long offset, long size, long compressedSize, uint bundleMajorVersion)
        {
            BundleMajorVersion = bundleMajorVersion;
            Type = fileType;
            RelativePath = relativePath.Replace('\\', DirectorySeparatorChar);
            Offset = offset;
            Size = size;
            CompressedSize = compressedSize;
        }

        public void Write(BinaryWriter writer)
        {
            var start = writer.BaseStream.Position;
            writer.Write(Offset);
            writer.Write(Size);
            // compression is used only in version 6.0+
            if (BundleMajorVersion >= 6)
            {
                writer.Write(CompressedSize);
            }
            writer.Write((byte)Type);
            writer.Write(RelativePath);
            Debug.Assert(writer.BaseStream.Position - start == GetFileEntryLength(BundleMajorVersion, RelativePath),
                $"FileEntry size mismatch. Expected: {GetFileEntryLength(BundleMajorVersion, RelativePath)}, Actual: {writer.BaseStream.Position - start}");
        }

        /// <summary>
        /// Returns the length of the FileEntry in the manifest in bytes. This is not the size of the file itself.
        /// </summary>
        public static uint GetFileEntryLength(uint bundleMajorVersion, string bundleRelativePath)
        {
           return sizeof(long) // Offset
                    + sizeof(long) // Size
                    + (bundleMajorVersion >= 6 ? sizeof(long) : 0u) // CompressedSize
                    + sizeof(FileType) // Type (FileType)
                    + Bundler.GetBinaryWriterStringLength(bundleRelativePath);
        }

        public override string ToString() => $"{RelativePath} [{Type}] @{Offset} Sz={Size} CompressedSz={CompressedSize}";
    }
}
