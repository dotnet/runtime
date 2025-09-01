// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Java
{
    [CLSCompliant(false)]
    [SupportedOSPlatform("android")]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MarkCrossReferencesArgs
    {
        public nuint ComponentCount;
        public StronglyConnectedComponent* Components;
        public nuint CrossReferenceCount;
        public ComponentCrossReference* CrossReferences;
    }
}
