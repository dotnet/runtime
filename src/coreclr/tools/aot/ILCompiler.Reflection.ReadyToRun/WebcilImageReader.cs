// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

using Microsoft.NET.WebAssembly.Webcil;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// Wrapper around Webcil files that implements IBinaryImageReader.
    /// Webcil is a stripped-down PE format used for managed assemblies in WebAssembly environments.
    /// </summary>
    public class WebcilImageReader : IBinaryImageReader
    {
        private readonly byte[] _image;
        private readonly WebcilHeader _header;
        private readonly ImmutableArray<WebcilSectionHeader> _sections;
        private readonly long _webcilOffset;
        private readonly DirectoryEntry _corHeaderMetadataDirectory;
        private readonly CorFlags _corFlags;
        private readonly DirectoryEntry _managedNativeHeaderDirectory;

        public Machine Machine => Machine.I386; // Webcil doesn't encode machine type; wasm targets use a placeholder
        public OperatingSystem OperatingSystem => OperatingSystem.Unknown;
        public ulong ImageBase => 0;

        public WebcilImageReader(byte[] image)
        {
            _image = image;
            _webcilOffset = 0;

            // Check for WASM wrapper
            if (IsWasmModule(image))
            {
                if (!TryFindWebcilInWasm(image, out _webcilOffset))
                {
                    throw new BadImageFormatException("WASM module does not contain a Webcil payload");
                }
            }

            // Read the Webcil header
            if (!TryReadHeader(image, _webcilOffset, out _header))
            {
                throw new BadImageFormatException("Not a valid Webcil file");
            }

            // Read section headers
            _sections = ReadSections(image, _webcilOffset, _header);

            // Read the COR header to get metadata and R2R header locations
            ReadCorHeader(image, out _corFlags, out _corHeaderMetadataDirectory, out _managedNativeHeaderDirectory);
        }

        /// <summary>
        /// Detects whether a byte array starts with the Webcil magic bytes (or is a WASM module containing Webcil).
        /// </summary>
        public static bool IsWebcilImage(byte[] image)
        {
            if (image.Length < 4)
                return false;

            uint magic = BitConverter.ToUInt32(image, 0);
            if (magic == WebcilConstants.WEBCIL_MAGIC)
                return true;

            // Check if it's a WASM module that might contain Webcil
            if (IsWasmModule(image))
            {
                return TryFindWebcilInWasm(image, out _);
            }

            return false;
        }

        /// <summary>
        /// Detects whether the file at the specified path is a Webcil image.
        /// </summary>
        public static bool IsWebcilImage(string filename)
        {
            try
            {
                byte[] header = new byte[4];
                using var stream = File.OpenRead(filename);
                if (stream.Read(header, 0, 4) != 4)
                    return false;

                uint magic = BitConverter.ToUInt32(header, 0);
                if (magic == WebcilConstants.WEBCIL_MAGIC)
                    return true;

                // Check for WASM magic (\0asm)
                if (header[0] == 0x00 && header[1] == 0x61 && header[2] == 0x73 && header[3] == 0x6D)
                {
                    // Read the full file to check for Webcil inside WASM
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] fullImage = GC.AllocateArray<byte>((int)stream.Length, pinned: true);
                    stream.Read(fullImage, 0, fullImage.Length);
                    return TryFindWebcilInWasm(fullImage, out _);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public ImmutableArray<byte> GetEntireImage()
            => Unsafe.As<byte[], ImmutableArray<byte>>(ref Unsafe.AsRef(in _image));

        internal byte[] GetImage() => _image;

        public int GetOffset(int rva)
        {
            foreach (var section in _sections)
            {
                if ((uint)rva >= section.VirtualAddress && (uint)rva < section.VirtualAddress + section.VirtualSize)
                {
                    uint offset = (uint)rva - section.VirtualAddress;
                    if (offset >= section.SizeOfRawData)
                    {
                        throw new BadImageFormatException($"RVA 0x{rva:X} maps beyond section raw data");
                    }
                    return (int)(section.PointerToRawData + offset + _webcilOffset);
                }
            }
            throw new BadImageFormatException($"RVA 0x{rva:X} not found in any Webcil section");
        }

        public bool TryGetReadyToRunHeader(out int rva, out bool isComposite)
        {
            // Check the ManagedNativeHeaderDirectory (same as PE's CorHeader.ManagedNativeHeaderDirectory)
            if ((_corFlags & CorFlags.ILLibrary) != 0 && _managedNativeHeaderDirectory.Size != 0)
            {
                rva = _managedNativeHeaderDirectory.RelativeVirtualAddress;
                isComposite = false;
                return true;
            }

            rva = 0;
            isComposite = false;
            return false;
        }

        public IAssemblyMetadata GetStandaloneAssemblyMetadata()
        {
            if (_corHeaderMetadataDirectory.Size == 0)
                return null;

            int metadataOffset = GetOffset(_corHeaderMetadataDirectory.RelativeVirtualAddress);
            var metadataBytes = GC.AllocateArray<byte>(_corHeaderMetadataDirectory.Size, pinned: true);
            Array.Copy(_image, metadataOffset, metadataBytes, 0, _corHeaderMetadataDirectory.Size);

            return new WebcilAssemblyMetadata(metadataBytes, this);
        }

        public IAssemblyMetadata GetManifestAssemblyMetadata(MetadataReader manifestReader)
            => new ManifestAssemblyMetadata(manifestReader);

        public void DumpImageInformation(TextWriter writer)
        {
            writer.WriteLine($"Format: Webcil v{_header.VersionMajor}.{_header.VersionMinor}");
            writer.WriteLine($"Sections: {_header.CoffSections}");
            writer.WriteLine($"CliHeaderRVA: 0x{_header.PeCliHeaderRva:X}");
            writer.WriteLine($"CliHeaderSize: {_header.PeCliHeaderSize}");
            writer.WriteLine($"DebugRVA: 0x{_header.PeDebugRva:X}");
            writer.WriteLine($"DebugSize: {_header.PeDebugSize}");

            writer.WriteLine("Sections:");
            for (int i = 0; i < _sections.Length; i++)
            {
                var section = _sections[i];
                writer.WriteLine($"  [{i}] VA=0x{section.VirtualAddress:X} VSize=0x{section.VirtualSize:X} RawSize=0x{section.SizeOfRawData:X} RawPtr=0x{section.PointerToRawData:X}");
            }
        }

        public Dictionary<string, int> GetSections()
        {
            Dictionary<string, int> sectionMap = [];
            for (int i = 0; i < _sections.Length; i++)
            {
                sectionMap.Add($".webcil{i}", (int)_sections[i].SizeOfRawData);
            }
            return sectionMap;
        }

        private void ReadCorHeader(byte[] image, out CorFlags flags, out DirectoryEntry metadataDirectory, out DirectoryEntry managedNativeHeaderDirectory)
        {
            int corHeaderOffset = GetOffset((int)_header.PeCliHeaderRva);

            // CorHeader layout:
            // int32 cb (byte count)
            // uint16 MajorRuntimeVersion
            // uint16 MinorRuntimeVersion
            // DirectoryEntry MetaData (RVA + Size)
            // uint32 Flags
            // int32 EntryPointTokenOrRelativeVirtualAddress
            // DirectoryEntry Resources (RVA + Size)
            // DirectoryEntry StrongNameSignature (RVA + Size)
            // DirectoryEntry CodeManagerTable (RVA + Size)
            // DirectoryEntry VTableFixups (RVA + Size)
            // DirectoryEntry ExportAddressTableJumps (RVA + Size)
            // DirectoryEntry ManagedNativeHeader (RVA + Size)

            int offset = corHeaderOffset;
            offset += 4; // cb
            offset += 2; // MajorRuntimeVersion
            offset += 2; // MinorRuntimeVersion

            int metadataRva = BitConverter.ToInt32(image, offset); offset += 4;
            int metadataSize = BitConverter.ToInt32(image, offset); offset += 4;
            metadataDirectory = new DirectoryEntry(metadataRva, metadataSize);

            flags = (CorFlags)BitConverter.ToUInt32(image, offset); offset += 4;

            offset += 4; // EntryPointTokenOrRelativeVirtualAddress
            offset += 8; // Resources
            offset += 8; // StrongNameSignature
            offset += 8; // CodeManagerTable
            offset += 8; // VTableFixups
            offset += 8; // ExportAddressTableJumps

            int managedNativeRva = BitConverter.ToInt32(image, offset); offset += 4;
            int managedNativeSize = BitConverter.ToInt32(image, offset);
            managedNativeHeaderDirectory = new DirectoryEntry(managedNativeRva, managedNativeSize);
        }

        private static bool TryReadHeader(byte[] image, long offset, out WebcilHeader header)
        {
            header = default;

            // V0 header is 28 bytes, V1 is 32 bytes
            const int V0HeaderSize = 28;
            const int V1HeaderSize = 32;

            if (offset + V0HeaderSize > image.Length)
                return false;

            unsafe
            {
                fixed (byte* p = &image[(int)offset])
                {
                    WebcilHeader temp;
                    Buffer.MemoryCopy(p, &temp, sizeof(WebcilHeader), V0HeaderSize);
                    header = temp;
                }
            }

            if (!BitConverter.IsLittleEndian)
            {
                header.Id = BinaryPrimitives.ReverseEndianness(header.Id);
                header.VersionMajor = BinaryPrimitives.ReverseEndianness(header.VersionMajor);
                header.VersionMinor = BinaryPrimitives.ReverseEndianness(header.VersionMinor);
                header.CoffSections = BinaryPrimitives.ReverseEndianness(header.CoffSections);
                header.PeCliHeaderRva = BinaryPrimitives.ReverseEndianness(header.PeCliHeaderRva);
                header.PeCliHeaderSize = BinaryPrimitives.ReverseEndianness(header.PeCliHeaderSize);
                header.PeDebugRva = BinaryPrimitives.ReverseEndianness(header.PeDebugRva);
                header.PeDebugSize = BinaryPrimitives.ReverseEndianness(header.PeDebugSize);
            }

            if (header.Id != WebcilConstants.WEBCIL_MAGIC)
                return false;

            if (header.VersionMajor != 0 && header.VersionMajor != 1)
                return false;

            if (header.VersionMinor != WebcilConstants.WC_VERSION_MINOR)
                return false;

            if (header.VersionMajor >= 1)
            {
                if (offset + V1HeaderSize > image.Length)
                    return false;

                header.TableBase = BitConverter.ToUInt32(image, (int)offset + V0HeaderSize);
                if (!BitConverter.IsLittleEndian)
                {
                    header.TableBase = BinaryPrimitives.ReverseEndianness(header.TableBase);
                }
            }
            else
            {
                header.TableBase = uint.MaxValue;
            }

            return true;
        }

        private static unsafe ImmutableArray<WebcilSectionHeader> ReadSections(byte[] image, long webcilOffset, WebcilHeader header)
        {
            int sectionSize = sizeof(WebcilSectionHeader);
            long sectionDirectoryOffset = webcilOffset + (header.VersionMajor >= 1 ? 32 : 28);
            var sections = ImmutableArray.CreateBuilder<WebcilSectionHeader>(header.CoffSections);

            for (int i = 0; i < header.CoffSections; i++)
            {
                long sectionOffset = sectionDirectoryOffset + (i * sectionSize);
                WebcilSectionHeader sectionHeader;
                fixed (byte* p = &image[(int)sectionOffset])
                {
                    sectionHeader = *(WebcilSectionHeader*)p;
                }

                if (!BitConverter.IsLittleEndian)
                {
                    sectionHeader = new WebcilSectionHeader(
                        virtualSize: BinaryPrimitives.ReverseEndianness(sectionHeader.VirtualSize),
                        virtualAddress: BinaryPrimitives.ReverseEndianness(sectionHeader.VirtualAddress),
                        sizeOfRawData: BinaryPrimitives.ReverseEndianness(sectionHeader.SizeOfRawData),
                        pointerToRawData: BinaryPrimitives.ReverseEndianness(sectionHeader.PointerToRawData)
                    );
                }

                sections.Add(sectionHeader);
            }

            return sections.MoveToImmutable();
        }

        private static bool IsWasmModule(byte[] image)
        {
            // WASM magic: \0asm
            return image.Length >= 4
                && image[0] == 0x00
                && image[1] == 0x61
                && image[2] == 0x73
                && image[3] == 0x6D;
        }

        private static bool TryFindWebcilInWasm(byte[] image, out long webcilOffset)
        {
            webcilOffset = 0;

            // Simple scan: look for the Webcil magic in the WASM module
            // The Webcil payload is embedded as a custom section in the WASM module
            for (int i = 8; i <= image.Length - 4; i++)
            {
                uint candidate = BitConverter.ToUInt32(image, i);
                if (candidate == WebcilConstants.WEBCIL_MAGIC)
                {
                    // Verify this is a valid Webcil header
                    if (TryReadHeader(image, i, out _))
                    {
                        webcilOffset = i;
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Assembly metadata implementation for Webcil images that don't have a PEReader.
    /// </summary>
    internal sealed unsafe class WebcilAssemblyMetadata : IAssemblyMetadata
    {
        private readonly MetadataReader _metadataReader;
        private readonly WebcilImageReader _webcilReader;

        public WebcilAssemblyMetadata(byte[] metadataBytes, WebcilImageReader webcilReader)
        {
            _webcilReader = webcilReader;
            fixed (byte* p = metadataBytes)
            {
                _metadataReader = new MetadataReader(p, metadataBytes.Length);
            }
        }

        public BlobReader GetSectionData(int relativeVirtualAddress)
        {
            if (_webcilReader is null)
                return default;

            int offset = _webcilReader.GetOffset(relativeVirtualAddress);
            byte[] image = _webcilReader.GetImage();
            int remaining = image.Length - offset;
            fixed (byte* p = image)
            {
                return new BlobReader(p + offset, remaining);
            }
        }

        public MetadataReader MetadataReader => _metadataReader;
    }
}
