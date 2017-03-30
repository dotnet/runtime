// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [ConfigurationCollection(typeof(KeyValueConfigurationElement))]
    public class KeyValueConfigurationCollection : ConfigurationElementCollection
    {
        private static readonly ConfigurationPropertyCollection s_properties;

        static KeyValueConfigurationCollection()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection();
        }

        public KeyValueConfigurationCollection() :
            base(StringComparer.OrdinalIgnoreCase)
        {
            InternalAddToEnd = true;
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        protected override bool ThrowOnDuplicate => false;

        public new KeyValueConfigurationElement this[string key] => (KeyValueConfigurationElement)BaseGet(key);

        public string[] AllKeys => StringUtil.ObjectArrayToStringArray(BaseGetAllKeys());

        public void Add(KeyValueConfigurationElement keyValue)
        {
            // Need to initialize in order to get the key
            keyValue.Init();

            // the appsettings add works more like a namevalue collection add in that it appends values
            // when add is called and teh key already exists.
            KeyValueConfigurationElement oldValue = (KeyValueConfigurationElement)BaseGet(keyValue.Key);
            if (oldValue == null)
            {
                BaseAdd(keyValue);
            }
            else
            {
                oldValue.Value += "," + keyValue.Value;
                int index = BaseIndexOf(oldValue);
                BaseRemoveAt(index);
                BaseAdd(index, oldValue);
            }
        }

        public void Add(string key, string value)
        {
            KeyValueConfigurationElement element = new KeyValueConfigurationElement(key, value);
            Add(element);
        }

        public void Remove(string key)
        {
            BaseRemove(key);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new KeyValueConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((KeyValueConfigurationElement)element).Key;
        }
    }
}