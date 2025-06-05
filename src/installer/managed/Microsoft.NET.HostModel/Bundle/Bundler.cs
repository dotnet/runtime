// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.MachO;
#nullable enable

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
                       Version? targetFrameworkVersion = null,
                       bool diagnosticOutput = false,
                       string? appAssemblyName = null,
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
        private (long startOffset, long compressedSize) AddToBundle(MemoryMappedViewStream bundle, FileStream file, FileType type)
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
                    CorHeader? corHeader = peReader.PEHeaders.CorHeader;

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
        public static ImmutableArray<byte> BundleHeaderPlaceholder = [
            // 8 bytes represent the bundle header-offset
            // Zero for non-bundle apphosts (default).
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
            0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
            0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
            0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
            0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
        ];

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

            var relativePathToSpec = GetFilteredFileSpecs(fileSpecs);
            long bundledFilesSize = 0;
            // Conservatively estimate the size of bundled files.
            // Assume no compression and worst case alignment for assemblies.
            // There's no way to know the exact compressed sizes without reading the entire file,
            // which would be expensive.
            // We will memory map a larger file than needed, but we'll take that trade-off.
            foreach (var (spec, type) in relativePathToSpec)
            {
                bundledFilesSize += new FileInfo(spec.SourcePath).Length;
                if (type == FileType.Assembly)
                {
                    // Alignment could be as much as AssemblyAlignment - 1 bytes.
                    // Since the files may be compressed when written to the bundle we can't be sure of exactly how much space the padding will require.
                    // So we'll consvervatively add an additional AssemblyAlignment bytes.
                    bundledFilesSize += _target.AssemblyAlignment;
                }
            }

            string bundlePath = Path.Combine(_outputDir, _hostName);
            if (File.Exists(bundlePath))
            {
                _tracer.Log($"Ovewriting existing File {bundlePath}");
            }

            string destinationDirectory = new FileInfo(bundlePath).Directory!.FullName;
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            var bundleName = Path.GetFileName(bundlePath);
            var hostLength = new FileInfo(hostSource).Length;
            var bundleManifestLength = BundleManifest.GetManifestLength(BundleManifest.BundleMajorVersion, relativePathToSpec.Select(x => x.Spec.BundleRelativePath));
            long bundleTotalSize = hostLength + bundledFilesSize + bundleManifestLength;
            if (_target.IsOSX && _macosCodesign)
                bundleTotalSize += MachObjectFile.GetSignatureSizeEstimate((uint)bundleTotalSize, bundleName);

            using (MemoryMappedFile bundleMap = MemoryMappedFile.CreateNew(null, bundleTotalSize, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None))
            {
                long endOfHost;
                using (MemoryMappedViewStream bundleStream = bundleMap.CreateViewStream(0, 0, MemoryMappedFileAccess.ReadWrite))
                {
                    using (FileStream hostSourceStream = File.OpenRead(hostSource))
                    {
                        hostSourceStream.CopyTo(bundleStream);
                    }
                    endOfHost = bundleStream.Position;
                }
                Debug.Assert(endOfHost == hostLength, $"Host file size on disk does not match bytes written to the bundle. Expected {hostLength}, but got {endOfHost}. This may indicate that the host file is not a valid native binary or that it is not a single-file apphost.");
                MachObjectFile? machFile = null;
                EmbeddedSignatureBlob? signatureBlob = null;
                using (MemoryMappedViewAccessor viewAccessor = bundleMap.CreateViewAccessor())
                {
                    if (_target.IsOSX)
                    {
                        machFile = MachObjectFile.Create(viewAccessor);
                        signatureBlob = machFile.EmbeddedSignatureBlob;
                        if (machFile.RemoveCodeSignatureIfPresent(viewAccessor, out long? newEnd))
                        {
                            endOfHost = newEnd!.Value;
                        }
                    }
                }
                ulong endOfBundle;
                long headerOffset;
                using (MemoryMappedViewStream bundleStream = bundleMap.CreateViewStream(0, 0, MemoryMappedFileAccess.ReadWrite))
                {
                    bundleStream.Position = endOfHost;
                    foreach (var kvp in relativePathToSpec)
                    {
                        FileSpec fileSpec = kvp.Spec;
                        FileType type = kvp.Type;
                        string relativePath = fileSpec.BundleRelativePath;
                        using (FileStream file = File.OpenRead(fileSpec.SourcePath))
                        {
                            FileType targetType = _target.TargetSpecificFileType(type);
                            (long startOffset, long compressedSize) = AddToBundle(bundleStream, file, targetType);
                            FileEntry entry = BundleManifest.AddEntry(targetType, file, relativePath, startOffset, compressedSize, _target.BundleMajorVersion);
                            _tracer.Log($"Embed: {entry}");
                        }
                    }
                    Debug.Assert(bundleStream.Position - endOfHost <= bundledFilesSize, $"Not enough space allocated for bundled files. Allocated {bundledFilesSize}, but written {bundleStream.Position - endOfHost}");
                    var endOfBundledFiles = bundleStream.Position;
                    using (BinaryWriter writer = new BinaryWriter(bundleStream, Encoding.UTF8, leaveOpen: true))
                    {
                        // Write the bundle manifest
                        headerOffset = BundleManifest.Write(writer);
                        _tracer.Log($"Header Offset={headerOffset}");
                        _tracer.Log($"Meta-data Size={writer.BaseStream.Position - headerOffset}");
                        _tracer.Log($"Bundle: Path={bundlePath}, Size={bundleStream.Length}");
                    }
                    endOfBundle = (ulong)bundleStream.Position;
                    Debug.Assert((long)endOfBundle == endOfBundledFiles + bundleManifestLength, $"Bundle manifest is unexpected size. Expected {bundleManifestLength}, but got {(long)endOfBundle - endOfBundledFiles}");
                }
                using (MemoryMappedViewAccessor accessor = bundleMap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite))
                {
                    BinaryUtils.SearchAndReplace(accessor,
                                                BundleHeaderPlaceholder.AsSpan(),
                                                BitConverter.GetBytes(headerOffset),
                                                pad0s: false);
                    if (_target.IsOSX && machFile is not null)
                    {
                        if (!machFile.TryAdjustHeadersForBundle(endOfBundle, accessor))
                        {
                            throw new InvalidOperationException("The single-file bundle was unable to be created. This is likely because the bundled content is too large.");
                        }
                        if (_macosCodesign)
                        {
                            endOfBundle = (ulong)machFile.AdHocSignFile(accessor, bundleName, signatureBlob);
                        }
                    }

                    // MacOS keeps a cache of file signatures, so we must create a new inode to ensure the file signature is properly updated.
                    if (_macosCodesign && File.Exists(bundlePath))
                    {
                        _tracer.Log($"Removing existing bundle file to clear signature cache: {bundlePath}");
                        File.Delete(bundlePath);
                    }
                    using (FileStream bundleOutputStream = File.Open(bundlePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        BinaryUtils.WriteToStream(accessor, bundleOutputStream, (long)endOfBundle);
                    }
                }
            }
            HostWriter.Chmod755(bundlePath);
            return bundlePath;
        }

        private (FileSpec Spec, FileType Type)[] GetFilteredFileSpecs(IEnumerable<FileSpec> fileSpecs)
        {
            // Note: We're comparing file paths both on the OS we're running on as well as on the target OS for the app
            // We can't really make assumptions about the file systems (even on Linux there can be case insensitive file systems
            // and vice versa for Windows). So it's safer to do case sensitive comparison everywhere.
            var relativePathToSpec = new Dictionary<string, (FileSpec Spec, FileType Type)>(StringComparer.Ordinal);
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
                    if (!string.Equals(fileSpec.SourcePath, existingFileSpec.Spec.SourcePath, StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Invalid input specification: Found entries '{fileSpec.SourcePath}' and '{existingFileSpec.Spec.SourcePath}' with the same BundleRelativePath '{fileSpec.BundleRelativePath}'");
                    }

                    // Exact duplicate - intentionally skip and don't include a second copy in the bundle
                    continue;
                }
                else
                {
                    relativePathToSpec.Add(fileSpec.BundleRelativePath, (fileSpec, type));
                }
            }
            return relativePathToSpec.Values.ToArray();
        }

        /// <summary>
        /// Get the length of the string when written to a BinaryWriter.
        /// </summary>
        internal static uint GetBinaryWriterStringLength(string str)
        {
            // 1 byte for the length prefix + length of the string in bytes
            uint stringLength = (uint)Encoding.UTF8.GetByteCount(str); // BundleID with prefixed length
            // Prefixed length of bundle ID is 7-bit encoded
            // Strings 0-127 chars: 1 byte prefix
            // Strings 128-16,383 chars: 2 byte prefix
            // Strings 16,384-2,097,151 chars: 3 byte prefix
            // Strings 2,097,152-268,435,455 chars: 4 byte prefix
            // Strings 268,435,456+ chars: 5 byte prefix
            uint lengthPrefixLength = (stringLength < 128) ? 1u :
                           (stringLength < 16384) ? 2u :
                           (stringLength < 2097152) ? 3u :
                           (stringLength < 268435456) ? 4u : 5u;
            return lengthPrefixLength + stringLength;
        }
    }
}
