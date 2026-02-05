// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.MemoryMappedFiles;

namespace Microsoft.NET.HostModel.MachO
{
    public class MemoryMappedMachOViewAccessor : IMachOFileAccess
    {
        private readonly MemoryMappedViewAccessor _accessor;

        public long Capacity => _accessor.Capacity;

        public MemoryMappedMachOViewAccessor(MemoryMappedViewAccessor accessor)
        {
            _accessor = accessor;
        }

        public void Read<T>(long offset, out T result) where T : unmanaged
        {
            _accessor.Read(offset, out result);
        }

        public int Read(long position, byte[] buffer, int offset, int count)
        {
            return _accessor.ReadArray(position, buffer, offset, count);
        }

        public void ReadExactly(long offset, byte[] buffer)
        {
            _accessor.ReadArray(offset, buffer, 0, buffer.Length);
        }

        public uint ReadUInt32BigEndian(long offset)
        {
            return _accessor.ReadUInt32BigEndian(offset);
        }

        public void Write<T>(long offset, ref T value) where T : unmanaged
        {
            _accessor.Write(offset, ref value);
        }

        public void WriteUInt32BigEndian(long offset, uint value)
        {
            _accessor.WriteUInt32BigEndian(offset, value);
        }

        public void Write(long offset, byte[] buffer)
        {
            _accessor.WriteArray(offset, buffer, 0, buffer.Length);
        }

        public void WriteExactly(long offset, byte[] buffer)
        {
            _accessor.WriteArray(offset, buffer, 0, buffer.Length);
        }

        public void WriteByte(long offset, byte data)
        {
            _accessor.Write(offset, data);
        }
    }
}
