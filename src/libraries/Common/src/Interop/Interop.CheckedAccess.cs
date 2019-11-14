// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static partial class Interop
{
    public static unsafe void CheckBounds(byte* buffer, int bufferSize, byte* offset, int accessSize)
    {
        var start = checked((int)(IntPtr)(offset - buffer));
        var end = checked(start + accessSize);
        if (start < 0 || end > bufferSize)
        {
            throw new IndexOutOfRangeException();
        }
    }

    public static unsafe void CheckBounds(byte* buffer, int bufferSize, byte* offset)
    {
        CheckBounds(buffer, bufferSize, offset, sizeof(byte));
    }

    public static unsafe void CheckBounds(byte* buffer, int bufferSize, ushort* offset)
    {
        CheckBounds(buffer, bufferSize, (byte*)offset, sizeof(ushort));
    }

    public static unsafe void CheckBounds(byte* buffer, int bufferSize, uint* offset)
    {
        CheckBounds(buffer, bufferSize, (byte*)offset, sizeof(uint));
    }

    public static unsafe byte CheckedRead(byte* buffer, int bufferSize, byte* offset)
    {
        CheckBounds(buffer, bufferSize, offset);
        return *offset;
    }

    public static unsafe ushort CheckedRead(byte* buffer, int bufferSize, ushort* offset)
    {
        CheckBounds(buffer, bufferSize, offset);
        return *offset;
    }

    public static unsafe uint CheckedRead(byte* buffer, int bufferSize, uint* offset)
    {
        CheckBounds(buffer, bufferSize, offset);
        return *offset;
    }

    public static unsafe void CheckedWrite(byte* buffer, int bufferSize, byte* offset, byte value)
    {
        CheckBounds(buffer, bufferSize, offset);
        *offset = value;
    }

    public static unsafe void CheckedWrite(byte* buffer, int bufferSize, ushort* offset, ushort value)
    {
        CheckBounds(buffer, bufferSize, offset);
        *offset = value;
    }

    public static unsafe void CheckedWrite(byte* buffer, int bufferSize, uint* offset, uint value)
    {
        CheckBounds(buffer, bufferSize, offset);
        *offset = value;
    }
}
