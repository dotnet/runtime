// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections.Generic;
using System.Globalization;

namespace System.Runtime.Serialization
{
    public sealed class ExtensionDataObject
    {
        private IList<ExtensionDataMember> _members;

#if USE_REFEMIT
        public ExtensionDataObject()
#else
        internal ExtensionDataObject()
#endif
        {
        }

#if USE_REFEMIT
        public IList<ExtensionDataMember> Members
#else
        internal IList<ExtensionDataMember> Members
#endif
        {
            get { return _members; }
            set { _members = value; }
        }
    }

#if USE_REFEMIT
    public class ExtensionDataMember
#else
    internal class ExtensionDataMember
#endif
    {
        private string _name;
        private string _ns;
        private IDataNode _value;
        private int _memberIndex;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Namespace
        {
            get { return _ns; }
            set { _ns = value; }
        }

        public IDataNode Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public int MemberIndex
        {
            get { return _memberIndex; }
            set { _memberIndex = value; }
        }
    }

#if USE_REFEMIT
    public interface IDataNode
#else
    internal interface IDataNode
#endif
    {
        Type DataType { get; }
        object Value { get; set; }  // boxes for primitives
        string DataContractName { get; set; }
        string DataContractNamespace { get; set; }
        string ClrTypeName { get; set; }
        string ClrAssemblyName { get; set; }
        string Id { get; set; }
        bool PreservesReferences { get; }

        // NOTE: consider moving below APIs to DataNode<T> if IDataNode API is made public
        void GetData(ElementData element);
        bool IsFinalValue { get; set; }
        void Clear();
    }

    internal class DataNode<T> : IDataNode
    {
        protected Type dataType;
        private T _value;
        private string _dataContractName;
        private string _dataContractNamespace;
        private string _clrTypeName;
        private string _clrAssemblyName;
        private string _id = Globals.NewObjectId;
        private bool _isFinalValue;

        internal DataNode()
        {
            this.dataType = typeof(T);
            _isFinalValue = true;
        }

        internal DataNode(T value)
            : this()
        {
            _value = value;
        }

        public Type DataType
        {
            get { return dataType; }
        }

        public object Value
        {
            get { return _value; }
            set { _value = (T)value; }
        }

        bool IDataNode.IsFinalValue
        {
            get { return _isFinalValue; }
            set { _isFinalValue = value; }
        }

        public T GetValue()
        {
            return _value;
        }

#if NotUsed
        public void SetValue(T value)
        {
            this.value = value;
        }
#endif

        public string DataContractName
        {
            get { return _dataContractName; }
            set { _dataContractName = value; }
        }

        public string DataContractNamespace
        {
            get { return _dataContractNamespace; }
            set { _dataContractNamespace = value; }
        }

        public string ClrTypeName
        {
            get { return _clrTypeName; }
            set { _clrTypeName = value; }
        }

        public string ClrAssemblyName
        {
            get { return _clrAssemblyName; }
            set { _clrAssemblyName = value; }
        }

        public bool PreservesReferences
        {
            get { return (Id != Globals.NewObjectId); }
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public virtual void GetData(ElementData element)
        {
            element.dataNode = this;
            element.attributeCount = 0;
            element.childElementIndex = 0;

            if (DataContractName != null)
                AddQualifiedNameAttribute(element, Globals.XsiPrefix, Globals.XsiTypeLocalName, Globals.SchemaInstanceNamespace, DataContractName, DataContractNamespace);
            if (ClrTypeName != null)
                element.AddAttribute(Globals.SerPrefix, Globals.SerializationNamespace, Globals.ClrTypeLocalName, ClrTypeName);
            if (ClrAssemblyName != null)
                element.AddAttribute(Globals.SerPrefix, Globals.SerializationNamespace, Globals.ClrAssemblyLocalName, ClrAssemblyName);
        }

        public virtual void Clear()
        {
            // dataContractName not cleared because it is used when re-serializing from unknown data
            _clrTypeName = _clrAssemblyName = null;
        }

        internal void AddQualifiedNameAttribute(ElementData element, string elementPrefix, string elementName, string elementNs, string valueName, string valueNs)
        {
            string prefix = ExtensionDataReader.GetPrefix(valueNs);
            element.AddAttribute(elementPrefix, elementNs, elementName, String.Format(CultureInfo.InvariantCulture, "{0}:{1}", prefix, valueName));

            bool prefixDeclaredOnElement = false;
            if (element.attributes != null)
            {
                for (int i = 0; i < element.attributes.Length; i++)
                {
                    AttributeData attribute = element.attributes[i];
                    if (attribute != null && attribute.prefix == Globals.XmlnsPrefix && attribute.localName == prefix)
                    {
                        prefixDeclaredOnElement = true;
                        break;
                    }
                }
            }
            if (!prefixDeclaredOnElement)
                element.AddAttribute(Globals.XmlnsPrefix, Globals.XmlnsNamespace, prefix, valueNs);
        }
    }

    internal class ClassDataNode : DataNode<object>
    {
        private IList<ExtensionDataMember> _members;

        internal ClassDataNode()
        {
            dataType = Globals.TypeOfClassDataNode;
        }

        internal IList<ExtensionDataMember> Members
        {
            get { return _members; }
            set { _members = value; }
        }

        public override void Clear()
        {
            base.Clear();
            _members = null;
        }
    }

    internal class XmlDataNode : DataNode<object>
    {
        private IList<XmlAttribute> _xmlAttributes;
        private IList<XmlNode> _xmlChildNodes;
        private XmlDocument _ownerDocument;

        internal XmlDataNode()
        {
            dataType = Globals.TypeOfXmlDataNode;
        }

        internal IList<XmlAttribute> XmlAttributes
        {
            get { return _xmlAttributes; }
            set { _xmlAttributes = value; }
        }

        internal IList<XmlNode> XmlChildNodes
        {
            get { return _xmlChildNodes; }
            set { _xmlChildNodes = value; }
        }

        internal XmlDocument OwnerDocument
        {
            get { return _ownerDocument; }
            set { _ownerDocument = value; }
        }

        public override void Clear()
        {
            base.Clear();
            _xmlAttributes = null;
            _xmlChildNodes = null;
            _ownerDocument = null;
        }
    }

    internal class CollectionDataNode : DataNode<Array>
    {
        private IList<IDataNode> _items;
        private string _itemName;
        private string _itemNamespace;
        private int _size = -1;

        internal CollectionDataNode()
        {
            dataType = Globals.TypeOfCollectionDataNode;
        }

        internal IList<IDataNode> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        internal string ItemName
        {
            get { return _itemName; }
            set { _itemName = value; }
        }

        internal string ItemNamespace
        {
            get { return _itemNamespace; }
            set { _itemNamespace = value; }
        }

        internal int Size
        {
            get { return _size; }
            set { _size = value; }
        }

        public override void GetData(ElementData element)
        {
            base.GetData(element);

            element.AddAttribute(Globals.SerPrefix, Globals.SerializationNamespace, Globals.ArraySizeLocalName, Size.ToString(NumberFormatInfo.InvariantInfo));
        }

        public override void Clear()
        {
            base.Clear();
            _items = null;
            _size = -1;
        }
    }

    internal class ISerializableDataNode : DataNode<object>
    {
        private string _factoryTypeName;
        private string _factoryTypeNamespace;
        private IList<ISerializableDataMember> _members;

        internal ISerializableDataNode()
        {
            dataType = Globals.TypeOfISerializableDataNode;
        }

        internal string FactoryTypeName
        {
            get { return _factoryTypeName; }
            set { _factoryTypeName = value; }
        }

        internal string FactoryTypeNamespace
        {
            get { return _factoryTypeNamespace; }
            set { _factoryTypeNamespace = value; }
        }

        internal IList<ISerializableDataMember> Members
        {
            get { return _members; }
            set { _members = value; }
        }

        public override void GetData(ElementData element)
        {
            base.GetData(element);

            if (FactoryTypeName != null)
                AddQualifiedNameAttribute(element, Globals.SerPrefix, Globals.ISerializableFactoryTypeLocalName, Globals.SerializationNamespace, FactoryTypeName, FactoryTypeNamespace);
        }

        public override void Clear()
        {
            base.Clear();
            _members = null;
            _factoryTypeName = _factoryTypeNamespace = null;
        }
    }

    internal class ISerializableDataMember
    {
        private string _name;
        private IDataNode _value;

        internal string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        internal IDataNode Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
