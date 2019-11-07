// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [ConfigurationCollection(typeof(SchemeSettingElement),
        CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap,
        AddItemName = AddItemName,
        ClearItemsName = ClearItemsName,
        RemoveItemName = RemoveItemName)]
    public sealed class SchemeSettingElementCollection : ConfigurationElementCollection
    {
        internal const string AddItemName = "add";
        internal const string ClearItemsName = "clear";
        internal const string RemoveItemName = "remove";

        public SchemeSettingElementCollection()
        {
            AddElementName = AddItemName;
            ClearElementName = ClearItemsName;
            RemoveElementName = RemoveItemName;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
        }

        public SchemeSettingElement this[int index]
        {
            get { return (SchemeSettingElement)BaseGet(index); }
        }

        public new SchemeSettingElement this[string name]
        {
            get { return (SchemeSettingElement)BaseGet(name); }
        }

        public int IndexOf(SchemeSettingElement element)
        {
            return BaseIndexOf(element);
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new SchemeSettingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SchemeSettingElement)element).Name;
        }
    }

}
