// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ServiceProcess
{
    [Flags]
    public enum ServiceType
    {
        // Does not currently represent all of the Win32 values; see Interop.Advapi32.ServiceTypeOptions
        Adapter = Interop.Advapi32.ServiceTypeOptions.SERVICE_ADAPTER,
        FileSystemDriver = Interop.Advapi32.ServiceTypeOptions.SERVICE_FILE_SYSTEM_DRIVER,
        InteractiveProcess = Interop.Advapi32.ServiceTypeOptions.SERVICE_INTERACTIVE_PROCESS,
        KernelDriver = Interop.Advapi32.ServiceTypeOptions.SERVICE_KERNEL_DRIVER,
        RecognizerDriver = Interop.Advapi32.ServiceTypeOptions.SERVICE_RECOGNIZER_DRIVER,
        Win32OwnProcess = Interop.Advapi32.ServiceTypeOptions.SERVICE_WIN32_OWN_PROCESS,
        Win32ShareProcess = Interop.Advapi32.ServiceTypeOptions.SERVICE_WIN32_SHARE_PROCESS
    }
}
