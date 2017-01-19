// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [ConfigurationCollection(typeof(ProviderSettings))]
    public sealed class ProviderSettingsCollection : ConfigurationElementCollection
    {
        private static readonly ConfigurationPropertyCollection s_properties;

        static ProviderSettingsCollection()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection();
        }

        public ProviderSettingsCollection() :
            base(StringComparer.OrdinalIgnoreCase)
        { }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        public new ProviderSettings this[string key] => (ProviderSettings)BaseGet(key);

        public ProviderSettings this[int index]
        {
            get { return (ProviderSettings)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                    BaseRemoveAt(index);

                BaseAdd(index, value);
            }
        }

        public void Add(ProviderSettings provider)
        {
            if (provider != null)
            {
                provider.UpdatePropertyCollection();
                BaseAdd(provider);
            }
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ProviderSettings();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ProviderSettings)element).Name;
        }
    }
}