// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System.Reflection;
    using System.Collections;
    using System.IO;
    using System.Xml.Schema;
    using System;
    using System.Text;
    using System.Threading;
    using System.Globalization;
    using System.Security;
    using System.Xml.Serialization.Configuration;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Runtime.Versioning;
    using System.Xml;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlDeserializationEvents"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public struct XmlDeserializationEvents
    {
        private XmlNodeEventHandler _onUnknownNode;
        private XmlAttributeEventHandler _onUnknownAttribute;
        private XmlElementEventHandler _onUnknownElement;
        private UnreferencedObjectEventHandler _onUnreferencedObject;
        internal object sender;

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlDeserializationEvents.OnUnknownNode"]/*' />
        public XmlNodeEventHandler OnUnknownNode
        {
            get
            {
                return _onUnknownNode;
            }

            set
            {
                _onUnknownNode = value;
            }
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlDeserializationEvents.OnUnknownAttribute"]/*' />
        public XmlAttributeEventHandler OnUnknownAttribute
        {
            get
            {
                return _onUnknownAttribute;
            }
            set
            {
                _onUnknownAttribute = value;
            }
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlDeserializationEvents.OnUnknownElement"]/*' />
        public XmlElementEventHandler OnUnknownElement
        {
            get
            {
                return _onUnknownElement;
            }
            set
            {
                _onUnknownElement = value;
            }
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlDeserializationEvents.OnUnreferencedObject"]/*' />
        public UnreferencedObjectEventHandler OnUnreferencedObject
        {
            get
            {
                return _onUnreferencedObject;
            }
            set
            {
                _onUnreferencedObject = value;
            }
        }
    }

    /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializerImplementation"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public abstract class XmlSerializerImplementation
    {
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializerImplementation.Reader"]/*' />
        public virtual XmlSerializationReader Reader { get { throw new NotSupportedException(); } }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializerImplementation.Writer"]/*' />
        public virtual XmlSerializationWriter Writer { get { throw new NotSupportedException(); } }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializerImplementation.ReadMethods"]/*' />
        public virtual Hashtable ReadMethods { get { throw new NotSupportedException(); } }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializerImplementation.WriteMethods"]/*' />
        public virtual Hashtable WriteMethods { get { throw new NotSupportedException(); } }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializerImplementation.TypedSerializers"]/*' />
        public virtual Hashtable TypedSerializers { get { throw new NotSupportedException(); } }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializerImplementation.CanSerialize"]/*' />
        public virtual bool CanSerialize(Type type) { throw new NotSupportedException(); }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializerImplementation.GetSerializer"]/*' />
        public virtual XmlSerializer GetSerializer(Type type) { throw new NotSupportedException(); }
    }

    // This enum is intentionally kept outside of the XmlSerializer class since if it would be a subclass
    // of XmlSerializer, then any access to this enum would be treated by AOT compilers as access to the XmlSerializer
    // as well, which has a large static ctor which brings in a lot of code. So keeping the enum separate
    // makes sure that using just the enum itself doesn't bring in the whole of serialization code base.
#if FEATURE_SERIALIZATION_UAPAOT
    public enum SerializationMode
#else
    internal enum SerializationMode
#endif
    {
        CodeGenOnly,
        ReflectionOnly,
        ReflectionAsBackup,
        PreGenOnly
    }

    /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSerializer
    {
#if FEATURE_SERIALIZATION_UAPAOT
        public static SerializationMode Mode { get; set; } = SerializationMode.ReflectionAsBackup;
#else
        internal static SerializationMode Mode { get; set; } = SerializationMode.ReflectionAsBackup;
#endif

        private static bool ReflectionMethodEnabled
        {
            get
            {
                return Mode == SerializationMode.ReflectionOnly || Mode == SerializationMode.ReflectionAsBackup;
            }
        }

        private TempAssembly _tempAssembly;
#pragma warning disable 0414
        private bool _typedSerializer;
#pragma warning restore 0414
        private Type _primitiveType;
        private XmlMapping _mapping;
        private XmlDeserializationEvents _events = new XmlDeserializationEvents();
#if FEATURE_SERIALIZATION_UAPAOT
        private XmlSerializer innerSerializer;
        public string DefaultNamespace = null;
#else
        internal string DefaultNamespace = null;
#endif
        private Type _rootType;

        private static TempAssemblyCache s_cache = new TempAssemblyCache();
        private static volatile XmlSerializerNamespaces s_defaultNamespaces;
        private static XmlSerializerNamespaces DefaultNamespaces
        {
            get
            {
                if (s_defaultNamespaces == null)
                {
                    XmlSerializerNamespaces nss = new XmlSerializerNamespaces();
                    nss.AddInternal("xsi", XmlSchema.InstanceNamespace);
                    nss.AddInternal("xsd", XmlSchema.Namespace);
                    if (s_defaultNamespaces == null)
                    {
                        s_defaultNamespaces = nss;
                    }
                }
                return s_defaultNamespaces;
            }
        }

        private static readonly Dictionary<Type, Dictionary<XmlSerializerMappingKey, XmlSerializer>> s_xmlSerializerTable = new Dictionary<Type, Dictionary<XmlSerializerMappingKey, XmlSerializer>>();

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.XmlSerializer8"]/*' />
        ///<internalonly/>
        protected XmlSerializer()
        {
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.XmlSerializer"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace) :
            this(type, overrides, extraTypes, root, defaultNamespace, null)
        {
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.XmlSerializer2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer(Type type, XmlRootAttribute root) : this(type, null, Array.Empty<Type>(), root, null, null)
        {
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.XmlSerializer3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer(Type type, Type[] extraTypes) : this(type, null, extraTypes, null, null, null)
        {
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.XmlSerializer4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer(Type type, XmlAttributeOverrides overrides) : this(type, overrides, Array.Empty<Type>(), null, null, null)
        {
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.XmlSerializer5"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer(XmlTypeMapping xmlTypeMapping)
        {
            if (xmlTypeMapping == null)
                throw new ArgumentNullException(nameof(xmlTypeMapping));

#if !FEATURE_SERIALIZATION_UAPAOT
            _tempAssembly = GenerateTempAssembly(xmlTypeMapping);
#endif
            _mapping = xmlTypeMapping;
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.XmlSerializer6"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer(Type type) : this(type, (string)null)
        {
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.XmlSerializer1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer(Type type, string defaultNamespace)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            DefaultNamespace = defaultNamespace;
            _rootType = type;

            _mapping = GetKnownMapping(type, defaultNamespace);
            if (_mapping != null)
            {
                _primitiveType = type;
                return;
            }
#if !FEATURE_SERIALIZATION_UAPAOT
            _tempAssembly = s_cache[defaultNamespace, type];
            if (_tempAssembly == null)
            {
                lock (s_cache)
                {
                    _tempAssembly = s_cache[defaultNamespace, type];
                    if (_tempAssembly == null)
                    {
                        {
                            XmlSerializerImplementation contract = null;
                            Assembly assembly = TempAssembly.LoadGeneratedAssembly(type, defaultNamespace, out contract);
                            if (assembly == null)
                            {
                                if (Mode == SerializationMode.PreGenOnly)
                                {
                                    AssemblyName name = type.Assembly.GetName();
                                    var serializerName = Compiler.GetTempAssemblyName(name, defaultNamespace);
                                    throw new FileLoadException(SR.Format(SR.FailLoadAssemblyUnderPregenMode, serializerName));
                                }

                                // need to reflect and generate new serialization assembly
                                XmlReflectionImporter importer = new XmlReflectionImporter(defaultNamespace);
                                _mapping = importer.ImportTypeMapping(type, null, defaultNamespace);
                                _tempAssembly = GenerateTempAssembly(_mapping, type, defaultNamespace);
                            }
                            else
                            {
                                // we found the pre-generated assembly, now make sure that the assembly has the right serializer
                                // try to avoid the reflection step, need to get ElementName, namespace and the Key form the type
                                _mapping = XmlReflectionImporter.GetTopLevelMapping(type, defaultNamespace);
                                _tempAssembly = new TempAssembly(new XmlMapping[] { _mapping }, assembly, contract);
                            }
                        }
                    }
                    s_cache.Add(defaultNamespace, type, _tempAssembly);
                }
            }
            if (_mapping == null)
            {
                _mapping = XmlReflectionImporter.GetTopLevelMapping(type, defaultNamespace);
            }
#else
            XmlSerializerImplementation contract = GetXmlSerializerContractFromGeneratedAssembly();

            if (contract != null)
            {
                this.innerSerializer = contract.GetSerializer(type);
            }
#endif
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.XmlSerializer7"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace, string location)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            DefaultNamespace = defaultNamespace;
            _rootType = type;
            _mapping = GenerateXmlTypeMapping(type, overrides, extraTypes, root, defaultNamespace);
#if !FEATURE_SERIALIZATION_UAPAOT
            _tempAssembly = GenerateTempAssembly(_mapping, type, defaultNamespace, location);
#endif
        }

        private XmlTypeMapping GenerateXmlTypeMapping(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace)
        {
            XmlReflectionImporter importer = new XmlReflectionImporter(overrides, defaultNamespace);
            if (extraTypes != null)
            {
                for (int i = 0; i < extraTypes.Length; i++)
                    importer.IncludeType(extraTypes[i]);
            }

            return importer.ImportTypeMapping(type, root, defaultNamespace);
        }

        internal static TempAssembly GenerateTempAssembly(XmlMapping xmlMapping)
        {
            return GenerateTempAssembly(xmlMapping, null, null);
        }

        internal static TempAssembly GenerateTempAssembly(XmlMapping xmlMapping, Type type, string defaultNamespace)
        {
            return GenerateTempAssembly(xmlMapping, type, defaultNamespace, null);
        }

        internal static TempAssembly GenerateTempAssembly(XmlMapping xmlMapping, Type type, string defaultNamespace, string location)
        {
            if (xmlMapping == null)
            {
                throw new ArgumentNullException(nameof(xmlMapping));
            }

            xmlMapping.CheckShallow();
            if (xmlMapping.IsSoap)
            {
                return null;
            }

            return new TempAssembly(new XmlMapping[] { xmlMapping }, new Type[] { type }, defaultNamespace, location);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Serialize"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Serialize(TextWriter textWriter, object o)
        {
            Serialize(textWriter, o, null);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Serialize1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Serialize(TextWriter textWriter, object o, XmlSerializerNamespaces namespaces)
        {
            XmlTextWriter xmlWriter = new XmlTextWriter(textWriter);
            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.Indentation = 2;
            Serialize(xmlWriter, o, namespaces);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Serialize2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Serialize(Stream stream, object o)
        {
            Serialize(stream, o, null);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Serialize3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Serialize(Stream stream, object o, XmlSerializerNamespaces namespaces)
        {
            XmlTextWriter xmlWriter = new XmlTextWriter(stream, null);
            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.Indentation = 2;
            Serialize(xmlWriter, o, namespaces);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Serialize4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Serialize(XmlWriter xmlWriter, object o)
        {
            Serialize(xmlWriter, o, null);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Serialize5"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Serialize(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces)
        {
            Serialize(xmlWriter, o, namespaces, null);
        }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Serialize6"]/*' />
        public void Serialize(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces, string encodingStyle)
        {
            Serialize(xmlWriter, o, namespaces, encodingStyle, null);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Serialize6"]/*' />
        public void Serialize(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces, string encodingStyle, string id)
        {
            try
            {
                if (_primitiveType != null)
                {
                    if (encodingStyle != null && encodingStyle.Length > 0)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidEncodingNotEncoded1, encodingStyle));
                    }
                    SerializePrimitive(xmlWriter, o, namespaces);
                }
                else if (ShouldUseReflectionBasedSerialization(_mapping))
                {
                    SerializeUsingReflection(xmlWriter, o, namespaces, encodingStyle, id);
                }
#if !FEATURE_SERIALIZATION_UAPAOT
                else if (_tempAssembly == null || _typedSerializer)
                {
                    // The contion for the block is never true, thus the block is never hit.
                    XmlSerializationWriter writer = CreateWriter();
                    writer.Init(xmlWriter, namespaces == null || namespaces.Count == 0 ? DefaultNamespaces : namespaces, encodingStyle, id, _tempAssembly);
                    try
                    {
                        Serialize(o, writer);
                    }
                    finally
                    {
                        writer.Dispose();
                    }
                }
                else
                    _tempAssembly.InvokeWriter(_mapping, xmlWriter, o, namespaces == null || namespaces.Count == 0 ? DefaultNamespaces : namespaces, encodingStyle, id);
#else
                else
                {
                    if (this.innerSerializer != null)
                    {
                        if (!string.IsNullOrEmpty(this.DefaultNamespace))
                        {
                            this.innerSerializer.DefaultNamespace = this.DefaultNamespace;
                        }

                        XmlSerializationWriter writer = this.innerSerializer.CreateWriter();
                        writer.Init(xmlWriter, namespaces == null || namespaces.Count == 0 ? DefaultNamespaces : namespaces, encodingStyle, id);
                        try
                        {
                            this.innerSerializer.Serialize(o, writer);
                        }
                        finally
                        {
                            writer.Dispose();
                        }
                    }
                    else if (ReflectionMethodEnabled)
                    {
                        SerializeUsingReflection(xmlWriter, o, namespaces, encodingStyle, id);
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.Xml_MissingSerializationCodeException, this._rootType, typeof(XmlSerializer).Name));
                    }
                }
#endif
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = e.InnerException;
                throw new InvalidOperationException(SR.XmlGenError, e);
            }
            xmlWriter.Flush();
        }

        private void SerializeUsingReflection(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces, string encodingStyle, string id)
        {
            XmlMapping mapping = GetMapping();
            var writer = new ReflectionXmlSerializationWriter(mapping, xmlWriter, namespaces == null || namespaces.Count == 0 ? DefaultNamespaces : namespaces, encodingStyle, id);
            writer.WriteObject(o);
        }

        private XmlMapping GetMapping()
        {
            if (_mapping == null || !_mapping.GenerateSerializer)
            {
                _mapping = GenerateXmlTypeMapping(_rootType, null, null, null, DefaultNamespace);
            }

            return _mapping;
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Deserialize"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object Deserialize(Stream stream)
        {
            XmlTextReader xmlReader = new XmlTextReader(stream);
            xmlReader.WhitespaceHandling = WhitespaceHandling.Significant;
            xmlReader.Normalization = true;
            xmlReader.XmlResolver = null;
            return Deserialize(xmlReader, null);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Deserialize1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object Deserialize(TextReader textReader)
        {
            XmlTextReader xmlReader = new XmlTextReader(textReader);
            xmlReader.WhitespaceHandling = WhitespaceHandling.Significant;
            xmlReader.Normalization = true;
            xmlReader.XmlResolver = null;
            return Deserialize(xmlReader, null);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Deserialize2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public object Deserialize(XmlReader xmlReader)
        {
            return Deserialize(xmlReader, null);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Deserialize3"]/*' />
        public object Deserialize(XmlReader xmlReader, XmlDeserializationEvents events)
        {
            return Deserialize(xmlReader, null, events);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Deserialize4"]/*' />
        public object Deserialize(XmlReader xmlReader, string encodingStyle)
        {
            return Deserialize(xmlReader, encodingStyle, _events);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Deserialize5"]/*' />
        public object Deserialize(XmlReader xmlReader, string encodingStyle, XmlDeserializationEvents events)
        {
            events.sender = this;
            try
            {
                if (_primitiveType != null)
                {
                    if (encodingStyle != null && encodingStyle.Length > 0)
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidEncodingNotEncoded1, encodingStyle));
                    }
                    return DeserializePrimitive(xmlReader, events);
                }
                else if (ShouldUseReflectionBasedSerialization(_mapping))
                {
                    return DeserializeUsingReflection(xmlReader, encodingStyle, events);
                }
#if !FEATURE_SERIALIZATION_UAPAOT
                else if (_tempAssembly == null || _typedSerializer)
                {
                    XmlSerializationReader reader = CreateReader();
                    reader.Init(xmlReader, events, encodingStyle, _tempAssembly);
                    try
                    {
                        return Deserialize(reader);
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                }
                else
                {
                    return _tempAssembly.InvokeReader(_mapping, xmlReader, events, encodingStyle);
                }
#else
                else
                {
                    if (this.innerSerializer != null)
                    {
                        if (!string.IsNullOrEmpty(this.DefaultNamespace))
                        {
                            this.innerSerializer.DefaultNamespace = this.DefaultNamespace;
                        }

                        XmlSerializationReader reader = this.innerSerializer.CreateReader();
                        reader.Init(xmlReader, events, encodingStyle);
                        try
                        {
                            return this.innerSerializer.Deserialize(reader);
                        }
                        finally
                        {
                            reader.Dispose();
                        }
                    }
                    else if (ReflectionMethodEnabled)
                    {
                        return DeserializeUsingReflection(xmlReader, encodingStyle, events);
                    }
                    else
                    {

                        throw new InvalidOperationException(SR.Format(SR.Xml_MissingSerializationCodeException, this._rootType, typeof(XmlSerializer).Name));
                    }
                }
#endif
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = e.InnerException;

                if (xmlReader is IXmlLineInfo)
                {
                    IXmlLineInfo lineInfo = (IXmlLineInfo)xmlReader;
                    throw new InvalidOperationException(SR.Format(SR.XmlSerializeErrorDetails, lineInfo.LineNumber.ToString(CultureInfo.InvariantCulture), lineInfo.LinePosition.ToString(CultureInfo.InvariantCulture)), e);
                }
                else
                {
                    throw new InvalidOperationException(SR.XmlSerializeError, e);
                }
            }
        }

        private object DeserializeUsingReflection(XmlReader xmlReader, string encodingStyle, XmlDeserializationEvents events)
        {
            XmlMapping mapping = GetMapping();
            var reader = new ReflectionXmlSerializationReader(mapping, xmlReader, events, encodingStyle);
            return reader.ReadObject();
        }

        private static bool ShouldUseReflectionBasedSerialization(XmlMapping mapping)
        {
            return Mode == SerializationMode.ReflectionOnly
                || (mapping != null && mapping.IsSoap);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.CanDeserialize"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual bool CanDeserialize(XmlReader xmlReader)
        {
            if (_primitiveType != null)
            {
                TypeDesc typeDesc = (TypeDesc)TypeScope.PrimtiveTypes[_primitiveType];
                return xmlReader.IsStartElement(typeDesc.DataType.Name, string.Empty);
            }
#if !FEATURE_SERIALIZATION_UAPAOT
            else if (_tempAssembly != null)
            {
                return _tempAssembly.CanRead(_mapping, xmlReader);
            }
            else
            {
                return false;
            }
#else
            if (this.innerSerializer != null)
            {
                return this.innerSerializer.CanDeserialize(xmlReader);
            }
            else
            {
                return ReflectionMethodEnabled;
            }
#endif
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.FromMappings"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static XmlSerializer[] FromMappings(XmlMapping[] mappings)
        {
            return FromMappings(mappings, (Type)null);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.FromMappings1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static XmlSerializer[] FromMappings(XmlMapping[] mappings, Type type)
        {
            if (mappings == null || mappings.Length == 0) return Array.Empty<XmlSerializer>();
#if FEATURE_SERIALIZATION_UAPAOT
            XmlSerializer[] serializers = GetReflectionBasedSerializers(mappings, type);
            return serializers;
#else
            bool anySoapMapping = false;
            foreach (var mapping in mappings)
            {
                if (mapping.IsSoap)
                {
                    anySoapMapping = true;
                }
            }

            if ((anySoapMapping && ReflectionMethodEnabled) || Mode == SerializationMode.ReflectionOnly)
            {
                XmlSerializer[] serializers = GetReflectionBasedSerializers(mappings, type);
                return serializers;
            }

            XmlSerializerImplementation contract = null;
            Assembly assembly = type == null ? null : TempAssembly.LoadGeneratedAssembly(type, null, out contract);
            TempAssembly tempAssembly = null;
            if (assembly == null)
            {
                if (Mode == SerializationMode.PreGenOnly)
                {
                    AssemblyName name = type.Assembly.GetName();
                    string serializerName = Compiler.GetTempAssemblyName(name, null);
                    throw new FileLoadException(SR.Format(SR.FailLoadAssemblyUnderPregenMode, serializerName));
                }

                if (XmlMapping.IsShallow(mappings))
                {
                    return Array.Empty<XmlSerializer>();
                }
                else
                {
                    if (type == null)
                    {
                        tempAssembly = new TempAssembly(mappings, new Type[] { type }, null, null);
                        XmlSerializer[] serializers = new XmlSerializer[mappings.Length];

                        contract = tempAssembly.Contract;

                        for (int i = 0; i < serializers.Length; i++)
                        {
                            serializers[i] = (XmlSerializer)contract.TypedSerializers[mappings[i].Key];
                            serializers[i].SetTempAssembly(tempAssembly, mappings[i]);
                        }

                        return serializers;
                    }
                    else
                    {
                        // Use XmlSerializer cache when the type is not null.
                        return GetSerializersFromCache(mappings, type);
                    }
                }
            }
            else
            {
                XmlSerializer[] serializers = new XmlSerializer[mappings.Length];
                for (int i = 0; i < serializers.Length; i++)
                    serializers[i] = (XmlSerializer)contract.TypedSerializers[mappings[i].Key];
                return serializers;
            }
#endif
        }

        private static XmlSerializer[] GetReflectionBasedSerializers(XmlMapping[] mappings, Type type)
        {
            var serializers = new XmlSerializer[mappings.Length];
            for (int i = 0; i < serializers.Length; i++)
            {
                serializers[i] = new XmlSerializer();
                serializers[i]._rootType = type;
                serializers[i]._mapping = mappings[i];
            }

            return serializers;
        }

#if !FEATURE_SERIALIZATION_UAPAOT
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        [ResourceExposure(ResourceScope.None)]
        public static bool GenerateSerializer(Type[] types, XmlMapping[] mappings, Stream stream)
        {
            if (types == null || types.Length == 0)
                return false;

            if (mappings == null)
                throw new ArgumentNullException(nameof(mappings));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (XmlMapping.IsShallow(mappings))
            {
                throw new InvalidOperationException(SR.Format(SR.XmlMelformMapping));
            }

            Assembly assembly = null;
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (DynamicAssemblies.IsTypeDynamic(type))
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlPregenTypeDynamic, type.FullName));
                }

                if (assembly == null)
                {
                    assembly = type.Assembly;
                }
                else if (type.Assembly != assembly)
                {
                    throw new ArgumentException(SR.Format(SR.XmlPregenOrphanType, type.FullName, assembly.Location), "types");
                }
            }

            return TempAssembly.GenerateSerializerToStream(mappings, types, null, assembly, new Hashtable(), stream);
        }
#endif

        private static XmlSerializer[] GetSerializersFromCache(XmlMapping[] mappings, Type type)
        {
            XmlSerializer[] serializers = new XmlSerializer[mappings.Length];

            Dictionary<XmlSerializerMappingKey, XmlSerializer> typedMappingTable = null;
            lock (s_xmlSerializerTable)
            {
                if (!s_xmlSerializerTable.TryGetValue(type, out typedMappingTable))
                {
                    typedMappingTable = new Dictionary<XmlSerializerMappingKey, XmlSerializer>();
                    s_xmlSerializerTable[type] = typedMappingTable;
                }
            }

            lock (typedMappingTable)
            {
                var pendingKeys = new Dictionary<XmlSerializerMappingKey, int>();
                for (int i = 0; i < mappings.Length; i++)
                {
                    XmlSerializerMappingKey mappingKey = new XmlSerializerMappingKey(mappings[i]);
                    if (!typedMappingTable.TryGetValue(mappingKey, out serializers[i]))
                    {
                        pendingKeys.Add(mappingKey, i);
                    }
                }

                if (pendingKeys.Count > 0)
                {
                    XmlMapping[] pendingMappings = new XmlMapping[pendingKeys.Count];
                    int index = 0;
                    foreach (XmlSerializerMappingKey mappingKey in pendingKeys.Keys)
                    {
                        pendingMappings[index++] = mappingKey.Mapping;
                    }

                    TempAssembly tempAssembly = new TempAssembly(pendingMappings, new Type[] { type }, null, null);
                    XmlSerializerImplementation contract = tempAssembly.Contract;

                    foreach (XmlSerializerMappingKey mappingKey in pendingKeys.Keys)
                    {
                        index = pendingKeys[mappingKey];
                        serializers[index] = (XmlSerializer)contract.TypedSerializers[mappingKey.Mapping.Key];
                        serializers[index].SetTempAssembly(tempAssembly, mappingKey.Mapping);

                        typedMappingTable[mappingKey] = serializers[index];
                    }
                }
            }

            return serializers;
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.FromTypes"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static XmlSerializer[] FromTypes(Type[] types)
        {
            if (types == null)
                return Array.Empty<XmlSerializer>();

#if FEATURE_SERIALIZATION_UAPAOT
            var serializers = new XmlSerializer[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                serializers[i] = new XmlSerializer(types[i]);
            }
            return serializers;
#else
            XmlReflectionImporter importer = new XmlReflectionImporter();
            XmlTypeMapping[] mappings = new XmlTypeMapping[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                mappings[i] = importer.ImportTypeMapping(types[i]);
            }
            return FromMappings(mappings);
#endif
        }

#if FEATURE_SERIALIZATION_UAPAOT
        // this the global XML serializer contract introduced for multi-file
        private static XmlSerializerImplementation xmlSerializerContract;

        internal static XmlSerializerImplementation GetXmlSerializerContractFromGeneratedAssembly()
        {
            // hack to pull in SetXmlSerializerContract which is only referenced from the
            // code injected by MainMethodInjector transform
            // there's probably also a way to do this via [DependencyReductionRoot],
            // but I can't get the compiler to find that...
            if (xmlSerializerContract == null)
                SetXmlSerializerContract(null);

            // this method body used to be rewritten by an IL transform
            // with the restructuring for multi-file, it has become a regular method
            return xmlSerializerContract;
        }

        public static void SetXmlSerializerContract(XmlSerializerImplementation xmlSerializerImplementation)
        {
            xmlSerializerContract = xmlSerializerImplementation;
        }
#endif

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.GetXmlSerializerAssemblyName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string GetXmlSerializerAssemblyName(Type type)
        {
            return GetXmlSerializerAssemblyName(type, null);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.GetXmlSerializerAssemblyName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static string GetXmlSerializerAssemblyName(Type type, string defaultNamespace)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            return Compiler.GetTempAssemblyName(type.Assembly.GetName(), defaultNamespace);
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.UnknownNode"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public event XmlNodeEventHandler UnknownNode
        {
            add
            {
                _events.OnUnknownNode += value;
            }
            remove
            {
                _events.OnUnknownNode -= value;
            }
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.UnknownAttribute"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public event XmlAttributeEventHandler UnknownAttribute
        {
            add
            {
                _events.OnUnknownAttribute += value;
            }
            remove
            {
                _events.OnUnknownAttribute -= value;
            }
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.UnknownElement"]/*' />
        public event XmlElementEventHandler UnknownElement
        {
            add
            {
                _events.OnUnknownElement += value;
            }
            remove
            {
                _events.OnUnknownElement -= value;
            }
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.UnreferencedObject"]/*' />
        public event UnreferencedObjectEventHandler UnreferencedObject
        {
            add
            {
                _events.OnUnreferencedObject += value;
            }
            remove
            {
                _events.OnUnreferencedObject -= value;
            }
        }

        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.CreateReader"]/*' />
        ///<internalonly/>
        protected virtual XmlSerializationReader CreateReader() { throw new NotImplementedException(); }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Deserialize4"]/*' />
        ///<internalonly/>
        protected virtual object Deserialize(XmlSerializationReader reader) { throw new NotImplementedException(); }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.CreateWriter"]/*' />
        ///<internalonly/>
        protected virtual XmlSerializationWriter CreateWriter() { throw new NotImplementedException(); }
        /// <include file='doc\XmlSerializer.uex' path='docs/doc[@for="XmlSerializer.Serialize7"]/*' />
        ///<internalonly/>
        protected virtual void Serialize(object o, XmlSerializationWriter writer) { throw new NotImplementedException(); }

        internal void SetTempAssembly(TempAssembly tempAssembly, XmlMapping mapping)
        {
            _tempAssembly = tempAssembly;
            _mapping = mapping;
            _typedSerializer = true;
        }

        private static XmlTypeMapping GetKnownMapping(Type type, string ns)
        {
            if (ns != null && ns != string.Empty)
                return null;
            TypeDesc typeDesc = (TypeDesc)TypeScope.PrimtiveTypes[type];
            if (typeDesc == null)
                return null;
            ElementAccessor element = new ElementAccessor();
            element.Name = typeDesc.DataType.Name;
            XmlTypeMapping mapping = new XmlTypeMapping(null, element);
            mapping.SetKeyInternal(XmlMapping.GenerateKey(type, null, null));
            return mapping;
        }

        private void SerializePrimitive(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces)
        {
            XmlSerializationPrimitiveWriter writer = new XmlSerializationPrimitiveWriter();
            writer.Init(xmlWriter, namespaces, null, null, null);
            switch (_primitiveType.GetTypeCode())
            {
                case TypeCode.String:
                    writer.Write_string(o);
                    break;
                case TypeCode.Int32:
                    writer.Write_int(o);
                    break;
                case TypeCode.Boolean:
                    writer.Write_boolean(o);
                    break;
                case TypeCode.Int16:
                    writer.Write_short(o);
                    break;
                case TypeCode.Int64:
                    writer.Write_long(o);
                    break;
                case TypeCode.Single:
                    writer.Write_float(o);
                    break;
                case TypeCode.Double:
                    writer.Write_double(o);
                    break;
                case TypeCode.Decimal:
                    writer.Write_decimal(o);
                    break;
                case TypeCode.DateTime:
                    writer.Write_dateTime(o);
                    break;
                case TypeCode.Char:
                    writer.Write_char(o);
                    break;
                case TypeCode.Byte:
                    writer.Write_unsignedByte(o);
                    break;
                case TypeCode.SByte:
                    writer.Write_byte(o);
                    break;
                case TypeCode.UInt16:
                    writer.Write_unsignedShort(o);
                    break;
                case TypeCode.UInt32:
                    writer.Write_unsignedInt(o);
                    break;
                case TypeCode.UInt64:
                    writer.Write_unsignedLong(o);
                    break;

                default:
                    if (_primitiveType == typeof(XmlQualifiedName))
                    {
                        writer.Write_QName(o);
                    }
                    else if (_primitiveType == typeof(byte[]))
                    {
                        writer.Write_base64Binary(o);
                    }
                    else if (_primitiveType == typeof(Guid))
                    {
                        writer.Write_guid(o);
                    }
                    else if (_primitiveType == typeof(TimeSpan))
                    {
                        writer.Write_TimeSpan(o);
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlUnxpectedType, _primitiveType.FullName));
                    }
                    break;
            }
        }

        private object DeserializePrimitive(XmlReader xmlReader, XmlDeserializationEvents events)
        {
            XmlSerializationPrimitiveReader reader = new XmlSerializationPrimitiveReader();
            reader.Init(xmlReader, events, null, null);
            object o;
            switch (_primitiveType.GetTypeCode())
            {
                case TypeCode.String:
                    o = reader.Read_string();
                    break;
                case TypeCode.Int32:
                    o = reader.Read_int();
                    break;
                case TypeCode.Boolean:
                    o = reader.Read_boolean();
                    break;
                case TypeCode.Int16:
                    o = reader.Read_short();
                    break;
                case TypeCode.Int64:
                    o = reader.Read_long();
                    break;
                case TypeCode.Single:
                    o = reader.Read_float();
                    break;
                case TypeCode.Double:
                    o = reader.Read_double();
                    break;
                case TypeCode.Decimal:
                    o = reader.Read_decimal();
                    break;
                case TypeCode.DateTime:
                    o = reader.Read_dateTime();
                    break;
                case TypeCode.Char:
                    o = reader.Read_char();
                    break;
                case TypeCode.Byte:
                    o = reader.Read_unsignedByte();
                    break;
                case TypeCode.SByte:
                    o = reader.Read_byte();
                    break;
                case TypeCode.UInt16:
                    o = reader.Read_unsignedShort();
                    break;
                case TypeCode.UInt32:
                    o = reader.Read_unsignedInt();
                    break;
                case TypeCode.UInt64:
                    o = reader.Read_unsignedLong();
                    break;

                default:
                    if (_primitiveType == typeof(XmlQualifiedName))
                    {
                        o = reader.Read_QName();
                    }
                    else if (_primitiveType == typeof(byte[]))
                    {
                        o = reader.Read_base64Binary();
                    }
                    else if (_primitiveType == typeof(Guid))
                    {
                        o = reader.Read_guid();
                    }
                    else if (_primitiveType == typeof(TimeSpan))
                    {
                        o = reader.Read_TimeSpan();
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlUnxpectedType, _primitiveType.FullName));
                    }
                    break;
            }
            return o;
        }

        private class XmlSerializerMappingKey
        {
            public XmlMapping Mapping;
            public XmlSerializerMappingKey(XmlMapping mapping)
            {
                this.Mapping = mapping;
            }

            public override bool Equals(object obj)
            {
                XmlSerializerMappingKey other = obj as XmlSerializerMappingKey;
                if (other == null)
                    return false;

                if (this.Mapping.Key != other.Mapping.Key)
                    return false;

                if (this.Mapping.ElementName != other.Mapping.ElementName)
                    return false;

                if (this.Mapping.Namespace != other.Mapping.Namespace)
                    return false;

                if (this.Mapping.IsSoap != other.Mapping.IsSoap)
                    return false;

                return true;
            }

            public override int GetHashCode()
            {
                int hashCode = this.Mapping.IsSoap ? 0 : 1;

                if (this.Mapping.Key != null)
                    hashCode ^= this.Mapping.Key.GetHashCode();

                if (this.Mapping.ElementName != null)
                    hashCode ^= this.Mapping.ElementName.GetHashCode();

                if (this.Mapping.Namespace != null)
                    hashCode ^= this.Mapping.Namespace.GetHashCode();

                return hashCode;
            }
        }
    }
}
