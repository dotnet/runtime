// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.NET.WebAssembly.Webcil;

public static class WasmMemory
{
    private const uint MaximumWasm32MemoryPages = 65536;
    // Limits encoding: https://webassembly.github.io/spec/core/binary/types.html#limits
    public static byte[] MakeShared(byte[] image)
    {
        var reader = new Reader(image);
        if (image.Length < 8 || image[0] != 0 || image[1] != 0x61 || image[2] != 0x73 || image[3] != 0x6d
            || image[4] != 1 || image[5] != 0 || image[6] != 0 || image[7] != 0)
            throw new InvalidDataException("Invalid WebAssembly header.");
        reader.ReadSection(8);

        var memoryFlags = new List<int>();
        while (!reader.AtEnd)
        {
            byte section = reader.ReadByte();
            uint length = reader.ReadUInt32();
            Reader payload = reader.ReadSection(length);
            if (section == 2)
            {
                uint count = payload.ReadUInt32();
                for (uint i = 0; i < count; i++)
                {
                    payload.SkipName();
                    payload.SkipName();
                    switch (payload.ReadByte())
                    {
                        case 0:
                            payload.ReadUInt32();
                            break;
                        case 1:
                            payload.ReadReferenceType();
                            payload.ReadLimits(memory: false);
                            break;
                        case 2:
                            memoryFlags.Add(payload.Position);
                            payload.ReadLimits(memory: true);
                            break;
                        case 3:
                            payload.ReadValueType();
                            if (payload.ReadByte() > 1)
                                throw new InvalidDataException("Invalid WebAssembly global mutability.");
                            break;
                        case 4:
                            if (payload.ReadByte() != 0)
                                throw new InvalidDataException("Invalid WebAssembly tag attribute.");
                            payload.ReadUInt32();
                            break;
                        default:
                            throw new InvalidDataException("Unsupported WebAssembly import kind.");
                    }
                }
                payload.RequireEnd();
            }
            else if (section == 5)
            {
                uint count = payload.ReadUInt32();
                for (uint i = 0; i < count; i++)
                {
                    memoryFlags.Add(payload.Position);
                    payload.ReadLimits(memory: true);
                }
                payload.RequireEnd();
            }
        }

        if (memoryFlags.Count != 1)
            throw new InvalidDataException("Shared-memory deployment requires exactly one WebAssembly memory.");

        byte[] result = (byte[])image.Clone();
        result[memoryFlags[0]] |= 2;
        return result;
    }

    private sealed class Reader
    {
        private readonly byte[] _image;
        private readonly int _end;
        public int Position { get; private set; }
        public bool AtEnd => Position == _end;

        public Reader(byte[] image) : this(image, 0, image.Length) { }

        private Reader(byte[] image, int start, int end)
        {
            _image = image;
            Position = start;
            _end = end;
        }

        public byte ReadByte()
        {
            if (Position == _end)
                throw new InvalidDataException("Truncated WebAssembly section.");

            return _image[Position++];
        }

        public uint ReadUInt32()
        {
            uint result = 0;
            for (int shift = 0; shift < 35; shift += 7)
            {
                byte value = ReadByte();
                if (shift == 28 && value > 15)
                    throw new InvalidDataException("Invalid WebAssembly u32 encoding.");
                result |= (uint)(value & 127) << shift;
                if ((value & 128) == 0)
                    return result;
            }

            throw new InvalidDataException("Invalid WebAssembly u32 encoding.");
        }

        public Reader ReadSection(uint length)
        {
            if (length > _end - Position)
                throw new InvalidDataException("Truncated WebAssembly section.");
            int start = Position;
            Position += (int)length;

            return new Reader(_image, start, Position);
        }

        public void SkipName() => ReadSection(ReadUInt32());

        public void RequireEnd()
        {
            if (!AtEnd)
                throw new InvalidDataException("Unexpected bytes after WebAssembly section payload.");
        }

        public void ReadReferenceType()
        {
            if (ReadByte() is not (0x70 or 0x6f))
                throw new InvalidDataException("Unsupported WebAssembly reference type.");
        }

        public void ReadValueType()
        {
            if (ReadByte() is not (0x7f or 0x7e or 0x7d or 0x7c or 0x7b or 0x70 or 0x6f))
                throw new InvalidDataException("Unsupported WebAssembly value type.");
        }

        public void ReadLimits(bool memory)
        {
            // Require the single-byte flags used by the writer, so the patch is size-preserving.
            byte flags = ReadByte();
            if (flags > (memory ? 3 : 1) || (memory && (flags & 1) == 0))
                throw new InvalidDataException("Shared-memory deployment requires bounded wasm32 memory. Rebuild the R2R image with a maximum.");
            uint minimum = ReadUInt32();
            uint maximum = (flags & 1) != 0 ? ReadUInt32() : uint.MaxValue;
            if (minimum > maximum || (memory && maximum > MaximumWasm32MemoryPages))
                throw new InvalidDataException("Invalid WebAssembly memory limits.");
        }
    }
}
