// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [ConfigurationCollection(typeof(ConnectionStringSettings))]
    public sealed class ConnectionStringSettingsCollection : ConfigurationElementCollection
    {
        private static readonly ConfigurationPropertyCollection s_properties;

        static ConnectionStringSettingsCollection()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection();
        }

        public ConnectionStringSettingsCollection()
            : base(StringComparer.OrdinalIgnoreCase)
        { }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        public ConnectionStringSettings this[int index]
        {
            get { return (ConnectionStringSettings)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null) BaseRemoveAt(index);
                BaseAdd(index, value);
            }
        }

        public new ConnectionStringSettings this[string name] => (ConnectionStringSettings)BaseGet(name);

        public int IndexOf(ConnectionStringSettings settings)
        {
            return BaseIndexOf(settings);
        }

        // the connection string behavior is strange in that is acts kind of like a
        // basic map and partially like an add remove clear collection
        // Overriding these methods allows for the specific behaviors to be
        // patterened
        protected override void BaseAdd(int index, ConfigurationElement element)
        {
            if (index == -1) BaseAdd(element, false);
            else base.BaseAdd(index, element);
        }

        public void Add(ConnectionStringSettings settings)
        {
            BaseAdd(settings);
        }

        public void Remove(ConnectionStringSettings settings)
        {
            if (BaseIndexOf(settings) >= 0) BaseRemove(settings.Key);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ConnectionStringSettings();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConnectionStringSettings)element).Key;
        }

        public void Clear()
        {
            BaseClear();
        }
    }
}
