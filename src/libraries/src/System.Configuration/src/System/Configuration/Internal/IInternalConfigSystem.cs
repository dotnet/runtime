// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
    [ComVisible(false)]
    public interface IInternalConfigSystem
    {
        // Supports user config
        bool SupportsUserConfig { get; }
        // Returns the config object for the specified key.
        // It's actually GetSection
        object GetSection(string configKey);

        // Refreshes the configuration system.
        // It's actually RefreshSection
        void RefreshConfig(string sectionName);
    }
}