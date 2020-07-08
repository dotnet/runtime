// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Error_L1, PreserveSig = false)]
        internal static extern IRestrictedErrorInfo GetRestrictedErrorInfo();
    }
}
