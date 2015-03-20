// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

#pragma warning disable 0162
#pragma warning disable 0429
using System;
using System.Linq;
using System.Xml;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Xml.Extensions;

namespace System.Xml.Serialization.LegacyNetCF
{
    internal partial class XmlSerializationReflector
    {
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Type Look Up
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//

        /// <summary>
        /// Collections can be implemented by derivative types without reflecting over
        /// those derivative types.  This method converts a runtime type to its nearest
        /// reflected base collection type.
        /// </summary>
        private Type findPostReflectionType(Type type, string ns, bool encoded, TypeOrigin originsToSearch)
        {
            Debug.Assert(type != null);
            TypeOrigin typeOrigin;

            // If the type passed in has already been reflected over, just return it.
            if (m_TypeContainer.ContainsType(type, ns, encoded, originsToSearch, out typeOrigin))
                return type;

            // Try to find the declared type, or some other type closer to what might have
            // been reflected over.

            // If the type is an array, look for an array type with elements of a base class.
            if (type.IsArray)
            {
                Type baseType = type.GetElementType().GetTypeInfo().BaseType;
                if (baseType != null && !string.IsNullOrEmpty(baseType.FullName))
                {
                    Type baseTypeArray = baseType.GetTypeInfo().Assembly.GetType(baseType.FullName + "[]");
                    Debug.Assert(baseTypeArray != null);
                    return findPostReflectionType(baseTypeArray, ns, encoded, originsToSearch);
                }
            }
            else if (type.GetTypeInfo().BaseType != null && SerializationHelper.isEnumerable(type.GetTypeInfo().BaseType))
            {
                // If the type has a base class that is a collection type, try that one.
                return findPostReflectionType(type.GetTypeInfo().BaseType, ns, encoded, originsToSearch);
            }
            // No reflected type could be found.  Give up.
            return null;
        }

        public LogicalType FindTypeForRoot(Type type, bool encoded, string defaultNamespace)
        {
            return AddType(type, encoded, defaultNamespace, false, true);
        }

        /// <summary>
        /// This method should be called to reflect over a type. If the type already 
        /// exists in the TypeContainer then it will not be reflected again. If the type
        /// has not been reflected over it and its members will be reflected over before
        /// being added to the TypeContainer using the default namespace.
        /// </summary>
        public LogicalType FindType(Type type, bool encoded, string defaultNamespace)
        {
            return FindType(type, encoded, defaultNamespace, TypeOrigin.All, false/*genericNullableArg*/);
        }

        public LogicalType FindType(Type type, bool encoded, string defaultNamespace, TypeOrigin originsToSearch)
        {
            return FindType(type, encoded, defaultNamespace, originsToSearch, false/*genericNullableArg*/);
        }

        public LogicalType FindType(Type type, bool encoded, string defaultNamespace, TypeOrigin originsToSearch, bool genericNullableArg)
        {
            return FindType(type, encoded, defaultNamespace, originsToSearch, genericNullableArg, true);
        }

        private LogicalType FindTypeNoThrow(Type type, bool encoded, string defaultNamespace, TypeOrigin originsToSearch, bool genericNullableArg)
        {
            return FindType(type, encoded, defaultNamespace, originsToSearch, genericNullableArg, false);
        }

        /// <summary>
        /// This method should be called to reflect over a type. If the type already 
        /// exists in the TypeContainer then it will not be reflected again. If the type
        /// has not been reflected over it and its members will be reflected over before
        /// being added to the TypeContainer using the default namespace. 
        /// </summary>
        /// <param name="searchIntrinsics">
        /// Determines whether the intrinsic types should be
        /// searched while attempting to find the type in the TypeContainer. If 
        /// searchIntrinsics is false, then intrinsic types are excluded from the
        /// search.
        /// </param>
        private LogicalType FindType(Type type, bool encoded, string defaultNamespace, TypeOrigin originsToSearch, bool genericNullableArg, bool canThrow)
        {
            TypeOrigin typeOrigin;
            Type substituteType;

            if (m_TypeContainer.ContainsType(type, defaultNamespace, encoded, originsToSearch, out typeOrigin))
            {
                return m_TypeContainer.GetType(type, defaultNamespace, encoded, originsToSearch, out typeOrigin);
            }
            else if (!m_ReflectionDisabled)
            {
                return AddType(type, encoded, defaultNamespace, genericNullableArg, false);
            }
            else if ((substituteType = findPostReflectionType(type, defaultNamespace, encoded, originsToSearch)) != null)
            {
                return m_TypeContainer.GetType(substituteType, defaultNamespace, encoded, originsToSearch, out typeOrigin);
            }
            else
            {
                if (canThrow)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, type.FullName));
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Searches a list of namespaces for a previously-reflected LogicalType and returns
        /// the first match it finds.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no match could be found.</exception>
        /// <remarks>
        /// This method may be a kludge.  Ideally the XmlSerializationReader/Writer would know exactly
        /// which namespace something should be found in.  Until that's fixed up,
        /// this method is there to help.
        /// </remarks>
        public LogicalType FindTypeInNamespaces(Type type, bool encoded, params string[] namespaces)
        {
            Debug.Assert(type != null);
            Debug.Assert(namespaces != null);
            Debug.Assert(namespaces.Length > 0);
            foreach (string ns in namespaces)
            {
                LogicalType lType = FindTypeNoThrow(type, encoded, ns, TypeOrigin.All, false);
                if (lType != null)
                {
                    return lType;
                }
            }
            throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, type.FullName));
        }

        /// <summary>
        /// Searches a list of namespaces for a previously-reflected LogicalType and returns
        /// the first match it finds.
        /// </summary>
        /// Returns null if match not found.
        /// <remarks>
        /// This method may be a kludge.  Ideally the XmlSerializationReader/Writer would know exactly
        /// which namespace something should be found in.  Until that's fixed up,
        /// this method is there to help.
        /// </remarks>
        public LogicalType FindTypeInNamespacesNoThrow(Type type, bool encoded, params string[] namespaces)
        {
            Debug.Assert(type != null);
            Debug.Assert(namespaces != null);
            Debug.Assert(namespaces.Length > 0);
            foreach (string ns in namespaces)
            {
                LogicalType lType = FindTypeNoThrow(type, encoded, ns, TypeOrigin.All, false);
                if (lType != null)
                {
                    return lType;
                }
            }
            return null;
        }

        /// <summary>
        /// This method should be called to reflect over type specified by a string. 
        /// All of the data type are assumed to be in the xsd namespace. If the type is
        /// not in the TypeContainer's cache then an InvalidOperationException is 
        /// thrown.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the type is not in the TypeContainer's cache.
        /// </exception>
        public LogicalType FindType(string dataType, bool encoded)
        {
            TypeOrigin typeOrigin;

            LogicalType retLType = m_TypeContainer.GetType(new XmlQualifiedName(dataType, Soap.XsdUrl), encoded, out typeOrigin);
            if (retLType == null)
                throw new InvalidOperationException(SR.Format(SR.XmlS_IllegalDataType_1, dataType));
            return retLType;
        }

        /// <summary>
        /// This method should be called to find a type by name and namespace. If the 
        /// type is not found then null is returned.
        /// </summary>
        public LogicalType FindType(XmlQualifiedName qname, bool encoded)
        {
            TypeOrigin typeOrigin;

            if (!m_TypeContainer.ContainsType(qname, encoded, out typeOrigin))
                return SynthesizeArrayType(qname, encoded);
            return m_TypeContainer.GetType(qname, encoded, out typeOrigin);
        }

        /// <summary>
        /// This takes the data type, type, and member type for an attribute and 
        /// produces the logical type specified by this attribute.  If it is a logical 
        /// array, then isArray is true and elementType is initialized.
        /// </summary>
        protected void ResolveLiteralType(string attrDataType, Type attrType, Type memberType, string defaultNS,
            out bool isArray, out LogicalType type, out LogicalType elementType)
        {
            elementType = null;
            isArray = false;

            type = GetLiteralTypeFromElementAttribute(attrDataType, attrType, defaultNS);
            if (type == null)
            {
                ResolveLiteralTypeUsingDeclaredType(memberType, defaultNS, out type, out elementType, out isArray);
                return;
            }

            if (memberType == typeof(byte[]))
            {
                if (!memberType.IsAssignableFrom(type.Type))
                    throw new InvalidOperationException(SR.Format(SR.XmlS_DataTypeNotValid_1, type.Type));
                isArray = false;
            }
            else if (memberType.HasElementType)
            {
                if (!memberType.GetElementType().IsAssignableFrom(type.Type))
                    throw new InvalidOperationException(SR.Format(SR.XmlS_DataTypeNotValid_1, type.Type));
                isArray = true;
                elementType = type;
                type = FindType(memberType, false/*encoded*/, defaultNS);
            }
            else if (SerializationHelper.isCollection(memberType))
            {
                PropertyInfo propertyInfo;
                Type collectionElementType = GetCollectionElementType(memberType, out propertyInfo);
                if (!collectionElementType.IsAssignableFrom(type.Type))
                    throw new InvalidOperationException(SR.Format(SR.XmlS_DataTypeNotValid_1, type.Type));
                isArray = true;
                elementType = type;
                type = FindType(memberType, false/*encoded*/, defaultNS);
            }
            else if (SerializationHelper.isEnumerable(memberType))
            {
                Type enumeratorElementType = GetEnumeratorElementType(memberType);
                Debug.Assert(enumeratorElementType != null);
                if (!enumeratorElementType.IsAssignableFrom(type.Type))
                    throw new InvalidOperationException(SR.Format(SR.XmlS_DataTypeNotValid_1, type.Type));
                isArray = true;
                elementType = type;
                type = FindType(memberType, false/*encoded*/, defaultNS);
            }
            else if (memberType.IsAssignableFrom(type.Type))
            {
                if (SerializationHelper.isLogicalArray(type))
                {
                    Debug.Assert(type.TypeAccessor != null, "Null TypeAccessor for array member");
                    Debug.Assert(type.TypeAccessor.NestedAccessors != null, "Null NestedAccessors for array member");
                    Debug.Assert(type.TypeAccessor.NestedAccessors.Default != null, "Null Default accessors in the TypeAccessor.NestedAccessor for array member");
                    elementType = type.TypeAccessor.NestedAccessors.Default.Type;
                }
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.XmlS_DataTypeNotValid_1, attrDataType));
            }
        }

        /// <summary>
        /// Determines the LogicalType given the properties of an XmlElement attribute.
        /// </summary>
        /// <example>
        ///  [XmlElement(DateType = "string")]   =>  String LogicalType 
        ///  [XmlElement(Type = typeof(int)]     =>  Int LogicalType
        /// </example>
        private LogicalType GetLiteralTypeFromElementAttribute(string attrDataType, Type attrType, string defaultNS)
        {
            LogicalType type = null;
            if (!string.IsNullOrEmpty(attrDataType))   // Example: XmlElement(DataType = "object")
                type = FindType(attrDataType, false/*encoded*/ );
            else if (attrType != null)                             // Example: XmlElement(Type = typeof(object))                 
                type = FindType(attrType, false/*encoded*/, defaultNS);

            return type;
        }

        /// <summary>
        /// Determines the LogicalType using the member's declared type.
        /// </summary>
        /// <example>
        ///  public string StringProperty    =>  String LogicalType
        ///  public int IntProperty          =>  Int LogicalType
        ///  public string[] ArrayProperty   =>  String Array LogicalType 
        /// </example>
        private void ResolveLiteralTypeUsingDeclaredType(Type memberType, string defaultNS,
            out LogicalType type, out LogicalType elementType, out bool isArray)
        {
            type = FindType(memberType, false/*encoded*/, defaultNS);

            if (SerializationHelper.isBuiltInBinary(type))
            {
                elementType = null;
                isArray = false;
            }
            else if (SerializationHelper.isArray(memberType))
            {
                elementType = type.TypeAccessor.NestedAccessors.Default.Type;
                isArray = true;
            }
            else if (SerializationHelper.isCollection(memberType) && !SerializationHelper.IsSerializationPrimitive(memberType))
            {
                PropertyInfo propertyInfo;
                elementType = FindType(GetCollectionElementType(memberType, out propertyInfo), false, defaultNS);
                isArray = true;
            }
            else if (SerializationHelper.isEnumerable(memberType) && !SerializationHelper.IsSerializationPrimitive(memberType))
            {
                elementType = FindType(GetEnumeratorElementType(memberType), false, defaultNS);
                isArray = true;
            }
            else
            {
                elementType = null;
                isArray = false;
            }
        }

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Adding New Types
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//

        /// <summary>
        /// Adds a new LogicalType for a type that already has a LogicalType in the intrinsics
        /// table.  This is useful because intrinsic types may have their namespaces or names
        /// changed, and having a new LogicalType to represent the variant.
        /// </summary>
        /// <param name="rootName">The tag name this type should be serialized with.</param>
        /// <param name="defaultNS">The namespace this tag should serialize with.</param>
        /// <remarks>This should do roughly what TypeContainer.MakeIntrinsicType(...) does.</remarks>
        protected LogicalType AddIntrinsicType(Type type, string defaultNS, TypeAttributes attrs, bool encoded)
        {
            Debug.Assert(TypeContainer.ContainsIntrinsicType(type));
            LogicalType existingIntrinsicType = TypeContainer.GetIntrinsicType(type);

            string rootName = attrs.XmlRoot != null && !string.IsNullOrEmpty(attrs.XmlRoot.ElementName)
                ? attrs.XmlRoot.ElementName : existingIntrinsicType.RootAccessor.Name;

            LogicalType lType = new LogicalType(
                rootName,
                defaultNS,
                existingIntrinsicType.NamespaceIsPortable,
                existingIntrinsicType.Type,
                existingIntrinsicType.IsNullable,
                existingIntrinsicType.Serializer,
                existingIntrinsicType.TypeID,
                existingIntrinsicType.MappingFlags
            );
            lType.CustomSerializer = existingIntrinsicType.CustomSerializer;
            if (existingIntrinsicType.CustomSerializer == CustomSerializerType.Object)
            {
                lType.Members = new MemberValueCollection();
            }
            m_TypeContainer.AddType(lType, rootName, defaultNS, encoded);
            return lType;
        }

        /// <summary>
        /// Adds a LogicalType that represents a concrete Nullable&lt;T&gt; instance to the 
        /// TypeContainer. The Nullable&lt;T&gt; must not have any generic parameters. Meaning 
        /// that all T must be bound to a runtime type. For example, Nullable&lt;T&gt; is 
        /// rejected but Nullable&lt;int&gt; is acceptable.
        /// </summary>
        /// <param name="nullableType">Runtime type of the concrete Nullable&lt;T&gt; instance.</param>
        /// <param name="typeName">The name of the Nullable type. In most cases this is 
        ///     "NullableOfT" where T is the name of the value type. For example the 
        ///     typeName for Nullable&lt;int&gt; is usually "NullableOfInt32". This name 
        ///     is used to maps the LogicalType to its XmlQualifiedName.</param>
        /// <param name="typeNS">The namespce of the Nullable type.</param>
        /// <param name="attrs">The type attributes that decorate nullableType. 
        ///     This is usually null, but there are cases where an XmlRoot, XmlType, or 
        ///     SoapType is applied to the nullableType. In these cases we pull the 
        ///     properties off of the attributes and use them to initialize the 
        ///     LogicalType's accessors.</param>
        /// <param name="encoded">true if we should use encoding semantics, false to use 
        ///     literal semantics</param>
        /// <param name="defaultNS">The default namespace of the type if none is specified.</param>
        protected LogicalType AddNullableType(Type nullableType, string typeName, string typeNS, bool namespaceIsPortable, TypeAttributes attrs, bool encoded, string defaultNS)
        {
            Debug.Assert(IsNullableType(nullableType), "The type argument must be a Nullable<T> type");
            Debug.Assert(nullableType.GetGenericArguments()[0] != null, "Optional value missing a generic parameter");

            Type valueType = nullableType.GetGenericArguments()[0];
            // We do not search intrinsics in order to get a primitive with the correct RootAccessor.Namespace.
            // Getting it "correct" only works the first time you request a custom primitive, but if RootAccessor
            // even matters, then it will be the first one we ask for since that is where reflection begins.
            LogicalType valueLType = FindType(valueType, encoded, defaultNS, TypeOrigin.User, true/*genericNullableArg*/);
            LogicalType nullableLType = new LogicalType(valueLType.Name, valueLType.TypeAccessor.Namespace, namespaceIsPortable, nullableType, true, valueLType.Serializer, valueLType.TypeID, TypeMappingFlags.Default);
            nullableLType.Members = valueLType.Members;
            nullableLType.IsNullableType = true;
            nullableLType.NullableValueType = valueLType;

            // The TypeAccessor is copied from the TypeAccessor of the value type. The
            // TypeAccessor.Type is changed to the Nullable's LogicalType. If there is an 
            // XmlType attribute present on the Nullable then TypeAccessor.Name and
            // TypeAccessor.Namespace is set to XmlType.TypeName and 
            // XmlType.Namespace respectively.
            nullableLType.TypeAccessor = valueLType.TypeAccessor.copy();
            nullableLType.TypeAccessor.Type = nullableLType;
            nullableLType.TypeAccessor.IsNullable = true;
            if (!encoded)
            {
                if (attrs != null && attrs.XmlType != null)
                {
                    if (attrs.XmlType.TypeName != null && attrs.XmlType.TypeName.Length > 0)
                        nullableLType.TypeAccessor.Name = attrs.XmlType.TypeName;
                    if (attrs.XmlType.Namespace != null)
                        nullableLType.TypeAccessor.Namespace = attrs.XmlType.Namespace;
                }
            }

            // The RootAccessor is copied from the RootAccessor of the value type. The
            // RootAccessor.Type is changed to the Nullable's LogicalType. If there is an 
            // XmlRoot attribute present on the Nullable then RootAccessor.Name,
            // RootAccessor.Namespace is set to XmlRoot.TypeName and 
            // XmlRoot.Namespace respectively.
            nullableLType.RootAccessor = valueLType.RootAccessor.copy();
            nullableLType.RootAccessor.Type = nullableLType;
            nullableLType.RootAccessor.IsNullable = true;
            if (!encoded && attrs != null && attrs.XmlRoot != null)
            {
                if (attrs.XmlRoot.ElementName != null && attrs.XmlRoot.ElementName.Length > 0)
                    nullableLType.RootAccessor.Name = attrs.XmlRoot.ElementName;
                if (attrs.XmlRoot.Namespace != null)
                    nullableLType.RootAccessor.Namespace = attrs.XmlRoot.Namespace;
            }

            TypeOrigin typeOriginAlreadyExists;
            bool nameAlreadyMapped = m_TypeContainer.ContainsType(new XmlQualifiedName(typeName, typeNS), encoded, out typeOriginAlreadyExists);
            if (nameAlreadyMapped)
            {
                nullableLType.MappingFlags &= ~TypeMappingFlags.AllowNameMapping;
            }

            // Add the nullable type to the type container. The type is aliased using the 
            // specified typeName and typeNS. This allows us to look up the Nullable by 
            // its type name, "NullableOfInt32", but when serializing use the name in the 
            // TypeAccessor or RootAccessor, i.e "int"
            m_TypeContainer.AddType(nullableLType, typeName, typeNS, encoded);

            return nullableLType;
        }

        /// <summary>
        /// Adds a LogicalType that represents a concrete Nullable&lt;T&gt; instance to the 
        /// TypeContainer. The Nullable&lt;T&gt; must not have any generic parameters. Meaning 
        /// that all T must be bound to a runtime type. For example, Nullable&lt;T&gt; is 
        /// rejected but Nullable&lt;int&gt; is acceptable.
        /// </summary>
        /// <param name="primitiveType">Runtime type of the concrete Nullable&lt;T&gt; instance.</param>
        /// <param name="typeName">The name of the Nullable type. In most cases this is 
        ///      "NullableOfT" where T is the name of the value type. For example the 
        ///      typeName for Nullable&lt;int&gt; is usually "NullableOfInt32". This name is 
        ///      used to maps the LogicalType to its XmlQualifiedName.</param>
        /// <param name="typeNS">The namespce of the Nullable type.</param>
        /// <param name="attrs">The type attributes that decorate nullableType. This 
        ///      is usually null, but there are cases where an XmlRoot, XmlType, or 
        ///      SoapType is applied to the nullableType. In these cases we pull the 
        ///      properties off of the attributes and use them to initialize the 
        ///      LogicalType's accessors.</param>
        /// <param name="encoded">true if we should use encoding semantics, false to use 
        ///      literal semantics</param>
        /// <param name="defaultNS">The default namespace of the type if none is specified.</param>
        private LogicalType AddPrimitiveType(Type primitiveType, string typeName, string typeNS, TypeAttributes attrs, bool encoded, string defaultNS)
        {
            Debug.Assert(SerializationHelper.IsSerializationPrimitive(primitiveType), "The type argument must be a primtive type");

            // Look up a template of this primitive type, and not care about namespaces
            // as we'll override it in this method anyway.
            LogicalType lType = FindType(primitiveType, encoded, defaultNS, TypeOrigin.Intrinsic);
            Debug.Assert(lType.Serializer == SerializerType.Primitive);
            LogicalType primitiveLType = new LogicalType(lType.Name, typeNS, true, primitiveType, true, SerializerType.Primitive, lType.TypeID, TypeMappingFlags.Default);
            primitiveLType.Members = lType.Members;

            // The TypeAccessor is copied from the TypeAccessor of the value type. The
            // TypeAccessor.Type is changed to the Nullable's LogicalType. If there is an 
            // XmlType attribute present on the Nullable then TypeAccessor.Name and
            // TypeAccessor.Namespace is set to XmlType.TypeName and 
            // XmlType.Namespace respectively.
            primitiveLType.TypeAccessor = lType.TypeAccessor.copy();
            primitiveLType.TypeAccessor.Type = primitiveLType;
            primitiveLType.TypeAccessor.IsNullable = true;
            if (!encoded)
            {
                if (attrs != null && attrs.XmlType != null)
                {
                    if (attrs.XmlType.TypeName != null && attrs.XmlType.TypeName.Length > 0)
                        primitiveLType.TypeAccessor.Name = attrs.XmlType.TypeName;
                    if (attrs.XmlType.Namespace != null && attrs.XmlType.Namespace.Length > 0)
                        primitiveLType.TypeAccessor.Namespace = attrs.XmlType.Namespace;
                }
            }
            else
            {
                if (attrs != null && attrs.SoapType != null)
                {
                    if (attrs.SoapType.TypeName != null && attrs.SoapType.TypeName.Length > 0)
                        primitiveLType.TypeAccessor.Name = attrs.SoapType.TypeName;
                    if (attrs.SoapType.Namespace != null && attrs.SoapType.Namespace.Length > 0)
                        primitiveLType.TypeAccessor.Namespace = attrs.SoapType.Namespace;
                }
            }

            // The RootAccessor is copied from the RootAccessor of the value type. The
            // RootAccessor.Type is changed to the Nullable's LogicalType. If there is an 
            // XmlRoot attribute present on the Nullable then RootAccessor.Name,
            // RootAccessor.Namespace is set to XmlRoot.TypeName and 
            // XmlRoot.Namespace respectively.
            primitiveLType.RootAccessor = lType.RootAccessor.copy();
            primitiveLType.RootAccessor.Type = primitiveLType;
            primitiveLType.RootAccessor.Namespace = defaultNS;
            if (!encoded)
            {
                if (attrs != null && attrs.XmlRoot != null)
                {
                    if (attrs.XmlRoot.ElementName != null && attrs.XmlRoot.ElementName.Length > 0)
                        primitiveLType.RootAccessor.Name = attrs.XmlRoot.ElementName;
                    if (attrs.XmlRoot.Namespace != null)
                        primitiveLType.RootAccessor.Namespace = attrs.XmlRoot.Namespace;
                }
            }

            // Add the primitive type to the type manager. The type is aliased using the specified 
            // typeName and typeNS. This allows us to look up the Nullable by its type name,
            // "NullableOfInt32", but when serializing use the name in the TypeAccessor or 
            // RootAccessor, i.e "int"
            m_TypeContainer.AddType(primitiveLType, typeName, typeNS, encoded);

            return primitiveLType;
        }

        protected LogicalType AddComplexType(Type type, TypeAttributes attrs, string typeName, string typeNS, bool namespaceIsPortable, bool typeIsNullable, bool encoded, string defaultNS, bool genericNullableArg)
        {
            LogicalType lType;
            if (encoded)
            {
                lType = new LogicalType(typeName, typeNS, namespaceIsPortable, type, typeIsNullable, SerializerType.Complex, TypeID.Compound, TypeMappingFlags.Default);
            }
            else
            {
                string rootName = typeName;
                string rootNS;
                if (false /* AppDomain.CompatVersion <= AppDomain.Whidbey */)
                    rootNS = typeNS ?? defaultNS;
                else
                    rootNS = defaultNS;
                bool rootIsNullable = typeIsNullable;
                if (attrs.XmlRoot != null)
                {
                    XmlRootAttribute xra = attrs.XmlRoot;
                    if (xra.ElementName != null && xra.ElementName.Length > 0)
                        rootName = xra.ElementName;
                    if (xra.Namespace != null)
                        rootNS = xra.Namespace;
                    if (xra.IsNullableSpecified)
                        rootIsNullable = xra.IsNullable;
                    if (rootIsNullable && type.GetTypeInfo().IsValueType && !genericNullableArg)
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidIsNullable, type));
                }

                TypeMappingFlags typeMapping = (attrs.XmlType != null && attrs.XmlType.AnonymousType) ?
                    TypeMappingFlags.AllowTypeMapping : TypeMappingFlags.Default;

                lType = new LogicalType(typeName, typeNS, namespaceIsPortable, type, typeIsNullable, rootName, rootNS, rootIsNullable, SerializerType.Complex, TypeID.Compound, typeMapping);
                lType.IsAnonymousType = attrs.XmlType != null && attrs.XmlType.AnonymousType;
            }

            // Add the type early, so types that reference themselves work
            m_TypeContainer.AddType(lType, encoded);

            MemberValueCollection members = new MemberValueCollection();
            lType.MembersToAddLate = new Queue<AddMembersLateDelegate>(0);

            string defaultMemberNS = encoded ? string.Empty : typeNS;

            IEntityFinder choiceFinder = new MemberFinder(type);
            bool shouldBeOrdered = false;

            // To match element ordering with desktop, we keep a sequenceId to track the original order that
            // elements are reflected over in the class.  We deliberately read all fields first and then all properties.
            // Although that could change ordering around when fields are intermixed with properties in their
            // declaration, it's consistent with desktop, which is important for compatibility.
            int sequenceId = 0;

            // for each MemberInfo, build a LogicalMemberValue for it.
            // if the member value is null, next MemberInfo.
            // look for XmlSerializationNamespaces.  Any other special types?
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int curFieldNdx = 0; curFieldNdx < fields.Length; ++curFieldNdx)
            {
                FieldInfo field = fields[curFieldNdx];
                addComplexTypeMemberHelper(type, field, encoded,
                    ref shouldBeOrdered, choiceFinder, members, defaultMemberNS, ref sequenceId);
            }

            PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo pi = props[i];
                // All properties must have read accessors. Properties that are readonly
                // must be ICollections or IEnumerable (something with an Add method).                                
                if (CheckProperty(pi, type))
                {
                    addComplexTypeMemberHelper(type, pi, encoded,
                        ref shouldBeOrdered, choiceFinder, members, defaultMemberNS, ref sequenceId);
                }
            }

            lType.Members = members;
            while (lType.MembersToAddLate.Count > 0) lType.MembersToAddLate.Dequeue()();
            lType.MembersToAddLate = null;
            members.prepare();

            // Reflect any included types. Must be done after the decorated type has been reflected                     
            ReflectIncludeAttributes(attrs);

            return lType;
        }

        private void addComplexTypeMemberHelper(Type type, MemberInfo member, bool encoded,
            ref bool shouldBeOrdered, IEntityFinder choiceFinder,
            MemberValueCollection members, string typeNS, ref int sequenceId)
        {
            Debug.Assert(member != null);
            Debug.Assert(type != null);
            Debug.Assert(members != null);
            SpecialMember specialType;

            FieldInfo field = member as FieldInfo;
            PropertyInfo pi = member as PropertyInfo;
            Debug.Assert(field != null || pi != null);
            Type memberType = field != null ? field.FieldType : pi.PropertyType;
            try
            {
                if (member.DeclaringType != type)
                {
                    LogicalType declaringType = FindType(member.DeclaringType, encoded, typeNS);
                    ++sequenceId;
                    if (declaringType.Members == null)
                    {
                        members.DelayedMemberCount++;
                        int anonymousSequenceId = sequenceId;
                        declaringType.MembersToAddLate.Enqueue(delegate
                        {
                            AddMemberUsingDeclaringType(type, member, members, typeNS, encoded, anonymousSequenceId);
                            members.DelayedMemberCount--;
                        });
                    }
                    else
                    {
                        AddMemberUsingDeclaringType(type, member, members, typeNS, encoded, sequenceId);
                    }
                }
                else
                {
                    MemberFetcher fetcher = new MemberFetcher(member);
                    MemberFixup fixup = new MemberFixup(member);
                    bool canRead = field != null || pi.CanRead;
                    bool canWrite = field != null || pi.CanWrite;
                    LogicalMemberValue logicalMemberValue = ReflectMemberValue(
                        memberType, member, member.Name,
                        typeNS, choiceFinder,
                        fetcher, fixup, members, encoded,
                        canRead, canWrite,
                        out specialType, ref shouldBeOrdered);
                    if (logicalMemberValue != null)
                    {
                        fixup.TargetType = logicalMemberValue.Accessors.Default.Type;
                        logicalMemberValue.SequenceId = ++sequenceId;
                    }
                }
            }
            catch (NotSupportedException ex)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlSerializerUnsupportedMember,
                    type.FullName + "." + member.Name, memberType), ex);
            }
        }

        /// <summary>
        /// Adds an Array type to the TypeContainer's cache.
        /// </summary>
        protected LogicalType AddArrayType(Type type, TypeAttributes attrs, string defaultNS, bool encoded)
        {
            Debug.Assert(type.IsArray, "Attempting to add an array type that is not an array");

            LogicalType ret = MakeArrayType(type, attrs, encoded, defaultNS);

            // Reflecting over the include attribute must be done after the decorated type has been reflected            
            ReflectIncludeAttributes(attrs);

            return ret;
        }

        /// <summary>
        /// Creates a LogicalType for given array type.
        /// </summary>
        private LogicalType MakeArrayType(Type arrayType, TypeAttributes attrs, bool encoded, string defaultNS)
        {
            Debug.Assert(arrayType.HasElementType, string.Format(CultureInfo.CurrentCulture, "Calling MakeArrayType on a non-array type: {0}", arrayType));

            // Find the LogicalType and the runtime type for the array element 
            LogicalType eLType = FindType(arrayType.GetElementType(), encoded, defaultNS);

            bool dontCheckIntrinsics = false;
            bool noTypeMapping = false;
            if (arrayType == typeof(byte[]))
            {
                // If we find a byte[] then we should not check the intrinsics table. This is because MakeArrayType
                // is called only for byte[] when the byte[] should be serialized as something other then the built
                // in byte[] type, (example of buit in byte types: Base64, Base64Binary, or HexBinary. Instead, we 
                // want to serialize using a true byte array, meaning each byte in the array has its own element.
                dontCheckIntrinsics = true;
            }
            else
            {
                // For all other array type, whether we check the intrinics table or not depends on the type attributes.
                // Specifically, XmlRoot, XmlType, or SoapType. If these are defined then some one has create a new 
                // array type, hence the type will not be found in the intrinsics table.
                if (!encoded)
                    dontCheckIntrinsics = attrs != null && (attrs.XmlRoot != null || attrs.XmlType != null);
                else
                    dontCheckIntrinsics = attrs != null && attrs.SoapType != null;
            }

            // If we are not checking intrinsics (we have an XmlRoot) then we should not check 
            // the global intrinsic type table, because it may contain an array of the type we're 
            // looking for but without the appropriate overrides.
            TypeOrigin originsToSearch = dontCheckIntrinsics ? TypeOrigin.User : TypeOrigin.All;
            TypeOrigin typeOrigin;
            if (!m_TypeContainer.ContainsType(arrayType, defaultNS, encoded, originsToSearch, out typeOrigin))
            {
                Debug.Assert(eLType != null, "About to initialize an array like but we don't have a LogicalType for the elements");
                LogicalType ret = new LogicalType();
                InitializeArrayLike(ret, arrayType, attrs, eLType, SerializerType.Array, noTypeMapping, false/*noNameMapping*/, encoded, defaultNS);

                m_TypeContainer.AddType(ret, encoded);
                return ret;
            }
            else
            {
                return m_TypeContainer.GetType(arrayType, defaultNS, encoded, originsToSearch, out typeOrigin);
            }
        }

        /// <summary>
        /// Adds an IXmlSerializable type to the TypeContainer's cache.
        /// </summary>
        protected LogicalType AddIXmlSerializableType(Type type, TypeAttributes attrs, string typeName, string typeNS, bool typeIsNullable, string defaultNS, bool encoded)
        {
            // IXmlSerializables do not need to be reflected for member type.
            Debug.Assert(!encoded, "Using IXmlSerializable in an encoded message");
            Debug.Assert(typeof(IXmlSerializable).IsAssignableFrom(type), "Adding an IXmlSerializable type that does not implement IXmlSerializable");

            LogicalType ret = MakeSerializableType(type, attrs, typeName, typeNS, typeIsNullable, defaultNS);
            m_TypeContainer.AddType(ret, encoded);

            // Reflecting over the include attribute must be done after the decorated type has been reflected
            ReflectIncludeAttributes(attrs);

            return ret;
        }

        /// <summary>
        /// Creates a LogicalType for given IXmlSerializable type.
        /// </summary>
        private LogicalType MakeSerializableType(Type type, TypeAttributes attrs,
            string name, string typeNS,
            bool isNullable, string defaultNS)
        {
            if (attrs.XmlType != null)
            {
                // We allow XmlRoot attribute on IXmlSerializable, but not others                            
                throw new InvalidOperationException(SR.Format(SR.XmlSerializableAttributes, type.FullName, typeof(XmlSchemaProviderAttribute).Name));
            }
            const bool namespaceIsPortable = true;

            if (attrs.XmlRoot != null)
            {
                bool rootIsNullable = isNullable;
                XmlRootAttribute xra = attrs.XmlRoot;

                if (xra.IsNullableSpecified)
                    rootIsNullable = xra.IsNullable;

                // The XmlRoot attribute takes precedence over the XmlSchemaProviderAttribute
                return new LogicalType(name, typeNS, namespaceIsPortable, type, rootIsNullable, SerializerType.Serializable, TypeID.Compound, TypeMappingFlags.Default);
            }

            // get the schema method info
            object[] schemaProviderAttrs = type.GetTypeInfo().GetCustomAttributes(typeof(XmlSchemaProviderAttribute), false).ToArray();

            if (schemaProviderAttrs.Length > 0)
            {
                // New IXmlSerializable
                XmlSchemaProviderAttribute provider = (XmlSchemaProviderAttribute)schemaProviderAttrs[0];

                if ((provider.IsAny == true) && (provider.MethodName == null))
                {
                    return new LogicalType(name, typeNS, namespaceIsPortable, type, isNullable, SerializerType.Serializable, TypeID.Compound, TypeMappingFlags.Default);
                }
                MethodInfo getMethod = null;

                if (type.GetTypeInfo().IsGenericType)
                {
                    getMethod = type.GetMethod(provider.MethodName, /* BindingFlags.DeclaredOnly | */ BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(XmlSchemaSet), typeof(Type[]) });
                }
                else
                {
                    getMethod = type.GetMethod(provider.MethodName, /* BindingFlags.DeclaredOnly | */ BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(XmlSchemaSet) });
                }

                if (getMethod == null)
                    throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaMethodMissing, provider.MethodName, typeof(XmlSchemaSet).Name, type.FullName));

                if (!(typeof(XmlQualifiedName).IsAssignableFrom(getMethod.ReturnType)) && !(typeof(XmlSchemaType).IsAssignableFrom(getMethod.ReturnType)))
                    throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaMethodReturnType, type.Name, provider.MethodName, typeof(XmlSchemaProviderAttribute).Name, typeof(XmlQualifiedName).FullName, typeof(XmlSchemaType).FullName));


                // get the type info
#if !FEATURE_LEGACYNETCF
                XmlSchemaSet schemas = new XmlSchemaSet();
#else
                XmlSchemaSet schemas = null;
#endif

                object typeInfo = getMethod.Invoke(null, type.GetTypeInfo().IsGenericType ? new object[] { schemas, type.GetGenericArguments() } : new object[] { schemas });

#if !FEATURE_LEGACYNETCF
                // make sure that user-specified schemas are valid
                schemas.ValidationEventHandler += new ValidationEventHandler(ValidationCallbackWithErrorCode);
                schemas.Compile();

                XmlSchemaType xsdType = null;
#endif

                XmlQualifiedName qname = XmlQualifiedName.Empty;
                if (typeInfo != null)
                {
                    if (typeof(XmlSchemaType).IsAssignableFrom(getMethod.ReturnType))
                    {
#if FEATURE_LEGACYNETCF
                        throw new NotSupportedException(SR.Format(SR.XmlUnsupportedType, typeof(XmlSchemaType).ToString()));
#else
                        xsdType = (XmlSchemaType)typeInfo;
                        // check if type is named
                        qname = xsdType.QualifiedName;
#endif
                    }
                    else if (typeof(XmlQualifiedName).IsAssignableFrom(getMethod.ReturnType))
                    {
                        qname = (XmlQualifiedName)typeInfo;
                        if (qname.IsEmpty)
                            throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaEmptyTypeName, type.FullName, provider.MethodName));
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaMethodReturnType, type.Name, provider.MethodName, typeof(XmlSchemaProviderAttribute).Name, typeof(XmlQualifiedName).FullName));
                    }
                }

                if (!qname.IsEmpty)
                {
#if !FEATURE_LEGACYNETCF
                    // try to find the type in the schemas collection
                    if (qname.Namespace != XmlSchema.Namespace) {
                        ArrayList srcSchemas = (ArrayList)schemas.Schemas(qname.Namespace);

                        if (srcSchemas.Count == 0) {
                            throw new InvalidOperationException(SR.Format(SR.XmlMissingSchema, qname.Namespace));
                        }
                        if (srcSchemas.Count > 1) {
                            throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaInclude, qname.Namespace, type.FullName, provider.MethodName));
                        }

                        XmlSchema s = (XmlSchema)srcSchemas[0];
                        if (s == null) {
                            throw new InvalidOperationException(SR.Format(SR.XmlMissingSchema, qname.Namespace));
                        }

                        xsdType = (XmlSchemaType)s.SchemaTypes[qname];
                        if (xsdType == null) {
                            throw new InvalidOperationException(SR.Format(SR.XmlGetSchemaTypeMissing, type.FullName, provider.MethodName, qname.Name, qname.Namespace));
                        }
                        xsdType = xsdType.Redefined != null ? xsdType.Redefined : xsdType;
                    }

                    return new LogicalType(qname.Name, qname.Namespace == XmlSchema.Namespace ? null : qname.Namespace, namespaceIsPortable, type, isNullable, SerializerType.Serializable, TypeID.Compound, TypeMappingFlags.Default);
#else
                    return new LogicalType(qname.Name, qname.Namespace, namespaceIsPortable, type, isNullable, SerializerType.Serializable, TypeID.Compound, TypeMappingFlags.Default);
#endif
                }
                else
                {
                    return new LogicalType(name, typeNS, namespaceIsPortable, type, isNullable, SerializerType.Serializable, TypeID.Compound, TypeMappingFlags.Default);
                }
            }
            else
            {
                // Old IXmlSerializable
                return new LogicalType(name, typeNS, namespaceIsPortable, type, isNullable, SerializerType.Serializable, TypeID.Compound, TypeMappingFlags.Default);
            }
        }

        /// <summary>
        /// Adds an ICollection type to the TypeContainer's cache.
        /// </summary>
        protected LogicalType AddICollectionType(Type type, TypeAttributes attrs, bool encoded, string defaultNS)
        {
            Debug.Assert(SerializationHelper.isCollection(type), "Attempting to add a collection type that is not a collection");

            LogicalType ret = MakeCollectionType(type, attrs, encoded, defaultNS);

            // Reflecting over the include attribute must be done after the decorated type has been reflected
            ReflectIncludeAttributes(attrs);

            return ret;
        }

        /// <summary>
        /// Creates a LogicalType for an ICollection.
        /// </summary>
        private LogicalType MakeCollectionType(Type type, TypeAttributes attrs, bool encoded, string defaultNS)
        {
            PropertyInfo indexer;
            Type eltType = GetCollectionElementType(type, out indexer);
            LogicalType eltLType = FindType(eltType, encoded, defaultNS);
            if (null == eltLType) return null;

            //now look for an add method that matches the indexer            
            MethodInfo addMethod = type.GetMethod("Add", new Type[] { eltLType.Type });
            if (addMethod == null)
                throw new InvalidOperationException(SR.Format(SR.XmlS_CollNoAdd_1, type));

            TypeOrigin typeOrigin;
            if (!m_TypeContainer.ContainsType(type, defaultNS, encoded, out typeOrigin))
            {
                LogicalType collectionLType = new LogicalCollection(indexer, addMethod);
                bool noNameMapping = (eltType == typeof(object));
                InitializeArrayLike(collectionLType, type, attrs, eltLType, SerializerType.Collection, false/*noTypeMapping*/, noNameMapping, encoded, defaultNS);

                if (attrs.XmlType != null && attrs.XmlType.AnonymousType)
                {
                    collectionLType.MappingFlags &= (~TypeMappingFlags.AllowNameMapping);
                    collectionLType.IsAnonymousType = attrs.XmlType.AnonymousType;
                }

                m_TypeContainer.AddType(collectionLType, encoded);
                return collectionLType;
            }
            else
            {
                return m_TypeContainer.GetType(type, defaultNS, encoded, out typeOrigin);
            }
        }


        /// <summary>
        /// Adds an XmlNode type to the TypeContainer's cache.
        /// </summary>
        protected LogicalType AddXmlNodeType(Type type, TypeAttributes attrs, bool encoded, string defaultNS)
        {
            Debug.Assert(!encoded, "Using XmlNode in an encoded message");
            Debug.Assert(typeof(XmlNode).IsAssignableFrom(type), "Attempting to add an XmlNode type that is not an XmlNode");

            LogicalType ret = MakeXmlNodeType(type, encoded, defaultNS);

            // Reflecting over the include attribute must be done after the decorated type has been reflected
            ReflectIncludeAttributes(attrs);

            return ret;
        }

        /// <summary>
        /// Creates a LogicalType for the XmlNode type.
        /// </summary>
        private LogicalType MakeXmlNodeType(Type type, bool encoded, string defaultNS)
        {
            if (encoded)
                throw new InvalidOperationException(SR.Format(SR.XmlUnsupportedSoapTypeKind, type));

            TypeOrigin typeOrigin;
            if (!m_TypeContainer.ContainsType(type, null /* ns */, encoded, out typeOrigin))
            {
                // We drop defaultNS when creating this, as XmlNodes carry all namespace info that
                // they need with them, which also makes them namespaceIsPortable = true.
                LogicalType ret = new LogicalType(type.Name, null, true /*namespaceIsPortable*/, type, false, SerializerType.XmlNode, TypeID.Compound, TypeMappingFlags.Default);
                m_TypeContainer.AddType(ret, encoded);
                return ret;
            }
            else
            {
                return m_TypeContainer.GetType(type, null /* ns */, encoded, out typeOrigin);
            }
        }

        /// <summary>
        /// Adds an IEnumerable type to the TypeContainer's cache.
        /// </summary>
        protected LogicalType AddIEnumerableType(Type type, TypeAttributes attrs, bool encoded, string defaultNS)
        {
            Debug.Assert(SerializationHelper.isEnumerable(type), "Attempting to add an IEnumerable type that is not an IEnumerable");
            Debug.Assert(!typeof(ICollection).IsAssignableFrom(type), "Attempting to add an IEnumerable that is also an ICollection. It should be added as an ICollection");

            LogicalType ret = MakeEnumerableType(type, attrs, encoded, defaultNS);

            // Reflecting over the include attribute must be done after the decorated type has been reflected            
            ReflectIncludeAttributes(attrs);

            return ret;
        }

        /// <summary>
        /// Creates a LogicalType for an IEnumerable type.
        /// </summary>
        private LogicalType MakeEnumerableType(Type type, TypeAttributes attrs, bool encoded, string defaultNS)
        {
            TypeOrigin typeOrigin;
            if (!m_TypeContainer.ContainsType(type, defaultNS, encoded, out typeOrigin))
            {
                Type enumedType = GetEnumeratorElementType(type);
                if (enumedType == null) return null;
                LogicalType enumedLType = FindType(enumedType, encoded, defaultNS);
                MethodInfo addMethod = type.GetMethod("Add", new Type[] { enumedType });

                if (addMethod == null)
                    throw new InvalidOperationException(SR.Format(SR.XmlS_IEnumNoAdd_3, enumedType, enumedType.FullName, "IEnumerable"));

                LogicalType enumerableLType = new LogicalEnumerable(addMethod);
                bool noNameMapping = (enumedType == typeof(object));
                InitializeArrayLike(enumerableLType, type, attrs, enumedLType, SerializerType.Enumerable, false/*noTypeMapping*/,
                    noNameMapping, encoded, defaultNS);

                if (attrs.XmlType != null && attrs.XmlType.AnonymousType)
                {
                    enumerableLType.MappingFlags &= (~TypeMappingFlags.AllowNameMapping);
                    enumerableLType.IsAnonymousType = attrs.XmlType.AnonymousType;
                }

                m_TypeContainer.AddType(enumerableLType, encoded);
                return enumerableLType;
            }
            else
            {
                return m_TypeContainer.GetType(type, defaultNS, encoded, out typeOrigin);
            }
        }

        /// <summary>
        /// Adds an Enum type to the TypeContainer's cache.
        /// </summary>
        protected LogicalType AddEnumType(Type type, TypeAttributes attrs, string typeName, string typeNS, bool typeIsNullable, string defaultNS, bool encoded)
        {
            Debug.Assert(type.GetTypeInfo().IsEnum, "Attempting to add an Enum type that is not an Enum");

            LogicalType ret;
            if (encoded)
            {
                ret = MakeEnumType(type, attrs, typeName, typeNS, typeIsNullable, null, null, false, encoded);
            }
            else
            {
                string rootName = typeName;
                string rootNS = defaultNS;
                bool rootIsNullable = typeIsNullable;

                if (attrs.XmlRoot != null)
                {
                    XmlRootAttribute xra = attrs.XmlRoot;
                    if (xra.ElementName != null && xra.ElementName.Length > 0)
                        rootName = xra.ElementName;
                    if (xra.Namespace != null)
                        rootNS = xra.Namespace;
                    if (xra.IsNullableSpecified)
                        rootIsNullable = xra.IsNullable;
                }

                ret = MakeEnumType(type, attrs, typeName, typeNS, typeIsNullable, rootName, rootNS, rootIsNullable, encoded);
            }

            // Reflecting over the include attribute must be done after the decorated type has been reflected
            ReflectIncludeAttributes(attrs);
            return ret;
        }

        /// <summary>
        /// Creates a special type for an enumerated type and adds it to the cache.
        /// </summary>
        private LogicalEnum MakeEnumType(Type type, TypeAttributes attrs, string name, string ns, bool isNullable, string rootName, string rootNS, bool rootIsNullable, bool encoded)
        {
            TypeOrigin typeOrigin;

            if (!m_TypeContainer.ContainsType(type, ns, encoded, out typeOrigin))
            {
                FieldInfo[] values = type.GetFields(BindingFlags.Static | BindingFlags.Public);


                LogicalEnum enumeration = encoded ?
                    new LogicalEnum(name, ns, type, isNullable, attrs.IsFlag) :
                    new LogicalEnum(name, ns, type, isNullable, rootName, rootNS, rootIsNullable, attrs.IsFlag);

                if (attrs.XmlType != null && attrs.XmlType.AnonymousType)
                {
                    enumeration.MappingFlags &= (~TypeMappingFlags.AllowNameMapping);
                    enumeration.IsAnonymousType = attrs.XmlType.AnonymousType;
                }

                for (int i = 0; i < values.Length; ++i)
                {
                    FieldInfo enumValue = values[i];
                    string valueName = null;

                    if (encoded)
                    {
                        SoapAttributes soapAtts = FindOverrideSoapAttributes(enumValue.DeclaringType, enumValue.Name);
                        EncodedAttributes enumAttrs = new EncodedAttributes(enumValue, soapAtts);
                        valueName = (enumAttrs.SoapEnum != null) ? enumAttrs.SoapEnum.Name : enumValue.Name;
                    }
                    else
                    {
                        XmlAttributes xmlAtts = FindOverrideXmlAttributes(enumValue.DeclaringType, enumValue.Name);
                        LiteralAttributes enumAttrs = new LiteralAttributes(enumValue, xmlAtts);
                        valueName = (enumAttrs.XmlEnum != null) ? enumAttrs.XmlEnum.Name : enumValue.Name;
                    }

                    if (valueName == null || valueName.Length == 0)
                        valueName = enumValue.Name;
                    /* Default keyword in case of enums represent 0 value. 
                     * This value can be explicitly set to one of the enum members which will be 
                     * assigned the value 0. All the other members will hence get their respective values.
                     * Since the enum member which has been set as default will be browsed twice 
                     * we need to restrict it to one time.
                    */
                    if (!valueName.Equals("Default"))
                        enumeration.AddEnumMapping(valueName, ns, enumValue.GetValue(null));
                }

                m_TypeContainer.AddType(enumeration, encoded);
                return enumeration;
            }
            else
            {
                LogicalEnum logicalEnum = m_TypeContainer.GetType(type, ns, encoded, out typeOrigin) as LogicalEnum;
                Debug.Assert(logicalEnum != null, string.Format(CultureInfo.CurrentCulture, "TypeContainer returned a non-logical enum for {0}", type));

                return logicalEnum;
            }
        }

        /// <summary>
        /// This adds a type to the TypeContainer.
        /// </summary>
        protected LogicalType AddType(Type type, bool encoded, string defaultNS, bool genericNullableArg, bool modelAfterIntrinsics)
        {
            string typeName, typeNS;
            bool typeIsNullable;
            bool isNullableType = IsNullableType(type);

            if (UnsupportedTypes.Contains(type))
            {
                throw new NotSupportedException(SR.Format(SR.XmlUnsupportedType, type.FullName));
            }
            else if (type.GetTypeInfo().ContainsGenericParameters)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlUnsupportedOpenGenericType, type.FullName));
            }
            else if (!type.GetTypeInfo().IsPublic && !type.GetTypeInfo().IsNestedPublic)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlTypeInaccessible, type.FullName));
            }
            else if (type.GetTypeInfo().IsAbstract && type.GetTypeInfo().IsSealed)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlTypeStatic, type.FullName));
            }

            // Reflect over generic types if they are present. Note, for generic types that implement
            // IXmlSerializable, their generic argument types will not be reflected over. The knowledge
            // of these types are not neccessary, since the generic will be handling its own serialization
            // via the IXmlSerializable interface.
            //
            // Example:
            // public class GenericType<T> : IXmlSerializable {
            //     ...
            // }
            if (type.GetTypeInfo().IsGenericType && !typeof(IXmlSerializable).IsAssignableFrom(type))
            {
                Type[] arguments = type.GetGenericArguments();
                for (int argNdx = 0; argNdx < arguments.Length; ++argNdx)
                {
                    try
                    {
                        FindTypeNoThrow(arguments[argNdx], encoded, defaultNS, TypeOrigin.All, isNullableType);
                    }
                    catch (NotSupportedException)
                    {
                        // Eat the exception.  If this generic type argument is actually referenced during serialization, we'll
                        // fail again and throw from there.
                    }
                }
            }

            try
            {
                // Get any Xml/Soap attributes defined on the type or overridden using the Xml/SoapAttributeOverrides collection            
                TypeAttributes attrs = encoded ?
                    new TypeAttributes(type.GetTypeInfo(), FindOverrideSoapAttributes(type)) :
                    new TypeAttributes(type.GetTypeInfo(), FindOverrideXmlAttributes(type));

                // Get the type name, type namespace and isNullable information out of the TypeAttributes.
                if (encoded)
                    GetEncodedTypeInformation(type, attrs, isNullableType, genericNullableArg, out typeName, out typeNS, out typeIsNullable);
                else
                    GetLiteralTypeInformation(type, attrs, isNullableType, genericNullableArg, out typeName, out typeNS, out typeIsNullable);

                // If an XmlTypeAttribute.Namespace was specified, then this type can be a member of
                // a type in any namespace, since the namespace within this type will be coerced to a
                // specific one anyway.  Set the typeNS to null to signify that the namespace is
                // not significant.
                // If an XmlTypeAttribute.Namespace was NOT specified, then this type can change 
                // namespace to fit whatever type it is used within, so it's specific and we should
                // set the typeNS to the defaultNS of this context.
                // By setting typeNS to null, we allow TypeContainer.ContainsType and TypeContainer.GetType
                // to lookup this reflected type in the future, regardless of what namespace the type
                // would be pulled into.
                bool namespaceIsPortable = typeNS != null || encoded;

                if (encoded)
                {
                    // If the type doesn't explicitly request a namespace for itself
                    // (it's root tag), then use the universally set defaultNamespace.
                    // That is, we specifically avoid using the defaultNS that is passed
                    // as a parameter, because by encoded rules, types don't inherit
                    // namespaces from containing types.
                    typeNS = typeNS ?? m_DefaultNamespace;
                }
                else
                {
                    // If the type doesn't explicitly request a namespace for its members,
                    // use the defaultNamespace of context.
                    typeNS = typeNS ?? defaultNS;
                }

                if (modelAfterIntrinsics && TypeContainer.ContainsIntrinsicType(type))
                    return AddIntrinsicType(type, typeNS, attrs, encoded);
                if (type.GetTypeInfo().IsValueType && isNullableType)
                    return AddNullableType(type, typeName, typeNS, namespaceIsPortable, attrs, encoded, defaultNS);
                if (SerializationHelper.IsSerializationPrimitive(type))
                    return AddPrimitiveType(type, typeName, typeNS, attrs, encoded, defaultNS);
                if (type.IsArray)
                    return AddArrayType(type, attrs, defaultNS, encoded);
                if (!encoded && typeof(IXmlSerializable).IsAssignableFrom(type))
                    return AddIXmlSerializableType(type, attrs, typeName, typeNS, typeIsNullable, defaultNS, encoded);
                if (SerializationHelper.isCollection(type))
                    return AddICollectionType(type, attrs, encoded, defaultNS);
                if (!encoded && typeof(XmlNode).IsAssignableFrom(type))
                    return AddXmlNodeType(type, attrs, encoded, defaultNS);
                if (SerializationHelper.isEnumerable(type) && !typeof(ICollection).IsAssignableFrom(type))
                    return AddIEnumerableType(type, attrs, encoded, defaultNS);
                if (type.GetTypeInfo().IsEnum)
                    return AddEnumType(type, attrs, typeName, typeNS, typeIsNullable, defaultNS, encoded);

                return AddComplexType(type, attrs, typeName, typeNS, namespaceIsPortable, typeIsNullable, encoded, defaultNS, genericNullableArg);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlTypeReflectionError, type.FullName), e);
            }
        }

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
        // Static Methods
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//

        // These two funtions are helpers for addType.  They process the class
        // attributes and return the information.  These functions are used by
        // makeEnumType, so that's why they have this funny setup.               


        /// <summary>
        /// Processes the encoded attributes declared on a type. The information is 
        /// returned through out parameters
        /// </summary>
        private static void GetEncodedTypeInformation(Type type, TypeAttributes attrs, bool isNullableType, bool genericNullableArg, out string typeName, out string typeNS, out bool isNullable)
        {
            typeName = TypeName(type);
            typeNS = null;
            isNullable = isNullableType || !type.GetTypeInfo().IsValueType || genericNullableArg;

            if (attrs.SoapType != null)
            {
                SoapTypeAttribute sta = attrs.SoapType;
                if (sta.TypeName != null && sta.TypeName.Length > 0)
                    typeName = sta.TypeName;
                if (sta.Namespace != null)
                    typeNS = sta.Namespace;
            }
        }

        /// <summary>
        /// Processes the literal attributes declared on a type. The information is 
        /// returned through out parameters
        /// </summary>
        private static void GetLiteralTypeInformation(Type type, TypeAttributes attrs, bool isNullableType, bool genericNullableArg, out string typeName, out string typeNS, out bool typeIsNullable)
        {
            typeName = TypeName(type);
            typeNS = null;
            typeIsNullable = isNullableType || !type.GetTypeInfo().IsValueType || genericNullableArg;

            // Look on the type for XmlType and XmlRoot attributes.
            if (attrs.XmlType != null)
            {
                XmlTypeAttribute xta = attrs.XmlType;

                if (xta.TypeName != null && xta.TypeName.Length > 0)
                    typeName = xta.TypeName;

                if (xta.Namespace != null)
                    typeNS = xta.Namespace;
            }
            else if (attrs.XmlRoot != null)
            {
                XmlRootAttribute xra = attrs.XmlRoot;
                if (xra.ElementName != null && xra.ElementName.Length > 0)
                    typeName = xra.ElementName;
                if (xra.Namespace != null)
                    typeNS = xra.Namespace;
                if (xra.IsNullableSpecified && !genericNullableArg)
                {
                    if (type.GetTypeInfo().IsEnum && xra.IsNullable)
                        throw new InvalidOperationException(SR.Format(SR.XmlInvalidIsNullable, type));
                    typeIsNullable = xra.IsNullable;
                }
            }
        }

        /// <summary>
        /// Creates the type name for the specified type.
        /// </summary>
        private static string TypeName(Type t)
        {
            if (t.IsArray)
            {
                return SerializationStrings.ArrayOf + TypeName(t.GetElementType());
            }
            else if (t.GetTypeInfo().IsGenericType)
            {
                StringBuilder sb = new StringBuilder();
                int nameEnd = t.Name.IndexOfAny(new char[] { '`', '!' });
                if (true /* AppDomain.CompatVersion > AppDomain.Whidbey */)
                    sb.Append(nameEnd > 0 ? t.Name.Substring(0, nameEnd) : t.Name);
                else // old behavior
                    sb.Append(nameEnd > 0 ? t.Name.Remove(nameEnd, 1) : t.Name);
                sb.Append("Of");
                Type[] arguments = t.GetGenericArguments();
                for (int argNdx = 0; argNdx < arguments.Length; ++argNdx)
                {
                    sb.Append(TypeName(arguments[argNdx]));
                }
                return sb.ToString();
            }
            return t.Name;
        }

        /// <summary>
        /// Creates a LogicalMemberValue instance with default accessor set 
        /// appropriately.
        /// </summary>
        private static LogicalMemberValue MakeMemberWithDefault(LogicalType type, Fetcher fetcher, Fixup fixup)
        {
            AccessorCollection accessors = new AccessorCollection();
            Accessor accessor = new Accessor(null, null, type, false, false);
            accessors.add(accessor);
            accessors.Default = accessor;

            LogicalMemberValue ret = new LogicalMemberValue(accessors, false/*required*/, true/*canRead*/, true/*canWrite*/ );

            if (fetcher != null)
                ret.Fetcher = fetcher;

            if (fixup != null)
            {
                ret.Fixup = fixup;
                ((AssigningFixup)ret.Fixup).TargetType = type;
            }

            return ret;
        }

        /// <summary>
        /// Adds a member using the member info on the declared type.
        /// </summary>
        private void AddMemberUsingDeclaringType(Type type, MemberInfo memberInfo, MemberValueCollection members, string typeNS, bool encoded, int sequenceId)
        {
            if (encoded)
            {
                SoapAttributes soapAtts = FindOverrideSoapAttributes(memberInfo.DeclaringType, memberInfo.Name);
                EncodedAttributes attrs = new EncodedAttributes(memberInfo, soapAtts);
                if (attrs.Ignore) return;
            }
            else
            {
                XmlAttributes xmlAtts = FindOverrideXmlAttributes(memberInfo.DeclaringType, memberInfo.Name);
                LiteralAttributes attrs = new LiteralAttributes(memberInfo, xmlAtts);
                if (attrs.Ignore) return;
            }

            LogicalMemberValue member = null;
            LogicalType declaringType = FindType(memberInfo.DeclaringType, encoded, typeNS);
            int classDefinitionLevel = GetClassDefinitionLevel(type, memberInfo);
            MemberValueCollection declaringTypeMembers = declaringType.Members;

            member = FindMemberInDeclaringTypesMembers(declaringTypeMembers, memberInfo, typeNS, encoded);
            if (member != null)
            {
                member.ClassDefinitionLevel = classDefinitionLevel;
                member.SequenceId = sequenceId;
                members.addMemberValue(member, memberInfo.Name);
                return;
            }

            member = FindMemberInDeclaringTypesXmlText(declaringTypeMembers, memberInfo, encoded);
            if (member != null)
            {
                member.ClassDefinitionLevel = classDefinitionLevel;
                member.SequenceId = sequenceId;
                members.XmlText = member;
                return;
            }

            member = FindMemberInDeclaringTypesXmlAnyAttribute(declaringTypeMembers, memberInfo, encoded);
            if (member != null)
            {
                member.ClassDefinitionLevel = classDefinitionLevel;
                member.SequenceId = sequenceId;
                members.XmlAny.XmlAnyAttribute = member;
                return;
            }

            bool isDefaultAnyElement;
            IEnumerable anyElementQNames = null;
            XmlChoiceSupport choiceSupport = null;
            AccessorCollection anyElementAccessors = null;
            member = FindMemberInDeclaringTypesXmlAnyElement(declaringTypeMembers, memberInfo, encoded, out isDefaultAnyElement, out anyElementQNames, out anyElementAccessors, out choiceSupport);
            if (member != null)
            {
                member.ClassDefinitionLevel = classDefinitionLevel;
                member.SequenceId = sequenceId;

                if (isDefaultAnyElement)
                    members.XmlAny.Default = member;
                else
                {
                    foreach (XmlQualifiedName anyElementQName in anyElementQNames)
                    {
                        members.XmlAny.addAnyMember(anyElementQName.Name, anyElementQName.Namespace, member, memberInfo.Name);
                    }
                }

                if (choiceSupport != null)
                    members.XmlAny.addChoiceSupport(choiceSupport, member);
                if (anyElementAccessors != null)
                {
                    foreach (Accessor anyElementAccessor in anyElementAccessors)
                    {
                        members.XmlAny.addElementAccessor(anyElementAccessor.copy(), member);
                    }
                }
                return;
            }
            // SOAP method calls can get by all these "if" statements by design.
            // An example is in test 3413: System.XmlSerializer.WSXmlNSDAttr08.exe.
        }

        /// <summary>
        /// Locates a LogicalMemberValue of a type by looking in the member's of one of
        /// its base classes.
        /// </summary>
        private LogicalMemberValue FindMemberInDeclaringTypesMembers(MemberValueCollection baseTypeMembers, MemberInfo memberInfo, string typeNS, bool encoded)
        {
            Debug.Assert(baseTypeMembers != null);
            Debug.Assert(memberInfo != null);

            // Check the member value collection enumerator            
            for (int curMemberNdx = 0; curMemberNdx < baseTypeMembers.count(); ++curMemberNdx)
            {
                if (((MemberFixup)baseTypeMembers.getMember(curMemberNdx).Fixup).m_member.Name == memberInfo.Name)
                {
                    LogicalMemberValue member = baseTypeMembers.getMember(curMemberNdx).copy();
                    if (!encoded)
                    {
                        if (member.Accessors != null && member.Accessors.Default != null && !member.Accessors.Default.IsAttribute &&
                            member.Accessors.Default.Namespace == null)
                            member.Accessors.Default.Namespace = typeNS;
                    }
                    return member;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a member is an XmlText of in one of the parent type's of the 
        /// members declared type.
        /// </summary>
        private LogicalMemberValue FindMemberInDeclaringTypesXmlText(MemberValueCollection baseTypeMembers, MemberInfo memberInfo, bool encoded)
        {
            Debug.Assert(baseTypeMembers != null);
            Debug.Assert(memberInfo != null);

            // Check the XmlText member            
            if (baseTypeMembers.XmlText != null)
            {
                if (((MemberFixup)baseTypeMembers.XmlText.Fixup).m_member.Name == memberInfo.Name)
                {
                    return baseTypeMembers.XmlText.copy();
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a member is an XmlAnyAttribute of in one of the parent type's of 
        /// the members declared type.
        /// </summary>
        private LogicalMemberValue FindMemberInDeclaringTypesXmlAnyAttribute(MemberValueCollection baseTypeMembers, MemberInfo memberInfo, bool encoded)
        {
            Debug.Assert(baseTypeMembers != null);
            Debug.Assert(memberInfo != null);

            // Check the XmlAnyAttribute of the XmlAnyCollection            
            if (baseTypeMembers.HasXmlAny && baseTypeMembers.XmlAny.XmlAnyAttribute != null)
            {
                LogicalMemberValue anyAttribute = baseTypeMembers.XmlAny.XmlAnyAttribute;
                if (((MemberFixup)anyAttribute.Fixup).m_member.Name == memberInfo.Name)
                {
                    return anyAttribute.copy();
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a member is an XmlElementAttribute of in one of the parent type's 
        /// of the members declared type.
        /// </summary>
        private LogicalMemberValue FindMemberInDeclaringTypesXmlAnyElement(MemberValueCollection baseTypeMembers, MemberInfo memberInfo, bool encoded,
            out bool isDefaultAnyElement, out IEnumerable anyElementQNames, out AccessorCollection elementAccessors, out XmlChoiceSupport choice)
        {
            Debug.Assert(baseTypeMembers != null);
            Debug.Assert(memberInfo != null);

            // Check the XmlAnyElement of the XmlAnyCollection            
            if (baseTypeMembers.HasXmlAny)
            {
                LogicalMemberValue defaultAny = baseTypeMembers.XmlAny.Default;
                if (defaultAny != null && ((MemberFixup)defaultAny.Fixup).m_member.Name == memberInfo.Name)
                {
                    anyElementQNames = null;
                    choice = baseTypeMembers.XmlAny.lookupChoiceSupport(defaultAny);
                    elementAccessors = baseTypeMembers.XmlAny.lookupElementAccessors(defaultAny);
                    isDefaultAnyElement = true;
                    return defaultAny.copy();
                }

                foreach (LogicalMemberValue member in baseTypeMembers.XmlAny.getSpecializedElementAnys())
                {
                    if (((MemberFixup)member.Fixup).m_member.Name == memberInfo.Name)
                    {
                        anyElementQNames = baseTypeMembers.XmlAny.lookupAnyElementQName(member);
                        choice = baseTypeMembers.XmlAny.lookupChoiceSupport(member);
                        elementAccessors = baseTypeMembers.XmlAny.lookupElementAccessors(member);
                        isDefaultAnyElement = false;
                        return member.copy();
                    }
                }
            }

            anyElementQNames = null;
            choice = null;
            elementAccessors = null;
            isDefaultAnyElement = false;
            return null;
        }

        /// <summary>
        /// Returns the class definition level of the member info. The class definition
        /// level is 0 if the member's declared type is equal to the member's reflected
        /// type. If this is not true we then start to walk up the reflected type's 
        /// inheritance tree. For each step up the inheritance tree the class definition
        /// increases by 1.
        /// </summary>
        private static int GetClassDefinitionLevel(Type type, MemberInfo memberInfo)
        {
            int ret = 0;
            Type declaringType = memberInfo.DeclaringType;

            while (type != declaringType)
            {
                ret++;
                type = type.GetTypeInfo().BaseType;
            }

            return ret;
        }

        /// <summary>
        /// Detects whether a give type is an Nullable<T>.
        /// </T>
        private static bool IsNullableType(Type type)
        {
            if (type.GetTypeInfo().IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<long>).GetGenericTypeDefinition())
                    return true;
            }
            return false;
        }

#if !FEATURE_LEGACYNETCF
        internal static void ValidationCallbackWithErrorCode(object sender, ValidationEventArgs args) {
            if (args.Severity == XmlSeverityType.Error)
                throw new InvalidOperationException(SR.Format(SR.XmlSerializableSchemaError, typeof(IXmlSerializable).Name, args.Message));
        }
#endif

        /// <summary>
        /// Checks whether the property has the correct access.
        /// </summary>
        private static bool CheckProperty(PropertyInfo pi, Type type)
        {
            if (!pi.CanRead) return false;
            if (!pi.CanWrite && !SerializationHelper.isCollection(pi.PropertyType) && !SerializationHelper.isEnumerable(pi.PropertyType))
                return false;


            MethodInfo getMethod = pi.GetMethod;
            if (getMethod.IsStatic) return false;
            ParameterInfo[] parameters = getMethod.GetParameters();
            if (parameters.Length > 0) return false;
            return true;
        }

        /// <summary>
        /// Determines the IEnumerable's element type.
        /// </summary>
        private static Type GetEnumeratorElementType(Type type)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                MethodInfo e = type.GetMethod("GetEnumerator", new Type[0]);

                if (e == null || !typeof(IEnumerator).IsAssignableFrom(e.ReturnType))
                {
                    // try generic implementation
                    e = null;
                    foreach (MemberInfo member in findMembersStartingWith(type, "System.Collections.Generic.IEnumerable<", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        e = member as MethodInfo;
                        if (e != null && typeof(IEnumerator).IsAssignableFrom(e.ReturnType))
                        {
                            // use the first one we find
                            break;
                        }
                        else
                        {
                            e = null;
                        }
                    }
                    if (e == null)
                    {
                        // and finally private interface implementation
                        e = type.GetMethod("System.Collections.IEnumerable.GetEnumerator", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, new Type[0]);
                    }
                }
                if (e == null || !typeof(IEnumerator).IsAssignableFrom(e.ReturnType))
                {
                    return null;
                }

                LiteralAttributes methodAttrs = new LiteralAttributes(e);
                if (methodAttrs.Ignore) return null;
                PropertyInfo p = e.ReturnType.GetProperty("Current");
                Type currentType = (p == null ? typeof(object) : p.PropertyType);

                MethodInfo addMethod = type.GetMethod("Add", new Type[] { currentType });

                if (addMethod == null && currentType != typeof(object))
                {
                    currentType = typeof(object);
                    addMethod = type.GetMethod("Add", new Type[] { currentType });
                }
                if (addMethod == null)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlNoAddMethod, type.FullName, currentType, "IEnumerable"));
                }
                return currentType;
            }
            else
            {
                return null;
            }
        }

        // This helper method is necessary because Type.GetMember(string, BindingFlags) in NetCF does not
        // support desktop's asterisk wildcard character as part of the member name.
        private static MemberInfo[] findMembersStartingWith(Type type, string startsWith, BindingFlags flags)
        {
            List<MemberInfo> memberList = new List<MemberInfo>(type.GetMembers(flags));
            for (int i = memberList.Count - 1; i >= 0; i--)
                if (!memberList[i].Name.StartsWith(startsWith, StringComparison.Ordinal))
                    memberList.RemoveAt(i);
            return memberList.ToArray();
        }

        /// <summary>
        /// Determines the ICollection's element type.
        /// </summary>
        private static Type GetCollectionElementType(Type type, out PropertyInfo propertyInfo)
        {
            propertyInfo = GetDefaultIndexer(type);
            return propertyInfo.PropertyType;
        }

        /// <summary>
        /// Determines the default indexer's type.
        /// </summary>  
        private static PropertyInfo GetDefaultIndexer(Type type)
        {
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                throw new NotSupportedException(SR.Format(SR.XmlUnsupportedIDictionary, type.FullName));
            }

            MemberInfo[] defaultMembers = type.GetDefaultMembers();
            PropertyInfo indexer = null;
            if (defaultMembers != null && defaultMembers.Length > 0)
            {
                for (Type t = type; t != null; t = t.GetTypeInfo().BaseType)
                {
                    for (int i = 0; i < defaultMembers.Length; i++)
                    {
                        if (defaultMembers[i] is PropertyInfo)
                        {
                            PropertyInfo defaultProp = (PropertyInfo)defaultMembers[i];
                            if (defaultProp.DeclaringType != t) continue;
                            if (!defaultProp.CanRead) continue;
                            MethodInfo getMethod = defaultProp.GetMethod;
                            ParameterInfo[] parameters = getMethod.GetParameters();
                            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
                            {
                                indexer = defaultProp;
                                break;
                            }
                        }
                    }
                    if (indexer != null) break;
                }
            }
            if (indexer == null)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlNoDefaultAccessors, type.FullName));
            }
            MethodInfo addMethod = type.GetMethod("Add", new Type[] { indexer.PropertyType });
            if (addMethod == null)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlNoAddMethod, type.FullName, indexer.PropertyType, "ICollection"));
            }
            return indexer;
        }

        /// <summary>
        /// Retrieves all of the namespaces that are used to map to a LogicalType. These
        /// namespaces only apply to type added using encoded symantics.
        /// </summary>

        [System.Security.FrameworkVisibilityCompactFrameworkInternalAttribute()]
        internal string[] GetEncodedNamespaces()
        {
            return m_TypeContainer.GetEncodedNamespaces();
        }

        /// <summary>
        /// This initializes a LogicalType (or subclass) as an array.  Includes
        /// construction of a TypeAccessor and NestedAccessors if appropriate.
        /// </summary>
        private void InitializeArrayLike(LogicalType toInit, Type arrayType, TypeAttributes attrs, LogicalType eLType, SerializerType serType, bool noTypeMapping, bool noNameMapping, bool encoded, string defaultNS)
        {
            Debug.Assert(arrayType != null, "Synthesized array with null type");
            Accessor defaultEltAccessor;

            XmlRootAttribute rootAtt = attrs != null ? attrs.XmlRoot : null;
            XmlTypeAttribute typeAtt = attrs != null ? attrs.XmlType : null;

            //The name of this type is the base name followed by the correct
            //number of []'s.
            //Inherit the type mapping flags from the element
            toInit.Name = eLType.Name + "[]";
            toInit.Namespace = defaultNS;
            toInit.Type = arrayType;
            toInit.IsNullable = false;
            toInit.Serializer = serType;

            if (encoded)
            {
                //The Type accessor is always Soap.Encoding:Array.                
                toInit.TypeAccessor = new Accessor(SerializationStrings.Array, Soap.Encoding, toInit, false, false);

                // encoded array elements are called "Item" and in the empty namespace
                defaultEltAccessor = new Accessor(SerializationStrings.ArrayItem, string.Empty, eLType, eLType.IsNullable, false);
                toInit.TypeAccessor.EncodedNamespaceForArrays = eLType.TypeAccessor.EncodedNamespaceForArrays ?? eLType.TypeAccessor.Namespace;
            }
            else
            {
                if (rootAtt != null)
                {
                    string rootName = rootAtt.ElementName != null && rootAtt.ElementName.Length > 0 ? rootAtt.ElementName : MakeLiteralArrayName(eLType);
                    string rootNamespace = rootAtt.Namespace ?? defaultNS;
                    bool isNullable = rootAtt.IsNullableSpecified ? rootAtt.IsNullable : false;
                    toInit.TypeAccessor = new Accessor(rootName, rootNamespace, toInit, isNullable, false /*isAttribute*/ );
                }
                else if (typeAtt != null)
                {
                    string typeName = typeAtt.TypeName != null && typeAtt.TypeName.Length > 0 ? typeAtt.TypeName : MakeLiteralArrayName(eLType);
                    toInit.TypeAccessor = new Accessor(typeName, defaultNS, toInit, false /*IsNullable*/, false /*isAttribute*/ );
                }
                else
                {
                    //  An array still has the type name elementType[], however, its default accessor is ArrayOfelementType                           
                    string arrayName = MakeLiteralArrayName(eLType);
                    toInit.TypeAccessor = new Accessor(arrayName, defaultNS, toInit, false /*isNullable*/, false /*isAttribute*/ );
                }

                bool elementIsEncodedBinary = eLType.CustomSerializer == CustomSerializerType.Base64 || eLType.CustomSerializer == CustomSerializerType.Hex;
                string eleName = eLType.Type.HasElementType && !elementIsEncodedBinary ? MakeLiteralArrayName(FindType(eLType.Type.GetElementType(), false/*encoded*/, defaultNS)) : eLType.Name;
                defaultEltAccessor = new Accessor(eleName, toInit.TypeAccessor.Namespace, eLType, true/*isNullable*/, false);
            }

            // Note that an array's namespace portability cannot be assumed to match that of the array's elements,
            // unless the array's accessors can be guaranteed to be correctly initialized.  As you can see above,
            // the defaultEltAccessor is initialized to the array's (LogicalType)TypeAccessor.Namespace variable,
            // which suggests the accessor will be attached to a specific namespace that may not apply correctly
            // in every case.  Just to be sure we get the namespace right on every node then, we say our namespace
            // is not portable so that we reflect over T[] for every namespace in which it appears.
            // Except in the case of encoded semantics, when an array will always belong to a fixed namespace,
            // and reference its elements by their namespace, so it is unaffected by the contextual default namespace.
            toInit.NamespaceIsPortable = encoded;

            // In cases where similar array-likes are reflected over (i.e. List<string> and string[])
            // we can only give one of them the name "string[]" for string look-up. 
            // We just let the first one to be reflected keep the name.
            TypeOrigin typeOriginAlreadyExists;
            bool nameAlreadyMapped = m_TypeContainer.ContainsType(new XmlQualifiedName(toInit.Name, toInit.Namespace), encoded, out typeOriginAlreadyExists);

            toInit.MappingFlags = eLType.MappingFlags;
            if (noTypeMapping)
                toInit.MappingFlags &= (~TypeMappingFlags.AllowTypeMapping);
            if (noNameMapping || nameAlreadyMapped)
                toInit.MappingFlags &= (~TypeMappingFlags.AllowNameMapping);

            toInit.TypeID = TypeID.ArrayLike;

            toInit.RootAccessor = toInit.TypeAccessor;

            if (eLType.Serializer == SerializerType.Array)
            {
                //see top of LogicalSoapInfo.cs for this line
                defaultEltAccessor.NestedAccessors = eLType.TypeAccessor.NestedAccessors;
            }

            //this is an array, so it needs to have a default
            toInit.TypeAccessor.NestedAccessors = new AccessorCollection();
            toInit.TypeAccessor.NestedAccessors.add(defaultEltAccessor);
            toInit.TypeAccessor.NestedAccessors.Default = defaultEltAccessor;
        }

        private LogicalType SynthesizeArrayType(XmlQualifiedName qname, bool encoded)
        {
            LogicalType eltLType;
            string eltName;
            string arrayName = qname.Name;
            string arrayNS = qname.Namespace;

            // Remove the ending brackets from the array type's name 
            int lastBracketNdx = arrayName.LastIndexOf('[');
            if (lastBracketNdx == -1) return null;
            eltName = arrayName.Substring(0, lastBracketNdx);

            if (encoded)
            {
                eltLType = FindType(new XmlQualifiedName(eltName, arrayNS), encoded);
            }
            else
            {
                // Look up the element by name found in the document
                eltLType = FindType(new XmlQualifiedName(eltName, arrayNS), encoded);

                if (eltLType == null)
                {
                    // If the first look up failed, then look up the element type by lower casing 
                    // the first letter in the element's name.
                    char lowerFirstChar = char.ToLowerInvariant(eltName[0]);
                    eltName = lowerFirstChar + eltName.Substring(1, eltName.Length - 1);
                    eltLType = FindType(new XmlQualifiedName(eltName, arrayNS), encoded);
                }
            }

            if (eltLType == null)
                return null;

            return SynthesizeArrayFromElement(eltLType, encoded);
        }

        private LogicalType SynthesizeArrayFromElement(LogicalType eltLType, bool encoded)
        {
            Debug.Assert(eltLType != null, "Can not synthesize an array from a null element type");

            Type arrayType = Array.CreateInstance(eltLType.Type, 0).GetType();

            if (arrayType == null)
                throw new InvalidOperationException(SR.Format(SR.XmlS_BadArrayType_1, eltLType.Type));

            LogicalType arrayLType = new LogicalType();
            InitializeArrayLike(arrayLType, arrayType, null /*TypeAttributes*/, eltLType, SerializerType.Array,
                false/*noTypeMapping*/, false/*noNameMapping*/, encoded, eltLType.TypeAccessor.Namespace);

            TypeOrigin typeOrigin;
            if (!m_TypeContainer.ContainsType(arrayType, arrayLType.Namespace, encoded, out typeOrigin))
                m_TypeContainer.AddType(arrayLType, encoded);
            return arrayLType;
        }

        /// <summary>
        /// Creates the name of an array to be used while serializing and deserializing
        /// using literal symantics. This method correctly handles multidimensional and 
        /// jagged arrays. Note that the array's element type is always capitalized.
        /// </summary>
        /// <example>
        ///  xsd:string[]    => ArrayOfString
        ///  tns:foo[]       => ArrayOfFoo
        ///  xsd:string[][]  => ArrayOfArrayOfString
        ///  tns:foo[][]     => ArrayOfArrayOfFoo
        /// </example>
        private static string MakeLiteralArrayName(LogicalType eltLType)
        {
            string eltName;
            Type eltType = eltLType.Type;
            StringBuilder arrayName = new StringBuilder();

            while (true)
            {
                arrayName.Append(SerializationStrings.ArrayOf);
                if (!eltType.HasElementType ||
                    TypeContainer.ContainsIntrinsicType(eltType)) //arrays which are also intrinsic, need to be handled separately.
                    break;
                eltType = eltType.GetElementType();
            }

            eltName = StripArrayBrackets(eltLType.Name);
            arrayName.Append(char.ToUpperInvariant(eltName[0]));
            arrayName.Append(eltName.Substring(1));

            return arrayName.ToString();
        }

        /// <summary>
        /// Helper for the MakeLiteralArrayName method. It removes the brackets from an 
        /// array name. 
        /// </summary>
        /// <example>
        ///  string[]    => string
        ///  foo         => foo
        ///  string[][]  => string
        ///  foo[][]     => foo
        /// </example>
        private static string StripArrayBrackets(string eleName)
        {
            StringBuilder eleNameSB = new StringBuilder(eleName.Length);

            for (int i = 0; i < eleName.Length; i++)
            {
                char c = eleName[i];

                if (c == '[')
                    break;

                eleNameSB.Append(c);
            }

            return eleNameSB.ToString();
        }
    }
}
