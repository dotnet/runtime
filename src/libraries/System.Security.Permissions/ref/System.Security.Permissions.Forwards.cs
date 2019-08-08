// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.IPermission))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.ISecurityEncodable))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.SecurityElement))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.CodeAccessSecurityAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.SecurityAction))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.SecurityAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.SecurityPermissionAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.SecurityPermissionFlag))]
#if netcoreapp || uap
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.IStackWalk))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.PermissionSet))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Security.Permissions.PermissionState))]
#endif
