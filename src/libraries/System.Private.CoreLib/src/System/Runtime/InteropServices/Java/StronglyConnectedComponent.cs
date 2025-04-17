// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Java
{
    [CLSCompliant(false)]
    [SupportedOSPlatform("android")]
    public unsafe struct StronglyConnectedComponent
    {
        /// <summary>
        /// Number of objects in each collection.
        /// </summary>
        public nint Count;

        /// <summary>
        /// Contains pointers to context passed during
        /// creation of each GCHandle.
        /// </summary>
        public IntPtr* Context;
    }
}
