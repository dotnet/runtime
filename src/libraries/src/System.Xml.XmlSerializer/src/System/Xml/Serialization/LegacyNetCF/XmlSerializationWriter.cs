// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
//#define FORMATTED_OUTPUT

#pragma warning disable 0162
#pragma warning disable 0429
using System;
using System.Xml;
using System.Xml.Schema;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;

using Hashtable = System.Collections.InternalHashtable;

namespace System.Xml.Serialization.LegacyNetCF
{
    //===========================================================
    // This queue holds the objects that are to be serialized. 
    //===========================================================
    internal class SerializationQueue
    {
        private SerializationTask _head;
        private SerializationTask _tail;

        public SerializationQueue()
        {
        }

        //*********************************************************
        // Add a serialization task to the queue.
        //*********************************************************        
        public void enqueueTask(MemberValueCollection mvc, LogicalMemberValue m, Accessor a, object o)
        {
            enqueueTask(mvc, m, a, o, null);
        }

        public void enqueueTask(MemberValueCollection mvc, LogicalMemberValue m, Accessor a, object o, object f)
        {
            enqueueTask(mvc, m, a, o, f, null);
        }

        public void enqueueTask(MemberValueCollection mvc, LogicalMemberValue m, Accessor a, object o, object f, string parentElementNamespace)
        {
            SerializationTask t = new SerializationTask(mvc, m, a, o, f, parentElementNamespace);
            if (_head == null)
            {
                _head = t;
                _tail = t;
            }
            else
            {
                _tail.m_nextTask = t;
                _tail = t;
            }
        }

        //**************************************************************
        // Remove a serialization task from the queue.
        //**************************************************************        
        public void dequeueTask(out MemberValueCollection mvc, out LogicalMemberValue m, out Accessor a, out object o, out object f)
        {
            string p;

            dequeueTask(out mvc, out m, out a, out o, out f, out p);
        }

        public void dequeueTask(out MemberValueCollection mvc, out LogicalMemberValue m, out Accessor a, out object o, out object f, out string p)
        {
            Debug.Assert(!isEmpty(), "dequeue from empty list");
            SerializationTask ret = _head;
            _head = _head.m_nextTask;
            if (_head == null)
            {
                Debug.Assert(_tail == ret, "mismatch for 1 elt list");
                _tail = null;
            }
            mvc = ret.m_members;
            m = ret.m_member;
            a = ret.m_accessor;
            o = ret.m_value;
            f = ret.m_fetcherTarget;
            p = ret.m_parentElementNamespace;
        }

        public bool isEmpty()
        {
            return _head == null;
        }

        //==============================================================
        // This object represents a taks the XmlSerailizationWriter will
        // serialize. The task consists of the object to be serialized
        // and its associated accessor. There is also a link to the next
        // task in the queue.
        //==============================================================
        private class SerializationTask
        {
            public MemberValueCollection m_members;
            public LogicalMemberValue m_member;
            public Accessor m_accessor;
            public object m_value;
            public object m_fetcherTarget;
            public string m_parentElementNamespace;
            public SerializationTask m_nextTask = null;


            public SerializationTask(MemberValueCollection members, LogicalMemberValue member, Accessor accessor, object o, object f, string parentElementNamespace)
            {
                m_members = members;
                m_member = member;
                m_accessor = accessor;
                m_value = o;
                m_fetcherTarget = f;
                m_parentElementNamespace = parentElementNamespace;
            }
        }
    }

    internal class XmlSerializationWriter
    {
        private const int NOT_IN_CACHE = -1;
        private const int Base64LineSize = 76;
        private const int InDataLineSize = Base64LineSize / 4 * 3;
        private static char[] s_crlf = new char[] { '\r', '\n' };

        protected XmlWriter Writer;
        protected int m_namespaceNumber = 1;
        protected XmlSerializationReflector m_Reflector;
        protected bool m_Soap12;
        protected bool m_Encoded = false;
        protected bool m_WriteSerializerNamespaces = true;

        private Hashtable _forwardRefCache = new Hashtable();
        private int _forwardRefID = 1;

        private Hashtable _currentCycle = new Hashtable();

        //***************************************************************
        // Constructors
        //***************************************************************
        //
        public XmlSerializationWriter(XmlWriter xw, XmlSerializationReflector reflector, bool soap12)
        {
            m_Reflector = reflector;
            m_Soap12 = soap12;
            InitWriter(xw);
        }

        //***************************************************************
        // Properties
        //***************************************************************

        //-----------------------------------------------------------------
        // Decides which forward reference string to use based on the soap
        // version.
        //----------------------------------------------------------------- 
        protected string RefName
        {
            get { return m_Soap12 ? "ref" : "href"; }
        }

        internal bool Encoded
        {
            get { return m_Encoded; }
            set { m_Encoded = value; }
        }

        internal XmlSerializationReflector Reflector
        {
            get { return m_Reflector; }
        }

        //***************************************************************
        // Serialization
        //***************************************************************

        //-----------------------------------------------------------------
        // Serializes a primative as an element. If we need to serialize a 
        // primative we use this custom handling to make smaller, more 
        // compact serialization that interops with the desktop. These 
        // methods should only be called in the literal case.
        //-----------------------------------------------------------------
        public static void SerializePrimitive(object value, XmlWriter writer, LogicalType lType, string defaultNamespace, bool isSoap)
        {
            Debug.Assert(null != value, "Null value passed to SerializePrimitive");
            Debug.Assert(!isSoap, "XmlSerializationWriter.SerializePrimitive() should be called only when using literal serialization");

            // convert the primitive to it xml serialized string
            string strValue = SerializationHelper.PrimitiveToString(lType, value);

            // write the start document if it has not been written.
            if (writer.WriteState == WriteState.Start)
                writer.WriteStartDocument();

            // Write the element with simple content
            writer.WriteStartElement(lType.RootAccessor.EncodedName, lType.RootAccessor.Namespace);
            writer.WriteString(strValue);
            writer.WriteEndElement();
        }

        //-----------------------------------------------------------------
        // Serializes an enumeration as an element. If we need to serialize 
        // an enumeration we use this custom handling to make smaller, more 
        // compact serialization that interops with the desktop. This 
        // method should only be called in the literal case.
        //-----------------------------------------------------------------
        public static void SerializeEnumeration(object value, XmlWriter writer, LogicalType type, string defaultNamespace, bool isSoap)
        {
            Debug.Assert(null != value, "Null value passed to SerializeEnum");
            Debug.Assert(!isSoap, "XmlSerializationWriter.SerializeEnumeration() should be called only when using literal serialization");

            // convert the enum to it xml serialized string
            string strValue = SerializationHelper.EnumerationToString(type, value);

            // write the start document if it has not been written.
            if (writer.WriteState == WriteState.Start)
                writer.WriteStartDocument();

            // Write the element with simple content
            writer.WriteStartElement(type.RootAccessor.EncodedName, type.RootAccessor.Namespace);
            writer.WriteString(strValue);
            writer.WriteEndElement();
        }

        protected void serializeMembers(SerializationQueue queue,
                                         SerializationQueue delayedWork,
                                         bool recursiveCall,
                                         bool schemaReqTypeAttr,
                                         XmlSerializerNamespaces serializeNs)
        {
            while (!queue.isEmpty())
            {
                MemberValueCollection members;
                LogicalMemberValue member;
                Accessor a;
                object value;
                object fetcherTarget;
                string parentElementNamespace;

                queue.dequeueTask(out members, out member, out a, out value, out fetcherTarget, out parentElementNamespace);
                if (a.IsAttribute)
                    serializeAsAttribute(a, value, serializeNs, parentElementNamespace);
                else
                {
                    if (members != null && members.IsOrdered)
                    {
                        if (member != null && member.IsXmlAnyElement)
                        {
                            serializeXmlAny(members.XmlAny, member, value, fetcherTarget, member.Accessors.Default.Type, delayedWork, serializeNs);
                        }
                        else
                        {
                            SerializeAsElement(a, value, fetcherTarget, delayedWork, recursiveCall, schemaReqTypeAttr, serializeNs);
                        }
                    }
                    else
                    {
                        SerializeAsElement(a, value, fetcherTarget, delayedWork, recursiveCall, schemaReqTypeAttr, serializeNs);
                    }
                }
            }
        }

        protected virtual void SerializeNullValueAsElement(LogicalType serializeAs, Accessor accessor,
                                                            object value, SerializationQueue delayedWork,
                                                            bool recursiveCall, bool requiresTypeAttr,
                                                            XmlSerializerNamespaces serializerNs)
        {
            // write the start element 
            WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);

            // Write the XsiType id on is required
            if (requiresTypeAttr) WriteXsiTypeAttribute(serializeAs, serializerNs);

            // Write the nil attribute 
            if (accessor.IsNullable)
            {
                WriteNilAttribute();
            }

            // End the element
            Writer.WriteEndElement();
        }

        protected virtual void SerializePrimitiveAsElement(LogicalType serializeAs, Accessor accessor,
                                                            object value, SerializationQueue delayedWork,
                                                            bool recursiveCall, bool requiresTypeAttr,
                                                            XmlSerializerNamespaces serializerNs)
        {
            // Get the string value of the primitive
            string str = SerializationHelper.PrimitiveToString(serializeAs, value);

            // Write the start element                                    
            WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);

            // Write the XsiType if required
            if (requiresTypeAttr) WriteXsiTypeAttribute(serializeAs, serializerNs);

            // Write the primitive and the close the element
            Writer.WriteString(str);
            Writer.WriteEndElement();
        }

        protected virtual void SerializeEnumeratedAsElement(LogicalType serializeAs, Accessor accessor,
                                                            object value, SerializationQueue delayedWork,
                                                            bool recursiveCall, bool requiresTypeAttr,
                                                            XmlSerializerNamespaces serializerNs)
        {
            LogicalType serializeAsType = serializeAs.IsNullableType ? serializeAs.NullableValueType : serializeAs;

            // Get the string value of the enumerated value
            string str = SerializationHelper.EnumerationToString(serializeAsType, value);

            // Write the start element 
            WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);

            // Write the XsiType is required            
            if (requiresTypeAttr) WriteXsiTypeAttribute(serializeAsType, serializerNs);

            // Write the enumerated value and the end element
            Writer.WriteString(str);
            Writer.WriteEndElement();
        }

        protected virtual void SerializeArrayLikeAsElement(LogicalType serializeAs, Accessor accessor,
                                                            object value, object fetcherTarget,
                                                            SerializationQueue delayedWork,
                                                            bool recursiveCall, bool requiresTypeAttr,
                                                            XmlSerializerNamespaces serializerNs)
        {
            if (!m_Encoded)
                serializeLiteralArrayLike(accessor, serializeAs, value, fetcherTarget, requiresTypeAttr, serializerNs);
            else
                serializeEncodedArrayLike(accessor, serializeAs, value, delayedWork, recursiveCall, serializerNs);
        }

        protected virtual void SerializeComplexAsElement(LogicalType serializeAs, Accessor accessor,
                                                            object value, SerializationQueue delayedWork,
                                                            bool recursiveCall, bool requiresTypeAttr,
                                                            XmlSerializerNamespaces serializerNs)
        {
            int id = NOT_IN_CACHE;
            if (m_Encoded)
            {
                //in the case of encoded messages, there are 3 cases.
                //1) I'm an object at the top level.  (Includes
                //  synthetic element) 
                //2)    Serialize my members should be serialized.
                //      Unless I'm the synthetic element, I must have
                //      an ID.
                //3) I'm an object not at the top level.
                //      If I'm not in the cache, allocate an ID, and
                //      enqueue a serialization task.
                //
                //      in both cases, write a forward reference
                //
                // sync code with Array case                                 
                if (!recursiveCall)
                {
                    id = NOT_IN_CACHE;
                    //null type indicates synthetic root element.
                    if (serializeAs.Type != null)
                    {
                        id = lookInCache(value);
                        if (id == NOT_IN_CACHE)
                            id = addToCache(value);
                    }
                }
                else
                {
                    id = lookInCache(value);
                    if (m_Soap12)
                    {
                        if (id != NOT_IN_CACHE)
                        {
                            WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);
                            WriteForwardReference(id);
                            Writer.WriteEndElement();
                            return;
                        }
                    }
                    else
                    {
                        if (id == NOT_IN_CACHE)
                        {
                            id = addToCache(value);
                            delayedWork.enqueueTask(null, null, serializeAs.TypeAccessor, value);
                        }

                        //write out the forward reference
                        WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);
                        WriteForwardReference(id);
                        Writer.WriteEndElement();
                        return;
                    }
                }
            }

            if (!m_Encoded)
                addObjectToCycle(value);

            WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);

            if (m_Encoded)
            {
                if (0 == (serializeAs.MappingFlags & TypeMappingFlags.Synthetic))
                {
                    if (id == NOT_IN_CACHE)
                        id = addToCache(value);
                    WriteObjectIdentifier(id);
                }
            }

            if (requiresTypeAttr)
                WriteXsiTypeAttribute(serializeAs, serializerNs);

            MemberValueCollection members = serializeAs.Members;
            SerializationQueue attributes = new SerializationQueue();
            SerializationQueue elements = new SerializationQueue();

            if (members.IsOrdered)
            {
                // Enqueue the ordered elements
                EnqueueAttributeMembers(members, attributes, value, accessor.Namespace);
                EnqueueElementMembersInOrder(members, elements, value);
            }
            else
            {
                // Enqueue all of the members regardless of order
                EnqueueMembers(serializeAs.Members, attributes, elements, value, accessor.Namespace);
            }

            if (!m_Encoded)
            {
                //write out possible namespaces
                LogicalMemberValue namespaces = members.XmlNamespaceDecls;
                if (namespaces != null)
                    WriteNamespacesMember(namespaces, value, serializerNs);

                //write out possible any attributes
                serializeAnyAttributes(members, value, serializerNs);
            }

            serializeMembers(attributes, null, true, false, serializerNs);
            serializeMembers(elements, delayedWork, true, m_Encoded, serializerNs);

            if (!m_Encoded)
            {
                serializeText(members, value);
                if (!members.IsOrdered)
                    serializeAnyElements(members, value, delayedWork, serializerNs);
            }

            Writer.WriteEndElement();

            if (!m_Encoded)
                removeObjectFromCycle(value);
        }

        protected virtual void SerializeSerializableAsElement(LogicalType serializeAs,
                                                               Accessor accessor,
                                                               object value,
                                                               SerializationQueue delayedWork,
                                                               bool recursiveCall,
                                                               bool requiresTypeAttr,
                                                               XmlSerializerNamespaces serializerNs)
        {
            WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);

            ((IXmlSerializable)value).WriteXml(Writer);

            Writer.WriteEndElement();
        }

        protected virtual void SerializeCustomAsElement(LogicalType serializeAs, Accessor accessor,
                                                        object value, SerializationQueue delayedWork,
                                                        bool recursiveCall, bool requiresTypeAttr,
                                                        XmlSerializerNamespaces serializerNs)
        {
            if (serializeAs.CustomSerializer == CustomSerializerType.QName)
            {
                SerializeQNameAsElement(serializeAs, accessor, (XmlQualifiedName)value, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                return;
            }

            WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);
            if (requiresTypeAttr) WriteXsiTypeAttribute(serializeAs, serializerNs);
            WriteCustom(serializeAs, value);
            Writer.WriteEndElement();
        }

        private void SerializeQNameAsElement(LogicalType serializeAs, Accessor accessor,
            XmlQualifiedName value, SerializationQueue delayedWork,
            bool recursiveCall, bool requiresTypeAttr,
            XmlSerializerNamespaces serializerNs)
        {
            Debug.Assert(serializeAs.CustomSerializer == CustomSerializerType.QName, "Attempting to serialize an object as a QName but its serializer property reports that it is not a qname");

            if ((accessor.Namespace != null && accessor.Namespace.Length > 0) && (value.Namespace == null || value.Namespace.Length == 0))
            {
                // The wrapping element has a namespace, but the qname does not have a namespace. In 
                // this case, a prefix is created that maps to the element's namespace and the wrapping 
                // elment is explicitly prefixed with this new prefix.  
                // Ex: <q1:wrapper xmlns="" xmlns:q1="http://wrappernamespace">localName</q1:wrapper> 
                Writer.WriteStartElement(MakePrefix(), accessor.EncodedName, accessor.Namespace);
                if (requiresTypeAttr) WriteXsiTypeAttribute(serializeAs, serializerNs);
                if (value == XmlQualifiedName.Empty)
                {
                    Writer.WriteEndElement();
                    return;
                }
                Writer.WriteAttributeString("xmlns", null, string.Empty);
            }
            else
            {
                // In this case, the wrapper's namespace is set as the default namespace and a
                // prefix for the qname's namepace is declared on the wrapper element.
                // Ex: <wrapper xmlns="http://wrappernamespace" xmlns:q1="qnnamespace">q1:localName</wrapper>
                WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);
                if (requiresTypeAttr) WriteXsiTypeAttribute(serializeAs, serializerNs);
                if (value == XmlQualifiedName.Empty)
                {
                    Writer.WriteEndElement();
                    return;
                }

                EnsureNamespaceDeclaration(value.Namespace, serializerNs);
            }

            WriteCustom(serializeAs, value);
            Writer.WriteEndElement();
        }

#if !FEATURE_LEGACYNETCF
        protected virtual void SerializeXmlNodeAsElement(LogicalType serializeAs, Accessor accessor,
                                                            object value, SerializationQueue delayedWork,
                                                            bool recursiveCall, bool requiresTypeAttr,
                                                            XmlSerializerNamespaces serializerNs) {
            WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);
            ((XmlNode)value).WriteTo(Writer);
            Writer.WriteEndElement();
        }
#endif

        // This method should be overridden by serializers that want to handle
        // a value who's serializer type is UnknownHeader.
        protected virtual void SerializeUnknownHeaderAsElement(LogicalType serializeAs,
                                                                Accessor accessor,
                                                                object value,
                                                                SerializationQueue delayedWork,
                                                                bool recursiveCall,
                                                                bool requiresTypeAttr,
                                                                XmlSerializerNamespaces serializerNs)
        {
            Debug.Assert(false, "XmlSerializer can not serialize unknown headers.");
        }

        protected void SerializeQueuedObjects(SerializationQueue queue)
        {
            while (!queue.isEmpty())
            {
                MemberValueCollection mvc;
                LogicalMemberValue m;
                Accessor a;
                object value;
                object fetcherTarget;

                queue.dequeueTask(out mvc, out m, out a, out value, out fetcherTarget);
                if (!a.IsAttribute)
                    SerializeAsElement(a, value, fetcherTarget, queue, false, true, null);
            }
        }


        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public void SerializeAsElement(Accessor accessor, object value)
        {
            SerializationQueue delayedWork = new SerializationQueue();
            SerializeAsElement(accessor, value, null, delayedWork, false, false, null/*XmlSerializerNamespaces*/);
            SerializeQueuedObjects(delayedWork);
        }

        public void SerializeAsElement(Accessor accessor, object value, XmlSerializerNamespaces ns)
        {
            SerializationQueue delayedWork = new SerializationQueue();
            SerializeAsElement(accessor, value, null, delayedWork, false, false, ns);
            SerializeQueuedObjects(delayedWork);
        }



        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        protected virtual void SerializeAsElement(Accessor accessor, object value, SerializationQueue delayedWork, bool recursiveCall, bool schemaReqTypeAttr)
        {
            SerializeAsElement(accessor, value, null, delayedWork, recursiveCall, schemaReqTypeAttr, null/*XmlSerializerNamespaces*/);
        }

        private bool WriteNamespaceOnType(Accessor accessor, LogicalType serializeAs)
        {
            if (m_Encoded || (accessor.Namespace != Soap.XsdUrl && accessor.Namespace != Soap.UrtTypesNS)) return true;

            return serializeAs.Serializer != SerializerType.Primitive &&
                    serializeAs.Serializer != SerializerType.Custom &&
                    accessor.Type.CustomSerializer != CustomSerializerType.Object;
        }

        //////////
        // The schemaReqTypeAttr parameter is a special case for RPC encoded
        // arrays.  Encoded arrays are the only place where the type attribute
        // isn't required for an element.        
        protected virtual void SerializeAsElement(Accessor accessor, object value, object fetcherTarget,
                                                   SerializationQueue delayedWork, bool recursiveCall,
                                                   bool schemaReqTypeAttr, XmlSerializerNamespaces serializerNs)
        {
            bool requiresTypeAttr;
            LogicalType serializeAs;

            if (value == null)
            {
                serializeAs = accessor.Type;
                //only emit the type attribute on the null if necessary.
                requiresTypeAttr = (schemaReqTypeAttr && serializeAs.Type != null);
            }
            else
            {
                Debug.Assert(!accessor.IsAttribute, "Serializing an attribute as an element");
                Debug.Assert((value == null ? accessor.IsNullable : true), "serializing omitted element");

                serializeAs = getTypeForSerialization(ref accessor, value, out requiresTypeAttr);
            }

            Debug.Assert(serializeAs != null, "Could not find the LogicalType with which to serialize the object.");
            if (schemaReqTypeAttr && serializeAs.Type != null)
                requiresTypeAttr = true;
            if (false /* AppDomain.CompatVersion < AppDomain.Orcas */ && !WriteNamespaceOnType(accessor, serializeAs))
            { // prior to Orcas
                accessor = accessor.copy();
                accessor.Namespace = string.Empty;
            }

            //write null
            if (value == null)
            {
                SerializeNullValueAsElement(serializeAs, accessor, value, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                return;
            }

            switch (serializeAs.Serializer)
            {
                case SerializerType.Primitive:
                    SerializePrimitiveAsElement(serializeAs, accessor, value, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                    break;
                case SerializerType.Enumerated:
                    SerializeEnumeratedAsElement(serializeAs, accessor, value, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                    break;
                case SerializerType.Array:
                case SerializerType.Collection:
                case SerializerType.Enumerable:
                    SerializeArrayLikeAsElement(serializeAs, accessor, value, fetcherTarget, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                    break;
                case SerializerType.Complex:
                    SerializeComplexAsElement(serializeAs, accessor, value, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                    break;
                case SerializerType.Serializable:
                    SerializeSerializableAsElement(serializeAs, accessor, value, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                    break;
                case SerializerType.Custom:
                    SerializeCustomAsElement(serializeAs, accessor, value, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                    break;
#if !FEATURE_LEGACYNETCF
                case SerializerType.XmlNode:
                    SerializeXmlNodeAsElement(serializeAs, accessor, value, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                    break;
#endif
                case SerializerType.UnknownHeader:
                    SerializeUnknownHeaderAsElement(serializeAs, accessor, value, delayedWork, recursiveCall, requiresTypeAttr, serializerNs);
                    break;
            }
        }

        protected void SerializePrimitiveAsAttribute(LogicalType serializeAs, Accessor accessor, object value, XmlSerializerNamespaces serializerNs, string parentElementNamespace)
        {
            string str = SerializationHelper.PrimitiveToString(serializeAs, value);
            if (Soap.XmlNamespace.Equals(accessor.Namespace) && null == LookupPrefix(accessor.Namespace, serializerNs))
                WriteAttributeString(Soap.XmlPrefix, accessor.EncodedName, accessor.Namespace, str);
            else if (parentElementNamespace == accessor.Namespace)
                WriteAttributeString(accessor.EncodedName, null, str, serializerNs);
            else
                WriteAttributeString(accessor.EncodedName, accessor.Namespace, str, serializerNs);
        }

        protected void SerializeEnumeratedAsAttribute(LogicalType serializeAs, Accessor accessor, object value, XmlSerializerNamespaces serializerNs, string parentElementNamespace)
        {
            string str = SerializationHelper.EnumerationToString(!serializeAs.IsNullableType ? serializeAs : serializeAs.NullableValueType, value);
            if (Soap.XmlNamespace.Equals(accessor.Namespace) && null == LookupPrefix(accessor.Namespace, serializerNs))
                WriteAttributeString(Soap.XmlPrefix, accessor.EncodedName, accessor.Namespace, str);
            else if (parentElementNamespace == accessor.Namespace)
                WriteAttributeString(accessor.EncodedName, null, str, serializerNs);
            else
                WriteAttributeString(accessor.EncodedName, accessor.Namespace, str, serializerNs);
        }

        protected virtual void SerializeCustomAsAttribute(LogicalType serializeAs, Accessor accessor, object value, XmlSerializerNamespaces serializerNs, string parentElementNamespace)
        {
            if (serializeAs.CustomSerializer == CustomSerializerType.QName)
            {
                if ((XmlQualifiedName)value == XmlQualifiedName.Empty)
                    return;

                EnsureNamespaceDeclaration(((XmlQualifiedName)value).Namespace, serializerNs);
            }

            if (Soap.XmlNamespace.Equals(accessor.Namespace) && null == LookupPrefix(accessor.Namespace, serializerNs))
                WriteStartAttribute(Soap.XmlPrefix, accessor.EncodedName, accessor.Namespace);
            else if (parentElementNamespace == accessor.Namespace)
                WriteStartAttribute(accessor.EncodedName, null, serializerNs);
            else
                WriteStartAttribute(accessor.EncodedName, accessor.Namespace, serializerNs);
            WriteCustom(serializeAs, value);
            Writer.WriteEndAttribute();
        }

        protected void SerializeArrayAsAttribute(LogicalType serializeAs, Accessor accessor, object value, XmlSerializerNamespaces serializerNs, string parentElementNamespace)
        {
            //algorithm:
            //1) Determine element type.
            //2) If the type is primitive, write out the values using
            //      WriteString separated by spaces.
            //3) If the value is an enum, write out the values using
            //      WriteString separated by spaces.
            //4) If the value is custom.
            //      For qualified names, Ensure all the prefixes are
            //      declared, then go back and write the strings.
            //      For everythign else, same as 2 and 3
            //Note: SerializeCollectionAsAttribute is very similar in terms of logic to the array serialization
            //If a bug fix is made here, make sure to see if it applies to that function as well.
            Debug.Assert(!m_Encoded, "serializing an encoded attribute array");

            //this gets me the right element type for things with datatypes.
            AccessorCollection nested = accessor.NestedAccessors;
            if (nested == null)
                nested = serializeAs.TypeAccessor.NestedAccessors;
            LogicalType elementType = nested.Default.Type;
            Array array = (Array)value;

            string attributeNs = (parentElementNamespace == accessor.Namespace) ? null : accessor.Namespace;
            if (elementType.Serializer == SerializerType.Primitive)
            {
                WriteStartAttribute(accessor.EncodedName, attributeNs, serializerNs);
                for (int i = 0; i < array.Length; ++i)
                {
                    object val = array.GetValue(i);
                    if (false /* AppDomain.CompatVersion <= AppDomain.Orcas */)
                    {
                        if (val == null) continue;
                    }
                    if (i != 0) Writer.WriteString(" ");

                    string elt = SerializationHelper.PrimitiveToString(elementType, val);
                    Writer.WriteString(elt);
                }
            }
            else if (elementType.Serializer == SerializerType.Enumerated)
            {
                WriteStartAttribute(accessor.EncodedName, attributeNs, serializerNs);
                for (int i = 0; i < array.Length; ++i)
                {
                    object val = array.GetValue(i);
                    if (val == null) continue;
                    if (i != 0) Writer.WriteString(" ");

                    string elt = SerializationHelper.EnumerationToString(elementType, val);
                    Writer.WriteString(elt);
                }
            }
            else if (elementType.Serializer == SerializerType.Custom)
            {
                if (elementType.CustomSerializer == CustomSerializerType.QName)
                {
                    for (int i = 0; i < array.Length; ++i)
                    {
                        if (array.GetValue(i) != null)
                        {
                            XmlQualifiedName qname = (XmlQualifiedName)array.GetValue(i);
                            EnsureNamespaceDeclaration(qname.Namespace, serializerNs);
                        }
                    }

                    // Serialize the array of qnames. 
                    WriteStartAttribute(accessor.EncodedName, attributeNs, serializerNs);
                    for (int i = 0; i < array.Length; ++i)
                    {
                        XmlQualifiedName qname = (XmlQualifiedName)array.GetValue(i);
                        if (qname == null) continue;
                        if (i != 0) Writer.WriteString(" ");

                        string prefix = LookupPrefix(qname.Namespace);
                        string s = (prefix != null && prefix.Length > 0) ?
                            prefix + ":" + XmlConvert.EncodeLocalName(qname.Name) :
                            qname.Name;

                        Writer.WriteString(s);
                    }
                }
                else if (elementType.CustomSerializer == CustomSerializerType.Base64 || elementType.CustomSerializer == CustomSerializerType.Hex)
                {
                    WriteStartAttribute(accessor.EncodedName, attributeNs, serializerNs);
                    bool isBase64 = elementType.CustomSerializer == CustomSerializerType.Base64;

                    for (int i = 0; i < array.Length; ++i)
                    {
                        object val = array.GetValue(i);
                        if (val == null) continue;

                        byte[] bytes = (byte[])array.GetValue(i);
                        if (isBase64)
                            WriteBase64(bytes);
                        else
                            Writer.WriteBinHex(bytes, 0, bytes.Length);
                    }
                }
                else
                {
                    Debug.WriteLine("Invalid custom type " + elementType.Type.FullName);
                    throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, elementType.Type));
                }
            }
            Writer.WriteEndAttribute();
        }

        protected void SerializeCollectionAsAttribute(LogicalType serializeAs, Accessor accessor, object value, XmlSerializerNamespaces serializerNs, string parentElementNamespace)
        {
            //algorithm:
            //1) Determine element type.
            //2) If the type is primitive, write out the values using
            //      WriteString separated by spaces.
            //3) If the value is an enum, write out the values using
            //      WriteString separated by spaces.
            //4) If the value is custom.
            //      For qualified names, Ensure all the prefixes are
            //      declared, then go back and write the strings.
            //      For everythign else, same as 2 and 3
            //Note: SerializeArrayAsAttribute is very similar in terms of logic to the collection serialization
            //If a bug fix is made here, make sure to see if it applies to that function as well.
            Debug.Assert(!m_Encoded, "serializing an encoded attribute array");

            //this gets me the right element type for things with datatypes.
            AccessorCollection nested = accessor.NestedAccessors;
            if (nested == null)
                nested = serializeAs.TypeAccessor.NestedAccessors;
            LogicalType elementType = nested.Default.Type;
            IEnumerable enumerable = (IEnumerable)value;
            IEnumerator enumerator = enumerable.GetEnumerator();

            string attributeNs = (parentElementNamespace == accessor.Namespace) ? null : accessor.Namespace;
            if (elementType.Serializer == SerializerType.Primitive)
            {
                WriteStartAttribute(accessor.EncodedName, attributeNs, serializerNs);
                int i = 0;

                while (enumerator.MoveNext())
                {
                    object val = enumerator.Current;
                    if (false /* AppDomain.CompatVersion <= AppDomain.Orcas */)
                    {
                        if (val == null) continue;
                    }
                    if (i != 0) Writer.WriteString(" ");

                    string elt = SerializationHelper.PrimitiveToString(elementType, val);
                    Writer.WriteString(elt);
                    ++i;
                }
            }
            else if (elementType.Serializer == SerializerType.Enumerated)
            {
                WriteStartAttribute(accessor.EncodedName, attributeNs, serializerNs);
                int i = 0;

                while (enumerator.MoveNext())
                {
                    object val = enumerator.Current;
                    if (val == null) continue;
                    if (i != 0) Writer.WriteString(" ");

                    string elt = SerializationHelper.EnumerationToString(elementType, val);
                    Writer.WriteString(elt);
                    ++i;
                }
            }
            else if (elementType.Serializer == SerializerType.Custom)
            {
                if (elementType.CustomSerializer == CustomSerializerType.QName)
                {
                    int i = 0;

                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current != null)
                        {
                            XmlQualifiedName qname = (XmlQualifiedName)enumerator.Current;
                            EnsureNamespaceDeclaration(qname.Namespace, serializerNs);
                        }
                        ++i;
                    }

                    // Serialize the array of qnames. 
                    WriteStartAttribute(accessor.EncodedName, attributeNs, serializerNs);
                    i = 0;

                    while (enumerator.MoveNext())
                    {
                        XmlQualifiedName qname = (XmlQualifiedName)enumerator.Current;
                        if (qname == null) continue;
                        if (i != 0) Writer.WriteString(" ");

                        string prefix = LookupPrefix(qname.Namespace);
                        string s = (prefix != null && prefix.Length > 0) ?
                            prefix + ":" + XmlConvert.EncodeLocalName(qname.Name) :
                            qname.Name;

                        Writer.WriteString(s);
                        ++i;
                    }
                }
                else if (elementType.CustomSerializer == CustomSerializerType.Base64 || elementType.CustomSerializer == CustomSerializerType.Hex)
                {
                    WriteStartAttribute(accessor.EncodedName, attributeNs, serializerNs);
                    bool isBase64 = elementType.CustomSerializer == CustomSerializerType.Base64;

                    int i = 0;

                    while (enumerator.MoveNext())
                    {
                        object val = enumerator.Current;
                        if (val == null) continue;

                        byte[] bytes = (byte[])enumerator.Current;
                        if (isBase64)
                            WriteBase64(bytes);
                        else
                            Writer.WriteBinHex(bytes, 0, bytes.Length);
                        ++i;
                    }
                }
                else
                {
                    Debug.WriteLine("Invalid custom type " + elementType.Type.FullName);
                    throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, elementType.Type));
                }
            }
            Writer.WriteEndAttribute();
        }

        protected void serializeAsAttribute(Accessor accessor, object value, XmlSerializerNamespaces serializerNs, string parentElementNamespace)
        {
            Debug.Assert(accessor.IsAttribute, "Serializing an element as an attribute");
            Debug.Assert(value != null, "serializing null attribute");
            bool requiresTypeAttr;

            LogicalType serializeAs = getTypeForSerialization(ref accessor, value, out requiresTypeAttr);
            if (requiresTypeAttr)
            {
                Debug.WriteLine("Requires type attribute on an attribute");
                throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, value.GetType()));
            }

            switch (serializeAs.Serializer)
            {
                case SerializerType.Primitive:
                    SerializePrimitiveAsAttribute(serializeAs, accessor, value, serializerNs, parentElementNamespace);
                    break;
                case SerializerType.Enumerated:
                    SerializeEnumeratedAsAttribute(serializeAs, accessor, value, serializerNs, parentElementNamespace);
                    break;
                case SerializerType.Array:
                    //collections aren't legal for sequences
                    SerializeArrayAsAttribute(serializeAs, accessor, value, serializerNs, parentElementNamespace);
                    break;
                case SerializerType.Custom:
                    SerializeCustomAsAttribute(serializeAs, accessor, value, serializerNs, parentElementNamespace);
                    break;
                case SerializerType.Collection:
                    SerializeCollectionAsAttribute(serializeAs, accessor, value, serializerNs, parentElementNamespace);
                    break;
                default:
                    Debug.WriteLine("serializing " + serializeAs.Serializer + " as attribute");
                    throw new InvalidOperationException(SR.Format(SR.XmlS_IllegalAttrType_1, serializeAs.Type));
            }
        }

        protected void serializeEncodedArrayLike(Accessor accessor, LogicalType serializeAs,
                                                    object value, SerializationQueue delayedWork,
                                                    bool recursiveCall, XmlSerializerNamespaces serializerNs)
        {
            if (accessor.AllowRepeats)
            {
                Debug.Assert(accessor.IsHeader, "repeating non-header array");
                /*
                 * XXX
                 * * Thu 12/20/2001
                 * Very very special case of serializing arrays of headers.
                 * I'm allowed to serialize them as repeated element arrays.
                 * However, repeated element arrays are not a legal encoded
                 * constructs normally.
                 *
                 * In addition, arrays are the only legal ArrayLike here.
                 * */
                Array array = (Array)value;

                AccessorCollection eltAccessors = accessor.NestedAccessors;
                if (eltAccessors == null)
                {
                    eltAccessors = accessor.Type.TypeAccessor.NestedAccessors;
                }
                Accessor repeated = eltAccessors.Default;

                for (int i = 0; i < array.Length; ++i)
                {
                    SerializeAsElement(repeated, array.GetValue(i), null, delayedWork, false, false, serializerNs);
                }
                return;
            }
            int id;

            // sync code with Complex case            
            if (!recursiveCall)
            {
                id = lookInCache(value);
                if (id == NOT_IN_CACHE)
                    id = addToCache(value);
            }
            else
            {
                id = lookInCache(value);
                if (m_Soap12)
                {
                    if (id != NOT_IN_CACHE)
                    {
                        WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);
                        WriteForwardReference(id);
                        Writer.WriteEndElement();
                        return;
                    }
                }
                else
                {
                    if (id == NOT_IN_CACHE)
                    {
                        id = addToCache(value);
                        delayedWork.enqueueTask(null, null, serializeAs.TypeAccessor, value);
                    }

                    WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);
                    WriteForwardReference(id);
                    Writer.WriteEndElement();
                    return;
                }
            }

            WriteStartElement(accessor.EncodedName, accessor.GetEncodedNamespace(m_Soap12), serializerNs);

            if (m_Soap12 && id == NOT_IN_CACHE)
                id = addToCache(value);

            WriteObjectIdentifier(id);
            WriteArrayTypeAttribute(serializeAs, getLength(value, serializeAs), serializerNs);

            //for encoded arrays, everything has the same name            
            Accessor eltAccessor = m_Soap12 ? serializeAs.TypeAccessor.NestedAccessors.Default : accessor.NestedAccessors.Default;
            ElementSerializer serializer = new EncodedElementSerializer(this, eltAccessor, delayedWork);
            for_each(value, serializeAs, serializer, serializerNs);

            Writer.WriteEndElement();
        }

        protected void serializeLiteralArrayAsText(LogicalMemberValue arrayMember, LogicalType arrayType, object value, bool writeXsiType, XmlSerializerNamespaces serializerNs)
        {
            addObjectToCycle(value);
            ElementSerializer serializer = ElementSerializer.CreateElementSerializer(this, null, arrayMember.Accessors, LiteralElementSerializerType.Text);
            for_each(value, arrayType, serializer, serializerNs);
            removeObjectFromCycle(value);
        }

        protected void serializeLiteralArrayAsAny(AccessorCollection eltAccessors, LogicalType arrayType, object value, object fetcherTarget, bool writeXsiType, XmlSerializerNamespaces serializerNs)
        {
            addObjectToCycle(value);
            XmlChoiceSupport choice = eltAccessors != null ? eltAccessors.Choice : null;
            ElementSerializer serializer = ElementSerializer.CreateElementSerializer(this, null, eltAccessors, choice, fetcherTarget, LiteralElementSerializerType.AnyElement);
            for_each(value, arrayType, serializer, serializerNs);
            removeObjectFromCycle(value);
        }

        protected void serializeLiteralArrayLike(Accessor accessor, LogicalType type,
            object value, object fetcherTarget,
            bool writeXsiType, XmlSerializerNamespaces serializerNs)
        {
            //check for cycles
            addObjectToCycle(value);
            if (accessor.AllowRepeats)
            {
                //There are no forward references in literal messages.
                AccessorCollection eltAccessors = accessor.NestedAccessors;
                if (eltAccessors == null)
                {
                    eltAccessors = accessor.Type.TypeAccessor.NestedAccessors;
                }

                ElementSerializer serializer = ElementSerializer.CreateElementSerializer(this, accessor, eltAccessors, eltAccessors.Choice, fetcherTarget, LiteralElementSerializerType.Default);

                for_each(value, type, serializer, serializerNs);
            }
            else
            {
                WriteStartElement(accessor.EncodedName, accessor.Namespace, serializerNs);
                if (writeXsiType) WriteXsiTypeAttribute(type, serializerNs);

                // Serialize the elements as the type found in the accessor
                AccessorCollection eltAccessors = accessor.NestedAccessors;

                // If the accessor had no nested accessors then fallback to the
                // accessor found in the accessor's logical type.
                if (eltAccessors == null)
                    eltAccessors = accessor.Type.TypeAccessor.NestedAccessors;
                // If the accessor's logical type had no nested accessors, then
                // fall back to the LogicalType found in the type parameter.
                if (eltAccessors == null)
                    eltAccessors = type.TypeAccessor.NestedAccessors;
                // If the LogicalType had no nested accessors then we fall back to 
                // serializing the object as an object array. The call to 
                // findByType(typeof(object[])) should never fail. Object[] is a 
                // built in type.
                if (eltAccessors == null)
                    eltAccessors = m_Reflector.FindTypeInNamespaces(typeof(object[]), m_Encoded, m_Reflector.DefaultNamespace, accessor.Namespace).TypeAccessor.NestedAccessors;

                Debug.Assert(eltAccessors != null, "The accessors for the object array is null. We do not know how to serialize the array elements.");

                ElementSerializer serializer = ElementSerializer.CreateElementSerializer(this, accessor, eltAccessors, eltAccessors.Choice, fetcherTarget, LiteralElementSerializerType.Default);
                for_each(value, type, serializer, serializerNs);
                Writer.WriteEndElement();
            }

            removeObjectFromCycle(value);
        }

        protected void serializeText(MemberValueCollection members, object fetchTarget)
        {
            LogicalMemberValue member = members.XmlText;
            if (null == member) return;
            if (members.IsOrdered && member.IsXmlAnyElement)
                return;
            object value = member.Fetcher.fetch(fetchTarget);
            if (value == null) return;

            LogicalType textType = member.Accessors.Default.Type;
            SerializerType serializer = textType.Serializer;
            switch (textType.Serializer)
            {
                case SerializerType.Primitive:
                case SerializerType.Enumerated:
                case SerializerType.Custom:
                case SerializerType.XmlNode:
                    doValueSerialization(member, textType, value);
                    break;
                case SerializerType.Array:
                    serializeLiteralArrayAsText(member, textType, value, false/*writeXsiType*/, null/*XmlSerializerNamespaces*/ );
                    break;
                default:
                    Debug.WriteLine("Illegal text type " + textType.Type);
                    throw new InvalidOperationException(SR.Format(SR.XmlS_IllegalTextType_1, textType.Type));
            }
        }

        protected void serializeAnyAttributes(MemberValueCollection members, object fetchTarget, XmlSerializerNamespaces serializerNs)
        {
            if (!members.HasXmlAny)
                return;
            LogicalMemberValue any = members.XmlAny.XmlAnyAttribute;
            if (any == null)
                return;
            object value = any.Fetcher.fetch(fetchTarget);
            if (members.XmlAny.XmlAnyAttribute.OutArgument != null)
                members.XmlAny.XmlAnyAttribute.OutArgument.DefaultValue = value;
            if (value != null)
                serializeXmlAny(members.XmlAny, any, value, fetchTarget, any.Accessors.Default.Type, null, serializerNs);
        }

        protected void serializeAnyElements(MemberValueCollection members, object fetchTarget, SerializationQueue delayedWork, XmlSerializerNamespaces serializerNs)
        {
            object value;
            object textValue = members.XmlText != null ? members.XmlText.Fetcher.fetch(fetchTarget) : null;

            if (!members.HasXmlAny)
                return;

            Hashtable serializedAnys = new Hashtable();
            //serialize the specific any arrays           
            foreach (LogicalMemberValue member in members.XmlAny.getSpecializedElementAnys())
            {
                if (serializedAnys[member] != null)
                    continue;

                value = member.Fetcher.fetch(fetchTarget);
                if (value != null && value != textValue)
                    serializeXmlAny(members.XmlAny, member, value, fetchTarget, member.Accessors.Default.Type, delayedWork, serializerNs);
                serializedAnys[member] = member;
            }

            //serialize the general any array
            LogicalMemberValue member2 = members.XmlAny.Default;
            if (member2 != null)
            {
                if (serializedAnys[member2] == null)
                {
                    value = member2.Fetcher.fetch(fetchTarget);
                    if (value != null && value != textValue)
                        serializeXmlAny(members.XmlAny, member2, value, fetchTarget, member2.Accessors.Default.Type, delayedWork, serializerNs);
                }
            }
        }

        private void serializeXmlAny(XmlAnyCollection anyCollection, LogicalMemberValue member, object o, object fetcherTarget, LogicalType type, SerializationQueue delayedWork, XmlSerializerNamespaces serializerNs)
        {
            if (type.Type.IsArray)
            {
                serializeLiteralArrayAsAny(anyCollection.lookupElementAccessors(member), type, o, fetcherTarget, false /*writeXsiType*/, null /*serializerNs*/);
            }
            else
            {
                Accessor accessor = null;

                // Is there an choice member for the any?
                accessor = FindAccessorByChoiceMember(member, fetcherTarget, false);
                if (accessor != null)
                {
                    if (accessor.IsXmlAnyElement)
                        SerializeObjectAsNode(o, true);
                    else
                        SerializeAsElement(accessor, o, fetcherTarget, delayedWork, true, false, serializerNs);
                    return;
                }

                // Is there an XmlElementAccessor for the any
                accessor = anyCollection.lookupElementAccessor(o.GetType(), member);
                if (accessor != null)
                {
                    if (accessor.IsXmlAnyElement)
                        SerializeObjectAsNode(o, true);
                    else
                        SerializeAsElement(accessor, o, fetcherTarget, delayedWork, true, false, serializerNs);
                    return;
                }

                if (typeof(IXmlSerializable).IsAssignableFrom(type.Type))
                {
                    SerializeAsElement(type.TypeAccessor, o, fetcherTarget, delayedWork, true, false, serializerNs);
                    return;
                }

                // If there is not choice or element accessor then we can not determine the type, so
                // serialize the any as an XmlNode if possible, throw if the value is not an XmlNode.
                SerializeObjectAsNode(o, true);
            }
        }

        private void SerializeObjectAsNode(object o, bool throwIfNotNode)
        {
            if (typeof(XmlNode).IsAssignableFrom(o.GetType()))
            {
#if !FEATURE_LEGACYNETCF
                XmlNode node = o as XmlNode;
                if (node != null) node.WriteTo(Writer);
#endif
            }
            else if (throwIfNotNode)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, o.GetType().FullName));
            }
        }

        protected void doValueSerialization(LogicalMemberValue member, LogicalType textType, object value)
        {
            switch (textType.Serializer)
            {
                case SerializerType.Primitive:
                    {
                        string str = SerializationHelper.PrimitiveToString(textType, value);
                        Writer.WriteString(str);
                        break;
                    }
                case SerializerType.Enumerated:
                    {
                        string str = SerializationHelper.EnumerationToString(textType, value);
                        Writer.WriteString(str);
                        break;
                    }
#if !FEATURE_LEGACYNETCF
                case SerializerType.XmlNode: {
                        if (value == null) return;
                        ((XmlNode)value).WriteTo(Writer);
                        break;
                    }
#endif

                case SerializerType.Custom:
                    /*
                     * XXX
                     * * Wed 11/28/2001
                     * ok, this is REALLY SKETCHY.
                     * The only time its legal to have an xml text section of
                     * type QName is when all the other members are attributes.
                     * So, at this point I've written a bunch of other
                     * elements, and nothing else.  So, I can write a namespace
                     * declaration safely.
                     * */
                    if (textType.CustomSerializer == CustomSerializerType.QName)
                    {
                        if (Writer.WriteState != WriteState.Element)
                        {
                            Debug.Assert(member != null && member.Fixup != null, "Attempting to serialize a XmlQualifiedName as text, but the LogicalType has no member or fixup");
                            MemberInfo textMember = ((MemberFixup)member.Fixup).m_member;
                            throw new InvalidOperationException(
                                SR.Format(SR.XmlIllegalTypedTextAttribute,
                                textMember.DeclaringType.Name,
                                textMember.Name,
                                typeof(XmlQualifiedName).FullName));
                        }

                        XmlQualifiedName qName = (XmlQualifiedName)value;
                        Debug.Assert(qName != null, "Attempting to serialize a null XmlQualifiedName as XmlText");

                        if (qName != XmlQualifiedName.Empty)
                            EnsureNamespaceDeclaration(qName.Namespace, null/*XmlSerializerNamespaces*/ );
                    }

                    WriteCustom(textType, value);
                    break;
                default:
                    Debug.WriteLine("Illegal text type " + textType.Type);
                    throw new InvalidOperationException(SR.Format(SR.XmlS_IllegalTextType_1, textType.Type));
            }
        }

        protected Accessor FindAccessorByChoiceMember(LogicalMemberValue member, object fetcherTarget)
        {
            return FindAccessorByChoiceMember(member, fetcherTarget, true);
        }

        protected Accessor FindAccessorByChoiceMember(LogicalMemberValue member, object fetcherTarget, bool throwOnMissingAccessor)
        {
            // There is a non-array choice member. Therefore, we resolve the accessor using
            // the enum value of the choice member. Array choice members are handled when 
            // serializing the individual elements of the member array.

            XmlChoiceSupport choice = member.Accessors.Choice;
            if (choice == null || choice.ChoiceMemberIsArray) return null;

            LogicalMemberValue choiceMember = choice.ChoiceMember;
            Fetcher choiceFetcher = choiceMember.Fetcher;

            if (choiceFetcher == null)
                throw new InvalidOperationException(SR.XmlS_IllegalChoiceDirection);

            object enumValue = choiceFetcher.fetch(fetcherTarget);
            Accessor accessor = choice.getAccessor(enumValue);

            if (accessor == null && throwOnMissingAccessor)
                throw new InvalidOperationException(SR.Format(SR.XmlS_UnknownEnum_1, enumValue));
            return accessor;
        }

        protected bool SkipMember(LogicalMemberValue member, Accessor accessor, object value)
        {
            // If the value is null and it is not nullable, or if the value
            // matches the default value (specified in a DefaultValueAttribute), 
            // then skip it.  Otherwise serialize it.

            if (value == null && !accessor.IsNullable)
            {
                return true;
            }

            if ((member.DefaultValue != null) && member.DefaultValue.Equals(value))
            {
                return true;
            }

            return false;
        }

        protected bool GetSpecifiedMemberValue(LogicalMemberValue member, object fetcherTarget)
        {
            // If there is a specified member and its value is set
            // to false, then we skip the serialization of this member

            LogicalMemberValue specified = member.SpecifiedMember;
            if (specified == null)
                return true;

            return (bool)specified.Fetcher.fetch(fetcherTarget);
        }

        protected void EnqueueElementMembersInOrder(MemberValueCollection members,
            SerializationQueue queue,
            object fetcherTarget)
        {
            Debug.Assert(members.IsOrdered, "Attempting to serialize an object's members in order but no order is specified.");
            foreach (LogicalMemberValue curMember in members.OrderMap)
            {
                if (!GetSpecifiedMemberValue(curMember, fetcherTarget))
                    continue;

                object value = curMember.Fetcher.fetch(fetcherTarget);
                // If there is a corresponding out argument, then set the default value to 
                // the current value of the in argument.                
                if (curMember.OutArgument != null) curMember.OutArgument.DefaultValue = value;

                Accessor accessor = FindAccessorByChoiceMember(curMember, fetcherTarget);
                if (accessor == null)
                {
                    // if value == m_args, then this is the synthetic wrapper
                    // structure, so use the default.                     
                    accessor = (value == null || value == this) ?
                        curMember.Accessors.Default :
                        curMember.Accessors.findAccessor(value.GetType());
                }

                if (!SkipMember(curMember, accessor, value))
                    queue.enqueueTask(members, curMember, accessor, value, fetcherTarget);
            }
        }

        protected void EnqueueAttributeMembers(MemberValueCollection members,
            SerializationQueue queue,
            object fetcherTarget,
            string parentElementNamespace)
        {
            for (int i = 0; i < members.count(); ++i)
            {
                LogicalMemberValue member = members.getMember(i);
                if (!GetSpecifiedMemberValue(member, fetcherTarget))
                    continue;

                object value = member.Fetcher.fetch(fetcherTarget);
                // If there is a corresponding out argument, then set the default value to 
                // the current value of the in argument.                
                if (member.OutArgument != null) member.OutArgument.DefaultValue = value;

                Accessor accessor = FindAccessorByChoiceMember(member, fetcherTarget);
                if (accessor == null)
                {
                    // if value == m_args, then this is the synthetic wrapper
                    // structure, so use the default.                     
                    accessor = (value == null || value == this) ?
                        member.Accessors.Default :
                        member.Accessors.findAccessor(value.GetType());
                }

                if (!SkipMember(member, accessor, value) && accessor.IsAttribute)
                    queue.enqueueTask(members, member, accessor, value, fetcherTarget, parentElementNamespace);
            }
        }

        protected void EnqueueMembers(MemberValueCollection members,
            SerializationQueue attributes,
            SerializationQueue elements,
            object fetcherTarget,
            string parentElementNamespace)
        {
            for (int i = 0; i < members.count(); ++i)
            {
                EnqueueMember(members, members.getMember(i), attributes, elements, fetcherTarget, parentElementNamespace);
            }
        }

        protected void EnqueueMember(MemberValueCollection members, LogicalMemberValue member,
            SerializationQueue attributes,
            SerializationQueue elements,
            object fetcherTarget,
            string parentElementNamespace)
        {
            if (!GetSpecifiedMemberValue(member, fetcherTarget))
                return;

            object value = member.Fetcher.fetch(fetcherTarget);
            // If there is a corresponding out argument, then set the default value to 
            // the current value of the in argument.                
            if (member.OutArgument != null) member.OutArgument.DefaultValue = value;

            Accessor accessor = FindAccessorByChoiceMember(member, fetcherTarget);
            if (accessor == null)
            {
                // if value == m_args, then this is the synthetic wrapper
                // structure, so use the default.                     
                accessor = (value == null || value == this) ?
                    member.Accessors.Default :
                    member.Accessors.findAccessor(value.GetType());
            }

            if (!SkipMember(member, accessor, value))
            {
                if (accessor.IsAttribute)
                    attributes.enqueueTask(members, member, accessor, value, fetcherTarget, parentElementNamespace);
                else
                    elements.enqueueTask(members, member, accessor, value, fetcherTarget);
            }
        }

        //***************************************************************
        // Array Serialization
        //***************************************************************


        // Because there are three different types that are all logically
        // arrays, I need to have a generic serialization mechanism.  The
        // for_each function iterates over the elements of the array, and these
        // ElementSerializer strategy objects properly serialize each element.
        internal abstract class ElementSerializer
        {
            protected XmlSerializationWriter m_formatter;
            protected XmlSerializationReflector m_reflector;

            internal ElementSerializer(XmlSerializationWriter formatter)
            {
                Debug.Assert(formatter != null, "Creating an ElementSerializer using a null XmlSerializationWriter");
                m_formatter = formatter;
                m_reflector = formatter.Reflector;
            }

            internal abstract void serialize(object elt, XmlSerializerNamespaces serializerNs);

            internal LogicalType FindByType(Type type)
            {
                return m_reflector.FindTypeInNamespaces(type, m_formatter.Encoded, m_reflector.DefaultNamespace);
            }

            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            internal static ElementSerializer CreateElementSerializer(XmlSerializationWriter formatter, Accessor eltAccessor, SerializationQueue delayedWork)
            {
                return new EncodedElementSerializer(formatter, eltAccessor, delayedWork);
            }

            internal static ElementSerializer CreateElementSerializer(XmlSerializationWriter formatter, Accessor arrayAccessor, AccessorCollection eltAccessors, LiteralElementSerializerType serType)
            {
                return CreateElementSerializer(formatter, arrayAccessor, eltAccessors, null, null, serType);
            }

            internal static ElementSerializer CreateElementSerializer(
                XmlSerializationWriter formatter, Accessor arrayAccessor,
                AccessorCollection eltAccessors, XmlChoiceSupport choice,
                object choiceFetcherTarget, LiteralElementSerializerType serType)
            {
                IChoiceAccessorFetcher choiceAccessorFetcher = (choice != null && choiceFetcherTarget != null) ?
                    new ChoiceArrayAccessorFetcher(choice, choiceFetcherTarget) :
                    null;

                switch (serType)
                {
                    case LiteralElementSerializerType.Text:
                        return new LiteralTextElementSerializer(formatter, eltAccessors);
                    case LiteralElementSerializerType.AnyElement:
                        return new LiteralAnyElementSerializer(formatter, eltAccessors, choiceAccessorFetcher);
                    default:
                        return new LiteralElementSerializer(formatter, arrayAccessor, eltAccessors, choiceAccessorFetcher);
                }
            }
        }

        internal enum LiteralElementSerializerType
        {
            Default,
            Text,
            AnyElement
        }

        internal class LiteralAnyElementSerializer : ElementSerializer
        {
            private AccessorCollection _eltAccessors;
            private IChoiceAccessorFetcher _choiceAccessorFetcher;


            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            internal LiteralAnyElementSerializer(XmlSerializationWriter formatter, AccessorCollection eltAccessors) :
                this(formatter, eltAccessors, null)
            {
            }

            internal LiteralAnyElementSerializer(XmlSerializationWriter formatter, AccessorCollection eltAccessors, IChoiceAccessorFetcher choiceFetcher) :
                base(formatter)
            {
                _eltAccessors = eltAccessors;
                _choiceAccessorFetcher = choiceFetcher;
            }

            internal Accessor FindElementAccessor(object elt)
            {
                if (_eltAccessors == null)
                    return null;

                Accessor eltAccessor = _eltAccessors.findAccessorNoThrow(elt.GetType());
                if (eltAccessor != null &&
                    (eltAccessor.Type.Serializer == SerializerType.Primitive ||
                    eltAccessor.Type.Serializer == SerializerType.Custom ||
                    eltAccessor.Type.CustomSerializer == CustomSerializerType.Object) &&
                    eltAccessor.Namespace == Soap.XsdUrl)
                {
                    eltAccessor = eltAccessor.copy();
                    eltAccessor.Namespace = null;
                }

                return eltAccessor;
            }

            internal Accessor GetChoiceAccessor()
            {
                if (_choiceAccessorFetcher == null)
                    return null;

                return _choiceAccessorFetcher.GetNextAccessor();
            }

            internal void WriteAnyNode(object elt)
            {
#if !FEATURE_LEGACYNETCF
                XmlNode node = elt as XmlNode;
                if (node != null) {
                    m_formatter.WriteTo(node);
                    return;
                }
#endif

                throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, elt.GetType().FullName));
            }

            internal override void serialize(object elt, XmlSerializerNamespaces serializerNs)
            {
                if (elt == null) return;

                Accessor eltAccessor = null;
                if (_choiceAccessorFetcher != null)
                {
                    if (_choiceAccessorFetcher.NextAccessorIsAnyAccessor())
                    {
                        WriteAnyNode(elt);
                        return;
                    }
                    eltAccessor = GetChoiceAccessor();
                }
                else if (_eltAccessors != null)
                {
                    eltAccessor = FindElementAccessor(elt);
                }

                if (eltAccessor == null)
                {
                    WriteAnyNode(elt);
                    return;
                }

                bool writeXsiType = elt.GetType() != eltAccessor.Type.Type;
                m_formatter.SerializeAsElement(eltAccessor, elt, null, null, true, writeXsiType, serializerNs);
            }
        }

        internal class LiteralElementSerializer : ElementSerializer
        {
            private Accessor _arrayAccessor;
            private AccessorCollection _eltAccessors;
            private IChoiceAccessorFetcher _choiceAccessorFetcher;
            private string _elementNamespace;


            [System.Security.FrameworkVisibilityCompactFrameworkInternal]
            internal LiteralElementSerializer(XmlSerializationWriter formatter, Accessor arrayAccessor, AccessorCollection eltAccessors) :
                this(formatter, arrayAccessor, eltAccessors, null)
            {
            }

            internal LiteralElementSerializer(XmlSerializationWriter formatter, Accessor arrayAccessor, AccessorCollection eltAccessors, IChoiceAccessorFetcher choiceFetcher)
                : base(formatter)
            {
                _arrayAccessor = arrayAccessor;
                _eltAccessors = eltAccessors;
                _choiceAccessorFetcher = choiceFetcher;
                _elementNamespace = arrayAccessor != null ? arrayAccessor.Namespace : null;
                if (true /* AppDomain.CompatVersion >= AppDomain.Orcas */)
                {
                    if (_arrayAccessor != null && _arrayAccessor.Type.Type == typeof(object))
                        _elementNamespace = formatter.Reflector.DefaultNamespace;
                }
            }

            internal Accessor FindElementAccessor(object elt)
            {
                if (_eltAccessors == null)
                    return null;

                Accessor eltAccessor = _eltAccessors.findAccessorNoThrow(elt.GetType());
                if (eltAccessor != null)
                {
                    if ((eltAccessor.Type.Serializer == SerializerType.Primitive ||
                        eltAccessor.Type.Serializer == SerializerType.Custom ||
                        eltAccessor.Type.CustomSerializer == CustomSerializerType.Object) &&
                        eltAccessor.Namespace == Soap.XsdUrl &&
                        false /* AppDomain.CompatVersion < AppDomain.Orcas */)
                    {
                        eltAccessor = eltAccessor.copy();
                        eltAccessor.Namespace = null;
                    }
                    else if (!eltAccessor.IsXmlArrayItem)
                    {
                        eltAccessor = eltAccessor.copy();
                        eltAccessor.Namespace = _elementNamespace;
                    }
                }

                return eltAccessor;
            }

            internal Accessor GetChoiceAccessor()
            {
                if (_choiceAccessorFetcher == null)
                    return null;

                return _choiceAccessorFetcher.GetNextAccessor();
            }

            internal override void serialize(object elt, XmlSerializerNamespaces serializerNs)
            {
                // PERFPERF: For all sealed types, and some other types, I don't
                // need to do this (the findByType) every round.    
                Accessor eltAccessor;
                if (_choiceAccessorFetcher != null)
                {
                    // Check for an accessor pointed to by a choice identifier
                    eltAccessor = GetChoiceAccessor();
                }
                else if (_eltAccessors == null)
                {
                    // Use the generic object accessor if the the AccessorCollection is null
                    LogicalType eltType = FindByType(typeof(object));
                    eltAccessor = eltType.TypeAccessor.copy();
                    eltAccessor.Namespace = null;
                }
                else if (elt == null)
                {
                    // If the element to serialize is null then use the default accessor
                    eltAccessor = _eltAccessors.Default;
                }
                else
                {
                    // If neither the element nor the accessor collection is null then look up 
                    // the accessor in the accessor collectoion using the type of the element.                    
                    eltAccessor = FindElementAccessor(elt);
                    if (eltAccessor == null)
                        throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, elt.GetType()));
                }

                bool writeXsiType = false;
                if (eltAccessor == null)
                {
                    writeXsiType = true;
                }
                else if (elt != null)
                {
                    writeXsiType = !eltAccessor.Type.IsNullableType ?
                        elt.GetType() != eltAccessor.Type.Type :
                        elt.GetType() != eltAccessor.Type.NullableValueType.Type;
                }

                // Non-nullable elements that are null should be skipped.
                if (elt != null || eltAccessor.IsNullable)
                {
                    m_formatter.SerializeAsElement(eltAccessor, elt, null, null, true, writeXsiType, serializerNs);
                }
            }
        }

        internal abstract class IChoiceAccessorFetcher
        {
            public abstract Accessor GetNextAccessor();
            public abstract bool NextAccessorIsAnyAccessor();
        }

        internal class ChoiceArrayAccessorFetcher : IChoiceAccessorFetcher
        {
            private int _index;
            private object _choiceFetcherTarget;
            private XmlChoiceSupport _choice;

            internal ChoiceArrayAccessorFetcher(XmlChoiceSupport choice, object choiceFetcherTarget)
            {
                _index = -1;
                _choice = choice;
                _choiceFetcherTarget = choiceFetcherTarget;
            }

            public override bool NextAccessorIsAnyAccessor()
            {
                if (_choice == null) return false;

                int nextNdx = _index + 1;
                LogicalMemberValue choiceMember = _choice.ChoiceMember;
                Fetcher choiceFetcher = choiceMember.Fetcher;

                if (choiceFetcher == null)
                    throw new InvalidOperationException(SR.XmlS_IllegalChoiceDirection);

                Array enumArray = choiceFetcher.fetch(_choiceFetcherTarget) as Array;
                object enumValue = enumArray.GetValue(nextNdx);

                Accessor anyChoiceAccessor = _choice.getAccessor(enumValue);
                if (anyChoiceAccessor != null && anyChoiceAccessor.IsXmlAnyElement)
                {
                    ++_index;
                    return true;
                }

                return false;
            }

            public override Accessor GetNextAccessor()
            {
                if (_choice == null) return null;

                ++_index;
                LogicalMemberValue choiceMember = _choice.ChoiceMember;
                Fetcher choiceFetcher = choiceMember.Fetcher;

                if (choiceFetcher == null)
                    throw new InvalidOperationException(SR.XmlS_IllegalChoiceDirection);

                Array enumArray = choiceFetcher.fetch(_choiceFetcherTarget) as Array;
                object enumValue = enumArray.GetValue(_index);

                Accessor accessor = _choice.getAccessor(enumValue);
                if (accessor == null)
                    throw new InvalidOperationException(SR.Format(SR.XmlS_UnknownEnum_1, enumValue));
                return accessor;
            }
        }

        internal class LiteralTextElementSerializer : ElementSerializer
        {
            private static LogicalType s_logicalStringType;
            private static LogicalType s_logicalXmlNodeType;

            private AccessorCollection _textEltAccessors;

            internal LiteralTextElementSerializer(XmlSerializationWriter formatter, AccessorCollection textEltAccessors)
                : base(formatter)
            {
                _textEltAccessors = textEltAccessors;
            }

            private LogicalType LogicalStringType
            {
                get
                {
                    if (s_logicalStringType == null)
                        s_logicalStringType = FindByType(typeof(string));
                    return s_logicalStringType;
                }
            }

            private LogicalType LogicalXmlNodeType
            {
                get
                {
                    if (s_logicalXmlNodeType == null)
                        s_logicalXmlNodeType = FindByType(typeof(XmlNode));
                    return s_logicalXmlNodeType;
                }
            }

            internal Accessor FindTextElementAccessor(object elt)
            {
                if (_textEltAccessors == null)
                    return null;

                Accessor eltAccessor = _textEltAccessors.findAccessorNoThrow(elt.GetType());
                if (eltAccessor != null &&
                   (eltAccessor.Type.Serializer == SerializerType.Primitive ||
                    eltAccessor.Type.Serializer == SerializerType.Custom ||
                    eltAccessor.Type.CustomSerializer == CustomSerializerType.Object) &&
                    eltAccessor.Namespace == Soap.XsdUrl)
                {
                    eltAccessor = eltAccessor.copy();
                    eltAccessor.Namespace = null;
                }

                return eltAccessor;
            }

            internal override void serialize(object textElt, XmlSerializerNamespaces serializerNs)
            {
                if (textElt == null) return;

                // If the element is a string then write the element string
                if (textElt is string)
                {
                    m_formatter.doValueSerialization(null, LogicalStringType, textElt);
                    return;
                }

                // If the element is an XmlNode then write the element a node 
                if (typeof(XmlNode).IsAssignableFrom(textElt.GetType()))
                {
                    m_formatter.doValueSerialization(null, LogicalXmlNodeType, textElt);
                    return;
                }

                // If the element is node a string or an XmlNode then we have to look it up 
                // in the list of accessors that were built while reflecting the type. This
                // list of accessors are built when reflecting over the member. 
                Accessor textEltAccessor = FindTextElementAccessor(textElt);
                if (textEltAccessor == null)
                    throw new InvalidOperationException(SR.Format(SR.XmlInvalidUseOfType, textElt.GetType().FullName));

                // We write the XSI type on text element objects as well.                       
                bool writeXsiType = !textEltAccessor.Type.IsNullableType ?
                    textElt.GetType() != textEltAccessor.Type.Type :
                    textElt.GetType() != textEltAccessor.Type.NullableValueType.Type;
                m_formatter.SerializeAsElement(textEltAccessor, textElt, null, null, true, writeXsiType, serializerNs);
            }
        }

        internal class EncodedElementSerializer : ElementSerializer
        {
            private Accessor _eltAccessor;
            private SerializationQueue _delayedWork;

            internal EncodedElementSerializer(XmlSerializationWriter formatter, Accessor eltAccessor, SerializationQueue delayedWork)
                : base(formatter)
            {
                _eltAccessor = eltAccessor;
                _eltAccessor.IsNullable = true;
                _delayedWork = delayedWork;
            }

            internal override void serialize(object elt, XmlSerializerNamespaces serializerNs)
            {
                m_formatter.SerializeAsElement(_eltAccessor, elt, null, _delayedWork, true, false, serializerNs);
            }
        }

        //***************************************************************
        // Write Methods
        //***************************************************************	                       
        protected void WriteStartElement(string localName, string ns, XmlSerializerNamespaces serializerNS)
        {
            if (!m_Encoded)
            {
                WriteLiteralCompoundStartElement(localName, ns, serializerNS);
                return;
            }

            if (ns == null || ns.Length == 0)
            {
                Writer.WriteStartElement(localName, ns);
                WriteAndDeclareXmlSerializerNamespaces(serializerNS);
                return;
            }

            string prefix = LookupPrefix(ns, serializerNS);            // First, look for the prefix in the XmlSerializerNamespaces
            if (prefix == null) prefix = Writer.LookupPrefix(ns);     // If its not in the XmlSerializerNamespace, check if the prefix is defined in scope in the document              
            if (prefix == null) prefix = MakePrefix();                  // If its not in the XmlSerializerNamespace, then let's create a new one

            Writer.WriteStartElement(prefix, localName, ns);
            WriteAndDeclareXmlSerializerNamespaces(serializerNS);
        }

        //-----------------------------------------------------------------------
        // If the message is literal, and the namespace of this type is not the 
        // currently active default namespace, then declare it as a default 
        // namespace.
        //-----------------------------------------------------------------------
        protected void WriteLiteralCompoundStartElement(string name, string ns, XmlSerializerNamespaces serializerNS)
        {
            if (ns == null || ns.Length == 0)
            {
                Writer.WriteStartElement(name, ns);
                goto WRITE_AND_DECLARE_XML_SERIALIZER_NAMESPACES;
            }

            string prefix = LookupPrefix(ns, serializerNS);
            if (prefix != null)
            {
                // Prefix the literal with the prefix found in the namespace collection
                Writer.WriteStartElement(prefix, name, ns);
                goto WRITE_AND_DECLARE_XML_SERIALIZER_NAMESPACES;
            }

            string serializerDefaultNS;
            if (m_WriteSerializerNamespaces && ContainsDefaultNSDeclaration(serializerNS, out serializerDefaultNS))
            {
                // A prefix is need for the namespace because the a default 
                //declaration exists in the XmlSerializerNamespaces object
                if (ns.Equals(serializerDefaultNS))
                    Writer.WriteStartElement(name, ns);
                else
                    Writer.WriteStartElement(MakePrefix(), name, ns);

                goto WRITE_AND_DECLARE_XML_SERIALIZER_NAMESPACES;
            }

            Writer.WriteStartElement(name, ns);
            goto WRITE_AND_DECLARE_XML_SERIALIZER_NAMESPACES;

        WRITE_AND_DECLARE_XML_SERIALIZER_NAMESPACES:
            WriteAndDeclareXmlSerializerNamespaces(serializerNS);
        }

        protected void WriteAttributeString(string prefix, string localName, string ns, string value)
        {
            Writer.WriteAttributeString(prefix, localName, ns, value);
        }

        protected void WriteAttributeString(string localName, string ns, string value, XmlSerializerNamespaces serializerNs)
        {
            if (serializerNs == null || ns == null || ns.Length == 0)
            {
                Writer.WriteAttributeString(localName, ns, value);
                return;
            }

            string prefix = LookupPrefix(ns, serializerNs);
            if (prefix == null)
            {
                prefix = Writer.LookupPrefix(ns);
                if (prefix == null) prefix = MakePrefix();
            }
            else if (prefix != Writer.LookupPrefix(ns))
            {
                prefix = MakePrefix();
            }

            Writer.WriteAttributeString(prefix, localName, ns, value);
        }



#if !FEATURE_LEGACYNETCF
        protected void WriteTo(XmlNode node) {
            if (node != null)
                node.WriteTo(Writer);
        }
#endif

        protected void WriteStartAttribute(string prefix, string localName, string ns)
        {
            Writer.WriteStartAttribute(prefix, localName, ns);
        }

        protected void WriteStartAttribute(string localName, string ns, XmlSerializerNamespaces serializerNs)
        {
            if (serializerNs == null || ns == null || ns.Length == 0)
            {
                Writer.WriteStartAttribute(localName, ns);
                return;
            }

            string prefix = LookupPrefix(ns, serializerNs);
            if (prefix == null)
            {
                prefix = Writer.LookupPrefix(ns);
                if (prefix == null) prefix = MakePrefix();
            }
            else if (prefix != Writer.LookupPrefix(ns))
            {
                prefix = MakePrefix();
            }

            Writer.WriteStartAttribute(prefix, localName, ns);
        }

        protected void WriteNilAttribute()
        {
            Writer.WriteAttributeString("nil", Soap.XsiUrl, "true");
        }

        protected void WriteForwardReference(int id)
        {
            Writer.WriteAttributeString(RefName,
                (m_Soap12 ? Soap12.Encoding : null),
                (m_Soap12 ? "id" : "#id") + id);
        }

        protected void WriteObjectIdentifier(int id)
        {
            Writer.WriteAttributeString("id",
                (m_Soap12 ? Soap12.Encoding : null),
                "id" + id);
        }

        protected void WriteBase64(byte[] data)
        {
            int start = 0;
            int end = data.Length;
            int chunkSize = InDataLineSize;
            while (start < end)
            {
                //hoist the assignment, since it only gets changed once
                Debug.Assert(chunkSize == InDataLineSize,
                    "optimization was wrong.");
                if (start + chunkSize > end)
                    chunkSize = end - start;
                Writer.WriteBase64(data, start, chunkSize);
                start += chunkSize;
                if (start < end)
                    Writer.WriteRaw(s_crlf, 0, 2);
            }
        }

        protected void WriteCustom(LogicalType type, object value)
        {
            if (type.CustomSerializer == CustomSerializerType.Base64)
            {
                byte[] array = (byte[])value;
                WriteBase64(array);
            }
            else if (type.CustomSerializer == CustomSerializerType.Hex)
            {
                byte[] array = (byte[])value;
                Writer.WriteBinHex(array, 0, array.Length);
            }
            else if (type.CustomSerializer == CustomSerializerType.QName)
            {
                XmlQualifiedName qname = (XmlQualifiedName)value;
                string name = XmlConvert.EncodeLocalName(qname.Name);
                string prefix = LookupPrefix(qname.Namespace);

                if (prefix != null && prefix.Length > 0)
                    Writer.WriteString(prefix + ":" + name);
                else
                    Writer.WriteString(name);
            }
            else
            {
                Debug.WriteLine("Invalid custom type " + type.Type.FullName);
                throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, type.Type));
            }
        }

        /*
         * XXX
         * * Wed 05/15/2002
         * .NET Remoting REQUIRES that we send a length.
         * */
        protected void WriteArrayTypeAttribute(LogicalType arrayType, string length, XmlSerializerNamespaces serializerNs)
        {
            string ns = m_Encoded ? arrayType.TypeAccessor.EncodedNamespaceForArrays : arrayType.TypeAccessor.NestedAccessors.Default.Namespace;
            string name = arrayType.Name;

            EnsureNamespaceDeclaration(ns, serializerNs);
            string prefix = LookupPrefix(ns);

            if (m_Soap12)
            {
                // Array's with multiply dimensions serialize as itemType = "xsd:anyType",                
                if (arrayType.Type.HasElementType && arrayType.Type.GetElementType().HasElementType)
                {
                    EnsureNamespaceDeclaration(Soap.XsdUrl, serializerNs);
                    prefix = LookupPrefix(Soap.XsdUrl);
                    name = "anyType";
                }

                if (name.EndsWith("[]", StringComparison.Ordinal))
                {
                    name = name.TrimEnd("[]".ToCharArray());
                }

                Writer.WriteAttributeString("itemType", Soap12.Encoding, (prefix != null && prefix.Length > 0) ? prefix + ":" + name : name);
                Writer.WriteAttributeString("arraySize", Soap12.Encoding, length);
            }
            else
            {
                Debug.Assert(name[name.Length - 1] == ']', "Bad array type");

                string typeName = name.Substring(0, name.Length - 1) + length + "]";
                Writer.WriteAttributeString("arrayType", Soap.Encoding, (prefix != null && prefix.Length > 0) ? prefix + ":" + typeName : typeName);
            }
        }

        protected void WriteXsiTypeAttribute(LogicalType type, XmlSerializerNamespaces serializerNs)
        {
            string ns = m_Encoded ? type.Namespace : type.TypeAccessor.Namespace;
            string name = m_Encoded ? type.Name : type.TypeAccessor.Name;

            EnsureNamespaceDeclaration(ns, serializerNs);
            string prefix = LookupPrefix(ns);

            EnsureNamespaceDeclaration(Soap.XsiUrl, serializerNs);
            Writer.WriteStartAttribute("type", Soap.XsiUrl);
            string typeAtt = (prefix != null && prefix.Length > 0) ?
                prefix + ":" + name :
                name;

            Writer.WriteString(typeAtt);
            Writer.WriteEndAttribute();
        }

        //------------------------------------------------------------------
        // Ensures that a namespace is declared in the current scope.  If it
        // isn't current in scope , then a declaration is written.
        //------------------------------------------------------------------       
        protected string EnsureNamespaceDeclaration(string ns, XmlSerializerNamespaces serializerNs)
        {
            if (ns == null || ns.Length == 0)
                return null;

            string prefix = LookupPrefix(ns, serializerNs);
            if (prefix != null && prefix == Writer.LookupPrefix(ns))
                return null;

            if (Soap.XmlNamespace.Equals(ns))
                return Soap.XmlNamespace;

            if (prefix == null)
            {
                prefix = Writer.LookupPrefix(ns);
                if (prefix != null) return prefix;
                prefix = MakePrefix();
            }
            else if (prefix != Writer.LookupPrefix(ns))
            {
                prefix = MakePrefix();
            }

            Writer.WriteAttributeString("xmlns", prefix, Soap.Xmlns, ns);
            return prefix;
        }

        //--------------------------------------------------------------------
        // This method writes and declares the namespaces and prefixes 
        // contained in the XmlSerializerNamespaces object that is passed to 
        // the XmlSerializer. This is only on the root element of the 
        // object's XML.
        //--------------------------------------------------------------------
        protected bool WriteAndDeclareXmlSerializerNamespaces(XmlSerializerNamespaces ns)
        {
            bool defaultDefined = false;

            if (m_WriteSerializerNamespaces && ns != null)
            {
                XmlQualifiedName[] qNames = ns.ToArray();
                foreach (XmlQualifiedName qName in qNames)
                {
                    if (qName.Name != null && LookupPrefix(qName.Namespace) == null)
                    {
                        if (qName.Name.Length == 0 && qName.Namespace.Length == 0)
                            continue;

                        defaultDefined = qName.Name.Length == 0;
                        Writer.WriteAttributeString("xmlns", qName.Name, Soap.Xmlns, qName.Namespace);
                    }
                }
                m_WriteSerializerNamespaces = false;
            }

            return defaultDefined;
        }

        protected bool ContainsDefaultNSDeclaration(XmlSerializerNamespaces ns, out string declaredDefaultNS)
        {
            if (ns == null)
            {
                declaredDefaultNS = null;
                return false;
            }

            XmlQualifiedName[] qNames = ns.ToArray();
            foreach (XmlQualifiedName qName in qNames)
            {
                if ((qName.Name != null && qName.Name.Length == 0))
                    if (qName.Namespace != null && qName.Namespace.Length > 0)
                    {
                        declaredDefaultNS = qName.Namespace;
                        return true;
                    }
            }

            declaredDefaultNS = null;
            return false;
        }

        //--------------------------------------------------------------------
        // Writes the namespaces contained in the object's member that is 
        // marked with the XmlNamespaceDeclarationsAttribute.
        //--------------------------------------------------------------------
        protected void WriteNamespacesMember(LogicalMemberValue member, object fetchTarget, XmlSerializerNamespaces predefinedNamespaces)
        {
            XmlSerializerNamespaces namespaces = (XmlSerializerNamespaces)member.Fetcher.fetch(fetchTarget);

            if (namespaces != null)
            {
                XmlQualifiedName[] qNames = namespaces.ToArray();
                foreach (XmlQualifiedName qName in qNames)
                {
                    if (predefinedNamespaces != null)
                    {
                        // Check that this prefix isn't already used.
                        string oldNs = predefinedNamespaces.Namespaces[qName.Name] as string;
                        if (true /* AppDomain.CompatVersion > AppDomain.Whidbey */)
                        {
                            if (oldNs != null && oldNs != qName.Namespace)
                            {
                                throw new InvalidOperationException(SR.Format(SR.XmlDuplicateNs, qName.Name, qName.Namespace));
                            }
                        }
                    }
                    // Now print it if the namespace hasn't already been declared with another prefix.
                    if (qName.Name != null && LookupPrefix(qName.Namespace) == null)
                    {
                        if (qName.Name.Length == 0 && qName.Namespace.Length == 0)
                            continue;

                        Writer.WriteAttributeString("xmlns", qName.Name, Soap.Xmlns, qName.Namespace);
                    }
                }
            }
        }

        //--------------------------------------------------------------------
        // Finds the current prefix string mapped to the specified namespace. 
        // This method is a wrapper around the XmlWriter.LookupPrefix. The 
        // XmlWriter.LookupPrefix throws an exception if ns is null or the 
        // empty string. If the ns parameter is null or the empty string then
        // null is returned. 
        //--------------------------------------------------------------------
        protected string LookupPrefix(string ns)
        {
            if (ns == null || ns.Length == 0)
                return null;

            string prefix = Writer.LookupPrefix(ns);
            if (prefix != null) return prefix;
            if (Soap.XmlNamespace.Equals(ns)) return Soap.XmlPrefix;

            return null;
        }

        //--------------------------------------------------------------------
        // Finds the current prefix string mapped to the specified namespace. 
        // This method walks the specified XmlSerializerNamespaces collection
        // looking for the specified namespace. Null is returned if a prefix 
        // for the namespace does not exist in the collection. 
        //--------------------------------------------------------------------
        protected string LookupPrefix(string ns, XmlSerializerNamespaces xsns)
        {
            if (xsns != null)
            {
                XmlQualifiedName[] qNames = xsns.ToArray();
                foreach (XmlQualifiedName qName in qNames)
                {
                    if (qName.Namespace == ns && (qName.Name != null && qName.Name.Length > 0))
                        return qName.Name;
                }
            }
            return null;
        }

        //***************************************************************
        // Type Look Up
        //***************************************************************

        protected virtual LogicalType getTypeForSerialization(ref Accessor accessor, object value, out bool requiresTypeAttr)
        {
            //if the Runtime Type matches the accessor type, then just use the
            //type of the accessor.
            Type type = value.GetType();
            /*
             * XXX
             * * Thu 11/15/2001
             * If the type is null, then this is a synthetic type, so assume
             * it's a match.
             * */
            bool isTypesMatch = !accessor.Type.IsNullableType ?
                type.Equals(accessor.Type.Type) :
                type.Equals(accessor.Type.NullableValueType.Type);

            if (isTypesMatch || ((accessor.Type.MappingFlags & TypeMappingFlags.Synthetic) != 0))
            {
                requiresTypeAttr = false;
                return accessor.Type;
            }
            Debug.Assert((object)accessor.Type.Type != (object)null, "null type");

            //if the type isn't exactly the same, then look up the LogicalType
            //for the Runtime Type.

            // Q: What if the member is referencing a derived-type, which may have already been 
            //    reflected for the Reflector.DefaultNamespace, but there is an 
            //    XmlElementAttribute.Namespace on this member that should(??) change the namespace of
            //    this derived type?
            // A: Desktop doesn't do right thing (DevDiv Bugs 146755 or NetCF test sd031991a.exe) and assigns
            //    the wrong namespace to it.

            // For System.XmlSerializer.WSXmlRoot01.exe, it makes sense to only use Reflector.DefaultNamespace
            // because the accessor has the XmlRoot namespace of A instead of B's default namespace.
            // Ideally, if the Accessor's namespace is due to an Xml______Attribute.Namespace
            // affixed to it, it seems we should pick it up here as the namespace, 
            // but if it was just the default for this member, or if this is the root element, 
            // then we should stick with the Reflector.DefaultNamespace.
            // But to make this possible we'd need to have the type reflected under that namespace,
            // but there's no way to predict which derived types will be referenced from these members.
            LogicalType lType = m_Reflector.FindTypeInNamespacesNoThrow(type, m_Encoded, Reflector.DefaultNamespace, accessor.Namespace);

            // We didn't use the out type of the accessor, so it requires a type attribute, 
            // unless the value type and the declared type are array-like types.
            requiresTypeAttr = !(SerializationHelper.isLogicalArray(lType) && SerializationHelper.isLogicalArray(accessor.Type));

            // If the type is unknown and its an enum then use the underlying type
            if (lType == null && type.GetTypeInfo().IsEnum)
                lType = m_Reflector.FindTypeInNamespaces(Enum.GetUnderlyingType(type), m_Encoded, m_Reflector.DefaultNamespace, accessor.Namespace);
            //if I couldn't find the type, then error
            if (lType == null)
            {
                Debug.WriteLine("Couldn't find type " + value.GetType().FullName);
                throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, value.GetType()));
            }
            //I've found the type.  If the type is ambiguous (i.e. DateTime, or
            //String, or byte[]), and the actual type is exactly the same (i.e.
            //the LogicalType and type are byte[], rather than one being object
            //and the other byte[]) then use the type of the accessor.
            return lType;
        }

        //***************************************************************
        // Object Cache
        //***************************************************************

        //--------------------------------------------------------------------
        // This function prevents us from serializing a cycle in a literal
        // object graph.  A Cycle would cause inifinite recursion, and would be
        // A Bad Thing(TM).  This looks for an object in the cache.  If it
        // can't be found, it is added.
        //--------------------------------------------------------------------
        private void addObjectToCycle(object o)
        {
            Debug.Assert(o != null, "null object in cycle");
            if (_currentCycle.ContainsKey(o))
            {
                Debug.WriteLine("Serializing cycle in literal message.");
                throw new InvalidOperationException(SR.Format(SR.XmlS_CircularRef_1, o.GetType()));
            }
            _currentCycle[o] = null;
        }

        //--------------------------------------------------------------------
        // Related to the above function.  This removes the object from the
        // cycle cache.
        //--------------------------------------------------------------------
        private void removeObjectFromCycle(object o)
        {
            _currentCycle.Remove(o);
        }

        //***************************************************************
        // Forward Reference Cache
        //***************************************************************

        //--------------------------------------------------------------------
        // finds the id of an object in the forward ref cache. Returns 
        // NOT_IN_CACHE if it isn't found
        //--------------------------------------------------------------------
        private int lookInCache(object o)
        {
            object i = _forwardRefCache[o];
            if (i == null)
                return NOT_IN_CACHE;
            else
                return (int)i;
        }

        private int addToCache(object o)
        {
            Debug.Assert(!_forwardRefCache.ContainsKey(o), "Adding object twice");
            int id = _forwardRefID++;
            _forwardRefCache[o] = id;
            return id;
        }

        //***************************************************************
        // Helpers
        //***************************************************************

        protected void InitWriter(XmlWriter w)
        {
#if !FEATURE_LEGACYNETCF
            if (typeof(XmlTextWriter).IsAssignableFrom(w.GetType())) {
                XmlTextWriter xmlWriter = w as XmlTextWriter;

                if (xmlWriter.WriteState == WriteState.Start)
                    xmlWriter.Namespaces = true;

            }
#endif

            if (w.WriteState == WriteState.Start)
                w.WriteStartDocument();

            Writer = w;
        }

        //***************************************************************
        // Returns the length of the array as a string, or the empty 
        // string if it cannot be determined.  IEnumerables can't be 
        // determined.
        //***************************************************************
        private string getLength(object value, LogicalType type)
        {
            SerializerType sType = type.Serializer;
            if (SerializerType.Array == sType)
            {
                return ((Array)value).Length.ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (SerializerType.Collection == sType)
            {
                return ((ICollection)value).Count.ToString(NumberFormatInfo.InvariantInfo);
            }
            else if (SerializerType.Enumerable == sType)
            {
                //no way to determine length.
                return string.Empty;
            }
            else
            {
                Debug.Assert(false, "Bad serializer type");
                return String.Empty;
            }
        }

        private void for_each(object value, LogicalType type, ElementSerializer serializer, XmlSerializerNamespaces serializerNs)
        {
            if (value is Array)
            {
                Array array = value as Array;
                if (null == array) return;

                int count = array.Length;
                for (int i = 0; i < count; ++i)
                {
                    serializer.serialize(array.GetValue(i), serializerNs);
                }
            }
            else if (value is ArrayList)
            {
                // Special handler for ArrayList because the indexer is known. It would be 
                // a performance hit to interact with it through the ICollection interface. 

                ArrayList arrayList = value as ArrayList;
                if (null == arrayList) return;

                int count = arrayList.Count;
                for (int i = 0; i < count; ++i)
                {
                    serializer.serialize(arrayList[i], serializerNs);
                }
            }
            else if (value is ICollection)
            {
                Debug.Assert(type is LogicalCollection, "Attempting to serialize the elements of a ICollection, but its LogicalType is not a LogicalCollection");
                ICollection collection = value as ICollection;
                if (null == collection) return;

                PropertyInfo collectionIndexer = ((LogicalCollection)type).Indexer;
                object[] indexerArguments = new object[1];
                int count = collection.Count;

                for (int i = 0; i < count; ++i)
                {
                    indexerArguments[0] = i;
                    object elt = collectionIndexer.GetValue(collection, indexerArguments);
                    serializer.serialize(elt, serializerNs);
                }
            }
            else if (value is IEnumerable)
            {
                IEnumerable enumerable = value as IEnumerable;
                if (null == enumerable) return;

                foreach (object elt in enumerable)
                {
                    serializer.serialize(elt, serializerNs);
                }
            }
        }

        internal string MakePrefix()
        {
            return "q" + (m_namespaceNumber++);
        }
    }
}
