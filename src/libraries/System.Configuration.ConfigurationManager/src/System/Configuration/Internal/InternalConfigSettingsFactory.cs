// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration.Internal
{
    internal sealed class InternalConfigSettingsFactory : IInternalConfigSettingsFactory
    {
        private InternalConfigSettingsFactory() { }

        void IInternalConfigSettingsFactory.SetConfigurationSystem(IInternalConfigSystem configSystem, bool initComplete)
        {
            ConfigurationManager.SetConfigurationSystem(configSystem, initComplete);
        }

        void IInternalConfigSettingsFactory.CompleteInit()
        {
            ConfigurationManager.CompleteConfigInit();
        }
    }
}