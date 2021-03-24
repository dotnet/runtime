// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeProcessHandle() : base (default(bool)) { }
        public SafeProcessHandle(System.IntPtr existingHandle, bool ownsHandle) : base (default(bool)) { }
        protected override bool ReleaseHandle() { throw null; }
    }
}
namespace System.Diagnostics
{
    public partial class DataReceivedEventArgs : System.EventArgs
    {
        internal DataReceivedEventArgs() { }
        public string? Data { get { throw null; } }
    }
    public delegate void DataReceivedEventHandler(object sender, System.Diagnostics.DataReceivedEventArgs e);
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public partial class MonitoringDescriptionAttribute : System.ComponentModel.DescriptionAttribute
    {
        public MonitoringDescriptionAttribute(string description) { }
        public override string Description { get { throw null; } }
    }
    [System.ComponentModel.DesignerAttribute("System.Diagnostics.Design.ProcessDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class Process : System.ComponentModel.Component, System.IDisposable
    {
        public Process() { }
        public int BasePriority { get { throw null; } }
        public bool EnableRaisingEvents { get { throw null; } set { } }
        public int ExitCode { get { throw null; } }
        public System.DateTime ExitTime { get { throw null; } }
        public System.IntPtr Handle { get { throw null; } }
        public int HandleCount { get { throw null; } }
        public bool HasExited { get { throw null; } }
        public int Id { get { throw null; } }
        public string MachineName { get { throw null; } }
        public System.Diagnostics.ProcessModule? MainModule { get { throw null; } }
        public System.IntPtr MainWindowHandle { get { throw null; } }
        public string MainWindowTitle { get { throw null; } }
        public System.IntPtr MaxWorkingSet { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] [System.Runtime.Versioning.SupportedOSPlatformAttribute("macos")] [System.Runtime.Versioning.SupportedOSPlatformAttribute("freebsd")] set { } }
        public System.IntPtr MinWorkingSet { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] [System.Runtime.Versioning.SupportedOSPlatformAttribute("macos")] [System.Runtime.Versioning.SupportedOSPlatformAttribute("freebsd")] set { } }
        public System.Diagnostics.ProcessModuleCollection Modules { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.NonpagedSystemMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int NonpagedSystemMemorySize { get { throw null; } }
        public long NonpagedSystemMemorySize64 { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PagedMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PagedMemorySize { get { throw null; } }
        public long PagedMemorySize64 { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PagedSystemMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PagedSystemMemorySize { get { throw null; } }
        public long PagedSystemMemorySize64 { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PeakPagedMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PeakPagedMemorySize { get { throw null; } }
        public long PeakPagedMemorySize64 { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PeakVirtualMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PeakVirtualMemorySize { get { throw null; } }
        public long PeakVirtualMemorySize64 { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PeakWorkingSet64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PeakWorkingSet { get { throw null; } }
        public long PeakWorkingSet64 { get { throw null; } }
        public bool PriorityBoostEnabled { get { throw null; } set { } }
        public System.Diagnostics.ProcessPriorityClass PriorityClass { get { throw null; } set { } }
        [System.ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.PrivateMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int PrivateMemorySize { get { throw null; } }
        public long PrivateMemorySize64 { get { throw null; } }
        public System.TimeSpan PrivilegedProcessorTime { get { throw null; } }
        public string ProcessName { get { throw null; } }
        public System.IntPtr ProcessorAffinity { get { throw null; } set { } }
        public bool Responding { get { throw null; } }
        public Microsoft.Win32.SafeHandles.SafeProcessHandle SafeHandle { get { throw null; } }
        public int SessionId { get { throw null; } }
        public System.IO.StreamReader StandardError { get { throw null; } }
        public System.IO.StreamWriter StandardInput { get { throw null; } }
        public System.IO.StreamReader StandardOutput { get { throw null; } }
        public System.Diagnostics.ProcessStartInfo StartInfo { get { throw null; } set { } }
        public System.DateTime StartTime { get { throw null; } }
        public System.ComponentModel.ISynchronizeInvoke? SynchronizingObject { get { throw null; } set { } }
        public System.Diagnostics.ProcessThreadCollection Threads { get { throw null; } }
        public System.TimeSpan TotalProcessorTime { get { throw null; } }
        public System.TimeSpan UserProcessorTime { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.VirtualMemorySize64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int VirtualMemorySize { get { throw null; } }
        public long VirtualMemorySize64 { get { throw null; } }
        [System.ObsoleteAttribute("This property has been deprecated because the type of the property can't represent all valid results. Please use System.Diagnostics.Process.WorkingSet64 instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int WorkingSet { get { throw null; } }
        public long WorkingSet64 { get { throw null; } }
        public event System.Diagnostics.DataReceivedEventHandler? ErrorDataReceived { add { } remove { } }
        public event System.EventHandler Exited { add { } remove { } }
        public event System.Diagnostics.DataReceivedEventHandler? OutputDataReceived { add { } remove { } }
        public void BeginErrorReadLine() { }
        public void BeginOutputReadLine() { }
        public void CancelErrorRead() { }
        public void CancelOutputRead() { }
        public void Close() { }
        public bool CloseMainWindow() { throw null; }
        protected override void Dispose(bool disposing) { }
        public static void EnterDebugMode() { }
        public static System.Diagnostics.Process GetCurrentProcess() { throw null; }
        public static System.Diagnostics.Process GetProcessById(int processId) { throw null; }
        public static System.Diagnostics.Process GetProcessById(int processId, string machineName) { throw null; }
        public static System.Diagnostics.Process[] GetProcesses() { throw null; }
        public static System.Diagnostics.Process[] GetProcesses(string machineName) { throw null; }
        public static System.Diagnostics.Process[] GetProcessesByName(string? processName) { throw null; }
        public static System.Diagnostics.Process[] GetProcessesByName(string? processName, string machineName) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public void Kill() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public void Kill(bool entireProcessTree) { }
        public static void LeaveDebugMode() { }
        protected void OnExited() { }
        public void Refresh() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public bool Start() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Diagnostics.Process? Start(System.Diagnostics.ProcessStartInfo startInfo) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Diagnostics.Process Start(string fileName) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Diagnostics.Process Start(string fileName, string arguments) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Diagnostics.Process Start(string fileName, System.Collections.Generic.IEnumerable<string> arguments) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Diagnostics.Process? Start(string fileName, string userName, System.Security.SecureString password, string domain) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Diagnostics.Process? Start(string fileName, string arguments, string userName, System.Security.SecureString password, string domain) { throw null; }
        public override string ToString() { throw null; }
        public void WaitForExit() { }
        public bool WaitForExit(int milliseconds) { throw null; }
        public System.Threading.Tasks.Task WaitForExitAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public bool WaitForInputIdle() { throw null; }
        public bool WaitForInputIdle(int milliseconds) { throw null; }
    }
    [System.ComponentModel.DesignerAttribute("System.Diagnostics.Design.ProcessModuleDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class ProcessModule : System.ComponentModel.Component
    {
        internal ProcessModule() { }
        public System.IntPtr BaseAddress { get { throw null; } }
        public System.IntPtr EntryPointAddress { get { throw null; } }
        public string? FileName { get { throw null; } }
        public System.Diagnostics.FileVersionInfo FileVersionInfo { get { throw null; } }
        public int ModuleMemorySize { get { throw null; } }
        public string? ModuleName { get { throw null; } }
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
        Normal = 32,
        Idle = 64,
        High = 128,
        RealTime = 256,
        BelowNormal = 16384,
        AboveNormal = 32768,
    }
    public sealed partial class ProcessStartInfo
    {
        public ProcessStartInfo() { }
        public ProcessStartInfo(string fileName) { }
        public ProcessStartInfo(string fileName, string arguments) { }
        public System.Collections.ObjectModel.Collection<string> ArgumentList { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Arguments { get { throw null; } set { } }
        public bool CreateNoWindow { get { throw null; } set { } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Domain { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, string?> Environment { get { throw null; } }
        [System.ComponentModel.EditorAttribute("System.Diagnostics.Design.StringDictionaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public System.Collections.Specialized.StringDictionary EnvironmentVariables { get { throw null; } }
        public bool ErrorDialog { get { throw null; } set { } }
        public System.IntPtr ErrorDialogParentHandle { get { throw null; } set { } }
        [System.ComponentModel.EditorAttribute("System.Diagnostics.Design.StartFileNameEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string FileName { get { throw null; } set { } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public bool LoadUserProfile { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public System.Security.SecureString? Password { get { throw null; } set { } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public string? PasswordInClearText { get { throw null; } set { } }
        public bool RedirectStandardError { get { throw null; } set { } }
        public bool RedirectStandardInput { get { throw null; } set { } }
        public bool RedirectStandardOutput { get { throw null; } set { } }
        public System.Text.Encoding? StandardErrorEncoding { get { throw null; } set { } }
        public System.Text.Encoding? StandardInputEncoding { get { throw null; } set { } }
        public System.Text.Encoding? StandardOutputEncoding { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string UserName { get { throw null; } set { } }
        public bool UseShellExecute { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Verb { get { throw null; } set { } }
        public string[] Verbs { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(System.Diagnostics.ProcessWindowStyle.Normal)]
        public System.Diagnostics.ProcessWindowStyle WindowStyle { get { throw null; } set { } }
        [System.ComponentModel.EditorAttribute("System.Diagnostics.Design.WorkingDirectoryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string WorkingDirectory { get { throw null; } set { } }
    }
    [System.ComponentModel.DesignerAttribute("System.Diagnostics.Design.ProcessThreadDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class ProcessThread : System.ComponentModel.Component
    {
        internal ProcessThread() { }
        public int BasePriority { get { throw null; } }
        public int CurrentPriority { get { throw null; } }
        public int Id { get { throw null; } }
        public int IdealProcessor { set { } }
        public bool PriorityBoostEnabled { get { throw null; } set { } }
        public System.Diagnostics.ThreadPriorityLevel PriorityLevel { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public System.TimeSpan PrivilegedProcessorTime { get { throw null; } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
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
    public enum ProcessWindowStyle
    {
        Normal = 0,
        Hidden = 1,
        Minimized = 2,
        Maximized = 3,
    }
    public enum ThreadPriorityLevel
    {
        Idle = -15,
        Lowest = -2,
        BelowNormal = -1,
        Normal = 0,
        AboveNormal = 1,
        Highest = 2,
        TimeCritical = 15,
    }
    public enum ThreadState
    {
        Initialized = 0,
        Ready = 1,
        Running = 2,
        Standby = 3,
        Terminated = 4,
        Wait = 5,
        Transition = 6,
        Unknown = 7,
    }
    public enum ThreadWaitReason
    {
        Executive = 0,
        FreePage = 1,
        PageIn = 2,
        SystemAllocation = 3,
        ExecutionDelay = 4,
        Suspended = 5,
        UserRequest = 6,
        EventPairHigh = 7,
        EventPairLow = 8,
        LpcReceive = 9,
        LpcReply = 10,
        VirtualMemory = 11,
        PageOut = 12,
        Unknown = 13,
    }
}
