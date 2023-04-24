// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    // This structure is used to pass context about a type's static class construction state from the runtime
    // to the classlibrary via the CheckStaticClassConstruction callback. It is permissable for the
    // classlibrary to add its own fields after these for its own use. These must not contain GC
    // references and will be zero initialized.
    [StructLayout(LayoutKind.Sequential)]
    public struct StaticClassConstructionContext
    {
        // Pointer to the code for the static class constructor method. Set to 0 .
        public volatile IntPtr cctorMethodAddress;
    }
}
