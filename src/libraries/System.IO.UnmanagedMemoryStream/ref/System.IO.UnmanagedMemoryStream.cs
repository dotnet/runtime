// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO
{
    public partial class UnmanagedMemoryAccessor : System.IDisposable
    {
        protected UnmanagedMemoryAccessor() { }
        public UnmanagedMemoryAccessor(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity) { }
        public UnmanagedMemoryAccessor(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity, System.IO.FileAccess access) { }
        public bool CanRead { get { throw null; } }
        public bool CanWrite { get { throw null; } }
        public long Capacity { get { throw null; } }
        protected bool IsOpen { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected void Initialize(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity, System.IO.FileAccess access) { }
        public int ReadArray<T>(long position, T[] array, int offset, int count) where T : struct { throw null; }
        public bool ReadBoolean(long position) { throw null; }
        public byte ReadByte(long position) { throw null; }
        public char ReadChar(long position) { throw null; }
        public decimal ReadDecimal(long position) { throw null; }
        public double ReadDouble(long position) { throw null; }
        public short ReadInt16(long position) { throw null; }
        public int ReadInt32(long position) { throw null; }
        public long ReadInt64(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public sbyte ReadSByte(long position) { throw null; }
        public float ReadSingle(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort ReadUInt16(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint ReadUInt32(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong ReadUInt64(long position) { throw null; }
        public void Read<T>(long position, out T structure) where T : struct { throw null; }
        public void Write(long position, bool value) { }
        public void Write(long position, byte value) { }
        public void Write(long position, char value) { }
        public void Write(long position, decimal value) { }
        public void Write(long position, double value) { }
        public void Write(long position, short value) { }
        public void Write(long position, int value) { }
        public void Write(long position, long value) { }
        [System.CLSCompliantAttribute(false)]
        public void Write(long position, sbyte value) { }
        public void Write(long position, float value) { }
        [System.CLSCompliantAttribute(false)]
        public void Write(long position, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public void Write(long position, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void Write(long position, ulong value) { }
        public void WriteArray<T>(long position, T[] array, int offset, int count) where T : struct { }
        public void Write<T>(long position, ref T structure) where T : struct { }
    }
    public partial class UnmanagedMemoryStream : System.IO.Stream
    {
        protected UnmanagedMemoryStream() { }
        [System.CLSCompliantAttribute(false)]
        public unsafe UnmanagedMemoryStream(byte* pointer, long length) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, System.IO.FileAccess access) { }
        public UnmanagedMemoryStream(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long length) { }
        public UnmanagedMemoryStream(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long length, System.IO.FileAccess access) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public long Capacity { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        public unsafe byte* PositionPointer { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.CLSCompliantAttribute(false)]
        protected unsafe void Initialize(byte* pointer, long length, long capacity, System.IO.FileAccess access) { }
        protected void Initialize(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long length, System.IO.FileAccess access) { }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override int Read(System.Span<byte> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin loc) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
}
