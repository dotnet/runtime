// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeProcessHandle(System.IntPtr existingHandle, bool ownsHandle) : base(default(bool)) { }
        public override bool IsInvalid { [System.Security.SecurityCritical] get { throw null; } }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System.Diagnostics
{
    public partial class DataReceivedEventArgs : System.EventArgs
    {
        internal DataReceivedEventArgs() { }
        public string Data { get { throw null; } }
    }
    public delegate void DataReceivedEventHandler(object sender, System.Diagnostics.DataReceivedEventArgs e);
    public partial class Process : System.ComponentModel.Component
    {
        public IntPtr Handle { get { throw null; } }
        public int HandleCount { get { throw null; } }
        public IntPtr MainWindowHandle { get { throw null; } }
        public string MainWindowTitle { get { throw null; } }
        public int NonpagedSystemMemorySize { get { throw null; } }
        public int PagedMemorySize { get { throw null; } }
        public int PagedSystemMemorySize { get { throw null; } }
        public int PeakPagedMemorySize { get { throw null; } }
        public int PeakVirtualMemorySize { get { throw null; } }
        public int PeakWorkingSet { get { throw null; } }
        public int PrivateMemorySize { get { throw null; } }
        public bool Responding { get { throw null; } }
        public System.ComponentModel.ISynchronizeInvoke SynchronizingObject { get { throw null; } set { } }
        public int VirtualMemorySize { get { throw null; } }
        public int WorkingSet { get { throw null; } }
        public void Close() { }
        public bool CloseMainWindow() { throw null; }
        protected override void Dispose(bool disposing) { }
        public bool WaitForInputIdle() { throw null; }
        public bool WaitForInputIdle(int milliseconds) { throw null; }
        public override string ToString() { throw null; }

        public Process() { }
        public int BasePriority { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool EnableRaisingEvents { get { throw null; } set { } }
        public int ExitCode { get { throw null; } }
        public System.DateTime ExitTime { get { throw null; } }
        public bool HasExited { get { throw null; } }
        public int Id { get { throw null; } }
        public string MachineName { get { throw null; } }
        public System.Diagnostics.ProcessModule MainModule { get { throw null; } }
        public System.IntPtr MaxWorkingSet { get { throw null; } set { } }
        public System.IntPtr MinWorkingSet { get { throw null; } set { } }
        public System.Diagnostics.ProcessModuleCollection Modules { get { throw null; } }
        public long NonpagedSystemMemorySize64 { get { throw null; } }
        public long PagedMemorySize64 { get { throw null; } }
        public long PagedSystemMemorySize64 { get { throw null; } }
        public long PeakPagedMemorySize64 { get { throw null; } }
        public long PeakVirtualMemorySize64 { get { throw null; } }
        public long PeakWorkingSet64 { get { throw null; } }
        public bool PriorityBoostEnabled { get { throw null; } set { } }
        public System.Diagnostics.ProcessPriorityClass PriorityClass { get { throw null; } set { } }
        public long PrivateMemorySize64 { get { throw null; } }
        public System.TimeSpan PrivilegedProcessorTime { get { throw null; } }
        public string ProcessName { get { throw null; } }
        public System.IntPtr ProcessorAffinity { get { throw null; } set { } }
        public Microsoft.Win32.SafeHandles.SafeProcessHandle SafeHandle { get { throw null; } }
        public int SessionId { get { throw null; } }
        public System.IO.StreamReader StandardError { get { throw null; } }
        public System.IO.StreamWriter StandardInput { get { throw null; } }
        public System.IO.StreamReader StandardOutput { get { throw null; } }
        public System.Diagnostics.ProcessStartInfo StartInfo { get { throw null; } set { } }
        public System.DateTime StartTime { get { throw null; } }
        public System.Diagnostics.ProcessThreadCollection Threads { get { throw null; } }
        public System.TimeSpan TotalProcessorTime { get { throw null; } }
        public System.TimeSpan UserProcessorTime { get { throw null; } }
        public long VirtualMemorySize64 { get { throw null; } }
        public long WorkingSet64 { get { throw null; } }
        public event System.Diagnostics.DataReceivedEventHandler ErrorDataReceived { add { } remove { } }
        public event System.EventHandler Exited { add { } remove { } }
        public event System.Diagnostics.DataReceivedEventHandler OutputDataReceived { add { } remove { } }
        public void BeginErrorReadLine() { }
        public void BeginOutputReadLine() { }
        public void CancelErrorRead() { }
        public void CancelOutputRead() { }
        public static void EnterDebugMode() { }
        public static System.Diagnostics.Process GetCurrentProcess() { throw null; }
        public static System.Diagnostics.Process GetProcessById(int processId) { throw null; }
        public static System.Diagnostics.Process GetProcessById(int processId, string machineName) { throw null; }
        public static System.Diagnostics.Process[] GetProcesses() { throw null; }
        public static System.Diagnostics.Process[] GetProcesses(string machineName) { throw null; }
        public static System.Diagnostics.Process[] GetProcessesByName(string processName) { throw null; }
        public static System.Diagnostics.Process[] GetProcessesByName(string processName, string machineName) { throw null; }
        public void Kill() { }
        public static void LeaveDebugMode() { }
        protected void OnExited() { }
        public void Refresh() { }
        public bool Start() { throw null; }
        public static System.Diagnostics.Process Start(System.Diagnostics.ProcessStartInfo startInfo) { throw null; }
        public static System.Diagnostics.Process Start(string fileName) { throw null; }
        public static System.Diagnostics.Process Start(string fileName, string arguments) { throw null; }
        public void WaitForExit() { }
        public bool WaitForExit(int milliseconds) { throw null; }
    }
    public partial class ProcessModule : System.ComponentModel.Component
    {
        internal ProcessModule() { }
        public System.IntPtr BaseAddress { get { throw null; } }
        public System.IntPtr EntryPointAddress { get { throw null; } }
        public string FileName { get { throw null; } }
        public int ModuleMemorySize { get { throw null; } }
        public string ModuleName { get { throw null; } }
        public FileVersionInfo FileVersionInfo { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public partial class ProcessModuleCollection : System.Collections.ReadOnlyCollectionBase
    {
        protected ProcessModuleCollection() { }
        public ProcessModuleCollection(System.Diagnostics.ProcessModule[] processModules) { }
        public System.Diagnostics.ProcessModule this[int index] { get { throw null; } }
        public bool Contains(System.Diagnostics.ProcessModule module) { throw null; }
        public void CopyTo(System.Diagnostics.ProcessModule[] array, int index) { }
        public int IndexOf(System.Diagnostics.ProcessModule module) { throw null; }
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
        public string Arguments { get { throw null; } set { } }
        public bool CreateNoWindow { get { throw null; } set { } }
        public string Domain { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Collections.Generic.IDictionary<string, string> Environment { get { throw null; } }
        public string FileName { get { throw null; } set { } }
        public bool LoadUserProfile { get { throw null; } set { } }
        public bool RedirectStandardError { get { throw null; } set { } }
        public bool RedirectStandardInput { get { throw null; } set { } }
        public bool RedirectStandardOutput { get { throw null; } set { } }
        public System.Text.Encoding StandardErrorEncoding { get { throw null; } set { } }
        public System.Text.Encoding StandardOutputEncoding { get { throw null; } set { } }
        public string UserName { get { throw null; } set { } }
        public bool UseShellExecute { get { throw null; } set { } }
        public string WorkingDirectory { get { throw null; } set { } }
    }
    public partial class ProcessThread : System.ComponentModel.Component
    {
        internal ProcessThread() { }
        public int BasePriority { get { throw null; } }
        public int CurrentPriority { get { throw null; } }
        public int Id { get { throw null; } }
        public int IdealProcessor { set { } }
        public bool PriorityBoostEnabled { get { throw null; } set { } }
        public System.Diagnostics.ThreadPriorityLevel PriorityLevel { get { throw null; } set { } }
        public System.TimeSpan PrivilegedProcessorTime { get { throw null; } }
        public System.IntPtr ProcessorAffinity { set { } }
        public System.IntPtr StartAddress { get { throw null; } }
        public System.DateTime StartTime { get { throw null; } }
        public System.Diagnostics.ThreadState ThreadState { get { throw null; } }
        public System.TimeSpan TotalProcessorTime { get { throw null; } }
        public System.TimeSpan UserProcessorTime { get { throw null; } }
        public System.Diagnostics.ThreadWaitReason WaitReason { get { throw null; } }
        public void ResetIdealProcessor() { }
    }
    public partial class ProcessThreadCollection : System.Collections.ReadOnlyCollectionBase
    {
        protected ProcessThreadCollection() { }
        public ProcessThreadCollection(System.Diagnostics.ProcessThread[] processThreads) { }
        public System.Diagnostics.ProcessThread this[int index] { get { throw null; } }
        public int Add(System.Diagnostics.ProcessThread thread) { throw null; }
        public bool Contains(System.Diagnostics.ProcessThread thread) { throw null; }
        public void CopyTo(System.Diagnostics.ProcessThread[] array, int index) { }
        public int IndexOf(System.Diagnostics.ProcessThread thread) { throw null; }
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

    public enum ProcessWindowStyle 
    {
        Hidden = 1,
        Maximized = 3,
        Minimized = 2,
        Normal = 0,
    }

    public class MonitoringDescriptionAttribute : System.ComponentModel.DescriptionAttribute 
    {
        public MonitoringDescriptionAttribute(string description) { throw null; }
        public override string Description { get { throw null; } }
    }
}
