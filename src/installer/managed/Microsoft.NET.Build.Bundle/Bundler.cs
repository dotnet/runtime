// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection.PortableExecutable;

namespace Microsoft.NET.Build.Bundle
{
    /// <summary>
    /// Bundler: Functionality to embed the managed app and its dependencies
    /// into the host native binary.
    /// </summary>
    public class Bundler
    {
        string HostName;
        string SourceDir;
        string OutputDir;
        bool EmbedPDBs;

        string Application;
        string DepsJson;
        string RuntimeConfigJson;
        string RuntimeConfigDevJson;

        /// <summary>
        /// Align embedded assemblies such that they can be loaded 
        /// directly from memory-mapped bundle.
        /// TBD: Set the correct value of alignment while working on 
        /// the runtime changes to load the embedded assemblies.
        /// </summary>
        const int AssemblyAlignment = 16;

        public static string Version => (Manifest.MajorVersion + "." + Manifest.MinorVersion);

        public Bundler(string hostName, string sourceDir, string outputDir, bool embedPDBs)
        {
            SourceDir = sourceDir;
            OutputDir = outputDir;
            HostName = hostName;
            EmbedPDBs = embedPDBs;
        }

        void ValidateFiles()
        {
            // Check required directories
            if (!Directory.Exists(SourceDir))
            {
                throw new BundleException("Dirctory not found: " + SourceDir);
            }
            if (!Directory.Exists(OutputDir))
            {
                throw new BundleException("Dirctory not found: " + OutputDir);
            }

            // Convert relative paths to absolute paths.
            SourceDir = Path.GetFullPath(SourceDir);
            OutputDir = Path.GetFullPath(OutputDir);

            // Set default names
            string baseName = Path.GetFileNameWithoutExtension(HostName);
            Application = baseName + ".dll";
            DepsJson = baseName + ".deps.json";
            RuntimeConfigJson = baseName + ".runtimeconfig.json";
            RuntimeConfigDevJson = baseName + ".runtimeconfig.dev.json";

            // Check that required files exist on disk.
            Action<string> checkFileExists = (string name) =>
            {
                string path = Path.Combine(SourceDir, name);
                if (!File.Exists(path))
                {
                    throw new BundleException("File not found: " + path);
                }
            };

            checkFileExists(HostName);
            checkFileExists(Application);
            // The *.json files may or may not exist.
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

            if (fileRelativePath.Equals(Application))
            {
                return FileType.Application;
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

        void GenerateBundle()
        {
            string bundlePath = Path.Combine(OutputDir, HostName);

            if (File.Exists(bundlePath))
            {
                Program.Log($"Ovewriting existing File {bundlePath}");
            }

            // Start with a copy of the host executable.
            // Copy the file to preserve its permissions.
            File.Copy(Path.Combine(SourceDir, HostName), bundlePath, overwrite: true);

            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(bundlePath)))
            {
                Stream bundle = writer.BaseStream;
                Manifest manifest = new Manifest();

                bundle.Position = bundle.Length;
                int sourceDirLen = SourceDir.Length + 1;

                // Get all files in the source directory and all sub-directories.
                string[] sources = Directory.GetFiles(SourceDir, searchPattern: "*", searchOption: SearchOption.AllDirectories);
            
                // Sort the file names to keep the bundle construction deterministic.
                Array.Sort(sources, StringComparer.Ordinal);

                foreach (string filePath in sources)
                {
                    // filePath is the full-path of files within source directory, and any of its sub-directories.
                    // We only need the relative paths with respect to the source directory.
                    string relativePath = filePath.Substring(sourceDirLen);

                    if (!ShouldEmbed(relativePath))
                    {
                        Program.Log($"Skip: {relativePath}");
                        continue;
                    }

                    using (FileStream file = File.OpenRead(filePath))
                    {
                        FileType type = InferType(relativePath, file);
                        long startOffset = AddToBundle(bundle, file, type);
                        FileEntry entry = new FileEntry(type, relativePath, startOffset, file.Length);
                        manifest.Files.Add(entry);
                        Program.Log($"Embed: {entry}");
                    }
                }

                manifest.Write(writer);
                Program.Log($"Bundle: Path={bundlePath} Size={bundle.Length}");
            }
        }

        public void MakeBundle()
        {
            ValidateFiles();
            GenerateBundle();
        }
    }
}

