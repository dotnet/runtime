// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Net
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Bindings
    {
        // SecPkgContext_Bindings in sspi.h.
        internal int BindingsLength;
        internal IntPtr pBindings;
    }
}
