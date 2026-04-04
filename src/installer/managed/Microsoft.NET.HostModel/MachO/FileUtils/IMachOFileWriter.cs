// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO
{
    /// <summary>
    /// An abstraction for writing Mach-O files.
    /// </summary>
    public interface IMachOFileWriter
    {
        void Write<T>(long offset, ref T value) where T : unmanaged;
        void WriteUInt32BigEndian(long offset, uint value);
        void WriteExactly(long offset, byte[] buffer);
        void WriteByte(long offset, byte data);
        long Capacity { get; }
    }
}
