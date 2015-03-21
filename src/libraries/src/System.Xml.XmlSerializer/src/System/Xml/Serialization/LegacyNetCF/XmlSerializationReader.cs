// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

#pragma warning disable 0162
#pragma warning disable 0429

#if DEBUG
//#define EXTRA_TRACING

#endif
using System.Xml;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Extensions;

using Hashtable = System.Collections.InternalHashtable;

namespace System.Xml.Serialization.LegacyNetCF
{
    internal class XmlSerializationReader
    {
        protected static NullFixup G_nullFixup = new NullFixup();

        protected XmlSerializationReflector m_Reflector;
        protected XmlReader Reader;
        protected bool m_Soap12;
        protected bool m_Encoded;
        protected List<Fixup> m_fixups = new List<Fixup>();
        protected Hashtable m_idCache = new Hashtable();
        protected ArrayLikeFixup m_headArrayLikeFixup = null;
#if !FEATURE_LEGACYNETCF
        protected XmlDocument m_Document = null;
#endif
        private XmlDeserializationEvents _events;

        //********************************************************
        // Constructors
        //********************************************************

        //-----------------------------------------------------------------        
        //
        //-----------------------------------------------------------------        

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public XmlSerializationReader(XmlReader xr) :
            this(xr, new XmlDeserializationEvents(), new XmlSerializationReflector(), false, false)
        {
        }

        //-----------------------------------------------------------------        
        //
        //-----------------------------------------------------------------        

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public XmlSerializationReader(XmlReader xr, bool soap12) :
            this(xr, new XmlDeserializationEvents(), new XmlSerializationReflector(), soap12, false)
        {
        }

        //-----------------------------------------------------------------        
        //
        //-----------------------------------------------------------------        

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        public XmlSerializationReader(XmlReader xr, XmlSerializationReflector reflector, bool soap12) :
            this(xr, new XmlDeserializationEvents(), reflector, soap12, false)
        {
        }

        //-----------------------------------------------------------------        
        //
        //-----------------------------------------------------------------        
        internal XmlSerializationReader(XmlReader xr, XmlDeserializationEvents e, XmlSerializationReflector reflector, bool soap12, bool encoded)
        {
            Debug.Assert(null != xr, "Passing a null reader to the XmlSerializationReader");
            Debug.Assert(xr.ReadState != ReadState.Closed, "XmlSerializationReader can not accept a closed reader");
            Debug.Assert(null != reflector, "Passing a null XmlSerializationReflector to the XmlSerializationReader");

            m_Reflector = reflector;
            m_Soap12 = soap12;
            m_Encoded = encoded;
            _events = e;

#if !FEATURE_LEGACYNETCF
            XmlTextReader xmlReader = xr as XmlTextReader;
            if( xmlReader != null && xmlReader.ReadState == ReadState.Initial)
                xmlReader.Namespaces = true;	                            
#endif

            Reader = xr;
        }

        //********************************************************
        // Properties
        //********************************************************

        //-----------------------------------------------------------------        
        //
        //-----------------------------------------------------------------        
        protected string RefName
        {
            get { return m_Soap12 ? "ref" : "href"; }
        }

        //-----------------------------------------------------------------        
        //wrapper functions around some reader operations
        //-----------------------------------------------------------------        
        protected string NodeLocalName
        {
            get
            {
                //get a DECODED version of the name
                return XmlConvert.DecodeName(Reader.LocalName);
            }
        }

        //-----------------------------------------------------------------        
        // Return the NamespaceURI of the element. If the namespace
        // is string.Empty, then the element belongs to the default 
        // namespace. This method should be used when the 
        //-----------------------------------------------------------------        
        protected string NodeNamespace
        {
            get
            {
                string ret = Reader.NamespaceURI;
                return (ret.Length == 0) ?
                    Reader.LookupNamespace(string.Empty) :
                    ret;
            }
        }

        protected string AttributeNamespace
        {
            get
            {
                return Reader.NamespaceURI;
            }
        }

#if !FEATURE_LEGACYNETCF
        //-----------------------------------------------------------------        
        // XmlDocument used to create XmlNode subclasses. This accessor is
        // lazy created and cached so it is only created once and only when
        // needed.
        //-----------------------------------------------------------------        
        protected XmlDocument Document
        {
            get{
                if( m_Document == null ) {
                    m_Document = new XmlDocument();
                    m_Document.SetBaseURI(Reader.BaseURI);
                }
                return m_Document;
            }
        }
#endif

        //********************************************************
        // Deserialization
        //********************************************************

        //----------------------------------------------------------------------------------------------
        // This method should be overridden by a SerializationReader that can handle headers. The 
        // override should use determine whether the MemberValueCollection parameter represents headers.
        //----------------------------------------------------------------------------------------------
        protected virtual bool InHeaders(MemberValueCollection members)
        {
            return false;
        }

        private void StartDeserializingMembers(MemberValueCollection members)
        {
            members.PushDeserializationContext();
        }

        //----------------------------------------------------------------------------------------------
        // Look through the MemberValueCollection and make sure I deserialized everything.
        //---------------------------------------------------------------------------------------------- 
        protected virtual void FinishDeserializingMembers(MemberValueCollection members, object fixupTarget, Hashtable internalState)
        {
            LogicalMemberValue member;
            int countMembers = members.count();

            members.PopDeserializationContext();
            for (int i = 0; i < countMembers; ++i)
            {
                member = members.getMember(i);
                if (!member.CanWrite || internalState.ContainsKey(member))
                    continue;

                // This member didn't appear in the xml stream. If there is a default value, then
                // use it to initialize the member. Only set the property if the default value is
                // different from the member's value                  
                AssigningFixup assigningFixup = member.Fixup as AssigningFixup;
                object defaultValue = member.DefaultValue;
                object currentValue = (member.Fetcher != null) ? member.Fetcher.fetch(fixupTarget) :
                                        (assigningFixup != null && assigningFixup.CanGetValue) ? assigningFixup.GetValue(fixupTarget) :
                                        null;

                if (currentValue == null)
                {
                    if (defaultValue != null)
                        member.Fixup.fixup(fixupTarget, defaultValue);
                    else if (assigningFixup != null)
                        assigningFixup.FixupWithDefaultValueForType(fixupTarget);
                }

                //mark it as unspecified
                LogicalMemberValue specified = member.SpecifiedMember;
                if (specified != null)
                {
                    Debug.WriteLine("Marking " + member.Accessors.Default.Namespace + ":" + member.Accessors.Default.Name + " as not specified");
                    specified.Fixup.fixup(fixupTarget, false);
                }
            }

            // Handle the XmlAnyAttribute membet that has a default values but no any attribtues were found in the xml stream
            if (members.HasXmlAny && members.XmlAny.XmlAnyAttribute != null)
            {
                if (members.XmlAny.XmlAnyAttribute.DefaultValue != null)
                {
                    if (members.XmlAny.XmlAnyAttribute.Fetcher != null && members.XmlAny.XmlAnyAttribute.Fetcher.fetch(fixupTarget) == null)
                    {
                        members.XmlAny.XmlAnyAttribute.Fixup.fixup(fixupTarget, members.XmlAny.XmlAnyAttribute.DefaultValue);
                    }
                }
            }
        }

        //----------------------------------------------------------------------------------------
        // Get the XmlSerializerNamespaces member in the MemberValueCollection. If an XmlDeclaration
        // appears on the element being deserialized, then the declaration is stored in the member
        // returned from this method
        //----------------------------------------------------------------------------------------
        private XmlSerializerNamespaces GetXmlSerializerNamespacesMember(MemberValueCollection members, object fixupTarget)
        {
            if (members == null || fixupTarget == null)
                return null;

            XmlSerializerNamespaces toInitialize = null;
            if (members.XmlNamespaceDecls != null)
            {
                LogicalMemberValue decls = members.XmlNamespaceDecls;
                toInitialize = new XmlSerializerNamespaces();
                decls.Fixup.fixup(fixupTarget, toInitialize);
            }

            return toInitialize;
        }

        //----------------------------------------------------------------------------------------
        // Walks through the attributes of the current element and deserializes an members that 
        // where serialized as attributes. This method also stores an XmlDeclarations in the 
        // XmlSerializerNamespaces parameter. If this parameter is null then now declarations are
        // stored. When this method completes the Reader is left on the on the node that owns the
        // attributes.
        //----------------------------------------------------------------------------------------
        private void DeserializeAttributeMembers(MemberValueCollection members, object fixupTarget,
                                                    Hashtable internalState, XmlSerializerNamespaces toInitialize)
        {
            Debug.Assert(Reader.NodeType == XmlNodeType.Element, "DeserializeAttributeMembers requires that the XmlReader be positioned on a Element node");

            string attributeLocalName, attributeNamespace, parentElementNamespace;
            LogicalMemberValue member;
            Accessor accessor;
            bool success;

            // Grab the namespace of the parent element before moving through the attributes.
            // This namespace will be used when attempting to map the attribute to the correct
            // member. The problem is that the desktop considers attributes to be in the parent
            // elements namespace. This is a direct violation of the XML specification. But since
            // the desktop does this then we need to respect it.            
            parentElementNamespace = NodeNamespace;

            if (Reader.MoveToFirstAttribute())
            {
                do
                {
                    attributeLocalName = NodeLocalName;
                    attributeNamespace = AttributeNamespace;

                    // Namespace declarations are stored in the XmlSerializerNamespace property, if present.
                    // If no XmlSerializerNamespace property is present then we ignore the namespace
                    // declaration. We do not fire an unknown attribute event.                    
                    if (Soap.Xmlns == attributeNamespace)
                    {
                        if (toInitialize == null) continue;

                        if (Reader.LocalName.Equals("xmlns"))   // xmlns="http://namespaceUri"
                            toInitialize.Add(string.Empty, Reader.Value);
                        else                                     // xmlns:prefix="http://namespaceUri"
                            toInitialize.Add(Reader.LocalName, Reader.Value);
                        continue;
                    }

                    // Skip over soap ID/REF attributes
                    if (IsSoapIDREFAttribute(attributeLocalName, attributeNamespace, m_Encoded, m_Soap12))
                        continue;

                    // Skip over type attributes
                    if (IsTypeAttribute(attributeLocalName, attributeNamespace, m_Encoded, m_Soap12))
                        continue;

                    success = doLookup(members, attributeLocalName, attributeNamespace, internalState, Reader, true, m_Encoded, out member, out accessor);

                    // The desktop assumes that the attribute's namespace can be the namespace of its parent 
                    // element. This goes against the XML 1.1 spec (section 5.2 Namespace Defaulting) which
                    // states that "...default namespaces do not apply directly to attribtues".  However, 
                    // interoperation must be exist between the .NetFX and .NetCF
                    if (!success)
                        success = doLookup(members, attributeLocalName, parentElementNamespace, internalState, Reader, true, m_Encoded, out member, out accessor);

                    if (success)
                    {
                        Debug.Assert(member != null, "doLookup() returned true but the LogicalMemberValue was not initialized");
                        Debug.Assert(accessor != null, "doLookup() returned true but the Accessor was not initialized");

                        if (!member.CanWrite)
                            continue;

                        if (!accessor.IsAttribute)
                        {
                            Debug.WriteLine("Deserializing element as attribute");
                            throw new InvalidOperationException(SR.Format(SR.XmlS_ElementAsAttribute_2, attributeNamespace, attributeLocalName));
                        }

                        object value = deserializeAttribute(accessor);
                        member.Fixup.fixup(fixupTarget, value);

                        XmlChoiceSupport choice = member.Accessors.Choice;
                        if (choice != null)
                            SetChoiceMemberValue(member.Accessors.Choice, accessor, fixupTarget);

                        LogicalMemberValue specified = member.SpecifiedMember;
                        if (specified != null)
                            SetSpecifiedMemberValue(specified, fixupTarget);
                    }
                    else
                    {
                        handleUnknownAttribute(attributeLocalName, attributeNamespace, fixupTarget, members);
                    }
                } while (Reader.MoveToNextAttribute());
                Reader.MoveToContent();
            }
        }

        //----------------------------------------------------------------------------------------
        // This method looks for attributes are soap ID or soap REF attributes. If the attribute is 
        // one of these attributes then it should not be deserialized. 
        //
        // attributeName        = the local name of the attribute
        // attributeNamespace   = the namespace of the attribute
        // isEncoded            = specifies whether the document is soap encoded
        // isSoap12             = specified whether the document uses soap 12 encoding
        //
        // Returns true is the attribute is a SOAP ID/REF attribute. Returns false otherwise. 
        //----------------------------------------------------------------------------------------
        private bool IsSoapIDREFAttribute(string attributeName, string attributeNamespace, bool isEncoded, bool isSoap12)
        {
            const bool IS_SOAP_ID_ATTRIBUTE = true;

            // There are no SOAP:ID attributes in literal encoding
            if (!isEncoded)
                return !IS_SOAP_ID_ATTRIBUTE;

            if (isSoap12 && attributeNamespace == Soap12.Encoding &&
                (attributeName.Equals("id") || attributeName.Equals(RefName)))
            {   // Soap 1.2 ID/REF Attribute
                return IS_SOAP_ID_ATTRIBUTE;
            }
            else if (isEncoded && !isSoap12 &&
              (attributeNamespace == null || attributeNamespace.Length == 0) &&
              (attributeName.Equals("id") || attributeName.Equals(RefName)))
            {   // Soap 1.1 ID/REF Attribute
                return IS_SOAP_ID_ATTRIBUTE;
            }

            return !IS_SOAP_ID_ATTRIBUTE;
        }

        //----------------------------------------------------------------------------------------
        // This method looks for attributes that resemble attributes that signify the type of the 
        // elements type. If the attribute is one of these type attributes then it should not be 
        // deserialized. These attributes are:
        //
        // xsi:Type="..."
        // SOAP-ENC:arrayType="..."
        // SOAP-ENC:arraySize="..."
        // SOAP-ENC:itemType="..."
        //
        // attributeName        = the local name of the attribute
        // attributeNamespace   = the namespace of the attribute
        // isEncoded            = specifies whether the document is soap encoded
        // isSoap12             = specified whether the document uses soap 12 encoding
        //
        // Returns true is the attribute is a type attribute. Returns false otherwise. 
        //----------------------------------------------------------------------------------------
        private bool IsTypeAttribute(string attributeName, string attributeNamespace, bool isEncoded, bool isSoap12)
        {
            const bool IS_TYPE_ATTRIBUTE = true;

            // The attribute must have a namespace inorder to be a type attribtue.
            if (attributeNamespace == null || attributeNamespace.Length == 0)
                return !IS_TYPE_ATTRIBUTE;

            //
            // xsi:Type
            //
            if (attributeName.Equals("type") && attributeNamespace.Equals(Soap.XsiUrl))
                return IS_TYPE_ATTRIBUTE;

            //
            // Encoded Type Attributes 
            //
            if (isEncoded)
            {
                if (!isSoap12)
                {
                    // Soap 1.1 Type Attributes

                    // SOAP-ENC:arrayType
                    if (attributeName.Equals("arrayType") && attributeNamespace.Equals(Soap.Encoding))
                        return IS_TYPE_ATTRIBUTE;
                }
                else
                {
                    // Soap 1.2 Type Attributes

                    // SOAP-ENC:arraySize
                    if (attributeName.Equals("arraySize") && attributeNamespace.Equals(Soap12.Encoding))
                        return IS_TYPE_ATTRIBUTE;

                    // SOAP-ENC:itemType
                    if (attributeName.Equals("itemType") && attributeNamespace.Equals(Soap12.Encoding))
                        return IS_TYPE_ATTRIBUTE;
                }
            }

            return !IS_TYPE_ATTRIBUTE;
        }

        //----------------------------------------------------------------------------------------
        // This method deserializes a single child text element as a member. The reader should be 
        // positioned on the text node to be deserialized. When this method completes the 
        // XmlReader is left on the next node adjacent to the current text node. In most 
        // cases this is either the start element of the next member or the end element of the 
        // object that contains these members. 
        //----------------------------------------------------------------------------------------
        private void DeserializeTextMember(MemberValueCollection members, object fixupTarget)
        {
            Debug.Assert(Reader.NodeType == XmlNodeType.Text || Reader.NodeType == XmlNodeType.CDATA, "DeserializeTextMember requires that the XmlReader be positioned on a Text node");

            if (members.XmlText != null)
            {
                deserializeText(members.XmlText, fixupTarget);
            }
            else
            {
                Debug.WriteLine("Skipping text node");
                UnknownNode(fixupTarget);
            }

            // Eat any whitespace left over after the text/CDATA node
            IgnoreWhitespace();
        }

        //----------------------------------------------------------------------------------------
        // This method deserializes a single child element as a member. The reader should be 
        // positioned on the start element of the object to be deserialized. When this method 
        // completes the Reader is left on the next node adjacent to the current node. In most 
        // cases this is either the start element of the next member or the end element of the 
        // object that contains these members
        //----------------------------------------------------------------------------------------
        private void DeserializeElementMember(MemberValueCollection members, object fixupTarget, Hashtable internalState, ref bool firstElement)
        {
            Debug.Assert(Reader.NodeType == XmlNodeType.Element, "DeserializeElementMember requires that the XmlReader be positioned on an Element node");

            bool success;
            XmlQualifiedName nodeQName = new XmlQualifiedName(NodeLocalName, NodeNamespace);
            LogicalMemberValue member;
            Accessor accessor;

            success = doLookup(members, nodeQName.Name, nodeQName.Namespace, internalState, Reader, false, m_Encoded, out member, out accessor);
            if (!success && firstElement && (!m_Soap12 && members.BoundFirstArgument != null))
            {
                success = true;
                member = members.BoundFirstArgument;
                internalState[member] = member;
                accessor = member.Accessors.Default;
            }

            firstElement = false;
            if (success)
            {
                Debug.Assert(member != null, "doLookup() returned true but the LogicalMemberValue was not initialized");
                Debug.Assert(accessor != null, "doLookup() returned true but the Accessor was not initialized");

                // Only de-serialize properties that have read access
                if (!member.CanRead)
                {
                    Reader.Skip();
                    IgnoreWhitespace(); // Eat any whitespace after the element
                    return;
                }

                if (!member.CanWrite)
                {
                    // Only readonly collections/enumerables can be de-serialized
                    if (!SerializationHelper.isCollection(member.Accessors.Default.Type.Type) &&
                        !SerializationHelper.isEnumerable(member.Accessors.Default.Type.Type))
                    {
                        Reader.Skip();
                        IgnoreWhitespace(); // Eat any whitespace after the element
                        return;
                    }
                }

                if (accessor.IsAttribute)
                {
                    Debug.WriteLine("Deserializing attribute as element");
                    throw new InvalidOperationException(SR.Format(SR.XmlS_AttributeAsElement_2, nodeQName.Namespace, nodeQName.Name));
                }

                deserializeElement(accessor, member.Fixup, fixupTarget);

                XmlChoiceSupport choice = member.Accessors.Choice;
                if (choice != null)
                    SetChoiceMemberValue(member.Accessors.Choice, accessor, fixupTarget);

                LogicalMemberValue specified = member.SpecifiedMember;
                if (specified != null)
                    SetSpecifiedMemberValue(specified, fixupTarget);
            }
            else
            {
                LogicalType lType;
                LogicalMemberValue ukAny = null;
                bool inHeaders = InHeaders(members);

                if (members.HasXmlAny)
                    ukAny = members.XmlAny.UnknownHeaders;

                if (!m_Encoded)
                {
                    if (!inHeaders)
                    {
                        handleUnknownElement(nodeQName, fixupTarget, members);
                    }
                    else if (ukAny == null)
                    {
                        skipSUkH();
                    }
                    else
                    {
                        saveSUkH(ukAny, fixupTarget);
                    }
                }
                else
                {
                    // Derive the type using type attributes on the xml element.                               
                    try
                    {
                        lType = deriveTypeFromAttributes(null);
                    }
                    catch (InvalidOperationException)
                    {
                        Debug.WriteLine("Unrecognized type for skiped element " + nodeQName);
                        lType = null;
                    }

                    // Deriving type from type attributes failed. Let's try element name and ns
                    if (lType == null)
                    {
                        lType = m_Reflector.FindType(nodeQName, m_Encoded);
                    }

                    // If we've found a type then let's use it to deserialize the element.
                    if (lType != null)
                    {
                        deserializeElement(lType.TypeAccessor, G_nullFixup, null);
                        return;
                    }

                    // We could not derive the type. This block decides how to ignore the element
                    if (!inHeaders)
                    {
                        handleUnknownElement(nodeQName, fixupTarget, members);
                    }
                    else if (ukAny == null)
                    {
                        skipSUkH();
                    }
                    else
                    {
                        saveSUkH(ukAny, fixupTarget);
                    }
                }
            }

            // Eat the whitespace after the member element
            IgnoreWhitespace();
        }

        internal void SetSpecifiedMemberValue(LogicalMemberValue specified, object fixupTarget)
        {
            Debug.Assert(specified != null, "Null Specified argument");
            specified.Fixup.fixup(fixupTarget, true);
        }

        internal void SetAnyElementChoiceMemberValue(XmlChoiceSupport choice, string name, string ns, object fixupTarget)
        {
            Debug.Assert(choice != null, "Null XmlChoiceSupport argument");

            object enumValue = choice.getValue(name, ns);
            if (enumValue != null)
            {
                LogicalMemberValue choiceMember = choice.ChoiceMember;
                if (choice.ChoiceMemberIsArray)
                {
                    Fixup choiceFixup = getFixupForCollection(choiceMember.Fixup, fixupTarget, choice.EnumArray, null/*identifier*/ );
                    object collectionNdx = getIndexForNextElement(choiceFixup);
                    choiceFixup.fixup(collectionNdx, enumValue);
                }
                else
                {
                    Fixup choiceFixup = choiceMember.Fixup;
                    if (choiceFixup == null)
                        throw new InvalidOperationException(SR.XmlS_IllegalChoiceDirection);
                    choiceFixup.fixup(fixupTarget, enumValue);
                }
            }
        }

        internal void SetChoiceMemberValue(XmlChoiceSupport choice, Accessor memberAccessor, object fixupTarget)
        {
            Debug.Assert(choice != null, "Null XmlChoiceSupport argument");
            object enumValue = choice.getValue(memberAccessor);
            LogicalMemberValue choiceMember = choice.ChoiceMember;

            if (choice.ChoiceMemberIsArray)
            {
                Fixup choiceFixup = getFixupForCollection(choiceMember.Fixup, fixupTarget, choice.EnumArray, null/*identifier*/ );
                object collectionNdx = getIndexForNextElement(choiceFixup);
                choiceFixup.fixup(collectionNdx, enumValue);
            }
            else
            {
                Fixup choiceFixup = choiceMember.Fixup;
                if (choiceFixup == null)
                    throw new InvalidOperationException(SR.XmlS_IllegalChoiceDirection);
                choiceFixup.fixup(fixupTarget, enumValue);
            }
        }

        //----------------------------------------------------------------------------------------
        // This method should be called by the XmlSerializer when it needs to deserialize an object 
        // the was serialized using Soap Section 5 Encoding. When deserializing an object that was 
        // serialized using literal encoding the DeserializeElement() method should be called.
        //----------------------------------------------------------------------------------------

        [System.Security.FrameworkVisibilityCompactFrameworkInternal]
        internal void DeserializeMembers(MemberValueCollection members, object fixupTarget)
        {
            deserializeMembers(members, fixupTarget);
        }

        //----------------------------------------------------------------------------------------
        // This serializes MemberValues at the current location.  It assumes
        // that the reader is at the "root" (i.e. object element, soap::Body 
        // or soap::Header) element. 
        //          
        protected void deserializeMembers(MemberValueCollection members, object fixupTarget)
        {
            // Internal look table of deserialized members 
            Hashtable internalState = new Hashtable();
            //This holds the namespaces declared on this node.
            XmlSerializerNamespaces toInitialize = GetXmlSerializerNamespacesMember(members, fixupTarget);
            // Set up the member value collection to be serialized            
            StartDeserializingMembers(members);

#if DEBUG
            string topLevelName = NodeLocalName;
            string topLevelNamespace = NodeNamespace;
#endif

            //Read through the attributes on this element.
            DeserializeAttributeMembers(members, fixupTarget, internalState, toInitialize);

            // Deserialize the child elements as members
            if (!Reader.IsEmptyElement)
            {
                Reader.ReadStartElement();  // Read the object's start element                
                IgnoreWhitespace();         // Eat whitespace before the first member element

                bool firstElement = true;
                while (Reader.NodeType != XmlNodeType.EndElement)
                {
                    switch (Reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            DeserializeElementMember(members, fixupTarget, internalState, ref firstElement);
                            break;
                        case XmlNodeType.Text:
                        case XmlNodeType.CDATA:
                            DeserializeTextMember(members, fixupTarget);
                            break;
                        default:
                            Debug.WriteLine("Found unknown node of type: " + Reader.NodeType);
                            UnknownNode(fixupTarget);
                            break;
                    }

                    IgnoreWhitespace(); // Eat the trailing whitespace after each member
                }
#if DEBUG
                Debug.Assert(NodeLocalName.Equals(topLevelName));
                Debug.Assert(NodeNamespace == topLevelNamespace);
#endif                       
                Reader.ReadEndElement();
            }
            else
            {
                Reader.ReadStartElement();
            }

            IgnoreWhitespace(); // Eat the trailing whitespace after the object's end element                                    
            FinishDeserializingMembers(members, fixupTarget, internalState);
        }

        //-----------------------------------------------------------------        
        // This method is called by the XmlSerializer. This method is used
        // to read all of the members for a given object. this method also
        // reads any reference elements that appear after the original 
        // object. 
        //-----------------------------------------------------------------        
        internal void DeserializeElement(MemberValueCollection members, object fixupTarget)
        {
            // Keeps an internal list of members
            Hashtable internalState = new Hashtable();
            // Get the XmlSerializerNamespacesMembers
            XmlSerializerNamespaces toInitialize = GetXmlSerializerNamespacesMember(members, fixupTarget);
            // Initialize the member value collection for deserialization
            StartDeserializingMembers(members);
            // Read through the attributes and desrialize them
            DeserializeAttributeMembers(members, fixupTarget, internalState, toInitialize);

            // Read through the child elements an any reference elements after the current element
            int depth = Reader.Depth;
            if (!Reader.IsEmptyElement)
            {
                Reader.ReadStartElement();
                IgnoreWhitespace(); // Eat the whitespace before the first member element

                bool firstElement = true;
                while (!Reader.EOF && Reader.Depth >= depth)
                {
                    if (Reader.NodeType == XmlNodeType.EndElement)
                    {
                        Reader.ReadEndElement();
                        IgnoreWhitespace(); // Eat the whitespace after the end element
                        continue;
                    }

                    // The DeserializeElementMember eats any whitespace after the element member                    
                    DeserializeElementMember(members, fixupTarget, internalState, ref firstElement);
                }
            }
            else
            {
                Reader.ReadStartElement();
            }

            IgnoreWhitespace(); // Eat whitespace after the objects element
            FinishDeserializingMembers(members, fixupTarget, internalState);
        }

        //-----------------------------------------------------------------        
        // This method should be overriden by serializers that can handle 
        // soap headers in the xml. The override should "skip" the unknown
        // header. In the XmlSerializationReader, this means completely 
        // ignoring the element. The reader is left on the element adjacent
        // to the unknown header. 
        //-----------------------------------------------------------------        
        protected virtual void skipSUkH()
        {
            Debug.WriteLine("The XmlSerialzationReader can not deserialize SoapHeader elements");
            Reader.Skip();
            IgnoreWhitespace(); // Eat whitespace after the unknown header
        }


        //-----------------------------------------------------------------        
        // This method should be overriden by serializers that can handle 
        // soap headers in the xml. The override should "save" the unknown 
        // header. In the XmlSerializationReader, this means completely 
        // ignoring the element. The reader is left on the element adjacent
        // to the unknown header.
        //-----------------------------------------------------------------        
        protected virtual void saveSUkH(LogicalMemberValue any, object fixupTarget)
        {
            Debug.WriteLine("The XmlSerializationReader can not deserialize SoapHeader elements");
            Reader.Skip();
            IgnoreWhitespace(); // Eat whitespace after the unknown header
        }

        //-----------------------------------------------------------------        
        // This should be called by the XmlSerializer. This is the main 
        // entry method that will deserialize an object instance. 
        //-----------------------------------------------------------------        
        internal void DeserializeElement(Accessor accessor, Fixup fixup)
        {
            deserializeElement(accessor, fixup, null);
            runFixups();
        }

        //-----------------------------------------------------------------        
        // Deserializes an object from the element. The Reader must be 
        // positioned on the root element corresponding to the object. The
        // Reader is left on the element after the root element's end element
        //-----------------------------------------------------------------        
        protected void deserializeElement(Accessor accessor, Fixup fixup, object fixupTarget)
        {
            string identifier;
            bool nullValue = false;
            bool emptyValue = false;

            if (!m_Encoded)
            {
                identifier = null;
            }
            else
            {
                string href = Reader.GetAttribute(RefName, m_Soap12 ? Soap12.Encoding : null);
                if (href != null)
                {
                    doHrefFixup(href, fixup, fixupTarget);
                    return;
                }

                identifier = Reader.GetAttribute("id", m_Soap12 ? Soap12.Encoding : null);
            }

            //check for null elements
            if (Reader.IsEmptyElement)
            {
                emptyValue = true;
                nullValue = isNull();
            }

            LogicalType deserializeAs;
            Debug.Assert(accessor != null, "null accessor.  anonymous parse?");

            // XXX
            // // Thu 02/28/2002
            // wsdl/AWR generate types that have the same name as the synthetic
            // response object.  So, it is possible that there are "two" types
            // in the system with the same name (the synthetic request type and
            // the actual C# type).  It is possible that some implementation
            // has stuck an xsi:type attribute on the wrapper element.  If we
            // see that, we'll deserialize the wrapper as the C# type, and not
            // our synthetic type.  That would be Bad(TM).  This code allows a
            // synthetic type to "override" all deserialization attributes.
            //
            //
            // XXX
            // // Tue 04/23/2002
            // If there weren't enough weird things going on here.  If the
            // current target is a repeated element array, the xsi:type
            // actually refers to the type of the element, and not the array.
            // If the accessor is a repeating element array, then ignore the
            // xsi:type.        
            if ((accessor.Type.MappingFlags & TypeMappingFlags.Synthetic) != 0)
            {
                deserializeAs = accessor.Type;
            }
            else if (accessor.AllowRepeats)
            {
                deserializeAs = accessor.Type;
            }
            else
            {
                deserializeAs = deriveTypeFromAttributes(accessor.Type);
                if (deserializeAs == null)
                    deserializeAs = accessor.Type;
                else if (accessor.Type.IsNullableType && deserializeAs.Type == accessor.Type.NullableValueType.Type)
                    deserializeAs = accessor.Type;
                else if (deserializeAs != accessor.Type)
                    accessor = deserializeAs.TypeAccessor;
            }

            if (deserializeAs == null)
            {
                object o = null;
                if (fixupTarget != null)
                {
                    o = fixupTarget;
                }
                else if (fixup != null && fixup is ObjectFixup)
                {
                    o = ((ObjectFixup)fixup).Object;
                }

                Debug.WriteLine("Could not find type to use for deserializing " + NodeNamespace + ":" + NodeLocalName);
                handleUnknownElement(new XmlQualifiedName(NodeLocalName, NodeNamespace), o, null);
                return;
            }

            switch (deserializeAs.Serializer)
            {
                case SerializerType.Primitive:
                    DeserializePrimitiveElement(deserializeAs, accessor, fixup, fixupTarget, identifier, emptyValue, nullValue);
                    break;
                case SerializerType.Enumerated:
                    DeserializeEnumeratedElement(deserializeAs, accessor, fixup, fixupTarget, identifier, emptyValue, nullValue);
                    break;
                //Collections and Enumerables are handled by the fixups
                case SerializerType.Array:
                case SerializerType.Collection:
                case SerializerType.Enumerable:
                    DeserializeArrayLikeElement(deserializeAs, accessor, fixup, fixupTarget, identifier, emptyValue, nullValue);
                    break;
                case SerializerType.Complex:
                    DeserializeComplexElement(deserializeAs, accessor, fixup, fixupTarget, identifier, emptyValue, nullValue);
                    break;
                case SerializerType.Serializable:
                    DeserializeSerializableElement(deserializeAs, accessor, fixup, fixupTarget, identifier, emptyValue, nullValue);
                    break;
                case SerializerType.Custom:
                    DeserializeCustomElement(deserializeAs, accessor, fixup, fixupTarget, identifier, emptyValue, nullValue);
                    break;
#if !FEATURE_LEGACYNETCF
                case SerializerType.XmlNode:
                    DeserializeXmlNode(deserializeAs, accessor, fixup, fixupTarget, identifier, emptyValue, nullValue);
                    break;
#endif
                case SerializerType.SoapFault:
                    DeserializeSoapFaultElement(deserializeAs, accessor, fixup, fixupTarget, identifier, emptyValue, nullValue);
                    break;
            }

            // Eat the whitespace after the object element.
            IgnoreWhitespace();
        }

        //-----------------------------------------------------------------        
        // This method should be overridden to customize how primitive 
        // values are deserialized from elements. This method leaves the 
        // Reader on the element after the primitive element.
        //-----------------------------------------------------------------        
        protected virtual void DeserializePrimitiveElement(LogicalType deserializeAs, Accessor accessor,
                                                            Fixup fixup, object fixupTarget,
                                                            string identifier, bool emptyValue,
                                                            bool nullValue)
        {
            object ret;
            if (nullValue)
            {
                processNullInReader(deserializeAs, identifier, fixup, fixupTarget);
            }
            else
            {
                Reader.ReadStartElement();

                if (emptyValue)
                {
                    ret = SerializationHelper.StringToPrimitive(deserializeAs, string.Empty);
                }
                else
                {
                    ret = SerializationHelper.StringToPrimitive(deserializeAs, Reader.ReadString());
                    Reader.ReadEndElement();
                }

                // Eat the whitespace after the primitive's element
                IgnoreWhitespace();

                // Assign desrialized value to fixup target;                
                RunFixup(accessor, fixup, fixupTarget, ret);

                // It's possible for primitive types to have a forward reference.              
                if (m_Encoded)
                {
                    if (identifier != null)
                    {
                        add(identifier, ret);
                    }
                }
            }
        }

        //-----------------------------------------------------------------        
        // This method should be overridden to customize how enumerated 
        // values are deserialized from elements. This method leaves the 
        // Reader on the content element after the enumerated element.
        //-----------------------------------------------------------------        
        protected virtual void DeserializeEnumeratedElement(LogicalType deserializeAs, Accessor accessor,
                                                                Fixup fixup, object fixupTarget,
                                                                string identifier, bool emptyValue,
                                                                bool nullValue)
        {
            object ret;
            LogicalType deserializeAsType = deserializeAs.IsNullableType ? deserializeAs.NullableValueType : deserializeAs;

            if (nullValue)
            {
                processNullInReader(deserializeAsType, identifier, fixup, fixupTarget);
            }
            else
            {
                Reader.ReadStartElement();
                if (emptyValue)
                {
                    ret = SerializationHelper.StringToEnumeration(deserializeAsType, string.Empty);
                }
                else
                {
                    ret = SerializationHelper.StringToEnumeration(deserializeAsType, Reader.ReadString());
                    Reader.ReadEndElement();
                }

                // Ignore whitespace after enumerated element
                IgnoreWhitespace();

                // Assign desrialized value to fixup target;
                RunFixup(accessor, fixup, fixupTarget, ret);

                // It's possible for primitive types to have a forward reference.
                if (m_Encoded)
                {
                    if (identifier != null)
                    {
                        add(identifier, ret);
                    }
                }
            }
        }

        //-----------------------------------------------------------------        
        // This method should be overridden to customize how array like 
        // values are deserialized from an element. This method reads the
        // entire array like including the start and end elements. The Reader
        // is left on the element after the array like.
        //-----------------------------------------------------------------        
        protected virtual void DeserializeArrayLikeElement(LogicalType deserializeAs, Accessor accessor,
                                                            Fixup fixup, object fixupTarget,
                                                            string identifier, bool emptyValue,
                                                            bool nullValue)
        {
            if (!m_Encoded)
            {
                deserializeLiteralArray(accessor, deserializeAs, fixup, fixupTarget, nullValue);
            }
            else
            {
                deserializeEncodedArray(deserializeAs.TypeAccessor, deserializeAs, fixup,
                                        fixupTarget, nullValue, identifier);
            }
        }

        //-----------------------------------------------------------------        
        // This method should be overridden to customize how complex types
        // are deserialized from elements. The entire complex element is 
        // read including the start and end element. This method leaves the 
        // Reader on the element after the complex element. 
        //-----------------------------------------------------------------        
        protected virtual void DeserializeComplexElement(LogicalType deserializeAs, Accessor accessor,
                                                            Fixup fixup, object fixupTarget,
                                                            string identifier, bool emptyValue,
                                                            bool nullValue)
        {
            if (nullValue)
            {
                processNullInReader(deserializeAs, identifier, fixup, fixupTarget);
            }
            else
            {
                //allocate instance of type.
                object instance;
                if (deserializeAs.Type != null)
                {
                    Type instanceType = deserializeAs.IsNullableType ? deserializeAs.NullableValueType.Type : deserializeAs.Type;
                    instance = SerializationHelper.CreateInstance(instanceType);
                }
                else
                {
                    //null type is special case (synthetic response type)
                    //special case for the synthetic type for arguments                    
                    instance = this;
                }

                //save off the instance of the object here, because I may
                //encounter this object again during deserialization of
                //this one (a graph with a cycle, for example)
                if (m_Encoded)
                {
                    if (identifier != null)
                    {
                        add(identifier, instance);
                    }
                }

                // Read the child elements as members. The reader is left on
                // element after the complex element, so there no need to eat
                // any whitespace after deserializing the members.
                deserializeMembers(deserializeAs.Members, instance);

                // Assign deserialized value to fixup target;
                RunFixup(accessor, fixup, fixupTarget, instance);
            }
        }

        //-----------------------------------------------------------------        
        // This method should be overridden to customize how custom values
        // are deserialized from elements. This method reads the entire
        // custom element including the start and end elements. The Reader
        // is left on the element after the custom element's end element.
        //-----------------------------------------------------------------        
        protected virtual void DeserializeCustomElement(LogicalType deserializeAs, Accessor accessor,
                                                            Fixup fixup, object fixupTarget,
                                                            string identifier, bool emptyValue,
                                                            bool nullValue)
        {
            object ret = null;
            if (nullValue)
            {
                processNullInReader(deserializeAs, identifier, fixup, fixupTarget);
            }
            else if (deserializeAs.CustomSerializer == CustomSerializerType.Base64)
            {
                ret = readByteArray(true);
                RunFixup(accessor, fixup, fixupTarget, ret);
            }
            else if (deserializeAs.CustomSerializer == CustomSerializerType.Hex)
            {
                ret = readByteArray(false);
                RunFixup(accessor, fixup, fixupTarget, ret);
            }
            else if (deserializeAs.CustomSerializer == CustomSerializerType.QName)
            {
                Reader.ReadStartElement();
                IgnoreWhitespace(); // Eat whitespace before QName

                if (emptyValue)
                {
                    ret = makeQName(string.Empty, true /*useDefaultNamespace*/ );
                }
                else
                {
                    ret = makeQName(Reader.ReadString(), true /*useDefaultNamespace*/, true /*dontThrowOnNullNamespace*/ );
                    Reader.ReadEndElement();
                }

                RunFixup(accessor, fixup, fixupTarget, ret);
            }
            else
            {
                Debug.Assert(false, "Invalid custom type" + deserializeAs.Type.FullName);
                throw new InvalidOperationException();
            }

            // Eat whitespace after the end element.
            IgnoreWhitespace();

            // It's possible for primitive types to have a forward
            // reference.  Null values have already processed this.            
            if (!nullValue && m_Encoded)
            {
                if (identifier != null)
                {
                    add(identifier, ret);
                }
            }
        }

        //-----------------------------------------------------------------        
        // This method should be overridden to customize how to handle 
        // deserialzing a serializable object from an element. This method
        // is leaves the Reader where IXmlserializable.ReadXml left it,
        // except that it eats any whitespace that the IXmlSerializable 
        // object may have missed.
        //-----------------------------------------------------------------        
        protected virtual void DeserializeSerializableElement(LogicalType deserializeAs, Accessor accessor,
                                                                Fixup fixup, object fixupTarget,
                                                                string identifier, bool emptyValue,
                                                                bool nullValue)
        {
            if (nullValue && false /* AppDomain.CompatVersion < AppDomain.Shaman */)
            { // only do special null handling prior to Orcas, Shaman
                processNullInReader(deserializeAs, identifier, fixup, fixupTarget);
            }
            else
            {
                Debug.Assert(!m_Encoded, "IXS in encoded message");
                IXmlSerializable ixs;
                if (!deserializeAs.IsNullableType)
                {
                    ixs = (IXmlSerializable)SerializationHelper.CreateInstance(deserializeAs.Type);
                }
                else
                {
                    Debug.Assert(deserializeAs.NullableValueType != null, "The logical type corresponding to the value type of an Nullable is null");
                    ixs = (IXmlSerializable)SerializationHelper.CreateInstance(deserializeAs.NullableValueType.Type);
                }

                ixs.ReadXml(Reader);

                // Eat the whitespace after the serializable element
                IgnoreWhitespace();

                // Assign desrialized value to fixup target;
                RunFixup(accessor, fixup, fixupTarget, ixs);
            }
        }

#if !FEATURE_LEGACYNETCF
        //-----------------------------------------------------------------        
        // This method should be overridden to customize deserializing
        // a XmlNode from an element. The reader is left on the element after
        // the XmlNode's element.
        //-----------------------------------------------------------------        
        protected virtual void DeserializeXmlNode(  LogicalType deserializeAs, Accessor accessor, 
                                                    Fixup fixup, object fixupTarget,
                                                    string identifier, bool emptyValue,
                                                    bool nullValue ) 
        {
            // Should an empty node be constructed here?             
            if( emptyValue ) {
                processNullInReader( deserializeAs, identifier, fixup, fixupTarget );                
            }
            else {                
                string tag = Reader.LocalName;
                int depth = Reader.Depth;

                Reader.ReadStartElement();                
                IgnoreWhitespace();             // Eat any whitespace before the inner XmlNode                                
                XmlNode n = Document.ReadNode(Reader);                             
                while( depth < Reader.Depth )   // Eat any nodes that appear after the inner XmlNode
                    Reader.Read();

                Debug.Assert(Reader.NodeType == XmlNodeType.EndElement && Reader.LocalName == tag, "No End Element for the XmlNode");
                Reader.ReadEndElement();
                
                // If the node was originally XmlNode was an XmlDocument then we must
                // wrap the deserialized node in a new XmlDocument and then run the 
                // fixup with the document.                
                if( deserializeAs.Type == typeof(XmlDocument ) ) {
                    XmlDocument doc = new XmlDocument();
                    if( n != null ) doc.LoadXml( n.OuterXml );                                            
                    n = doc;
                }

                // Assign desrialized value to fixup target;
                RunFixup(accessor, fixup, fixupTarget, n);                                
            }

            // Eat any whitespace after the entire element
            IgnoreWhitespace();             
        }
#endif

        //-----------------------------------------------------------------        
        // This method should be overridden to customize deserializing a
        // Soap fault from an element. The reader is left on the element 
        // after the soap fault.
        //-----------------------------------------------------------------        
        protected virtual void DeserializeSoapFaultElement(LogicalType deserializeAs, Accessor accessor,
                                                            Fixup fixup, object fixupTarget,
                                                            string identifier, bool emptyValue,
                                                            bool nullValue)
        {
            Debug.Assert(false, "The XmlSerializationReader can not deserialize SoapFaults");
            Reader.Skip();
            IgnoreWhitespace(); // Eat the whitespace after the soap fault element
        }

        //-----------------------------------------------------------------        
        // Note: Attribute Accessors will never be "Delayed", so no fixup.
        //-----------------------------------------------------------------        
        private object deserializeAttribute(Accessor accessor)
        {
            LogicalType deserializeAs = accessor.Type;

            switch (deserializeAs.Serializer)
            {
                case SerializerType.Primitive:
                    return SerializationHelper.StringToPrimitive(deserializeAs, Reader.Value);
                case SerializerType.Enumerated:
                    return SerializationHelper.StringToEnumeration(!deserializeAs.IsNullableType ? deserializeAs : deserializeAs.NullableValueType, Reader.Value);
                case SerializerType.Array:	 //other ArrayLike's other than Collection are not legal here
                    {
                        Debug.Assert(!m_Encoded, "deserializing encoded attribute array");
                        string[] strElts = Reader.Value.Split(null);

                        //guarantees the nested type is right
                        AccessorCollection nested = accessor.NestedAccessors;
                        if (nested == null)
                            nested = deserializeAs.TypeAccessor.NestedAccessors;
                        LogicalType eltType = nested.Default.Type;

                        Array ret = Array.CreateInstance(eltType.Type,
                            strElts.Length);
                        SerializerType eltSerializer = eltType.Serializer;
                        for (int i = 0; i < ret.Length; ++i)
                        {
                            object elt;
                            if (eltSerializer == SerializerType.Primitive)
                            {
                                elt = SerializationHelper.StringToPrimitive(eltType, strElts[i]);
                            }
                            else if (eltSerializer == SerializerType.Enumerated)
                            {
                                elt = SerializationHelper.StringToEnumeration(eltType.IsNullableType ? eltType : eltType.NullableValueType, strElts[i]);
                            }
                            else if (eltSerializer == SerializerType.Custom)
                            {
                                if (eltType.CustomSerializer == CustomSerializerType.QName)
                                {
                                    elt = makeQName(strElts[i]);
                                }
                                else if (eltType.CustomSerializer == CustomSerializerType.Base64)
                                {
                                    elt = Convert.FromBase64String(strElts[i]);
                                }
                                else if (eltType.CustomSerializer == CustomSerializerType.Hex)
                                {
                                    elt = ExtensionMethods.FromBinHexString(strElts[i], true);
                                }
                                else
                                {
                                    Debug.Assert(false, "Invalid custom type " + eltType.Type.FullName);
                                    throw new InvalidOperationException();
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Invalid sequence type " + deserializeAs.Type);
                                throw new InvalidOperationException(SR.Format(SR.XmlS_IllegalSequenceType_1, deserializeAs.Type));
                            }

                            ret.SetValue(elt, i);
                        }

                        return ret;
                    }
                case SerializerType.Collection:
                    {
                        Debug.Assert(!m_Encoded, "deserializing encoded attribute collection");
                        string[] strElts = Reader.Value.Split(null);

                        //guarantees the nested type is right
                        AccessorCollection nested = accessor.NestedAccessors;
                        if (nested == null)
                            nested = deserializeAs.TypeAccessor.NestedAccessors;
                        LogicalType eltType = nested.Default.Type;

                        object ret = Activator.CreateInstance(deserializeAs.Type);
                        IEnumerable iEnum = ret as IEnumerable;
                        if (iEnum != null)
                        {
                            SerializerType eltSerializer = eltType.Serializer;
                            MethodInfo addMethod = deserializeAs.Type.GetMethod("Add", new Type[] { eltType.Type });

                            for (int i = 0; i < strElts.Length; ++i)
                            {
                                object elt;
                                if (eltSerializer == SerializerType.Primitive)
                                {
                                    elt = SerializationHelper.StringToPrimitive(eltType, strElts[i]);
                                }
                                else if (eltSerializer == SerializerType.Enumerated)
                                {
                                    elt = SerializationHelper.StringToEnumeration(eltType.IsNullableType ? eltType : eltType.NullableValueType, strElts[i]);
                                }
                                else if (eltSerializer == SerializerType.Custom)
                                {
                                    if (eltType.CustomSerializer == CustomSerializerType.QName)
                                    {
                                        elt = makeQName(strElts[i]);
                                    }
                                    else if (eltType.CustomSerializer == CustomSerializerType.Base64)
                                    {
                                        elt = Convert.FromBase64String(strElts[i]);
                                    }
                                    else if (eltType.CustomSerializer == CustomSerializerType.Hex)
                                    {
                                        elt = ExtensionMethods.FromBinHexString(strElts[i], true);
                                    }
                                    else
                                    {
                                        Debug.Assert(false, "Invalid custom type " + eltType.Type.FullName);
                                        throw new InvalidOperationException();
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("Invalid sequence type " + deserializeAs.Type);
                                    throw new InvalidOperationException(SR.Format(SR.XmlS_IllegalSequenceType_1, deserializeAs.Type));
                                }

                                addMethod.Invoke(ret, new object[] { elt });
                            }
                        }

                        return ret;
                    }
                case SerializerType.Custom:
                    if (deserializeAs.CustomSerializer == CustomSerializerType.Base64)
                    {
                        return Convert.FromBase64String(Reader.Value);
                    }
                    else if (deserializeAs.CustomSerializer == CustomSerializerType.Hex)
                    {
                        return ExtensionMethods.FromBinHexString(Reader.Value, true);
                    }
                    else if (deserializeAs.CustomSerializer == CustomSerializerType.QName)
                    {
                        return makeQName(Reader.Value);
                    }
                    else
                    {
                        Debug.Assert(false, "Invalid custom type" + deserializeAs.Type.FullName);
                        throw new InvalidOperationException();
                    }

                default:
                    Debug.WriteLine("Invalid attribute type"
                        + deserializeAs.Name);

                    throw new InvalidOperationException(SR.Format(SR.XmlUnexpectedType, deserializeAs.Type));
            }
        }

        //-----------------------------------------------------------------        
        // Deserializes an encoded array like element. This method reads 
        // the entire array like element. The reader is left on the element
        // after the array like's root element.
        //-----------------------------------------------------------------        
        internal void deserializeEncodedArray(Accessor accessor, LogicalType deserializeAs,
                                                Fixup fixup, object fixupTarget, bool isNull,
                                                string id)
        {
            Debug.Assert(m_Encoded, "Attempting to deserialize an encoded array, but the read is set for literal deserialization");

            //handle a null.
            if (isNull)
            {
                processNullInReader(deserializeAs, id, fixup, fixupTarget);
                return;
            }

            //I know this is an array, and not just an array forward reference.

            //here's the algorithm
            //1) From the array type, get the element type.
            //2) deserialize based on that type.

            //This is a default accessor for elements.  Most elements are going
            //to be deserialized using their xsi:type.
            Accessor defEltAccessor = accessor.NestedAccessors.Default;
            Fixup newFixup = getFixupForCollection(fixup, fixupTarget, deserializeAs, id);

            // If this element is empty then we simply skip it	
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
                IgnoreWhitespace(); // Eat whitespace after the empty element
                return;
            }

            Reader.ReadStartElement();
            IgnoreWhitespace(); // Eat the whitespace after the array like's root element

            while (Reader.NodeType != XmlNodeType.EndElement)
            {
                //If there is xml text embedded in an array, I get thrown
                //off.  Skip the xml text and start over.
                if (Reader.NodeType == XmlNodeType.Text)
                {
                    Debug.WriteLine("Skipping text in an array \"" + Reader.Value + "\"");
                    Reader.ReadString();
                    continue;
                }

                //getting the fixup actually mutates the target
                //list, so I need to call this in the loop.
                object newTarget = getIndexForNextElement(newFixup);
                deserializeElement(defEltAccessor, newFixup, newTarget);
            }

            Reader.ReadEndElement();
            IgnoreWhitespace(); // Eat the whitespace after the array like's end element
        }

        //-----------------------------------------------------------------        
        // Deserializes a literal array-like element. This method reads 
        // the entire array-like element. The reader is left on the element
        // after the array-like's root element.
        //-----------------------------------------------------------------        
        protected void deserializeLiteralArray(Accessor accessor, LogicalType deserializeAs,
                                                Fixup fixup, object fixupTarget, bool isNull)
        {
            Debug.Assert(!m_Encoded, "Attempting to deserialize a literal array, but the read is set for encoded deserialization");

            if (accessor.AllowRepeats)
            {
                Fixup newFixup = getFixupForCollection(fixup, fixupTarget, deserializeAs, null);
                object newTarget = getIndexForNextElement(newFixup);
                if (isNull)
                {
                    //we have a null
                    processNullInReader(deserializeAs, null, newFixup, newTarget);
                }
                else
                {
                    Accessor elementAccessor = accessor.NestedAccessors.findAccessorNoThrow(NodeLocalName, NodeNamespace);
                    if (elementAccessor == null)
                    {
                        Debug.WriteLine("No repeating element accessor for " + NodeNamespace + ":" + NodeLocalName);
                        throw new InvalidOperationException(SR.Format(SR.XmlS_NoRepeatingAccessor_2, NodeNamespace, NodeLocalName));
                    }

                    deserializeElement(elementAccessor, newFixup, newTarget);
                }
            }
            else
            {
                // If xsiNil is set to '1' or 'true', then the entire array is considered 
                // null. The entire array is considered null, not just the null                    
                if (isNull)
                {
                    processNullInReader(deserializeAs, null, fixup, fixupTarget);
                    return;
                }

                Fixup newFixup = getFixupForCollection(fixup, fixupTarget, deserializeAs, null);

                // If this element is empty then we simply skip it	                
                if (Reader.IsEmptyElement)
                {
                    Reader.Skip();
                    IgnoreWhitespace(); // Eat the whitespace after the empty element
                    return;
                }

                AccessorCollection nested = accessor.NestedAccessors;
                if (nested == null)
                {
                    //I need to use the accessor for the parameter type, not
                    //the actual serialization type.
                    nested = accessor.Type.TypeAccessor.NestedAccessors;
                }

                Reader.ReadStartElement();
                IgnoreWhitespace(); // Ignore the whitespace after the array like's root element

                while (Reader.NodeType != XmlNodeType.EndElement)
                {
                    // If there is xml text or comments embedded in an array skip it
                    // and move to the the next node.
                    if (Reader.NodeType == XmlNodeType.Text || Reader.NodeType == XmlNodeType.Comment)
                    {
                        Debug.WriteLine("Skipping text or comment in an array: \"" + Reader.Value + "\"");
                        Reader.Read();
                        continue;
                    }

                    // Get the element's local name and node namespace.
                    XmlQualifiedName elementQName = new XmlQualifiedName(NodeLocalName, NodeNamespace);

                    // Look in the nested accessor collection for a possible matching accessor.
                    // 
                    // XAA_NOTE: According to spec, literal array elements are supposed to be in no 
                    // namespace (meaning that nested.findAccessorNoThrow(elementLocalName, null)
                    // is called), but there are cases where the array element will have a 
                    // namespace. One case is when an XmlArrayItemAttribute decorates the array 
                    // and the Namespace property is set. For example:                  
                    //
                    //     XmlArrayAttribute[("FooArray")]
                    //     XmlArrayItem[type(foo), Namespace="http://myNamespace/types"]
                    //     ArrayList fooArray;
                    //
                    // To compensate for this, nested.findAccessorNoThrow() will be called with 
                    // the element's namespace specified. If no accessor is found then 
                    // nested.findAccessorNoThrow() is called with null as the namespace. If still 
                    // no accessor is found, then the nested collection's default accessor is used.                    
                    Accessor elementAccessor = nested.findAccessorNoThrow(elementQName.Name, elementQName.Namespace);
                    if (elementAccessor == null || elementAccessor.Type.CustomSerializer == CustomSerializerType.Object)
                    {
                        Accessor elementAccessorNoNS = nested.findAccessorNoThrow(elementQName.Name, null);
                        if (elementAccessorNoNS != null)
                        {
                            elementAccessor = elementAccessorNoNS;
                        }
                    }

                    // No accessor was found for the array element. In this case the nested 
                    // collection's default accessor will be used.
                    if (false /*AppDomain.CompatVersion < AppDomain.NetCFV37 */)
                    {
                        if (elementAccessor == null)
                        {
                            elementAccessor = nested.Default;
                        }
                    }

                    // If the element accessor could not be found in the nested accessor and the default 
                    // accessor is null, then query the XmlSerializationReflector to see if a mapping 
                    // between a type and the element's name and namespace exists. If a valid mapping 
                    // exists then the mapping's type will be used for deserialization

                    //
                    // NOTE: This means that the type was not specified as a valid type that could appear 
                    // in this array. Would it be more proper to throw in exception in the case???
                    if (elementAccessor == null || elementAccessor.Type.CustomSerializer == CustomSerializerType.Object)
                    {
                        // See XAA_NOTE above to understand why we call FindType with elementNamespace
                        // before trying FindType with null as the element namespace.
                        LogicalType eltType = m_Reflector.FindType(elementQName, m_Encoded);
                        if (eltType == null || eltType.CustomSerializer == CustomSerializerType.Object)
                        {
                            LogicalType eltTypeNoNS = m_Reflector.FindType(new XmlQualifiedName(elementQName.Name, null), m_Encoded);
                            if (eltTypeNoNS != null)
                            {
                                eltType = eltTypeNoNS;
                            }
                        }

                        // If an element type was found in the XmlSerializationReflection then it's accessor 
                        // is specified as the desrializing accessor.
                        if (eltType != null)
                        {
                            elementAccessor = eltType.TypeAccessor;
                        }
                    }

                    // If no valid mapping could be found in tehe XmlSerializationReflector and no
                    // element accessor could be found in the nested accessor collection, then there 
                    // is no other way to determine what type this element maps to. 
                    if (elementAccessor == null)
                    {
                        if (false /* AppDomain.CompatVersion < AppDomain.NetCFV37 */)
                        {
                            Debug.WriteLine("Couldn't deserialize array " + "element " + elementQName);
                            throw new InvalidOperationException(SR.Format(SR.XmlS_NoArrayEltAcc_2, elementQName.Namespace, elementQName.Name));
                        }
                        else
                        {
                            handleUnknownElement(elementQName, null, null);
                        }
                    }
                    else
                    {
                        // Get the index in the fixup array for the array like. The
                        // fixup of the deserialized element is not created until
                        // getIndexForNextElement is called, so getIndexForNextElement
                        // this must be called for each deserialized element.
                        object newTarget = getIndexForNextElement(newFixup);

                        // Deserialize the array element using the element accessor 
                        deserializeElement(elementAccessor, newFixup, newTarget);
                    }
                }

                Reader.ReadEndElement();
                IgnoreWhitespace(); //Eat the whitespaceafter the array like's end element    
            }
        }

        //-----------------------------------------------------------------        
        //
        //-----------------------------------------------------------------        
        protected void deserializeText(LogicalMemberValue textMember, object fixupTarget)
        {
            LogicalType textType = textMember.Accessors.Default.Type;
            SerializerType serializer = textType.Serializer;
            object value;

            if (serializer == SerializerType.Primitive
                || serializer == SerializerType.Enumerated
                || serializer == SerializerType.Custom
                || serializer == SerializerType.XmlNode)
            {
                value = doDeserializeText(textType);
                textMember.Fixup.fixup(fixupTarget, value);
            }
            else if (serializer == SerializerType.Array)
            {
                //other ArrayLikes are not legal as text types.

                // DataTypes only apply to primitive types, so I'm always
                // serializing by type.                
                LogicalType eltType = m_Reflector.FindTypeInNamespaces(textType.Type.GetElementType(), m_Encoded,
                    m_Reflector.DefaultNamespace, textMember.Accessors.Default.Namespace);
                if (eltType == null)
                {
                    Debug.WriteLine("Can't find " + textType.Type.GetElementType());
                    throw new InvalidOperationException(SR.Format(SR.XmlS_IllegalTextType_1, textType.Type.GetElementType()));
                }
                else if (eltType.Type == typeof(object))
                {
                    eltType = m_Reflector.FindTypeInNamespaces(typeof(string), m_Encoded,
                        m_Reflector.DefaultNamespace, textMember.Accessors.Default.Namespace);
                }

                Fixup newFixup = getFixupForCollection(textMember.Fixup, fixupTarget, textType, null);
                object newTarget = getIndexForNextElement(newFixup);
                value = doDeserializeText(eltType);
                newFixup.fixup(newTarget, value);
            }
            else
            {
                Debug.WriteLine("Illegal text type " + textType.Type);
                throw new InvalidOperationException(SR.Format(SR.XmlS_IllegalTextType_1, textType.Type));
            }

            IgnoreWhitespace(); // Eat the whitespace after the text node
        }

        //-----------------------------------------------------------------        
        //
        //-----------------------------------------------------------------        
        protected object doDeserializeText(LogicalType deserializeAs)
        {
            switch (deserializeAs.Serializer)
            {
                case SerializerType.Primitive:
                    return SerializationHelper.StringToPrimitive(deserializeAs, Reader.ReadString());
                case SerializerType.Enumerated:
                    return SerializationHelper.StringToEnumeration(deserializeAs, Reader.ReadString());
#if !FEATURE_LEGACYNETCF
                case SerializerType.XmlNode:
                    return (new XmlDocument()).CreateTextNode( Reader.ReadString() );
#endif
                case SerializerType.Custom:
                    if (deserializeAs.CustomSerializer == CustomSerializerType.QName)
                    {
                        return makeQName(Reader.ReadString(), true /*useDefaultNamespace*/, true /*dontThrowOnNullNamespace*/ );
                    }
                    else if (deserializeAs.CustomSerializer == CustomSerializerType.Base64)
                    {
                        return Convert.FromBase64String(Reader.ReadString());
                    }
                    else if (deserializeAs.CustomSerializer == CustomSerializerType.Hex)
                    {
                        return ExtensionMethods.FromBinHexString(Reader.ReadString(), true);
                    }
                    else
                    {
                        Debug.Assert(false, "Invalid custom type" + deserializeAs.Type.FullName);
                        throw new InvalidOperationException();
                    }
                default:
                    Debug.WriteLine("Illegal text type " + deserializeAs.Type);
                    throw new InvalidOperationException(SR.Format(SR.XmlS_IllegalTextType_1, deserializeAs.Type));
            }
        }

        //-----------------------------------------------------------------        
        // This method reads a byte array from the XmlReader. The method 
        // was taken from desktop's XmlSerializationReader with the 
        // following modifications:        
        //      - reduced the MAX_ALLOC_SIZE
        //      - removed XmlReader code path
        //      - changed reader to Reader        
        //-----------------------------------------------------------------
        private byte[] readByteArray(bool isBase64)
        {
            List<byte[]> list = new List<byte[]>(4);
            const int MAX_ALLOC_SIZE = 8 * 1024;
            int currentSize = 1024;
            byte[] buffer;
            int bytes = -1;
            int offset = 0;
            int total = 0;

            buffer = new byte[currentSize];
            list.Add(buffer);
            while (bytes != 0)
            {
                if (offset == buffer.Length)
                {
                    currentSize = Math.Min(currentSize * 2, MAX_ALLOC_SIZE);
                    buffer = new byte[currentSize];
                    offset = 0;
                    list.Add(buffer);
                }

                if (isBase64)
                {
                    bytes = Reader.ReadElementContentAsBase64(buffer, offset, buffer.Length - offset);
                }
                else
                {
                    bytes = Reader.ReadElementContentAsBinHex(buffer, offset, buffer.Length - offset);
                }

                offset += bytes;
                total += bytes;
            }

            byte[] result = new byte[total];
            offset = 0;
            foreach (byte[] block in list)
            {
                currentSize = Math.Min(block.Length, total);
                if (currentSize > 0)
                {
                    Buffer.BlockCopy(block, 0, result, offset, currentSize);
                    offset += currentSize;
                    total -= currentSize;
                }
            }
            list.Clear();
            return result;
        }

        //-----------------------------------------------------------------
        // Handles an element that represents a null value. If this is an 
        // encoded object then the null is associated the id, if the element
        // has one.
        //-----------------------------------------------------------------
        protected void processNullInReader(LogicalType deserializeAs, string identifier, Fixup fixup, object fixupTarget)
        {
            if (!m_Encoded)
            {
                // Associate the null with a potential identifier.
                fixup.fixup(fixupTarget, null);
            }
            else
            {
                fixupWithIdentifier(identifier, fixup, fixupTarget, null);
            }

            Reader.MoveToContent();
            Reader.Read();
            IgnoreWhitespace(); // Eat whitespace after the empty
        }

        //********************************************************
        // Handle Reference Records
        //********************************************************

        //-----------------------------------------------------------------
        // Returns all ReferenceRecord objects seen thus far. 
        //-----------------------------------------------------------------
        public IEnumerable getAllRecords()
        {
            return m_idCache.Values;
        }


        //-----------------------------------------------------------------
        // Finds a reference record associated with id, otherwise returns 
        // null.      
        //-----------------------------------------------------------------
        public ReferenceRecord getRecord(string id)
        {
            return (ReferenceRecord)m_idCache[id];
        }

        //-----------------------------------------------------------------
        // Adds a reference record to the id cache. This methos throws an
        // XmlException if the id is already present in the id cache. 
        //-----------------------------------------------------------------
        public void add(string id, object o)
        {
            if (m_idCache.ContainsKey(id))
            {
                Debug.WriteLine("The identifier " + id + " appears more than once.");
                throw new XmlException(SR.Format(SR.XmlS_RepeatedIdentifier_1, id));
            }

            ReferenceRecord record = new ReferenceRecord(id, o);
            m_idCache[id] = record;
        }

        //-----------------------------------------------------------------
        // Assigns the value of a meber to the object that is identified by
        // the href attribute. If the id in the href has not yet been 
        // deserialized then we create a referenceFixup and put it on the 
        // delayed fixup stack. If the object has been seend then we assign 
        // the already deserialized object to the meber using the fixup
        // and fixupTarget parameters. The readers is left on the element 
        // after the href'ed element.
        //-----------------------------------------------------------------
        private void doHrefFixup(string hrefIdentifier, Fixup fixup, object fixupTarget)
        {
            //must start with # and must have at least one character in the reference
            if (!m_Soap12 && (hrefIdentifier.Length < 2 || hrefIdentifier[0] != '#'))
            {
                Debug.WriteLine("Illegal href format");
                throw new InvalidOperationException(SR.Format(SR.XmlS_InvalidHref_1, hrefIdentifier));
            }

            string identifier = m_Soap12 ?
                hrefIdentifier :
                hrefIdentifier.Substring(1);

            ReferenceRecord referenced = getRecord(identifier);
            if (referenced != null)
            {
                // store the previously constructed referenced value.
                fixup.fixup(fixupTarget, referenced.value);
            }
            else
            {
                // I haven't seen the object yet, so push the fixup
                pushFixup(makeReferenceFixup(identifier, fixup, fixupTarget));
            }

            Reader.Skip();
            IgnoreWhitespace(); // Eat the whitespace after the href'ed element
        }

        internal virtual void CheckUnreferencedObjects(object o)
        {
            if (!m_Encoded)
                return;

            foreach (ReferenceRecord refRecord in getAllRecords())
            {
                if (!refRecord.referenced && refRecord.value != o)
                {
                    UnreferencedObject(refRecord.id, refRecord.value);
                }
            }
        }

        protected void RunFixup(Accessor accessor, Fixup fixup, object fixupTarget, object value)
        {
            // Assign the enumerated value to the fixup target
            if (accessor.IsXmlTextArrayElement || accessor.IsXmlAnyArrayElement)
            {
                LogicalType arrayElement = m_Reflector.FindTypeInNamespaces(typeof(object[]), m_Encoded,
                    m_Reflector.DefaultNamespace, accessor.Namespace);
                Fixup newFixup = getFixupForCollection(fixup, fixupTarget, arrayElement, null);
                object newTarget = getIndexForNextElement(newFixup);
                newFixup.fixup(newTarget, value);
            }
            else
            {
                fixup.fixup(fixupTarget, value);
            }
        }

        private string currentTag()
        {
            switch (Reader.NodeType)
            {
                case XmlNodeType.Element:
                    return "<" + Reader.LocalName + " xmlns='" + Reader.NamespaceURI + "'>";
                case XmlNodeType.EndElement:
                    return ">";
                case XmlNodeType.Text:
                    return Reader.Value;
                case XmlNodeType.CDATA:
                    return "CDATA";
                case XmlNodeType.Comment:
                    return "<--";
                case XmlNodeType.ProcessingInstruction:
                    return "<?";
                default:
                    return "(unknown)";
            }
        }

        protected internal Exception CreateUnknownNodeException()
        {
            return new InvalidOperationException(SR.Format(SR.XmlUnknownNode, currentTag()));
        }

        //********************************************************
        // Handle UnknownElements
        //********************************************************

        //-----------------------------------------------------------------
        // This method should be overridden to customize how
        // unknown attributes are handled. 
        //-----------------------------------------------------------------
        protected virtual void handleUnknownAttribute(string localName, string ns, object fixupTarget, MemberValueCollection members)
        {
            if ((Soap.Xmlns.Equals(ns) || "xmlns".Equals(localName)) ||
                (Soap.XsiUrl.Equals(ns) && "type".Equals(localName)) ||
                (Soap.XsdUrl.Equals(ns) && Soap.Xsd.Equals(localName)))
                return;

#if !FEATURE_LEGACYNETCF
            //XmlNode value = Document.ReadNode( Reader );                        
            XmlAttribute value = Document.CreateAttribute(Reader.Prefix, Reader.LocalName, Reader.NamespaceURI);
            value.AppendChild(Document.CreateTextNode(Reader.Value));
            Debug.WriteLine( "Unknown XML Attribute " + value.LocalName + " in namespace " + value.NamespaceURI );

            LogicalMemberValue any = (members != null && members.HasXmlAny ) ? 
                members.XmlAny.XmlAnyAttribute : 
                null;                                                                          

            if( any != null ) 
                assign( any, fixupTarget, value );                      
            else
                UnknownNode( value as XmlAttribute, fixupTarget );            
#endif
        }

        //-----------------------------------------------------------------
        // This method should be overridden to customize how
        // unknown elements are handles.
        //-----------------------------------------------------------------
        protected virtual void handleUnknownElement(XmlQualifiedName qname, object fixupTarget, MemberValueCollection members)
        {
            Debug.WriteLine("Unknown XML Element " + qname.Name + " in namespace " + qname.Namespace);

            LogicalMemberValue any = (members != null && members.HasXmlAny) ?
                members.XmlAny.lookupAnyElement(qname.Name, qname.Namespace) :
                null;

            if (any != null)
            {
                LogicalType type = any.Accessors.Default.Type;
                if (typeof(IXmlSerializable).IsAssignableFrom(type.Type) ||
                    (type.Type.IsArray &&
                     typeof(IXmlSerializable).IsAssignableFrom(XmlSerializationReflector.GetElementType(type).Type)))
                {
                    deserializeElement(type.TypeAccessor, any.Fixup, fixupTarget);
                    return;
                }

#if !FEATURE_LEGACYNETCF
                XmlNode value = Document.ReadNode( Reader );
                assign( any, fixupTarget, value );
#else
                UnknownNode(null);
#endif

                XmlChoiceSupport choice = members.XmlAny.lookupChoiceSupport(any);
                if (choice != null)
                    SetAnyElementChoiceMemberValue(choice, qname.Name, qname.Namespace, fixupTarget);
            }
            else
            {
#if !FEATURE_LEGACYNETCF
                XmlNode value = Document.ReadNode( Reader );
                UnknownNode( value as XmlElement, fixupTarget );
#else
                UnknownNode(null);
#endif
            }

            // Ignore the whitespace after the unknown element
            IgnoreWhitespace();
        }

#if !FEATURE_LEGACYNETCF
        protected void UnknownElement(object o, XmlElement elem) 
        {            
            if (events.onUnknownElement != null) 
            {
                int lineNumber, linePosition;
                GetCurrentPosition(out lineNumber, out linePosition);
                XmlElementEventArgs e = new XmlElementEventArgs(elem, lineNumber, linePosition, o);
                events.onUnknownElement(events.sender, e);
            }            
        }

        protected void UnknownAttribute(object o, XmlAttribute attr) 
        {
            if (events.onUnknownAttribute != null) 
            {
                int lineNumber, linePosition;
                GetCurrentPosition(out lineNumber, out linePosition);
                XmlAttributeEventArgs e = new XmlAttributeEventArgs(attr, lineNumber, linePosition, o);
                events.onUnknownAttribute(events.sender, e);
            }
        }

        protected void UnknownNode(object o) 
        {
            XmlNode unknownNode = Document.ReadNode(Reader);
            UnknownNode(unknownNode, o);
        }

        void UnknownNode(XmlNode unknownNode, object o)
        {            
            if (unknownNode == null)
                return;
            if (unknownNode.NodeType != XmlNodeType.None && Reader.NodeType != XmlNodeType.Whitespace && events.onUnknownNode != null) 
            {
                int lineNumber, linePosition;
                GetCurrentPosition(out lineNumber, out linePosition);
                XmlNodeEventArgs e = new XmlNodeEventArgs(unknownNode, lineNumber, linePosition, o);
                events.onUnknownNode(events.sender, e);
            }
            if (unknownNode.NodeType == XmlNodeType.Attribute) 
            {
                UnknownAttribute(o, (XmlAttribute)unknownNode);
            }
            else if (unknownNode.NodeType == XmlNodeType.Element) 
            {
                UnknownElement(o, (XmlElement)unknownNode);
            }
        }

        protected void UnreferencedObject(string id, object o) 
        {
            if (events.onUnreferencedObject != null) 
            {
                UnreferencedObjectEventArgs e = new UnreferencedObjectEventArgs(o, id);
                events.onUnreferencedObject(events.sender, e);
            }
        }
#else
        protected void UnknownNode(object o)
        {
            if (Reader.NodeType != XmlNodeType.Attribute)
                Reader.ReadInnerXml();
        }

        protected void UnreferencedObject(string id, object o)
        {
        }
#endif

        private void GetCurrentPosition(out int lineNumber, out int linePosition)
        {
#if !FEATURE_LEGACYNETCF
            if (Reader is XmlTextReader)
            {
                XmlTextReader textReader = (XmlTextReader)Reader;
                lineNumber = textReader.LineNumber;
                linePosition = textReader.LinePosition;
            }   
            else
#endif
            lineNumber = linePosition = -1;
        }

        //-----------------------------------------------------------------
        // Creates a LogicalType object based on the string representation
        // of the type name. 
        //-----------------------------------------------------------------        
        protected LogicalType nameToType(string typeName)
        {
            typeName = CreateArrayTypeName(typeName);
            XmlQualifiedName qname = makeQName(typeName, false /*useDefaultNamespace*/, true /*dontThrowOnNullNamespace*/);

            // Don't throw if we could not resolve the name to a type. If
            // this happens, then we will fall back on the type we found 
            // while reflecting the proxy.            
            return m_Reflector.FindType(qname, m_Encoded);
        }

        protected LogicalType nameToType(string typeName, string typeNS)
        {
            typeName = CreateArrayTypeName(typeName);

            // Don't throw if we could not resolve the name to a type. If
            // this happens, then we will fall back on the type we found 
            // while reflecting the proxy.
            foreach (XmlQualifiedName qname in getTypeNamesToSearch(typeName, typeNS))
            {
                LogicalType lType = m_Reflector.FindType(qname, m_Encoded);
                if (lType != null)
                {
                    return lType;
                }
            }
            return null;
        }

        private IEnumerable<XmlQualifiedName> getTypeNamesToSearch(string typeName, string typeNS)
        {
            List<XmlQualifiedName> checkList = new List<XmlQualifiedName>(3);
            // Check the most obvious name and namespace (the given one).
            checkList.Add(new XmlQualifiedName(typeName, typeNS));
            // Also shift the first character in the name to lowercase and try that,
            // since intrinsic types (such as string) can be referred to as "String" in schema.
            if (typeName.Length > 0 && char.IsUpper(typeName, 0))
            {
                string typeNameWithLowercaseFirstLetter = char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);
                checkList.Add(new XmlQualifiedName(typeNameWithLowercaseFirstLetter));
            }
            // We need to check both the declared namespace and the 
            // Xsd namespace as Xsd ones aren't always specified.
            checkList.Add(new XmlQualifiedName(typeName, Soap.XsdUrl));

            return checkList;
        }

        private string CreateArrayTypeName(string typeName)
        {
            int indexOffset = typeName.IndexOf('[');
            if (indexOffset != -1)
            {
                //copies everything up to (but not including) the '['
                StringBuilder sb = new StringBuilder(typeName, 0, indexOffset, typeName.Length);
                sb.Append("[]");
                //build the rest of the type.
                for (int i = indexOffset + 1; i < typeName.Length; ++i)
                {
                    if (typeName[i] == '[') sb.Append("[]");
                }
                typeName = sb.ToString();
            }
            else if (typeName.StartsWith(SerializationStrings.ArrayOf, StringComparison.Ordinal) && typeName.Length > SerializationStrings.ArrayOf.Length)
            {
                int startIndex = 0;
                int arrayRank = 0;
                string tempTypeName;

                while (startIndex < typeName.Length)
                {
                    if (typeName.IndexOf(SerializationStrings.ArrayOf, startIndex, StringComparison.Ordinal) < 0)
                        break;

                    ++arrayRank;
                    startIndex += SerializationStrings.ArrayOf.Length;
                }

                StringBuilder brackets = null;
                if (arrayRank > 0)
                {
                    brackets = new StringBuilder(arrayRank * 2);
                    for (int curRank = 0; curRank < arrayRank; ++curRank)
                    {
                        brackets.Append("[]");
                    }
                }

                tempTypeName = typeName.Substring(startIndex);
                if (tempTypeName == "AnyType" || tempTypeName == "AnyUri")
                    tempTypeName = char.ToLowerInvariant(tempTypeName[0]) + tempTypeName.Substring(1);

                typeName = arrayRank > 0 ?
                    tempTypeName += brackets.ToString() :
                    tempTypeName;
            }

            return typeName;
        }

        internal LogicalType deriveTypeFromTypeAttribute(XmlReader reader, LogicalType defaultType)
        {
            LogicalType derivedType = null;
            string xsiTypeString = reader.GetAttribute("type", Soap.XsiUrl);

            if (xsiTypeString != null && xsiTypeString.Length > 0)
            {
                if (xsiTypeString.IndexOf(':') < 0)
                    derivedType = nameToType(xsiTypeString, reader.LookupNamespace(string.Empty));
                else
                    derivedType = nameToType(xsiTypeString);
            }

            return (derivedType != null) ? derivedType : defaultType;
        }

        //-----------------------------------------------------------------
        // Determines the type that the element deserializes as using the 
        // attributes on the element itself. If the method fails to derive 
        // the type by the attributes then the defaultType is returned. Note 
        // that the return type can be null if the defaultType is null. 
        //-----------------------------------------------------------------
        internal LogicalType deriveTypeFromAttributes(LogicalType defaultType)
        {
            if (m_Soap12)
            {
                string xsiTypeString = Reader.GetAttribute("type", Soap.XsiUrl);
                if (xsiTypeString != null)
                {
                    return xsiTypeString.IndexOf(':') < 0 ?
                        nameToType(xsiTypeString, Reader.LookupNamespace(string.Empty)) :
                        nameToType(xsiTypeString);
                }

                // Check for the arraySize attribute and throw an exception if it is malformed
                EnsureArraySizeAttribute(Reader.GetAttribute("arraySize", Soap12.Encoding));

                // Now check for itemType attribute. This is only for array elements. It 
                // should not be on stand alone or child elements.
                string itemTypeString = Reader.GetAttribute("itemType", Soap12.Encoding);
                if (itemTypeString != null && itemTypeString.Length > 0)
                {
                    LogicalType itemType = itemTypeString.IndexOf(':') < 0 ?
                        nameToType(itemTypeString, Reader.LookupNamespace(string.Empty)) :
                        nameToType(itemTypeString);
                    if (itemType == null) return defaultType;

                    if (defaultType != null)
                    {
                        Type defaultElementType = defaultType.Type.HasElementType ? defaultType.Type.GetElementType() : null;
                        if (defaultElementType == null || !itemType.Type.IsAssignableFrom(defaultElementType))
                        {
                            LogicalType arrayType = itemTypeString.IndexOf(':') < 0 ?
                                nameToType(itemTypeString + "[]", Reader.LookupNamespace(string.Empty)) :
                                nameToType(itemTypeString + "[]");
                            return arrayType == null ? defaultType : arrayType;
                        }
                    }
                    else
                    {
                        return itemTypeString.IndexOf(':') < 0 ?
                            nameToType(itemTypeString + "[]", Reader.LookupNamespace(string.Empty)) :
                            nameToType(itemTypeString + "[]");
                    }
                }

                return defaultType;
            }
            else
            {
                string xsiType = Reader.GetAttribute("type", Soap.XsiUrl);
                string soapEnc = Reader.GetAttribute("arrayType", Soap.Encoding);

                if (soapEnc != null)
                {
                    LogicalType soapEncodedArrayType = nameToType(soapEnc);
                    return soapEncodedArrayType == null ? defaultType : soapEncodedArrayType;
                }
                else if (xsiType != null)
                {
                    LogicalType xsiArrayType = xsiType.IndexOf(':') < 0 ?
                        nameToType(xsiType, Reader.LookupNamespace(string.Empty)) :
                        nameToType(xsiType);
                    return xsiArrayType ?? defaultType;
                }

                return defaultType;
            }
        }

        //-----------------------------------------------------------------
        // This method ensures that the arraysize attribute is formated 
        // correctly. The method either returns or throws an exception. If
        // it returns, then attribute was formatted correctly. If it throws
        // then the attribute is malformed. The exception contains the array
        // size dimenstion that caused the exception. The array dimensions 
        // are not stored since we do not use them to actually deserialize 
        // the array. We may want to call this this methid only when 
        // debugging so we can save some processing time. 
        //----------------------------------------------------------------- 
        private void EnsureArraySizeAttribute(string arraySize)
        {
            if (arraySize == null || arraySize.Length <= 0)
                return;

            string[] dimensions = arraySize.Split(null);
            for (int curSizeNdx = 0; curSizeNdx < dimensions.Length; ++curSizeNdx)
            {
                if (dimensions[curSizeNdx].Length > 0)
                {
                    if (dimensions[curSizeNdx] == "*") continue;

                    try
                    {
                        Int32.Parse(dimensions[curSizeNdx], NumberFormatInfo.InvariantInfo);
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException(SR.Format(SR.XmlInvalidArrayLength, dimensions[curSizeNdx]), "arraySize");
                    }
                    catch (OverflowException)
                    {
                        throw new ArgumentException(SR.Format(SR.XmlInvalidArrayLength, dimensions[curSizeNdx]), "arraySize");
                    }
                    catch (ArgumentNullException)
                    {
                        throw new ArgumentException(SR.Format(SR.XmlInvalidArrayLength, dimensions[curSizeNdx]), "arraySize");
                    }
                }
            }
        }

        //----------------------------------------------------------------- 
        // Finds the LogicalMember and Accessor associated the name and
        // namespace in the specified member context.  Returns true if the
        // member is found, false otherwise.
        //----------------------------------------------------------------- 
        private static bool doLookup(MemberValueCollection members, string name, string ns,
                                        Hashtable internalState, XmlReader reader, bool isAttribute,
                                        bool encoded, out LogicalMemberValue member, out Accessor accessor)
        {
            if (members == null)
            {
                member = null;
                accessor = null;
                return false;
            }

            // Look up the logical member based on the name and namespace of the element/attribute            
            XmlQualifiedName qname = new XmlQualifiedName(name, ns);
            bool success = members.doLookup(qname, isAttribute, out member, out accessor);

            // Look up failed. Return false.
            if (!success)
            {
#if DEBUG
                member = null;
                accessor = null;
#endif
                return false;
            }

            // Look up passed. The member and accessor MUST NOT be null.
            Debug.Assert(member != null, "MemberValueCollection.doLookup() return true but returned a null LogicalMemberValue");
            Debug.Assert(accessor != null, "MemberValueCollection.doLookup() return true but returned a null Accessor");

            // If this is a repeated element array allow it to appear more than once.
            if (!accessor.AllowRepeats && !member.IsXmlAnyElementArray && internalState.ContainsKey(member))
            {
#if DEBUG
                member = null;
                accessor = null;
#endif            
                return false;
            }

            internalState[member] = member;
            return true;
        }

        //*********************************************************************
        // Looking up fixups
        //
        //
        // The getFixupForCollection() and getIndex() manage collections:
        //      - getFixupForCollection does not mutate the Fixup. 
        //      - getIndex does mutate the Fixup so, it must be called exactly 
        //        once per element.        
        //*********************************************************************

        //----------------------------------------------------------------- 
        // This method gets the fixup for a given object/MemberValue pair.
        //----------------------------------------------------------------- 
        protected Fixup getFixupForCollection(Fixup memberFixup, object memberFixupTarget,
                                                LogicalType collectionType,
                                                string identifier)
        {
            // search for the fixup that matches this array.  A "match" is a fixup for the same member with the same target.
            ArrayLikeFixup fixup = null;
            if (m_headArrayLikeFixup != null)
            {
                ArrayLikeFixup current = m_headArrayLikeFixup;
                while (current != null)
                {
                    if (current.MatchFixup(memberFixup, memberFixupTarget, identifier))
                    {
                        fixup = current;
                        break;
                    }
                    current = current.m_nextFixup;
                }
            }

            if (fixup == null)
            {
                fixup = new ArrayLikeFixup(collectionType, m_idCache, memberFixup, memberFixupTarget, identifier);
                fixup.m_nextFixup = m_headArrayLikeFixup;
                m_headArrayLikeFixup = fixup;

                // now allocate the fixup to push.
                Fixup delayed = new ArrayLikeAssignmentFixup(fixup);
                pushFixup(delayed);
            }


            return fixup;
        }

        //----------------------------------------------------------------- 
        // This method gets the index of the next fixup for the any item in 
        // an array-like object that is being deserialized. The array fixup
        // will use this index to assign values when it is creating the 
        // array.
        //----------------------------------------------------------------- 
        protected int getIndexForNextElement(Fixup memberFixup)
        {
            ArrayLikeFixup fixup = (ArrayLikeFixup)memberFixup;
            return fixup.GetIndex();
        }

        //********************************************************
        // Handle Fixups
        //********************************************************

        //----------------------------------------------------------------- 
        //pushes a fixup onto the stack.  Fixups will be run in reverse order.
        //----------------------------------------------------------------- 
        protected void pushFixup(Fixup fixup)
        {
            m_fixups.Add(fixup);
        }

        //----------------------------------------------------------------- 
        // Run all of the fixups on the Fixup stack. The fixups on the 
        // stack are delayed fixups i.e. Array fixups, Reference Fixups and
        // they do not require a target or a value inorder to assign a value. 
        //----------------------------------------------------------------- 
        public void runFixups()
        {
            for (int i = m_fixups.Count - 1; i >= 0; --i)
            {
                Fixup fixup = m_fixups[i];
                fixup.fixup(null /*target*/, null /*value*/);
            }
            m_fixups.Clear();
        }

        //----------------------------------------------------------------- 
        // Creates a ReferenceFixup object.
        //----------------------------------------------------------------- 
        protected Fixup makeReferenceFixup(string id, Fixup nestedFixup, object nestedTarget)
        {
            return new ReferenceFixup(id, nestedFixup, nestedTarget, this);
        }

        //----------------------------------------------------------------- 
        // This function stores "value" in the fixup/fixupTarget pair.  In
        // addition, if "identifier" is not null, it adds "value" to the id
        // cache mapped to "identifier".
        //----------------------------------------------------------------- 
        protected void fixupWithIdentifier(string identifier, Fixup fixup,
                                            object fixupTarget, object value)
        {
            if (identifier != null)
                add(identifier, value);

            fixup.fixup(fixupTarget, value);
        }

        //********************************************************
        // Helpers
        //********************************************************

        //----------------------------------------------------------------- 
        // Creates an XmlQualifiedName object based on the string parameter.
        // The string value is expected to be in the format "prefix:name".
        // The namespace associated with the prefix is found by calling the
        // LookupNamespace method on the XmlReader. If the namespace could 
        // not be found than XmlException is thrown. If the string is not 
        // prefixed and the useDefaultNamespace parameter is true, then the
        // default namespace is used to create the XmlQualifiedName.
        //-----------------------------------------------------------------         
        protected XmlQualifiedName
        makeQName(string value, bool useDefaultNamespace, bool dontThrowOnNullNamespace)
        {
            if (value == null || value.Length <= 0)
                return XmlQualifiedName.Empty;

            int idx = value.IndexOf(':');
            string prefix = (idx == -1) ? (useDefaultNamespace ? string.Empty : null) : value.Substring(0, idx);
            string localName = value.Substring(idx + 1);
            string ns = prefix == null ? string.Empty : Reader.LookupNamespace(prefix);
            if (ns == null && !useDefaultNamespace && !dontThrowOnNullNamespace)
            {
                throw new XmlException(SR.Format(SR.XmlS_UnknownPrefix_1, prefix));
            }
            return new XmlQualifiedName(XmlConvert.DecodeName(localName), ns);
        }

        protected XmlQualifiedName makeQName(string value, bool useDefaultNamespace)
        {
            return makeQName(value, useDefaultNamespace, false /*dontThrowOnNullNamespace*/);
        }

        protected XmlQualifiedName makeQName(string value)
        {
            return makeQName(value, false /*useDefaultNamespace*/, false /*dontThrowOnNullNamespace*/);
        }

        //----------------------------------------------------------------- 
        // Sets the value of an object's member. It does this by calling 
        // the fixup associated with the member parameter using the 
        // fixupTarget and the value parameters.
        //----------------------------------------------------------------- 
        protected void assign(LogicalMemberValue member, object fixupTarget, object value)
        {
            Debug.WriteLine("in assign.");
            LogicalType type = member.Accessors.Default.Type;
            if (SerializationHelper.isLogicalArray(type))
            {
                Fixup newFixup = getFixupForCollection(member.Fixup, fixupTarget, type, null);
                object newTarget = getIndexForNextElement(newFixup);
                newFixup.fixup(newTarget, value);
            }
            else
            {
                member.Fixup.fixup(fixupTarget, value);
            }
        }

        //----------------------------------------------------------------- 
        // Returns true if the current node is a null (xsi:null/nil attribute)
        //----------------------------------------------------------------- 
        protected bool isNull()
        {
            string nil = Reader.GetAttribute("null", Soap.XsiUrl);
            if (nil == null)
            {
                nil = Reader.GetAttribute("nil", Soap.XsiUrl);
            }

            return (nil != null && (nil.Equals("1") || nil.Equals("true")));
        }

        //----------------------------------------------------------------- 
        // Ignores any whitespace or significant whitespace nodes.
        //-----------------------------------------------------------------       
        protected void IgnoreWhitespace()
        {
            while (Reader.NodeType == XmlNodeType.Whitespace || Reader.NodeType == XmlNodeType.SignificantWhitespace)
                Reader.Read();
        }
    }
}
