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
    public class StreamBasedMachOFile : IMachOFileAccess
    {
        private readonly Stream _stream;

        public StreamBasedMachOFile(Stream stream)
        {
            _stream = stream;
        }

        public long Capacity => _stream.Length;

        public void Read<T>(long offset, out T result) where T : unmanaged
        {
            var tmpPosition = _stream.Position;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Read(out result);
            _stream.Seek(tmpPosition, SeekOrigin.Begin);
        }

        public int Read(long position, byte[] buffer, int offset, int count)
        {
            var tmpPosition = _stream.Position;
            _stream.Seek(position, SeekOrigin.Begin);
            int bytesRead = _stream.Read(buffer, offset, count);
            _stream.Seek(tmpPosition, SeekOrigin.Begin);
            return bytesRead;
        }

        public void ReadExactly(long offset, byte[] buffer)
        {
            var tmpPosition = _stream.Position;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.ReadExactly(buffer);
            _stream.Seek(tmpPosition, SeekOrigin.Begin);
        }

        public uint ReadUInt32BigEndian(long offset)
        {
            var tmpPosition = _stream.Position;
            _stream.Seek(offset, SeekOrigin.Begin);
            uint result = _stream.ReadUInt32BigEndian();
            _stream.Seek(tmpPosition, SeekOrigin.Begin);
            return result;
        }

        public void Write<T>(long offset, ref T value) where T : unmanaged
        {
            var tmpPosition = _stream.Position;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Write(ref value);
            _stream.Seek(tmpPosition, SeekOrigin.Begin);
        }

        public void Write(long offset, byte[] buffer)
        {
            var tmpPosition = _stream.Position;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Write(buffer, 0, buffer.Length);
            _stream.Seek(tmpPosition, SeekOrigin.Begin);
        }

        public void WriteByte(long offset, byte data)
        {
            var tmpPosition = _stream.Position;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.WriteByte(data);
            _stream.Seek(tmpPosition, SeekOrigin.Begin);
        }

        public void WriteExactly(long offset, byte[] buffer)
        {
            var tmpPosition = _stream.Position;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.Write(buffer, 0, buffer.Length);
            _stream.Seek(tmpPosition, SeekOrigin.Begin);
        }

        public void WriteUInt32BigEndian(long offset, uint value)
        {
            var tmpPosition = _stream.Position;
            _stream.Seek(offset, SeekOrigin.Begin);
            _stream.WriteUInt32BigEndian(value);
            _stream.Seek(tmpPosition, SeekOrigin.Begin);
        }
    }
}
