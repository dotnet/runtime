// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// CallingConventions is a set of Bits representing the calling conventions in the system.

namespace System.Reflection
{
    [Flags]
    public enum CallingConventions
    {
        //NOTE: If you change this please update COMMember.cpp.  These
        //    are defined there.
        Standard = 0x0001,
        VarArgs = 0x0002,
        Any = Standard | VarArgs,
        HasThis = 0x0020,
        ExplicitThis = 0x0040,
    }
}
