// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.ServiceProcess
{
    public partial class ServiceController : System.IDisposable
    {
        public ServiceController(string name) { }
        public ServiceController(string name, string machineName) { }
        public bool CanPauseAndContinue { get { return default(bool); } }
        public bool CanShutdown { get { return default(bool); } }
        public bool CanStop { get { return default(bool); } }
        public System.ServiceProcess.ServiceController[] DependentServices { get { return default(System.ServiceProcess.ServiceController[]); } }
        public string DisplayName { get { return default(string); } }
        public string MachineName { get { return default(string); } }
        public System.Runtime.InteropServices.SafeHandle ServiceHandle { get { return default(System.Runtime.InteropServices.SafeHandle); } }
        public string ServiceName { get { return default(string); } }
        public System.ServiceProcess.ServiceController[] ServicesDependedOn { get { return default(System.ServiceProcess.ServiceController[]); } }
        public System.ServiceProcess.ServiceType ServiceType { get { return default(System.ServiceProcess.ServiceType); } }
        public System.ServiceProcess.ServiceControllerStatus Status { get { return default(System.ServiceProcess.ServiceControllerStatus); } }
        public void Continue() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public static System.ServiceProcess.ServiceController[] GetDevices() { return default(System.ServiceProcess.ServiceController[]); }
        public static System.ServiceProcess.ServiceController[] GetDevices(string machineName) { return default(System.ServiceProcess.ServiceController[]); }
        public static System.ServiceProcess.ServiceController[] GetServices() { return default(System.ServiceProcess.ServiceController[]); }
        public static System.ServiceProcess.ServiceController[] GetServices(string machineName) { return default(System.ServiceProcess.ServiceController[]); }
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
        Disabled = 4,
        Manual = 3,
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
    public partial class TimeoutException : System.Exception
    {
        public TimeoutException() { }
        public TimeoutException(string message) { }
        public TimeoutException(string message, System.Exception innerException) { }
    }
}
