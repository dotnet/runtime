// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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