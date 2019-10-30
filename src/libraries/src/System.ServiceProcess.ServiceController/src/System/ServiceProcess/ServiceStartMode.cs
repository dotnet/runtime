// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceProcess
{
    public enum ServiceStartMode
    {
        Manual = Interop.Advapi32.ServiceStartModes.START_TYPE_DEMAND,
        Automatic = Interop.Advapi32.ServiceStartModes.START_TYPE_AUTO,
        Disabled = Interop.Advapi32.ServiceStartModes.START_TYPE_DISABLED,
        Boot = Interop.Advapi32.ServiceStartModes.START_TYPE_BOOT,
        System = Interop.Advapi32.ServiceStartModes.START_TYPE_SYSTEM
    }
}
