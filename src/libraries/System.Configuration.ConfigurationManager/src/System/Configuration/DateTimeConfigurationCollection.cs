// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [ConfigurationCollection(typeof(DateTimeConfigurationElement))]
    public sealed class DateTimeConfigurationCollection : ConfigurationElementCollection
    {
        private static readonly ConfigurationPropertyCollection s_properties;

        static DateTimeConfigurationCollection()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection();
        }

        protected internal override ConfigurationPropertyCollection Properties => s_properties;

        public DateTime this[int index]
        {
            get { return ((DateTimeConfigurationElement)BaseGet(index)).Value; }
            set
            {
                if (BaseGet(index) != null) BaseRemoveAt(index);
                BaseAdd(index, new DateTimeConfigurationElement(value));
            }
        }

        public object[] AllKeys => BaseGetAllKeys();

        public void Add(DateTime dateTime)
        {
            BaseAdd(new DateTimeConfigurationElement(dateTime));
        }

        public void Remove(DateTime dateTime)
        {
            BaseRemove(dateTime);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new DateTimeConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DateTimeConfigurationElement)element).Value;
        }
    }
}