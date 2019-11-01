// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System
{
    internal static class InternalHelpers
    {
        internal static void SetErrorCode(this Exception ex, int code)
        {
            ex.HResult = code;
        }
    }
}
