// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Provider;

namespace System.Configuration
{
    public class SettingsProviderCollection : ProviderCollection
    {
        public override void Add(ProviderBase provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (!(provider is SettingsProvider))
            {
                throw new ArgumentException(SR.Format(SR.Config_provider_must_implement_type, typeof(SettingsProvider)), nameof(provider));
            }

            base.Add(provider);
        }

        new public SettingsProvider this[string name]
        {
            get
            {
                return (SettingsProvider)base[name];
            }
        }
    }

}
