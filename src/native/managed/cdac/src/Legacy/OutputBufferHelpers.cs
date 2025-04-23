// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class OutputBufferHelpers
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
}
