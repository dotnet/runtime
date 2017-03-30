// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    [Serializable]
    [Flags]
    public enum HostProtectionResource
    {
        All = 511,
        ExternalProcessMgmt = 4,
        ExternalThreading = 16,
        MayLeakOnAbort = 256,
        None = 0,
        SecurityInfrastructure = 64,
        SelfAffectingProcessMgmt = 8,
        SelfAffectingThreading = 32,
        SharedState = 2,
        Synchronization = 1,
        UI = 128,
    }
}
