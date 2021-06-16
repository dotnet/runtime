// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.NET.HostModel.AppHost;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// Bundler: Functionality to embed the managed app and its dependencies
    /// into the host native binary.
    /// </summary>
    public class Bundler
    {
        public const uint BundlerMajorVersion = 6;
        public const uint BundlerMinorVersion = 0;

        private readonly string HostName;
        private readonly string OutputDir;
        private readonly string DepsJson;
        private readonly string RuntimeConfigJson;
        private readonly string RuntimeConfigDevJson;

        private readonly Trace Tracer;
        public readonly Manifest BundleManifest;
        private readonly TargetInfo Target;
        private readonly BundleOptions Options;

        public Bundler(string hostName,
                       string outputDir,
                       BundleOptions options = BundleOptions.None,
                       OSPlatform? targetOS = null,
                       Architecture? targetArch = null,
                       Version targetFrameworkVersion = null,
                       bool diagnosticOutput = false,
                       string appAssemblyName = null)
        {
            Tracer = new Trace(diagnosticOutput);

            HostName = hostName;
            OutputDir = Path.GetFullPath(string.IsNullOrEmpty(outputDir) ? Environment.CurrentDirectory : outputDir);
            Target = new TargetInfo(targetOS, targetArch, targetFrameworkVersion);

            if (Target.BundleMajorVersion < 6 &&
                (options & BundleOptions.EnableCompression) != 0)
            {
                throw new ArgumentException("Compression requires framework version 6.0 or above", nameof(options));
            }

            appAssemblyName ??= Target.GetAssemblyName(hostName);
            DepsJson = appAssemblyName + ".deps.json";
            RuntimeConfigJson = appAssemblyName + ".runtimeconfig.json";
            RuntimeConfigDevJson = appAssemblyName + ".runtimeconfig.dev.json";

            BundleManifest = new Manifest(Target.BundleMajorVersion, netcoreapp3CompatMode: options.HasFlag(BundleOptions.BundleAllContent));
            Options = Target.DefaultOptions | options;
        }

        private bool ShouldCompress(FileType type)
        {
            if (!Options.HasFlag(BundleOptions.EnableCompression))
            {
                return false;
            }

            switch (type)
            {
                case FileType.DepsJson:
                case FileType.RuntimeConfigJson:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Embed 'file' into 'bundle'
        /// </summary>
        /// <returns>
        /// startOffset: offset of the start 'file' within 'bundle'
        /// compressedSize: size of the compressed data, if entry was compressed, otherwise 0
        /// </returns>
        private (long startOffset, long compressedSize) AddToBundle(Stream bundle, Stream file, FileType type)
        {
            long startOffset = bundle.Position;
            if (ShouldCompress(type))
            {
                long fileLength = file.Length;
                file.Position = 0;

                // We use DeflateStream here.
                // It uses GZip algorithm, but with a trivial header that does not contain file info.
                using (DeflateStream compressionStream = new DeflateStream(bundle, CompressionLevel.Optimal, leaveOpen: true))
                {
                    file.CopyTo(compressionStream);
                }

                long compressedSize = bundle.Position - startOffset;
                if (compressedSize < fileLength * 0.75)
                {
                    return (startOffset, compressedSize);
                }

                // compression rate was not good enough
                // roll back the bundle offset and let the uncompressed code path take care of the entry.
                bundle.Seek(startOffset, SeekOrigin.Begin);
            }

            if (type == FileType.Assembly)
            {
                long misalignment = (bundle.Position % Target.AssemblyAlignment);

                if (misalignment != 0)
                {
                    long padding = Target.AssemblyAlignment - misalignment;
                    bundle.Position += padding;
                }
            }

            file.Position = 0;
            startOffset = bundle.Position;
            file.CopyTo(bundle);

            return (startOffset, 0);
        }

        private bool IsHost(string fileRelativePath)
        {
            return fileRelativePath.Equals(HostName);
        }

        private bool ShouldIgnore(string fileRelativePath)
        {
            return fileRelativePath.Equals(RuntimeConfigDevJson);
        }

        private bool ShouldExclude(FileType type, string relativePath)
        {
            switch (type)
            {
                case FileType.Assembly:
                case FileType.DepsJson:
                case FileType.RuntimeConfigJson:
                    return false;

                case FileType.NativeBinary:
                    return !Options.HasFlag(BundleOptions.BundleNativeBinaries) || Target.ShouldExclude(relativePath);

                case FileType.Symbols:
                    return !Options.HasFlag(BundleOptions.BundleSymbolFiles);

                case FileType.Unknown:
                    return !Options.HasFlag(BundleOptions.BundleOtherFiles);

                default:
                    Debug.Assert(false);
                    return false;
            }
        }

        private bool IsAssembly(string path, out bool isPE)
        {
            isPE = false;

            using (FileStream file = File.OpenRead(path))
            {
                try
                {
                    PEReader peReader = new PEReader(file);
                    CorHeader corHeader = peReader.PEHeaders.CorHeader;

                    isPE = true; // If peReader.PEHeaders doesn't throw, it is a valid PEImage
                    return corHeader != null;
                }
                catch (BadImageFormatException)
                {
                }
            }

            return false;
        }

        private FileType InferType(FileSpec fileSpec)
        {
            if (fileSpec.BundleRelativePath.Equals(DepsJson))
            {
                return FileType.DepsJson;
            }

            if (fileSpec.BundleRelativePath.Equals(RuntimeConfigJson))
            {
                return FileType.RuntimeConfigJson;
            }

            if (Path.GetExtension(fileSpec.BundleRelativePath).ToLowerInvariant().Equals(".pdb"))
            {
                return FileType.Symbols;
            }

            bool isPE;
            if (IsAssembly(fileSpec.SourcePath, out isPE))
            {
                return FileType.Assembly;
            }

            bool isNativeBinary = Target.IsWindows ? isPE : Target.IsNativeBinary(fileSpec.SourcePath);

            if (isNativeBinary)
            {
                return FileType.NativeBinary;
            }

            return FileType.Unknown;
        }

        /// <summary>
        /// Generate a bundle, given the specification of embedded files
        /// </summary>
        /// <param name="fileSpecs">
        /// An enumeration FileSpecs for the files to be embedded.
        ///
        /// Files in fileSpecs that are not bundled within the single file bundle,
        /// and should be published as separate files are marked as "IsExcluded" by this method.
        /// This doesn't include unbundled files that should be dropped, and not publised as output.
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
            Tracer.Log($"Bundler Version: {BundlerMajorVersion}.{BundlerMinorVersion}");
            Tracer.Log($"Bundle  Version: {BundleManifest.BundleVersion}");
            Tracer.Log($"Target Runtime: {Target}");
            Tracer.Log($"Bundler Options: {Options}");

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
                throw new ArgumentException("Invalid input specification: Must specify the host binary");
            }

            string bundlePath = Path.Combine(OutputDir, HostName);
            if (File.Exists(bundlePath))
            {
                Tracer.Log($"Ovewriting existing File {bundlePath}");
            }

            BinaryUtils.CopyFile(hostSource, bundlePath);

            // Note: We're comparing file paths both on the OS we're running on as well as on the target OS for the app
            // We can't really make assumptions about the file systems (even on Linux there can be case insensitive file systems
            // and vice versa for Windows). So it's safer to do case sensitive comparison everywhere.
            var relativePathToSpec = new Dictionary<string, FileSpec>(StringComparer.Ordinal);

            long headerOffset = 0;
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(bundlePath)))
            {
                Stream bundle = writer.BaseStream;
                bundle.Position = bundle.Length;

                foreach (var fileSpec in fileSpecs)
                {
                    string relativePath = fileSpec.BundleRelativePath;

                    if (IsHost(relativePath))
                    {
                        continue;
                    }

                    if (ShouldIgnore(relativePath))
                    {
                        Tracer.Log($"Ignore: {relativePath}");
                        continue;
                    }

                    FileType type = InferType(fileSpec);

                    if (ShouldExclude(type, relativePath))
                    {
                        Tracer.Log($"Exclude [{type}]: {relativePath}");
                        fileSpec.Excluded = true;
                        continue;
                    }

                    if (relativePathToSpec.TryGetValue(fileSpec.BundleRelativePath, out var existingFileSpec))
                    {
                        if (!string.Equals(fileSpec.SourcePath, existingFileSpec.SourcePath, StringComparison.Ordinal))
                        {
                            throw new ArgumentException($"Invalid input specification: Found entries '{fileSpec.SourcePath}' and '{existingFileSpec.SourcePath}' with the same BundleRelativePath '{fileSpec.BundleRelativePath}'");
                        }

                        // Exact duplicate - intentionally skip and don't include a second copy in the bundle
                        continue;
                    }
                    else
                    {
                        relativePathToSpec.Add(fileSpec.BundleRelativePath, fileSpec);
                    }

                    using (FileStream file = File.OpenRead(fileSpec.SourcePath))
                    {
                        FileType targetType = Target.TargetSpecificFileType(type);
                        (long startOffset, long compressedSize) = AddToBundle(bundle, file, targetType);
                        FileEntry entry = BundleManifest.AddEntry(targetType, file, relativePath, startOffset, compressedSize, Target.BundleMajorVersion);
                        Tracer.Log($"Embed: {entry}");
                    }
                }

                // Write the bundle manifest
                headerOffset = BundleManifest.Write(writer);
                Tracer.Log($"Header Offset={headerOffset}");
                Tracer.Log($"Meta-data Size={writer.BaseStream.Position - headerOffset}");
                Tracer.Log($"Bundle: Path={bundlePath}, Size={bundle.Length}");
            }

            HostWriter.SetAsBundle(bundlePath, headerOffset);

            return bundlePath;
        }
    }
}
