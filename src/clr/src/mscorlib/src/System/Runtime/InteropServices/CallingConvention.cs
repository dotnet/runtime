// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
namespace System.Runtime.InteropServices {

    using System;
    // Used for the CallingConvention named argument to the DllImport attribute
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum CallingConvention
    {
        Winapi          = 1,
        Cdecl           = 2,
        StdCall         = 3,
        ThisCall        = 4,
        FastCall        = 5,
    }
    
}
