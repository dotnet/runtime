// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : System.Runtime.InteropServices.SafeHandle
    {
        public SafePipeHandle(System.IntPtr preexistingHandle, bool ownsHandle) : base(default(System.IntPtr), default(bool)) { }
        protected override bool ReleaseHandle() { return default(bool); }
    }
}
namespace System.IO.Pipes
{
    public sealed partial class AnonymousPipeClientStream : System.IO.Pipes.PipeStream
    {
        public AnonymousPipeClientStream(System.IO.Pipes.PipeDirection direction, Microsoft.Win32.SafeHandles.SafePipeHandle safePipeHandle) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeClientStream(System.IO.Pipes.PipeDirection direction, string pipeHandleAsString) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeClientStream(string pipeHandleAsString) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public override System.IO.Pipes.PipeTransmissionMode ReadMode { set { } }
        public override System.IO.Pipes.PipeTransmissionMode TransmissionMode { get { return default(System.IO.Pipes.PipeTransmissionMode); } }
        ~AnonymousPipeClientStream() { }
    }
    public sealed partial class AnonymousPipeServerStream : System.IO.Pipes.PipeStream
    {
        public AnonymousPipeServerStream() : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, Microsoft.Win32.SafeHandles.SafePipeHandle serverSafePipeHandle, Microsoft.Win32.SafeHandles.SafePipeHandle clientSafePipeHandle) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, System.IO.HandleInheritability inheritability) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, System.IO.HandleInheritability inheritability, int bufferSize) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public Microsoft.Win32.SafeHandles.SafePipeHandle ClientSafePipeHandle { get { return default(Microsoft.Win32.SafeHandles.SafePipeHandle); } }
        public override System.IO.Pipes.PipeTransmissionMode ReadMode { set { } }
        public override System.IO.Pipes.PipeTransmissionMode TransmissionMode { get { return default(System.IO.Pipes.PipeTransmissionMode); } }
        protected override void Dispose(bool disposing) { }
        public void DisposeLocalCopyOfClientHandle() { }
        ~AnonymousPipeServerStream() { }
        public string GetClientHandleAsString() { return default(string); }
    }
    public sealed partial class NamedPipeClientStream : System.IO.Pipes.PipeStream
    {
        public NamedPipeClientStream(System.IO.Pipes.PipeDirection direction, bool isAsync, bool isConnected, Microsoft.Win32.SafeHandles.SafePipeHandle safePipeHandle) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string pipeName) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeOptions options) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeOptions options, System.Security.Principal.TokenImpersonationLevel impersonationLevel) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public int NumberOfServerInstances { get { return default(int); } }
        public void Connect() { }
        public void Connect(int timeout) { }
        public System.Threading.Tasks.Task ConnectAsync() { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ConnectAsync(int timeout) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ConnectAsync(int timeout, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ConnectAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        ~NamedPipeClientStream() { }
    }
    public sealed partial class NamedPipeServerStream : System.IO.Pipes.PipeStream
    {
        public NamedPipeServerStream(System.IO.Pipes.PipeDirection direction, bool isAsync, bool isConnected, Microsoft.Win32.SafeHandles.SafePipeHandle safePipeHandle) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode, System.IO.Pipes.PipeOptions options) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode, System.IO.Pipes.PipeOptions options, int inBufferSize, int outBufferSize) : base(default(System.IO.Pipes.PipeDirection), default(int)) { }
        public void Disconnect() { }
        ~NamedPipeServerStream() { }
        public string GetImpersonationUserName() { return default(string); }
        public void WaitForConnection() { }
        public System.Threading.Tasks.Task WaitForConnectionAsync() { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task WaitForConnectionAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
    }
    public enum PipeDirection
    {
        In = 1,
        InOut = 3,
        Out = 2,
    }
    [System.FlagsAttribute]
    public enum PipeOptions
    {
        Asynchronous = 1073741824,
        None = 0,
        WriteThrough = -2147483648,
    }
    public abstract partial class PipeStream : System.IO.Stream
    {
        protected PipeStream(System.IO.Pipes.PipeDirection direction, int bufferSize) { }
        protected PipeStream(System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeTransmissionMode transmissionMode, int outBufferSize) { }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public virtual int InBufferSize { get { return default(int); } }
        public bool IsAsync { get { return default(bool); } }
        public bool IsConnected { get { return default(bool); } protected set { } }
        public bool IsMessageComplete { get { return default(bool); } }
        public override long Length { get { return default(long); } }
        public virtual int OutBufferSize { get { return default(int); } }
        public override long Position { get { return default(long); } set { } }
        public virtual System.IO.Pipes.PipeTransmissionMode ReadMode { get { return default(System.IO.Pipes.PipeTransmissionMode); } set { } }
        public Microsoft.Win32.SafeHandles.SafePipeHandle SafePipeHandle { get { return default(Microsoft.Win32.SafeHandles.SafePipeHandle); } }
        public virtual System.IO.Pipes.PipeTransmissionMode TransmissionMode { get { return default(System.IO.Pipes.PipeTransmissionMode); } }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) { buffer = default(byte[]); return default(int); }
        public override int ReadByte() { return default(int); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public void WaitForPipeDrain() { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void WriteByte(byte value) { }
    }
    public enum PipeTransmissionMode
    {
        Byte = 0,
        Message = 1,
    }
}
