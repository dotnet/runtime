// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ServiceProcess
{
    [Flags]
    public enum ServiceType
    {
        Adapter = Interop.mincore.ServiceTypeOptions.SERVICE_TYPE_ADAPTER,
        FileSystemDriver = Interop.mincore.ServiceTypeOptions.SERVICE_TYPE_FILE_SYSTEM_DRIVER,
        InteractiveProcess = Interop.mincore.ServiceTypeOptions.SERVICE_TYPE_INTERACTIVE_PROCESS,
        KernelDriver = Interop.mincore.ServiceTypeOptions.SERVICE_TYPE_KERNEL_DRIVER,
        RecognizerDriver = Interop.mincore.ServiceTypeOptions.SERVICE_TYPE_RECOGNIZER_DRIVER,
        Win32OwnProcess = Interop.mincore.ServiceTypeOptions.SERVICE_TYPE_WIN32_OWN_PROCESS,
        Win32ShareProcess = Interop.mincore.ServiceTypeOptions.SERVICE_TYPE_WIN32_SHARE_PROCESS
    }
}
