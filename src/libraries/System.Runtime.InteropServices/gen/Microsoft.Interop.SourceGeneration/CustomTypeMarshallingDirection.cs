// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.ComponentModel;


namespace Microsoft.Interop
{
    /// <summary>
    /// A direction of marshalling data into or out of the managed environment
    /// </summary>
    [Flags]
    public enum CustomTypeMarshallingDirection
    {
        /// <summary>
        /// No marshalling direction
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        None = 0,
        /// <summary>
        /// Marshalling from a managed environment to an unmanaged environment
        /// </summary>
        In = 0x1,
        /// <summary>
        /// Marshalling from an unmanaged environment to a managed environment
        /// </summary>
        Out = 0x2,
        /// <summary>
        /// Marshalling to and from managed and unmanaged environments
        /// </summary>
        Ref = In | Out,
    }
}
