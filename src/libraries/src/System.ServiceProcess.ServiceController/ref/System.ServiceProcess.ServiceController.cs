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
        public bool CanPauseAndContinue { get { throw null; } }
        public bool CanShutdown { get { throw null; } }
        public bool CanStop { get { throw null; } }
        public System.ServiceProcess.ServiceController[] DependentServices { get { throw null; } }
        public string DisplayName { get { throw null; } }
        public string MachineName { get { throw null; } }
        public System.Runtime.InteropServices.SafeHandle ServiceHandle { get { throw null; } }
        public string ServiceName { get { throw null; } }
        public System.ServiceProcess.ServiceController[] ServicesDependedOn { get { throw null; } }
        public System.ServiceProcess.ServiceType ServiceType { get { throw null; } }
        public System.ServiceProcess.ServiceStartMode StartType { get { throw null; } }
        public System.ServiceProcess.ServiceControllerStatus Status { get { throw null; } }
        public void Continue() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
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
    public partial class TimeoutException : System.Exception
    {
        public TimeoutException() { }
        public TimeoutException(string message) { }
        public TimeoutException(string message, System.Exception innerException) { }
        protected TimeoutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
