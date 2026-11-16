// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace SuperPmi;

public struct TOCElement
{
    // Native layout: int64 Offset, int Number, char Hash[33], padded to 8 bytes.
    public const int Size = 48;
    private const int HashOffset = 12;

    public long Offset;
    public int Number;
    public byte[] Hash; // MM3_HASH_BUFFER_SIZE bytes, including the null terminator

    public TOCElement(int number, long offset, string hash)
    {
        Number = number;
        Offset = offset;
        Hash = new byte[SuperPmi.Hash.MM3_HASH_BUFFER_SIZE];
        if (hash != null)
            Encoding.ASCII.GetBytes(hash, Hash);
    }

    public readonly string HashString
    {
        get
        {
            int nul = Array.IndexOf(Hash, (byte)0);
            return Encoding.ASCII.GetString(Hash, 0, nul >= 0 ? nul : Hash.Length);
        }
    }

    public static TOCElement Read(ReadOnlySpan<byte> data) => new TOCElement
    {
        Offset = BinaryPrimitives.ReadInt64LittleEndian(data),
        Number = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(8)),
        Hash = data.Slice(HashOffset, SuperPmi.Hash.MM3_HASH_BUFFER_SIZE).ToArray(),
    };

    public readonly void Write(Span<byte> data)
    {
        data.Slice(0, Size).Clear();
        BinaryPrimitives.WriteInt64LittleEndian(data, Offset);
        BinaryPrimitives.WriteInt32LittleEndian(data.Slice(8), Number);
        Hash.CopyTo(data.Slice(HashOffset));
    }
}

// The table of contents (.mct) for a method context file: "INDX", the element count, the elements, "INDX".
public sealed class TOCFile
{
    public static readonly uint Sentinel = BinaryPrimitives.ReadUInt32LittleEndian("INDX"u8);

    private TOCElement[] m_tocArray = [];

    public int GetTocCount() => m_tocArray.Length;

    public TOCElement GetElement(int i) => m_tocArray[i];

    public void Clear() => m_tocArray = [];

    public void LoadToc(string inputFileName, bool validate = true)
    {
        byte[] data;
        try
        {
            data = File.ReadAllBytes(inputFileName);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Failed to open file '{inputFileName}'. GetLastError()={ErrorHandling.LastError(e)}");
            return;
        }

        if (data.Length < 8 || BinaryPrimitives.ReadUInt32LittleEndian(data) != Sentinel)
        {
            Logger.LogWarning($"The index file {inputFileName} is invalid: it seems to be missing the starting sentinel/length");
            return;
        }

        int count = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4));
        long elementsEnd = 8 + (long)count * TOCElement.Size;
        if (count < 0 || elementsEnd > data.Length)
        {
            Logger.LogWarning($"The index file {inputFileName} is invalid: it appears to be truncated.");
            return;
        }

        if (elementsEnd + 4 > data.Length || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)elementsEnd)) != Sentinel)
        {
            Logger.LogWarning($"The index file {inputFileName} is invalid: it appears to be missing the ending sentinel.");
            return;
        }

        var elements = new TOCElement[count];
        for (int i = 0; i < count; i++)
            elements[i] = TOCElement.Read(data.AsSpan(8 + i * TOCElement.Size, TOCElement.Size));

        if (validate)
        {
            int lastNum = -1;
            foreach (TOCElement element in elements)
            {
                if (element.Number <= lastNum)
                {
                    Logger.LogWarning($"The index file {inputFileName} is invalid: it is not sorted.");
                    return;
                }
                lastNum = element.Number;
            }
        }

        m_tocArray = elements;
    }
}
