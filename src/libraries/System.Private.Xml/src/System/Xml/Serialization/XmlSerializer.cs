// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.Serialization.Configuration;

namespace System.Xml.Serialization
{
    public struct XmlDeserializationEvents
    {
        private XmlNodeEventHandler? _onUnknownNode;
        private XmlAttributeEventHandler? _onUnknownAttribute;
        private XmlElementEventHandler? _onUnknownElement;
        private UnreferencedObjectEventHandler? _onUnreferencedObject;
        internal object? sender;

        public XmlNodeEventHandler? OnUnknownNode
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

        public XmlAttributeEventHandler? OnUnknownAttribute
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

        public XmlElementEventHandler? OnUnknownElement
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

        public UnreferencedObjectEventHandler? OnUnreferencedObject
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

    public abstract class XmlSerializerImplementation
    {
        public virtual XmlSerializationReader Reader { get { throw new NotSupportedException(); } }

        public virtual XmlSerializationWriter Writer { get { throw new NotSupportedException(); } }

        public virtual Hashtable ReadMethods { get { throw new NotSupportedException(); } }

        public virtual Hashtable WriteMethods { get { throw new NotSupportedException(); } }

        public virtual Hashtable TypedSerializers { get { throw new NotSupportedException(); } }

        public virtual bool CanSerialize(Type type) { throw new NotSupportedException(); }

        public virtual XmlSerializer GetSerializer(Type type) { throw new NotSupportedException(); }
    }

    // This enum is intentionally kept outside of the XmlSerializer class since if it would be a subclass
    // of XmlSerializer, then any access to this enum would be treated by AOT compilers as access to the XmlSerializer
    // as well, which has a large static ctor which brings in a lot of code. So keeping the enum separate
    // makes sure that using just the enum itself doesn't bring in the whole of serialization code base.
    internal enum SerializationMode
    {
        CodeGenOnly,
        ReflectionOnly,
        ReflectionAsBackup,
        PreGenOnly
    }

    public class XmlSerializer
    {
        private static SerializationMode s_mode = SerializationMode.ReflectionAsBackup;

        internal static SerializationMode Mode
        {
            get => RuntimeFeature.IsDynamicCodeSupported ? s_mode : SerializationMode.ReflectionOnly;
            set => s_mode = value;
        }

        private static bool ReflectionMethodEnabled
        {
            get
            {
                return Mode == SerializationMode.ReflectionOnly || Mode == SerializationMode.ReflectionAsBackup;
            }
        }

        private TempAssembly? _tempAssembly;
#pragma warning disable 0414
        private bool _typedSerializer;
#pragma warning restore 0414
        private readonly Type? _primitiveType;
        private XmlMapping _mapping = null!;
        private XmlDeserializationEvents _events;
        internal string? DefaultNamespace;
        private Type? _rootType;
        private bool _isReflectionBasedSerializer;

        private static readonly TempAssemblyCache s_cache = new TempAssemblyCache();
        private static volatile XmlSerializerNamespaces? s_defaultNamespaces;
        private static XmlSerializerNamespaces DefaultNamespaces
        {
            get
            {
                if (s_defaultNamespaces == null)
                {
                    XmlSerializerNamespaces nss = new XmlSerializerNamespaces();
                    nss.AddInternal("xsi", XmlSchema.InstanceNamespace);
                    nss.AddInternal("xsd", XmlSchema.Namespace);
                    s_defaultNamespaces ??= nss;
                }
                return s_defaultNamespaces;
            }
        }

        // Trimmer warning messages
        internal const string TrimSerializationWarning = "Members from serialized types may be trimmed if not referenced directly";
        private const string TrimDeserializationWarning = "Members from deserialized types may be trimmed if not referenced directly";

        private static readonly ContextAwareTables<Dictionary<XmlSerializerMappingKey, XmlSerializer>> s_xmlSerializerTable = new ContextAwareTables<Dictionary<XmlSerializerMappingKey, XmlSerializer>>();
        protected XmlSerializer()
        {
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public XmlSerializer(Type type, XmlAttributeOverrides? overrides, Type[]? extraTypes, XmlRootAttribute? root, string? defaultNamespace) :
            this(type, overrides, extraTypes, root, defaultNamespace, null)
        {
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public XmlSerializer(Type type, XmlRootAttribute? root) : this(type, null, Type.EmptyTypes, root, null, null)
        {
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public XmlSerializer(Type type, Type[]? extraTypes) : this(type, null, extraTypes, null, null, null)
        {
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public XmlSerializer(Type type, XmlAttributeOverrides? overrides) : this(type, overrides, Type.EmptyTypes, null, null, null)
        {
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public XmlSerializer(XmlTypeMapping xmlTypeMapping)
        {
            ArgumentNullException.ThrowIfNull(xmlTypeMapping);

            if (Mode != SerializationMode.ReflectionOnly)
            {
                _tempAssembly = GenerateTempAssembly(xmlTypeMapping);
            }
            _mapping = xmlTypeMapping;
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public XmlSerializer(Type type) : this(type, (string?)null)
        {
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public XmlSerializer(Type type, string? defaultNamespace)
        {
            ArgumentNullException.ThrowIfNull(type);

            DefaultNamespace = defaultNamespace;
            _rootType = type;

            _mapping = GetKnownMapping(type, defaultNamespace)!;
            if (_mapping != null)
            {
                _primitiveType = type;
                return;
            }

            if (Mode == SerializationMode.ReflectionOnly)
            {
                return;
            }

            _tempAssembly = s_cache[defaultNamespace, type];
            if (_tempAssembly == null)
            {
                lock (s_cache)
                {
                    _tempAssembly = s_cache[defaultNamespace, type];
                    if (_tempAssembly == null)
                    {
                        XmlSerializerImplementation? contract = null;
                        Assembly? assembly = TempAssembly.LoadGeneratedAssembly(type, defaultNamespace, out contract);
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
                            _tempAssembly = GenerateTempAssembly(_mapping, type, defaultNamespace)!;
                        }
                        else
                        {
                            // we found the pre-generated assembly, now make sure that the assembly has the right serializer
                            // try to avoid the reflection step, need to get ElementName, namespace and the Key form the type
                            _mapping = XmlReflectionImporter.GetTopLevelMapping(type, defaultNamespace);
                            _tempAssembly = new TempAssembly(new XmlMapping[] { _mapping }, assembly, contract);
                        }
                    }
                    s_cache.Add(defaultNamespace, type, _tempAssembly);
                }
            }

            _mapping ??= XmlReflectionImporter.GetTopLevelMapping(type, defaultNamespace);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public XmlSerializer(Type type, XmlAttributeOverrides? overrides, Type[]? extraTypes, XmlRootAttribute? root, string? defaultNamespace, string? location)
        {
            ArgumentNullException.ThrowIfNull(type);

            DefaultNamespace = defaultNamespace;
            _rootType = type;
            _mapping = GenerateXmlTypeMapping(type, overrides, extraTypes, root, defaultNamespace);
            if (Mode != SerializationMode.ReflectionOnly)
            {
                _tempAssembly = GenerateTempAssembly(_mapping, type, defaultNamespace, location);
            }
        }

        [RequiresUnreferencedCode("calls ImportTypeMapping")]
        private static XmlTypeMapping GenerateXmlTypeMapping(Type type, XmlAttributeOverrides? overrides, Type[]? extraTypes, XmlRootAttribute? root, string? defaultNamespace)
        {
            XmlReflectionImporter importer = new XmlReflectionImporter(overrides, defaultNamespace);
            if (extraTypes != null)
            {
                for (int i = 0; i < extraTypes.Length; i++)
                    importer.IncludeType(extraTypes[i]);
            }

            return importer.ImportTypeMapping(type, root, defaultNamespace);
        }

        [RequiresUnreferencedCode("creates TempAssembly")]
        internal static TempAssembly? GenerateTempAssembly(XmlMapping xmlMapping)
        {
            return GenerateTempAssembly(xmlMapping, null, null);
        }

        [RequiresUnreferencedCode("creates TempAssembly")]
        internal static TempAssembly? GenerateTempAssembly(XmlMapping xmlMapping, Type? type, string? defaultNamespace)
        {
            return GenerateTempAssembly(xmlMapping, type, defaultNamespace, null);
        }

        [RequiresUnreferencedCode("creates TempAssembly")]
        internal static TempAssembly? GenerateTempAssembly(XmlMapping xmlMapping, Type? type, string? defaultNamespace, string? location)
        {
            ArgumentNullException.ThrowIfNull(xmlMapping);

            xmlMapping.CheckShallow();
            if (xmlMapping.IsSoap)
            {
                return null;
            }

            return new TempAssembly(new XmlMapping[] { xmlMapping }, new Type?[] { type }, defaultNamespace, location);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public void Serialize(TextWriter textWriter, object? o)
        {
            Serialize(textWriter, o, null);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public void Serialize(TextWriter textWriter, object? o, XmlSerializerNamespaces? namespaces)
        {
            XmlWriter xmlWriter = XmlWriter.Create(textWriter);
            Serialize(xmlWriter, o, namespaces);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public void Serialize(Stream stream, object? o)
        {
            Serialize(stream, o, null);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public void Serialize(Stream stream, object? o, XmlSerializerNamespaces? namespaces)
        {
            XmlWriter xmlWriter = XmlWriter.Create(stream);
            Serialize(xmlWriter, o, namespaces);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public void Serialize(XmlWriter xmlWriter, object? o)
        {
            Serialize(xmlWriter, o, null);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public void Serialize(XmlWriter xmlWriter, object? o, XmlSerializerNamespaces? namespaces)
        {
            Serialize(xmlWriter, o, namespaces, null);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public void Serialize(XmlWriter xmlWriter, object? o, XmlSerializerNamespaces? namespaces, string? encodingStyle)
        {
            Serialize(xmlWriter, o, namespaces, encodingStyle, null);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public void Serialize(XmlWriter xmlWriter, object? o, XmlSerializerNamespaces? namespaces, string? encodingStyle, string? id)
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
                else if (ShouldUseReflectionBasedSerialization(_mapping) || _isReflectionBasedSerializer)
                {
                    SerializeUsingReflection(xmlWriter, o, namespaces, encodingStyle, id);
                }
                else if (_tempAssembly == null || _typedSerializer)
                {
                    // The contion for the block is never true, thus the block is never hit.
                    XmlSerializationWriter writer = CreateWriter();
                    writer.Init(xmlWriter, namespaces == null || namespaces.Count == 0 ? DefaultNamespaces : namespaces, encodingStyle, id, _tempAssembly);
                    Serialize(o, writer);
                }
                else
                {
                    _tempAssembly.InvokeWriter(_mapping, xmlWriter, o, namespaces == null || namespaces.Count == 0 ? DefaultNamespaces : namespaces, encodingStyle, id);
                }
            }
            catch (Exception? e)
            {
                if (e is TargetInvocationException)
                    e = e.InnerException;
                throw new InvalidOperationException(SR.XmlGenError, e);
            }
            xmlWriter.Flush();
        }

        [RequiresUnreferencedCode("calls GetMapping")]
        private void SerializeUsingReflection(XmlWriter xmlWriter, object? o, XmlSerializerNamespaces? namespaces, string? encodingStyle, string? id)
        {
            XmlMapping mapping = GetMapping();
            var writer = new ReflectionXmlSerializationWriter(mapping, xmlWriter, namespaces == null || namespaces.Count == 0 ? DefaultNamespaces : namespaces, encodingStyle, id);
            writer.WriteObject(o);
        }

        [RequiresUnreferencedCode("calls GenerateXmlTypeMapping")]
        private XmlMapping GetMapping()
        {
            if (_mapping == null || !_mapping.GenerateSerializer)
            {
                _mapping = GenerateXmlTypeMapping(_rootType!, null, null, null, DefaultNamespace);
            }

            return _mapping;
        }

        [RequiresUnreferencedCode(TrimDeserializationWarning)]
        public object? Deserialize(Stream stream)
        {
            XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings() { IgnoreWhitespace = true });
            return Deserialize(xmlReader, null);
        }

        [RequiresUnreferencedCode(TrimDeserializationWarning)]
        public object? Deserialize(TextReader textReader)
        {
            XmlTextReader xmlReader = new XmlTextReader(textReader);
            xmlReader.WhitespaceHandling = WhitespaceHandling.Significant;
            xmlReader.Normalization = true;
            xmlReader.XmlResolver = null;
            return Deserialize(xmlReader, null);
        }

        [RequiresUnreferencedCode(TrimDeserializationWarning)]
        public object? Deserialize(XmlReader xmlReader)
        {
            return Deserialize(xmlReader, null);
        }

        [RequiresUnreferencedCode(TrimDeserializationWarning)]
        public object? Deserialize(XmlReader xmlReader, XmlDeserializationEvents events)
        {
            return Deserialize(xmlReader, null, events);
        }

        [RequiresUnreferencedCode(TrimDeserializationWarning)]
        public object? Deserialize(XmlReader xmlReader, string? encodingStyle)
        {
            return Deserialize(xmlReader, encodingStyle, _events);
        }

        [RequiresUnreferencedCode(TrimDeserializationWarning)]
        public object? Deserialize(XmlReader xmlReader, string? encodingStyle, XmlDeserializationEvents events)
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
                else if (ShouldUseReflectionBasedSerialization(_mapping) || _isReflectionBasedSerializer)
                {
                    return DeserializeUsingReflection(xmlReader, encodingStyle, events);
                }
                else if (_tempAssembly == null || _typedSerializer)
                {
                    XmlSerializationReader reader = CreateReader();
                    reader.Init(xmlReader, events, encodingStyle, _tempAssembly);
                    return Deserialize(reader);
                }
                else
                {
                    return _tempAssembly.InvokeReader(_mapping, xmlReader, events, encodingStyle);
                }
            }
            catch (Exception? e)
            {
                if (e is TargetInvocationException)
                    e = e.InnerException;

                if (xmlReader is IXmlLineInfo lineInfo)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlSerializeErrorDetails, lineInfo.LineNumber.ToString(CultureInfo.InvariantCulture), lineInfo.LinePosition.ToString(CultureInfo.InvariantCulture)), e);
                }
                else
                {
                    throw new InvalidOperationException(SR.XmlSerializeError, e);
                }
            }
        }

        [RequiresUnreferencedCode("calls GetMapping")]
        private object? DeserializeUsingReflection(XmlReader xmlReader, string? encodingStyle, XmlDeserializationEvents events)
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

        public virtual bool CanDeserialize(XmlReader xmlReader)
        {
            if (_primitiveType != null)
            {
                TypeDesc typeDesc = (TypeDesc)TypeScope.PrimtiveTypes[_primitiveType]!;
                return xmlReader.IsStartElement(typeDesc.DataType!.Name!, string.Empty);
            }
            else if (ShouldUseReflectionBasedSerialization(_mapping) || _isReflectionBasedSerializer)
            {
                // If we should use reflection, we will try to do reflection-based deserialization, without fallback.
                // Don't check xmlReader.IsStartElement to avoid having to duplicate SOAP deserialization logic here.
                // It is better to return an incorrect 'true', which will throw during Deserialize than to return an
                // incorrect 'false', and the caller won't even try to Deserialize when it would succeed.
                return true;
            }
            else if (_tempAssembly != null)
            {
                return _tempAssembly.CanRead(_mapping, xmlReader);
            }
            else
            {
                return false;
            }
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public static XmlSerializer[] FromMappings(XmlMapping[]? mappings)
        {
            return FromMappings(mappings, (Type?)null);
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public static XmlSerializer[] FromMappings(XmlMapping[]? mappings, Type? type)
        {
            if (mappings == null || mappings.Length == 0) return Array.Empty<XmlSerializer>();
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

            XmlSerializerImplementation? contract = null;
            Assembly? assembly = type == null ? null : TempAssembly.LoadGeneratedAssembly(type, null, out contract);
            TempAssembly? tempAssembly;
            if (assembly == null)
            {
                if (Mode == SerializationMode.PreGenOnly)
                {
                    AssemblyName name = type!.Assembly.GetName();
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
                        tempAssembly = new TempAssembly(mappings, new Type?[] { type }, null, null);
                        XmlSerializer[] serializers = new XmlSerializer[mappings.Length];

                        contract = tempAssembly.Contract;

                        for (int i = 0; i < serializers.Length; i++)
                        {
                            serializers[i] = (XmlSerializer)contract.TypedSerializers[mappings[i].Key!]!;
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
                {
                    serializers[i] = (XmlSerializer)contract!.TypedSerializers[mappings[i].Key!]!;
                    TempAssembly.VerifyLoadContext(serializers[i]._rootType, type!.Assembly);
                }
                return serializers;
            }
        }

        private static XmlSerializer[] GetReflectionBasedSerializers(XmlMapping[] mappings, Type? type)
        {
            var serializers = new XmlSerializer[mappings.Length];
            for (int i = 0; i < serializers.Length; i++)
            {
                serializers[i] = new XmlSerializer();
                serializers[i]._rootType = type;
                serializers[i]._mapping = mappings[i];
                serializers[i]._isReflectionBasedSerializer = true;
            }

            return serializers;
        }

        [RequiresUnreferencedCode("calls GenerateSerializerToStream")]
        [UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file",
            Justification = "Code is used on diagnostics so we fallback to print assembly.FullName if assembly.Location is empty")]
        internal static bool GenerateSerializer(Type[]? types, XmlMapping[] mappings, Stream stream)
        {
            if (types == null || types.Length == 0)
                return false;

            ArgumentNullException.ThrowIfNull(mappings);
            ArgumentNullException.ThrowIfNull(stream);

            if (XmlMapping.IsShallow(mappings))
            {
                throw new InvalidOperationException(SR.XmlMelformMapping);
            }

            Assembly? assembly = null;
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
                    string? nameOrLocation = assembly.Location;
                    if (nameOrLocation == string.Empty)
                        nameOrLocation = assembly.FullName;
                    throw new ArgumentException(SR.Format(SR.XmlPregenOrphanType, type.FullName, nameOrLocation), nameof(types));
                }
            }

            return TempAssembly.GenerateSerializerToStream(mappings, types, null, assembly, new Hashtable(), stream);
        }

        [RequiresUnreferencedCode("calls Contract")]
        private static XmlSerializer[] GetSerializersFromCache(XmlMapping[] mappings, Type type)
        {
            XmlSerializer?[] serializers = new XmlSerializer?[mappings.Length];
            Dictionary<XmlSerializerMappingKey, XmlSerializer>? typedMappingTable = null;
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(type.Assembly);

            typedMappingTable = s_xmlSerializerTable.GetOrCreateValue(type, _ => new Dictionary<XmlSerializerMappingKey, XmlSerializer>());

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
                        serializers[index] = (XmlSerializer)contract.TypedSerializers[mappingKey.Mapping.Key!]!;
                        serializers[index]!.SetTempAssembly(tempAssembly, mappingKey.Mapping);

                        typedMappingTable[mappingKey] = serializers[index]!;
                    }
                }
            }

            return serializers!;
        }

        [RequiresUnreferencedCode(TrimSerializationWarning)]
        public static XmlSerializer[] FromTypes(Type[]? types)
        {
            if (types == null)
                return Array.Empty<XmlSerializer>();

            XmlReflectionImporter importer = new XmlReflectionImporter();
            XmlTypeMapping[] mappings = new XmlTypeMapping[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                mappings[i] = importer.ImportTypeMapping(types[i]);
            }
            return FromMappings(mappings);
        }

        public static string GetXmlSerializerAssemblyName(Type type)
        {
            return GetXmlSerializerAssemblyName(type, null);
        }

        public static string GetXmlSerializerAssemblyName(Type type, string? defaultNamespace)
        {
            ArgumentNullException.ThrowIfNull(type);

            return Compiler.GetTempAssemblyName(type.Assembly.GetName(), defaultNamespace);
        }

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

        protected virtual XmlSerializationReader CreateReader() { throw new NotImplementedException(); }

        protected virtual object Deserialize(XmlSerializationReader reader) { throw new NotImplementedException(); }

        protected virtual XmlSerializationWriter CreateWriter() { throw new NotImplementedException(); }

        protected virtual void Serialize(object? o, XmlSerializationWriter writer) { throw new NotImplementedException(); }

        internal void SetTempAssembly(TempAssembly tempAssembly, XmlMapping mapping)
        {
            _tempAssembly = tempAssembly;
            _mapping = mapping;
            _typedSerializer = true;
        }

        private static XmlTypeMapping? GetKnownMapping(Type type, string? ns)
        {
            if (ns != null && ns != string.Empty)
                return null;
            TypeDesc? typeDesc = (TypeDesc?)TypeScope.PrimtiveTypes[type];
            if (typeDesc == null)
                return null;
            ElementAccessor element = new ElementAccessor();
            element.Name = typeDesc.DataType!.Name;
            XmlTypeMapping mapping = new XmlTypeMapping(null, element);
            mapping.SetKeyInternal(XmlMapping.GenerateKey(type, null, null));
            return mapping;
        }

        private void SerializePrimitive(XmlWriter xmlWriter, object? o, XmlSerializerNamespaces? namespaces)
        {
            XmlSerializationPrimitiveWriter writer = new XmlSerializationPrimitiveWriter();
            writer.Init(xmlWriter, namespaces, null, null, null);
            switch (Type.GetTypeCode(_primitiveType))
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
                    else if (_primitiveType == typeof(DateTimeOffset))
                    {
                        writer.Write_dateTimeOffset(o);
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlUnxpectedType, _primitiveType!.FullName));
                    }
                    break;
            }
        }

        private object? DeserializePrimitive(XmlReader xmlReader, XmlDeserializationEvents events)
        {
            XmlSerializationPrimitiveReader reader = new XmlSerializationPrimitiveReader();
            reader.Init(xmlReader, events, null, null);
            object? o;
            switch (Type.GetTypeCode(_primitiveType))
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
                    else if (_primitiveType == typeof(DateTimeOffset))
                    {
                        o = reader.Read_dateTimeOffset();
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlUnxpectedType, _primitiveType!.FullName));
                    }
                    break;
            }
            return o;
        }

        private sealed class XmlSerializerMappingKey
        {
            public XmlMapping Mapping;
            public XmlSerializerMappingKey(XmlMapping mapping)
            {
                this.Mapping = mapping;
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                XmlSerializerMappingKey? other = obj as XmlSerializerMappingKey;
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
