// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Configuration;

namespace System.Runtime.Caching.Configuration
{
    [ConfigurationCollection(typeof(MemoryCacheElement),
    CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
    public sealed class MemoryCacheSettingsCollection : ConfigurationElementCollection
    {
        private static ConfigurationPropertyCollection s_properties;

        static MemoryCacheSettingsCollection()
        {
            // Property initialization
            s_properties = new ConfigurationPropertyCollection();
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                return s_properties;
            }
        }

        public MemoryCacheSettingsCollection()
        {
        }

        public MemoryCacheElement this[int index]
        {
            get { return (MemoryCacheElement)base.BaseGet(index); }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                base.BaseAdd(index, value);
            }
        }

        public new MemoryCacheElement this[string key]
        {
            get
            {
                return (MemoryCacheElement)BaseGet(key);
            }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMapAlternate;
            }
        }

        public int IndexOf(MemoryCacheElement cache)
        {
            return BaseIndexOf(cache);
        }

        public void Add(MemoryCacheElement cache)
        {
            BaseAdd(cache);
        }

        public void Remove(MemoryCacheElement cache)
        {
            BaseRemove(cache.Name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new MemoryCacheElement();
        }

        protected override ConfigurationElement CreateNewElement(string elementName)
        {
            return new MemoryCacheElement(elementName);
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MemoryCacheElement)element).Name;
        }
    }
}
