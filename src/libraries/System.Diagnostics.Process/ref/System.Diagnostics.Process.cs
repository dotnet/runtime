// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : System.Runtime.InteropServices.SafeHandle
    {
        public SafeProcessHandle(System.IntPtr existingHandle, bool ownsHandle) : base(default(System.IntPtr), default(bool)) { }
        protected override bool ReleaseHandle() { return default(bool); }
    }
}
namespace System.Diagnostics
{
    public partial class DataReceivedEventArgs : System.EventArgs
    {
        internal DataReceivedEventArgs() { }
        public string Data { get { return default(string); } }
    }
    public delegate void DataReceivedEventHandler(object sender, System.Diagnostics.DataReceivedEventArgs e);
    public partial class Process
    {
        public Process() { }
        public int BasePriority { get { return default(int); } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool EnableRaisingEvents { get { return default(bool); } set { } }
        public int ExitCode { get { return default(int); } }
        public System.DateTime ExitTime { get { return default(System.DateTime); } }
        public bool HasExited { get { return default(bool); } }
        public int Id { get { return default(int); } }
        public string MachineName { get { return default(string); } }
        public System.Diagnostics.ProcessModule MainModule { get { return default(System.Diagnostics.ProcessModule); } }
        public System.IntPtr MaxWorkingSet { get { return default(System.IntPtr); } set { } }
        public System.IntPtr MinWorkingSet { get { return default(System.IntPtr); } set { } }
        public System.Diagnostics.ProcessModuleCollection Modules { get { return default(System.Diagnostics.ProcessModuleCollection); } }
        public long NonpagedSystemMemorySize64 { get { return default(long); } }
        public long PagedMemorySize64 { get { return default(long); } }
        public long PagedSystemMemorySize64 { get { return default(long); } }
        public long PeakPagedMemorySize64 { get { return default(long); } }
        public long PeakVirtualMemorySize64 { get { return default(long); } }
        public long PeakWorkingSet64 { get { return default(long); } }
        public bool PriorityBoostEnabled { get { return default(bool); } set { } }
        public System.Diagnostics.ProcessPriorityClass PriorityClass { get { return default(System.Diagnostics.ProcessPriorityClass); } set { } }
        public long PrivateMemorySize64 { get { return default(long); } }
        public System.TimeSpan PrivilegedProcessorTime { get { return default(System.TimeSpan); } }
        public string ProcessName { get { return default(string); } }
        public System.IntPtr ProcessorAffinity { get { return default(System.IntPtr); } set { } }
        public Microsoft.Win32.SafeHandles.SafeProcessHandle SafeHandle { get { return default(Microsoft.Win32.SafeHandles.SafeProcessHandle); } }
        public int SessionId { get { return default(int); } }
        public System.IO.StreamReader StandardError { get { return default(System.IO.StreamReader); } }
        public System.IO.StreamWriter StandardInput { get { return default(System.IO.StreamWriter); } }
        public System.IO.StreamReader StandardOutput { get { return default(System.IO.StreamReader); } }
        public System.Diagnostics.ProcessStartInfo StartInfo { get { return default(System.Diagnostics.ProcessStartInfo); } set { } }
        public System.DateTime StartTime { get { return default(System.DateTime); } }
        public System.Diagnostics.ProcessThreadCollection Threads { get { return default(System.Diagnostics.ProcessThreadCollection); } }
        public System.TimeSpan TotalProcessorTime { get { return default(System.TimeSpan); } }
        public System.TimeSpan UserProcessorTime { get { return default(System.TimeSpan); } }
        public long VirtualMemorySize64 { get { return default(long); } }
        public long WorkingSet64 { get { return default(long); } }
        public event System.Diagnostics.DataReceivedEventHandler ErrorDataReceived { add { } remove { } }
        public event System.EventHandler Exited { add { } remove { } }
        public event System.Diagnostics.DataReceivedEventHandler OutputDataReceived { add { } remove { } }
        public void BeginErrorReadLine() { }
        public void BeginOutputReadLine() { }
        public void CancelErrorRead() { }
        public void CancelOutputRead() { }
        public static void EnterDebugMode() { }
        public static System.Diagnostics.Process GetCurrentProcess() { return default(System.Diagnostics.Process); }
        public static System.Diagnostics.Process GetProcessById(int processId) { return default(System.Diagnostics.Process); }
        public static System.Diagnostics.Process GetProcessById(int processId, string machineName) { return default(System.Diagnostics.Process); }
        public static System.Diagnostics.Process[] GetProcesses() { return default(System.Diagnostics.Process[]); }
        public static System.Diagnostics.Process[] GetProcesses(string machineName) { return default(System.Diagnostics.Process[]); }
        public static System.Diagnostics.Process[] GetProcessesByName(string processName) { return default(System.Diagnostics.Process[]); }
        public static System.Diagnostics.Process[] GetProcessesByName(string processName, string machineName) { return default(System.Diagnostics.Process[]); }
        public void Kill() { }
        public static void LeaveDebugMode() { }
        protected void OnExited() { }
        public void Refresh() { }
        public bool Start() { return default(bool); }
        public static System.Diagnostics.Process Start(System.Diagnostics.ProcessStartInfo startInfo) { return default(System.Diagnostics.Process); }
        public static System.Diagnostics.Process Start(string fileName) { return default(System.Diagnostics.Process); }
        public static System.Diagnostics.Process Start(string fileName, string arguments) { return default(System.Diagnostics.Process); }
        public void WaitForExit() { }
        public bool WaitForExit(int milliseconds) { return default(bool); }
    }
    public partial class ProcessModule
    {
        internal ProcessModule() { }
        public System.IntPtr BaseAddress { get { return default(System.IntPtr); } }
        public System.IntPtr EntryPointAddress { get { return default(System.IntPtr); } }
        public string FileName { get { return default(string); } }
        public int ModuleMemorySize { get { return default(int); } }
        public string ModuleName { get { return default(string); } }
        public override string ToString() { return default(string); }
    }
    public partial class ProcessModuleCollection
    {
        protected ProcessModuleCollection() { }
        public ProcessModuleCollection(System.Diagnostics.ProcessModule[] processModules) { }
        public System.Diagnostics.ProcessModule this[int index] { get { return default(System.Diagnostics.ProcessModule); } }
        public bool Contains(System.Diagnostics.ProcessModule module) { return default(bool); }
        public void CopyTo(System.Diagnostics.ProcessModule[] array, int index) { }
        public int IndexOf(System.Diagnostics.ProcessModule module) { return default(int); }
    }
    public enum ProcessPriorityClass
    {
        AboveNormal = 32768,
        BelowNormal = 16384,
        High = 128,
        Idle = 64,
        Normal = 32,
        RealTime = 256,
    }
    public sealed partial class ProcessStartInfo
    {
        public ProcessStartInfo() { }
        public ProcessStartInfo(string fileName) { }
        public ProcessStartInfo(string fileName, string arguments) { }
        public string Arguments { get { return default(string); } set { } }
        public bool CreateNoWindow { get { return default(bool); } set { } }
        public string Domain { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Collections.Generic.IDictionary<string, string> Environment { get { return default(System.Collections.Generic.IDictionary<string, string>); } }
        public string FileName { get { return default(string); } set { } }
        public bool LoadUserProfile { get { return default(bool); } set { } }
        public bool RedirectStandardError { get { return default(bool); } set { } }
        public bool RedirectStandardInput { get { return default(bool); } set { } }
        public bool RedirectStandardOutput { get { return default(bool); } set { } }
        public System.Text.Encoding StandardErrorEncoding { get { return default(System.Text.Encoding); } set { } }
        public System.Text.Encoding StandardOutputEncoding { get { return default(System.Text.Encoding); } set { } }
        public string UserName { get { return default(string); } set { } }
        public bool UseShellExecute { get { return default(bool); } set { } }
        public string WorkingDirectory { get { return default(string); } set { } }
    }
    public partial class ProcessThread
    {
        internal ProcessThread() { }
        public int BasePriority { get { return default(int); } }
        public int CurrentPriority { get { return default(int); } }
        public int Id { get { return default(int); } }
        public int IdealProcessor { set { } }
        public bool PriorityBoostEnabled { get { return default(bool); } set { } }
        public System.Diagnostics.ThreadPriorityLevel PriorityLevel { get { return default(System.Diagnostics.ThreadPriorityLevel); } set { } }
        public System.TimeSpan PrivilegedProcessorTime { get { return default(System.TimeSpan); } }
        public System.IntPtr ProcessorAffinity { set { } }
        public System.IntPtr StartAddress { get { return default(System.IntPtr); } }
        public System.DateTime StartTime { get { return default(System.DateTime); } }
        public System.Diagnostics.ThreadState ThreadState { get { return default(System.Diagnostics.ThreadState); } }
        public System.TimeSpan TotalProcessorTime { get { return default(System.TimeSpan); } }
        public System.TimeSpan UserProcessorTime { get { return default(System.TimeSpan); } }
        public System.Diagnostics.ThreadWaitReason WaitReason { get { return default(System.Diagnostics.ThreadWaitReason); } }
        public void ResetIdealProcessor() { }
    }
    public partial class ProcessThreadCollection
    {
        protected ProcessThreadCollection() { }
        public ProcessThreadCollection(System.Diagnostics.ProcessThread[] processThreads) { }
        public System.Diagnostics.ProcessThread this[int index] { get { return default(System.Diagnostics.ProcessThread); } }
        public int Add(System.Diagnostics.ProcessThread thread) { return default(int); }
        public bool Contains(System.Diagnostics.ProcessThread thread) { return default(bool); }
        public void CopyTo(System.Diagnostics.ProcessThread[] array, int index) { }
        public int IndexOf(System.Diagnostics.ProcessThread thread) { return default(int); }
        public void Insert(int index, System.Diagnostics.ProcessThread thread) { }
        public void Remove(System.Diagnostics.ProcessThread thread) { }
    }
    public enum ThreadPriorityLevel
    {
        AboveNormal = 1,
        BelowNormal = -1,
        Highest = 2,
        Idle = -15,
        Lowest = -2,
        Normal = 0,
        TimeCritical = 15,
    }
    public enum ThreadState
    {
        Initialized = 0,
        Ready = 1,
        Running = 2,
        Standby = 3,
        Terminated = 4,
        Transition = 6,
        Unknown = 7,
        Wait = 5,
    }
    public enum ThreadWaitReason
    {
        EventPairHigh = 7,
        EventPairLow = 8,
        ExecutionDelay = 4,
        Executive = 0,
        FreePage = 1,
        LpcReceive = 9,
        LpcReply = 10,
        PageIn = 2,
        PageOut = 12,
        Suspended = 5,
        SystemAllocation = 3,
        Unknown = 13,
        UserRequest = 6,
        VirtualMemory = 11,
    }
}
