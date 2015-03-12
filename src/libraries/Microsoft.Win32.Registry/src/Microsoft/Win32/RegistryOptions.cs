// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Win32
{
    [Flags]
    public enum RegistryOptions
    {
        None = Interop.mincore.RegistryOptions.REG_OPTION_NON_VOLATILE,       // 0x0000
        Volatile = Interop.mincore.RegistryOptions.REG_OPTION_VOLATILE,      // 0x0001
    };
}
