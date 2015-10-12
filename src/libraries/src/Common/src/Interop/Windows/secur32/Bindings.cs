// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
