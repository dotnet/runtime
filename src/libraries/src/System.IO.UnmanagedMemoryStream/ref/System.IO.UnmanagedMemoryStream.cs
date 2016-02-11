// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.IO
{
    public partial class UnmanagedMemoryAccessor : System.IDisposable
    {
        protected UnmanagedMemoryAccessor() { }
        public UnmanagedMemoryAccessor(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity) { }
        public UnmanagedMemoryAccessor(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity, System.IO.FileAccess access) { }
        public bool CanRead { get { return default(bool); } }
        public bool CanWrite { get { return default(bool); } }
        public long Capacity { get { return default(long); } }
        protected bool IsOpen { get { return default(bool); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected void Initialize(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity, System.IO.FileAccess access) { }
        public bool ReadBoolean(long position) { return default(bool); }
        public byte ReadByte(long position) { return default(byte); }
        public char ReadChar(long position) { return default(char); }
        public decimal ReadDecimal(long position) { return default(decimal); }
        public double ReadDouble(long position) { return default(double); }
        public short ReadInt16(long position) { return default(short); }
        public int ReadInt32(long position) { return default(int); }
        public long ReadInt64(long position) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public sbyte ReadSByte(long position) { return default(sbyte); }
        public float ReadSingle(long position) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public ushort ReadUInt16(long position) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public uint ReadUInt32(long position) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public ulong ReadUInt64(long position) { return default(ulong); }
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
    }
    public partial class UnmanagedMemoryStream : System.IO.Stream
    {
        protected UnmanagedMemoryStream() { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe UnmanagedMemoryStream(byte* pointer, long length) { }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, System.IO.FileAccess access) { }
        public UnmanagedMemoryStream(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long length) { }
        public UnmanagedMemoryStream(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long length, System.IO.FileAccess access) { }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public long Capacity { get { return default(long); } }
        public override long Length { get { return default(long); } }
        public override long Position { get { return default(long); } set { } }
        [System.CLSCompliantAttribute(false)]
        public unsafe byte* PositionPointer {[System.Security.SecurityCriticalAttribute]get { return default(byte*); }[System.Security.SecurityCriticalAttribute]set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        protected unsafe void Initialize(byte* pointer, long length, long capacity, System.IO.FileAccess access) { }
        protected void Initialize(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long length, System.IO.FileAccess access) { }
        public override int Read(byte[] buffer, int offset, int count) { buffer = default(byte[]); return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override int ReadByte() { return default(int); }
        public override long Seek(long offset, System.IO.SeekOrigin loc) { return default(long); }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override void WriteByte(byte value) { }
    }
}
