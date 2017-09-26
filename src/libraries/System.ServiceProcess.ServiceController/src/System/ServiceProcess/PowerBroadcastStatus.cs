// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceProcess
{
    public enum PowerBroadcastStatus
    {
        BatteryLow          = Interop.Advapi32.PowerBroadcastStatus.PBT_APMBATTERYLOW,
        OemEvent            = Interop.Advapi32.PowerBroadcastStatus.PBT_APMOEMEVENT,
        PowerStatusChange   = Interop.Advapi32.PowerBroadcastStatus.PBT_APMPOWERSTATUSCHANGE,
        QuerySuspend        = Interop.Advapi32.PowerBroadcastStatus.PBT_APMQUERYSUSPEND,
        QuerySuspendFailed  = Interop.Advapi32.PowerBroadcastStatus.PBT_APMQUERYSUSPENDFAILED,
        ResumeAutomatic     = Interop.Advapi32.PowerBroadcastStatus.PBT_APMRESUMEAUTOMATIC,
        ResumeCritical      = Interop.Advapi32.PowerBroadcastStatus.PBT_APMRESUMECRITICAL,
        ResumeSuspend       = Interop.Advapi32.PowerBroadcastStatus.PBT_APMRESUMESUSPEND,
        Suspend             = Interop.Advapi32.PowerBroadcastStatus.PBT_APMSUSPEND,
    }
}

