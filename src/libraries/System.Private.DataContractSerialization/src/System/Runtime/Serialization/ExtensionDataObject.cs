// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace System.Runtime.Serialization
{
    public sealed class ExtensionDataObject
    {
        private IList<ExtensionDataMember>? _members;

        internal ExtensionDataObject()
        {
        }

        internal IList<ExtensionDataMember>? Members
        {
            get { return _members; }
            set { _members = value; }
        }
    }

    internal sealed class ExtensionDataMember
    {
        private IDataNode? _value;
        private int _memberIndex;

        public ExtensionDataMember(string name, string ns)
        {
            Name = name;
            Namespace = ns;
        }

        public string Name { get; }

        public string Namespace { get; }

        public IDataNode? Value
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

    internal interface IDataNode
    {
        Type DataType { get; }
        object? Value { get; set; }  // boxes for primitives
        string? DataContractName { get; set; }
        string? DataContractNamespace { get; set; }
        string? ClrTypeName { get; set; }
        string? ClrAssemblyName { get; set; }
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
        private T _value = default!;
        private string? _dataContractName;
        private string? _dataContractNamespace;
        private string? _clrTypeName;
        private string? _clrAssemblyName;
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

        public object? Value
        {
            get { return _value; }
            set { _value = (T)value!; }
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

        public string? DataContractName
        {
            get { return _dataContractName; }
            set { _dataContractName = value; }
        }

        public string? DataContractNamespace
        {
            get { return _dataContractNamespace; }
            set { _dataContractNamespace = value; }
        }

        public string? ClrTypeName
        {
            get { return _clrTypeName; }
            set { _clrTypeName = value; }
        }

        public string? ClrAssemblyName
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

        internal static void AddQualifiedNameAttribute(ElementData element, string elementPrefix, string elementName, string elementNs, string valueName, string? valueNs)
        {
            string prefix = ExtensionDataReader.GetPrefix(valueNs);
            element.AddAttribute(elementPrefix, elementNs, elementName, prefix + ":" + valueName);

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

    internal sealed class ClassDataNode : DataNode<object>
    {
        private IList<ExtensionDataMember>? _members;

        internal ClassDataNode()
        {
            dataType = Globals.TypeOfClassDataNode;
        }

        internal IList<ExtensionDataMember>? Members
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

    internal sealed class XmlDataNode : DataNode<object>
    {
        private IList<XmlAttribute>? _xmlAttributes;
        private IList<XmlNode>? _xmlChildNodes;
        private XmlDocument? _ownerDocument;

        internal XmlDataNode()
        {
            dataType = Globals.TypeOfXmlDataNode;
        }

        internal IList<XmlAttribute>? XmlAttributes
        {
            get { return _xmlAttributes; }
            set { _xmlAttributes = value; }
        }

        internal IList<XmlNode>? XmlChildNodes
        {
            get { return _xmlChildNodes; }
            set { _xmlChildNodes = value; }
        }

        internal XmlDocument? OwnerDocument
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

    internal sealed class CollectionDataNode : DataNode<Array>
    {
        private IList<IDataNode?>? _items;
        private string? _itemName;
        private string? _itemNamespace;
        private int _size = -1;

        internal CollectionDataNode()
        {
            dataType = Globals.TypeOfCollectionDataNode;
        }

        internal IList<IDataNode?>? Items
        {
            get { return _items; }
            set { _items = value; }
        }

        internal string? ItemName
        {
            get { return _itemName; }
            set { _itemName = value; }
        }

        internal string? ItemNamespace
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

    internal sealed class ISerializableDataNode : DataNode<object>
    {
        private string? _factoryTypeName;
        private string? _factoryTypeNamespace;
        private IList<ISerializableDataMember>? _members;

        internal ISerializableDataNode()
        {
            dataType = Globals.TypeOfISerializableDataNode;
        }

        internal string? FactoryTypeName
        {
            get { return _factoryTypeName; }
            set { _factoryTypeName = value; }
        }

        internal string? FactoryTypeNamespace
        {
            get { return _factoryTypeNamespace; }
            set { _factoryTypeNamespace = value; }
        }

        internal IList<ISerializableDataMember>? Members
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

    internal sealed class ISerializableDataMember
    {
        private IDataNode? _value;

        public ISerializableDataMember(string name)
        {
            Name = name;
        }

        internal string Name { get; }

        internal IDataNode? Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
