// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Java
{
    [CLSCompliant(false)]
    [SupportedOSPlatform("android")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ComponentCrossReference
    {
        /// <summary>
        /// Index of the source group.
        /// </summary>
        public nuint SourceGroupIndex;

        /// <summary>
        /// Index of the destination group.
        /// </summary>
        public nuint DestinationGroupIndex;
    }
}
