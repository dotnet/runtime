// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    ///  BundleManifest is a description of the contents of a bundle file.
    ///  This class handles creation and consumption of bundle-manifests.
    ///  
    ///  Here is the description of the Bundle Layout:
    ///  _______________________________________________
    ///  AppHost 
    ///
    ///
    /// ------------Embedded Files ---------------------
    /// The embedded files including the app, its
    /// configuration files, dependencies, and 
    /// possibly the runtime.
    /// 
    /// 
    /// 
    /// 
    /// 
    /// 
    ///
    /// ------------ Bundle Header -------------
    ///     MajorVersion
    ///     MinorVersion
    ///     NumEmbeddedFiles
    ///     ExtractionID
    ///     
    /// - - - - - - Manifest Entries - - - - - - - - - - -
    ///     Series of FileEntries (for each embedded file)
    ///     [File Type, Name, Offset, Size information]
    ///     
    ///     
    /// 
    /// _________________________________________________
    /// </summary>
    public class Manifest
    {
        public const uint MajorVersion = 1;
        public const uint MinorVersion = 0;

        // Bundle ID is a string that is used to uniquely 
        // identify this bundle. It is choosen to be compatible
        // with path-names so that the AppHost can use it in
        // extraction path.
        public readonly string BundleID;

        public List<FileEntry> Files;

        public Manifest()
        {
            Files = new List<FileEntry>();
            BundleID = Path.GetRandomFileName();
        }

        public FileEntry AddEntry(FileType type, string relativePath, long offset, long size)
        {
            FileEntry entry = new FileEntry(type, relativePath, offset, size);
            Files.Add(entry);
            return entry;
        }

        public long Write(BinaryWriter writer)
        {
            long startOffset = writer.BaseStream.Position;

            // Write the bundle header
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
            writer.Write(Files.Count());
            writer.Write(BundleID);

            // Write the manifest entries
            foreach (FileEntry entry in Files)
            {
                entry.Write(writer);
            }

            return startOffset;
        }

        public bool Contains(string relativePath)
        {
            return Files.Any(entry => relativePath.Equals(entry.RelativePath));
        }
    }
}
