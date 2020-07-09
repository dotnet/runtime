// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Configuration
{
    // obsolete
    public interface IConfigurationSystem
    {
        // Returns the config object for the specified key.
        object GetConfig(string configKey);

        // Initializes the configuration system.
        void Init();
    }
}
