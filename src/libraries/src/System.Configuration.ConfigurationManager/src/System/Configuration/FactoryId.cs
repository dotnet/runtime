// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Configuration
{
    // Identifies a factory
    [DebuggerDisplay("FactoryId {ConfigKey}")]
    internal class FactoryId
    {
        internal FactoryId(string configKey, string group, string name)
        {
            ConfigKey = configKey;
            Group = group;
            Name = name;
        }

        internal string ConfigKey { get; }

        internal string Group { get; }

        internal string Name { get; }
    }
}