// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Unicode, EntryPoint = "OutputDebugStringW", ExactSpelling = true)]
        internal static extern void OutputDebugString(string message);
    }
}
