// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// An implementation of IMachOFileWriter that writes to a MemoryStream.
/// This class is useful for writing MachO data in memory without needing to write to a file on disk.
/// Particularly useful for writing blobs to a buffer for hashing.
/// </summary>
public class MemoryStreamWriter : IMachOFileWriter, IDisposable
{
    private readonly StreamBasedMachOFile inner;
    private readonly MemoryStream _stream;

    public long Capacity => inner.Capacity;

    public byte[] GetBuffer() => _stream.GetBuffer();

    public MemoryStreamWriter(int size)
    {
        _stream = new MemoryStream(size);
        inner = new StreamBasedMachOFile(_stream);
    }

    public void Write<T>(long offset, ref T value) where T : unmanaged => inner.Write(offset, ref value);
    public void WriteByte(long offset, byte data) => inner.WriteByte(offset, data);
    public void WriteExactly(long offset, byte[] buffer) => inner.WriteExactly(offset, buffer);
    public void WriteUInt32BigEndian(long offset, uint value) => inner.WriteUInt32BigEndian(offset, value);

    public void Dispose()
    {
        _stream.Dispose();
    }
}
