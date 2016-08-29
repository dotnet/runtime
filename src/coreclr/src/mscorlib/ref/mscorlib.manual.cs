// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("System.Runtime.WindowsRuntime, PublicKey=00000000000000000400000000000000")]

namespace System
{
    partial class Exception
    {
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal void AddExceptionDataForRestrictedErrorInfo(string restrictedError, string restrictedErrorReference, string restrictedCapabilitySid, object restrictedErrorObject, bool hasrestrictedLanguageErrorObject = false) { }
    }
}

namespace System.Diagnostics.Tracing
{
    [System.Diagnostics.Tracing.EventSourceAttribute(Guid = "8E9F5090-2D75-4d03-8A81-E5AFBF85DAF1", Name = "System.Diagnostics.Eventing.FrameworkEventSource")]
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
    internal sealed partial class FrameworkEventSource : System.Diagnostics.Tracing.EventSource
    {
        internal static readonly System.Diagnostics.Tracing.FrameworkEventSource Log;
        private FrameworkEventSource() { }
        internal static bool IsInitialized { get { throw null; } }
        [System.Diagnostics.Tracing.EventAttribute(31, Level = (System.Diagnostics.Tracing.EventLevel)(5), Keywords = (System.Diagnostics.Tracing.EventKeywords)(18))]
        internal void ThreadPoolDequeueWork(long workID) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        internal void ThreadPoolDequeueWorkObject(object workID) { }
        [System.Diagnostics.Tracing.EventAttribute(30, Level = (System.Diagnostics.Tracing.EventLevel)(5), Keywords = (System.Diagnostics.Tracing.EventKeywords)(18))]
        internal void ThreadPoolEnqueueWork(long workID) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        internal void ThreadPoolEnqueueWorkObject(object workID) { }
        [System.Diagnostics.Tracing.EventAttribute(151, Level = (System.Diagnostics.Tracing.EventLevel)(4), Keywords = (System.Diagnostics.Tracing.EventKeywords)(16), Task = (System.Diagnostics.Tracing.EventTask)(3), Opcode = (System.Diagnostics.Tracing.EventOpcode)(240))]
        internal void ThreadTransferReceive(long id, int kind, string info) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        internal void ThreadTransferReceiveObj(object id, int kind, string info) { }
        [System.Diagnostics.Tracing.EventAttribute(150, Level = (System.Diagnostics.Tracing.EventLevel)(4), Keywords = (System.Diagnostics.Tracing.EventKeywords)(16), Task = (System.Diagnostics.Tracing.EventTask)(3), Opcode = (System.Diagnostics.Tracing.EventOpcode)(9))]
        internal void ThreadTransferSend(long id, int kind, string info, bool multiDequeues) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        internal void ThreadTransferSendObj(object id, int kind, string info, bool multiDequeues) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        private void WriteEvent(int eventId, long arg1, int arg2, string arg3) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        private void WriteEvent(int eventId, long arg1, int arg2, string arg3, bool arg4) { }
        public static partial class Keywords
        {
            public const System.Diagnostics.Tracing.EventKeywords DynamicTypeUsage = (System.Diagnostics.Tracing.EventKeywords)8;
            public const System.Diagnostics.Tracing.EventKeywords Loader = (System.Diagnostics.Tracing.EventKeywords)1;
            public const System.Diagnostics.Tracing.EventKeywords NetClient = (System.Diagnostics.Tracing.EventKeywords)4;
            public const System.Diagnostics.Tracing.EventKeywords ThreadPool = (System.Diagnostics.Tracing.EventKeywords)2;
            public const System.Diagnostics.Tracing.EventKeywords ThreadTransfer = (System.Diagnostics.Tracing.EventKeywords)16;
        }
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        public static partial class Opcodes
        {
            public const System.Diagnostics.Tracing.EventOpcode ReceiveHandled = (System.Diagnostics.Tracing.EventOpcode)11;
        }
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        public static partial class Tasks
        {
            public const System.Diagnostics.Tracing.EventTask GetRequestStream = (System.Diagnostics.Tracing.EventTask) 2;
            public const System.Diagnostics.Tracing.EventTask GetResponse = (System.Diagnostics.Tracing.EventTask)1;
            public const System.Diagnostics.Tracing.EventTask ThreadTransfer = (System.Diagnostics.Tracing.EventTask)3;
        }
    }
}

namespace System.Globalization
{
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
    internal partial class CultureData
    {
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal static System.Globalization.CultureData GetCultureData(string cultureName, bool useUserOverride) { throw null; }
    }

    [System.FlagsAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public enum CultureTypes
    {
        AllCultures = 7,
        [System.ObsoleteAttribute("This value has been deprecated.  Please use other values in CultureTypes.")]
        FrameworkCultures = 64,
        InstalledWin32Cultures = 4,
        NeutralCultures = 1,
        ReplacementCultures = 16,
        SpecificCultures = 2,
        UserCustomCulture = 8,
        [System.ObsoleteAttribute("This value has been deprecated.  Please use other values in CultureTypes.")]
        WindowsOnlyCultures = 32,
    }
}

namespace System.IO
{
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    internal sealed partial class BufferedStream : System.IO.Stream
    {
        private byte[] _buffer;
        private readonly int _bufferSize;
        private const int _DefaultBufferSize = 4096;
        private System.Threading.Tasks.Task<int> _lastSyncCompletedReadTask;
        private int _readLen;
        private int _readPos;
        private System.IO.Stream _stream;
        private int _writePos;
        private const int MaxShadowBufferSize = 81920;
        private BufferedStream() { }
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal BufferedStream(System.IO.Stream stream, int bufferSize) { }
        internal int BufferSize { [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        internal System.IO.Stream UnderlyingStream { [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]get { throw null; } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        private System.IAsyncResult BeginReadFromUnderlyingStream(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state, int bytesAlreadySatisfied, System.Threading.Tasks.Task semaphoreLockTask) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        private System.IAsyncResult BeginWriteToUnderlyingStream(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state, System.Threading.Tasks.Task semaphoreLockTask) { throw null; }
        private void ClearReadBufferBeforeWrite() { }
        protected override void Dispose(bool disposing) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        private void EnsureBeginEndAwaitableAllocated() { }
        private void EnsureBufferAllocated() { }
        private void EnsureCanRead() { }
        private void EnsureCanSeek() { }
        private void EnsureCanWrite() { }
        private void EnsureNotClosed() { }
        private void EnsureShadowBufferAllocated() { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        private static System.Threading.Tasks.Task FlushAsyncInternal(System.Threading.CancellationToken cancellationToken, System.IO.BufferedStream _this, System.IO.Stream stream, int writePos, int readPos, int readLen) { throw null; }
        private void FlushRead() { }
        private void FlushWrite() { }
        private System.Threading.Tasks.Task FlushWriteAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        private System.Threading.Tasks.Task<int> LastSyncCompletedReadTask(int val) { throw null; }
        public override int Read(byte[] array, int offset, int count) { array = default(byte[]); throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int ReadByte() { throw null; }
        private int ReadFromBuffer(byte[] array, int offset, int count) { throw null; }
        private int ReadFromBuffer(byte[] array, int offset, int count, out System.Exception error) { error = default(System.Exception); throw null; }
        private System.Threading.Tasks.Task<int> ReadFromUnderlyingStreamAsync(byte[] array, int offset, int count, System.Threading.CancellationToken cancellationToken, int bytesAlreadySatisfied, System.Threading.Tasks.Task semaphoreLockTask, bool useApmPattern) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] array, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void WriteByte(byte value) { }
        private void WriteToBuffer(byte[] array, ref int offset, ref int count) { }
        private void WriteToBuffer(byte[] array, ref int offset, ref int count, out System.Exception error) { error = default(System.Exception); }
        private System.Threading.Tasks.Task WriteToUnderlyingStreamAsync(byte[] array, int offset, int count, System.Threading.CancellationToken cancellationToken, System.Threading.Tasks.Task semaphoreLockTask, bool useApmPattern) { throw null; }
    }
}

namespace System.Resources
{
    [System.Security.SecurityCriticalAttribute]
    internal partial class WindowsRuntimeResourceManagerBase
    {
        public WindowsRuntimeResourceManagerBase() { }
        public virtual System.Globalization.CultureInfo GlobalResourceContextBestFitCultureInfo { [System.Security.SecurityCriticalAttribute]get { throw null; } }
        [System.Security.SecurityCriticalAttribute]
        public virtual string GetString(string stringName, string startingCulture, string neutralResourcesCulture) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual bool Initialize(string libpath, string reswFilename, out System.Resources.PRIExceptionInfo exceptionInfo) { exceptionInfo = default(System.Resources.PRIExceptionInfo); throw null; }
        [System.Security.SecurityCriticalAttribute]
        public virtual bool SetGlobalResourceContextDefaultCulture(System.Globalization.CultureInfo ci) { throw null; }
    }

    internal partial class PRIExceptionInfo
    {
        [System.CLSCompliantAttribute(false)]
        public string _PackageSimpleName;
        [System.CLSCompliantAttribute(false)]
        public string _ResWFile;
        public PRIExceptionInfo() { }
    }
}

namespace System.Runtime.CompilerServices
{
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
    internal static partial class JitHelpers
    {
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        [System.Security.SecurityCriticalAttribute]
        internal static T UnsafeCast<T>(object o) where T : class { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2044), AllowMultiple = false, Inherited = false)]
    internal sealed partial class FriendAccessAllowedAttribute : System.Attribute
    {
        public FriendAccessAllowedAttribute() { }
    }
    partial class ConditionalWeakTable<TKey, TValue>
    {
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        internal TKey FindEquivalentKeyUnsafe(TKey key, out TValue value) { value = default(TValue); throw null; }
    }
}

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(5148), Inherited = false)]
    internal sealed partial class WindowsRuntimeImportAttribute : System.Attribute
    {
        internal WindowsRuntimeImportAttribute() { }
    }

    [System.Runtime.InteropServices.GuidAttribute("82BA7092-4C88-427D-A7BC-16DD93FEB67E")]
    [System.Runtime.InteropServices.InterfaceTypeAttribute((System.Runtime.InteropServices.ComInterfaceType)(1))]
    internal partial interface IRestrictedErrorInfo
    {
        void GetErrorDetails(out string description, out int error, out string restrictedDescription, out string capabilitySid);
        void GetReference(out string reference);
    }

#if FEATURE_COMINTEROP
    [System.AttributeUsageAttribute((System.AttributeTargets)(1028), AllowMultiple=false, Inherited=false)]
    public sealed partial class DefaultInterfaceAttribute : System.Attribute
    {
        public DefaultInterfaceAttribute(System.Type defaultInterface) { }
        public System.Type DefaultInterface { get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct EventRegistrationToken
    {
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken left, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken right) { throw null; }
        public static bool operator !=(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken left, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken right) { throw null; }
    }
    public sealed partial class EventRegistrationTokenTable<T> where T : class
    {
        public EventRegistrationTokenTable() { }
        public T InvocationList { get { throw null; } set { } }
        public System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken AddEventHandler(T handler) { throw null; }
        public static System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<T> refEventTable) { throw null; }
        public void RemoveEventHandler(T handler) { }
        public void RemoveEventHandler(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken token) { }

        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal T ExtractHandler(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken token) { throw null; }
    }
    [System.Runtime.InteropServices.GuidAttribute("00000035-0000-0000-C000-000000000046")]
    public partial interface IActivationFactory
    {
        object ActivateInstance();
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1028), Inherited=false, AllowMultiple=true)]
    public sealed partial class InterfaceImplementedInVersionAttribute : System.Attribute
    {
        public InterfaceImplementedInVersionAttribute(System.Type interfaceType, byte majorVersion, byte minorVersion, byte buildVersion, byte revisionVersion) { }
        public byte BuildVersion { get { throw null; } }
        public System.Type InterfaceType { get { throw null; } }
        public byte MajorVersion { get { throw null; } }
        public byte MinorVersion { get { throw null; } }
        public byte RevisionVersion { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false, AllowMultiple=false)]
    public sealed partial class ReadOnlyArrayAttribute : System.Attribute
    {
        public ReadOnlyArrayAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(12288), AllowMultiple=false, Inherited=false)]
    public sealed partial class ReturnValueNameAttribute : System.Attribute
    {
        public ReturnValueNameAttribute(string name) { }
        public string Name { get { throw null; } }
    }
    public static partial class WindowsRuntimeMarshal
    {
        [System.Security.SecurityCriticalAttribute]
        public static void AddEventHandler<T>(System.Func<T, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> addMethod, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod, T handler) { }
        [System.Security.SecurityCriticalAttribute]
        public static void FreeHString(System.IntPtr ptr) { }
        [System.Security.SecurityCriticalAttribute]
        public static System.Runtime.InteropServices.WindowsRuntime.IActivationFactory GetActivationFactory(System.Type type) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringHString(System.IntPtr ptr) { throw null; }
        [System.Security.SecurityCriticalAttribute]
        public static void RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod) { }
        [System.Security.SecurityCriticalAttribute]
        public static void RemoveEventHandler<T>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod, T handler) { }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr StringToHString(string s) { throw null; }
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        [System.Security.SecuritySafeCriticalAttribute]
        internal static bool ReportUnhandledError(System.Exception e) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false, AllowMultiple=false)]
    public sealed partial class WriteOnlyArrayAttribute : System.Attribute
    {
        public WriteOnlyArrayAttribute() { }
    }
#endif //FEATURE_COMINTEROP
}

namespace System.StubHelpers
{
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
    internal static partial class EventArgsMarshaler
    {
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        [System.Security.SecurityCriticalAttribute]
        internal static System.IntPtr CreateNativeNCCEventArgsInstance(int action, object newItems, object oldItems, int newIndex, int oldIndex) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.PreserveSig)]
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        [System.Security.SecurityCriticalAttribute]
        internal static System.IntPtr CreateNativePCEventArgsInstance(string name) { throw null; }
    }
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
    internal static partial class InterfaceMarshaler
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal static object ConvertToManagedWithoutUnboxing(System.IntPtr pNative) { throw null; }
    }
}

namespace System.Threading
{
    internal enum StackCrawlMark
    {
        LookForMe = 0,
        LookForMyCaller = 1,
        LookForMyCallersCaller = 2,
        LookForThread = 3,
    }
    [System.Security.SecurityCriticalAttribute]
    internal partial class WinRTSynchronizationContextFactoryBase
    {
        public WinRTSynchronizationContextFactoryBase() { }
        [System.Security.SecurityCriticalAttribute]
        public virtual System.Threading.SynchronizationContext Create(object coreDispatcher) { throw null; }
    }
    partial struct CancellationTokenRegistration
    {
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal bool TryDeregister() { throw null; }
    }
    partial class ExecutionContext
    {
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        [System.Security.SecurityCriticalAttribute]
        internal static void Run(System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, object state, bool preserveSyncCtx) { }
        internal static System.Threading.ExecutionContext FastCapture() { return default(System.Threading.ExecutionContext); }
    }
}

namespace System.Threading.Tasks
{
#if FEATURE_COMINTEROP
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
    internal enum AsyncCausalityStatus
    {
        Canceled = 2,
        Completed = 1,
        Error = 3,
        Started = 0,
    }
    internal enum CausalityRelation
    {
        AssignDelegate = 0,
        Cancel = 3,
        Choice = 2,
        Error = 4,
        Join = 1,
    }
    internal enum CausalitySynchronousWork
    {
        CompletionNotification = 0,
        Execution = 2,
        ProgressNotification = 1,
    }
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
    internal static partial class AsyncCausalityTracer
    {
        private static System.Threading.Tasks.AsyncCausalityTracer.Loggers f_LoggingOn;
        //private const Windows.Foundation.Diagnostics.CausalitySource s_CausalitySource = 1;
        private static readonly System.Guid s_PlatformId;
        private static Windows.Foundation.Diagnostics.IAsyncCausalityTracerStatics s_TracerFactory;
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal static bool LoggingOn { [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]get { throw null; } }
        internal static void EnableToETW(bool enabled) { }
        private static ulong GetOperationId(uint taskId) { throw null; }
        private static void LogAndDisable(System.Exception ex) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal static void TraceOperationCompletion(System.Threading.Tasks.CausalityTraceLevel traceLevel, int taskId, System.Threading.Tasks.AsyncCausalityStatus status) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal static void TraceOperationCreation(System.Threading.Tasks.CausalityTraceLevel traceLevel, int taskId, string operationName, ulong relatedContext) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void TraceOperationRelation(System.Threading.Tasks.CausalityTraceLevel traceLevel, int taskId, System.Threading.Tasks.CausalityRelation relation) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void TraceSynchronousWorkCompletion(System.Threading.Tasks.CausalityTraceLevel traceLevel, System.Threading.Tasks.CausalitySynchronousWork work) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void TraceSynchronousWorkStart(System.Threading.Tasks.CausalityTraceLevel traceLevel, int taskId, System.Threading.Tasks.CausalitySynchronousWork work) { }
        [System.Security.SecuritySafeCriticalAttribute]
        private static void TracingStatusChangedHandler(object sender, Windows.Foundation.Diagnostics.TracingStatusChangedEventArgs args) { }
        [System.FlagsAttribute]
        private enum Loggers : byte
        {
            CausalityTracer = (byte)1,
            ETW = (byte)2,
        }
    }
    [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
    internal enum CausalityTraceLevel
    {
        Important = 1,
        Required = 0,
        Verbose = 2,
    }
#endif

    partial class Task
    {
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal static bool s_asyncDebuggingEnabled;
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal static bool AddToActiveTasks(System.Threading.Tasks.Task task) { throw null; }
        [System.Runtime.CompilerServices.FriendAccessAllowedAttribute]
        internal static void RemoveFromActiveTasks(int taskId) { }
    }
}

namespace System.Security.Cryptography
{
    public abstract class HashAlgorithm : System.IDisposable, System.Security.Cryptography.ICryptoTransform
    {
        protected internal byte[] HashValue;
        protected int HashSizeValue;
        protected int State;
        protected HashAlgorithm() { }
        public virtual bool CanReuseTransform { get { throw null; } }
        public virtual bool CanTransformMultipleBlocks { get { throw null; } }
        public virtual byte[] Hash { get { throw null; } }
        public virtual int HashSize { get { throw null; } }
        public virtual int InputBlockSize { get { throw null; } }
        public virtual int OutputBlockSize { get { throw null; } }
        public void Clear() { }
        public byte[] ComputeHash(byte[] buffer) { throw null; }
        public byte[] ComputeHash(byte[] buffer, int offset, int count) { throw null; }
        public byte[] ComputeHash(System.IO.Stream inputStream) { throw null; }
        public static HashAlgorithm Create() { throw null; }
        public static HashAlgorithm Create(string hashName) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected abstract void HashCore(byte[] array, int ibStart, int cbSize);
        protected abstract byte[] HashFinal();
        public abstract void Initialize();
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) { throw null; }
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) { throw null; }
    }

    public abstract class SymmetricAlgorithm : System.IDisposable
    {
        protected byte[] IVValue;
        protected byte[] KeyValue;
        protected int BlockSizeValue;
        protected int FeedbackSizeValue;
        protected int KeySizeValue;
        protected CipherMode ModeValue;
        protected KeySizes[] LegalBlockSizesValue;
        protected KeySizes[] LegalKeySizesValue;
        protected PaddingMode PaddingValue;
        protected SymmetricAlgorithm() { }
        public virtual int BlockSize { get; set; }
        public virtual int FeedbackSize { get; set; }
        public virtual byte[] IV { get; set; }
        public virtual byte[] Key { get; set; }
        public virtual int KeySize { get; set; }
        public virtual KeySizes[] LegalBlockSizes { get; }
        public virtual KeySizes[] LegalKeySizes { get; }
        public virtual CipherMode Mode { get; set; }
        public virtual PaddingMode Padding { get; set; }
        public void Clear() { }
        public static SymmetricAlgorithm Create() { throw null; }
        public static SymmetricAlgorithm Create(string algName) { throw null; }
        public virtual ICryptoTransform CreateDecryptor() { throw null; }
        public abstract ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV);
        public virtual ICryptoTransform CreateEncryptor() { throw null; }
        public abstract ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV);
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract void GenerateIV();
        public abstract void GenerateKey();
        public bool ValidKeySize(int bitLength) { throw null; }
    }

    public interface ICryptoTransform : System.IDisposable
    {
        int InputBlockSize { get; }
        int OutputBlockSize { get; }
        bool CanTransformMultipleBlocks { get; }
        bool CanReuseTransform { get; }
        int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);
        byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);
    }

    public sealed class KeySizes
    {
        public KeySizes(int minSize, int maxSize, int skipSize) { }
        public int MaxSize { get; }
        public int MinSize { get; }
        public int SkipSize { get; }
    }

    public enum PaddingMode
    {
        ANSIX923 = 4,
        ISO10126 = 5,
        None = 1,
        PKCS7 = 2,
        Zeros = 3,
    }

    public enum CipherMode
    {
        CBC = 1,
        CFB = 4,
        CTS = 5,
        ECB = 2,
        OFB = 3,
    }
}

#if FEATURE_COMINTEROP
namespace Windows.Foundation.Diagnostics
{
    internal enum AsyncCausalityStatus
    {
        Canceled = 2,
        Completed = 1,
        Error = 3,
        Started = 0,
    }
    internal enum CausalityRelation
    {
        AssignDelegate = 0,
        Cancel = 3,
        Choice = 2,
        Error = 4,
        Join = 1,
    }
    internal enum CausalitySource
    {
        Application = 0,
        Library = 1,
        System = 2,
    }
    internal enum CausalitySynchronousWork
    {
        CompletionNotification = 0,
        Execution = 2,
        ProgressNotification = 1,
    }
    internal enum CausalityTraceLevel
    {
        Important = 1,
        Required = 0,
        Verbose = 2,
    }
    [System.Runtime.InteropServices.GuidAttribute("50850B26-267E-451B-A890-AB6A370245EE")]
    internal partial interface IAsyncCausalityTracerStatics
    {
        System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken add_TracingStatusChanged(System.EventHandler<Windows.Foundation.Diagnostics.TracingStatusChangedEventArgs> eventHandler);
        void TraceOperationCompletion(Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel, Windows.Foundation.Diagnostics.CausalitySource source, System.Guid platformId, ulong operationId, Windows.Foundation.Diagnostics.AsyncCausalityStatus status);
        void TraceOperationCreation(Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel, Windows.Foundation.Diagnostics.CausalitySource source, System.Guid platformId, ulong operationId, string operationName, ulong relatedContext);
        void TraceOperationRelation(Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel, Windows.Foundation.Diagnostics.CausalitySource source, System.Guid platformId, ulong operationId, Windows.Foundation.Diagnostics.CausalityRelation relation);
        void TraceSynchronousWorkCompletion(Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel, Windows.Foundation.Diagnostics.CausalitySource source, Windows.Foundation.Diagnostics.CausalitySynchronousWork work);
        void TraceSynchronousWorkStart(Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel, Windows.Foundation.Diagnostics.CausalitySource source, System.Guid platformId, ulong operationId, Windows.Foundation.Diagnostics.CausalitySynchronousWork work);
    }
    [System.Runtime.InteropServices.GuidAttribute("410B7711-FF3B-477F-9C9A-D2EFDA302DC3")]
    internal partial interface ITracingStatusChangedEventArgs
    {
        bool Enabled { get; }
        Windows.Foundation.Diagnostics.CausalityTraceLevel TraceLevel { get; }
    }
    [System.Runtime.InteropServices.GuidAttribute("410B7711-FF3B-477F-9C9A-D2EFDA302DC3")]
    internal sealed partial class TracingStatusChangedEventArgs : Windows.Foundation.Diagnostics.ITracingStatusChangedEventArgs
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        public TracingStatusChangedEventArgs() { }
        public bool Enabled { [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
        public Windows.Foundation.Diagnostics.CausalityTraceLevel TraceLevel { [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]get { throw null; } }
    }
}
#endif

namespace System.Security.Claims
{
    public partial class Claim
    {
        public Claim(System.IO.BinaryReader reader) { }
        public Claim(System.IO.BinaryReader reader, System.Security.Claims.ClaimsIdentity subject) { }
        protected Claim(System.Security.Claims.Claim other) { }
        protected Claim(System.Security.Claims.Claim other, System.Security.Claims.ClaimsIdentity subject) { }
        public Claim(string type, string value) { }
        public Claim(string type, string value, string valueType) { }
        public Claim(string type, string value, string valueType, string issuer) { }
        public Claim(string type, string value, string valueType, string issuer, string originalIssuer) { }
        public Claim(string type, string value, string valueType, string issuer, string originalIssuer, System.Security.Claims.ClaimsIdentity subject) { }
        protected virtual byte[] CustomSerializationData { get { throw null; } }
        public string Issuer { get { throw null; } }
        public string OriginalIssuer { get { throw null; } }
        public System.Collections.Generic.IDictionary<string, string> Properties { get { throw null; } }
        public System.Security.Claims.ClaimsIdentity Subject { get { throw null; } }
        public string Type { get { throw null; } }
        public string Value { get { throw null; } }
        public string ValueType { get { throw null; } }
        public virtual System.Security.Claims.Claim Clone() { throw null; }
        public virtual System.Security.Claims.Claim Clone(System.Security.Claims.ClaimsIdentity identity) { throw null; }
        public override string ToString() { throw null; }
        public virtual void WriteTo(System.IO.BinaryWriter writer) { }
        protected virtual void WriteTo(System.IO.BinaryWriter writer, byte[] userData) { }
    }
    public partial class ClaimsIdentity : System.Security.Principal.IIdentity
    {
        public const string DefaultIssuer = "LOCAL AUTHORITY";
        public const string DefaultNameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        public const string DefaultRoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        public ClaimsIdentity() { }
        public ClaimsIdentity(System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims) { }
        public ClaimsIdentity(System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, string authenticationType) { }
        public ClaimsIdentity(System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, string authenticationType, string nameType, string roleType) { }
        public ClaimsIdentity(System.IO.BinaryReader reader) { }
        protected ClaimsIdentity(System.Security.Claims.ClaimsIdentity other) { }
        public ClaimsIdentity(System.Security.Principal.IIdentity identity) { }
        public ClaimsIdentity(System.Security.Principal.IIdentity identity, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims) { }
        public ClaimsIdentity(System.Security.Principal.IIdentity identity, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, string authenticationType, string nameType, string roleType) { }
        public ClaimsIdentity(string authenticationType) { }
        public ClaimsIdentity(string authenticationType, string nameType, string roleType) { }
        public System.Security.Claims.ClaimsIdentity Actor { get { throw null; } set { } }
        public virtual string AuthenticationType { get { throw null; } }
        public object BootstrapContext { get { throw null; } set { } }
        public virtual System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> Claims { get { throw null; } }
        protected virtual byte[] CustomSerializationData { get { throw null; } }
        public virtual bool IsAuthenticated { get { throw null; } }
        public string Label { get { throw null; } set { } }
        public virtual string Name { get { throw null; } }
        public string NameClaimType { get { throw null; } }
        public string RoleClaimType { get { throw null; } }
        public virtual void AddClaim(System.Security.Claims.Claim claim) { }
        public virtual void AddClaims(System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims) { }
        public virtual System.Security.Claims.ClaimsIdentity Clone() { throw null; }
        protected virtual System.Security.Claims.Claim CreateClaim(System.IO.BinaryReader reader) { throw null; }
        public virtual System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> FindAll(System.Predicate<System.Security.Claims.Claim> match) { throw null; }
        public virtual System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> FindAll(string type) { throw null; }
        public virtual System.Security.Claims.Claim FindFirst(System.Predicate<System.Security.Claims.Claim> match) { throw null; }
        public virtual System.Security.Claims.Claim FindFirst(string type) { throw null; }
        public virtual bool HasClaim(System.Predicate<System.Security.Claims.Claim> match) { throw null; }
        public virtual bool HasClaim(string type, string value) { throw null; }
        public virtual void RemoveClaim(System.Security.Claims.Claim claim) { }
        public virtual bool TryRemoveClaim(System.Security.Claims.Claim claim) { throw null; }
        public virtual void WriteTo(System.IO.BinaryWriter writer) { }
        protected virtual void WriteTo(System.IO.BinaryWriter writer, byte[] userData) { }
    }
    public partial class ClaimsPrincipal : System.Security.Principal.IPrincipal
    {
        public ClaimsPrincipal() { }
        public ClaimsPrincipal(System.Collections.Generic.IEnumerable<System.Security.Claims.ClaimsIdentity> identities) { }
        public ClaimsPrincipal(System.IO.BinaryReader reader) { }
        public ClaimsPrincipal(System.Security.Principal.IIdentity identity) { }
        public ClaimsPrincipal(System.Security.Principal.IPrincipal principal) { }
        public virtual System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> Claims { get { throw null; } }
        public static System.Func<System.Security.Claims.ClaimsPrincipal> ClaimsPrincipalSelector { get { throw null; } set { } }
        public static System.Security.Claims.ClaimsPrincipal Current { get { throw null; } }
        protected virtual byte[] CustomSerializationData { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Security.Claims.ClaimsIdentity> Identities { get { throw null; } }
        public virtual System.Security.Principal.IIdentity Identity { get { throw null; } }
        public static System.Func<System.Collections.Generic.IEnumerable<System.Security.Claims.ClaimsIdentity>, System.Security.Claims.ClaimsIdentity> PrimaryIdentitySelector { get { throw null; } set { } }
        public virtual void AddIdentities(System.Collections.Generic.IEnumerable<System.Security.Claims.ClaimsIdentity> identities) { }
        public virtual void AddIdentity(System.Security.Claims.ClaimsIdentity identity) { }
        public virtual System.Security.Claims.ClaimsPrincipal Clone() { throw null; }
        protected virtual System.Security.Claims.ClaimsIdentity CreateClaimsIdentity(System.IO.BinaryReader reader) { throw null; }
        public virtual System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> FindAll(System.Predicate<System.Security.Claims.Claim> match) { throw null; }
        public virtual System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> FindAll(string type) { throw null; }
        public virtual System.Security.Claims.Claim FindFirst(System.Predicate<System.Security.Claims.Claim> match) { throw null; }
        public virtual System.Security.Claims.Claim FindFirst(string type) { throw null; }
        public virtual bool HasClaim(System.Predicate<System.Security.Claims.Claim> match) { throw null; }
        public virtual bool HasClaim(string type, string value) { throw null; }
        public virtual bool IsInRole(string role) { throw null; }
        public virtual void WriteTo(System.IO.BinaryWriter writer) { }
        protected virtual void WriteTo(System.IO.BinaryWriter writer, byte[] userData) { }
    }
    public static partial class ClaimTypes
    {
        public const string Actor = "http://schemas.xmlsoap.org/ws/2009/09/identity/claims/actor";
        public const string Anonymous = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/anonymous";
        public const string Authentication = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authentication";
        public const string AuthenticationInstant = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationinstant";
        public const string AuthenticationMethod = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod";
        public const string AuthorizationDecision = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/authorizationdecision";
        public const string CookiePath = "http://schemas.microsoft.com/ws/2008/06/identity/claims/cookiepath";
        public const string Country = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/country";
        public const string DateOfBirth = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth";
        public const string DenyOnlyPrimaryGroupSid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarygroupsid";
        public const string DenyOnlyPrimarySid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarysid";
        public const string DenyOnlySid = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid";
        public const string DenyOnlyWindowsDeviceGroup = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlywindowsdevicegroup";
        public const string Dns = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dns";
        public const string Dsa = "http://schemas.microsoft.com/ws/2008/06/identity/claims/dsa";
        public const string Email = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        public const string Expiration = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration";
        public const string Expired = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expired";
        public const string Gender = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/gender";
        public const string GivenName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname";
        public const string GroupSid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid";
        public const string Hash = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/hash";
        public const string HomePhone = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/homephone";
        public const string IsPersistent = "http://schemas.microsoft.com/ws/2008/06/identity/claims/ispersistent";
        public const string Locality = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/locality";
        public const string MobilePhone = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone";
        public const string Name = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        public const string NameIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        public const string OtherPhone = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/otherphone";
        public const string PostalCode = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/postalcode";
        public const string PrimaryGroupSid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarygroupsid";
        public const string PrimarySid = "http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid";
        public const string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        public const string Rsa = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/rsa";
        public const string SerialNumber = "http://schemas.microsoft.com/ws/2008/06/identity/claims/serialnumber";
        public const string Sid = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid";
        public const string Spn = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/spn";
        public const string StateOrProvince = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/stateorprovince";
        public const string StreetAddress = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/streetaddress";
        public const string Surname = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname";
        public const string System = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/system";
        public const string Thumbprint = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/thumbprint";
        public const string Upn = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
        public const string Uri = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/uri";
        public const string UserData = "http://schemas.microsoft.com/ws/2008/06/identity/claims/userdata";
        public const string Version = "http://schemas.microsoft.com/ws/2008/06/identity/claims/version";
        public const string Webpage = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/webpage";
        public const string WindowsAccountName = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsaccountname";
        public const string WindowsDeviceClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdeviceclaim";
        public const string WindowsDeviceGroup = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdevicegroup";
        public const string WindowsFqbnVersion = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsfqbnversion";
        public const string WindowsSubAuthority = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority";
        public const string WindowsUserClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsuserclaim";
        public const string X500DistinguishedName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/x500distinguishedname";
    }
    public static partial class ClaimValueTypes
    {
        public const string Base64Binary = "http://www.w3.org/2001/XMLSchema#base64Binary";
        public const string Base64Octet = "http://www.w3.org/2001/XMLSchema#base64Octet";
        public const string Boolean = "http://www.w3.org/2001/XMLSchema#boolean";
        public const string Date = "http://www.w3.org/2001/XMLSchema#date";
        public const string DateTime = "http://www.w3.org/2001/XMLSchema#dateTime";
        public const string DaytimeDuration = "http://www.w3.org/TR/2002/WD-xquery-operators-20020816#dayTimeDuration";
        public const string DnsName = "http://schemas.xmlsoap.org/claims/dns";
        public const string Double = "http://www.w3.org/2001/XMLSchema#double";
        public const string DsaKeyValue = "http://www.w3.org/2000/09/xmldsig#DSAKeyValue";
        public const string Email = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        public const string Fqbn = "http://www.w3.org/2001/XMLSchema#fqbn";
        public const string HexBinary = "http://www.w3.org/2001/XMLSchema#hexBinary";
        public const string Integer = "http://www.w3.org/2001/XMLSchema#integer";
        public const string Integer32 = "http://www.w3.org/2001/XMLSchema#integer32";
        public const string Integer64 = "http://www.w3.org/2001/XMLSchema#integer64";
        public const string KeyInfo = "http://www.w3.org/2000/09/xmldsig#KeyInfo";
        public const string Rfc822Name = "urn:oasis:names:tc:xacml:1.0:data-type:rfc822Name";
        public const string Rsa = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/rsa";
        public const string RsaKeyValue = "http://www.w3.org/2000/09/xmldsig#RSAKeyValue";
        public const string Sid = "http://www.w3.org/2001/XMLSchema#sid";
        public const string String = "http://www.w3.org/2001/XMLSchema#string";
        public const string Time = "http://www.w3.org/2001/XMLSchema#time";
        public const string UInteger32 = "http://www.w3.org/2001/XMLSchema#uinteger32";
        public const string UInteger64 = "http://www.w3.org/2001/XMLSchema#uinteger64";
        public const string UpnName = "http://schemas.xmlsoap.org/claims/UPN";
        public const string X500Name = "urn:oasis:names:tc:xacml:1.0:data-type:x500Name";
        public const string YearMonthDuration = "http://www.w3.org/TR/2002/WD-xquery-operators-20020816#yearMonthDuration";
    }
}