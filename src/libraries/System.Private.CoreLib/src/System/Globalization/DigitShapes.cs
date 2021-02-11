// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    public enum DigitShapes : int
    {
        Context         = 0x0000,   // The shape depends on the previous text in the same output.
        None            = 0x0001,   // Gives full Unicode compatibility.
        NativeNational  = 0x0002    // National shapes
    }
}
