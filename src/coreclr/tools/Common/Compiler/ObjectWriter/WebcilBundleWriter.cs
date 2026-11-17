// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.NET.WebAssembly.Webcil;

namespace ILCompiler.ObjectWriter;

internal sealed class WebcilBundleWriter
{
    private const uint Magic = 0x42496257; // WbIB
    private const int HeaderSize = 20;
    private const int EntrySize = 16;
    private const int ImageAlignment = 16;
    private static readonly UTF8Encoding s_utf8 = new(false, true);

    private readonly List<(byte[] Name, byte[] Payload)> _entries = new();
    private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string name, byte[] payload)
    {
        name = name.Replace('\\', '/');
        if (string.IsNullOrEmpty(name) || name.StartsWith('/'))
        {
            throw new InvalidDataException($"Invalid Webcil bundle entry name '{name}'.");
        }

        foreach (char character in name)
        {
            if (character < ' ')
            {
                throw new InvalidDataException($"Invalid Webcil bundle entry name '{name}'.");
            }
        }

        foreach (string part in name.Split('/'))
        {
            if (part is "" or "." or "..")
            {
                throw new InvalidDataException($"Invalid Webcil bundle entry name '{name}'.");
            }
        }

        if (!_names.Add(Path.GetFileName(name)))
        {
            throw new InvalidDataException($"Duplicate Webcil bundle assembly name '{name}'.");
        }

        _entries.Add((s_utf8.GetBytes(name), payload));
    }

    public void Write(string outputPath)
    {
        int stringTableSize = 0;
        foreach ((byte[] name, byte[] _) in _entries)
        {
            stringTableSize = checked(stringTableSize + name.Length);
        }

        int stringTableOffset = checked(HeaderSize + _entries.Count * EntrySize);
        int nextImageOffset = Align(checked(stringTableOffset + stringTableSize));
        int nextNameOffset = 0;

        using MemoryStream payload = new();
        using (BinaryWriter writer = new(payload, s_utf8, leaveOpen: true))
        {
            writer.Write(Magic);
            writer.Write((ushort)1);
            writer.Write((ushort)0);
            writer.Write((uint)_entries.Count);
            writer.Write((uint)stringTableOffset);
            writer.Write((uint)stringTableSize);

            foreach ((byte[] name, byte[] image) in _entries)
            {
                writer.Write((uint)nextNameOffset);
                writer.Write((uint)name.Length);
                writer.Write((uint)nextImageOffset);
                writer.Write((uint)image.Length);
                nextNameOffset = checked(nextNameOffset + name.Length);
                nextImageOffset = Align(checked(nextImageOffset + image.Length));
            }

            foreach ((byte[] name, byte[] _) in _entries)
            {
                writer.Write(name);
            }

            foreach ((byte[] _, byte[] image) in _entries)
            {
                while (payload.Position % ImageAlignment != 0)
                {
                    writer.Write((byte)0);
                }

                writer.Write(image);
            }
        }

        payload.Position = 0;
        string fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        bool succeeded = false;
        try
        {
            using (FileStream output = new(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new WebcilWasmWrapper(payload, selfInstalling: true).WriteWasmWrappedWebcil(output);
            }
            succeeded = true;
        }
        finally
        {
            if (!succeeded)
            {
                File.Delete(fullPath);
            }
        }
    }

    private static int Align(int value) => checked((value + ImageAlignment - 1) & ~(ImageAlignment - 1));
}
