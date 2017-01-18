// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    // Represents an update to a configuration section, either in its
    // declaration or its definition.
    internal abstract class Update
    {
        internal Update(string configKey, bool moved, string updatedXml)
        {
            ConfigKey = configKey;
            Moved = moved;
            UpdatedXml = updatedXml;
        }

        internal string ConfigKey { get; }

        internal bool Moved { get; }

        internal string UpdatedXml { get; }

        internal bool Retrieved { get; set; }
    }
}