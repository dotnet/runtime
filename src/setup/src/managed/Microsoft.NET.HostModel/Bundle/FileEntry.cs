﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    /// </summary>
    public class FileEntry
    {
        public long Offset;
        public long Size;
        public FileType Type;
        public string RelativePath; // Path of an embedded file, relative to the Bundle source-directory.

        public FileEntry(FileType fileType, string relativePath, long offset, long size)
        {
            Type = fileType;
            RelativePath = relativePath.Replace(Path.DirectorySeparatorChar, Manifest.DirectorySeparatorChar);
            Offset = offset;
            Size = size;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Offset);
            writer.Write(Size);
            writer.Write((byte)Type);
            writer.Write(RelativePath);
        }

        public static FileEntry Read(BinaryReader reader)
        {
            long offset = reader.ReadInt64();
            long size = reader.ReadInt64();
            FileType type = (FileType)reader.ReadByte();
            string fileName = reader.ReadString();
            return new FileEntry(type, fileName, offset, size);
        }

        public override string ToString()
        {
            return String.Format($"{RelativePath} [{Type}] @{Offset} Sz={Size}");
        }
    }
}

