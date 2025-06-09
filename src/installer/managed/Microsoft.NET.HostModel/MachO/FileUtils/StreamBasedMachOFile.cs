// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.NET.HostModel.MachO
{
    /// <summary>
    /// Represents a Mach-O file that is backed by a stream.
    /// This class implements both reading and writing capabilities for Mach-O files.
    /// It does not take ownership of the stream, so the caller is responsible for disposing of it when necessary.
    /// </summary>
    public class StreamBasedMachOFile : IMachOFile
    {
        private readonly Stream _stream;

        public StreamBasedMachOFile(Stream stream)
        {
            _stream = stream;
        }

        public long Capacity => _stream.Length;

        public void Read<T>(long offset, out T result) where T : unmanaged
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Read(out result);
        }

        public int Read(long position, byte[] buffer, int offset, int count)
        {
            _stream.Seek(position, SeekOrigin.Begin);
            return _stream.Read(buffer, offset, count);
        }

        public void ReadExactly(long offset, byte[] buffer)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.ReadExactly(buffer);
        }

        public uint ReadUInt32BigEndian(long offset)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            return _stream.ReadUInt32BigEndian();
        }

        public void Write<T>(long offset, ref T value) where T : unmanaged
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Write(ref value);
        }

        public void Write(long offset, byte[] buffer)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void WriteByte(long offset, byte data)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.WriteByte(data);
        }

        public void WriteExactly(long offset, byte[] buffer)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void WriteUInt32BigEndian(long offset, uint value)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.WriteUInt32BigEndian(value);
        }
    }
}
