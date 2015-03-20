// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

#pragma warning disable 0162
#pragma warning disable 0429
using System;
using System.Linq;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Globalization;

namespace System.Xml.Serialization.LegacyNetCF
{
    internal struct XmlDeserializationEvents
    {
#if !FEATURE_LEGACYNETCF
        public XmlNodeEventHandler onUnknownNode;
        public XmlAttributeEventHandler onUnknownAttribute;
        public XmlElementEventHandler onUnknownElement;
        public UnreferencedObjectEventHandler onUnreferencedObject;
#endif
        public object sender;
    }

    public class XmlSerializer
    {
        //*****************************************************************
        // Static Data
        //*****************************************************************
        private static XmlSerializerNamespaces s_DEFAULT_NAMESPACES;

        //*****************************************************************
        // Data
        //*****************************************************************
        /// <summary>
        /// Events that will be fired while deserializing the object
        /// </summary>
        private XmlDeserializationEvents _events = new XmlDeserializationEvents();
        /// <summary>
        /// The reflector that gathers info on object
        /// </summary>
        private XmlSerializationReflector _reflector;
        /// <summary>
        /// The reflection data to be used while serializing
        /// and deserializing the object. (primary serialization type)
        /// </summary>
        private LogicalType _logicalType;
        /// <summary>
        /// Describes whether the object should be serialized using Soap section 5 encoding. 
        /// </summary>
        /// <remarks>
        /// True for RPC/encoded.  
        /// False for RPC/literal and Document/literal.
        /// Document/encoded is not an allowed configuration.
        /// When true, the SoapElementAttribute-style will be noticed 
        /// instead of the XmlElementAttribute-style.
        /// </remarks>
        private bool _isEncoded;
        /// <summary>
        /// Array of extra types that can be serialized
        /// </summary>
        private LogicalType[] _extraTypes;
        /// <summary>
        /// The default namespace to use for all the XML elements.
        /// </summary>
        private string _defaultNamespace;

        //*****************************************************************
        // Constructors
        //*****************************************************************                

        /// <summary>
        /// Constructs the XmlSerializer without reflecting over any types.
        /// </summary>
        /// <remarks>
        /// The default constructor. This constructor does not initialize 
        /// the logical type reflection data used to serialize and 
        /// deserialize the data. So, if this constructor is used then the
        /// reflection data is not collected until the object is serialized.
        /// If Deserialize is called before the reflection data is collected
        /// then we attempt to find the logical type using the name and 
        /// namespace of the serialize object. 
        /// </remarks>
        protected XmlSerializer()
        {
        }

        /// <summary>
        /// Constructs the XmlSerializer, reflecting over just the one given 
        /// type and any types that are statically referenced from that type.
        /// </summary>
        public XmlSerializer(Type type)
            : this(type, null, null, null, null)
        {
        }

        /// <summary>
        /// Constructs the XmlSerializer, reflecting over just the one given 
        /// type and any types that are statically referenced from that type.
        /// </summary>
        /// <param name="defaultNamespace">The default namespace to use for all the XML elements.</param>
        public XmlSerializer(Type type, string defaultNamespace)
            : this(type, null, null, null, defaultNamespace)
        {
        }

        /// <summary>
        /// Constructs the XmlSerializer, reflecting over just the one given 
        /// type and any types that are statically referenced from that type.
        /// Reflection overrides may be given.
        /// </summary>
        /// <remarks>
        /// This consturctor allows you to specifiy attribute overrides. The 
        /// overrides will be used to override any attributes found on the 
        /// types inspected by the reflector.
        /// </remarks>
        public XmlSerializer(Type type, XmlAttributeOverrides overrides)
            : this(type, overrides, null, null, null)
        {
        }

        /// <summary>
        /// Constructs the XmlSerializer, reflecting over just the one given 
        /// type and any types that are statically referenced from that type.
        /// Reflection overrides may be given.
        /// </summary>
        public XmlSerializer(Type type, XmlRootAttribute root)
            : this(type, null, null, root, null)
        {
        }

        /// <summary>
        /// Constructs the XmlSerializer, reflecting over the given 
        /// types and any types that are statically referenced from them.
        /// Reflection overrides may be given.
        /// </summary>
        /// <param name="extraTypes">The additional types the serializer should be 
        /// prepared to encounter during serialization of the primary <paramref name="type"/>.</param>
        /// <param name="defaultNamespace">The default namespace to use for all the XML elements.</param>
        public XmlSerializer(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (defaultNamespace == null)
                defaultNamespace = "";

            _events.sender = this;
            _isEncoded = false;
            if (root != null)
            {
                if (overrides == null)
                    overrides = new XmlAttributeOverrides();
                // If we're dealing with a nullable type, we need to set the override
                // on the generic type argument as well.
                System.Collections.Generic.List<Type> typesToOverride = new System.Collections.Generic.List<Type>(2);
                typesToOverride.Add(type);
                if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    Type[] args = type.GetGenericArguments();
                    if (args.Length > 0)
                    { // just in case they passed in typeof(Nullable<>) with no generic arg
                        typesToOverride.Add(args[0]);
                    }
                }
                foreach (Type t in typesToOverride)
                {
                    XmlAttributes attrs = overrides[t];
                    if (attrs != null)
                    {
                        attrs.XmlRoot = root;
                    }
                    else
                    {
                        attrs = new XmlAttributes();
                        attrs.XmlRoot = root;
                        // Preserve any XmlType that may be declared on the type itself,
                        // since by providing this XmlRoot override, we prevent any declared
                        // XmlTypeAttribute from being reflected.
                        object[] declaredAttributes = t.GetTypeInfo().GetCustomAttributes(typeof(XmlTypeAttribute), false).ToArray();
                        if (declaredAttributes.Length > 0)
                        {
                            // The user shouldn't have more than one defined, but just in case he does,
                            // we read the last reflected one, to emulate the behavior of the
                            // TypeAttributes class.
                            attrs.XmlType = (XmlTypeAttribute)declaredAttributes[declaredAttributes.Length - 1];
                        }
                        overrides.Add(t, attrs);
                    }
                }
            }

            _defaultNamespace = defaultNamespace;
            _reflector = new XmlSerializationReflector(overrides, defaultNamespace);
            // Reflect over the main type.  Never search over intrinsics so we get a
            // LogicalType with a RootAccessor whose Namespace property reflects the defaultNamespace.
            // In fact, any type's RootAccessor.Namespace may not be set correctly given
            // the defaultNamespace if it was reflected over previously.  But since this
            // is the very first request we make of the m_Reflector, we're going to get
            // the right one.  And this is the only place where that is important.
            _logicalType = _reflector.FindTypeForRoot(type, _isEncoded, defaultNamespace);
            // Reflect over the extra types
            if (extraTypes != null && extraTypes.Length > 0)
            {
                _extraTypes = new LogicalType[extraTypes.Length];
                for (int typeNDX = 0; typeNDX < extraTypes.Length; ++typeNDX)
                {
                    _extraTypes[typeNDX] = findTypeByType(extraTypes[typeNDX], defaultNamespace);
                }
            }
            // Reflect over the included types
            _reflector.ReflectIncludedTypes();
            if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                _reflector.ReflectionDisabled = true;
        }

        /// <summary>
        /// Constructs the XmlSerializer using the XmlTypeMapping parameter 
        /// to initialize the logical type(reflection data) of the object
        /// that will be serialized/de-serialized. 
        /// </summary>
        /// <param name="xmlMapping"></param>
        /// <remarks>
        /// The XmlTypeMapping parameter has already reflected over the type, 
        /// we just pull the reflection data from the <see cref="XmlTypeMapping"/> object.
        /// </remarks>
        public XmlSerializer(XmlTypeMapping xmlMapping)
        {
            if (xmlMapping == null)
                throw new ArgumentNullException("xmlMapping");
            _events.sender = this;
            _reflector = xmlMapping.Reflector;
            _logicalType = xmlMapping.LogicalType;
            _isEncoded = xmlMapping.IsSoap;
            if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                _reflector.ReflectionDisabled = true;
        }

        /// <summary>
        /// Constructs the XmlSerializer, reflecting over the given 
        /// types and any types that are statically referenced from them.
        /// </summary>
        /// <param name="extraTypes">The additional types the serializer should be 
        /// prepared to encounter during serialization of the primary <paramref name="type"/>.</param>
        public XmlSerializer(Type type, Type[] extraTypes)
            : this(type, null, extraTypes, null, null)
        {
        }

        /// <summary>
        /// Creates an array of XmlSerializers. There is one
        /// XmlSerializer for each Type object in the specified types array.
        /// </summary>
        public static XmlSerializer[] FromTypes(Type[] types)
        {
            if (types == null)
                return new XmlSerializer[0];

            XmlSerializer[] ret = new XmlSerializer[types.Length];
            for (int typeNdx = 0; typeNdx < types.Length; ++typeNdx)
            {
                ret[typeNdx] = new XmlSerializer(types[typeNdx]);
            }

            return ret;
        }

        /// <summary>
        /// Checks whether this XmlSerializer can begin deserialization at a given XmlElement. 
        /// </summary>
        /// <remarks>
        /// It may return false for types that the serializer can deserialize as part of another type.
        /// </remarks>
        public virtual bool CanDeserialize(XmlReader reader)
        {
            if (_logicalType == null)
                return false;

            if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
            {
                // Check the type passed to the constructor
                if (_isEncoded)
                {
                    if (startElementMatchesAccessor(reader, _logicalType.TypeAccessor, false) ||
                        startElementMatchesAccessor(reader, _logicalType.TypeAccessor, true))
                        return true;
                }
                else
                {
                    if (startElementMatchesAccessor(reader, _logicalType.RootAccessor, false))
                        return true;
                }

                bool soap12;
                XmlSerializationReader serialReader = initXmlSerializationReader(reader, null, out soap12);
                LogicalType type = serialReader.deriveTypeFromTypeAttribute(reader, null);
                if (type != null && _logicalType.Type.IsAssignableFrom(type.Type))
                {
                    return true;
                }
            }
            else
            { /* Rogue or earlier */
                // Check the type passed to the constructor
                if (startElementMatchesAccessor(reader, _logicalType.TypeAccessor, false))
                    return true;

                // Checking extra types is only something we do for backward compatibility
                if (_extraTypes != null)
                {
                    for (int typeNdx = 0; typeNdx < _extraTypes.Length; ++typeNdx)
                    {
                        if (startElementMatchesAccessor(reader, _extraTypes[typeNdx].TypeAccessor, false))
                            return true;
                    }
                }

                LogicalType type = _reflector.FindType(new XmlQualifiedName(reader.Name, reader.NamespaceURI), _isEncoded);
                if (type != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool startElementMatchesAccessor(XmlReader reader, Accessor accessor, bool soap12)
        {
            Debug.Assert(reader != null, "Null XmlReader");
            Debug.Assert(accessor != null, "Null accessor");

            if (true /* AppDomain.CompatVersion > AppDomain.Orcas */)
            {
                Debug.Assert(accessor.Namespace != null, accessor.Type.Type.FullName + " accessor created with null namespace.");
                return reader.IsStartElement(accessor.EncodedName, accessor.GetEncodedNamespace(soap12) ?? string.Empty);
            }
            else
            {
                if (reader.IsStartElement(accessor.EncodedName, accessor.Namespace))
                    return true;
                else
                    return reader.LocalName.Equals(accessor.EncodedName);
            }
        }

#if !FEATURE_LEGACYNETCF
        /// <summary>
        /// Serialize an object.
        /// </summary>
        public void Serialize(TextWriter textWriter, Object o) {
            Serialize(textWriter, o, null);
        }

        public void Serialize(TextWriter textWriter, Object o, XmlSerializerNamespaces namespaces) {
            // Initialize the xml text writer
            XmlTextWriter xmlWriter = new XmlTextWriter(textWriter);
            initWriter(xmlWriter);

            // Serialize object 
            Serialize(xmlWriter, o, namespaces);
        }

        public void Serialize(Stream stream, Object o) {
            Serialize(stream, o, defaultNamespace);
        }

        public void Serialize(Stream stream, Object o, XmlSerializerNamespaces namespaces) {
            // Initialize the xml text writer
            XmlTextWriter xmlWriter = new XmlTextWriter(stream, null);
            initWriter(xmlWriter);

            // Serialize object
            Serialize(xmlWriter, o, namespaces);
        }
#endif

        public void Serialize(XmlWriter xmlWriter, Object o)
        {
            Serialize(xmlWriter, o, null);
        }

        public void Serialize(XmlWriter xmlWriter, Object o, XmlSerializerNamespaces namespaces)
        {
            Serialize(xmlWriter, o, namespaces, null /*EncodingStyle*/);
        }

        [System.Security.FrameworkVisibilitySilverlightInternal]
        public void Serialize(XmlWriter xmlWriter, Object o, XmlSerializerNamespaces namespaces, string encodingStyle)
        {
            Debug.Assert(null != _logicalType, "Reflection info not initialized before calling Serialize");
            try
            {
                bool objIsNotNull = (o != null);
                Type objType = objIsNotNull ? o.GetType() : null;
                LogicalType type;
                if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                {
                    if (objIsNotNull && !_logicalType.Type.IsAssignableFrom(objType))
                        throw new InvalidCastException(SR.Format(SR.XmlInvalidCast, objType.FullName, _logicalType.Type.FullName));
                    type = _logicalType;
                }
                else
                { // Rogue/Whidbey and earlier
                    type = (objIsNotNull && !_logicalType.Type.IsAssignableFrom(objType)) ?
                        findTypeByType(objType, _defaultNamespace) :
                        _logicalType;
                }

                // The soap message formatter considers enums to be non-nullable. 
                // The desktop serializes null enums perfectly. So, we need to 
                // flip this bit, so that we can serialize null enums as well.
                if (type.Type.GetTypeInfo().IsEnum)
                {
                    type.TypeAccessor.IsNullable = type.RootAccessor.IsNullable = true;
                }

                if (objIsNotNull && !_isEncoded)
                {
                    // Use custom handling if we are serializing a non-null primitive using literal encoding.
                    if (SerializationHelper.IsSerializationPrimitive(type.Type))
                    {
                        XmlSerializationWriter.SerializePrimitive(o, xmlWriter, type, _defaultNamespace, _isEncoded);
                        xmlWriter.Flush();
                        return;
                    }

                    // Use custom handling if we are serializing a non-null enumeration using literal encoding.
                    if (type.Type.GetTypeInfo().IsEnum)
                    {
                        XmlSerializationWriter.SerializeEnumeration(o, xmlWriter, type, _defaultNamespace, _isEncoded);
                        xmlWriter.Flush();
                        return;
                    }
                }

                // Initialize the formatter
                XmlSerializationWriter serialWriter = initXmlSerializationWriter(xmlWriter, encodingStyle);

                // Initialize the namespaces that will be passed to the serialization writer                
                if (namespaces == null || namespaces.Count == 0)
                {
                    // Use the default namespaces if the namespaces parameter is null and 
                    // the type requires that we use default namespaces
                    namespaces = useDefaultNamespaces(type) ? defaultNamespace : null;
                }
                else if (objIsNotNull && typeof(System.Xml.Schema.XmlSchema).IsAssignableFrom(objType))
                {
                    // We special case the XmlSchema to consider the Xsd Namespace mapping
                    checkXsdNamespaceMapping(ref namespaces, serialWriter);
                }

                // Serialize the object. 
                serialWriter.SerializeAsElement(_isEncoded ? type.TypeAccessor : type.RootAccessor, o, namespaces);

                // We only flush the stream after writing the object since the user may want 
                // to furthur manipulate the stream once the object is serialized.
                xmlWriter.Flush();
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = e.InnerException;
                throw new InvalidOperationException(SR.XmlGenError, e);
            }
        }

#if !FEATURE_LEGACYNETCF
        /// <summary>
        /// Deserialize an object 
        /// </summary>
        public object Deserialize(Stream stream) {
            XmlTextReader xmlReader = new XmlTextReader(stream);
            initReader(xmlReader);

            return Deserialize(xmlReader);
        }

        public object Deserialize(TextReader textReader) {
            XmlTextReader xmlReader = new XmlTextReader(textReader);
            initReader(xmlReader);

            return Deserialize(xmlReader);
        }
#endif

        public object Deserialize(XmlReader xmlReader)
        {
            return Deserialize(xmlReader, null /*encodingStyle*/);
        }

        [System.Security.FrameworkVisibilitySilverlightInternal]
        public object Deserialize(XmlReader xmlReader, string encodingStyle)
        {
            Debug.Assert(null != _logicalType, "Reflection info not initialized before calling Serialize");

            try
            {
                // Initialize the parser
                bool soap12;
                XmlSerializationReader serialReader = initXmlSerializationReader(xmlReader, encodingStyle, out soap12);
                xmlReader.MoveToContent();

                // Get the correct deserializing type
                LogicalType deserializingType = resolveDeserializingType(xmlReader, serialReader, soap12);

                // Deserialize the element            
                object deserializedObject;
                if (_isEncoded && (isNullableWithStructValue(deserializingType) || isComplexObjectValue(deserializingType)))
                {
                    Type instType = deserializingType.IsNullableType ?
                        deserializingType.NullableValueType.Type :
                        deserializingType.Type;

                    object fixupTarget = SerializationHelper.CreateInstance(instType);
                    serialReader.DeserializeElement(deserializingType.Members, fixupTarget);
                    serialReader.runFixups();

                    deserializedObject = fixupTarget;
                }
                else
                {
                    ObjectFixup fixup = new ObjectFixup(deserializingType);
                    serialReader.DeserializeElement(deserializingType.TypeAccessor, fixup);
                    serialReader.runFixups();
                    deserializedObject = fixup.Object;
                }

                serialReader.CheckUnreferencedObjects(deserializedObject);
                return deserializedObject;
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = e.InnerException;

#if !FEATURE_LEGACYNETCF
                if (xmlReader is XmlTextReader) {
                    XmlTextReader XmlTextReader = (XmlTextReader)xmlReader;
                    throw new InvalidOperationException(SR.Format(SR.XmlSerializeErrorDetails, XmlTextReader.LineNumber.ToString(NumberFormatInfo.InvariantInfo), XmlTextReader.LinePosition.ToString(NumberFormatInfo.InvariantInfo)), e);
                }
                else 
#endif
                {
                    throw new InvalidOperationException(SR.XmlSerializeError, e);
                }
            }
        }

        //*****************************************************************
        // Helpers
        //*****************************************************************	  

        /// <summary>
        /// Checks whether the prefix "xs" is defined as the Xsd uri. If it 
        /// has been defined as something other then the Xsd uri and the 
        /// xsd uri has not been mapped to a prefix then maps the Xsd uri to 
        /// a unique prefix. 
        /// </summary>
        /// <remarks>
        /// This is a work around specifically for the 
        /// XmlSchema object.
        /// 
        /// namespaces = The XmlSerializerNamespaces object that holds 
        ///              namespace mappings. 
        /// serialWriter = The XmlSerializationWriter that will write out 
        ///                the serialized object. This object is used to 
        ///                create a unique prefix.
        /// </remarks>
        private void checkXsdNamespaceMapping(ref XmlSerializerNamespaces namespaces, XmlSerializationWriter serialWriter)
        {
            bool xsdRedefined = false, xsdUriPresent = false;
            const string XmlSchemaPrefix = "xs";

            foreach (XmlQualifiedName qName in namespaces.ToArray())
            {
                if (qName.Name == XmlSchemaPrefix && qName.Namespace != Soap.XsdUrl)
                    xsdRedefined = true;
                if (qName.Namespace == Soap.XsdUrl)
                    xsdUriPresent = true;
            }

            if (xsdRedefined && !xsdUriPresent)
                namespaces.Add(serialWriter.MakePrefix(), Soap.XsdUrl);
        }

        /// <summary>
        /// Returns true if the LogicalType represents a Nullable<T> type 
        /// and the T is a primitive or enum.
        /// </summary>
        private bool isNullableWithStructValue(LogicalType type)
        {
            return type.IsNullableType &&
                !SerializationHelper.IsSerializationPrimitive(type.NullableValueType.Type) &&
                !type.NullableValueType.Type.GetTypeInfo().IsEnum;
        }

        /// <summary>
        /// Returns true if the LogicalType represents a complex type 
        /// (struct or reference type). 
        /// </summary> 
        /// <remarks>
        /// Specifically a type that is not Nullable<T>, not a logical array,
        /// not a primitive, not an enum, and not a QName.
        /// </remarks>
        private bool isComplexObjectValue(LogicalType type)
        {
            if (!type.IsNullableType &&
                !SerializationHelper.isLogicalArray(type) &&
                !SerializationHelper.IsSerializationPrimitive(type.Type) &&
                !type.Type.GetTypeInfo().IsEnum &&
                type.CustomSerializer != CustomSerializerType.QName &&
                !SerializationHelper.isBuiltInBinary(type))
                return true;
            return false;
        }

        private bool useDefaultNamespaces(LogicalType type)
        {
            return (!type.IsNullableType || isNullableWithStructValue(type)) &&   // Nullable<T> with a struct
                   (!typeof(IXmlSerializable).IsAssignableFrom(type.Type));       // Not an IXmlSerializable                       
        }

        /// <summary>
        /// Gets the logical type to deserialize.
        /// </summary>
        /// <remarks>
        /// This is done by checking the type attributes on the current start element.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the type attributes are not present.</exception>
        private LogicalType resolveDeserializingType(XmlReader reader, XmlSerializationReader serialReader, bool soap12)
        {
            if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
            {
                LogicalType type = serialReader.deriveTypeFromTypeAttribute(reader, null);
                if (type != null && _logicalType.Type.IsAssignableFrom(type.Type))
                {
                    return type;
                }

                Accessor accessor = _isEncoded ? _logicalType.TypeAccessor : _logicalType.RootAccessor;
                if (startElementMatchesAccessor(reader, accessor, soap12))
                {
                    return _logicalType;
                }

                throw serialReader.CreateUnknownNodeException();
            }
            else
            { /* Rogue or earlier */
                LogicalType type = serialReader.deriveTypeFromTypeAttribute(reader, null);
                if (type != null)
                {
                    return type;
                }

                if (startElementMatchesAccessor(reader, _logicalType.TypeAccessor, soap12))
                {
                    return _logicalType;
                }

                type = _reflector.FindType(new XmlQualifiedName(reader.Name, reader.NamespaceURI), _isEncoded);
                if (type != null)
                {
                    return type;
                }

                return _logicalType;
            }
        }



#if !FEATURE_LEGACYNETCF
        /// <summary>
        /// This method initializes the XmlTextWriter that will be used to 
        /// serialize the object.
        /// </summary>
        /// <remarks>
        /// The SoapMessageFormatter uses an XmlTextWriter and not the 
        /// abstract XmlWriter, so specialized XmlWriters cannot be used. 
        /// Only XmlTextWriters and their children can be used.
        /// </remarks>
        void initWriter(XmlWriter xmlWriter) {
            if (typeof(XmlTextWriter).IsAssignableFrom(xmlWriter.GetType())) {
                XmlTextWriter writer = xmlWriter as XmlTextWriter;
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
            }
        }

        /// <summary>
        /// This method initializes the XmlTextReader that will be used to 
        /// deserialize the object. 
        /// </summary>
        void initReader(XmlReader xmlReader) {
            if (typeof(XmlTextReader).IsAssignableFrom(xmlReader.GetType())) {
                XmlTextReader reader = xmlReader as XmlTextReader;
                reader.WhitespaceHandling = WhitespaceHandling.Significant;
                reader.Normalization = true;
            }
        }
#endif

        /// <summary>
        /// Gets the reflection data (logical type) of a given CLR type.  
        /// </summary>
        /// <remarks>
        /// The type is reflected over if it has not been previously,
        /// provided reflection hasn't been disabled.
        /// </remarks>
        private LogicalType findTypeByType(Type type, string defaultNamespace)
        {
            System.Diagnostics.Debug.Assert(null != _reflector, "The XmlSerializationReflector has not been initialized.");
            bool dontCheckIntrinsics = false;
            if (_isEncoded)
            {
                if (_reflector.SoapAttributeOverrides != null && _reflector.SoapAttributeOverrides[type] != null)
                {
                    dontCheckIntrinsics = _reflector.SoapAttributeOverrides[type].SoapType != null;
                }
            }
            else
            {
                if (_reflector.XmlAttributeOverrides != null && _reflector.XmlAttributeOverrides[type] != null)
                {
                    dontCheckIntrinsics = _reflector.XmlAttributeOverrides[type].XmlRoot != null ||
                                            _reflector.XmlAttributeOverrides[type].XmlType != null;
                }
            }

            return _reflector.FindType(type, _isEncoded, defaultNamespace, dontCheckIntrinsics ? TypeOrigin.User : TypeOrigin.All);
        }

        /// <summary>
        /// Initializes the XmlSerializationWrter used to serialize the object.
        /// </summary>
        private XmlSerializationWriter initXmlSerializationWriter(XmlWriter xmlWriter, string encodingStyle)
        {
            System.Diagnostics.Debug.Assert(null != _reflector, "The XmlSerializationReflector has not been initialized.");
            if (!_isEncoded && encodingStyle != null)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidEncodingNotEncoded1, encodingStyle));

            bool soap12 = false;
            verifyEncodingStyle(encodingStyle, out soap12);
            XmlSerializationWriter serialWriter = new XmlSerializationWriter(xmlWriter, _reflector, soap12);
            serialWriter.Encoded = _isEncoded;

            return serialWriter;
        }

        /// <summary>
        /// Initializes the XmlSerializationReader used to deserialize the 
        /// object.
        /// </summary>
        private XmlSerializationReader initXmlSerializationReader(XmlReader reader, string encodingStyle, out bool soap12)
        {
            System.Diagnostics.Debug.Assert(null != _reflector, "The XmlSerializationReflector has not been initialized.");
            if (!_isEncoded && encodingStyle != null)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidEncodingNotEncoded1, encodingStyle));

            verifyEncodingStyle(encodingStyle, out soap12);
            XmlSerializationReader xmlSerializationReader = new XmlSerializationReader(reader, _events, _reflector, soap12, _isEncoded);
            return xmlSerializationReader;
        }

        /// <summary>
        /// Ensures that the encodingStyle string is either Soap.Encoding or 
        /// Soap12.Encoding. The soap12 parameter is set to true if the 
        /// encodingStyle string equals Soap12.Encoding.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when an encoding style other than SOAP and SOAP 1.2 is specified.</exception>
        private void verifyEncodingStyle(string encodingStyle, out bool soap12)
        {
            if (encodingStyle != null && encodingStyle != Soap.Encoding && encodingStyle != Soap12.Encoding)
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidEncoding3, encodingStyle, Soap.Encoding, Soap12.Encoding));

            soap12 = (encodingStyle == Soap12.Encoding);
        }

        private static XmlSerializerNamespaces defaultNamespace
        {
            get
            {
                if (s_DEFAULT_NAMESPACES == null)
                {
                    XmlSerializerNamespaces nss = new XmlSerializerNamespaces();
                    nss.Add(Soap.Xsd, Soap.XsdUrl);
                    nss.Add(Soap.Xsi, Soap.XsiUrl);
                    if (s_DEFAULT_NAMESPACES == null)
                    {
                        s_DEFAULT_NAMESPACES = nss;
                    }
                }
                return s_DEFAULT_NAMESPACES;
            }
        }

#if !FEATURE_LEGACYNETCF
        public event XmlNodeEventHandler UnknownNode
        {

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            add {
                events.onUnknownNode += value;
            }

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            remove {
                events.onUnknownNode -= value;
            }
        }

        public event XmlAttributeEventHandler UnknownAttribute
        {

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            add {
                events.onUnknownAttribute += value;
            }

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            remove {
                events.onUnknownAttribute -= value;
            }
        }

        public event XmlElementEventHandler UnknownElement
        {

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            add {
                events.onUnknownElement += value;
            }

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            remove {
                events.onUnknownElement -= value;
            }
        }

        public event UnreferencedObjectEventHandler UnreferencedObject
        {

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            add {
                events.onUnreferencedObject += value;
            }

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            remove {
                events.onUnreferencedObject -= value;
            }
        }
#endif


        [System.Security.FrameworkVisibilitySilverlightInternal]
        public void Serialize(XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces, string encodingStyle, string id)
        {
            throw new NotSupportedException();
        }

        public static XmlSerializer[] FromMappings(XmlMapping[] mappings)
        {
            throw new NotSupportedException();
        }

        public static XmlSerializer[] FromMappings(XmlMapping[] mappings, Type type)
        {
            throw new NotSupportedException();
        }
    }
}
