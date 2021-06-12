// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// BundleOptions: Optional settings for configuring the type of files
    ///                included in the single file bundle.
    /// </summary>
    [Flags]
    public enum BundleOptions
    {
        None = 0,
        BundleNativeBinaries = 1,
        BundleOtherFiles = 2,
        BundleSymbolFiles = 4,
        BundleAllContent = BundleNativeBinaries | BundleOtherFiles,
        EnableCompression = 8,
    };
}
