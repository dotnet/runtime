// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

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
    ///     DepsJson Location [Version 2+]
    ///        Offset
    ///        Size
    ///     RuntimeConfigJson Location [Version 2+]
    ///        Offset
    ///        Size
    ///     Flags [Version 2+]
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
        // NetcoreApp3CompatMode flag is set on a .net5 app,
        // which chooses to build single-file apps in .netcore3.x compat mode,
        // by constructing the bundler with BundleAllConent option.
        // This mode is expected to be deprecated in future versions of .NET.
        [Flags]
        private enum HeaderFlags : ulong
        {
            None = 0,
            NetcoreApp3CompatMode = 1
        }

        // Bundle ID is a string that is used to uniquely
        // identify this bundle. It is choosen to be compatible
        // with path-names so that the AppHost can use it in
        // extraction path.
        public string BundleID { get; private set; }
        //Same as Path.GetRandomFileName
        private const int BundleIdLength = 12;
        private SHA256 bundleHash = SHA256.Create();
        public readonly uint BundleMajorVersion;
        // The Minor version is currently unused, and is always zero
        public const uint BundleMinorVersion = 0;
        private FileEntry DepsJsonEntry;
        private FileEntry RuntimeConfigJsonEntry;
        private HeaderFlags Flags;
        public List<FileEntry> Files;
        public string BundleVersion => $"{BundleMajorVersion}.{BundleMinorVersion}";

        public Manifest(uint bundleMajorVersion, bool netcoreapp3CompatMode = false)
        {
            BundleMajorVersion = bundleMajorVersion;
            Files = new List<FileEntry>();
            Flags = (netcoreapp3CompatMode) ? HeaderFlags.NetcoreApp3CompatMode : HeaderFlags.None;
        }

        public FileEntry AddEntry(FileType type, FileStream fileContent, string relativePath, long offset, long compressedSize, uint bundleMajorVersion)
        {
            if (bundleHash == null)
            {
                throw new InvalidOperationException("It is forbidden to change Manifest state after it was written or BundleId was obtained.");
            }

            FileEntry entry = new FileEntry(type, relativePath, offset, fileContent.Length, compressedSize, bundleMajorVersion);
            Files.Add(entry);

            fileContent.Position = 0;
            byte[] hashBytes = ComputeSha256Hash(fileContent);
            bundleHash.TransformBlock(hashBytes, 0, hashBytes.Length, hashBytes, 0);

            switch (entry.Type)
            {
                case FileType.DepsJson:
                    DepsJsonEntry = entry;
                    break;
                case FileType.RuntimeConfigJson:
                    RuntimeConfigJsonEntry = entry;
                    break;

                case FileType.Assembly:
                    break;

                default:
                    break;
            }

            return entry;
        }

        private static byte[] ComputeSha256Hash(Stream stream)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(stream);
            }
        }

        private string GenerateDeterministicId()
        {
            bundleHash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[] manifestHash = bundleHash.Hash;
            bundleHash.Dispose();
            bundleHash = null;

            return Convert.ToBase64String(manifestHash).Substring(BundleIdLength).Replace('/', '_');
        }

        public long Write(BinaryWriter writer)
        {
            BundleID = BundleID ?? GenerateDeterministicId();

            long startOffset = writer.BaseStream.Position;

            // Write the bundle header
            writer.Write(BundleMajorVersion);
            writer.Write(BundleMinorVersion);
            writer.Write(Files.Count);
            writer.Write(BundleID);

            if (BundleMajorVersion >= 2)
            {
                writer.Write((DepsJsonEntry != null) ? DepsJsonEntry.Offset : 0);
                writer.Write((DepsJsonEntry != null) ? DepsJsonEntry.Size : 0);

                writer.Write((RuntimeConfigJsonEntry != null) ? RuntimeConfigJsonEntry.Offset : 0);
                writer.Write((RuntimeConfigJsonEntry != null) ? RuntimeConfigJsonEntry.Size : 0);

                writer.Write((ulong)Flags);
            }

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
