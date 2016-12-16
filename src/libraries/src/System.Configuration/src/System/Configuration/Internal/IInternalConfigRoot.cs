// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Configuration.Internal
{
    public interface IInternalConfigRoot
    {
        bool IsDesignTime { get; }

        void Init(IInternalConfigHost host, bool isDesignTime);

        event InternalConfigEventHandler ConfigChanged;

        event InternalConfigEventHandler ConfigRemoved;

        object GetSection(string section, string configPath);

        // Get the configPath of the nearest ancestor that has the config data
        string GetUniqueConfigPath(string configPath);

        IInternalConfigRecord GetUniqueConfigRecord(string configPath);

        IInternalConfigRecord GetConfigRecord(string configPath);

        void RemoveConfig(string configPath);
    }
}