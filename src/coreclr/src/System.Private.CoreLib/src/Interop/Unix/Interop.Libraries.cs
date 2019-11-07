// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static partial class Libraries
    {
        internal const string Kernel32 = RuntimeHelpers.QCall;
        internal const string User32   = RuntimeHelpers.QCall;
        internal const string Ole32    = RuntimeHelpers.QCall;
        internal const string OleAut32 = RuntimeHelpers.QCall;
        internal const string Advapi32 = RuntimeHelpers.QCall;
    }
}
