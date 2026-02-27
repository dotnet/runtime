// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.IO.Compression
{
    internal static class ZipArchiveSettings
    {
        [FeatureSwitchDefinition("System.IO.Compression.ZipArchive.AllowZstandard")]
        internal static bool AllowZstandard { get; } =
            AppContext.TryGetSwitch("System.IO.Compression.ZipArchive.AllowZstandard", out bool isEnabled) ? isEnabled : true;
    }
}
