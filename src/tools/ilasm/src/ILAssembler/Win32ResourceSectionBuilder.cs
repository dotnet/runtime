// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Copyright (c) .NET Foundation and Contributors.
// Adapted from Roslyn's CvtRes.cs and PEWriter/NativeResourceWriter.cs:

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILAssembler;

// Format specifications: https://learn.microsoft.com/windows/win32/menurc/resource-file-formats
// and https://learn.microsoft.com/windows/win32/debug/pe-format#the-rsrc-section
internal sealed class Win32ResourceSectionBuilder(byte[] sectionBytes, int[] relocations) : ResourceSectionBuilder
{
    private const int DirectoryHeaderSize = 16;
    private const int DirectoryEntrySize = 8;
    private const int DataEntrySize = 16;
    private const uint SubdirectoryOrNameFlag = 0x80000000;
    private const ushort DialogIncludeResourceType = 17;

    internal static Win32ResourceSectionBuilder Create(byte[] bytes)
    {
        try
        {
            if (bytes.Length < sizeof(uint))
            {
                throw new InvalidDataException("The Win32 resource file is empty or truncated.");
            }

            return BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0 ? ReadRes(bytes) : ReadCoff(bytes);
        }
        catch (Exception ex) when (ex is EndOfStreamException or BadImageFormatException or OverflowException)
        {
            throw new InvalidDataException("The Win32 resource file is malformed.", ex);
        }
    }

    protected override void Serialize(BlobBuilder builder, SectionLocation location)
    {
        var writer = new BlobWriter(builder.ReserveBytes(sectionBytes.Length));
        writer.WriteBytes(sectionBytes);
        foreach (int offset in relocations)
        {
            writer.Offset = offset;
            writer.WriteUInt32(checked(BinaryPrimitives.ReadUInt32LittleEndian(sectionBytes.AsSpan(offset)) +
                (uint)location.RelativeVirtualAddress));
        }
    }

    private readonly record struct Identifier(ushort Id, string? Name = null) : IComparable<Identifier>
    {
        public int CompareTo(Identifier other) => Name is null
            ? other.Name is null ? Id.CompareTo(other.Id) : 1
            : other.Name is null ? -1 : StringComparer.OrdinalIgnoreCase.Compare(Name, other.Name);
    }

    private sealed class Node
    {
        internal readonly SortedDictionary<Identifier, Node> Children = new();
        internal byte[]? Data;
    }

    private static Win32ResourceSectionBuilder ReadRes(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BinaryReader(stream, Encoding.Unicode);
        var root = new Node();
        bool firstResource = true;
        while (stream.Position < stream.Length)
        {
            int start = checked((int)stream.Position);
            int dataSize = checked((int)reader.ReadUInt32());
            int headerSize = checked((int)reader.ReadUInt32());
            // Two empty UTF-16 identifiers are the shortest possible type and name.
            const int MinimumHeaderSize = 2 * sizeof(uint) + 2 * sizeof(ushort) + 16;
            if (headerSize < MinimumHeaderSize || (headerSize & 3) != 0)
            {
                throw new InvalidDataException($"Invalid Win32 resource header at offset {start}.");
            }
            CheckRange(start, headerSize, bytes.Length);
            int headerEnd = checked(start + headerSize);
            CheckRange(headerEnd, dataSize, bytes.Length);
            Identifier type = ReadIdentifier(reader, headerEnd - 16);
            Identifier name = ReadIdentifier(reader, headerEnd - 16);
            stream.Position = Align(checked((int)stream.Position));
            if (stream.Position > headerEnd - 16)
            {
                throw new InvalidDataException($"Invalid Win32 resource identifiers at offset {start}.");
            }
            reader.ReadUInt32(); // DataVersion
            reader.ReadUInt16(); // MemoryFlags
            ushort language = reader.ReadUInt16();
            reader.ReadUInt32(); // Version
            reader.ReadUInt32(); // Characteristics
            stream.Position = headerEnd;
            byte[] data = reader.ReadBytes(dataSize);
            stream.Position = Align(checked(headerEnd + dataSize));

            if (firstResource)
            {
                if (dataSize != 0 || type != new Identifier(0) || name != new Identifier(0))
                {
                    throw new InvalidDataException("The .res file does not begin with a null resource.");
                }
                firstResource = false;
                continue;
            }
            if (type == new Identifier(DialogIncludeResourceType))
            {
                continue;
            }

            Node languageDirectory = GetOrAdd(GetOrAdd(root, type), name);
            if (!languageDirectory.Children.TryAdd(new Identifier(language), new Node { Data = data }))
            {
                throw new InvalidDataException("The .res file contains duplicate resource type, name, and language identifiers.");
            }
        }

        var builder = new BlobBuilder();
        var relocations = new List<int>();
        WriteDirectory(root, builder, relocations);
        return new Win32ResourceSectionBuilder(builder.ToArray(), relocations.ToArray());
    }

    private static Node GetOrAdd(Node parent, Identifier key)
    {
        if (!parent.Children.TryGetValue(key, out Node? node))
        {
            node = new Node();
            parent.Children.Add(key, node);
        }

        return node;
    }

    private static Identifier ReadIdentifier(BinaryReader reader, int end)
    {
        ushort first = ReadWord();
        if (first == ushort.MaxValue)
        {
            return new Identifier(ReadWord());
        }

        var name = new StringBuilder();
        for (ushort value = first; value != 0; value = ReadWord())
        {
            name.Append((char)value);
        }

        return new Identifier(0, name.ToString());

        ushort ReadWord()
        {
            if (reader.BaseStream.Position > end - sizeof(ushort))
            {
                throw new InvalidDataException("A Win32 resource identifier extends beyond its header.");
            }

            return reader.ReadUInt16();
        }
    }

    private static void WriteDirectory(Node directory, BlobBuilder builder, List<int> relocations)
    {
        var writer = new BlobWriter(builder.ReserveBytes(checked(DirectoryHeaderSize + DirectoryEntrySize * directory.Children.Count)));
        writer.WriteUInt32(0); // Characteristics
        writer.WriteUInt32(0); // TimeDateStamp
        writer.WriteUInt32(0); // MajorVersion, MinorVersion
        int namedCount = directory.Children.Keys.Count(key => key.Name is not null);
        writer.WriteUInt16(checked((ushort)namedCount));
        writer.WriteUInt16(checked((ushort)(directory.Children.Count - namedCount)));
        foreach ((Identifier identifier, Node child) in directory.Children)
        {
            if (identifier.Name is string name)
            {
                writer.WriteUInt32((uint)builder.Count | SubdirectoryOrNameFlag);
                builder.WriteUInt16(checked((ushort)name.Length));
                builder.WriteUTF16(name);
                builder.Align(sizeof(uint));
            }
            else
            {
                writer.WriteUInt32(identifier.Id);
            }

            int offset = builder.Count;
            if (child.Data is byte[] data)
            {
                writer.WriteUInt32((uint)offset);
                relocations.Add(offset);
                builder.WriteUInt32(checked((uint)(offset + DataEntrySize)));
                builder.WriteUInt32((uint)data.Length);
                builder.WriteUInt32(0); // CodePage
                builder.WriteUInt32(0); // Reserved
                builder.WriteBytes(data);
                builder.Align(sizeof(uint));
            }
            else
            {
                writer.WriteUInt32((uint)offset | SubdirectoryOrNameFlag);
                WriteDirectory(child, builder, relocations);
            }
        }
    }

    private static Win32ResourceSectionBuilder ReadCoff(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var headers = new PEHeaders(stream);
        SectionHeader directory = default;
        SectionHeader data = default;
        int directorySection = 0;
        int dataSection = 0;
        for (int i = 0; i < headers.SectionHeaders.Length; i++)
        {
            SectionHeader section = headers.SectionHeaders[i];
            if (section.Name == ".rsrc$01" && directorySection == 0)
            {
                directory = section;
                directorySection = i + 1;
            }
            else if (section.Name == ".rsrc$02" && dataSection == 0)
            {
                data = section;
                dataSection = i + 1;
            }
            else if (section.Name is ".rsrc$01" or ".rsrc$02")
            {
                throw new InvalidDataException("The COFF resource file has duplicate resource sections.");
            }
        }
        if (headers.PEHeader is not null || directorySection == 0 || dataSection == 0 || directory.SizeOfRawData < DirectoryHeaderSize)
        {
            throw new InvalidDataException("The COFF resource file must contain .rsrc$01 and .rsrc$02 sections.");
        }

        CheckRange(directory.PointerToRawData, directory.SizeOfRawData, bytes.Length);
        CheckRange(data.PointerToRawData, data.SizeOfRawData, bytes.Length);
        const int RelocationSize = 10;
        const int SymbolSize = 18;
        CheckRange(directory.PointerToRelocations, checked(directory.NumberOfRelocations * RelocationSize), bytes.Length);
        CheckRange(headers.CoffHeader.PointerToSymbolTable, checked(headers.CoffHeader.NumberOfSymbols * SymbolSize), bytes.Length);
        if (data.NumberOfRelocations != 0)
        {
            throw new InvalidDataException("The COFF resource data section must not contain relocations.");
        }
        ushort expectedRelocation = headers.CoffHeader.Machine switch
        {
            Machine.I386 => 0x0007, // IMAGE_REL_I386_DIR32NB
            Machine.Amd64 => 0x0003, // IMAGE_REL_AMD64_ADDR32NB
            Machine.Arm64 => 0x0002, // IMAGE_REL_ARM64_ADDR32NB
            _ => throw new InvalidDataException($"Unsupported COFF resource machine '{headers.CoffHeader.Machine}'.")
        };

        byte[] sectionBytes = new byte[checked(directory.SizeOfRawData + data.SizeOfRawData)];
        bytes.AsSpan(directory.PointerToRawData, directory.SizeOfRawData).CopyTo(sectionBytes);
        bytes.AsSpan(data.PointerToRawData, data.SizeOfRawData).CopyTo(sectionBytes.AsSpan(directory.SizeOfRawData));
        int[] relocations = new int[directory.NumberOfRelocations];
        var seenOffsets = new HashSet<int>();
        for (int i = 0; i < relocations.Length; i++)
        {
            int relocation = directory.PointerToRelocations + i * RelocationSize;
            int offset = checked((int)ReadUInt32(bytes, relocation));
            uint symbolIndex = ReadUInt32(bytes, relocation + 4);
            ushort relocationType = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(relocation + 8));
            CheckRange(offset, sizeof(uint), directory.SizeOfRawData);
            if (symbolIndex >= headers.CoffHeader.NumberOfSymbols || relocationType != expectedRelocation || !seenOffsets.Add(offset))
            {
                throw new InvalidDataException("Invalid COFF resource relocation.");
            }

            int symbol = checked(headers.CoffHeader.PointerToSymbolTable + (int)symbolIndex * SymbolSize);
            uint value = ReadUInt32(bytes, symbol + 8);
            short sectionNumber = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(symbol + 12));
            ushort symbolType = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(symbol + 14));
            if (sectionNumber != dataSection || symbolType != 0)
            {
                throw new InvalidDataException("A COFF resource relocation must refer to a symbol in .rsrc$02.");
            }

            uint dataOffset = checked(value + ReadUInt32(sectionBytes, offset));
            if (dataOffset > data.SizeOfRawData)
            {
                throw new InvalidDataException("A COFF resource relocation points outside .rsrc$02.");
            }
            BinaryPrimitives.WriteUInt32LittleEndian(sectionBytes.AsSpan(offset), checked(dataOffset + (uint)directory.SizeOfRawData));
            relocations[i] = offset;
        }

        ValidateCoffDirectory(sectionBytes, directory.SizeOfRawData, seenOffsets);
        return new Win32ResourceSectionBuilder(sectionBytes, relocations);
    }

    private static void ValidateCoffDirectory(byte[] bytes, int directorySize, HashSet<int> relocations)
    {
        var directories = new HashSet<int>();
        var dataEntries = new HashSet<int>();
        ValidateDirectory(0, 0);
        if (!dataEntries.SetEquals(relocations))
        {
            throw new InvalidDataException("COFF resource relocations must identify every resource data address and no other fields.");
        }

        void ValidateDirectory(int offset, int level)
        {
            CheckRange(offset, DirectoryHeaderSize, directorySize);
            if (!directories.Add(offset))
            {
                throw new InvalidDataException("The COFF resource directory contains a cycle or a shared subdirectory.");
            }

            int namedCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 12));
            int idCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 14));
            int count = namedCount + idCount;
            int entriesStart = checked(offset + DirectoryHeaderSize);
            CheckRange(entriesStart, checked(count * DirectoryEntrySize), directorySize);
            for (int i = 0; i < count; i++)
            {
                int entry = entriesStart + i * DirectoryEntrySize;
                uint name = ReadUInt32(bytes, entry);
                bool isNamed = (name & SubdirectoryOrNameFlag) != 0;
                if (isNamed != (i < namedCount) || (level == 2 && isNamed))
                {
                    throw new InvalidDataException("Invalid COFF resource directory identifier.");
                }
                if (isNamed)
                {
                    int nameOffset = (int)(name & ~SubdirectoryOrNameFlag);
                    CheckRange(nameOffset, sizeof(ushort), directorySize);
                    int length = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(nameOffset));
                    CheckRange(nameOffset + sizeof(ushort), length * sizeof(char), directorySize);
                }

                uint child = ReadUInt32(bytes, entry + sizeof(uint));
                bool isDirectory = (child & SubdirectoryOrNameFlag) != 0;
                if (isDirectory != (level < 2))
                {
                    throw new InvalidDataException("The COFF resource directory must have type, name, and language levels.");
                }
                int childOffset = (int)(child & ~SubdirectoryOrNameFlag);
                if (isDirectory)
                {
                    ValidateDirectory(childOffset, level + 1);
                }
                else
                {
                    CheckRange(childOffset, DataEntrySize, directorySize);
                    int dataOffset = checked((int)ReadUInt32(bytes, childOffset));
                    int dataSize = checked((int)ReadUInt32(bytes, childOffset + sizeof(uint)));
                    CheckRange(dataOffset - directorySize, dataSize, bytes.Length - directorySize);
                    dataEntries.Add(childOffset);
                }
            }
        }
    }

    private static uint ReadUInt32(byte[] bytes, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));

    private static int Align(int value) => checked(value + 3) & ~3;

    private static void CheckRange(int offset, int size, int length)
    {
        if (offset < 0 || size < 0 || offset > length - size)
        {
            throw new InvalidDataException("The Win32 resource file contains an invalid offset or size.");
        }
    }
}
