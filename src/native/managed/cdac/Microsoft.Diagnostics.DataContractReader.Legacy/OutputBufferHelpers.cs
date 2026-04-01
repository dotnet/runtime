// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

public static class OutputBufferHelpers
{
    public static unsafe void CopyStringToBuffer(char* stringBuf, uint bufferSize, uint* neededBufferSize, string str)
    {
        ReadOnlySpan<char> strSpan = str.AsSpan();
        if (neededBufferSize != null)
            *neededBufferSize = checked((uint)(strSpan.Length + 1));

        if (stringBuf != null && bufferSize > 0)
        {
            Span<char> target = new Span<char>(stringBuf, checked((int)bufferSize));
            int nullTerminatorLocation = strSpan.Length > bufferSize - 1 ? checked((int)(bufferSize - 1)) : strSpan.Length;
            strSpan = strSpan.Slice(0, nullTerminatorLocation);
            strSpan.CopyTo(target);
            target[nullTerminatorLocation] = '\0';
        }
    }

    public static unsafe void CopyUtf8StringToBuffer(byte* stringBuf, uint bufferSize, uint* neededBufferSize, string str)
    {
        int byteCount = Encoding.UTF8.GetByteCount(str);
        if (neededBufferSize is not null)
            *neededBufferSize = checked((uint)(byteCount + 1));

        if (stringBuf is not null && bufferSize > 0)
        {
            int maxBytes = Math.Min(byteCount, (int)bufferSize - 1);
            Span<byte> target = new Span<byte>(stringBuf, checked(maxBytes));
            Encoding.UTF8.GetEncoder().Convert(str.AsSpan(), target, true, out _, out int bytesWritten, out _);
            stringBuf[bytesWritten] = (byte)'\0';
        }
    }
}
