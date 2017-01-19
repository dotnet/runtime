// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Win32
{
#if REGISTRY_ASSEMBLY
    public
#else
    internal
#endif
    enum RegistryView
    {
        Default = 0, // 0x0000 operate on the default registry view
        Registry64 = Interop.Advapi32.RegistryView.KEY_WOW64_64KEY, // 0x0100 operate on the 64-bit registry view
        Registry32 = Interop.Advapi32.RegistryView.KEY_WOW64_32KEY, // 0x0200 operate on the 32-bit registry view
    };
}
