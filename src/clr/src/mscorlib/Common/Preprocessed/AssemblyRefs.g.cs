// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// #line 1 "d:\\projectk_2\\src\\ndp\\clr\\src\\bcl.oss\\open\\src\\common\\assemblyrefs.cspp"

/*
 * Assembly attributes. This file is preprocessed to generate a .cs file
 * with the correct information.  The original lives in VBL\Tools\DevDiv\
 */

using System;
using System.Reflection;
using System.Resources;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal static class FXAssembly
{
    internal const string Version = "4.0.0.0";
}

internal static class ThisAssembly
{
    internal const string Version = "4.0.0.0";
    internal const int DailyBuildNumber = 22306;
}

internal static class AssemblyRef
{
    internal const string EcmaPublicKey = "b77a5c561934e089";
    internal const string EcmaPublicKeyToken = "b77a5c561934e089";
    internal const string MicrosoftPublicKeyToken = "b03f5f7f11d50a3a";
    internal const string SystemRuntimeWindowsRuntime = "System.Runtime.WindowsRuntime, Version=" + FXAssembly.Version + ", Culture=neutral, PublicKeyToken=" + EcmaPublicKey;
}
