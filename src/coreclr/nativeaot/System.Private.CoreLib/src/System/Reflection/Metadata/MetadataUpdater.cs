// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata
{
    public static class MetadataUpdater
    {
        public static void ApplyUpdate(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta)
        {
            throw new PlatformNotSupportedException();
        }

        [FeatureSwitchDefinition("System.Reflection.Metadata.MetadataUpdater.IsSupported")]
        public static bool IsSupported => false;
    }
}
