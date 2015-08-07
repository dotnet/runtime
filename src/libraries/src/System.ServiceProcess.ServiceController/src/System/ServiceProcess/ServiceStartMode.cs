// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ServiceProcess
{
    public enum ServiceStartMode
    {
        Manual = Interop.mincore.ServiceStartModes.START_TYPE_DEMAND,
        Automatic = Interop.mincore.ServiceStartModes.START_TYPE_AUTO,
        Disabled = Interop.mincore.ServiceStartModes.START_TYPE_DISABLED,
        Boot = Interop.mincore.ServiceStartModes.START_TYPE_BOOT,
        System = Interop.mincore.ServiceStartModes.START_TYPE_SYSTEM
    }
}
