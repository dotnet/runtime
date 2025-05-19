// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Java
{
    [CLSCompliant(false)]
    [SupportedOSPlatform("android")]
    public unsafe struct MarkCrossReferences
    {
        public nint ComponentsLen;
        public StronglyConnectedComponent* Components;
        public nint CrossReferencesLen;
        public ComponentCrossReference* CrossReferences;
    }
}
