// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Versioning
{
    // Default visibility is Public, which isn't specified in this enum.
    // Public == the lack of Private or Assembly
    // Does this actually work?  Need to investigate that.
    [Flags]
    public enum ResourceScope
    {
        None = 0,
        // Resource type
        Machine   = 0x1,
        Process   = 0x2,
        AppDomain = 0x4,
        Library   = 0x8,
        // Visibility
        Private  = 0x10,  // Private to this one class.
        Assembly = 0x20,  // Assembly-level, like C#'s "internal"
    }
}
