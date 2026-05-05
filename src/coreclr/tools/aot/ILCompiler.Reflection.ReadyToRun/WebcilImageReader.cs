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

        /// <summary>
        /// Returns true if this Webcil image is wrapped inside a WASM module.
        /// </summary>
        public bool IsWasmWrapped => _webcilOffset > 0;

        /// <summary>
        /// Represents a decoded WASM function body with its locals and type signature.
        /// </summary>
        public readonly struct WasmFunctionInfo
        {
            public byte[] Image { get; init; }
            public int InstructionOffset { get; init; }
            public int InstructionLength { get; init; }
            /// <summary>Local variable declarations: (count, valtype byte) pairs.</summary>
            public IReadOnlyList<(uint Count, byte ValType)> Locals { get; init; }
            /// <summary>Parameter types from the function's type signature.</summary>
            public IReadOnlyList<byte> ParamTypes { get; init; }
            /// <summary>Result types from the function's type signature.</summary>
            public IReadOnlyList<byte> ResultTypes { get; init; }
        }

        /// <summary>
        /// Gets the full function info for a WASM function by its index in the code section.
        /// Returns null if the image is not WASM-wrapped or the function index is out of range.
        /// </summary>
        public WasmFunctionInfo? GetWasmFunctionBody(int functionIndex)
        {
            if (!IsWasmWrapped)
                return null;

            // First pass: collect type section (section 1) and function section (section 3)
            List<(byte[] ParamTypes, byte[] ResultTypes)> types = null;
            List<uint> funcTypeIndices = null;
            int codeOffset = -1;

            int offset = 8; // Skip WASM magic + version
            while (offset < _image.Length)
            {
                byte sectionId = _image[offset++];
                uint sectionSize = ReadLebU32(_image, ref offset);
                int sectionEnd = offset + (int)sectionSize;

                switch (sectionId)
                {
                    case 1: // Type section
                        types = ParseTypeSection(_image, ref offset, sectionEnd);
                        break;
                    case 3: // Function section
                        funcTypeIndices = ParseFunctionSection(_image, ref offset, sectionEnd);
                        break;
                    case 10: // Code section
                        codeOffset = offset;
                        break;
                }

                offset = sectionEnd;
            }

            if (codeOffset < 0)
                return null;

            // Parse the code section to find the target function body
            offset = codeOffset;
            uint funcCount = ReadLebU32(_image, ref offset);
            for (uint i = 0; i < funcCount; i++)
            {
                uint bodySize = ReadLebU32(_image, ref offset);
                int bodyEnd = offset + (int)bodySize;

                if (i == (uint)functionIndex)
                {
                    // Read local declarations
                    var locals = new List<(uint Count, byte ValType)>();
                    uint localDeclCount = ReadLebU32(_image, ref offset);
                    for (uint j = 0; j < localDeclCount; j++)
                    {
                        uint count = ReadLebU32(_image, ref offset);
                        byte valType = _image[offset++];
                        locals.Add((count, valType));
                    }

                    int instrLength = bodyEnd - offset;

                    // Resolve type signature
                    byte[] paramTypes = Array.Empty<byte>();
                    byte[] resultTypes = Array.Empty<byte>();
                    if (funcTypeIndices is not null && (uint)functionIndex < funcTypeIndices.Count)
                    {
                        uint typeIdx = funcTypeIndices[functionIndex];
                        if (types is not null && typeIdx < types.Count)
                        {
                            paramTypes = types[(int)typeIdx].ParamTypes;
                            resultTypes = types[(int)typeIdx].ResultTypes;
                        }
                    }

                    return new WasmFunctionInfo
                    {
                        Image = _image,
                        InstructionOffset = offset,
                        InstructionLength = instrLength,
                        Locals = locals,
                        ParamTypes = paramTypes,
                        ResultTypes = resultTypes
                    };
                }

                offset = bodyEnd;
            }

            return null;
        }

        private static List<(byte[] ParamTypes, byte[] ResultTypes)> ParseTypeSection(byte[] data, ref int offset, int end)
        {
            var types = new List<(byte[], byte[])>();
            uint count = ReadLebU32(data, ref offset);
            for (uint i = 0; i < count && offset < end; i++)
            {
                byte form = data[offset++];
                // 0x60 = func type
                if (form != 0x60)
                {
                    // Skip unknown type forms
                    break;
                }
                uint paramCount = ReadLebU32(data, ref offset);
                byte[] paramTypes = new byte[paramCount];
                for (uint j = 0; j < paramCount; j++)
                    paramTypes[j] = data[offset++];
                uint resultCount = ReadLebU32(data, ref offset);
                byte[] resultTypes = new byte[resultCount];
                for (uint j = 0; j < resultCount; j++)
                    resultTypes[j] = data[offset++];
                types.Add((paramTypes, resultTypes));
            }
            return types;
        }

        private static List<uint> ParseFunctionSection(byte[] data, ref int offset, int end)
        {
            var indices = new List<uint>();
            uint count = ReadLebU32(data, ref offset);
            for (uint i = 0; i < count && offset < end; i++)
            {
                indices.Add(ReadLebU32(data, ref offset));
            }
            return indices;
        }

        private static uint ReadLebU32(byte[] data, ref int offset)
        {
            uint result = 0;
            int shift = 0;
            byte b;
            do
            {
                if (offset >= data.Length)
                    return result;
                b = data[offset++];
                result |= (uint)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return result;
        }

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
