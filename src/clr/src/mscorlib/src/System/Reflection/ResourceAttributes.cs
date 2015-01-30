// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// ResourceAttributes is an enum which defines the attributes that may be associated
// 
//  with a manifest resource.  The values here are defined in Corhdr.h.
//
//
namespace System.Reflection {
    
    using System;
[Serializable]
[Flags]  
[System.Runtime.InteropServices.ComVisible(true)]
    public enum ResourceAttributes
    {
        Public          =   0x0001,
        Private         =   0x0002,
    }
}
