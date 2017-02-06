// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Stubbed out types to be cleanup from CoreLib
//

namespace System.Security
{
    internal enum SecurityContextSource
    {
        CurrentAppDomain = 0,
        CurrentAssembly
    }

    internal sealed class PermissionSet
    {
    }
}

namespace System.Security.Permissions
{
    internal class HostProtectionAttribute : Attribute
    {
        public bool MayLeakOnAbort { get; set; }
    }

    internal enum PermissionState
    {
        Unrestricted = 1,
        None = 0,
    }
}

namespace System.Security.Policy
{
    internal sealed class Evidence
    {
    }

    internal sealed class ApplicationTrust
    {
    }
}

namespace System.Security.Principal
{
    internal interface IPrincipal
    {
    }
}
