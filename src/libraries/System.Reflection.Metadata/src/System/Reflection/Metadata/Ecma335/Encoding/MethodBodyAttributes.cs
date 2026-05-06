// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata.Ecma335
{
    /// <summary>
    /// Method body attributes.
    /// </summary>
    [Flags]
    public enum MethodBodyAttributes
    {
        /// <summary>
        /// No local memory initialization is performed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Zero-initialize any locals the method defines and dynamically allocated local memory.
        /// </summary>
        InitLocals = 1,
    }
}
