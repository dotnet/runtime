// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration.Internal
{
    // The runtime config system
    internal class ConfigSystem : IConfigSystem
    {
        private IInternalConfigHost _configHost;
        private IInternalConfigRoot _configRoot;

        void IConfigSystem.Init(Type typeConfigHost, params object[] hostInitParams)
        {
            _configRoot = new InternalConfigRoot();
            _configHost = (IInternalConfigHost)TypeUtil.CreateInstance(typeConfigHost);

            _configRoot.Init(_configHost, false);
            _configHost.Init(_configRoot, hostInitParams);
        }

        IInternalConfigHost IConfigSystem.Host => _configHost;

        IInternalConfigRoot IConfigSystem.Root => _configRoot;
    }
}