// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Xsl
{
    using TF = XmlQueryTypeFactory;

    /// <summary>
    /// This class is the only way to create concrete instances of the abstract XmlQueryType class.
    /// Once basic types have been created, they can be combined and transformed in various ways.
    /// </summary>
    internal static class XmlQueryTypeFactory
    {
        //-----------------------------------------------
        // Type Construction Operators
        //-----------------------------------------------

        /// <summary>
        /// Create an XmlQueryType from an XmlTypeCode.
        /// </summary>
        /// <param name="code">the type code of the item</param>
        /// <param name="isStrict">true if the dynamic type is guaranteed to match the static type exactly</param>
        /// <returns>the atomic value type</returns>
        public static XmlQueryType Type(XmlTypeCode code, bool isStrict)
        {
            return ItemType.Create(code, isStrict);
        }

        /// <summary>
        /// Create an XmlQueryType from an Xsd simple type (where variety can be Atomic, List, or Union).
        /// </summary>
        /// <param name="schemaType">the simple Xsd schema type of the atomic value</param>
        /// <param name="isStrict">true if the dynamic type is guaranteed to match the static type exactly</param>
        /// <returns>the atomic value type</returns>
        public static XmlQueryType Type(XmlSchemaSimpleType schemaType, bool isStrict)
        {
            if (schemaType.Datatype!.Variety == XmlSchemaDatatypeVariety.Atomic)
            {
                // We must special-case xs:anySimpleType because it is broken in Xsd and is sometimes treated as
                // an atomic value and sometimes as a list value.  In XQuery, it always maps to xdt:anyAtomicType*.
                if (schemaType == DatatypeImplementation.AnySimpleType)
                    return AnyAtomicTypeS;

                return ItemType.Create(schemaType, isStrict);
            }

            // Skip restrictions. It is safe to do that because this is a list or union, so it's not a build in type
            while (schemaType.DerivedBy == XmlSchemaDerivationMethod.Restriction)
                schemaType = (XmlSchemaSimpleType)schemaType.BaseXmlSchemaType!;

            // Convert Xsd list
            if (schemaType.DerivedBy == XmlSchemaDerivationMethod.List)
                return PrimeProduct(Type(((XmlSchemaSimpleTypeList)schemaType.Content!).BaseItemType!, isStrict), XmlQueryCardinality.ZeroOrMore);

            // Convert Xsd union
            Debug.Assert(schemaType.DerivedBy == XmlSchemaDerivationMethod.Union);
            XmlSchemaSimpleType[] baseMemberTypes = ((XmlSchemaSimpleTypeUnion)schemaType.Content!).BaseMemberTypes!;
            XmlQueryType[] queryMemberTypes = new XmlQueryType[baseMemberTypes!.Length];

            for (int i = 0; i < baseMemberTypes.Length; i++)
                queryMemberTypes[i] = Type(baseMemberTypes[i], isStrict);

            return Choice(queryMemberTypes);
        }

        /// <summary>
        /// Construct the union of two XmlQueryTypes
        /// </summary>
        /// <param name="left">the left type</param>
        /// <param name="right">the right type</param>
        /// <returns>the union type</returns>
        public static XmlQueryType Choice(XmlQueryType left, XmlQueryType right)
        {
            return SequenceType.Create(ChoiceType.Create(PrimeChoice(new List<XmlQueryType>(left), right)), left.Cardinality | right.Cardinality);
        }

        /// <summary>
        /// Construct the union of several XmlQueryTypes
        /// </summary>
        /// <param name="types">the list of types</param>
        /// <returns>the union type</returns>
        public static XmlQueryType Choice(params XmlQueryType[] types)
        {
            if (types.Length == 0)
                return None;
            else if (types.Length == 1)
                return types[0];

            // Union each type with next type
            List<XmlQueryType> list = new List<XmlQueryType>(types[0]);
            XmlQueryCardinality card = types[0].Cardinality;

            for (int i = 1; i < types.Length; i++)
            {
                PrimeChoice(list, types[i]);
                card |= types[i].Cardinality;
            }

            return SequenceType.Create(ChoiceType.Create(list), card);
        }

        /// <summary>
        /// Create a Node XmlQueryType which is the choice between several different node kinds.
        /// </summary>
        /// <param name="kinds">the node kinds which will make up the choice</param>
        /// <returns>the node type</returns>
        public static XmlQueryType NodeChoice(XmlNodeKindFlags kinds)
        {
            return ChoiceType.Create(kinds);
        }

        /// <summary>
        /// Construct the sequence of two XmlQueryTypes
        /// </summary>
        /// <param name="left">the left type</param>
        /// <param name="right">the right type</param>
        /// <returns>the sequence type</returns>
        public static XmlQueryType Sequence(XmlQueryType left, XmlQueryType right)
        {
            return SequenceType.Create(ChoiceType.Create(PrimeChoice(new List<XmlQueryType>(left), right)), left.Cardinality + right.Cardinality);
        }

#if NEVER
        /// <summary>
        /// Construct the sequence of several XmlQueryTypes
        /// </summary>
        /// <param name="types">the sequence of types</param>
        /// <returns>the sequence type</returns>
        public XmlQueryType Sequence(params XmlQueryType[] types) {
            XmlQueryCardinality card = XmlQueryCardinality.Zero;

            foreach (XmlQueryType t in types)
                card += t.Cardinality;

            return PrimeProduct(Choice(types), card);
        }
#endif

        /// <summary>
        /// Compute the product of the prime of "t" with cardinality "c".
        /// </summary>
        /// <param name="t">the member type</param>
        /// <param name="c">the cardinality</param>
        /// <returns>the prime type with the indicated cardinality applied</returns>
        public static XmlQueryType PrimeProduct(XmlQueryType t, XmlQueryCardinality c)
        {
            // If cardinality stays the same, then this is a no-op
            if (t.Cardinality == c && !t.IsDod)
                return t;

            return SequenceType.Create(t.Prime, c);
        }

        /// <summary>
        /// Compute a sequence of zero to some max cardinality.
        /// </summary>
        /// <param name="t">the type to sequence</param>
        /// <param name="c">the upper bound</param>
        /// <returns>the sequence of t from 0 to c</returns>
        public static XmlQueryType AtMost(XmlQueryType t, XmlQueryCardinality c)
        {
            return PrimeProduct(t, c.AtMost());
        }

        #region Built-in types
        //-----------------------------------------------
        // Pre-Created Types
        //
        // Abbreviations:
        //   P = Plus (+)
        //   Q = Question Mark (?)
        //   S = Star (*)
        //   X = Exact (IsStrict = true)
        //-----------------------------------------------

        public static readonly XmlQueryType None = ChoiceType.None;
        public static readonly XmlQueryType Empty = SequenceType.Zero;

        public static readonly XmlQueryType Item = TF.Type(XmlTypeCode.Item, false);
        public static readonly XmlQueryType ItemS = TF.PrimeProduct(Item, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType Node = TF.Type(XmlTypeCode.Node, false);
        public static readonly XmlQueryType NodeS = TF.PrimeProduct(Node, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType Element = TF.Type(XmlTypeCode.Element, false);
        public static readonly XmlQueryType ElementS = TF.PrimeProduct(Element, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType Document = TF.Type(XmlTypeCode.Document, false);
        public static readonly XmlQueryType DocumentS = TF.PrimeProduct(Document, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType Attribute = TF.Type(XmlTypeCode.Attribute, false);
        public static readonly XmlQueryType AttributeQ = TF.PrimeProduct(Attribute, XmlQueryCardinality.ZeroOrOne);
        public static readonly XmlQueryType AttributeS = TF.PrimeProduct(Attribute, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType Namespace = TF.Type(XmlTypeCode.Namespace, false);
        public static readonly XmlQueryType NamespaceS = TF.PrimeProduct(Namespace, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType Text = TF.Type(XmlTypeCode.Text, false);
        public static readonly XmlQueryType TextS = TF.PrimeProduct(Text, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType Comment = TF.Type(XmlTypeCode.Comment, false);
        public static readonly XmlQueryType CommentS = TF.PrimeProduct(Comment, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType PI = TF.Type(XmlTypeCode.ProcessingInstruction, false);
        public static readonly XmlQueryType PIS = TF.PrimeProduct(PI, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType DocumentOrElement = TF.Choice(Document, Element);
        public static readonly XmlQueryType DocumentOrElementQ = TF.PrimeProduct(DocumentOrElement, XmlQueryCardinality.ZeroOrOne);
        public static readonly XmlQueryType DocumentOrElementS = TF.PrimeProduct(DocumentOrElement, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType Content = TF.Choice(Element, Comment, PI, Text);
        public static readonly XmlQueryType ContentS = TF.PrimeProduct(Content, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType DocumentOrContent = TF.Choice(Document, Content);
        public static readonly XmlQueryType DocumentOrContentS = TF.PrimeProduct(DocumentOrContent, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType AttributeOrContent = TF.Choice(Attribute, Content);
        public static readonly XmlQueryType AttributeOrContentS = TF.PrimeProduct(AttributeOrContent, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType AnyAtomicType = TF.Type(XmlTypeCode.AnyAtomicType, false);
        public static readonly XmlQueryType AnyAtomicTypeS = TF.PrimeProduct(AnyAtomicType, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType String = TF.Type(XmlTypeCode.String, false);
        public static readonly XmlQueryType StringX = TF.Type(XmlTypeCode.String, true);
        public static readonly XmlQueryType StringXS = TF.PrimeProduct(StringX, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType Boolean = TF.Type(XmlTypeCode.Boolean, false);
        public static readonly XmlQueryType BooleanX = TF.Type(XmlTypeCode.Boolean, true);
        public static readonly XmlQueryType Int = TF.Type(XmlTypeCode.Int, false);
        public static readonly XmlQueryType IntX = TF.Type(XmlTypeCode.Int, true);
        public static readonly XmlQueryType IntXS = TF.PrimeProduct(IntX, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType IntegerX = TF.Type(XmlTypeCode.Integer, true);
        public static readonly XmlQueryType LongX = TF.Type(XmlTypeCode.Long, true);
        public static readonly XmlQueryType DecimalX = TF.Type(XmlTypeCode.Decimal, true);
        public static readonly XmlQueryType FloatX = TF.Type(XmlTypeCode.Float, true);
        public static readonly XmlQueryType Double = TF.Type(XmlTypeCode.Double, false);
        public static readonly XmlQueryType DoubleX = TF.Type(XmlTypeCode.Double, true);
        public static readonly XmlQueryType DateTimeX = TF.Type(XmlTypeCode.DateTime, true);
        public static readonly XmlQueryType QNameX = TF.Type(XmlTypeCode.QName, true);
        public static readonly XmlQueryType UntypedDocument = ItemType.UntypedDocument;
        public static readonly XmlQueryType UntypedElement = ItemType.UntypedElement;
        public static readonly XmlQueryType UntypedAttribute = ItemType.UntypedAttribute;
        public static readonly XmlQueryType UntypedNode = TF.Choice(UntypedDocument, UntypedElement, UntypedAttribute, Namespace, Text, Comment, PI);
        public static readonly XmlQueryType UntypedNodeS = TF.PrimeProduct(UntypedNode, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType NodeNotRtf = ItemType.NodeNotRtf;
        public static readonly XmlQueryType NodeNotRtfQ = TF.PrimeProduct(NodeNotRtf, XmlQueryCardinality.ZeroOrOne);
        public static readonly XmlQueryType NodeNotRtfS = TF.PrimeProduct(NodeNotRtf, XmlQueryCardinality.ZeroOrMore);
        public static readonly XmlQueryType NodeSDod = TF.PrimeProduct(NodeNotRtf, XmlQueryCardinality.ZeroOrMore);
        #endregion

        //-----------------------------------------------
        // Helpers
        //-----------------------------------------------

        /// <summary>
        /// Construct the union of two lists of prime XmlQueryTypes.  Types are added to "accumulator" as necessary to ensure
        /// it contains a superset of "types".
        /// </summary>
        private static List<XmlQueryType> PrimeChoice(List<XmlQueryType> accumulator, IList<XmlQueryType> types)
        {
            foreach (XmlQueryType sourceItem in types)
            {
                AddItemToChoice(accumulator, sourceItem);
            }
            return accumulator;
        }

        /// <summary>
        /// Adds itemType to a union. Returns false if new item is a subtype of one of the types in the list.
        /// </summary>
        private static void AddItemToChoice(List<XmlQueryType> accumulator, XmlQueryType itemType)
        {
            Debug.Assert(itemType.IsSingleton, "All types should be prime.");

            bool addToList = true;
            for (int i = 0; i < accumulator.Count; i++)
            {
                // If new prime is a subtype of existing prime, don't add it to the union
                if (itemType.IsSubtypeOf(accumulator[i]))
                {
                    return;
                }

                // If new prime is a subtype of existing prime, then replace the existing prime with new prime
                if (accumulator[i].IsSubtypeOf(itemType))
                {
                    if (addToList)
                    {
                        addToList = false;
                        accumulator[i] = itemType;
                    }
                    else
                    {
                        accumulator.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (addToList)
            {
                accumulator.Add(itemType);
            }
        }

        #region NodeKindToTypeCode
        /// <summary>
        /// Map XPathNodeType to XmlTypeCode.
        /// </summary>
        private static readonly XmlTypeCode[] s_nodeKindToTypeCode = {
            /* XPathNodeType.Root */                    XmlTypeCode.Document,
            /* XPathNodeType.Element */                 XmlTypeCode.Element,
            /* XPathNodeType.Attribute */               XmlTypeCode.Attribute,
            /* XPathNodeType.Namespace */               XmlTypeCode.Namespace,
            /* XPathNodeType.Text */                    XmlTypeCode.Text,
            /* XPathNodeType.SignificantWhitespace */   XmlTypeCode.Text,
            /* XPathNodeType.Whitespace */              XmlTypeCode.Text,
            /* XPathNodeType.ProcessingInstruction */   XmlTypeCode.ProcessingInstruction,
            /* XPathNodeType.Comment */                 XmlTypeCode.Comment,
            /* XPathNodeType.All */                     XmlTypeCode.Node,
        };
        #endregion

        //-----------------------------------------------
        // XmlQueryType Implementations
        //-----------------------------------------------

        /// <summary>
        /// Implementation of XmlQueryType for singleton types.
        /// </summary>
        private sealed class ItemType : XmlQueryType
        {
            // If you add new types here, add them to SpecialBuiltInItemTypes as well
            public static readonly XmlQueryType UntypedDocument;
            public static readonly XmlQueryType UntypedElement;
            public static readonly XmlQueryType UntypedAttribute;
            public static readonly XmlQueryType NodeNotRtf;

            private static readonly XmlQueryType[] s_builtInItemTypes;
            private static readonly XmlQueryType[] s_builtInItemTypesStrict;
            private static readonly XmlQueryType[] s_specialBuiltInItemTypes;

            private readonly XmlTypeCode _code;
            private readonly XmlQualifiedNameTest _nameTest;
            private readonly XmlSchemaType _schemaType;
            private readonly bool _isNillable;
            private readonly XmlNodeKindFlags _nodeKinds;
            private readonly bool _isStrict;
            private readonly bool _isNotRtf;

#pragma warning disable CA1810 // explicit static cctor
            /// <summary>
            /// Construct arrays of built-in types.
            /// </summary>
            static ItemType()
            {
#if DEBUG
                XmlTypeCode[] arrEnum = Enum.GetValues<XmlTypeCode>();
                Debug.Assert(arrEnum[arrEnum.Length - 1] == XmlTypeCode.DayTimeDuration,
                             "DayTimeDuration is no longer the last item in XmlTypeCode.  This code expects it to be.");
#endif

                int typeCount = (int)XmlTypeCode.DayTimeDuration + 1;

                s_builtInItemTypes = new XmlQueryType[typeCount];
                s_builtInItemTypesStrict = new XmlQueryType[typeCount];

                for (int i = 0; i < typeCount; i++)
                {
                    XmlTypeCode typeCode = (XmlTypeCode)i;

                    switch ((XmlTypeCode)i)
                    {
                        case XmlTypeCode.None:
                            s_builtInItemTypes[i] = ChoiceType.None;
                            s_builtInItemTypesStrict[i] = ChoiceType.None;
                            continue;

                        case XmlTypeCode.Item:
                        case XmlTypeCode.Node:
                            s_builtInItemTypes[i] = new ItemType(typeCode, XmlQualifiedNameTest.Wildcard, XmlSchemaComplexType.AnyType, false, false, false);
                            s_builtInItemTypesStrict[i] = s_builtInItemTypes[i];
                            break;

                        case XmlTypeCode.Document:
                        case XmlTypeCode.Element:
                        case XmlTypeCode.Namespace:
                        case XmlTypeCode.ProcessingInstruction:
                        case XmlTypeCode.Comment:
                        case XmlTypeCode.Text:
                            s_builtInItemTypes[i] = new ItemType(typeCode, XmlQualifiedNameTest.Wildcard, XmlSchemaComplexType.AnyType, false, false, true);
                            s_builtInItemTypesStrict[i] = s_builtInItemTypes[i];
                            break;

                        case XmlTypeCode.Attribute:
                            s_builtInItemTypes[i] = new ItemType(typeCode, XmlQualifiedNameTest.Wildcard, DatatypeImplementation.AnySimpleType, false, false, true);
                            s_builtInItemTypesStrict[i] = s_builtInItemTypes[i];
                            break;

                        case XmlTypeCode.AnyAtomicType:
                            s_builtInItemTypes[i] = new ItemType(typeCode, XmlQualifiedNameTest.Wildcard, DatatypeImplementation.AnyAtomicType, false, false, true);
                            s_builtInItemTypesStrict[i] = s_builtInItemTypes[i];
                            break;

                        case XmlTypeCode.UntypedAtomic:
                            // xdt:untypedAtomic is sealed, and therefore always strict
                            s_builtInItemTypes[i] = new ItemType(typeCode, XmlQualifiedNameTest.Wildcard, DatatypeImplementation.UntypedAtomicType, false, true, true);
                            s_builtInItemTypesStrict[i] = s_builtInItemTypes[i];
                            break;

                        default:
                            XmlSchemaType builtInType = XmlSchemaType.GetBuiltInSimpleType(typeCode);
                            s_builtInItemTypes[i] = new ItemType(typeCode, XmlQualifiedNameTest.Wildcard, builtInType, false, false, true);
                            s_builtInItemTypesStrict[i] = new ItemType(typeCode, XmlQualifiedNameTest.Wildcard, builtInType, false, true, true);
                            break;
                    }
                }

                UntypedDocument = new ItemType(XmlTypeCode.Document, XmlQualifiedNameTest.Wildcard, XmlSchemaComplexType.UntypedAnyType, false, false, true);
                UntypedElement = new ItemType(XmlTypeCode.Element, XmlQualifiedNameTest.Wildcard, XmlSchemaComplexType.UntypedAnyType, false, false, true);
                UntypedAttribute = new ItemType(XmlTypeCode.Attribute, XmlQualifiedNameTest.Wildcard, DatatypeImplementation.UntypedAtomicType, false, false, true);
                NodeNotRtf = new ItemType(XmlTypeCode.Node, XmlQualifiedNameTest.Wildcard, XmlSchemaComplexType.AnyType, false, false, true);

                s_specialBuiltInItemTypes = new XmlQueryType[4] { UntypedDocument, UntypedElement, UntypedAttribute, NodeNotRtf };
            }
#pragma warning restore CA1810

            /// <summary>
            /// Create ItemType from XmlTypeCode.
            /// </summary>
            public static XmlQueryType Create(XmlTypeCode code, bool isStrict)
            {
                // No objects need to be allocated, as corresponding ItemTypes for all type codes have been statically allocated
                if (isStrict)
                    return s_builtInItemTypesStrict[(int)code];

                return s_builtInItemTypes[(int)code];
            }

            /// <summary>
            /// Create ItemType from Xsd atomic type.
            /// </summary>
            public static XmlQueryType Create(XmlSchemaSimpleType schemaType, bool isStrict)
            {
                Debug.Assert(schemaType.Datatype!.Variety == XmlSchemaDatatypeVariety.Atomic, "List or Union Xsd types should have been handled by caller.");
                XmlTypeCode code = schemaType.Datatype.TypeCode;

                // If schemaType is a built-in type,
                if (schemaType == XmlSchemaType.GetBuiltInSimpleType(code))
                {
                    // Then use statically allocated type
                    return Create(code, isStrict);
                }

                // Otherwise, create a new type
                return new ItemType(code, XmlQualifiedNameTest.Wildcard, schemaType, false, isStrict, true);
            }

            /// <summary>
            /// Create Document, Element or Attribute with specified name test, content type and nillable.
            /// </summary>
            public static XmlQueryType Create(XmlTypeCode code, XmlQualifiedNameTest nameTest, XmlSchemaType contentType, bool isNillable)
            {
                // If this is a Document, Element, or Attribute,
                switch (code)
                {
                    case XmlTypeCode.Document:
                    case XmlTypeCode.Element:
                        if (nameTest.IsWildcard)
                        {
                            // Normalize document(*, xs:anyType), element(*, xs:anyType)
                            if (contentType == XmlSchemaComplexType.AnyType)
                                return Create(code, false);

                            // Normalize document(xs:untypedAny), element(*, xs:untypedAny)
                            if (contentType == XmlSchemaComplexType.UntypedAnyType)
                            {
                                Debug.Assert(!isNillable);
                                if (code == XmlTypeCode.Element)
                                    return UntypedElement;
                                if (code == XmlTypeCode.Document)
                                    return UntypedDocument;
                            }
                        }
                        // Create new ItemType
                        return new ItemType(code, nameTest, contentType, isNillable, false, true);

                    case XmlTypeCode.Attribute:
                        if (nameTest.IsWildcard)
                        {
                            // Normalize attribute(xs:anySimpleType)
                            if (contentType == DatatypeImplementation.AnySimpleType)
                                return Create(code, false);

                            // Normalize attribute(xs:untypedAtomic)
                            if (contentType == DatatypeImplementation.UntypedAtomicType)
                                return UntypedAttribute;
                        }
                        // Create new ItemType
                        return new ItemType(code, nameTest, contentType, isNillable, false, true);

                    default:
                        return Create(code, false);
                }
            }

            /// <summary>
            /// Private constructor.  Create methods should be used to create instances.
            /// </summary>
            private ItemType(XmlTypeCode code, XmlQualifiedNameTest nameTest, XmlSchemaType schemaType, bool isNillable, bool isStrict, bool isNotRtf)
            {
                Debug.Assert(nameTest != null, "nameTest cannot be null");
                Debug.Assert(schemaType != null, "schemaType cannot be null");
                _code = code;
                _nameTest = nameTest;
                _schemaType = schemaType;
                _isNillable = isNillable;
                _isStrict = isStrict;
                _isNotRtf = isNotRtf;

                Debug.Assert(!IsAtomicValue || schemaType.Datatype!.Variety == XmlSchemaDatatypeVariety.Atomic);

                _nodeKinds = code switch
                {
                    XmlTypeCode.Item => XmlNodeKindFlags.Any,
                    XmlTypeCode.Node => XmlNodeKindFlags.Any,
                    XmlTypeCode.Document => XmlNodeKindFlags.Document,
                    XmlTypeCode.Element => XmlNodeKindFlags.Element,
                    XmlTypeCode.Attribute => XmlNodeKindFlags.Attribute,
                    XmlTypeCode.Namespace => XmlNodeKindFlags.Namespace,
                    XmlTypeCode.ProcessingInstruction => XmlNodeKindFlags.PI,
                    XmlTypeCode.Comment => XmlNodeKindFlags.Comment,
                    XmlTypeCode.Text => XmlNodeKindFlags.Text,
                    _ => XmlNodeKindFlags.None,
                };
            }

            //-----------------------------------------------
            // Serialization
            //-----------------------------------------------

            /// <summary>
            /// Serialize the object to BinaryWriter.
            /// </summary>
            public override void GetObjectData(BinaryWriter writer)
            {
                sbyte code = (sbyte)_code;

                for (int idx = 0; idx < s_specialBuiltInItemTypes.Length; idx++)
                {
                    if ((object)this == (object)s_specialBuiltInItemTypes[idx])
                    {
                        code = (sbyte)~idx;
                        break;
                    }
                }

                writer.Write(code);

                if (0 <= code)
                {
                    Debug.Assert((object)this == (object)Create(_code, _isStrict), "Unknown type");
                    writer.Write(_isStrict);
                }
            }

            /// <summary>
            /// Deserialize the object from BinaryReader.
            /// </summary>
            public static XmlQueryType Create(BinaryReader reader)
            {
                sbyte code = reader.ReadSByte();

                if (0 <= code)
                    return Create((XmlTypeCode)code, /*isStrict:*/reader.ReadBoolean());
                else
                    return s_specialBuiltInItemTypes[~code];
            }

            //-----------------------------------------------
            // ItemType, OccurrenceIndicator Properties
            //-----------------------------------------------

            /// <summary>
            /// Return the TypeCode.
            /// </summary>
            public override XmlTypeCode TypeCode
            {
                get { return _code; }
            }

            /// <summary>
            /// Return the NameTest.
            /// </summary>
            public override XmlQualifiedNameTest NameTest
            {
                get { return _nameTest; }
            }

            /// <summary>
            /// Return the Xsd schema type.  This must be non-null for atomic value types.
            /// </summary>
            public override XmlSchemaType SchemaType
            {
                get { return _schemaType; }
            }

            /// <summary>
            /// Return the IsNillable.
            /// </summary>
            public override bool IsNillable
            {
                get { return _isNillable; }
            }

            /// <summary>
            /// Since this is always an atomic value type, NodeKinds = None.
            /// </summary>
            public override XmlNodeKindFlags NodeKinds
            {
                get { return _nodeKinds; }
            }

            /// <summary>
            /// Return flag indicating whether the dynamic type is guaranteed to be the same as the static type.
            /// </summary>
            public override bool IsStrict
            {
                get { return _isStrict; }
            }

            /// <summary>
            /// Return flag indicating whether this is not an Rtf.
            /// </summary>
            public override bool IsNotRtf
            {
                get { return _isNotRtf; }
            }

            /// <summary>
            /// Singleton types return false.
            /// </summary>
            public override bool IsDod
            {
                get { return false; }
            }

            /// <summary>
            /// Always return cardinality One.
            /// </summary>
            public override XmlQueryCardinality Cardinality
            {
                get { return XmlQueryCardinality.One; }
            }

            /// <summary>
            /// Prime of atomic value type is itself.
            /// </summary>
            public override XmlQueryType Prime
            {
                get { return this; }
            }

            //-----------------------------------------------
            // ListBase implementation
            //-----------------------------------------------

            /// <summary>
            /// AtomicValueType is only a composition of itself, rather than other smaller types.
            /// </summary>
            public override int Count
            {
                get { return 1; }
            }

            /// <summary>
            /// AtomicValueType is only a composition of itself, rather than other smaller types.
            /// </summary>
            public override XmlQueryType this[int index]
            {
                get
                {
                    if (index != 0)
                        throw new IndexOutOfRangeException();

                    return this;
                }
                set { throw new NotSupportedException(); }
            }
        }


        /// <summary>
        /// Implementation of XmlQueryType that composes a choice of various prime types.
        /// </summary>
        private sealed class ChoiceType : XmlQueryType
        {
            public static readonly XmlQueryType None = new ChoiceType(new List<XmlQueryType>());

            private readonly XmlTypeCode _code;
            private readonly XmlSchemaType? _schemaType;
            private readonly XmlNodeKindFlags _nodeKinds;
            private readonly List<XmlQueryType> _members;

            /// <summary>
            /// Create choice between node kinds.
            /// </summary>
            public static XmlQueryType Create(XmlNodeKindFlags nodeKinds)
            {
                List<XmlQueryType> members;

                // If exactly one kind is set, then create singleton ItemType
                if (Bits.ExactlyOne((uint)nodeKinds))
                    return ItemType.Create(s_nodeKindToTypeCode[Bits.LeastPosition((uint)nodeKinds)], false);

                members = new List<XmlQueryType>();
                while (nodeKinds != XmlNodeKindFlags.None)
                {
                    members.Add(ItemType.Create(s_nodeKindToTypeCode[Bits.LeastPosition((uint)nodeKinds)], false));

                    nodeKinds = (XmlNodeKindFlags)Bits.ClearLeast((uint)nodeKinds);
                }

                return Create(members);
            }

            /// <summary>
            /// Create choice containing the specified list of types.
            /// </summary>
            public static XmlQueryType Create(List<XmlQueryType> members)
            {
                if (members.Count == 0)
                    return None;

                if (members.Count == 1)
                    return members[0];

                return new ChoiceType(members);
            }

            /// <summary>
            /// Private constructor.  Create methods should be used to create instances.
            /// </summary>
            private ChoiceType(List<XmlQueryType> members)
            {
                Debug.Assert(members != null && members.Count != 1, "ChoiceType must contain a list with 0 or >1 types.");

                _members = members;

                // Compute supertype of all member types
                for (int i = 0; i < members.Count; i++)
                {
                    XmlQueryType t = members[i];
                    Debug.Assert(t.Cardinality == XmlQueryCardinality.One, "ChoiceType member types must be prime types.");

                    // Summarize the union of member types as a single type
                    if (_code == XmlTypeCode.None)
                    {
                        // None combined with member type is the member type
                        _code = t.TypeCode;
                        _schemaType = t.SchemaType;
                    }
                    else if (IsNode && t.IsNode)
                    {
                        // Node combined with node is node
                        if (_code == t.TypeCode)
                        {
                            // Element or attribute combined with element or attribute can be summarized as element(*, XmlSchemaComplexType.AnyType) or attribute(*, DatatypeImplementation.AnySimpleType)
                            if (_code == XmlTypeCode.Element)
                                _schemaType = XmlSchemaComplexType.AnyType;
                            else if (_code == XmlTypeCode.Attribute)
                                _schemaType = DatatypeImplementation.AnySimpleType;
                        }
                        else
                        {
                            _code = XmlTypeCode.Node;
                            _schemaType = null;
                        }
                    }
                    else if (IsAtomicValue && t.IsAtomicValue)
                    {
                        // Atomic value combined with atomic value is atomic value
                        _code = XmlTypeCode.AnyAtomicType;
                        _schemaType = DatatypeImplementation.AnyAtomicType;
                    }
                    else
                    {
                        // Else we'll summarize types as Item
                        _code = XmlTypeCode.Item;
                        _schemaType = null;
                    }

                    // Always track union of node kinds
                    _nodeKinds |= t.NodeKinds;
                }
            }

            private static readonly XmlTypeCode[] s_nodeKindToTypeCode = {
                /* None */          XmlTypeCode.None,
                /* Document */      XmlTypeCode.Document,
                /* Element */       XmlTypeCode.Element,
                /* Attribute */     XmlTypeCode.Attribute,
                /* Text */          XmlTypeCode.Text,
                /* Comment */       XmlTypeCode.Comment,
                /* PI */            XmlTypeCode.ProcessingInstruction,
                /* Namespace */     XmlTypeCode.Namespace,
            };

            //-----------------------------------------------
            // Serialization
            //-----------------------------------------------

            /// <summary>
            /// Serialize the object to BinaryWriter.
            /// </summary>
            public override void GetObjectData(BinaryWriter writer)
            {
                writer.Write(_members.Count);
                for (int i = 0; i < _members.Count; i++)
                {
                    TF.Serialize(writer, _members[i]);
                }
            }

            /// <summary>
            /// Deserialize the object from BinaryReader.
            /// </summary>
            public static XmlQueryType Create(BinaryReader reader)
            {
                int length = reader.ReadInt32();
                List<XmlQueryType> members = new List<XmlQueryType>(length);
                for (int i = 0; i < length; i++)
                {
                    members.Add(TF.Deserialize(reader));
                }
                return Create(members);
            }

            //-----------------------------------------------
            // ItemType, OccurrenceIndicator Properties
            //-----------------------------------------------

            /// <summary>
            /// Return a type code which is a supertype of all member types.
            /// </summary>
            public override XmlTypeCode TypeCode
            {
                get { return _code; }
            }

            /// <summary>
            /// Return the NameTest.
            /// </summary>
            public override XmlQualifiedNameTest NameTest
            {
                get { return XmlQualifiedNameTest.Wildcard; }
            }

            /// <summary>
            /// Return an Xsd schema type which is a supertype of all member types.
            /// </summary>
            public override XmlSchemaType SchemaType
            {
                get
                {
                    // https://github.com/dotnet/runtime/issues/44148
                    //     Per documentation of base class we should not be returning null here
                    //     Currently we return null for some type codes (i.e. Item, Node)
                    //     but doc says we should be returning AnyType
                    //     which presumably means XmlSchemaComplexType.AnyType
                    return _schemaType!;
                }
            }

            /// <summary>
            /// Return the IsNillable.
            /// </summary>
            public override bool IsNillable
            {
                get { return false; }
            }

            /// <summary>
            /// Return a set of NodeKinds which is the union of all member node kinds.
            /// </summary>
            public override XmlNodeKindFlags NodeKinds
            {
                get { return _nodeKinds; }
            }

            /// <summary>
            /// Choice types are always non-strict, except for the empty choice.
            /// </summary>
            public override bool IsStrict
            {
                get { return _members.Count == 0; }
            }

            /// <summary>
            /// Return true if every type in the choice is not an Rtf.
            /// </summary>
            public override bool IsNotRtf
            {
                get
                {
                    for (int i = 0; i < _members.Count; i++)
                    {
                        if (!_members[i].IsNotRtf)
                            return false;
                    }
                    return true;
                }
            }

            /// <summary>
            /// Singleton types return false.
            /// </summary>
            public override bool IsDod
            {
                get { return false; }
            }

            /// <summary>
            /// Always return cardinality none or one.
            /// </summary>
            public override XmlQueryCardinality Cardinality
            {
                get { return TypeCode == XmlTypeCode.None ? XmlQueryCardinality.None : XmlQueryCardinality.One; }
            }

            /// <summary>
            /// Prime of union type is itself.
            /// </summary>
            public override XmlQueryType Prime
            {
                get { return this; }
            }

            //-----------------------------------------------
            // ListBase implementation
            //-----------------------------------------------

            /// <summary>
            /// Return the number of union member types.
            /// </summary>
            public override int Count
            {
                get { return _members.Count; }
            }

            /// <summary>
            /// Return a union member type by index.
            /// </summary>
            public override XmlQueryType this[int index]
            {
                get { return _members[index]; }
                set { throw new NotSupportedException(); }
            }
        }


        /// <summary>
        /// Implementation of XmlQueryType that modifies the cardinality of a composed type.
        /// </summary>
        private sealed class SequenceType : XmlQueryType
        {
            public static readonly XmlQueryType Zero = new SequenceType(ChoiceType.None, XmlQueryCardinality.Zero);

            private readonly XmlQueryType _prime;
            private readonly XmlQueryCardinality _card;

            /// <summary>
            /// Create sequence type from prime and cardinality.
            /// </summary>
            public static XmlQueryType Create(XmlQueryType prime, XmlQueryCardinality card)
            {
                Debug.Assert(prime != null, "SequenceType can only modify the cardinality of a non-null XmlQueryType.");
                Debug.Assert(prime.IsSingleton, "Prime type must have cardinality one.");

                if (prime.TypeCode == XmlTypeCode.None)
                {
                    // If cardinality includes zero, then return (None, Zero), else return (None, None).
                    return XmlQueryCardinality.Zero <= card ? Zero : None;
                }

                // Normalize sequences with these cardinalities: None, Zero, One

                if (card == XmlQueryCardinality.None)
                {
                    return None;
                }
                else if (card == XmlQueryCardinality.Zero)
                {
                    return Zero;
                }
                else if (card == XmlQueryCardinality.One)
                {
                    return prime;
                }

                return new SequenceType(prime, card);
            }

            /// <summary>
            /// Private constructor.  Create methods should be used to create instances.
            /// </summary>
            private SequenceType(XmlQueryType prime, XmlQueryCardinality card)
            {
                _prime = prime;
                _card = card;
            }

            //-----------------------------------------------
            // Serialization
            //-----------------------------------------------

            /// <summary>
            /// Serialize the object to BinaryWriter.
            /// </summary>
            public override void GetObjectData(BinaryWriter writer)
            {
                writer.Write(this.IsDod);
                if (this.IsDod)
                    return;

                TF.Serialize(writer, _prime);
                _card.GetObjectData(writer);
            }

            /// <summary>
            /// Deserialize the object from BinaryReader.
            /// </summary>
            public static XmlQueryType Create(BinaryReader reader)
            {
                if (reader.ReadBoolean())
                    return TF.NodeSDod;

                XmlQueryType prime = TF.Deserialize(reader);
                XmlQueryCardinality card = new XmlQueryCardinality(reader);
                return Create(prime, card);
            }

            //-----------------------------------------------
            // ItemType, OccurrenceIndicator Properties
            //-----------------------------------------------

            /// <summary>
            /// Return the TypeCode of the prime type.
            /// </summary>
            public override XmlTypeCode TypeCode
            {
                get { return _prime.TypeCode; }
            }

            /// <summary>
            /// Return the NameTest of the prime type
            /// </summary>
            public override XmlQualifiedNameTest NameTest
            {
                get { return _prime.NameTest; }
            }

            /// <summary>
            /// Return the Xsd schema type of the prime type.
            /// </summary>
            public override XmlSchemaType SchemaType
            {
                get { return _prime.SchemaType; }
            }

            /// <summary>
            /// Return the IsNillable of the prime type
            /// </summary>
            public override bool IsNillable
            {
                get { return _prime.IsNillable; }
            }

            /// <summary>
            /// Return the NodeKinds of the prime type.
            /// </summary>
            public override XmlNodeKindFlags NodeKinds
            {
                get { return _prime.NodeKinds; }
            }

            /// <summary>
            /// Return the IsStrict flag of the prime type.
            /// </summary>
            public override bool IsStrict
            {
                get { return _prime.IsStrict; }
            }

            /// <summary>
            /// Return the IsNotRtf flag of the prime type.
            /// </summary>
            public override bool IsNotRtf
            {
                get { return _prime.IsNotRtf; }
            }

            /// <summary>
            /// Only NodeSDod type returns true.
            /// </summary>
            public override bool IsDod
            {
                get { return (object)this == (object)NodeSDod; }
            }

            /// <summary>
            /// Return the modified cardinality.
            /// </summary>
            public override XmlQueryCardinality Cardinality
            {
                get { return _card; }
            }

            /// <summary>
            /// Return prime of sequence type.
            /// </summary>
            public override XmlQueryType Prime
            {
                get { return _prime; }
            }

            //-----------------------------------------------
            // ListBase implementation
            //-----------------------------------------------

            /// <summary>
            /// Return the Count of the prime type.
            /// </summary>
            public override int Count
            {
                get { return _prime.Count; }
            }

            /// <summary>
            /// Return the parts of the prime type.
            /// </summary>
            public override XmlQueryType this[int index]
            {
                get { return _prime[index]; }
                set { throw new NotSupportedException(); }
            }
        }

        /// <summary>
        /// Create a Node XmlQueryType having an XSD content type.
        /// </summary>
        /// <param name="nameTest">The name test.</param>
        /// <param name="kind">unless kind is Root, Element, or Attribute, "contentType" is ignored</param>
        /// <param name="contentType">content type of the node</param>
        /// <param name="isNillable">Whether it's nillable.</param>
        /// <returns>the node type</returns>
        public static XmlQueryType Type(XPathNodeType kind, XmlQualifiedNameTest nameTest, XmlSchemaType contentType, bool isNillable)
        {
            return ItemType.Create(s_nodeKindToTypeCode[(int)kind], nameTest, contentType, isNillable);
        }

        #region Serialization
        /// <summary>
        /// Check if the given type can be serialized.
        /// </summary>
        [Conditional("DEBUG")]
        public static void CheckSerializability(XmlQueryType type)
        {
            type.GetObjectData(new BinaryWriter(Stream.Null));
        }

        /// <summary>
        /// Serialize XmlQueryType to BinaryWriter.
        /// </summary>
        public static void Serialize(BinaryWriter writer, XmlQueryType type)
        {
            sbyte subtypeId;

            if (type.GetType() == typeof(ItemType))
                subtypeId = 0;
            else if (type.GetType() == typeof(ChoiceType))
                subtypeId = 1;
            else if (type.GetType() == typeof(SequenceType))
                subtypeId = 2;
            else
            {
                Debug.Fail($"Don't know how to serialize {type.GetType()}");
                subtypeId = -1;
            }

            writer.Write(subtypeId);
            type.GetObjectData(writer);
        }

        /// <summary>
        /// Deserialize XmlQueryType from BinaryReader.
        /// </summary>
        public static XmlQueryType Deserialize(BinaryReader reader)
        {
            switch (reader.ReadByte())
            {
                case 0: return ItemType.Create(reader);
                case 1: return ChoiceType.Create(reader);
                case 2: return SequenceType.Create(reader);
                default:
                    Debug.Fail("Unexpected XmlQueryType's subtype id");
                    return null;
            }
        }
        #endregion

#if NEVER   // Remove from code since we don't use and FxCop complains.  May re-add later.
        private XmlSchemaSet schemaSet;

        /// <summary>
        /// Create an XmlQueryType having an XSD name test, content type and nillable.
        /// </summary>
        /// <param name="code">unless code is Document, Element, or Attribute, "contentType" is ignored</param>
        /// <param name="nameTest">name test on the node</param>
        /// <param name="contentType">content type of the node</param>
        /// <param name="isNillable">nillable property</param>
        /// <returns>the item type</returns>
        public XmlQueryType Type(XmlTypeCode code, XmlQualifiedNameTest nameTest, XmlSchemaType contentType, bool isNillable) {
            return ItemType.Create(code, nameTest, contentType, isNillable);
        }

        /// <summary>
        /// Create a strict XmlQueryType from the source.
        /// </summary>
        /// <param name="source">source type</param>
        /// <returns>strict type if the source is atomic, the source otherwise</returns>
        public XmlQueryType StrictType(XmlQueryType source) {
            if (source.IsAtomicValue && source.Count == 1)
                return SequenceType.Create(ItemType.Create((XmlSchemaSimpleType)source.SchemaType, true), source.Cardinality);

            return source;
        }

        /// <summary>
        /// Create an XmlQueryType from an XmlTypeCode and cardinality.
        /// </summary>
        /// <param name="code">the type code of the item</param>
        /// <param name="card">cardinality</param>
        /// <returns>build-in type type</returns>
        public XmlQueryType Type(XmlTypeCode code, XmlQueryCardinality card) {
            return SequenceType.Create(ItemType.Create(code, false), card);
        }

        /// <summary>
        /// Create an XmlQueryType having an XSD name test, content type, nillable and cardinality.
        /// </summary>
        /// <param name="code">unless code is Document, Element, or Attribute, "contentType" is ignored</param>
        /// <param name="nameTest">name test on the node</param>
        /// <param name="contentType">content type of the node</param>
        /// <param name="isNillable">nillable property</param>
        /// <param name="card">cardinality</param>
        /// <returns>the item type</returns>
        public XmlQueryType Type(XmlTypeCode code, XmlQualifiedNameTest nameTest, XmlSchemaType contentType, bool isNillable, XmlQueryCardinality card) {
            return SequenceType.Create(ItemType.Create(code, nameTest, contentType, isNillable), card);
        }

        /// <summary>
        /// Construct the intersection of two XmlQueryTypes
        /// </summary>
        /// <param name="left">the left type</param>
        /// <param name="right">the right type</param>
        /// <returns>the intersection type</returns>
        public XmlQueryType Intersect(XmlQueryType left, XmlQueryType right) {
            return SequenceType.Create(ChoiceType.Create(PrimeIntersect(left, right)), left.Cardinality & right.Cardinality);
        }

        /// <summary>
        /// Construct the intersection of several XmlQueryTypes
        /// </summary>
        /// <param name="types">the list of types</param>
        /// <returns>the intersection type</returns>
        public XmlQueryType Intersect(params XmlQueryType[] types) {
            if (types.Length == 0)
                return None;
            else if (types.Length == 1)
                return types[0];

            // Intersect each type with next type
            List<XmlQueryType> list = PrimeIntersect(types[0], types[1]);
            XmlQueryCardinality card = types[0].Cardinality & types[1].Cardinality;

            for (int i = 2; i < types.Length; i++) {
                list = PrimeIntersect(list, types[i]);
                card &= types[i].Cardinality;
            }

            return SequenceType.Create(ChoiceType.Create(list), card);
        }

        /// <summary>
        /// Construct the intersection of two lists of prime XmlQueryTypes.
        /// </summary>
        private List<XmlQueryType> PrimeIntersect(IList<XmlQueryType> left, IList<XmlQueryType> right) {
            List<XmlQueryType> list = new List<XmlQueryType>();

            foreach (XmlQueryType leftItem in left) {
                foreach (XmlQueryType rightItem in right) {
                    XmlQueryType intersection = IntersectItemTypes(leftItem, rightItem);
                    // Do not add none1 to a list
                    if ((object)intersection != (object)None) {
                        list.Add(intersection);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Converts type of sequence of items to type of sequence of atomic value
        //  See http://www.w3.org/TR/2004/xquery-semantics/#jd_data for the detailed description
        /// </summary>
        /// <param name="source">source type</param>
        /// <returns>type of the sequence of atomic values</returns>
        public XmlQueryType DataOn(XmlQueryType source) {
            List<XmlQueryType> list = new List<XmlQueryType>();
            XmlQueryCardinality card = XmlQueryCardinality.None;

            foreach (XmlQueryType sourceItem  in source) {
                switch (sourceItem.TypeCode) {
                case XmlTypeCode.Item:
                case XmlTypeCode.Node:
                    AddItemToChoice(list, AnyAtomicType);
                    card = XmlQueryCardinality.ZeroOrMore;
                    break;
                case XmlTypeCode.Document:
                case XmlTypeCode.Text:
                    AddItemToChoice(list, UntypedAtomic);
                    card |= XmlQueryCardinality.One;
                    break;
                case XmlTypeCode.Comment:
                case XmlTypeCode.ProcessingInstruction:
                    AddItemToChoice(list, String);
                    card |= XmlQueryCardinality.One;
                    break;
                case XmlTypeCode.Element:
                case XmlTypeCode.Attribute:
                    XmlSchemaType sourceSchemaType = sourceItem.SchemaType;
                    if (sourceSchemaType == XmlSchemaComplexType.UntypedAnyType || sourceSchemaType == DatatypeImplementation.UntypedAtomicType) {
                        AddItemToChoice(list, UntypedAtomic);
                        card |= XmlQueryCardinality.One;
                    }
                    else if (sourceSchemaType == XmlSchemaComplexType.AnyType || sourceSchemaType == DatatypeImplementation.AnySimpleType) {
                        AddItemToChoice(list, AnyAtomicType);
                        card = XmlQueryCardinality.ZeroOrMore;
                    }
                    else {
                        if (sourceSchemaType.Datatype == null) {
                            // Complex content adds anyAtomicType* if mixed
                            XmlSchemaComplexType complexType = (XmlSchemaComplexType)sourceItem.SchemaType;
                            if (complexType.ContentType == XmlSchemaContentType.Mixed) {
                                AddItemToChoice(list, AnyAtomicType);
                                card = XmlQueryCardinality.ZeroOrMore;
                            }
                            else {
                                // Error if mixed is false
                                return null;
                            }
                        }
                        else {
                            // Simple content
                            XmlSchemaType schemaType = sourceItem.SchemaType;

                            // Go up the tree until it's a simple type
                            while (schemaType is XmlSchemaComplexType) {
                                schemaType = schemaType.BaseXmlSchemaType;
                            }

                            // Calculate XmlQueryType from XmlSchemaSimpleType
                            XmlQueryType atomicSeq = Type((XmlSchemaSimpleType)schemaType, false);

                            // Add prime to a choice
                            // It doen't have to be a single item!
                            PrimeChoice(list, atomicSeq.Prime);

                            // Add cardinality to a choice
                            card |= atomicSeq.Cardinality;
                        }
                        // Add ? if nillable
                        if (sourceItem.IsNillable) {
                            card *= XmlQueryCardinality.ZeroOrOne;
                        }
                    }
                    break;
                case XmlTypeCode.Namespace:
                    card |= XmlQueryCardinality.Zero;
                    break;
                case XmlTypeCode.None:
                    break;
                default:
                    Debug.Assert(sourceItem.IsAtomicValue, "missed case for a node");
                    AddItemToChoice(list, sourceItem);
                    card |= XmlQueryCardinality.One;
                    break;
                }
            }
            return PrimeProduct(ChoiceType.Create(list), source.Cardinality * card);
        }

        /// <summary>
        /// Filter type of node sequence with a type (filter)
        /// </summary>
        /// <param name="source">source type</param>
        /// <param name="filter">type filter</param>
        /// <returns>type of the filtered node sequence</returns>
        public XmlQueryType FilterOf(XmlQueryType source, XmlQueryType filter) {
            Debug.Assert(filter.IsNode && filter.Count == 1 && filter.IsSingleton);
            List<XmlQueryType> list = new List<XmlQueryType>();
            XmlQueryCardinality card = XmlQueryCardinality.None;

            foreach (XmlQueryType sourceItem in source) {
                card |= AddFilteredPrime(list, sourceItem, filter, true);
            }
            // Make sure that cardinality is at least Zero
            return PrimeProduct(ChoiceType.Create(list), source.Cardinality * card);
        }

        /// <summary>
        /// For the type of node sequence calculate type of children filtered with a type (filter)
        /// </summary>
        /// <param name="source">source type</param>
        /// <param name="filter">type filter</param>
        /// <returns>type of the children node sequence</returns>
        public XmlQueryType ChildrenOf(XmlQueryType source, XmlQueryType filter) {
            Debug.Assert(filter.IsNode && filter.Count == 1 && filter.IsSingleton);
            List<XmlQueryType> list = new List<XmlQueryType>();
            XmlQueryCardinality card = XmlQueryCardinality.None;

            foreach (XmlQueryType sourceItem in source) {
                switch (sourceItem.TypeCode) {
                case XmlTypeCode.Node:
                case XmlTypeCode.Document:
                case XmlTypeCode.Element:
                    XmlSchemaType sourceSchemaType = sourceItem.SchemaType;
                    XmlQueryCardinality itemCard = XmlQueryCardinality.None;
                    // Only element and document can have children
                    if (sourceSchemaType == XmlSchemaComplexType.UntypedAnyType) {
                        // content of xdt:untypedAny is element(*, xdt:untypedAny)*
                        itemCard = (AddFilteredPrime(list, UntypedElement, filter) * XmlQueryCardinality.ZeroOrMore);
                        itemCard += AddFilteredPrime(list, Text, filter);
                    }
                    else if (sourceSchemaType.Datatype != null) {
                        // Text is the only child node simple type can have
                        itemCard = AddFilteredPrime(list, Text, filter, true) * XmlQueryCardinality.ZeroOrOne;
                    }
                    else {
                        // Complex content
                        XmlSchemaComplexType complexType = (XmlSchemaComplexType)sourceSchemaType;
                        itemCard = AddChildParticle(list, complexType.ContentTypeParticle, filter);
                        if (complexType.ContentType == XmlSchemaContentType.Mixed) {
                            itemCard += AddFilteredPrime(list, Text, filter);
                        }
                    }
                    itemCard += AddFilteredPrime(list, PI, filter);
                    itemCard += AddFilteredPrime(list, Comment, filter);
                    card |= itemCard;
                    break;

                case XmlTypeCode.Attribute:
                case XmlTypeCode.ProcessingInstruction:
                case XmlTypeCode.Comment:
                case XmlTypeCode.Namespace:
                case XmlTypeCode.Text:
                    card |= XmlQueryCardinality.Zero;
                    break;

                default:
                    Debug.Assert(sourceItem.IsAtomicValue, "missed case for a node");
                    return null;
                }
            }
            // Make sure that cardinality is at least Zero
            return PrimeProduct(ChoiceType.Create(list), source.Cardinality * card);
        }

        /// <summary>
        /// For the type of node sequence calculate type of attributes filtered with a type (filter)
        /// </summary>
        /// <param name="source">source type</param>
        /// <param name="filter">type filter</param>
        /// <returns>type of the children node sequence</returns>
        public XmlQueryType AttributesOf(XmlQueryType source, XmlQueryType filter) {
            Debug.Assert(filter.IsNode && filter.Count == 1 && filter.IsSingleton);
            List<XmlQueryType> list = new List<XmlQueryType>();
            XmlQueryCardinality card = XmlQueryCardinality.None;

            foreach (XmlQueryType sourceItem in source) {
                switch (sourceItem.TypeCode) {
                case XmlTypeCode.Node:
                case XmlTypeCode.Element:
                    XmlSchemaType sourceSchemaType = sourceItem.SchemaType;
                    if (sourceSchemaType == XmlSchemaComplexType.UntypedAnyType) {
                        // attributes of xdt:untypedAny are attribute(*, xdt:untypedAtomic)*
                        card |= AddFilteredPrime(list, UntypedAttribute, filter) * XmlQueryCardinality.ZeroOrOne;
                    }
                    else {
                        // Only complex type can have attributes
                        XmlSchemaComplexType type = sourceSchemaType as XmlSchemaComplexType;
                        if (type != null) {
                            card |= AddAttributes(list, type.AttributeUses, type.AttributeWildcard, filter);
                        }
                    }
                    break;

                case XmlTypeCode.Document:
                case XmlTypeCode.Attribute:
                case XmlTypeCode.ProcessingInstruction:
                case XmlTypeCode.Comment:
                case XmlTypeCode.Namespace:
                case XmlTypeCode.Text:
                    card |= XmlQueryCardinality.Zero;
                    break;

                default:
                    Debug.Assert(sourceItem.IsAtomicValue, "missed case for a node");
                    return null;
                }
            }
            // Make sure that cardinality is at least Zero
            return PrimeProduct(ChoiceType.Create(list), source.Cardinality * card);
        }

        /// <summary>
        /// For the type of node sequence calculate type of parent filtered with a type (filter)
        /// </summary>
        /// <param name="source">source type</param>
        /// <param name="filter">type filter</param>
        /// <returns>type of the parent node sequence</returns>
        public XmlQueryType ParentOf(XmlQueryType source, XmlQueryType filter) {
            Debug.Assert(filter.IsNode && filter.Count == 1 && filter.IsSingleton);
            List<XmlQueryType> list = new List<XmlQueryType>();
            XmlQueryCardinality card = XmlQueryCardinality.None;

            foreach (XmlQueryType sourceItem  in source) {
                switch (sourceItem.TypeCode) {
                case XmlTypeCode.Node:
                case XmlTypeCode.ProcessingInstruction:
                case XmlTypeCode.Comment:
                case XmlTypeCode.Text:
                case XmlTypeCode.Element:
                    if (schemaSet == null) {
                        card |= AddFilteredPrime(list, UntypedDocument, filter) * XmlQueryCardinality.ZeroOrOne;
                        card |= AddFilteredPrime(list, UntypedElement, filter) * XmlQueryCardinality.ZeroOrOne;
                    }
                    else {
                        card |= AddFilteredPrime(list, Document, filter) * XmlQueryCardinality.ZeroOrOne;
                        card |= AddFilteredPrime(list, Element, filter) * XmlQueryCardinality.ZeroOrOne;
                    }
                    break;

                case XmlTypeCode.Namespace:
                case XmlTypeCode.Attribute:
                    if (schemaSet == null) {
                        card |= (AddFilteredPrime(list, UntypedElement, filter) * XmlQueryCardinality.ZeroOrOne);
                    }
                    else {
                        card |= (AddFilteredPrime(list, Element, filter) * XmlQueryCardinality.ZeroOrOne);
                    }
                    break;

                case XmlTypeCode.Document:
                    card |= XmlQueryCardinality.Zero;
                    break;

                default:
                    Debug.Assert(sourceItem.IsAtomicValue, "missed case for a node");
                    break;
                }
            }
            // Make sure that cardinality is at least Zero
            return PrimeProduct(ChoiceType.Create(list), source.Cardinality * card);
        }

        /// <summary>
        /// For the type of node sequence calculate type of descendants filtered with a type (filter)
        /// </summary>
        /// <param name="source">source type</param>
        /// <param name="filter">type filter</param>
        /// <returns>type of the descendants node sequence</returns>
        public XmlQueryType DescendantsOf(XmlQueryType source, XmlQueryType filter) {
            Debug.Assert(filter.IsNode && filter.Count == 1 && filter.IsSingleton);
            List<XmlQueryType> list = new List<XmlQueryType>();
            XmlQueryCardinality card = XmlQueryCardinality.None;

            foreach (XmlQueryType sourceItem  in source) {
                switch (sourceItem.TypeCode) {
                case XmlTypeCode.Node:
                case XmlTypeCode.Document:
                case XmlTypeCode.Element:
                    XmlQueryCardinality itemCard = XmlQueryCardinality.None;
                    if ((filter.NodeKinds & (XmlNodeKindFlags.Element | XmlNodeKindFlags.Text)) != 0) {
                        Dictionary<XmlQualifiedName, XmlQueryCardinality> allTypes = new Dictionary<XmlQualifiedName, XmlQueryCardinality>();
                        XmlSchemaType sourceSchemaType = sourceItem.SchemaType;
                        if (sourceSchemaType == null) {
                            Debug.Assert(sourceItem.TypeCode == XmlTypeCode.Node);
                            sourceSchemaType = XmlSchemaComplexType.AnyType;
                        }
                        itemCard = AddElementOrTextDescendants(list, allTypes, sourceSchemaType, filter);
                    }
                    itemCard += AddFilteredPrime(list, PI, filter);
                    itemCard += AddFilteredPrime(list, Comment, filter);
                    card |= itemCard;
                    break;

                case XmlTypeCode.Attribute:
                case XmlTypeCode.ProcessingInstruction:
                case XmlTypeCode.Comment:
                case XmlTypeCode.Namespace:
                case XmlTypeCode.Text:
                    card |= XmlQueryCardinality.Zero;
                    break;

                default:
                    Debug.Assert(sourceItem.IsAtomicValue, "missed case for a node");
                        break;
                }
            }
            // Make sure that cardinality is at least Zero
            return PrimeProduct(ChoiceType.Create(list), source.Cardinality * card);
        }

        /// <summary>
        /// For the type of node sequence calculate type of ancestor filtered with a type (filter)
        /// </summary>
        /// <param name="source">source type</param>
        /// <param name="filter">type filter</param>
        /// <returns>type of the ancestor node sequence</returns>
        public XmlQueryType AncestorsOf(XmlQueryType source, XmlQueryType filter) {
            Debug.Assert(filter.IsNode && filter.Count == 1 && filter.IsSingleton);
            List<XmlQueryType> list = new List<XmlQueryType>();
            XmlQueryCardinality card = XmlQueryCardinality.None;

            foreach (XmlQueryType sourceItem  in source) {
                switch (sourceItem.TypeCode) {
                case XmlTypeCode.Node:
                case XmlTypeCode.ProcessingInstruction:
                case XmlTypeCode.Comment:
                case XmlTypeCode.Text:
                case XmlTypeCode.Element:
                case XmlTypeCode.Namespace:
                case XmlTypeCode.Attribute:
                    if (schemaSet == null) {
                        card |= (AddFilteredPrime(list, UntypedDocument, filter) * XmlQueryCardinality.ZeroOrOne)
                                + (AddFilteredPrime(list, UntypedElement, filter) * XmlQueryCardinality.ZeroOrMore);
                    }
                    else {
                        card |= (AddFilteredPrime(list, Document, filter) * XmlQueryCardinality.ZeroOrOne)
                                + (AddFilteredPrime(list, Element, filter) * XmlQueryCardinality.ZeroOrMore);
                    }
                    break;

                case XmlTypeCode.Document:
                    card |= XmlQueryCardinality.Zero;
                    break;

                default:
                    Debug.Assert(sourceItem.IsAtomicValue, "missed case for a node");
                    break;
                }
            }
            // Make sure that cardinality is at least Zero
            return PrimeProduct(ChoiceType.Create(list), source.Cardinality * card);
        }

        private XmlQueryCardinality AddAttributes(List<XmlQueryType> list, XmlSchemaObjectTable attributeUses, XmlSchemaAnyAttribute attributeWildcard, XmlQueryType filter) {
            XmlQueryCardinality card = XmlQueryCardinality.Zero;
            if (attributeWildcard != null) {
                XmlSchemaType attributeSchemaType = attributeWildcard.ProcessContentsCorrect == XmlSchemaContentProcessing.Skip ? DatatypeImplementation.UntypedAtomicType : DatatypeImplementation.AnySimpleType;

                // wildcard will match more then one attribute
                switch (attributeWildcard.NamespaceList.Type) {
                case NamespaceList.ListType.Set:
                    foreach (string ns in attributeWildcard.NamespaceList.Enumerate) {
                        card += AddFilteredPrime(list, CreateAttributeType(ns, false, attributeSchemaType), filter);
                    }
                    break;
                case NamespaceList.ListType.Other:
                    card += AddFilteredPrime(list, CreateAttributeType(attributeWildcard.NamespaceList.Excluded, true, attributeSchemaType), filter);
                    break;
                case NamespaceList.ListType.Any:
                default:
                    card +=  AddFilteredPrime(list, attributeWildcard.ProcessContentsCorrect == XmlSchemaContentProcessing.Skip ? UntypedAttribute : Attribute, filter);
                    break;
                }
                // Always optional
                card *= XmlQueryCardinality.ZeroOrOne;
            }
            foreach (XmlSchemaAttribute attribute in attributeUses.Values) {
                XmlQueryCardinality cardAttr = AddFilteredPrime(list, CreateAttributeType(attribute), filter);
                if (cardAttr != XmlQueryCardinality.Zero) {
                    Debug.Assert(cardAttr == XmlQueryCardinality.ZeroOrOne || cardAttr == XmlQueryCardinality.One);
                    card += (attribute.Use == XmlSchemaUse.Optional ? XmlQueryCardinality.ZeroOrOne : cardAttr);
                }
            }
            return card;
        }

        private XmlQueryType CreateAttributeType(XmlSchemaAttribute attribute) {
            return ItemType.Create(XmlTypeCode.Attribute, XmlQualifiedNameTest.New(attribute.QualifiedName), attribute.AttributeSchemaType, false);
        }

        private XmlQueryType CreateAttributeType(string ns, bool exclude, XmlSchemaType schemaType) {
            return ItemType.Create(XmlTypeCode.Attribute, XmlQualifiedNameTest.New(ns, exclude), schemaType, false);
        }

        private XmlQueryCardinality AddDescendantParticle(List<XmlQueryType> list, Dictionary<XmlQualifiedName, XmlQueryCardinality> allTypes, XmlSchemaParticle particle, XmlQueryType filter) {
            XmlQueryCardinality card = XmlQueryCardinality.None;
            XmlSchemaElement element = particle as XmlSchemaElement;
            if (element != null) {
                // Single element
                XmlQueryType elementType = CreateElementType(element);

                // Add it
                card = AddFilteredPrime(list, elementType, filter);

                // Descend
                card += AddElementOrTextDescendants(list, allTypes, elementType.SchemaType, filter);
            }
            else {
                XmlSchemaAny any = particle as XmlSchemaAny;
                if (any != null) {
                    // Descendants of any
                    card = AddFilteredPrime(list, Element, filter);
                }
                else {
                    XmlSchemaGroupBase group = particle as XmlSchemaGroupBase;
                    if (group.Items.Count != 0) {
                        if (particle is XmlSchemaChoice) {
                            foreach (XmlSchemaParticle p in group.Items) {
                                card |= AddDescendantParticle(list, allTypes, p, filter);
                            }
                        }
                        else { // Sequence and  All
                            foreach (XmlSchemaParticle p in group.Items) {
                                card += AddDescendantParticle(list, allTypes, p, filter);
                            }
                        }
                    }
                }
            }
            return card * CardinalityOfParticle(particle);
        }

        private XmlQueryCardinality AddElementOrTextDescendants(List<XmlQueryType> list,
            Dictionary<XmlQualifiedName, XmlQueryCardinality> allTypes, XmlSchemaType sourceSchemaType, XmlQueryType filter) {
            XmlQueryCardinality card = XmlQueryCardinality.None;
            if (sourceSchemaType == XmlSchemaComplexType.UntypedAnyType) {
                card = AddFilteredPrime(list, UntypedElement, filter) * XmlQueryCardinality.ZeroOrMore;
                card += AddFilteredPrime(list, Text, filter);
            }
            else if (sourceSchemaType.Datatype != null) {
                // Text is the only child node simple content of complext type
                card = AddFilteredPrime(list, Text, filter, true) * XmlQueryCardinality.ZeroOrOne;
            }
            else {
                // Complex content
                XmlSchemaComplexType complexType = (XmlSchemaComplexType)sourceSchemaType;
                if (complexType.QualifiedName.IsEmpty || !allTypes.TryGetValue(complexType.QualifiedName, out card)) {
                    allTypes[complexType.QualifiedName] = XmlQueryCardinality.ZeroOrMore; // take care of left recursion
                    card = AddDescendantParticle(list, allTypes, complexType.ContentTypeParticle, filter);
                    allTypes[complexType.QualifiedName] = card;  //set correct card
                    if (complexType.ContentType == XmlSchemaContentType.Mixed) {
                        card += AddFilteredPrime(list, Text, filter);
                    }
                }
            }
            return card;
        }

        /// <summary>
        /// Create type based on an XmlSchemaElement
        /// </summary>
        private XmlQueryType CreateElementType(XmlSchemaElement element) {
            return ItemType.Create(XmlTypeCode.Element, XmlQualifiedNameTest.New(element.QualifiedName), element.ElementSchemaType, element.IsNillable);
        }

        /// <summary>
        /// Create type based on a wildcard
        /// </summary>
        private XmlQueryType CreateElementType(string ns, bool exclude, XmlSchemaType schemaType) {
            return ItemType.Create(XmlTypeCode.Element, XmlQualifiedNameTest.New(ns, exclude), schemaType, false);
        }

        /// <summary>
        ///  Descend though the content model
        /// </summary>
        private XmlQueryCardinality AddChildParticle(List<XmlQueryType> list, XmlSchemaParticle particle, XmlQueryType filter) {
            XmlQueryCardinality card = XmlQueryCardinality.None;
            XmlSchemaElement element = particle as XmlSchemaElement;
            if (element != null) {
                // Single element
                card = AddFilteredPrime(list, CreateElementType(element), filter);
            }
            else {
                // XmlSchemaAny matches more then one element
                XmlSchemaAny any = particle as XmlSchemaAny;
                if (any != null) {
                    XmlSchemaType elementSchemaType = any.ProcessContentsCorrect == XmlSchemaContentProcessing.Skip ? XmlSchemaComplexType.UntypedAnyType : XmlSchemaComplexType.AnyType;
                    switch (any.NamespaceList.Type) {
                    case NamespaceList.ListType.Set:
                        // Add a separate type for each namespace in the list
                        foreach (string ns in any.NamespaceList.Enumerate) {
                            card |= AddFilteredPrime(list, CreateElementType(ns, false, elementSchemaType), filter);
                        }
                        break;
                    case NamespaceList.ListType.Other:
                        // Add ##other
                        card = AddFilteredPrime(list, CreateElementType(any.NamespaceList.Excluded, true, elementSchemaType), filter);
                        break;
                    case NamespaceList.ListType.Any:
                    default:
                        // Add ##any
                        card = AddFilteredPrime(list, any.ProcessContentsCorrect == XmlSchemaContentProcessing.Skip ? UntypedElement : Element, filter);
                        break;
                    }
                }
                else {
                    //  recurse into particle group
                    XmlSchemaGroupBase group = particle as XmlSchemaGroupBase;
                    if (group.Items.Count != 0) {
                        if (particle is XmlSchemaChoice) {
                            foreach (XmlSchemaParticle p in group.Items) {
                                card |= AddChildParticle(list, p, filter);
                            }
                        }
                        else { // Sequence and  All
                            foreach (XmlSchemaParticle p in group.Items) {
                                card += AddChildParticle(list, p, filter);
                            }
                        }
                    }
                }
            }
            return card * CardinalityOfParticle(particle);
        }

        /// <summary>
        /// Apply filter an item type, add the result to a list, return cardinality
        /// </summary>
        private XmlQueryCardinality AddFilteredPrime(List<XmlQueryType> list, XmlQueryType source, XmlQueryType filter) {
            return AddFilteredPrime(list, source, filter, false);

        }
        private XmlQueryCardinality AddFilteredPrime(List<XmlQueryType> list, XmlQueryType source, XmlQueryType filter, bool forseSingle) {
            Debug.Assert(source.IsNode && source.IsSingleton);
            Debug.Assert(filter.IsNode && filter.IsSingleton);

            // Intersect types
            XmlQueryType intersection = IntersectItemTypes(source, filter);
            if ((object)intersection == (object)None) {
                return XmlQueryCardinality.Zero;
            }
            AddItemToChoice(list, intersection);
            // In the case of forseSingle - filtering all nodes behave as singletones
            XmlTypeCode typeCode = (forseSingle ? XmlTypeCode.Node : intersection.TypeCode);
            switch (typeCode) {
            case XmlTypeCode.Node:
            case XmlTypeCode.Document:
            case XmlTypeCode.Element:
                // Filter can result in empty sequence if filter is not wider then source
                if (intersection == source)
                    return XmlQueryCardinality.One;
                else
                    return  XmlQueryCardinality.ZeroOrOne;

            case XmlTypeCode.Attribute:
                    // wildcard attribute matches more then one node
                if (!intersection.NameTest.IsSingleName)
                    return XmlQueryCardinality.ZeroOrMore;
                else if (intersection == source)
                    return XmlQueryCardinality.One;
                else
                    return  XmlQueryCardinality.ZeroOrOne;
            case XmlTypeCode.Comment:
            case XmlTypeCode.Text:
            case XmlTypeCode.ProcessingInstruction:
            case XmlTypeCode.Namespace:
                return XmlQueryCardinality.ZeroOrMore;

            default:
                Debug.Fail($"Unexpected type code {typeCode}");
                return XmlQueryCardinality.None;
            }
        }

        /// <summary>
        /// Construct the intersection of two lists of prime XmlQueryTypes.
        /// </summary>
        private XmlQueryType IntersectItemTypes(XmlQueryType left, XmlQueryType right) {
            Debug.Assert(left.Count == 1 && left.IsSingleton, "left should be an item");
            Debug.Assert(right.Count == 1 && right.IsSingleton, "right should be an item");
            if (left.TypeCode == right.TypeCode && (left.NodeKinds & (XmlNodeKindFlags.Document | XmlNodeKindFlags.Element | XmlNodeKindFlags.Attribute)) != 0) {
                if (left.TypeCode == XmlTypeCode.Node) {
                    return left;
                }
                // Intersect name tests
                XmlQualifiedNameTest nameTest = left.NameTest.Intersect(right.NameTest);

                // Intersect types
                XmlSchemaType type = XmlSchemaType.IsDerivedFrom(left.SchemaType, right.SchemaType, /* except:*/XmlSchemaDerivationMethod.Empty) ? left.SchemaType :
                    XmlSchemaType.IsDerivedFrom(right.SchemaType, left.SchemaType, /* except:*/XmlSchemaDerivationMethod.Empty) ? right.SchemaType : null;
                bool isNillable = left.IsNillable && right.IsNillable;

                if ((object)nameTest == (object)left.NameTest && type == left.SchemaType && isNillable == left.IsNillable) {
                    // left is a subtype of right return left
                    return left;
                }
                else if ((object)nameTest == (object)right.NameTest && type == right.SchemaType && isNillable == right.IsNillable) {
                    // right is a subtype of left return right
                    return right;
                }
                else if (nameTest != null && type != null) {
                    // create a new type
                    return ItemType.Create(left.TypeCode, nameTest, type, isNillable);
                }
            }
            else if (left.IsSubtypeOf(right)) {
                // left is a subset of right, so left is in the intersection
                return left;
            }
            else if (right.IsSubtypeOf(left)) {
                // right is a subset of left, so right is in the intersection
                return right;
            }
            return None;
        }

        /// <summary>
        /// Convert particle occurrence range into cardinality
        /// </summary>
        private XmlQueryCardinality CardinalityOfParticle(XmlSchemaParticle particle) {
            if (particle.MinOccurs == decimal.Zero) {
                if (particle.MaxOccurs == decimal.Zero) {
                    return XmlQueryCardinality.Zero;
                }
                else if (particle.MaxOccurs == decimal.One) {
                    return XmlQueryCardinality.ZeroOrOne;
                }
                else {
                    return XmlQueryCardinality.ZeroOrMore;
                }
            }
            else {
                if (particle.MaxOccurs == decimal.One) {
                    return XmlQueryCardinality.One;
                }
                else {
                    return XmlQueryCardinality.OneOrMore;
                }
            }
        }
#endif
    }
}
