// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Java
{
    [CLSCompliant(false)]
    [SupportedOSPlatform("android")]
    public static partial class JavaMarshal
    {
        public static unsafe void Initialize(delegate* unmanaged<MarkCrossReferencesArgs*, void> markCrossReferences)
        {
            throw new PlatformNotSupportedException();
        }

        public static unsafe GCHandle CreateReferenceTrackingHandle(object obj, void* context)
        {
            throw new PlatformNotSupportedException();
        }

        public static unsafe void* GetContext(GCHandle obj)
        {
            throw new PlatformNotSupportedException();
        }

        public static unsafe void FinishCrossReferenceProcessing(
            MarkCrossReferencesArgs* crossReferences,
            ReadOnlySpan<GCHandle> unreachableObjectHandles)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
