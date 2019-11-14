// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [Flags]
    public enum RegistrationClassContext
    {
        InProcessServer = 0x1,
        InProcessHandler = 0x2,
        LocalServer = 0x4,
        InProcessServer16 = 0x8,
        RemoteServer = 0x10,
        InProcessHandler16 = 0x20,
        Reserved1 = 0x40,
        Reserved2 = 0x80,
        Reserved3 = 0x100,
        Reserved4 = 0x200,
        NoCodeDownload = 0x400,
        Reserved5 = 0x800,
        NoCustomMarshal = 0x1000,
        EnableCodeDownload = 0x2000,
        NoFailureLog = 0x4000,
        DisableActivateAsActivator = 0x8000,
        EnableActivateAsActivator = 0x10000,
        FromDefaultContext = 0x20000
    }
}
