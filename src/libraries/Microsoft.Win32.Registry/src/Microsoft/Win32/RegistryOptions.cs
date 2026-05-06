// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Win32
{
    [Flags]
    public enum RegistryOptions
    {
        None = Interop.Advapi32.RegistryOptions.REG_OPTION_NON_VOLATILE,       // 0x0000
        Volatile = Interop.Advapi32.RegistryOptions.REG_OPTION_VOLATILE,      // 0x0001
    }
}
