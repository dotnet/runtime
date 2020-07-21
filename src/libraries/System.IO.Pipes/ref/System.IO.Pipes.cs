// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafePipeHandle(System.IntPtr preexistingHandle, bool ownsHandle) : base (default(bool)) { }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System.IO.Pipes
{
    public sealed partial class AnonymousPipeClientStream : System.IO.Pipes.PipeStream
    {
        public AnonymousPipeClientStream(System.IO.Pipes.PipeDirection direction, Microsoft.Win32.SafeHandles.SafePipeHandle safePipeHandle) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeClientStream(System.IO.Pipes.PipeDirection direction, string pipeHandleAsString) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeClientStream(string pipeHandleAsString) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public override System.IO.Pipes.PipeTransmissionMode ReadMode { set { } }
        public override System.IO.Pipes.PipeTransmissionMode TransmissionMode { get { throw null; } }
        ~AnonymousPipeClientStream() { }
    }
    public sealed partial class AnonymousPipeServerStream : System.IO.Pipes.PipeStream
    {
        public AnonymousPipeServerStream() : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, Microsoft.Win32.SafeHandles.SafePipeHandle serverSafePipeHandle, Microsoft.Win32.SafeHandles.SafePipeHandle clientSafePipeHandle) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, System.IO.HandleInheritability inheritability) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public AnonymousPipeServerStream(System.IO.Pipes.PipeDirection direction, System.IO.HandleInheritability inheritability, int bufferSize) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public Microsoft.Win32.SafeHandles.SafePipeHandle ClientSafePipeHandle { get { throw null; } }
        public override System.IO.Pipes.PipeTransmissionMode ReadMode { set { } }
        public override System.IO.Pipes.PipeTransmissionMode TransmissionMode { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public void DisposeLocalCopyOfClientHandle() { }
        ~AnonymousPipeServerStream() { }
        public string GetClientHandleAsString() { throw null; }
    }
    public static partial class AnonymousPipeServerStreamAcl
    {
        public static System.IO.Pipes.AnonymousPipeServerStream Create(System.IO.Pipes.PipeDirection direction, System.IO.HandleInheritability inheritability, int bufferSize, System.IO.Pipes.PipeSecurity? pipeSecurity) { throw null; }
    }
    public sealed partial class NamedPipeClientStream : System.IO.Pipes.PipeStream
    {
        public NamedPipeClientStream(System.IO.Pipes.PipeDirection direction, bool isAsync, bool isConnected, Microsoft.Win32.SafeHandles.SafePipeHandle safePipeHandle) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string pipeName) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeOptions options) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeOptions options, System.Security.Principal.TokenImpersonationLevel impersonationLevel) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeClientStream(string serverName, string pipeName, System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeOptions options, System.Security.Principal.TokenImpersonationLevel impersonationLevel, System.IO.HandleInheritability inheritability) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        public int NumberOfServerInstances { get { throw null; } }
        protected internal override void CheckPipePropertyOperations() { }
        public void Connect() { }
        public void Connect(int timeout) { }
        public System.Threading.Tasks.Task ConnectAsync() { throw null; }
        public System.Threading.Tasks.Task ConnectAsync(int timeout) { throw null; }
        public System.Threading.Tasks.Task ConnectAsync(int timeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task ConnectAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        ~NamedPipeClientStream() { }
    }
    public sealed partial class NamedPipeServerStream : System.IO.Pipes.PipeStream
    {
        public const int MaxAllowedServerInstances = -1;
        public NamedPipeServerStream(System.IO.Pipes.PipeDirection direction, bool isAsync, bool isConnected, Microsoft.Win32.SafeHandles.SafePipeHandle safePipeHandle) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode, System.IO.Pipes.PipeOptions options) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public NamedPipeServerStream(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode, System.IO.Pipes.PipeOptions options, int inBufferSize, int outBufferSize) : base (default(System.IO.Pipes.PipeDirection), default(int)) { }
        public System.IAsyncResult BeginWaitForConnection(System.AsyncCallback callback, object state) { throw null; }
        public void Disconnect() { }
        public void EndWaitForConnection(System.IAsyncResult asyncResult) { }
        ~NamedPipeServerStream() { }
        public string GetImpersonationUserName() { throw null; }
        public void RunAsClient(System.IO.Pipes.PipeStreamImpersonationWorker impersonationWorker) { }
        public void WaitForConnection() { }
        public System.Threading.Tasks.Task WaitForConnectionAsync() { throw null; }
        public System.Threading.Tasks.Task WaitForConnectionAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public static partial class NamedPipeServerStreamAcl
    {
        public static System.IO.Pipes.NamedPipeServerStream Create(string pipeName, System.IO.Pipes.PipeDirection direction, int maxNumberOfServerInstances, System.IO.Pipes.PipeTransmissionMode transmissionMode, System.IO.Pipes.PipeOptions options, int inBufferSize, int outBufferSize, System.IO.Pipes.PipeSecurity? pipeSecurity, System.IO.HandleInheritability inheritability = System.IO.HandleInheritability.None, System.IO.Pipes.PipeAccessRights additionalAccessRights = default(System.IO.Pipes.PipeAccessRights)) { throw null; }
    }
    [System.FlagsAttribute]
    public enum PipeAccessRights
    {
        ReadData = 1,
        WriteData = 2,
        CreateNewInstance = 4,
        ReadExtendedAttributes = 8,
        WriteExtendedAttributes = 16,
        ReadAttributes = 128,
        WriteAttributes = 256,
        Write = 274,
        Delete = 65536,
        ReadPermissions = 131072,
        Read = 131209,
        ReadWrite = 131483,
        ChangePermissions = 262144,
        TakeOwnership = 524288,
        Synchronize = 1048576,
        FullControl = 2032031,
        AccessSystemSecurity = 16777216,
    }
    public sealed partial class PipeAccessRule : System.Security.AccessControl.AccessRule
    {
        public PipeAccessRule(System.Security.Principal.IdentityReference identity, System.IO.Pipes.PipeAccessRights rights, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public PipeAccessRule(string identity, System.IO.Pipes.PipeAccessRights rights, System.Security.AccessControl.AccessControlType type) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AccessControlType)) { }
        public System.IO.Pipes.PipeAccessRights PipeAccessRights { get { throw null; } }
    }
    public sealed partial class PipeAuditRule : System.Security.AccessControl.AuditRule
    {
        public PipeAuditRule(System.Security.Principal.IdentityReference identity, System.IO.Pipes.PipeAccessRights rights, System.Security.AccessControl.AuditFlags flags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public PipeAuditRule(string identity, System.IO.Pipes.PipeAccessRights rights, System.Security.AccessControl.AuditFlags flags) : base (default(System.Security.Principal.IdentityReference), default(int), default(bool), default(System.Security.AccessControl.InheritanceFlags), default(System.Security.AccessControl.PropagationFlags), default(System.Security.AccessControl.AuditFlags)) { }
        public System.IO.Pipes.PipeAccessRights PipeAccessRights { get { throw null; } }
    }
    public enum PipeDirection
    {
        In = 1,
        Out = 2,
        InOut = 3,
    }
    [System.FlagsAttribute]
    public enum PipeOptions
    {
        WriteThrough = -2147483648,
        None = 0,
        CurrentUserOnly = 536870912,
        Asynchronous = 1073741824,
    }
    public static partial class PipesAclExtensions
    {
        public static System.IO.Pipes.PipeSecurity GetAccessControl(this System.IO.Pipes.PipeStream stream) { throw null; }
        public static void SetAccessControl(this System.IO.Pipes.PipeStream stream, System.IO.Pipes.PipeSecurity pipeSecurity) { }
    }
    public partial class PipeSecurity : System.Security.AccessControl.NativeObjectSecurity
    {
        public PipeSecurity() : base (default(bool), default(System.Security.AccessControl.ResourceType)) { }
        public override System.Type AccessRightType { get { throw null; } }
        public override System.Type AccessRuleType { get { throw null; } }
        public override System.Type AuditRuleType { get { throw null; } }
        public override System.Security.AccessControl.AccessRule AccessRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AccessControlType type) { throw null; }
        public void AddAccessRule(System.IO.Pipes.PipeAccessRule rule) { }
        public void AddAuditRule(System.IO.Pipes.PipeAuditRule rule) { }
        public sealed override System.Security.AccessControl.AuditRule AuditRuleFactory(System.Security.Principal.IdentityReference identityReference, int accessMask, bool isInherited, System.Security.AccessControl.InheritanceFlags inheritanceFlags, System.Security.AccessControl.PropagationFlags propagationFlags, System.Security.AccessControl.AuditFlags flags) { throw null; }
        protected internal void Persist(System.Runtime.InteropServices.SafeHandle handle) { }
        protected internal void Persist(string name) { }
        public bool RemoveAccessRule(System.IO.Pipes.PipeAccessRule rule) { throw null; }
        public void RemoveAccessRuleSpecific(System.IO.Pipes.PipeAccessRule rule) { }
        public bool RemoveAuditRule(System.IO.Pipes.PipeAuditRule rule) { throw null; }
        public void RemoveAuditRuleAll(System.IO.Pipes.PipeAuditRule rule) { }
        public void RemoveAuditRuleSpecific(System.IO.Pipes.PipeAuditRule rule) { }
        public void ResetAccessRule(System.IO.Pipes.PipeAccessRule rule) { }
        public void SetAccessRule(System.IO.Pipes.PipeAccessRule rule) { }
        public void SetAuditRule(System.IO.Pipes.PipeAuditRule rule) { }
    }
    public abstract partial class PipeStream : System.IO.Stream
    {
        protected PipeStream(System.IO.Pipes.PipeDirection direction, int bufferSize) { }
        protected PipeStream(System.IO.Pipes.PipeDirection direction, System.IO.Pipes.PipeTransmissionMode transmissionMode, int outBufferSize) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public virtual int InBufferSize { get { throw null; } }
        public bool IsAsync { get { throw null; } }
        public bool IsConnected { get { throw null; } protected set { } }
        protected bool IsHandleExposed { get { throw null; } }
        public bool IsMessageComplete { get { throw null; } }
        public override long Length { get { throw null; } }
        public virtual int OutBufferSize { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public virtual System.IO.Pipes.PipeTransmissionMode ReadMode { get { throw null; } set { } }
        public Microsoft.Win32.SafeHandles.SafePipeHandle SafePipeHandle { get { throw null; } }
        public virtual System.IO.Pipes.PipeTransmissionMode TransmissionMode { get { throw null; } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback? callback, object? state) { throw null; }
        protected internal virtual void CheckPipePropertyOperations() { }
        protected internal void CheckReadOperations() { }
        protected internal void CheckWriteOperations() { }
        protected override void Dispose(bool disposing) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected void InitializeHandle(Microsoft.Win32.SafeHandles.SafePipeHandle? handle, bool isExposed, bool isAsync) { }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override int Read(System.Span<byte> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        public void WaitForPipeDrain() { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
    public delegate void PipeStreamImpersonationWorker();
    public enum PipeTransmissionMode
    {
        Byte = 0,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        Message = 1,
    }
}
