// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration.Internal
{
    // The runtime config system
    internal sealed class ConfigSystem : IConfigSystem
    {
        private IInternalConfigHost _configHost;
        private IInternalConfigRoot _configRoot;

        void IConfigSystem.Init(Type typeConfigHost, params object[] hostInitParams)
        {
            _configRoot = new InternalConfigRoot();

            // Create the requested host and wrap in ImplicitMachineConfigHost so we can
            // stub in a simple machine.config if needed.
            IInternalConfigHost host = (IInternalConfigHost)TypeUtil.CreateInstance(typeConfigHost);
            _configHost = new ImplicitMachineConfigHost(host);

            _configRoot.Init(_configHost, isDesignTime: false);
            _configHost.Init(_configRoot, hostInitParams);
        }

        IInternalConfigHost IConfigSystem.Host => _configHost;

        IInternalConfigRoot IConfigSystem.Root => _configRoot;
    }
}
