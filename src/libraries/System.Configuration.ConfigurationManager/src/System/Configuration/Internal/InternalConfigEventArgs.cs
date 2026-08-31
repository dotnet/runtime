// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration.Internal
{
    public sealed class InternalConfigEventArgs : EventArgs
    {
        public InternalConfigEventArgs(string configPath)
        {
            ConfigPath = configPath;
        }

        public string ConfigPath { get; set; }
    }
}
