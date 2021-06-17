// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------
#pragma warning disable SYSLIB0003 // CAS Obsoletions

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.IPermission))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.ISecurityEncodable))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.SecurityElement))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.CodeAccessSecurityAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.SecurityAction))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.SecurityAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.SecurityPermissionAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.SecurityPermissionFlag))]
#if NET6_0_OR_GREATER
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Policy.Evidence))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Policy.EvidenceBase))]
#endif
#if NETCOREAPP
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.IStackWalk))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.PermissionSet))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.PermissionState))]
#endif
