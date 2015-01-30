// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
namespace System.Runtime.InteropServices {
    using System;
    // Used in the StructLayoutAttribute class
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public enum LayoutKind
    {
        Sequential      = 0, // 0x00000008,
        Explicit        = 2, // 0x00000010,
        Auto            = 3, // 0x00000000,
    }
}
