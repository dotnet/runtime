// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceProcess
{
    [Flags]
    public enum ServiceType
    {
        Adapter = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_ADAPTER,
        FileSystemDriver = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_FILE_SYSTEM_DRIVER,
        InteractiveProcess = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_INTERACTIVE_PROCESS,
        KernelDriver = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_KERNEL_DRIVER,
        RecognizerDriver = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_RECOGNIZER_DRIVER,
        Win32OwnProcess = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_WIN32_OWN_PROCESS,
        Win32ShareProcess = Interop.Advapi32.ServiceTypeOptions.SERVICE_TYPE_WIN32_SHARE_PROCESS
    }
}
