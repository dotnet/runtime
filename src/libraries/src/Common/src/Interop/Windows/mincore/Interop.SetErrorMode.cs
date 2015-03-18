// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.ErrorHandling, SetLastError = false, EntryPoint = "SetErrorMode", ExactSpelling = true)]
        internal static extern uint SetErrorMode(uint newMode);
    }
}
