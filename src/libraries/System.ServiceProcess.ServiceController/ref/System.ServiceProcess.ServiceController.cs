// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.ServiceProcess
{
    public enum PowerBroadcastStatus
    {
        BatteryLow = 9,
        OemEvent = 11,
        PowerStatusChange = 10,
        QuerySuspend = 0,
        QuerySuspendFailed = 2,
        ResumeAutomatic = 18,
        ResumeCritical = 6,
        ResumeSuspend = 7,
        Suspend = 4,
    }
    public partial class ServiceBase : System.ComponentModel.Component
    {
        public const int MaxNameLength = 80;
        public ServiceBase() { }
        public bool AutoLog { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool CanHandlePowerEvent { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool CanHandleSessionChangeEvent { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool CanPauseAndContinue { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool CanShutdown { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool CanStop { get { throw null; } set { } }
        public virtual System.Diagnostics.EventLog EventLog { get { throw null; } }
        public int ExitCode { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        protected System.IntPtr ServiceHandle { get { throw null; } }
        public string ServiceName { get { throw null; } set { } }
        protected override void Dispose(bool disposing) { }
        protected virtual void OnContinue() { }
        protected virtual void OnCustomCommand(int command) { }
        protected virtual void OnPause() { }
        protected virtual bool OnPowerEvent(System.ServiceProcess.PowerBroadcastStatus powerStatus) { throw null; }
        protected virtual void OnSessionChange(System.ServiceProcess.SessionChangeDescription changeDescription) { }
        protected virtual void OnShutdown() { }
        protected virtual void OnStart(string[] args) { }
        protected virtual void OnStop() { }
        public void RequestAdditionalTime(int milliseconds) { }
        public static void Run(System.ServiceProcess.ServiceBase service) { }
        public static void Run(System.ServiceProcess.ServiceBase[] services) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public void ServiceMainCallback(int argCount, System.IntPtr argPointer) { }
        public void Stop() { }
    }
    public partial class ServiceController : System.ComponentModel.Component
    {
        public ServiceController() { }
        public ServiceController(string name) { }
        public ServiceController(string name, string machineName) { }
        public bool CanPauseAndContinue { get { throw null; } }
        public bool CanShutdown { get { throw null; } }
        public bool CanStop { get { throw null; } }
        public System.ServiceProcess.ServiceController[] DependentServices { get { throw null; } }
        public string DisplayName { get { throw null; } set { } }
        public string MachineName { get { throw null; } set { } }
        public System.Runtime.InteropServices.SafeHandle ServiceHandle { get { throw null; } }
        public string ServiceName { get { throw null; } set { } }
        public System.ServiceProcess.ServiceController[] ServicesDependedOn { get { throw null; } }
        public System.ServiceProcess.ServiceType ServiceType { get { throw null; } }
        public System.ServiceProcess.ServiceStartMode StartType { get { throw null; } }
        public System.ServiceProcess.ServiceControllerStatus Status { get { throw null; } }
        public void Close() { }
        public void Continue() { }
        protected override void Dispose(bool disposing) { }
        public void ExecuteCommand(int command) { }
        public static System.ServiceProcess.ServiceController[] GetDevices() { throw null; }
        public static System.ServiceProcess.ServiceController[] GetDevices(string machineName) { throw null; }
        public static System.ServiceProcess.ServiceController[] GetServices() { throw null; }
        public static System.ServiceProcess.ServiceController[] GetServices(string machineName) { throw null; }
        public void Pause() { }
        public void Refresh() { }
        public void Start() { }
        public void Start(string[] args) { }
        public void Stop() { }
        public void WaitForStatus(System.ServiceProcess.ServiceControllerStatus desiredStatus) { }
        public void WaitForStatus(System.ServiceProcess.ServiceControllerStatus desiredStatus, System.TimeSpan timeout) { }
    }
    public enum ServiceControllerStatus
    {
        ContinuePending = 5,
        Paused = 7,
        PausePending = 6,
        Running = 4,
        StartPending = 2,
        Stopped = 1,
        StopPending = 3,
    }
    public enum ServiceStartMode
    {
        Automatic = 2,
        Boot = 0,
        Disabled = 4,
        Manual = 3,
        System = 1,
    }
    [System.FlagsAttribute]
    public enum ServiceType
    {
        Adapter = 4,
        FileSystemDriver = 2,
        InteractiveProcess = 256,
        KernelDriver = 1,
        RecognizerDriver = 8,
        Win32OwnProcess = 16,
        Win32ShareProcess = 32,
    }
    public readonly partial struct SessionChangeDescription
    {
        private readonly int _dummy;
        public System.ServiceProcess.SessionChangeReason Reason { get { throw null; } }
        public int SessionId { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ServiceProcess.SessionChangeDescription changeDescription) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ServiceProcess.SessionChangeDescription a, System.ServiceProcess.SessionChangeDescription b) { throw null; }
        public static bool operator !=(System.ServiceProcess.SessionChangeDescription a, System.ServiceProcess.SessionChangeDescription b) { throw null; }
    }
    public enum SessionChangeReason
    {
        ConsoleConnect = 1,
        ConsoleDisconnect = 2,
        RemoteConnect = 3,
        RemoteDisconnect = 4,
        SessionLock = 7,
        SessionLogoff = 6,
        SessionLogon = 5,
        SessionRemoteControl = 9,
        SessionUnlock = 8,
    }
    public partial class TimeoutException : System.SystemException
    {
        public TimeoutException() { }
        protected TimeoutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TimeoutException(string message) { }
        public TimeoutException(string message, System.Exception innerException) { }
    }
}
