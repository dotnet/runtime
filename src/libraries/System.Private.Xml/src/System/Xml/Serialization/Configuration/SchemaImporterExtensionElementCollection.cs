// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization.Configuration
{
    using System;
    using System.Configuration;

    internal sealed class SchemaImporterExtensionElementCollection : ConfigurationElementCollection
    {
        public SchemaImporterExtensionElementCollection()
        {
        }

        public SchemaImporterExtensionElement this[int index]
        {
            get
            {
                return (SchemaImporterExtensionElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public new SchemaImporterExtensionElement this[string name]
        {
            get
            {
                return (SchemaImporterExtensionElement)BaseGet(name);
            }
            set
            {
                if (BaseGet(name) != null)
                {
                    BaseRemove(name);
                }
                BaseAdd(value);
            }
        }

        public void Add(SchemaImporterExtensionElement element)
        {
            BaseAdd(element);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new SchemaImporterExtensionElement();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((SchemaImporterExtensionElement)element).Key;
        }

        public int IndexOf(SchemaImporterExtensionElement element)
        {
            return BaseIndexOf(element);
        }

        public void Remove(SchemaImporterExtensionElement element)
        {
            BaseRemove(element.Key);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }
    }
}


