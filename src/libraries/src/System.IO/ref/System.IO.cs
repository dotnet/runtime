// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.IO
{
    public partial class BinaryReader : System.IDisposable
    {
        public BinaryReader(System.IO.Stream input) { }
        public BinaryReader(System.IO.Stream input, System.Text.Encoding encoding) { }
        public BinaryReader(System.IO.Stream input, System.Text.Encoding encoding, bool leaveOpen) { }
        public virtual System.IO.Stream BaseStream { get { return default(System.IO.Stream); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected virtual void FillBuffer(int numBytes) { }
        public virtual int PeekChar() { return default(int); }
        public virtual int Read() { return default(int); }
        public virtual int Read(byte[] buffer, int index, int count) { return default(int); }
        public virtual int Read(char[] buffer, int index, int count) { return default(int); }
        protected internal int Read7BitEncodedInt() { return default(int); }
        public virtual bool ReadBoolean() { return default(bool); }
        public virtual byte ReadByte() { return default(byte); }
        public virtual byte[] ReadBytes(int count) { return default(byte[]); }
        public virtual char ReadChar() { return default(char); }
        public virtual char[] ReadChars(int count) { return default(char[]); }
        public virtual decimal ReadDecimal() { return default(decimal); }
        public virtual double ReadDouble() { return default(double); }
        public virtual short ReadInt16() { return default(short); }
        public virtual int ReadInt32() { return default(int); }
        public virtual long ReadInt64() { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public virtual sbyte ReadSByte() { return default(sbyte); }
        public virtual float ReadSingle() { return default(float); }
        public virtual string ReadString() { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public virtual ushort ReadUInt16() { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public virtual uint ReadUInt32() { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public virtual ulong ReadUInt64() { return default(ulong); }
    }
    public partial class BinaryWriter : System.IDisposable
    {
        public static readonly System.IO.BinaryWriter Null;
        protected System.IO.Stream OutStream;
        protected BinaryWriter() { }
        public BinaryWriter(System.IO.Stream output) { }
        public BinaryWriter(System.IO.Stream output, System.Text.Encoding encoding) { }
        public BinaryWriter(System.IO.Stream output, System.Text.Encoding encoding, bool leaveOpen) { }
        public virtual System.IO.Stream BaseStream { get { return default(System.IO.Stream); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual void Flush() { }
        public virtual long Seek(int offset, System.IO.SeekOrigin origin) { return default(long); }
        public virtual void Write(bool value) { }
        public virtual void Write(byte value) { }
        public virtual void Write(byte[] buffer) { }
        public virtual void Write(byte[] buffer, int index, int count) { }
        public virtual void Write(char ch) { }
        public virtual void Write(char[] chars) { }
        public virtual void Write(char[] chars, int index, int count) { }
        public virtual void Write(decimal value) { }
        public virtual void Write(double value) { }
        public virtual void Write(short value) { }
        public virtual void Write(int value) { }
        public virtual void Write(long value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(sbyte value) { }
        public virtual void Write(float value) { }
        public virtual void Write(string value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(ulong value) { }
        protected void Write7BitEncodedInt(int value) { }
    }
    public sealed partial class BufferedStream : System.IO.Stream 
    {
        public BufferedStream(Stream stream) { }
        public BufferedStream(Stream stream, int bufferSize) { }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public override long Length { get { return default(long); } }
        public override long Position { get { return default(long); } set { } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override int Read(byte[] array, int offset, int count) { return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override int ReadByte() { return default(int); }
        public override long Seek(long offset, SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public override void Write(byte[] array, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override void WriteByte(byte value) { }
    }
    public partial class EndOfStreamException : System.IO.IOException
    {
        public EndOfStreamException() { }
        public EndOfStreamException(string message) { }
        public EndOfStreamException(string message, System.Exception innerException) { }
    }
    public sealed partial class InvalidDataException : System.Exception
    {
        public InvalidDataException() { }
        public InvalidDataException(string message) { }
        public InvalidDataException(string message, System.Exception innerException) { }
    }
    public partial class MemoryStream : System.IO.Stream
    {
        public MemoryStream() { }
        public MemoryStream(byte[] buffer) { }
        public MemoryStream(byte[] buffer, bool writable) { }
        public MemoryStream(byte[] buffer, int index, int count) { }
        public MemoryStream(byte[] buffer, int index, int count, bool writable) { }
        public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) { }
        public MemoryStream(int capacity) { }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public virtual int Capacity { get { return default(int); } set { } }
        public override long Length { get { return default(long); } }
        public override long Position { get { return default(long); } set { } }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override int Read(byte[] buffer, int offset, int count) { buffer = default(byte[]); return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override int ReadByte() { return default(int); }
        public override long Seek(long offset, System.IO.SeekOrigin loc) { return default(long); }
        public override void SetLength(long value) { }
        public virtual byte[] ToArray() { return default(byte[]); }
        public virtual bool TryGetBuffer(out System.ArraySegment<byte> buffer) { buffer = default(System.ArraySegment<byte>); return default(bool); }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override void WriteByte(byte value) { }
        public virtual void WriteTo(System.IO.Stream stream) { }
    }
    public enum SeekOrigin
    {
        Begin = 0,
        Current = 1,
        End = 2,
    }
    public abstract partial class Stream : System.IDisposable
    {
        public static readonly System.IO.Stream Null;
        protected Stream() { }
        public abstract bool CanRead { get; }
        public abstract bool CanSeek { get; }
        public virtual bool CanTimeout { get { return default(bool); } }
        public abstract bool CanWrite { get; }
        public abstract long Length { get; }
        public abstract long Position { get; set; }
        public virtual int ReadTimeout { get { return default(int); } set { } }
        public virtual int WriteTimeout { get { return default(int); } set { } }
        public void CopyTo(System.IO.Stream destination) { }
        public void CopyTo(System.IO.Stream destination, int bufferSize) { }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract void Flush();
        public System.Threading.Tasks.Task FlushAsync() { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public abstract int Read(byte[] buffer, int offset, int count);
        public System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public virtual int ReadByte() { return default(int); }
        public abstract long Seek(long offset, System.IO.SeekOrigin origin);
        public abstract void SetLength(long value);
        public abstract void Write(byte[] buffer, int offset, int count);
        public System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public virtual void WriteByte(byte value) { }
    }
    public partial class StreamReader : System.IO.TextReader
    {
        public static readonly new System.IO.StreamReader Null;
        public StreamReader(System.IO.Stream stream) { }
        public StreamReader(System.IO.Stream stream, bool detectEncodingFromByteOrderMarks) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen) { }
        public virtual System.IO.Stream BaseStream { get { return default(System.IO.Stream); } }
        public virtual System.Text.Encoding CurrentEncoding { get { return default(System.Text.Encoding); } }
        public bool EndOfStream { get { return default(bool); } }
        public void DiscardBufferedData() { }
        protected override void Dispose(bool disposing) { }
        public override int Peek() { return default(int); }
        public override int Read() { return default(int); }
        public override int Read(char[] buffer, int index, int count) { buffer = default(char[]); return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public override int ReadBlock(char[] buffer, int index, int count) { buffer = default(char[]); return default(int); }
        public override System.Threading.Tasks.Task<int> ReadBlockAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public override string ReadLine() { return default(string); }
        public override System.Threading.Tasks.Task<string> ReadLineAsync() { return default(System.Threading.Tasks.Task<string>); }
        public override string ReadToEnd() { return default(string); }
        public override System.Threading.Tasks.Task<string> ReadToEndAsync() { return default(System.Threading.Tasks.Task<string>); }
    }
    public partial class StreamWriter : System.IO.TextWriter
    {
        public static readonly new System.IO.StreamWriter Null;
        public StreamWriter(System.IO.Stream stream) { }
        public StreamWriter(System.IO.Stream stream, System.Text.Encoding encoding) { }
        public StreamWriter(System.IO.Stream stream, System.Text.Encoding encoding, int bufferSize) { }
        public StreamWriter(System.IO.Stream stream, System.Text.Encoding encoding, int bufferSize, bool leaveOpen) { }
        public virtual bool AutoFlush { get { return default(bool); } set { } }
        public virtual System.IO.Stream BaseStream { get { return default(System.IO.Stream); } }
        public override System.Text.Encoding Encoding { get { return default(System.Text.Encoding); } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync() { return default(System.Threading.Tasks.Task); }
        public override void Write(char value) { }
        public override void Write(char[] buffer) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(string value) { }
        public override System.Threading.Tasks.Task WriteAsync(char value) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteAsync(string value) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteLineAsync() { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteLineAsync(char value) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteLineAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteLineAsync(string value) { return default(System.Threading.Tasks.Task); }
    }
    public partial class StringReader : System.IO.TextReader
    {
        public StringReader(string s) { }
        protected override void Dispose(bool disposing) { }
        public override int Peek() { return default(int); }
        public override int Read() { return default(int); }
        public override int Read(char[] buffer, int index, int count) { buffer = default(char[]); return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public override System.Threading.Tasks.Task<int> ReadBlockAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public override string ReadLine() { return default(string); }
        public override System.Threading.Tasks.Task<string> ReadLineAsync() { return default(System.Threading.Tasks.Task<string>); }
        public override string ReadToEnd() { return default(string); }
        public override System.Threading.Tasks.Task<string> ReadToEndAsync() { return default(System.Threading.Tasks.Task<string>); }
    }
    public partial class StringWriter : System.IO.TextWriter
    {
        public StringWriter() { }
        public StringWriter(System.IFormatProvider formatProvider) { }
        public StringWriter(System.Text.StringBuilder sb) { }
        public StringWriter(System.Text.StringBuilder sb, System.IFormatProvider formatProvider) { }
        public override System.Text.Encoding Encoding { get { return default(System.Text.Encoding); } }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.Task FlushAsync() { return default(System.Threading.Tasks.Task); }
        public virtual System.Text.StringBuilder GetStringBuilder() { return default(System.Text.StringBuilder); }
        public override string ToString() { return default(string); }
        public override void Write(char value) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(string value) { }
        public override System.Threading.Tasks.Task WriteAsync(char value) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteAsync(string value) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteLineAsync(char value) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteLineAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task WriteLineAsync(string value) { return default(System.Threading.Tasks.Task); }
    }
    public abstract partial class TextReader : System.IDisposable
    {
        public static readonly System.IO.TextReader Null;
        protected TextReader() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual int Peek() { return default(int); }
        public virtual int Read() { return default(int); }
        public virtual int Read(char[] buffer, int index, int count) { buffer = default(char[]); return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual int ReadBlock(char[] buffer, int index, int count) { buffer = default(char[]); return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadBlockAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual string ReadLine() { return default(string); }
        public virtual System.Threading.Tasks.Task<string> ReadLineAsync() { return default(System.Threading.Tasks.Task<string>); }
        public virtual string ReadToEnd() { return default(string); }
        public virtual System.Threading.Tasks.Task<string> ReadToEndAsync() { return default(System.Threading.Tasks.Task<string>); }
    }
    public abstract partial class TextWriter : System.IDisposable
    {
        protected char[] CoreNewLine;
        public static readonly System.IO.TextWriter Null;
        protected TextWriter() { }
        protected TextWriter(System.IFormatProvider formatProvider) { }
        public abstract System.Text.Encoding Encoding { get; }
        public virtual System.IFormatProvider FormatProvider { get { return default(System.IFormatProvider); } }
        public virtual string NewLine { get { return default(string); } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual void Flush() { }
        public virtual System.Threading.Tasks.Task FlushAsync() { return default(System.Threading.Tasks.Task); }
        public virtual void Write(bool value) { }
        public abstract void Write(char value);
        public virtual void Write(char[] buffer) { }
        public virtual void Write(char[] buffer, int index, int count) { }
        public virtual void Write(decimal value) { }
        public virtual void Write(double value) { }
        public virtual void Write(int value) { }
        public virtual void Write(long value) { }
        public virtual void Write(object value) { }
        public virtual void Write(float value) { }
        public virtual void Write(string value) { }
        public virtual void Write(string format, object arg0) { }
        public virtual void Write(string format, object arg0, object arg1) { }
        public virtual void Write(string format, object arg0, object arg1, object arg2) { }
        public virtual void Write(string format, params object[] arg) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(ulong value) { }
        public virtual System.Threading.Tasks.Task WriteAsync(char value) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task WriteAsync(char[] buffer) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task WriteAsync(string value) { return default(System.Threading.Tasks.Task); }
        public virtual void WriteLine() { }
        public virtual void WriteLine(bool value) { }
        public virtual void WriteLine(char value) { }
        public virtual void WriteLine(char[] buffer) { }
        public virtual void WriteLine(char[] buffer, int index, int count) { }
        public virtual void WriteLine(decimal value) { }
        public virtual void WriteLine(double value) { }
        public virtual void WriteLine(int value) { }
        public virtual void WriteLine(long value) { }
        public virtual void WriteLine(object value) { }
        public virtual void WriteLine(float value) { }
        public virtual void WriteLine(string value) { }
        public virtual void WriteLine(string format, object arg0) { }
        public virtual void WriteLine(string format, object arg0, object arg1) { }
        public virtual void WriteLine(string format, object arg0, object arg1, object arg2) { }
        public virtual void WriteLine(string format, params object[] arg) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void WriteLine(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void WriteLine(ulong value) { }
        public virtual System.Threading.Tasks.Task WriteLineAsync() { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task WriteLineAsync(char value) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task WriteLineAsync(char[] buffer) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task WriteLineAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task WriteLineAsync(string value) { return default(System.Threading.Tasks.Task); }
    }
}
