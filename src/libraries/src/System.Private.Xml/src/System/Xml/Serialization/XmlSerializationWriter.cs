// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if XMLSERIALIZERGENERATOR
namespace Microsoft.XmlSerializer.Generator
#else
namespace System.Xml.Serialization
#endif
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Xml.Schema;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Runtime.Versioning;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using System.Xml;

    ///<internalonly/>
#if XMLSERIALIZERGENERATOR
    internal abstract class XmlSerializationWriter : XmlSerializationGeneratedCode
#else
    public abstract class XmlSerializationWriter : XmlSerializationGeneratedCode
#endif
    {
        private XmlWriter _w;
        private XmlSerializerNamespaces _namespaces;
        private int _tempNamespacePrefix;
        private HashSet<int> _usedPrefixes;
        private Hashtable _references;
        private string _idBase;
        private int _nextId;
        private Hashtable _typeEntries;
        private ArrayList _referencesToWrite;
        private Hashtable _objectsInUse;
        private string _aliasBase = "q";
        private bool _soap12;
        private bool _escapeName = true;

#if FEATURE_SERIALIZATION_UAPAOT
        // this method must be called before any generated serialization methods are called
        internal void Init(XmlWriter w, XmlSerializerNamespaces namespaces, string encodingStyle, string idBase)
        {
            _w = w;
            _namespaces = namespaces;
        }
#endif

        // this method must be called before any generated serialization methods are called
        internal void Init(XmlWriter w, XmlSerializerNamespaces namespaces, string encodingStyle, string idBase, TempAssembly tempAssembly)
        {
            _w = w;
            _namespaces = namespaces;
            _soap12 = (encodingStyle == Soap12.Encoding);
            _idBase = idBase;
            Init(tempAssembly);
        }

        protected bool EscapeName
        {
            get
            {
                return _escapeName;
            }
            set
            {
                _escapeName = value;
            }
        }

        protected XmlWriter Writer
        {
            get
            {
                return _w;
            }
            set
            {
                _w = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected ArrayList Namespaces
        {
            get
            {
                return _namespaces == null ? null : _namespaces.NamespaceList;
            }
            set
            {
                if (value == null)
                {
                    _namespaces = null;
                }
                else
                {
                    XmlQualifiedName[] qnames = (XmlQualifiedName[])value.ToArray(typeof(XmlQualifiedName));
                    _namespaces = new XmlSerializerNamespaces(qnames);
                }
            }
        }

        protected static byte[] FromByteArrayBase64(byte[] value)
        {
            // Unlike other "From" functions that one is just a place holder for automatic code generation.
            // The reason is performance and memory consumption for (potentially) big 64base-encoded chunks
            // And it is assumed that the caller generates the code that will distinguish between byte[] and string return types
            //
            return value;
        }

        ///<internalonly/>
        protected static Assembly ResolveDynamicAssembly(string assemblyFullName)
        {
            return DynamicAssemblies.Get(assemblyFullName);
        }

        protected static string FromByteArrayHex(byte[] value)
        {
            return XmlCustomFormatter.FromByteArrayHex(value);
        }

        protected static string FromDateTime(DateTime value)
        {
            return XmlCustomFormatter.FromDateTime(value);
        }

        protected static string FromDate(DateTime value)
        {
            return XmlCustomFormatter.FromDate(value);
        }

        protected static string FromTime(DateTime value)
        {
            return XmlCustomFormatter.FromTime(value);
        }

        protected static string FromChar(char value)
        {
            return XmlCustomFormatter.FromChar(value);
        }

        protected static string FromEnum(long value, string[] values, long[] ids)
        {
            return XmlCustomFormatter.FromEnum(value, values, ids, null);
        }

        protected static string FromEnum(long value, string[] values, long[] ids, string typeName)
        {
            return XmlCustomFormatter.FromEnum(value, values, ids, typeName);
        }

        protected static string FromXmlName(string name)
        {
            return XmlCustomFormatter.FromXmlName(name);
        }

        protected static string FromXmlNCName(string ncName)
        {
            return XmlCustomFormatter.FromXmlNCName(ncName);
        }

        protected static string FromXmlNmToken(string nmToken)
        {
            return XmlCustomFormatter.FromXmlNmToken(nmToken);
        }

        protected static string FromXmlNmTokens(string nmTokens)
        {
            return XmlCustomFormatter.FromXmlNmTokens(nmTokens);
        }

        protected void WriteXsiType(string name, string ns)
        {
            WriteAttribute("type", XmlSchema.InstanceNamespace, GetQualifiedName(name, ns));
        }

        private XmlQualifiedName GetPrimitiveTypeName(Type type)
        {
            return GetPrimitiveTypeName(type, true);
        }

        private XmlQualifiedName GetPrimitiveTypeName(Type type, bool throwIfUnknown)
        {
            XmlQualifiedName qname = GetPrimitiveTypeNameInternal(type);
            if (throwIfUnknown && qname == null)
                throw CreateUnknownTypeException(type);
            return qname;
        }

        internal static XmlQualifiedName GetPrimitiveTypeNameInternal(Type type)
        {
            string typeName;
            string typeNs = XmlSchema.Namespace;

            switch (type.GetTypeCode())
            {
                case TypeCode.String: typeName = "string"; break;
                case TypeCode.Int32: typeName = "int"; break;
                case TypeCode.Boolean: typeName = "boolean"; break;
                case TypeCode.Int16: typeName = "short"; break;
                case TypeCode.Int64: typeName = "long"; break;
                case TypeCode.Single: typeName = "float"; break;
                case TypeCode.Double: typeName = "double"; break;
                case TypeCode.Decimal: typeName = "decimal"; break;
                case TypeCode.DateTime: typeName = "dateTime"; break;
                case TypeCode.Byte: typeName = "unsignedByte"; break;
                case TypeCode.SByte: typeName = "byte"; break;
                case TypeCode.UInt16: typeName = "unsignedShort"; break;
                case TypeCode.UInt32: typeName = "unsignedInt"; break;
                case TypeCode.UInt64: typeName = "unsignedLong"; break;
                case TypeCode.Char:
                    typeName = "char";
                    typeNs = UrtTypes.Namespace;
                    break;
                default:
                    if (type == typeof(XmlQualifiedName)) typeName = "QName";
                    else if (type == typeof(byte[])) typeName = "base64Binary";
                    else if (type == typeof(Guid))
                    {
                        typeName = "guid";
                        typeNs = UrtTypes.Namespace;
                    }
                    else if (type == typeof(TimeSpan))
                    {
                        typeName = "TimeSpan";
                        typeNs = UrtTypes.Namespace;
                    }
                    else if (type == typeof(XmlNode[]))
                    {
                        typeName = Soap.UrType;
                    }
                    else
                        return null;
                    break;
            }
            return new XmlQualifiedName(typeName, typeNs);
        }

        protected void WriteTypedPrimitive(string name, string ns, object o, bool xsiType)
        {
            string value = null;
            string type;
            string typeNs = XmlSchema.Namespace;
            bool writeRaw = true;
            bool writeDirect = false;
            Type t = o.GetType();
            bool wroteStartElement = false;

            switch (t.GetTypeCode())
            {
                case TypeCode.String:
                    value = (string)o;
                    type = "string";
                    writeRaw = false;
                    break;
                case TypeCode.Int32:
                    value = XmlConvert.ToString((int)o);
                    type = "int";
                    break;
                case TypeCode.Boolean:
                    value = XmlConvert.ToString((bool)o);
                    type = "boolean";
                    break;
                case TypeCode.Int16:
                    value = XmlConvert.ToString((short)o);
                    type = "short";
                    break;
                case TypeCode.Int64:
                    value = XmlConvert.ToString((long)o);
                    type = "long";
                    break;
                case TypeCode.Single:
                    value = XmlConvert.ToString((float)o);
                    type = "float";
                    break;
                case TypeCode.Double:
                    value = XmlConvert.ToString((double)o);
                    type = "double";
                    break;
                case TypeCode.Decimal:
                    value = XmlConvert.ToString((decimal)o);
                    type = "decimal";
                    break;
                case TypeCode.DateTime:
                    value = FromDateTime((DateTime)o);
                    type = "dateTime";
                    break;
                case TypeCode.Char:
                    value = FromChar((char)o);
                    type = "char";
                    typeNs = UrtTypes.Namespace;
                    break;
                case TypeCode.Byte:
                    value = XmlConvert.ToString((byte)o);
                    type = "unsignedByte";
                    break;
                case TypeCode.SByte:
                    value = XmlConvert.ToString((sbyte)o);
                    type = "byte";
                    break;
                case TypeCode.UInt16:
                    value = XmlConvert.ToString((UInt16)o);
                    type = "unsignedShort";
                    break;
                case TypeCode.UInt32:
                    value = XmlConvert.ToString((UInt32)o);
                    type = "unsignedInt";
                    break;
                case TypeCode.UInt64:
                    value = XmlConvert.ToString((UInt64)o);
                    type = "unsignedLong";
                    break;

                default:
                    if (t == typeof(XmlQualifiedName))
                    {
                        type = "QName";
                        // need to write start element ahead of time to establish context
                        // for ns definitions by FromXmlQualifiedName
                        wroteStartElement = true;
                        if (name == null)
                            _w.WriteStartElement(type, typeNs);
                        else
                            _w.WriteStartElement(name, ns);
                        value = FromXmlQualifiedName((XmlQualifiedName)o, false);
                    }
                    else if (t == typeof(byte[]))
                    {
                        value = String.Empty;
                        writeDirect = true;
                        type = "base64Binary";
                    }
                    else if (t == typeof(Guid))
                    {
                        value = XmlConvert.ToString((Guid)o);
                        type = "guid";
                        typeNs = UrtTypes.Namespace;
                    }
                    else if (t == typeof(TimeSpan))
                    {
                        value = XmlConvert.ToString((TimeSpan)o);
                        type = "TimeSpan";
                        typeNs = UrtTypes.Namespace;
                    }
                    else if (typeof(XmlNode[]).IsAssignableFrom(t))
                    {
                        if (name == null)
                            _w.WriteStartElement(Soap.UrType, XmlSchema.Namespace);
                        else
                            _w.WriteStartElement(name, ns);

                        XmlNode[] xmlNodes = (XmlNode[])o;
                        for (int i = 0; i < xmlNodes.Length; i++)
                        {
                            if (xmlNodes[i] == null)
                                continue;
                            xmlNodes[i].WriteTo(_w);
                        }
                        _w.WriteEndElement();
                        return;
                    }
                    else
                        throw CreateUnknownTypeException(t);
                    break;
            }
            if (!wroteStartElement)
            {
                if (name == null)
                    _w.WriteStartElement(type, typeNs);
                else
                    _w.WriteStartElement(name, ns);
            }

            if (xsiType) WriteXsiType(type, typeNs);

            if (value == null)
            {
                _w.WriteAttributeString("nil", XmlSchema.InstanceNamespace, "true");
            }
            else if (writeDirect)
            {
                // only one type currently writes directly to XML stream
                XmlCustomFormatter.WriteArrayBase64(_w, (byte[])o, 0, ((byte[])o).Length);
            }
            else if (writeRaw)
            {
                _w.WriteRaw(value);
            }
            else
                _w.WriteString(value);
            _w.WriteEndElement();
        }

        private string GetQualifiedName(string name, string ns)
        {
            if (ns == null || ns.Length == 0) return name;
            string prefix = _w.LookupPrefix(ns);
            if (prefix == null)
            {
                if (ns == XmlReservedNs.NsXml)
                {
                    prefix = "xml";
                }
                else
                {
                    prefix = NextPrefix();
                    WriteAttribute("xmlns", prefix, null, ns);
                }
            }
            else if (prefix.Length == 0)
            {
                return name;
            }
            return prefix + ":" + name;
        }

        protected string FromXmlQualifiedName(XmlQualifiedName xmlQualifiedName)
        {
            return FromXmlQualifiedName(xmlQualifiedName, true);
        }

        protected string FromXmlQualifiedName(XmlQualifiedName xmlQualifiedName, bool ignoreEmpty)
        {
            if (xmlQualifiedName == null) return null;
            if (xmlQualifiedName.IsEmpty && ignoreEmpty) return null;
            return GetQualifiedName(EscapeName ? XmlConvert.EncodeLocalName(xmlQualifiedName.Name) : xmlQualifiedName.Name, xmlQualifiedName.Namespace);
        }

        protected void WriteStartElement(string name)
        {
            WriteStartElement(name, null, null, false, null);
        }

        protected void WriteStartElement(string name, string ns)
        {
            WriteStartElement(name, ns, null, false, null);
        }

        protected void WriteStartElement(string name, string ns, bool writePrefixed)
        {
            WriteStartElement(name, ns, null, writePrefixed, null);
        }

        protected void WriteStartElement(string name, string ns, object o)
        {
            WriteStartElement(name, ns, o, false, null);
        }

        protected void WriteStartElement(string name, string ns, object o, bool writePrefixed)
        {
            WriteStartElement(name, ns, o, writePrefixed, null);
        }

        protected void WriteStartElement(string name, string ns, object o, bool writePrefixed, XmlSerializerNamespaces xmlns)
        {
            if (o != null && _objectsInUse != null)
            {
                if (_objectsInUse.ContainsKey(o)) throw new InvalidOperationException(SR.Format(SR.XmlCircularReference, o.GetType().FullName));
                _objectsInUse.Add(o, o);
            }

            string prefix = null;
            bool needEmptyDefaultNamespace = false;
            if (_namespaces != null)
            {
                foreach (string alias in _namespaces.Namespaces.Keys)
                {
                    string aliasNs = (string)_namespaces.Namespaces[alias];

                    if (alias.Length > 0 && aliasNs == ns)
                        prefix = alias;
                    if (alias.Length == 0)
                    {
                        if (aliasNs == null || aliasNs.Length == 0)
                            needEmptyDefaultNamespace = true;
                        if (ns != aliasNs)
                            writePrefixed = true;
                    }
                }
                _usedPrefixes = ListUsedPrefixes(_namespaces.Namespaces, _aliasBase);
            }
            if (writePrefixed && prefix == null && ns != null && ns.Length > 0)
            {
                prefix = _w.LookupPrefix(ns);
                if (prefix == null || prefix.Length == 0)
                {
                    prefix = NextPrefix();
                }
            }
            if (prefix == null && xmlns != null)
            {
                prefix = xmlns.LookupPrefix(ns);
            }
            if (needEmptyDefaultNamespace && prefix == null && ns != null && ns.Length != 0)
                prefix = NextPrefix();
            _w.WriteStartElement(prefix, name, ns);
            if (_namespaces != null)
            {
                foreach (string alias in _namespaces.Namespaces.Keys)
                {
                    string aliasNs = (string)_namespaces.Namespaces[alias];
                    if (alias.Length == 0 && (aliasNs == null || aliasNs.Length == 0))
                        continue;
                    if (aliasNs == null || aliasNs.Length == 0)
                    {
                        if (alias.Length > 0)
                            throw new InvalidOperationException(SR.Format(SR.XmlInvalidXmlns, alias));
                        WriteAttribute(nameof(xmlns), alias, null, aliasNs);
                    }
                    else
                    {
                        if (_w.LookupPrefix(aliasNs) == null)
                        {
                            // write the default namespace declaration only if we have not written it already, over wise we just ignore one provided by the user
                            if (prefix == null && alias.Length == 0)
                                break;
                            WriteAttribute(nameof(xmlns), alias, null, aliasNs);
                        }
                    }
                }
            }
            WriteNamespaceDeclarations(xmlns);
        }

        private HashSet<int> ListUsedPrefixes(Dictionary<string, string> nsList, string prefix)
        {
            var qnIndexes = new HashSet<int>();
            int prefixLength = prefix.Length;
            const string MaxInt32 = "2147483647";
            foreach (string alias in _namespaces.Namespaces.Keys)
            {
                string name;
                if (alias.Length > prefixLength)
                {
                    name = alias;
                    if (name.Length > prefixLength && name.Length <= prefixLength + MaxInt32.Length && name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        bool numeric = true;
                        for (int j = prefixLength; j < name.Length; j++)
                        {
                            if (!Char.IsDigit(name, j))
                            {
                                numeric = false;
                                break;
                            }
                        }
                        if (numeric)
                        {
                            Int64 index = Int64.Parse(name.Substring(prefixLength), NumberStyles.Integer, CultureInfo.InvariantCulture);
                            if (index <= Int32.MaxValue)
                            {
                                Int32 newIndex = (Int32)index;
                                qnIndexes.Add(newIndex);
                            }
                        }
                    }
                }
            }
            if (qnIndexes.Count > 0)
            {
                return qnIndexes;
            }
            return null;
        }

        protected void WriteNullTagEncoded(string name)
        {
            WriteNullTagEncoded(name, null);
        }

        protected void WriteNullTagEncoded(string name, string ns)
        {
            if (name == null || name.Length == 0)
                return;
            WriteStartElement(name, ns, null, true);
            _w.WriteAttributeString("nil", XmlSchema.InstanceNamespace, "true");
            _w.WriteEndElement();
        }

        protected void WriteNullTagLiteral(string name)
        {
            WriteNullTagLiteral(name, null);
        }

        protected void WriteNullTagLiteral(string name, string ns)
        {
            if (name == null || name.Length == 0)
                return;
            WriteStartElement(name, ns, null, false);
            _w.WriteAttributeString("nil", XmlSchema.InstanceNamespace, "true");
            _w.WriteEndElement();
        }

        protected void WriteEmptyTag(string name)
        {
            WriteEmptyTag(name, null);
        }

        protected void WriteEmptyTag(string name, string ns)
        {
            if (name == null || name.Length == 0)
                return;
            WriteStartElement(name, ns, null, false);
            _w.WriteEndElement();
        }

        protected void WriteEndElement()
        {
            _w.WriteEndElement();
        }

        protected void WriteEndElement(object o)
        {
            _w.WriteEndElement();

            if (o != null && _objectsInUse != null)
            {
#if DEBUG
                // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                if (!_objectsInUse.ContainsKey(o)) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "missing stack object of type " + o.GetType().FullName));
#endif

                _objectsInUse.Remove(o);
            }
        }

        protected void WriteSerializable(IXmlSerializable serializable, string name, string ns, bool isNullable)
        {
            WriteSerializable(serializable, name, ns, isNullable, true);
        }

        protected void WriteSerializable(IXmlSerializable serializable, string name, string ns, bool isNullable, bool wrapped)
        {
            if (serializable == null)
            {
                if (isNullable) WriteNullTagLiteral(name, ns);
                return;
            }
            if (wrapped)
            {
                _w.WriteStartElement(name, ns);
            }
            serializable.WriteXml(_w);
            if (wrapped)
            {
                _w.WriteEndElement();
            }
        }

        protected void WriteNullableStringEncoded(string name, string ns, string value, XmlQualifiedName xsiType)
        {
            if (value == null)
                WriteNullTagEncoded(name, ns);
            else
                WriteElementString(name, ns, value, xsiType);
        }

        protected void WriteNullableStringLiteral(string name, string ns, string value)
        {
            if (value == null)
                WriteNullTagLiteral(name, ns);
            else
                WriteElementString(name, ns, value, null);
        }


        protected void WriteNullableStringEncodedRaw(string name, string ns, string value, XmlQualifiedName xsiType)
        {
            if (value == null)
                WriteNullTagEncoded(name, ns);
            else
                WriteElementStringRaw(name, ns, value, xsiType);
        }

        protected void WriteNullableStringEncodedRaw(string name, string ns, byte[] value, XmlQualifiedName xsiType)
        {
            if (value == null)
                WriteNullTagEncoded(name, ns);
            else
                WriteElementStringRaw(name, ns, value, xsiType);
        }

        protected void WriteNullableStringLiteralRaw(string name, string ns, string value)
        {
            if (value == null)
                WriteNullTagLiteral(name, ns);
            else
                WriteElementStringRaw(name, ns, value, null);
        }

        protected void WriteNullableStringLiteralRaw(string name, string ns, byte[] value)
        {
            if (value == null)
                WriteNullTagLiteral(name, ns);
            else
                WriteElementStringRaw(name, ns, value, null);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected void WriteNullableQualifiedNameEncoded(string name, string ns, XmlQualifiedName value, XmlQualifiedName xsiType)
        {
            if (value == null)
                WriteNullTagEncoded(name, ns);
            else
                WriteElementQualifiedName(name, ns, value, xsiType);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected void WriteNullableQualifiedNameLiteral(string name, string ns, XmlQualifiedName value)
        {
            if (value == null)
                WriteNullTagLiteral(name, ns);
            else
                WriteElementQualifiedName(name, ns, value, null);
        }


        protected void WriteElementEncoded(XmlNode node, string name, string ns, bool isNullable, bool any)
        {
            if (node == null)
            {
                if (isNullable) WriteNullTagEncoded(name, ns);
                return;
            }
            WriteElement(node, name, ns, isNullable, any);
        }

        protected void WriteElementLiteral(XmlNode node, string name, string ns, bool isNullable, bool any)
        {
            if (node == null)
            {
                if (isNullable) WriteNullTagLiteral(name, ns);
                return;
            }
            WriteElement(node, name, ns, isNullable, any);
        }

        private void WriteElement(XmlNode node, string name, string ns, bool isNullable, bool any)
        {
            if (typeof(XmlAttribute).IsAssignableFrom(node.GetType()))
                throw new InvalidOperationException(SR.XmlNoAttributeHere);
            if (node is XmlDocument)
            {
                node = ((XmlDocument)node).DocumentElement;
                if (node == null)
                {
                    if (isNullable) WriteNullTagEncoded(name, ns);
                    return;
                }
            }
            if (any)
            {
                if (node is XmlElement && name != null && name.Length > 0)
                {
                    // need to check against schema
                    if (node.LocalName != name || node.NamespaceURI != ns)
                        throw new InvalidOperationException(SR.Format(SR.XmlElementNameMismatch, node.LocalName, node.NamespaceURI, name, ns));
                }
            }
            else
                _w.WriteStartElement(name, ns);

            node.WriteTo(_w);

            if (!any)
                _w.WriteEndElement();
        }

        protected Exception CreateUnknownTypeException(object o)
        {
            return CreateUnknownTypeException(o.GetType());
        }

        protected Exception CreateUnknownTypeException(Type type)
        {
            if (typeof(IXmlSerializable).IsAssignableFrom(type)) return new InvalidOperationException(SR.Format(SR.XmlInvalidSerializable, type.FullName));
            TypeDesc typeDesc = new TypeScope().GetTypeDesc(type);
            if (!typeDesc.IsStructLike) return new InvalidOperationException(SR.Format(SR.XmlInvalidUseOfType, type.FullName));
            return new InvalidOperationException(SR.Format(SR.XmlUnxpectedType, type.FullName));
        }

        protected Exception CreateMismatchChoiceException(string value, string elementName, string enumValue)
        {
            // Value of {0} mismatches the type of {1}, you need to set it to {2}.
            return new InvalidOperationException(SR.Format(SR.XmlChoiceMismatchChoiceException, elementName, value, enumValue));
        }

        protected Exception CreateUnknownAnyElementException(string name, string ns)
        {
            return new InvalidOperationException(SR.Format(SR.XmlUnknownAnyElement, name, ns));
        }

        protected Exception CreateInvalidChoiceIdentifierValueException(string type, string identifier)
        {
            return new InvalidOperationException(SR.Format(SR.XmlInvalidChoiceIdentifierValue, type, identifier));
        }

        protected Exception CreateChoiceIdentifierValueException(string value, string identifier, string name, string ns)
        {
            // XmlChoiceIdentifierMismatch=Value '{0}' of the choice identifier '{1}' does not match element '{2}' from namespace '{3}'.
            return new InvalidOperationException(SR.Format(SR.XmlChoiceIdentifierMismatch, value, identifier, name, ns));
        }

        protected Exception CreateInvalidEnumValueException(object value, string typeName)
        {
            return new InvalidOperationException(SR.Format(SR.XmlUnknownConstant, value, typeName));
        }

        protected Exception CreateInvalidAnyTypeException(object o)
        {
            return CreateInvalidAnyTypeException(o.GetType());
        }

        protected Exception CreateInvalidAnyTypeException(Type type)
        {
            return new InvalidOperationException(SR.Format(SR.XmlIllegalAnyElement, type.FullName));
        }

        protected void WriteReferencingElement(string n, string ns, object o)
        {
            WriteReferencingElement(n, ns, o, false);
        }

        protected void WriteReferencingElement(string n, string ns, object o, bool isNullable)
        {
            if (o == null)
            {
                if (isNullable) WriteNullTagEncoded(n, ns);
                return;
            }
            WriteStartElement(n, ns, null, true);
            if (_soap12)
                _w.WriteAttributeString("ref", Soap12.Encoding, GetId(o, true));
            else
                _w.WriteAttributeString("href", "#" + GetId(o, true));
            _w.WriteEndElement();
        }

        private bool IsIdDefined(object o)
        {
            if (_references != null) return _references.Contains(o);
            else return false;
        }

        private string GetId(object o, bool addToReferencesList)
        {
            if (_references == null)
            {
                _references = new Hashtable();
                _referencesToWrite = new ArrayList();
            }
            string id = (string)_references[o];
            if (id == null)
            {
                id = _idBase + "id" + (++_nextId).ToString(CultureInfo.InvariantCulture);
                _references.Add(o, id);
                if (addToReferencesList) _referencesToWrite.Add(o);
            }
            return id;
        }

        protected void WriteId(object o)
        {
            WriteId(o, true);
        }

        private void WriteId(object o, bool addToReferencesList)
        {
            if (_soap12)
                _w.WriteAttributeString("id", Soap12.Encoding, GetId(o, addToReferencesList));
            else
                _w.WriteAttributeString("id", GetId(o, addToReferencesList));
        }

        protected void WriteXmlAttribute(XmlNode node)
        {
            WriteXmlAttribute(node, null);
        }

        protected void WriteXmlAttribute(XmlNode node, object container)
        {
            XmlAttribute attr = node as XmlAttribute;
            if (attr == null) throw new InvalidOperationException(SR.XmlNeedAttributeHere);
            if (attr.Value != null)
            {
                if (attr.NamespaceURI == Wsdl.Namespace && attr.LocalName == Wsdl.ArrayType)
                {
                    string dims;
                    XmlQualifiedName qname = TypeScope.ParseWsdlArrayType(attr.Value, out dims, (container is XmlSchemaObject) ? (XmlSchemaObject)container : null);

                    string value = FromXmlQualifiedName(qname, true) + dims;

                    //<xsd:attribute xmlns:q3="s0" wsdl:arrayType="q3:FoosBase[]" xmlns:q4="http://schemas.xmlsoap.org/soap/encoding/" ref="q4:arrayType" />
                    WriteAttribute(Wsdl.ArrayType, Wsdl.Namespace, value);
                }
                else
                {
                    WriteAttribute(attr.Name, attr.NamespaceURI, attr.Value);
                }
            }
        }

        protected void WriteAttribute(string localName, string ns, string value)
        {
            if (value == null) return;
            if (localName == "xmlns" || localName.StartsWith("xmlns:", StringComparison.Ordinal))
            {
                ;
            }
            else
            {
                int colon = localName.IndexOf(':');
                if (colon < 0)
                {
                    if (ns == XmlReservedNs.NsXml)
                    {
                        string prefix = _w.LookupPrefix(ns);
                        if (prefix == null || prefix.Length == 0)
                            prefix = "xml";
                        _w.WriteAttributeString(prefix, localName, ns, value);
                    }
                    else
                    {
                        _w.WriteAttributeString(localName, ns, value);
                    }
                }
                else
                {
                    string prefix = localName.Substring(0, colon);
                    _w.WriteAttributeString(prefix, localName.Substring(colon + 1), ns, value);
                }
            }
        }

        protected void WriteAttribute(string localName, string ns, byte[] value)
        {
            if (value == null) return;
            if (localName == "xmlns" || localName.StartsWith("xmlns:", StringComparison.Ordinal))
            {
                ;
            }
            else
            {
                int colon = localName.IndexOf(':');
                if (colon < 0)
                {
                    if (ns == XmlReservedNs.NsXml)
                    {
                        string prefix = _w.LookupPrefix(ns);
                        if (prefix == null || prefix.Length == 0)
                            prefix = "xml";
                        _w.WriteStartAttribute("xml", localName, ns);
                    }
                    else
                    {
                        _w.WriteStartAttribute(null, localName, ns);
                    }
                }
                else
                {
                    string prefix = _w.LookupPrefix(ns);
                    _w.WriteStartAttribute(prefix, localName.Substring(colon + 1), ns);
                }
                XmlCustomFormatter.WriteArrayBase64(_w, value, 0, value.Length);
                _w.WriteEndAttribute();
            }
        }

        protected void WriteAttribute(string localName, string value)
        {
            if (value == null) return;
            _w.WriteAttributeString(localName, null, value);
        }

        protected void WriteAttribute(string localName, byte[] value)
        {
            if (value == null) return;

            _w.WriteStartAttribute(null, localName, (string)null);
            XmlCustomFormatter.WriteArrayBase64(_w, value, 0, value.Length);
            _w.WriteEndAttribute();
        }

        protected void WriteAttribute(string prefix, string localName, string ns, string value)
        {
            if (value == null) return;
            _w.WriteAttributeString(prefix, localName, null, value);
        }

        protected void WriteValue(string value)
        {
            if (value == null) return;
            _w.WriteString(value);
        }

        protected void WriteValue(byte[] value)
        {
            if (value == null) return;
            XmlCustomFormatter.WriteArrayBase64(_w, value, 0, value.Length);
        }

        protected void WriteStartDocument()
        {
            if (_w.WriteState == WriteState.Start)
            {
                _w.WriteStartDocument();
            }
        }

        protected void WriteElementString(String localName, String value)
        {
            WriteElementString(localName, null, value, null);
        }

        protected void WriteElementString(String localName, String ns, String value)
        {
            WriteElementString(localName, ns, value, null);
        }

        protected void WriteElementString(String localName, String value, XmlQualifiedName xsiType)
        {
            WriteElementString(localName, null, value, xsiType);
        }

        protected void WriteElementString(String localName, String ns, String value, XmlQualifiedName xsiType)
        {
            if (value == null) return;
            if (xsiType == null)
                _w.WriteElementString(localName, ns, value);
            else
            {
                _w.WriteStartElement(localName, ns);
                WriteXsiType(xsiType.Name, xsiType.Namespace);
                _w.WriteString(value);
                _w.WriteEndElement();
            }
        }

        protected void WriteElementStringRaw(String localName, String value)
        {
            WriteElementStringRaw(localName, null, value, null);
        }

        protected void WriteElementStringRaw(String localName, byte[] value)
        {
            WriteElementStringRaw(localName, null, value, null);
        }

        protected void WriteElementStringRaw(String localName, String ns, String value)
        {
            WriteElementStringRaw(localName, ns, value, null);
        }

        protected void WriteElementStringRaw(String localName, String ns, byte[] value)
        {
            WriteElementStringRaw(localName, ns, value, null);
        }

        protected void WriteElementStringRaw(String localName, String value, XmlQualifiedName xsiType)
        {
            WriteElementStringRaw(localName, null, value, xsiType);
        }

        protected void WriteElementStringRaw(String localName, byte[] value, XmlQualifiedName xsiType)
        {
            WriteElementStringRaw(localName, null, value, xsiType);
        }

        protected void WriteElementStringRaw(String localName, String ns, String value, XmlQualifiedName xsiType)
        {
            if (value == null) return;
            _w.WriteStartElement(localName, ns);
            if (xsiType != null)
                WriteXsiType(xsiType.Name, xsiType.Namespace);
            _w.WriteRaw(value);
            _w.WriteEndElement();
        }

        protected void WriteElementStringRaw(String localName, String ns, byte[] value, XmlQualifiedName xsiType)
        {
            if (value == null) return;
            _w.WriteStartElement(localName, ns);
            if (xsiType != null)
                WriteXsiType(xsiType.Name, xsiType.Namespace);
            XmlCustomFormatter.WriteArrayBase64(_w, value, 0, value.Length);
            _w.WriteEndElement();
        }

        protected void WriteRpcResult(string name, string ns)
        {
            if (!_soap12) return;
            WriteElementQualifiedName(Soap12.RpcResult, Soap12.RpcNamespace, new XmlQualifiedName(name, ns), null);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected void WriteElementQualifiedName(String localName, XmlQualifiedName value)
        {
            WriteElementQualifiedName(localName, null, value, null);
        }

        protected void WriteElementQualifiedName(string localName, XmlQualifiedName value, XmlQualifiedName xsiType)
        {
            WriteElementQualifiedName(localName, null, value, xsiType);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected void WriteElementQualifiedName(String localName, String ns, XmlQualifiedName value)
        {
            WriteElementQualifiedName(localName, ns, value, null);
        }

        protected void WriteElementQualifiedName(string localName, string ns, XmlQualifiedName value, XmlQualifiedName xsiType)
        {
            if (value == null) return;
            if (value.Namespace == null || value.Namespace.Length == 0)
            {
                WriteStartElement(localName, ns, null, true);
                WriteAttribute("xmlns", "");
            }
            else
                _w.WriteStartElement(localName, ns);
            if (xsiType != null)
                WriteXsiType(xsiType.Name, xsiType.Namespace);
            _w.WriteString(FromXmlQualifiedName(value, false));
            _w.WriteEndElement();
        }

        protected void AddWriteCallback(Type type, string typeName, string typeNs, XmlSerializationWriteCallback callback)
        {
            TypeEntry entry = new TypeEntry();
            entry.typeName = typeName;
            entry.typeNs = typeNs;
            entry.type = type;
            entry.callback = callback;
            _typeEntries[type] = entry;
        }

        private void WriteArray(string name, string ns, object o, Type type)
        {
            Type elementType = TypeScope.GetArrayElementType(type, null);
            string typeName;
            string typeNs;

            StringBuilder arrayDims = new StringBuilder();
            if (!_soap12)
            {
                while ((elementType.IsArray || typeof(IEnumerable).IsAssignableFrom(elementType)) && GetPrimitiveTypeName(elementType, false) == null)
                {
                    elementType = TypeScope.GetArrayElementType(elementType, null);
                    arrayDims.Append("[]");
                }
            }

            if (elementType == typeof(object))
            {
                typeName = Soap.UrType;
                typeNs = XmlSchema.Namespace;
            }
            else
            {
                TypeEntry entry = GetTypeEntry(elementType);
                if (entry != null)
                {
                    typeName = entry.typeName;
                    typeNs = entry.typeNs;
                }
                else if (_soap12)
                {
                    XmlQualifiedName qualName = GetPrimitiveTypeName(elementType, false);
                    if (qualName != null)
                    {
                        typeName = qualName.Name;
                        typeNs = qualName.Namespace;
                    }
                    else
                    {
                        Type elementBaseType = elementType.BaseType;
                        while (elementBaseType != null)
                        {
                            entry = GetTypeEntry(elementBaseType);
                            if (entry != null) break;
                            elementBaseType = elementBaseType.BaseType;
                        }
                        if (entry != null)
                        {
                            typeName = entry.typeName;
                            typeNs = entry.typeNs;
                        }
                        else
                        {
                            typeName = Soap.UrType;
                            typeNs = XmlSchema.Namespace;
                        }
                    }
                }
                else
                {
                    XmlQualifiedName qualName = GetPrimitiveTypeName(elementType);
                    typeName = qualName.Name;
                    typeNs = qualName.Namespace;
                }
            }

            if (arrayDims.Length > 0)
                typeName = typeName + arrayDims.ToString();

            if (_soap12 && name != null && name.Length > 0)
                WriteStartElement(name, ns, null, false);
            else
                WriteStartElement(Soap.Array, Soap.Encoding, null, true);

            WriteId(o, false);

            if (type.IsArray)
            {
                Array a = (Array)o;
                int arrayLength = a.Length;
                if (_soap12)
                {
                    _w.WriteAttributeString("itemType", Soap12.Encoding, GetQualifiedName(typeName, typeNs));
                    _w.WriteAttributeString("arraySize", Soap12.Encoding, arrayLength.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    _w.WriteAttributeString("arrayType", Soap.Encoding, GetQualifiedName(typeName, typeNs) + "[" + arrayLength.ToString(CultureInfo.InvariantCulture) + "]");
                }
                for (int i = 0; i < arrayLength; i++)
                {
                    WritePotentiallyReferencingElement("Item", "", a.GetValue(i), elementType, false, true);
                }
            }
            else
            {
#if DEBUG
                // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                if (!typeof(IEnumerable).IsAssignableFrom(type)) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "not array like type " + type.FullName));
#endif

                int arrayLength = typeof(ICollection).IsAssignableFrom(type) ? ((ICollection)o).Count : -1;
                if (_soap12)
                {
                    _w.WriteAttributeString("itemType", Soap12.Encoding, GetQualifiedName(typeName, typeNs));
                    if (arrayLength >= 0)
                        _w.WriteAttributeString("arraySize", Soap12.Encoding, arrayLength.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    string brackets = arrayLength >= 0 ? "[" + arrayLength + "]" : "[]";
                    _w.WriteAttributeString("arrayType", Soap.Encoding, GetQualifiedName(typeName, typeNs) + brackets);
                }
                IEnumerator e = ((IEnumerable)o).GetEnumerator();
                if (e != null)
                {
                    while (e.MoveNext())
                    {
                        WritePotentiallyReferencingElement("Item", "", e.Current, elementType, false, true);
                    }
                }
            }
            _w.WriteEndElement();
        }
        protected void WritePotentiallyReferencingElement(string n, string ns, object o)
        {
            WritePotentiallyReferencingElement(n, ns, o, null, false, false);
        }

        protected void WritePotentiallyReferencingElement(string n, string ns, object o, Type ambientType)
        {
            WritePotentiallyReferencingElement(n, ns, o, ambientType, false, false);
        }

        protected void WritePotentiallyReferencingElement(string n, string ns, object o, Type ambientType, bool suppressReference)
        {
            WritePotentiallyReferencingElement(n, ns, o, ambientType, suppressReference, false);
        }

        protected void WritePotentiallyReferencingElement(string n, string ns, object o, Type ambientType, bool suppressReference, bool isNullable)
        {
            if (o == null)
            {
                if (isNullable) WriteNullTagEncoded(n, ns);
                return;
            }
            Type t = o.GetType();
            if (t.GetTypeCode() == TypeCode.Object && !(o is Guid) && (t != typeof(XmlQualifiedName)) && !(o is XmlNode[]) && (t != typeof(byte[])))
            {
                if ((suppressReference || _soap12) && !IsIdDefined(o))
                {
                    WriteReferencedElement(n, ns, o, ambientType);
                }
                else
                {
                    if (n == null)
                    {
                        TypeEntry entry = GetTypeEntry(t);
                        WriteReferencingElement(entry.typeName, entry.typeNs, o, isNullable);
                    }
                    else
                        WriteReferencingElement(n, ns, o, isNullable);
                }
            }
            else
            {
                // Enums always write xsi:type, so don't write it again here.
                bool needXsiType = t != ambientType && !t.IsEnum;
                TypeEntry entry = GetTypeEntry(t);
                if (entry != null)
                {
                    if (n == null)
                        WriteStartElement(entry.typeName, entry.typeNs, null, true);
                    else
                        WriteStartElement(n, ns, null, true);

                    if (needXsiType) WriteXsiType(entry.typeName, entry.typeNs);
                    entry.callback(o);
                    _w.WriteEndElement();
                }
                else
                {
                    WriteTypedPrimitive(n, ns, o, needXsiType);
                }
            }
        }


        private void WriteReferencedElement(object o, Type ambientType)
        {
            WriteReferencedElement(null, null, o, ambientType);
        }

        private void WriteReferencedElement(string name, string ns, object o, Type ambientType)
        {
            if (name == null) name = String.Empty;
            Type t = o.GetType();
            if (t.IsArray || typeof(IEnumerable).IsAssignableFrom(t))
            {
                WriteArray(name, ns, o, t);
            }
            else
            {
                TypeEntry entry = GetTypeEntry(t);
                if (entry == null) throw CreateUnknownTypeException(t);
                WriteStartElement(name.Length == 0 ? entry.typeName : name, ns == null ? entry.typeNs : ns, null, true);
                WriteId(o, false);
                if (ambientType != t) WriteXsiType(entry.typeName, entry.typeNs);
                entry.callback(o);
                _w.WriteEndElement();
            }
        }

        private TypeEntry GetTypeEntry(Type t)
        {
            if (_typeEntries == null)
            {
                _typeEntries = new Hashtable();
                InitCallbacks();
            }
            return (TypeEntry)_typeEntries[t];
        }

        protected abstract void InitCallbacks();

        protected void WriteReferencedElements()
        {
            if (_referencesToWrite == null) return;

            for (int i = 0; i < _referencesToWrite.Count; i++)
            {
                WriteReferencedElement(_referencesToWrite[i], null);
            }
        }

        protected void TopLevelElement()
        {
            _objectsInUse = new Hashtable();
        }

        ///<internalonly/>
        protected void WriteNamespaceDeclarations(XmlSerializerNamespaces xmlns)
        {
            if (xmlns != null)
            {
                foreach (KeyValuePair<string, string> entry in xmlns.Namespaces)
                {
                    string prefix = (string)entry.Key;
                    string ns = (string)entry.Value;
                    if (_namespaces != null)
                    {
                        string oldNs;
                        if (_namespaces.Namespaces.TryGetValue(prefix, out oldNs) && oldNs != null && oldNs != ns)
                        {
                            throw new InvalidOperationException(SR.Format(SR.XmlDuplicateNs, prefix, ns));
                        }
                    }
                    string oldPrefix = (ns == null || ns.Length == 0) ? null : Writer.LookupPrefix(ns);

                    if (oldPrefix == null || oldPrefix != prefix)
                    {
                        WriteAttribute(nameof(xmlns), prefix, null, ns);
                    }
                }
            }
            _namespaces = null;
        }

        private string NextPrefix()
        {
            if (_usedPrefixes == null)
            {
                return _aliasBase + (++_tempNamespacePrefix);
            }
            while (_usedPrefixes.Contains(++_tempNamespacePrefix)) {; }
            return _aliasBase + _tempNamespacePrefix;
        }

        internal class TypeEntry
        {
            internal XmlSerializationWriteCallback callback;
            internal string typeNs;
            internal string typeName;
            internal Type type;
        }
    }


    ///<internalonly/>
#if XMLSERIALIZERGENERATOR
    internal delegate void XmlSerializationWriteCallback(object o);
#else
    public delegate void XmlSerializationWriteCallback(object o);
#endif


    internal static class DynamicAssemblies
    {
        private static ArrayList s_assembliesInConfig = new ArrayList();
        private static volatile Hashtable s_nameToAssemblyMap = new Hashtable();
        private static volatile Hashtable s_assemblyToNameMap = new Hashtable();
        private static Hashtable s_tableIsTypeDynamic = Hashtable.Synchronized(new Hashtable());

        // SxS: This method does not take any resource name and does not expose any resources to the caller.
        // It's OK to suppress the SxS warning.
        internal static bool IsTypeDynamic(Type type)
        {
            object oIsTypeDynamic = s_tableIsTypeDynamic[type];
            if (oIsTypeDynamic == null)
            {
                Assembly assembly = type.Assembly;
                bool isTypeDynamic = assembly.IsDynamic /*|| string.IsNullOrEmpty(assembly.Location)*/;
                if (!isTypeDynamic)
                {
                    if (type.IsArray)
                    {
                        isTypeDynamic = IsTypeDynamic(type.GetElementType());
                    }
                    else if (type.IsGenericType)
                    {
                        Type[] parameterTypes = type.GetGenericArguments();
                        if (parameterTypes != null)
                        {
                            for (int i = 0; i < parameterTypes.Length; i++)
                            {
                                Type parameterType = parameterTypes[i];
                                if (!(parameterType == null || parameterType.IsGenericParameter))
                                {
                                    isTypeDynamic = IsTypeDynamic(parameterType);
                                    if (isTypeDynamic)
                                        break;
                                }
                            }
                        }
                    }
                }
                s_tableIsTypeDynamic[type] = oIsTypeDynamic = isTypeDynamic;
            }
            return (bool)oIsTypeDynamic;
        }

#if XMLSERIALIZERGENERATOR
        internal static bool IsTypeDynamic(Type[] arguments)
        {
            foreach (Type t in arguments)
            {
                if (DynamicAssemblies.IsTypeDynamic(t))
                {
                    return true;
                }
            }
            return false;
        }

        internal static void Add(Assembly a)
        {
            lock (s_nameToAssemblyMap)
            {
                if (s_assemblyToNameMap[a] != null)
                {
                    //already added
                    return;
                }
                Assembly oldAssembly = s_nameToAssemblyMap[a.FullName] as Assembly;
                string key = null;
                if (oldAssembly == null)
                {
                    key = a.FullName;
                }
                else if (oldAssembly != a)
                {
                    //more than one assembly with same name
                    key = a.FullName + ", " + s_nameToAssemblyMap.Count;
                }
                if (key != null)
                {
                    s_nameToAssemblyMap.Add(key, a);
                    s_assemblyToNameMap.Add(a, key);
                }
            }
        }
#endif

        internal static Assembly Get(string fullName)
        {
            return s_nameToAssemblyMap != null ? (Assembly)s_nameToAssemblyMap[fullName] : null;
        }

#if XMLSERIALIZERGENERATOR
        internal static string GetName(Assembly a)
        {
            return s_assemblyToNameMap != null ? (string) s_assemblyToNameMap[a] : null;
        }
#endif
    }

#if XMLSERIALIZERGENERATOR
    internal class ReflectionAwareCodeGen
    {
        private const string hexDigits = "0123456789ABCDEF";
        private const string arrayMemberKey = "0";
        // reflectionVariables holds mapping between a reflection entity
        // referenced in the generated code (such as TypeInfo,
        // FieldInfo) and the variable which represent the entity (and
        // initialized before).
        // The types of reflection entity and corresponding key is
        // given below.
        // ----------------------------------------------------------------------------------
        // Entity           Key
        // ----------------------------------------------------------------------------------
        // Assembly         assembly.FullName
        // Type             CodeIdentifier.EscapedKeywords(type.FullName)
        // Field            fieldName+":"+CodeIdentifier.EscapedKeywords(containingType.FullName>)
        // Property         propertyName+":"+CodeIdentifier.EscapedKeywords(containingType.FullName)
        // ArrayAccessor    "0:"+CodeIdentifier.EscapedKeywords(typeof(Array).FullName)
        // MyCollectionAccessor     "0:"+CodeIdentifier.EscapedKeywords(typeof(MyCollection).FullName)
        // ----------------------------------------------------------------------------------
        private Hashtable _reflectionVariables = null;
        private int _nextReflectionVariableNumber = 0;
        private IndentedWriter _writer;
        internal ReflectionAwareCodeGen(IndentedWriter writer)
        {
            _writer = writer;
        }

        internal void WriteReflectionInit(TypeScope scope)
        {
            foreach (Type type in scope.Types)
            {
                TypeDesc typeDesc = scope.GetTypeDesc(type);
                if (typeDesc.UseReflection)
                    WriteTypeInfo(scope, typeDesc, type);
            }
        }

        private string WriteTypeInfo(TypeScope scope, TypeDesc typeDesc, Type type)
        {
            InitTheFirstTime();
            string typeFullName = typeDesc.CSharpName;
            string typeVariable = (string)_reflectionVariables[typeFullName];
            if (typeVariable != null)
                return typeVariable;

            if (type.IsArray)
            {
                typeVariable = GenerateVariableName("array", typeDesc.CSharpName);
                TypeDesc elementTypeDesc = typeDesc.ArrayElementTypeDesc;
                if (elementTypeDesc.UseReflection)
                {
                    string elementTypeVariable = WriteTypeInfo(scope, elementTypeDesc, scope.GetTypeFromTypeDesc(elementTypeDesc));
                    _writer.WriteLine("static " + typeof(Type).FullName + " " + typeVariable + " = " + elementTypeVariable + ".MakeArrayType();");
                }
                else
                {
                    string assemblyVariable = WriteAssemblyInfo(type);
                    _writer.Write("static " + typeof(Type).FullName + " " + typeVariable + " = " + assemblyVariable + ".GetType(");
                    WriteQuotedCSharpString(type.FullName);
                    _writer.WriteLine(");");
                }
            }
            else
            {
                typeVariable = GenerateVariableName(nameof(type), typeDesc.CSharpName);

                Type parameterType = Nullable.GetUnderlyingType(type);
                if (parameterType != null)
                {
                    string parameterTypeVariable = WriteTypeInfo(scope, scope.GetTypeDesc(parameterType), parameterType);
                    _writer.WriteLine("static " + typeof(Type).FullName + " " + typeVariable + " = typeof(System.Nullable<>).MakeGenericType(new " + typeof(Type).FullName + "[] {" + parameterTypeVariable + "});");
                }
                else
                {
                    string assemblyVariable = WriteAssemblyInfo(type);
                    _writer.Write("static " + typeof(Type).FullName + " " + typeVariable + " = " + assemblyVariable + ".GetType(");
                    WriteQuotedCSharpString(type.FullName);
                    _writer.WriteLine(");");
                }
            }

            _reflectionVariables.Add(typeFullName, typeVariable);

            TypeMapping mapping = scope.GetTypeMappingFromTypeDesc(typeDesc);
            if (mapping != null)
                WriteMappingInfo(mapping, typeVariable, type);
            if (typeDesc.IsCollection || typeDesc.IsEnumerable)
            {// Arrays use the generic item_Array
                TypeDesc elementTypeDesc = typeDesc.ArrayElementTypeDesc;
                if (elementTypeDesc.UseReflection)
                    WriteTypeInfo(scope, elementTypeDesc, scope.GetTypeFromTypeDesc(elementTypeDesc));
                WriteCollectionInfo(typeVariable, typeDesc, type);
            }
            return typeVariable;
        }

        private void InitTheFirstTime()
        {
            if (_reflectionVariables == null)
            {
                _reflectionVariables = new Hashtable();
                _writer.Write(String.Format(CultureInfo.InvariantCulture, s_helperClassesForUseReflection,
                    "object", "string", typeof(Type).FullName,
                    typeof(FieldInfo).FullName, typeof(PropertyInfo).FullName,
                    typeof(MemberInfo).FullName /*, typeof(MemberTypes).FullName*/));

                WriteDefaultIndexerInit(typeof(IList), typeof(Array).FullName, false, false);
            }
        }

        private void WriteMappingInfo(TypeMapping mapping, string typeVariable, Type type)
        {
            string typeFullName = mapping.TypeDesc.CSharpName;
            if (mapping is StructMapping)
            {
                StructMapping structMapping = mapping as StructMapping;
                for (int i = 0; i < structMapping.Members.Length; i++)
                {
                    MemberMapping member = structMapping.Members[i];
                    string memberVariable = WriteMemberInfo(type, typeFullName, typeVariable, member.Name);
                    if (member.CheckShouldPersist)
                    {
                        string memberName = "ShouldSerialize" + member.Name;
                        memberVariable = WriteMethodInfo(typeFullName, typeVariable, memberName, false);
                    }
                    if (member.CheckSpecified != SpecifiedAccessor.None)
                    {
                        string memberName = member.Name + "Specified";
                        memberVariable = WriteMemberInfo(type, typeFullName, typeVariable, memberName);
                    }
                    if (member.ChoiceIdentifier != null)
                    {
                        string memberName = member.ChoiceIdentifier.MemberName;
                        memberVariable = WriteMemberInfo(type, typeFullName, typeVariable, memberName);
                    }
                }
            }
            else if (mapping is EnumMapping)
            {
                FieldInfo[] enumFields = type.GetFields();
                for (int i = 0; i < enumFields.Length; i++)
                {
                    WriteMemberInfo(type, typeFullName, typeVariable, enumFields[i].Name);
                }
            }
        }
        private void WriteCollectionInfo(string typeVariable, TypeDesc typeDesc, Type type)
        {
            string typeFullName = CodeIdentifier.GetCSharpName(type);
            string elementTypeFullName = typeDesc.ArrayElementTypeDesc.CSharpName;
            bool elementUseReflection = typeDesc.ArrayElementTypeDesc.UseReflection;
            if (typeDesc.IsCollection)
            {
                WriteDefaultIndexerInit(type, typeFullName, typeDesc.UseReflection, elementUseReflection);
            }
            else if (typeDesc.IsEnumerable)
            {
                if (typeDesc.IsGenericInterface)
                {
                    WriteMethodInfo(typeFullName, typeVariable, "System.Collections.Generic.IEnumerable*", true);
                }
                else if (!typeDesc.IsPrivateImplementation)
                {
                    WriteMethodInfo(typeFullName, typeVariable, "GetEnumerator", true);
                }
            }
            WriteMethodInfo(typeFullName, typeVariable, "Add", false, GetStringForTypeof(elementTypeFullName, elementUseReflection));
        }

        private string WriteAssemblyInfo(Type type)
        {
            string assemblyFullName = type.Assembly.FullName;
            string assemblyVariable = (string)_reflectionVariables[assemblyFullName];
            if (assemblyVariable == null)
            {
                int iComma = assemblyFullName.IndexOf(',');
                string assemblyName = (iComma > -1) ? assemblyFullName.Substring(0, iComma) : assemblyFullName;
                assemblyVariable = GenerateVariableName("assembly", assemblyName);
                //writer.WriteLine("static "+ typeof(Assembly).FullName+" "+assemblyVariable+" = "+typeof(Assembly).FullName+".Load(");
                _writer.Write("static " + typeof(Assembly).FullName + " " + assemblyVariable + " = " + "ResolveDynamicAssembly(");
                WriteQuotedCSharpString(DynamicAssemblies.GetName(type.Assembly)/*assemblyFullName*/);
                _writer.WriteLine(");");
                _reflectionVariables.Add(assemblyFullName, assemblyVariable);
            }
            return assemblyVariable;
        }

        private string WriteMemberInfo(Type type, string escapedName, string typeVariable, string memberName)
        {
            MemberInfo[] memberInfos = type.GetMember(memberName);
            for (int i = 0; i < memberInfos.Length; i++)
            {
                if (memberInfos[i] is PropertyInfo)
                {
                    string propVariable = GenerateVariableName("prop", memberName);
                    _writer.Write("static XSPropInfo " + propVariable + " = new XSPropInfo(" + typeVariable + ", ");
                    WriteQuotedCSharpString(memberName);
                    _writer.WriteLine(");");
                    _reflectionVariables.Add(memberName + ":" + escapedName, propVariable);
                    return propVariable;
                }
                else if (memberInfos[i] is FieldInfo)
                {
                    string fieldVariable = GenerateVariableName("field", memberName);
                    _writer.Write("static XSFieldInfo " + fieldVariable + " = new XSFieldInfo(" + typeVariable + ", ");
                    WriteQuotedCSharpString(memberName);
                    _writer.WriteLine(");");
                    _reflectionVariables.Add(memberName + ":" + escapedName, fieldVariable);
                    return fieldVariable;
                }
            }
            throw new InvalidOperationException(SR.Format(SR.XmlSerializerUnsupportedType, memberInfos[0].ToString()));
        }

        private string WriteMethodInfo(string escapedName, string typeVariable, string memberName, bool isNonPublic, params string[] paramTypes)
        {
            string methodVariable = GenerateVariableName("method", memberName);
            _writer.Write("static " + typeof(MethodInfo).FullName + " " + methodVariable + " = " + typeVariable + ".GetMethod(");
            WriteQuotedCSharpString(memberName);
            _writer.Write(", ");

            string bindingFlags = typeof(BindingFlags).FullName;
            _writer.Write(bindingFlags);
            _writer.Write(".Public | ");
            _writer.Write(bindingFlags);
            _writer.Write(".Instance | ");
            _writer.Write(bindingFlags);
            _writer.Write(".Static");

            if (isNonPublic)
            {
                _writer.Write(" | ");
                _writer.Write(bindingFlags);
                _writer.Write(".NonPublic");
            }
            _writer.Write(", null, ");
            _writer.Write("new " + typeof(Type).FullName + "[] { ");
            for (int i = 0; i < paramTypes.Length; i++)
            {
                _writer.Write(paramTypes[i]);
                if (i < (paramTypes.Length - 1))
                    _writer.Write(", ");
            }
            _writer.WriteLine("}, null);");
            _reflectionVariables.Add(memberName + ":" + escapedName, methodVariable);
            return methodVariable;
        }

        private string WriteDefaultIndexerInit(Type type, string escapedName, bool collectionUseReflection, bool elementUseReflection)
        {
            string itemVariable = GenerateVariableName("item", escapedName);
            PropertyInfo defaultIndexer = TypeScope.GetDefaultIndexer(type, null);
            _writer.Write("static XSArrayInfo ");
            _writer.Write(itemVariable);
            _writer.Write("= new XSArrayInfo(");
            _writer.Write(GetStringForTypeof(CodeIdentifier.GetCSharpName(type), collectionUseReflection));
            _writer.Write(".GetProperty(");
            WriteQuotedCSharpString(defaultIndexer.Name);
            _writer.Write(",");
            //defaultIndexer.PropertyType is same as TypeDesc.ElementTypeDesc
            _writer.Write(GetStringForTypeof(CodeIdentifier.GetCSharpName(defaultIndexer.PropertyType), elementUseReflection));
            _writer.Write(",new ");
            _writer.Write(typeof(Type[]).FullName);
            _writer.WriteLine("{typeof(int)}));");
            _reflectionVariables.Add(arrayMemberKey + ":" + escapedName, itemVariable);
            return itemVariable;
        }

        private string GenerateVariableName(string prefix, string fullName)
        {
            ++_nextReflectionVariableNumber;
            return prefix + _nextReflectionVariableNumber + "_" +
                CodeIdentifier.MakeValidInternal(fullName.Replace('.', '_'));
        }
        internal string GetReflectionVariable(string typeFullName, string memberName)
        {
            string key;
            if (memberName == null)
                key = typeFullName;
            else
                key = memberName + ":" + typeFullName;
            return (string)_reflectionVariables[key];
        }


        internal string GetStringForMethodInvoke(string obj, string escapedTypeName, string methodName, bool useReflection, params string[] args)
        {
            StringBuilder sb = new StringBuilder();
            if (useReflection)
            {
                sb.Append(GetReflectionVariable(escapedTypeName, methodName));
                sb.Append(".Invoke(");
                sb.Append(obj);
                sb.Append(", new object[] {");
            }
            else
            {
                sb.Append(obj);
                sb.Append(".@");
                sb.Append(methodName);
                sb.Append("(");
            }
            for (int i = 0; i < args.Length; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(args[i]);
            }
            if (useReflection)
                sb.Append("})");
            else
                sb.Append(")");
            return sb.ToString();
        }

        internal string GetStringForEnumCompare(EnumMapping mapping, string memberName, bool useReflection)
        {
            if (!useReflection)
            {
                CodeIdentifier.CheckValidIdentifier(memberName);
                return mapping.TypeDesc.CSharpName + ".@" + memberName;
            }
            string memberAccess = GetStringForEnumMember(mapping.TypeDesc.CSharpName, memberName, useReflection);
            return GetStringForEnumLongValue(memberAccess, useReflection);
        }
        internal string GetStringForEnumLongValue(string variable, bool useReflection)
        {
            if (useReflection)
                return typeof(Convert).FullName + ".ToInt64(" + variable + ")";
            return "((" + typeof(long).FullName + ")" + variable + ")";
        }

        internal string GetStringForTypeof(string typeFullName, bool useReflection)
        {
            if (useReflection)
            {
                return GetReflectionVariable(typeFullName, null);
            }
            else
            {
                return "typeof(" + typeFullName + ")";
            }
        }
        internal string GetStringForMember(string obj, string memberName, TypeDesc typeDesc)
        {
            if (!typeDesc.UseReflection)
                return obj + ".@" + memberName;

            TypeDesc saveTypeDesc = typeDesc;
            while (typeDesc != null)
            {
                string typeFullName = typeDesc.CSharpName;
                string memberInfoName = GetReflectionVariable(typeFullName, memberName);
                if (memberInfoName != null)
                    return memberInfoName + "[" + obj + "]";
                // member may be part of the basetype 
                typeDesc = typeDesc.BaseTypeDesc;
                if (typeDesc != null && !typeDesc.UseReflection)
                    return "((" + typeDesc.CSharpName + ")" + obj + ").@" + memberName;
            }
            //throw GetReflectionVariableException(saveTypeDesc.CSharpName,memberName); 
            // NOTE, sowmys:Must never happen. If it does let the code
            // gen continue to help debugging what's gone wrong.
            // Eventually the compilation will fail.
            return "[" + obj + "]";
        }
        /*
        Exception GetReflectionVariableException(string typeFullName, string memberName){
            string key;
            if(memberName == null)
                key = typeFullName;
            else
                key = memberName+":"+typeFullName;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach(object varAvail in reflectionVariables.Keys){
                sb.Append(varAvail.ToString());
                sb.Append("\n");
            }
            return new Exception("No reflection variable for " + key + "\nAvailable keys\n"+sb.ToString());
        }*/

        internal string GetStringForEnumMember(string typeFullName, string memberName, bool useReflection)
        {
            if (!useReflection)
                return typeFullName + ".@" + memberName;

            string memberInfoName = GetReflectionVariable(typeFullName, memberName);
            return memberInfoName + "[null]";
        }
        internal string GetStringForArrayMember(string arrayName, string subscript, TypeDesc arrayTypeDesc)
        {
            if (!arrayTypeDesc.UseReflection)
            {
                return arrayName + "[" + subscript + "]";
            }
            string typeFullName = arrayTypeDesc.IsCollection ? arrayTypeDesc.CSharpName : typeof(Array).FullName;
            string arrayInfo = GetReflectionVariable(typeFullName, arrayMemberKey);
            return arrayInfo + "[" + arrayName + ", " + subscript + "]";
        }
        internal string GetStringForMethod(string obj, string typeFullName, string memberName, bool useReflection)
        {
            if (!useReflection)
                return obj + "." + memberName + "(";

            string memberInfoName = GetReflectionVariable(typeFullName, memberName);
            return memberInfoName + ".Invoke(" + obj + ", new object[]{";
        }
        internal string GetStringForCreateInstance(string escapedTypeName, bool useReflection, bool ctorInaccessible, bool cast)
        {
            return GetStringForCreateInstance(escapedTypeName, useReflection, ctorInaccessible, cast, string.Empty);
        }

        internal string GetStringForCreateInstance(string escapedTypeName, bool useReflection, bool ctorInaccessible, bool cast, string arg)
        {
            if (!useReflection && !ctorInaccessible)
                return "new " + escapedTypeName + "(" + arg + ")";
            return GetStringForCreateInstance(GetStringForTypeof(escapedTypeName, useReflection), cast && !useReflection ? escapedTypeName : null, ctorInaccessible, arg);
        }

        internal string GetStringForCreateInstance(string type, string cast, bool nonPublic, string arg)
        {
            StringBuilder createInstance = new StringBuilder();
            if (cast != null && cast.Length > 0)
            {
                createInstance.Append("(");
                createInstance.Append(cast);
                createInstance.Append(")");
            }
            createInstance.Append(typeof(Activator).FullName);
            createInstance.Append(".CreateInstance(");
            createInstance.Append(type);
            createInstance.Append(", ");
            string bindingFlags = typeof(BindingFlags).FullName;
            createInstance.Append(bindingFlags);
            createInstance.Append(".Instance | ");
            createInstance.Append(bindingFlags);
            createInstance.Append(".Public | ");
            createInstance.Append(bindingFlags);
            createInstance.Append(".CreateInstance");

            if (nonPublic)
            {
                createInstance.Append(" | ");
                createInstance.Append(bindingFlags);
                createInstance.Append(".NonPublic");
            }
            if (arg == null || arg.Length == 0)
            {
                createInstance.Append(", null, new object[0], null)");
            }
            else
            {
                createInstance.Append(", null, new object[] { ");
                createInstance.Append(arg);
                createInstance.Append(" }, null)");
            }
            return createInstance.ToString();
        }

        internal void WriteLocalDecl(string typeFullName, string variableName, string initValue, bool useReflection)
        {
            if (useReflection)
                typeFullName = "object";
            _writer.Write(typeFullName);
            _writer.Write(" ");
            _writer.Write(variableName);
            if (initValue != null)
            {
                _writer.Write(" = ");
                if (!useReflection && initValue != "null")
                {
                    _writer.Write("(" + typeFullName + ")");
                }
                _writer.Write(initValue);
            }
            _writer.WriteLine(";");
        }

        internal void WriteCreateInstance(string escapedName, string source, bool useReflection, bool ctorInaccessible)
        {
            _writer.Write(useReflection ? "object" : escapedName);
            _writer.Write(" ");
            _writer.Write(source);
            _writer.Write(" = ");
            _writer.Write(GetStringForCreateInstance(escapedName, useReflection, ctorInaccessible, !useReflection && ctorInaccessible));
            _writer.WriteLine(";");
        }
        internal void WriteInstanceOf(string source, string escapedTypeName, bool useReflection)
        {
            if (!useReflection)
            {
                _writer.Write(source);
                _writer.Write(" is ");
                _writer.Write(escapedTypeName);
                return;
            }
            _writer.Write(GetReflectionVariable(escapedTypeName, null));
            _writer.Write(".IsAssignableFrom(");
            _writer.Write(source);
            _writer.Write(".GetType())");
        }

        internal void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
        {
            if (arrayTypeDesc.UseReflection)
            {
                if (arrayTypeDesc.IsEnumerable)
                    typeName = typeof(IEnumerable).FullName;
                else if (arrayTypeDesc.IsCollection)
                    typeName = typeof(ICollection).FullName;
                else
                    typeName = typeof(Array).FullName;
            }
            _writer.Write(typeName);
            _writer.Write(" ");
            _writer.Write(variableName);
            if (initValue != null)
            {
                _writer.Write(" = ");
                if (initValue != "null")
                    _writer.Write("(" + typeName + ")");
                _writer.Write(initValue);
            }
            _writer.WriteLine(";");
        }
        internal void WriteEnumCase(string fullTypeName, ConstantMapping c, bool useReflection)
        {
            _writer.Write("case ");
            if (useReflection)
            {
                _writer.Write(c.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                _writer.Write(fullTypeName);
                _writer.Write(".@");
                CodeIdentifier.CheckValidIdentifier(c.Name);
                _writer.Write(c.Name);
            }
            _writer.Write(": ");
        }
        internal void WriteTypeCompare(string variable, string escapedTypeName, bool useReflection)
        {
            _writer.Write(variable);
            _writer.Write(" == ");
            _writer.Write(GetStringForTypeof(escapedTypeName, useReflection));
        }
        internal void WriteArrayTypeCompare(string variable, string escapedTypeName, string elementTypeName, bool useReflection)
        {
            if (!useReflection)
            {
                _writer.Write(variable);
                _writer.Write(" == typeof(");
                _writer.Write(escapedTypeName);
                _writer.Write(")");
                return;
            }
            _writer.Write(variable);
            _writer.Write(".IsArray ");
            _writer.Write(" && ");
            WriteTypeCompare(variable + ".GetElementType()", elementTypeName, useReflection);
        }

        internal static void WriteQuotedCSharpString(IndentedWriter writer, string value)
        {
            if (value == null)
            {
                writer.Write("null");
                return;
            }
            writer.Write("@\"");
            foreach (char ch in value)
            {
                if (ch < 32)
                {
                    if (ch == '\r')
                        writer.Write("\\r");
                    else if (ch == '\n')
                        writer.Write("\\n");
                    else if (ch == '\t')
                        writer.Write("\\t");
                    else
                    {
                        byte b = (byte)ch;
                        writer.Write("\\x");
                        writer.Write(hexDigits[b >> 4]);
                        writer.Write(hexDigits[b & 0xF]);
                    }
                }
                else if (ch == '\"')
                {
                    writer.Write("\"\"");
                }
                else
                {
                    writer.Write(ch);
                }
            }
            writer.Write("\"");
        }

        internal void WriteQuotedCSharpString(string value)
        {
            WriteQuotedCSharpString(_writer, value);
        }

        private static string s_helperClassesForUseReflection = @"
    sealed class XSFieldInfo {{
       {3} fieldInfo;
        public XSFieldInfo({2} t, {1} memberName){{
            fieldInfo = t.GetField(memberName);
        }}
        public {0} this[{0} o] {{
            get {{
                return fieldInfo.GetValue(o);
            }}
            set {{
                fieldInfo.SetValue(o, value);
            }}
        }}

    }}
    sealed class XSPropInfo {{
        {4} propInfo;
        public XSPropInfo({2} t, {1} memberName){{
            propInfo = t.GetProperty(memberName);
        }}
        public {0} this[{0} o] {{
            get {{
                return propInfo.GetValue(o, null);
            }}
            set {{
                propInfo.SetValue(o, value, null);
            }}
        }}
    }}
    sealed class XSArrayInfo {{
        {4} propInfo;
        public XSArrayInfo({4} propInfo){{
            this.propInfo = propInfo;
        }}
        public {0} this[{0} a, int i] {{
            get {{
                return propInfo.GetValue(a, new {0}[]{{i}});
            }}
            set {{
                propInfo.SetValue(a, value, new {0}[]{{i}});
            }}
        }}
    }}
";
    }
#endif
}
