// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Configuration;
using System.Runtime.Versioning;

namespace System.Runtime.Caching.Configuration
{
#if NET5_0_OR_GREATER
    [UnsupportedOSPlatform("browser")]
#endif
    [ConfigurationCollection(typeof(MemoryCacheElement),
    CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
    internal sealed class MemoryCacheSettingsCollection : ConfigurationElementCollection
    {
        private static readonly ConfigurationPropertyCollection s_properties = new ConfigurationPropertyCollection();

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
