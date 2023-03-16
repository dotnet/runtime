// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.DirectoryServices.Design;
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermission))]
[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermissionAccess))]
[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermissionAttribute))]
[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermissionEntry))]
[assembly: TypeForwardedTo(typeof(System.DirectoryServices.DirectoryServicesPermissionEntryCollection))]

namespace System.DirectoryServices
{
    [System.ComponentModel.TypeConverter(typeof(DirectoryEntryConverter))]
    public partial class DirectoryEntry { }
}

namespace System.DirectoryServices.Design
{
    internal sealed class DirectoryEntryConverter { }
}
