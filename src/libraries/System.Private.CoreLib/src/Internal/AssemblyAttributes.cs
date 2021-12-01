// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** This file exists to contain miscellaneous module-level attributes
** and other miscellaneous stuff.
**
**
**
===========================================================*/

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Resources;

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]

[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: AssemblyMetadata(".NETFrameworkAssembly", "")]
[assembly: AssemblyMetadata("IsTrimmable", "True")]

[assembly: NeutralResourcesLanguage("en-US")]
