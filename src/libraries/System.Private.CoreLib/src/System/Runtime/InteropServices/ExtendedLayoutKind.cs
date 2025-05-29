// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Indicates the layout kind of a struct when using extended layout.
    /// </summary>
    public enum ExtendedLayoutKind
    {
        /// <summary>
        /// The value type should have its fields laid out in accordance with the C language struct layout rules.
        /// </summary>
        CStruct = 0,
    }
}
