// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Used on classes derived from ConfigurationElementCollection. Specifies the collection item type and
    /// verbs used for add/remove/clear.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public sealed class ConfigurationCollectionAttribute : Attribute
    {
        private string _addItemName;
        private string _clearItemsName;
        private string _removeItemName;

        public ConfigurationCollectionAttribute(Type itemType)
        {
            if (itemType == null) throw new ArgumentNullException(nameof(itemType));

            ItemType = itemType;
        }

        public Type ItemType { get; }

        public string AddItemName
        {
            get { return _addItemName ?? ConfigurationElementCollection.DefaultAddItemName; }
            set
            {
                if (string.IsNullOrEmpty(value)) value = null;
                _addItemName = value;
            }
        }

        public string RemoveItemName
        {
            get { return _removeItemName ?? ConfigurationElementCollection.DefaultRemoveItemName; }
            set
            {
                if (string.IsNullOrEmpty(value)) value = null;
                _removeItemName = value;
            }
        }

        public string ClearItemsName
        {
            get { return _clearItemsName ?? ConfigurationElementCollection.DefaultClearItemsName; }
            set
            {
                if (string.IsNullOrEmpty(value)) value = null;
                _clearItemsName = value;
            }
        }

        public ConfigurationElementCollectionType CollectionType { get; set; } =
            ConfigurationElementCollectionType.AddRemoveClearMap;
    }
}