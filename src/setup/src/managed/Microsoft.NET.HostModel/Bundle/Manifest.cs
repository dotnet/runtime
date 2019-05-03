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
    /// ------------ Manifest Header -------------
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
    /// - - - - - - Manifest Footer - - - - - - - - - - -
    ///   Manifest header offset
    ///   Bundle Signature
    /// _________________________________________________
    /// </summary>
    public class Manifest
    {
        public const string Signature = ".NetCoreBundle";
        public const uint MajorVersion = 0;
        public const uint MinorVersion = 1;

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

        public Manifest(string bundleID)
        {
            Files = new List<FileEntry>();
            BundleID = bundleID;
        }

        public long Write(BinaryWriter writer)
        {
            long startOffset = writer.BaseStream.Position;

            // Write the manifest header
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
            writer.Write(Files.Count());
            writer.Write(BundleID);

            // Write the manifest entries
            foreach (FileEntry entry in Files)
            {
                entry.Write(writer);
            }

            // Write the manifest footer
            writer.Write(startOffset);
            writer.Write(Signature);

            return startOffset;
        }

        public static Manifest Read(BinaryReader reader)
        {
            // Read the manifest footer

            // signatureSize is one byte longer, for the length encoding.
            long signatureSize = Signature.Length + 1;
            reader.BaseStream.Position = reader.BaseStream.Length - signatureSize;
            string signature = reader.ReadString();
            if (!signature.Equals(Signature))
            {
                throw new BundleException("Invalid Bundle");
            }

            // The manifest header offset resides just behind the signature.
            reader.BaseStream.Position = reader.BaseStream.Length - signatureSize - sizeof(long);
            long headerOffset = reader.ReadInt64();

            // Read the manifest header
            reader.BaseStream.Position = headerOffset;
            uint majorVersion = reader.ReadUInt32();
            uint minorVersion = reader.ReadUInt32();
            int fileCount = reader.ReadInt32();
            string bundleID = reader.ReadString(); // Bundle ID

            if (majorVersion != MajorVersion || minorVersion != MinorVersion)
            {
                throw new BundleException("Extraction failed: Invalid Version");
            }

            // Read the manifest entries
            Manifest manifest = new Manifest(bundleID);
            for (long i = 0; i < fileCount; i++)
            {
                manifest.Files.Add(FileEntry.Read(reader));
            }

            return manifest;
        }
    }
}

