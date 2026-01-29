// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata
{
    public static partial class MetadataUpdater
    {
        /// <summary>
        /// Returns true if hot reload is explicitly disabled via the DOTNET_HOTRELOAD_DISABLED environment variable
        /// or the System.Reflection.Metadata.HotReloadDisabled AppContext switch set to true.
        /// </summary>
        internal static bool IsHotReloadDisabled =>
            AppContextConfigHelper.GetBooleanConfig("System.Reflection.Metadata.HotReloadDisabled", "DOTNET_HOTRELOAD_DISABLED");
    }
}
