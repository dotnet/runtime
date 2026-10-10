// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;

namespace Microsoft.Diagnostics.DataContractReader.Legacy.EnumMemory;

internal static class Sanitizer
{
    internal static void SanitizePdbPath(Target target, MemoryRegionEmitter emitter, TargetSpan codeView)
    {
        const uint RsdsSignature = 0x53445352;
        const uint PdbPathOffset = 24;
        if (codeView.Size <= PdbPathOffset)
            return;

        Span<byte> buffer = stackalloc byte[256];
        target.ReadBuffer(codeView.Address, buffer[..sizeof(uint)]);
        if (BinaryPrimitives.ReadUInt32LittleEndian(buffer) != RsdsSignature)
            return;

        ulong pathAddress = codeView.Address.Value + PdbPathOffset;
        ulong pathSize = codeView.Size - PdbPathOffset;
        ulong fileNameStart = 0;
        ulong length = 0;
        bool terminated = false;
        while (length < pathSize && !terminated)
        {
            int count = (int)Math.Min((ulong)buffer.Length, pathSize - length);
            target.ReadBuffer(pathAddress + length, buffer[..count]);
            for (int i = 0; i < count; i++, length++)
            {
                if (buffer[i] == 0)
                {
                    terminated = true;
                    break;
                }
                if (buffer[i] is (byte)'\\' or (byte)'/')
                    fileNameStart = length + 1;
            }
        }
        if (!terminated)
            throw new InvalidOperationException("The CodeView PDB path is not null-terminated.");

        ulong fileNameLength = length - fileNameStart;
        ulong written = 0;
        while (written < fileNameLength)
        {
            int count = (int)Math.Min((ulong)buffer.Length, fileNameLength - written);
            target.ReadBuffer(pathAddress + fileNameStart + written, buffer[..count]);
            if (!emitter.Update(pathAddress + written, buffer[..count]))
                return;
            written += (uint)count;
        }
        buffer.Clear();
        while (written < pathSize)
        {
            int count = (int)Math.Min((ulong)buffer.Length, pathSize - written);
            if (!emitter.Update(pathAddress + written, buffer[..count]))
                return;
            written += (uint)count;
        }
    }

    internal static void StripFileInfoFromStackTrace(Span<char> buffer)
    {
        // Mirror StripFileInfoFromStackTrace in vm/excep.cpp, including trailing-text removal.
        int depth = 0;
        int written = 0;
        int lastMethodEnd = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            char c = buffer[i];
            buffer[written++] = c;
            if (c == '(')
                depth++;
            else if (c == ')')
            {
                if (depth == 1)
                {
                    lastMethodEnd = written;
                    while (i + 1 < buffer.Length && buffer[i + 1] is not '\r' and not '\n')
                        i++;
                }
                depth--;
            }
        }

        buffer[lastMethodEnd..].Clear();
    }
}
