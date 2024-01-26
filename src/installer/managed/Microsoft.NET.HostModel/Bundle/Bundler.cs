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
        public readonly Manifest BundleManifest;

        private readonly string _hostName;
        private readonly string _outputDir;
        private readonly string _depsJson;
        private readonly string _runtimeConfigJson;
        private readonly string _runtimeConfigDevJson;

        private readonly Trace _tracer;
        private readonly TargetInfo _target;
        private readonly BundleOptions _options;
        private readonly bool _macosCodesign;

        public Bundler(string hostName,
                       string outputDir,
                       BundleOptions options = BundleOptions.None,
                       OSPlatform? targetOS = null,
                       Architecture? targetArch = null,
                       Version targetFrameworkVersion = null,
                       bool diagnosticOutput = false,
                       string appAssemblyName = null,
                       bool macosCodesign = true)
        {
            _tracer = new Trace(diagnosticOutput);

            _hostName = hostName;
            _outputDir = Path.GetFullPath(string.IsNullOrEmpty(outputDir) ? Environment.CurrentDirectory : outputDir);
            _target = new TargetInfo(targetOS, targetArch, targetFrameworkVersion);

            if (_target.BundleMajorVersion < 6 &&
                (options & BundleOptions.EnableCompression) != 0)
            {
                throw new ArgumentException("Compression requires framework version 6.0 or above", nameof(options));
            }

            appAssemblyName ??= _target.GetAssemblyName(hostName);
            _depsJson = appAssemblyName + ".deps.json";
            _runtimeConfigJson = appAssemblyName + ".runtimeconfig.json";
            _runtimeConfigDevJson = appAssemblyName + ".runtimeconfig.dev.json";

            BundleManifest = new Manifest(_target.BundleMajorVersion, netcoreapp3CompatMode: options.HasFlag(BundleOptions.BundleAllContent));
            _options = _target.DefaultOptions | options;
            _macosCodesign = macosCodesign;
        }

        private bool ShouldCompress(FileType type)
        {
            if (!_options.HasFlag(BundleOptions.EnableCompression))
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
        private (long startOffset, long compressedSize) AddToBundle(Stream bundle, FileStream file, FileType type)
        {
            long startOffset = bundle.Position;
            if (ShouldCompress(type))
            {
                long fileLength = file.Length;
                file.Position = 0;

                // We use DeflateStream here.
                // It uses GZip algorithm, but with a trivial header that does not contain file info.
                CompressionLevel smallestSize = (CompressionLevel)3;
                using (DeflateStream compressionStream = new DeflateStream(bundle, Enum.IsDefined(typeof(CompressionLevel), smallestSize) ? smallestSize : CompressionLevel.Optimal, leaveOpen: true))
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
                long misalignment = (bundle.Position % _target.AssemblyAlignment);

                if (misalignment != 0)
                {
                    long padding = _target.AssemblyAlignment - misalignment;
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
            return fileRelativePath.Equals(_hostName);
        }

        private bool ShouldIgnore(string fileRelativePath)
        {
            return fileRelativePath.Equals(_runtimeConfigDevJson);
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
                    return !_options.HasFlag(BundleOptions.BundleNativeBinaries) || _target.ShouldExclude(relativePath);

                case FileType.Symbols:
                    return !_options.HasFlag(BundleOptions.BundleSymbolFiles);

                case FileType.Unknown:
                    return !_options.HasFlag(BundleOptions.BundleOtherFiles);

                default:
                    Debug.Assert(false);
                    return false;
            }
        }

        private static bool IsAssembly(string path, out bool isPE)
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
            if (fileSpec.BundleRelativePath.Equals(_depsJson))
            {
                return FileType.DepsJson;
            }

            if (fileSpec.BundleRelativePath.Equals(_runtimeConfigJson))
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

            bool isNativeBinary = _target.IsWindows ? isPE : _target.IsNativeBinary(fileSpec.SourcePath);

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
        /// This doesn't include unbundled files that should be dropped, and not published as output.
        /// </param>
        /// <returns>
        /// The full path the generated bundle file
        /// </returns>
        /// <exceptions>
        /// ArgumentException if input is invalid
        /// IOExceptions and ArgumentExceptions from callees flow to the caller.
        /// </exceptions>
        public string GenerateBundle(IReadOnlyList<FileSpec> fileSpecs)
        {
            _tracer.Log($"Bundler Version: {BundlerMajorVersion}.{BundlerMinorVersion}");
            _tracer.Log($"Bundle  Version: {BundleManifest.BundleVersion}");
            _tracer.Log($"Target Runtime: {_target}");
            _tracer.Log($"Bundler Options: {_options}");

            if (fileSpecs.Any(x => !x.IsValid()))
            {
                throw new ArgumentException("Invalid input specification: Found entry with empty source-path or bundle-relative-path.");
            }

            string hostSource;
            try
            {
                hostSource = fileSpecs.Where(x => x.BundleRelativePath.Equals(_hostName)).Single().SourcePath;
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException("Invalid input specification: Must specify the host binary");
            }

            string bundlePath = Path.Combine(_outputDir, _hostName);
            if (File.Exists(bundlePath))
            {
                _tracer.Log($"Ovewriting existing File {bundlePath}");
            }

            BinaryUtils.CopyFile(hostSource, bundlePath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && HostModelUtils.IsCodesignAvailable())
            {
                RemoveCodesignIfNecessary(bundlePath);
            }

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
                        _tracer.Log($"Ignore: {relativePath}");
                        continue;
                    }

                    FileType type = InferType(fileSpec);

                    if (ShouldExclude(type, relativePath))
                    {
                        _tracer.Log($"Exclude [{type}]: {relativePath}");
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
                        FileType targetType = _target.TargetSpecificFileType(type);
                        (long startOffset, long compressedSize) = AddToBundle(bundle, file, targetType);
                        FileEntry entry = BundleManifest.AddEntry(targetType, file, relativePath, startOffset, compressedSize, _target.BundleMajorVersion);
                        _tracer.Log($"Embed: {entry}");
                    }
                }

                // Write the bundle manifest
                headerOffset = BundleManifest.Write(writer);
                _tracer.Log($"Header Offset={headerOffset}");
                _tracer.Log($"Meta-data Size={writer.BaseStream.Position - headerOffset}");
                _tracer.Log($"Bundle: Path={bundlePath}, Size={bundle.Length}");
            }

            HostWriter.SetAsBundle(bundlePath, headerOffset);

            // Sign the bundle if requested
            if (_macosCodesign && RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && HostModelUtils.IsCodesignAvailable())
            {
                var (exitCode, stdErr) = HostModelUtils.RunCodesign("-s -", bundlePath);
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to codesign '{bundlePath}': {stdErr}");
                }
            }

            return bundlePath;

            // Remove mac code signature if applied before bundling
            static void RemoveCodesignIfNecessary(string bundlePath)
            {
                Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
                Debug.Assert(HostModelUtils.IsCodesignAvailable());

                // `codesign -v` returns 0 if app is signed
                if (HostModelUtils.RunCodesign("-v", bundlePath).ExitCode == 0)
                {
                    var (exitCode, stdErr) = HostModelUtils.RunCodesign("--remove-signature", bundlePath);
                    if (exitCode != 0)
                    {
                        throw new InvalidOperationException($"Removing codesign from '{bundlePath}' failed: {stdErr}");
                    }
                }
            }
        }
    }
}
