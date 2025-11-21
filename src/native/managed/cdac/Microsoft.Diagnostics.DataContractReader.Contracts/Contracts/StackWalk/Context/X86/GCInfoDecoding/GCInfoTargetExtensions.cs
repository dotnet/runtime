// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.X86;

public static class GCInfoTargetExtensions
{
    /// <summary>
    /// Based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> decodeUnsigned
    /// </summary>
    public static uint GCDecodeUnsigned(this Target target, ref TargetPointer src)
    {
        TargetPointer begin = src;
        byte b = target.Read<byte>(src++);
        uint value = b & 0x7Fu;
        while ((b & 0x80) != 0)
        {
            if ((src - begin) > 5)
            {
                throw new InvalidOperationException("Invalid variable-length integer encoding.");
            }

            b = target.Read<byte>(src++);
            value <<= 7;
            value += b & 0x7Fu;
        }

        return value;
    }

    /// <summary>
    /// Based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> decodeSigned
    /// </summary>
    public static int GCDecodeSigned(this Target target, ref TargetPointer src)
    {
        TargetPointer begin = src;
        byte b = target.Read<byte>(src++);
        byte firstByte = b;
        int value = b & 0x3F;
        while ((b & 0x80) != 0)
        {
            if ((src - begin) > 5)
            {
                throw new InvalidOperationException("Invalid variable-length integer encoding.");
            }

            b = target.Read<byte>(src++);
            value <<= 7;
            value += b & 0x7F;
        }

        if ((firstByte & 0x40u) != 0)
        {
            value = -value;
        }

        return value;
    }

    /// <summary>
    /// Based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcdecoder.cpp">src\inc\gcdecoder.cpp</a> decodeUDelta
    /// </summary>
    public static uint GCDecodeUDelta(this Target target, ref TargetPointer src, uint lastValue)
    {
        uint delta = target.GCDecodeUnsigned(ref src);
        return lastValue + delta;
    }
}
