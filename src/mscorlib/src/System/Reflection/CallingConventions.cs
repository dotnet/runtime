// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// CallingConventions is a set of Bits representing the calling conventions
// 
//    in the system.
//
//
namespace System.Reflection {
    using System.Runtime.InteropServices;
    using System;
    [Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum CallingConventions
    {
        //NOTE: If you change this please update COMMember.cpp.  These
        //    are defined there.
        Standard        = 0x0001,
        VarArgs         = 0x0002,
        Any             = Standard | VarArgs,
        HasThis         = 0x0020,
        ExplicitThis    = 0x0040,
    }
}
