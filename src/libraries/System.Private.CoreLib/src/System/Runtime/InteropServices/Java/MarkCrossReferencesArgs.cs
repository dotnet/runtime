// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Java
{
    /// <summary>
    /// Data for the callback supplied to <see cref="O:JavaMarshal.Initialize" />
    /// for marking managed objects referenced from Java during cross-reference processing.
    /// </summary>
    [CLSCompliant(false)]
    [SupportedOSPlatform("android")]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MarkCrossReferencesArgs
    {
        /// <summary>
        /// The number of strongly connected components being reported.
        /// </summary>
        public nuint ComponentCount;

        /// <summary>
        /// A pointer to the array of strongly connected components.
        /// </summary>
        public StronglyConnectedComponent* Components;

        /// <summary>
        /// The number of cross-references being reported.
        /// </summary>
        public nuint CrossReferenceCount;

        /// <summary>
        /// A pointer to the array of cross-references.
        /// </summary>
        public ComponentCrossReference* CrossReferences;
    }
}
