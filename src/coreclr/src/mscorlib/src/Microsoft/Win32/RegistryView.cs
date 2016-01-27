// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Implements Microsoft.Win32.RegistryView
//
// ======================================================================================
namespace Microsoft.Win32 {
    using System;
    
    public enum RegistryView {
        Default      = 0,                           // 0x0000 operate on the default registry view
        Registry64   = Win32Native.KEY_WOW64_64KEY, // 0x0100 operate on the 64-bit registry view
        Registry32   = Win32Native.KEY_WOW64_32KEY, // 0x0200 operate on the 32-bit registry view
    };
}
