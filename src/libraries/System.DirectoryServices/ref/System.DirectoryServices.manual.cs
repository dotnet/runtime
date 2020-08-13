// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.DirectoryServices
{
    [System.ComponentModel.TypeConverter(typeof(DirectoryEntryConverter))]
    public partial class DirectoryEntry { }
    internal class DirectoryEntryConverter { }

    [System.ObsoleteAttribute("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public sealed partial class DirectoryServicesPermission : System.Security.Permissions.ResourcePermissionBase { }

    [System.ObsoleteAttribute("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public partial class DirectoryServicesPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute { }
}
