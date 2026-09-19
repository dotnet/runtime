// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

if (!RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
{
    GetEUid();
}

Console.WriteLine("guarded P/Invoke was not called");
return 42;

[DllImport("libSystem.Native", EntryPoint = "SystemNative_GetEUid")]
static extern uint GetEUid();
