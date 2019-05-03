// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection.PortableExecutable;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// Bundler: Functionality to embed the managed app and its dependencies
    /// into the host native binary.
    /// </summary>
    public class Bundler
    {
        readonly string HostName;
        readonly string OutputDir;
        readonly bool EmbedPDBs;
        readonly string DepsJson;
        readonly string RuntimeConfigJson;
        readonly string RuntimeConfigDevJson;

        readonly Trace trace;
        public readonly Manifest BundleManifest;

        /// <summary>
        /// Align embedded assemblies such that they can be loaded 
        /// directly from memory-mapped bundle.
        /// TBD: Set the correct value of alignment while working on 
        /// the runtime changes to load the embedded assemblies.
        /// </summary>
        const int AssemblyAlignment = 16;

        public static string Version => (Manifest.MajorVersion + "." + Manifest.MinorVersion);

        public Bundler(string hostName, string outputDir, bool embedPDBs = false, bool diagnosticOutput = false)
        {
            HostName = hostName;
            OutputDir = Path.GetFullPath(string.IsNullOrEmpty(outputDir) ? Environment.CurrentDirectory : outputDir);

            string baseName = Path.GetFileNameWithoutExtension(HostName);
            DepsJson = baseName + ".deps.json";
            RuntimeConfigJson = baseName + ".runtimeconfig.json";
            RuntimeConfigDevJson = baseName + ".runtimeconfig.dev.json";

            EmbedPDBs = embedPDBs;
            trace = new Trace(diagnosticOutput);
            BundleManifest = new Manifest();
        }

        /// <summary>
        /// Embed 'file' into 'bundle'
        /// </summary>
        /// <returns>Returns the offset of the start 'file' within 'bundle'</returns>

        long AddToBundle(Stream bundle, Stream file, FileType type = FileType.Extract)
        {
            // Allign assemblies, since they are loaded directly from bundle
            if (type == FileType.Assembly)
            {
                long misalignment = (bundle.Position % AssemblyAlignment);

                if (misalignment != 0)
                {
                    long padding = AssemblyAlignment - misalignment;
                    bundle.Position += padding;
                }
            }

            file.Position = 0;
            long startOffset = bundle.Position;
            file.CopyTo(bundle);

            return startOffset;
        }

        bool ShouldEmbed(string fileRelativePath)
        {
            if (fileRelativePath.Equals(HostName))
            {
                // The bundle starts with the host, so ignore it while embedding.
                return false;
            }

            if (fileRelativePath.Equals(RuntimeConfigDevJson))
            {
                // Ignore the machine specific configuration file.
                return false;
            }

            if (Path.GetExtension(fileRelativePath).ToLower().Equals(".pdb"))
            {
                return EmbedPDBs;
            }

            return true;
        }

        FileType InferType(string fileRelativePath, Stream file)
        {
            if (fileRelativePath.Equals(DepsJson))
            {
                return FileType.DepsJson;
            }

            if (fileRelativePath.Equals(RuntimeConfigJson))
            {
                return FileType.RuntimeConfigJson;
            }

            try
            {
                PEReader peReader = new PEReader(file);
                CorHeader corHeader = peReader.PEHeaders.CorHeader;
                if ((corHeader != null) && ((corHeader.Flags & CorFlags.ILOnly) != 0))
                {
                    return FileType.Assembly;
                }
            }
            catch (BadImageFormatException)
            {
            }

            return FileType.Extract;
        }

        /// <summary>
        /// Generate a bundle, given the specification of embedded files
        /// </summary>
        /// <param name="fileSpecs">
        /// An enumeration FileSpecs for the files to be embedded.
        /// </param>
        /// <returns>
        /// The full path the the generated bundle file
        /// </returns>
        /// <exceptions>
        /// ArgumentException if input is invalid
        /// IOExceptions and ArgumentExceptions from callees flow to the caller.
        /// </exceptions>
        public string GenerateBundle(IReadOnlyList<FileSpec> fileSpecs)
        {
            trace.Log($"Bundler version {Version}");

            if (fileSpecs.Any(x => !x.IsValid()))
            {
                throw new ArgumentException("Invalid input specification: Found entry with empty source-path or bundle-relative-path.");
            }

            string hostSource;
            try
            {
                hostSource = fileSpecs.Where(x => x.BundleRelativePath.Equals(HostName)).Single().SourcePath;
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException("Input must uniquely specify the host binary");
            }

            string bundlePath = Path.Combine(OutputDir, HostName);
            if (File.Exists(bundlePath))
            {
                trace.Log($"Ovewriting existing File {bundlePath}");
            }

            // Start with a copy of the host executable.
            // Copy the file to preserve its permissions.
            File.Copy(hostSource, bundlePath, overwrite: true);

            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(bundlePath)))
            {
                Stream bundle = writer.BaseStream;
                bundle.Position = bundle.Length;

                // Write the files from the specification into the bundle
                foreach (var fileSpec in fileSpecs)
                {
                    if (!ShouldEmbed(fileSpec.BundleRelativePath))
                    {
                        trace.Log($"Skip: {fileSpec.BundleRelativePath}");
                        continue;
                    }

                    using (FileStream file = File.OpenRead(fileSpec.SourcePath))
                    {
                        FileType type = InferType(fileSpec.BundleRelativePath, file);
                        long startOffset = AddToBundle(bundle, file, type);
                        FileEntry entry = new FileEntry(type, fileSpec.BundleRelativePath, startOffset, file.Length);
                        BundleManifest.Files.Add(entry);
                        trace.Log($"Embed: {entry}");
                    }
                }

                // Write the bundle manifest
                long manifestOffset = BundleManifest.Write(writer);
                trace.Log($"Manifest: Offset={manifestOffset}, Size={writer.BaseStream.Position - manifestOffset}");
                trace.Log($"Bundle: Path={bundlePath} Size={bundle.Length}");
            }

            return bundlePath;
        }

        string RelativePath(string dirFullPath, string fileFullPath)
        {
            // This function is used in lieu of Path.GetRelativePath because
            //   * Path.GetRelativePath() doesn't exist in netstandard2.0
            //   * This implementation is pretty much only intended for testing.
            //     SDK integration invokes GenerateBundle(fileSpecs) directly.
            // 
            // In later revisions, we should target netstandard2.1, and replace 
            // this function with Path.GetRelativePath().

            return fileFullPath.Substring(dirFullPath.TrimEnd(Path.DirectorySeparatorChar).Length).TrimStart(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Generate a bundle containind the (embeddable) files in sourceDir
        /// </summary>
        public string GenerateBundle(string sourceDir)
        {
            // Convert sourceDir to absolute path
            sourceDir = Path.GetFullPath(sourceDir);

            // Get all files in the source directory and all sub-directories.
            string[] sources = Directory.GetFiles(sourceDir, searchPattern: "*", searchOption: SearchOption.AllDirectories);

            // Sort the file names to keep the bundle construction deterministic.
            Array.Sort(sources, StringComparer.Ordinal);

            List<FileSpec> fileSpecs = new List<FileSpec>(sources.Length);
            foreach(var file in sources)
            {
                fileSpecs.Add(new FileSpec(file, RelativePath(sourceDir, file)));
            }

            return GenerateBundle(fileSpecs);
        }
    }
}

