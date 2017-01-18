// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    public sealed class SettingElementCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override string ElementName
        {
            get
            {
                return "setting";
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new SettingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SettingElement)element).Key;
        }

        public SettingElement Get(string elementKey)
        {
            return (SettingElement)BaseGet(elementKey);
        }

        public void Add(SettingElement element)
        {
            BaseAdd(element);
        }

        public void Remove(SettingElement element)
        {
            BaseRemove(GetElementKey(element));
        }

        public void Clear()
        {
            BaseClear();
        }
    }
}
