// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [ConfigurationCollection(typeof(NameValueConfigurationElement))]
    public sealed class NameValueConfigurationCollection : ConfigurationElementCollection
    {
        private static readonly ConfigurationPropertyCollection s_properties;

        static NameValueConfigurationCollection()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection();
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        public new NameValueConfigurationElement this[string name]
        {
            get { return (NameValueConfigurationElement)BaseGet(name); }
            set
            {
                int index = -1; // append by default
                NameValueConfigurationElement tempElement = (NameValueConfigurationElement)BaseGet(name);
                if (tempElement != null)
                {
                    index = BaseIndexOf(tempElement);
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public string[] AllKeys => StringUtil.ObjectArrayToStringArray(BaseGetAllKeys());

        public void Add(NameValueConfigurationElement nameValue)
        {
            BaseAdd(nameValue);
        }

        public void Remove(NameValueConfigurationElement nameValue)
        {
            if (BaseIndexOf(nameValue) >= 0)
                BaseRemove(nameValue.Name);
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
            return new NameValueConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((NameValueConfigurationElement)element).Name;
        }
    }
}