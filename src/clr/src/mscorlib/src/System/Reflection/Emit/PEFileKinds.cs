// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Emit {
    
    using System;
    // This Enum matchs the CorFieldAttr defined in CorHdr.h
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum PEFileKinds
    {
        Dll                = 0x0001,
        ConsoleApplication = 0x0002,
        WindowApplication = 0x0003,
    }
}
