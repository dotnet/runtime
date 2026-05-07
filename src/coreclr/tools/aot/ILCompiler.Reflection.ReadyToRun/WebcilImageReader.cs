// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using Microsoft.NET.WebAssembly.Webcil;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// Wrapper around Webcil files that implements IBinaryImageReader.
    /// Webcil is a stripped-down PE format used for managed assemblies in WebAssembly environments.
    /// </summary>
    public class WebcilImageReader : IBinaryImageReader
    {
        private readonly ImmutableArray<byte> _image;
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
            _image = ImmutableCollectionsMarshal.AsImmutableArray(image);
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
                    stream.ReadExactly(fullImage, 0, fullImage.Length);
                    return TryFindWebcilInWasm(fullImage, out _);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public ImmutableArray<byte> GetEntireImage() => _image;

        internal ImmutableArray<byte> GetImage() => _image;

        /// <summary>
        /// Returns true if this Webcil image is wrapped inside a WASM module.
        /// </summary>
        public bool IsWasmWrapped => _webcilOffset > 0;

        /// <summary>
        /// Represents a decoded WASM function body with its locals and type signature.
        /// </summary>
        public readonly struct WasmFunctionInfo
        {
            public ImmutableArray<byte> Image { get; init; }
            public int InstructionOffset { get; init; }
            public int InstructionLength { get; init; }
            /// <summary>Local variable declarations: (count, valtype byte) pairs.</summary>
            public IReadOnlyList<(uint Count, byte ValType)> Locals { get; init; }
            /// <summary>Parameter types from the function's type signature.</summary>
            public IReadOnlyList<byte> ParamTypes { get; init; }
            /// <summary>Result types from the function's type signature.</summary>
            public IReadOnlyList<byte> ResultTypes { get; init; }
        }

        private WasmFunctionInfo[] _wasmFunctionCache;

        /// <summary>
        /// Gets the full function info for a WASM function by its index in the code section.
        /// Returns null if the image is not WASM-wrapped or the function index is out of range.
        /// </summary>
        public WasmFunctionInfo? GetWasmFunctionBody(int functionIndex)
        {
            if (!IsWasmWrapped)
                return null;

            _wasmFunctionCache ??= BuildWasmFunctionCache();

            if ((uint)functionIndex >= (uint)_wasmFunctionCache.Length)
                return null;

            return _wasmFunctionCache[functionIndex];
        }

        private WasmFunctionInfo[] BuildWasmFunctionCache()
        {
            List<(byte[] ParamTypes, byte[] ResultTypes)> types = null;
            List<uint> funcTypeIndices = null;
            int codeOffset = -1;

            ReadOnlySpan<byte> imageSpan = _image.AsSpan();
            int offset = 8; // Skip WASM magic + version
            while (offset < imageSpan.Length)
            {
                byte sectionId = imageSpan[offset++];
                uint sectionSize = ReadLebU32(imageSpan, ref offset);
                int sectionEnd = offset + (int)sectionSize;

                if (sectionEnd > imageSpan.Length)
                    throw new BadImageFormatException($"WASM section {sectionId} size extends beyond image boundary");

                switch (sectionId)
                {
                    case 1: // Type section
                        types = ParseTypeSection(imageSpan, ref offset, sectionEnd);
                        break;
                    case 3: // Function section
                        funcTypeIndices = ParseFunctionSection(imageSpan, ref offset, sectionEnd);
                        break;
                    case 10: // Code section
                        codeOffset = offset;
                        break;
                }

                offset = sectionEnd;
            }

            if (codeOffset < 0)
                return [];

            offset = codeOffset;
            uint funcCount = ReadLebU32(imageSpan, ref offset);
            var cache = new WasmFunctionInfo[funcCount];

            for (uint i = 0; i < funcCount; i++)
            {
                uint bodySize = ReadLebU32(imageSpan, ref offset);
                int bodyEnd = offset + (int)bodySize;

                if (bodyEnd > imageSpan.Length)
                    throw new BadImageFormatException($"WASM function body size extends beyond image boundary");

                // Read local declarations
                var locals = new List<(uint Count, byte ValType)>();
                uint localDeclCount = ReadLebU32(imageSpan, ref offset);
                for (uint j = 0; j < localDeclCount; j++)
                {
                    uint count = ReadLebU32(imageSpan, ref offset);
                    byte valType = imageSpan[offset++];
                    locals.Add((count, valType));
                }

                int instrLength = bodyEnd - offset;

                // Resolve type signature
                byte[] paramTypes = Array.Empty<byte>();
                byte[] resultTypes = Array.Empty<byte>();
                if (funcTypeIndices is not null && i < (uint)funcTypeIndices.Count)
                {
                    uint typeIdx = funcTypeIndices[(int)i];
                    if (types is not null && typeIdx < (uint)types.Count)
                    {
                        paramTypes = types[(int)typeIdx].ParamTypes;
                        resultTypes = types[(int)typeIdx].ResultTypes;
                    }
                }

                cache[i] = new WasmFunctionInfo
                {
                    Image = _image,
                    InstructionOffset = offset,
                    InstructionLength = instrLength,
                    Locals = locals,
                    ParamTypes = paramTypes,
                    ResultTypes = resultTypes
                };

                offset = bodyEnd;
            }

            return cache;
        }

        private static List<(byte[] ParamTypes, byte[] ResultTypes)> ParseTypeSection(ReadOnlySpan<byte> data, ref int offset, int end)
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

        private static List<uint> ParseFunctionSection(ReadOnlySpan<byte> data, ref int offset, int end)
        {
            var indices = new List<uint>();
            uint count = ReadLebU32(data, ref offset);
            for (uint i = 0; i < count && offset < end; i++)
            {
                indices.Add(ReadLebU32(data, ref offset));
            }
            return indices;
        }

        private static uint ReadLebU32(ReadOnlySpan<byte> data, ref int offset)
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

        public int GetSectionRemainingSize(int rva)
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
                    return (int)(section.SizeOfRawData - offset);
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
            _image.CopyTo(metadataOffset, metadataBytes, 0, _corHeaderMetadataDirectory.Size);

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

        private void ReadCorHeader(ReadOnlySpan<byte> image, out CorFlags flags, out DirectoryEntry metadataDirectory, out DirectoryEntry managedNativeHeaderDirectory)
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

            int metadataRva = BinaryPrimitives.ReadInt32LittleEndian(image.Slice(offset)); offset += 4;
            int metadataSize = BinaryPrimitives.ReadInt32LittleEndian(image.Slice(offset)); offset += 4;
            metadataDirectory = new DirectoryEntry(metadataRva, metadataSize);

            flags = (CorFlags)BinaryPrimitives.ReadUInt32LittleEndian(image.Slice(offset)); offset += 4;

            offset += 4; // EntryPointTokenOrRelativeVirtualAddress
            offset += 8; // Resources
            offset += 8; // StrongNameSignature
            offset += 8; // CodeManagerTable
            offset += 8; // VTableFixups
            offset += 8; // ExportAddressTableJumps

            int managedNativeRva = BinaryPrimitives.ReadInt32LittleEndian(image.Slice(offset)); offset += 4;
            int managedNativeSize = BinaryPrimitives.ReadInt32LittleEndian(image.Slice(offset));
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

            ReadOnlySpan<byte> span = image.AsSpan((int)offset);
            header.Id = BinaryPrimitives.ReadUInt32LittleEndian(span);
            header.VersionMajor = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(4));
            header.VersionMinor = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(6));
            header.CoffSections = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(8));
            // span[10..12] is Reserved0
            header.PeCliHeaderRva = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(12));
            header.PeCliHeaderSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(16));
            header.PeDebugRva = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(20));
            header.PeDebugSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(24));

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

                header.TableBase = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(V0HeaderSize));
            }
            else
            {
                header.TableBase = uint.MaxValue;
            }

            return true;
        }

        private static ImmutableArray<WebcilSectionHeader> ReadSections(byte[] image, long webcilOffset, WebcilHeader header)
        {
            const int SectionSize = 16; // 4 uint32 fields
            long sectionDirectoryOffset = webcilOffset + (header.VersionMajor >= 1 ? 32 : 28);
            var sections = ImmutableArray.CreateBuilder<WebcilSectionHeader>(header.CoffSections);

            for (int i = 0; i < header.CoffSections; i++)
            {
                ReadOnlySpan<byte> span = image.AsSpan((int)(sectionDirectoryOffset + (i * SectionSize)));
                var sectionHeader = new WebcilSectionHeader(
                    virtualSize: BinaryPrimitives.ReadUInt32LittleEndian(span),
                    virtualAddress: BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4)),
                    sizeOfRawData: BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8)),
                    pointerToRawData: BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(12))
                );

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

            // Parse WASM module structure to find the data section (id=11)
            // which contains the Webcil payload as a passive data segment.
            int offset = 8; // Skip WASM magic + version
            while (offset < image.Length)
            {
                if (offset >= image.Length)
                    return false;

                byte sectionId = image[offset++];
                uint sectionSize = ReadLebU32(image, ref offset);
                int sectionEnd = offset + (int)sectionSize;

                if (sectionEnd > image.Length)
                    return false;

                if (sectionId == 11) // Data section
                {
                    // Data section contains: count(LEB128) then count segments.
                    // Each passive segment: kind=1(byte) + size(LEB128) + bytes
                    // The Webcil payload is in the second passive data segment.
                    uint segmentCount = ReadLebU32(image, ref offset);
                    for (uint i = 0; i < segmentCount && offset < sectionEnd; i++)
                    {
                        byte kind = image[offset++];
                        if (kind == 1) // Passive segment
                        {
                            uint dataSize = ReadLebU32(image, ref offset);
                            // Check if this segment starts with the Webcil magic
                            if (dataSize >= 4 && offset + dataSize <= image.Length)
                            {
                                uint magic = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan(offset));
                                if (magic == WebcilConstants.WEBCIL_MAGIC && TryReadHeader(image, offset, out _))
                                {
                                    webcilOffset = offset;
                                    return true;
                                }
                            }
                            offset += (int)dataSize;
                        }
                        else if (kind == 0) // Active segment (memory 0)
                        {
                            // Skip the init expression + data
                            SkipConstExpr(image, ref offset);
                            uint dataSize = ReadLebU32(image, ref offset);
                            offset += (int)dataSize;
                        }
                        else if (kind == 2) // Active segment (explicit memory index)
                        {
                            ReadLebU32(image, ref offset); // memory index
                            SkipConstExpr(image, ref offset);
                            uint dataSize = ReadLebU32(image, ref offset);
                            offset += (int)dataSize;
                        }
                        else
                        {
                            return false; // Unknown segment kind
                        }
                    }
                    return false;
                }

                offset = sectionEnd;
            }

            return false;
        }

        private static void SkipConstExpr(ReadOnlySpan<byte> data, ref int offset)
        {
            // Skip a WASM constant expression (terminated by 0x0B = end)
            while (offset < data.Length)
            {
                byte opcode = data[offset++];
                if (opcode == 0x0B) // end
                    return;

                switch (opcode)
                {
                    case 0x41: // i32.const
                    case 0x23: // global.get
                        ReadLebU32(data, ref offset); // skip LEB128 operand
                        break;
                    case 0x42: // i64.const
                        ReadLebU32(data, ref offset); // skip LEB128 operand (simplified)
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Assembly metadata implementation for Webcil images that don't have a PEReader.
    /// </summary>
    internal sealed unsafe class WebcilAssemblyMetadata : IAssemblyMetadata
    {
        private readonly MetadataReader _metadataReader;
        private readonly MetadataReaderProvider _metadataReaderProvider;
        private readonly WebcilImageReader _webcilReader;

        public WebcilAssemblyMetadata(byte[] metadataBytes, WebcilImageReader webcilReader)
        {
            _webcilReader = webcilReader;
            var immutableBytes = ImmutableCollectionsMarshal.AsImmutableArray(metadataBytes);
            _metadataReaderProvider = MetadataReaderProvider.FromMetadataImage(immutableBytes);
            _metadataReader = _metadataReaderProvider.GetMetadataReader();
        }

        public void GetSectionData(int relativeVirtualAddress, Action<BlobReader> action)
        {
            if (_webcilReader is null)
            {
                action(default);
                return;
            }

            int offset = _webcilReader.GetOffset(relativeVirtualAddress);
            int remaining = _webcilReader.GetSectionRemainingSize(relativeVirtualAddress);
            ImmutableArray<byte> image = _webcilReader.GetImage();
            fixed (byte* p = ImmutableCollectionsMarshal.AsArray(image))
            {
                action(new BlobReader(p + offset, remaining));
            }
        }

        public MetadataReader MetadataReader => _metadataReader;
    }
}
