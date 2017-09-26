// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Xsl
{
    /// <summary>
    /// XmlQueryType contains static type information that describes the structure and possible values of dynamic
    /// instances of the Xml data model.
    ///
    /// Every XmlQueryType is composed of a Prime type and a cardinality.  The Prime type itself may be a union
    /// between several item types.  The XmlQueryType IList<XmlQueryType/> implementation allows callers
    /// to enumerate the item types.  Other properties expose other information about the type.
    /// </summary>
    internal abstract class XmlQueryType : ListBase<XmlQueryType>
    {
        private static readonly BitMatrix s_typeCodeDerivation;
        private int _hashCode;


        //-----------------------------------------------
        // Static Constructor
        //-----------------------------------------------
        static XmlQueryType()
        {
            s_typeCodeDerivation = new BitMatrix(s_baseTypeCodes.Length);

            // Build derivation matrix
            for (int i = 0; i < s_baseTypeCodes.Length; i++)
            {
                int nextAncestor = i;

                while (true)
                {
                    s_typeCodeDerivation[i, nextAncestor] = true;
                    if ((int)s_baseTypeCodes[nextAncestor] == nextAncestor)
                        break;

                    nextAncestor = (int)s_baseTypeCodes[nextAncestor];
                }
            }
        }


        //-----------------------------------------------
        // ItemType, OccurrenceIndicator Properties
        //-----------------------------------------------

        /// <summary>
        /// Static data type code.  The dynamic type is guaranteed to be this type or a subtype of this code.
        /// This type code includes support for XQuery types that are not part of Xsd, such as Item,
        /// Node, AnyAtomicType, and Comment.
        /// </summary>
        public abstract XmlTypeCode TypeCode { get; }

        /// <summary>
        /// Set of allowed names for element, document{element}, attribute and PI
        /// Returns XmlQualifiedName.Wildcard for all other types
        /// </summary>
        public abstract XmlQualifiedNameTest NameTest { get; }

        /// <summary>
        /// Static Xsd schema type.  The dynamic type is guaranteed to be this type or a subtype of this type.
        /// SchemaType will follow these rules:
        ///   1. If TypeCode is an atomic type code, then SchemaType will be the corresponding non-null simple type
        ///   2. If TypeCode is Element or Attribute, then SchemaType will be the non-null content type
        ///   3. If TypeCode is Item, Node, Comment, PI, Text, Document, Namespace, None, then SchemaType will be AnyType
        /// </summary>
        public abstract XmlSchemaType SchemaType { get; }

        /// <summary>
        /// Permits the element or document{element} node to have the nilled property.
        /// Returns false for all other types
        /// </summary>
        public abstract bool IsNillable { get; }

        /// <summary>
        /// This property is always XmlNodeKindFlags.None unless TypeCode = XmlTypeCode.Node, in which case this
        /// property lists all node kinds that instances of this type may be.
        /// </summary>
        public abstract XmlNodeKindFlags NodeKinds { get; }

        /// <summary>
        /// If IsStrict is true, then the dynamic type is guaranteed to be the exact same as the static type, and
        /// will therefore never be a subtype of the static type.
        /// </summary>
        public abstract bool IsStrict { get; }

        /// <summary>
        /// This property specifies the possible cardinalities that instances of this type may have.
        /// </summary>
        public abstract XmlQueryCardinality Cardinality { get; }

        /// <summary>
        /// This property returns this type's Prime type, which is always cardinality One.
        /// </summary>
        public abstract XmlQueryType Prime { get; }

        /// <summary>
        /// True if dynamic data type of all items in this sequence is guaranteed to be not a subtype of Rtf.
        /// </summary>
        public abstract bool IsNotRtf { get; }

        /// <summary>
        /// True if items in the sequence are guaranteed to be nodes in document order with no duplicates.
        /// </summary>
        public abstract bool IsDod { get; }

        /// <summary>
        /// The XmlValueConverter maps each XmlQueryType to various Clr types which are capable of representing it.
        /// </summary>
        public abstract XmlValueConverter ClrMapping { get; }


        //-----------------------------------------------
        // Type Operations
        //-----------------------------------------------

        /// <summary>
        /// Returns true if every possible dynamic instance of this type is also an instance of "baseType".
        /// </summary>
        public bool IsSubtypeOf(XmlQueryType baseType)
        {
            XmlQueryType thisPrime, basePrime;

            // Check cardinality sub-typing rules
            if (!(Cardinality <= baseType.Cardinality) || (!IsDod && baseType.IsDod))
                return false;

            if (!IsDod && baseType.IsDod)
                return false;

            // Check early for common case that two types are the same object
            thisPrime = Prime;
            basePrime = baseType.Prime;
            if ((object)thisPrime == (object)basePrime)
                return true;

            // Check early for common case that two prime types are item types
            if (thisPrime.Count == 1 && basePrime.Count == 1)
                return thisPrime.IsSubtypeOfItemType(basePrime);

            // Check that each item type in this type is a subtype of some item type in "baseType"
            foreach (XmlQueryType thisItem in thisPrime)
            {
                bool match = false;

                foreach (XmlQueryType baseItem in basePrime)
                {
                    if (thisItem.IsSubtypeOfItemType(baseItem))
                    {
                        match = true;
                        break;
                    }
                }

                if (match == false)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if a dynamic instance (type None never has an instance) of this type can never be a subtype of "baseType".
        /// </summary>
        public bool NeverSubtypeOf(XmlQueryType baseType)
        {
            // Check cardinalities
            if (Cardinality.NeverSubset(baseType.Cardinality))
                return true;

            // If both this type and "other" type might be empty, it doesn't matter what the prime types are
            if (MaybeEmpty && baseType.MaybeEmpty)
                return false;

            // None is subtype of every other type
            if (Count == 0)
                return false;

            // Check item types
            foreach (XmlQueryType typThis in this)
            {
                foreach (XmlQueryType typThat in baseType)
                {
                    if (typThis.HasIntersectionItemType(typThat))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Strongly-typed Equals that returns true if this type and "that" type are equivalent.
        /// </summary>
        public bool Equals(XmlQueryType that)
        {
            if (that == null)
                return false;

            // Check cardinality and DocOrderDistinct property
            if (Cardinality != that.Cardinality || IsDod != that.IsDod)
                return false;

            // Check early for common case that two types are the same object
            XmlQueryType thisPrime = Prime;
            XmlQueryType thatPrime = that.Prime;
            if ((object)thisPrime == (object)thatPrime)
                return true;

            // Check that count of item types is equal
            if (thisPrime.Count != thatPrime.Count)
                return false;

            // Check early for common case that two prime types are item types
            if (thisPrime.Count == 1)
            {
                return (thisPrime.TypeCode == thatPrime.TypeCode &&
                        thisPrime.NameTest == thatPrime.NameTest &&
                        thisPrime.SchemaType == thatPrime.SchemaType &&
                        thisPrime.IsStrict == thatPrime.IsStrict &&
                        thisPrime.IsNotRtf == thatPrime.IsNotRtf);
            }


            // Check that each item type in this type is equal to some item type in "baseType"
            // (string | int) should be the same type as (int | string)
            foreach (XmlQueryType thisItem in this)
            {
                bool match = false;

                foreach (XmlQueryType thatItem in that)
                {
                    if (thisItem.TypeCode == thatItem.TypeCode &&
                        thisItem.NameTest == thatItem.NameTest &&
                        thisItem.SchemaType == thatItem.SchemaType &&
                        thisItem.IsStrict == thatItem.IsStrict &&
                        thisItem.IsNotRtf == thatItem.IsNotRtf)
                    {
                        // Found match so proceed to next type
                        match = true;
                        break;
                    }
                }

                if (match == false)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Overload == operator to call Equals rather than do reference equality.
        /// </summary>
        public static bool operator ==(XmlQueryType left, XmlQueryType right)
        {
            if ((object)left == null)
                return ((object)right == null);

            return left.Equals(right);
        }

        /// <summary>
        /// Overload != operator to call Equals rather than do reference inequality.
        /// </summary>
        public static bool operator !=(XmlQueryType left, XmlQueryType right)
        {
            if ((object)left == null)
                return ((object)right != null);

            return !left.Equals(right);
        }


        //-----------------------------------------------
        // Convenience Properties
        //-----------------------------------------------

        /// <summary>
        /// True if dynamic cardinality of this sequence is guaranteed to be 0.
        /// </summary>
        public bool IsEmpty
        {
            get { return Cardinality <= XmlQueryCardinality.Zero; }
        }

        /// <summary>
        /// True if dynamic cardinality of this sequence is guaranteed to be 1.
        /// </summary>
        public bool IsSingleton
        {
            get { return Cardinality <= XmlQueryCardinality.One; }
        }

        /// <summary>
        /// True if dynamic cardinality of this sequence might be 0.
        /// </summary>
        public bool MaybeEmpty
        {
            get { return XmlQueryCardinality.Zero <= Cardinality; }
        }

        /// <summary>
        /// True if dynamic cardinality of this sequence might be >1.
        /// </summary>
        public bool MaybeMany
        {
            get { return XmlQueryCardinality.More <= Cardinality; }
        }

        /// <summary>
        /// True if dynamic data type of all items in this sequence is guaranteed to be a subtype of Node.
        /// Equivalent to calling IsSubtypeOf(TypeFactory.NodeS).
        /// </summary>
        public bool IsNode
        {
            get { return (s_typeCodeToFlags[(int)TypeCode] & TypeFlags.IsNode) != 0; }
        }

        /// <summary>
        /// True if dynamic data type of all items in this sequence is guaranteed to be a subtype of AnyAtomicType.
        /// Equivalent to calling IsSubtypeOf(TypeFactory.AnyAtomicTypeS).
        /// </summary>
        public bool IsAtomicValue
        {
            get { return (s_typeCodeToFlags[(int)TypeCode] & TypeFlags.IsAtomicValue) != 0; }
        }

        /// <summary>
        /// True if dynamic data type of all items in this sequence is guaranteed to be a subtype of Decimal, Double, or Float.
        /// Equivalent to calling IsSubtypeOf(TypeFactory.NumericS).
        /// </summary>
        public bool IsNumeric
        {
            get { return (s_typeCodeToFlags[(int)TypeCode] & TypeFlags.IsNumeric) != 0; }
        }


        //-----------------------------------------------
        // System.Object implementation
        //-----------------------------------------------

        /// <summary>
        /// True if "obj" is an XmlQueryType, and this type is the exact same static type.
        /// </summary>
        public override bool Equals(object obj)
        {
            XmlQueryType that = obj as XmlQueryType;

            if (that == null)
                return false;

            return Equals(that);
        }

        /// <summary>
        /// Return hash code of this instance.
        /// </summary>
        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                int hash;
                XmlSchemaType schemaType;

                hash = (int)TypeCode;
                schemaType = SchemaType;

                unchecked
                {
                    if (schemaType != null)
                        hash += (hash << 7) ^ schemaType.GetHashCode();

                    hash += (hash << 7) ^ (int)NodeKinds;
                    hash += (hash << 7) ^ Cardinality.GetHashCode();
                    hash += (hash << 7) ^ (IsStrict ? 1 : 0);

                    // Mix hash code a bit more
                    hash -= hash >> 17;
                    hash -= hash >> 11;
                    hash -= hash >> 5;
                }

                // Save hashcode.  Don't save 0, so that it won't ever be recomputed.
                _hashCode = (hash == 0) ? 1 : hash;
            }

            return _hashCode;
        }

        /// <summary>
        /// Return a user-friendly string representation of the XmlQueryType.
        /// </summary>
        public override string ToString()
        {
            return ToString("G");
        }

        /// <summary>
        /// Return a string representation of the XmlQueryType using the specified format.  The following formats are
        /// supported:
        ///
        ///   "G" (General): This is the default mode, and is used if no other format is recognized.  This format is
        ///                  easier to read than the canonical format, since it excludes redundant information.
        ///                  (e.g. element instead of element(*, xs:anyType))
        ///
        ///   "X" (XQuery): Return the canonical XQuery representation, which excludes Qil specific information and
        ///                 includes extra, redundant information, such as fully specified types.
        ///                 (e.g. element(*, xs:anyType) instead of element)
        ///
        ///   "S" (Serialized): This format is used to serialize parts of the type which can be serialized easily, in
        ///                     a format that is easy to parse.  Only the cardinality, type code, and strictness flag
        ///                     are serialized.  User-defined type information and element/attribute content types
        ///                     are lost.
        ///                     (e.g. One;Attribute|String|Int;true)
        ///
        /// </summary>
        public string ToString(string format)
        {
            string[] sa;
            StringBuilder sb;
            bool isXQ;

            if (format == "S")
            {
                sb = new StringBuilder();
                sb.Append(Cardinality.ToString(format));
                sb.Append(';');

                for (int i = 0; i < Count; i++)
                {
                    if (i != 0)
                        sb.Append("|");
                    sb.Append(this[i].TypeCode.ToString());
                }

                sb.Append(';');
                sb.Append(IsStrict);
                return sb.ToString();
            }

            isXQ = (format == "X");

            if (Cardinality == XmlQueryCardinality.None)
            {
                return "none";
            }
            else if (Cardinality == XmlQueryCardinality.Zero)
            {
                return "empty";
            }

            sb = new StringBuilder();

            switch (Count)
            {
                case 0:
                    // This assert depends on the way we are going to represent None
                    // Debug.Assert(false);
                    sb.Append("none");
                    break;
                case 1:
                    sb.Append(this[0].ItemTypeToString(isXQ));
                    break;
                default:
                    sa = new string[Count];
                    for (int i = 0; i < Count; i++)
                        sa[i] = this[i].ItemTypeToString(isXQ);

                    Array.Sort(sa);

                    sb = new StringBuilder();
                    sb.Append('(');
                    sb.Append(sa[0]);
                    for (int i = 1; i < sa.Length; i++)
                    {
                        sb.Append(" | ");
                        sb.Append(sa[i]);
                    }

                    sb.Append(')');
                    break;
            }

            sb.Append(Cardinality.ToString());

            if (!isXQ && IsDod)
                sb.Append('#');

            return sb.ToString();
        }


        //-----------------------------------------------
        // Serialization
        //-----------------------------------------------

        /// <summary>
        /// Serialize the object to BinaryWriter.
        /// </summary>
        public abstract void GetObjectData(BinaryWriter writer);

        //-----------------------------------------------
        // Helpers
        //-----------------------------------------------

        /// <summary>
        /// Returns true if this item type is a subtype of another item type.
        /// </summary>
        private bool IsSubtypeOfItemType(XmlQueryType baseType)
        {
            Debug.Assert(Count == 1 && IsSingleton, "This method should only be called for item types.");
            Debug.Assert(baseType.Count == 1 && baseType.IsSingleton, "This method should only be called for item types.");
            Debug.Assert(!IsDod && !baseType.IsDod, "Singleton types may not have DocOrderDistinct property");
            XmlSchemaType baseSchemaType = baseType.SchemaType;

            if (TypeCode != baseType.TypeCode)
            {
                // If "baseType" is strict, then IsSubtypeOf must be false
                if (baseType.IsStrict)
                    return false;

                // If type codes are not the same, then IsSubtypeOf can return true *only* if "baseType" is a built-in type
                XmlSchemaType builtInType = XmlSchemaType.GetBuiltInSimpleType(baseType.TypeCode);
                if (builtInType != null && baseSchemaType != builtInType)
                    return false;

                // Now check whether TypeCode is derived from baseType.TypeCode
                return s_typeCodeDerivation[TypeCode, baseType.TypeCode];
            }
            else if (baseType.IsStrict)
            {
                // only atomic values can be strict
                Debug.Assert(IsAtomicValue && baseType.IsAtomicValue);

                // If schema types are not the same, then IsSubtype is false if "baseType" is strict
                return IsStrict && SchemaType == baseSchemaType;
            }
            else
            {
                // Otherwise, check derivation tree
                return (IsNotRtf || !baseType.IsNotRtf) && NameTest.IsSubsetOf(baseType.NameTest) &&
                       (baseSchemaType == XmlSchemaComplexType.AnyType || XmlSchemaType.IsDerivedFrom(SchemaType, baseSchemaType, /* except:*/XmlSchemaDerivationMethod.Empty)) &&
                       (!IsNillable || baseType.IsNillable);
            }
        }

        /// <summary>
        /// Returns true if the intersection between this item type and "other" item type is not empty.
        /// </summary>
        private bool HasIntersectionItemType(XmlQueryType other)
        {
            Debug.Assert(this.Count == 1 && this.IsSingleton, "this should be an item");
            Debug.Assert(other.Count == 1 && other.IsSingleton, "other should be an item");

            if (this.TypeCode == other.TypeCode && (this.NodeKinds & (XmlNodeKindFlags.Document | XmlNodeKindFlags.Element | XmlNodeKindFlags.Attribute)) != 0)
            {
                if (this.TypeCode == XmlTypeCode.Node)
                    return true;

                // Intersect name tests
                if (!this.NameTest.HasIntersection(other.NameTest))
                    return false;

                if (!XmlSchemaType.IsDerivedFrom(this.SchemaType, other.SchemaType, /* except:*/XmlSchemaDerivationMethod.Empty) &&
                    !XmlSchemaType.IsDerivedFrom(other.SchemaType, this.SchemaType, /* except:*/XmlSchemaDerivationMethod.Empty))
                {
                    return false;
                }

                return true;
            }
            else if (this.IsSubtypeOf(other) || other.IsSubtypeOf(this))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return the string representation of an item type (cannot be a union or a sequence).
        /// </summary>
        private string ItemTypeToString(bool isXQ)
        {
            string s;
            Debug.Assert(Count == 1, "Do not pass a Union type to this method.");
            Debug.Assert(IsSingleton, "Do not pass a Sequence type to this method.");

            if (IsNode)
            {
                // Map TypeCode to string
                s = s_typeNames[(int)TypeCode];

                switch (TypeCode)
                {
                    case XmlTypeCode.Document:
                        if (!isXQ)
                            goto case XmlTypeCode.Element;

                        s += "{(element" + NameAndType(true) + "?&text?&comment?&processing-instruction?)*}";
                        break;

                    case XmlTypeCode.Element:
                    case XmlTypeCode.Attribute:
                        s += NameAndType(isXQ);
                        break;
                }
            }
            else if (SchemaType != XmlSchemaComplexType.AnyType)
            {
                // Get QualifiedName from SchemaType
                if (SchemaType.QualifiedName.IsEmpty)
                    s = "<:" + s_typeNames[(int)TypeCode];
                else
                    s = QNameToString(SchemaType.QualifiedName);
            }
            else
            {
                // Map TypeCode to string
                s = s_typeNames[(int)TypeCode];
            }

            if (!isXQ && IsStrict)
                s += "=";

            return s;
        }

        /// <summary>
        /// Return "(name-test, type-name)" for this type.  If isXQ is false, normalize xs:anySimpleType and
        /// xs:anyType to "*".
        /// </summary>
        private string NameAndType(bool isXQ)
        {
            string nodeName = NameTest.ToString();
            string typeName = "*";

            if (SchemaType.QualifiedName.IsEmpty)
            {
                typeName = "typeof(" + nodeName + ")";
            }
            else
            {
                if (isXQ || (SchemaType != XmlSchemaComplexType.AnyType && SchemaType != DatatypeImplementation.AnySimpleType))
                    typeName = QNameToString(SchemaType.QualifiedName);
            }

            if (IsNillable)
                typeName += " nillable";

            // Normalize "(*, *)" to ""
            if (nodeName == "*" && typeName == "*")
                return "";

            return "(" + nodeName + ", " + typeName + ")";
        }

        /// <summary>
        /// Convert an XmlQualifiedName to a string, using somewhat different rules than XmlQualifiedName.ToString():
        ///   1. Empty QNames are assumed to be wildcard names, so return "*"
        ///   2. Recognize the built-in xs: and xdt: namespaces and print the short prefix rather than the long namespace
        ///   3. Use brace characters "{", "}" around the namespace portion of the QName
        /// </summary>
        private static string QNameToString(XmlQualifiedName name)
        {
            if (name.IsEmpty)
            {
                return "*";
            }
            else if (name.Namespace.Length == 0)
            {
                return name.Name;
            }
            else if (name.Namespace == XmlReservedNs.NsXs)
            {
                return "xs:" + name.Name;
            }
            else if (name.Namespace == XmlReservedNs.NsXQueryDataType)
            {
                return "xdt:" + name.Name;
            }
            else
            {
                return "{" + name.Namespace + "}" + name.Name;
            }
        }

        #region TypeFlags
        private enum TypeFlags
        {
            None = 0,
            IsNode = 1,
            IsAtomicValue = 2,
            IsNumeric = 4,
        }
        #endregion

        #region  TypeCodeToFlags
        private static readonly TypeFlags[] s_typeCodeToFlags = {
                /* XmlTypeCode.None                  */ TypeFlags.IsNode | TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.Item                  */ TypeFlags.None,
                /* XmlTypeCode.Node                  */ TypeFlags.IsNode,
                /* XmlTypeCode.Document              */ TypeFlags.IsNode,
                /* XmlTypeCode.Element               */ TypeFlags.IsNode,
                /* XmlTypeCode.Attribute             */ TypeFlags.IsNode,
                /* XmlTypeCode.Namespace             */ TypeFlags.IsNode,
                /* XmlTypeCode.ProcessingInstruction */ TypeFlags.IsNode,
                /* XmlTypeCode.Comment               */ TypeFlags.IsNode,
                /* XmlTypeCode.Text                  */ TypeFlags.IsNode,
                /* XmlTypeCode.AnyAtomicType         */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.UntypedAtomic         */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.String                */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Boolean               */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Decimal               */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.Float                 */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.Double                */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.Duration              */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.DateTime              */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Time                  */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Date                  */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.GYearMonth            */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.GYear                 */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.GMonthDay             */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.GDay                  */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.GMonth                */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.HexBinary             */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Base64Binary          */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.AnyUri                */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.QName                 */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Notation              */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.NormalizedString      */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Token                 */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Language              */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.NmToken               */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Name                  */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.NCName                */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Id                    */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Idref                 */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Entity                */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.Integer               */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.NonPositiveInteger    */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.NegativeInteger       */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.Long                  */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.Int                   */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.Short                 */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.Byte                  */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.NonNegativeInteger    */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.UnsignedLong          */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.UnsignedInt           */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.UnsignedShort         */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.UnsignedByte          */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.PositiveInteger       */ TypeFlags.IsAtomicValue | TypeFlags.IsNumeric,
                /* XmlTypeCode.YearMonthDuration     */ TypeFlags.IsAtomicValue,
                /* XmlTypeCode.DayTimeDuration       */ TypeFlags.IsAtomicValue,
        };

        private static readonly XmlTypeCode[] s_baseTypeCodes = {
            /* None                        */ XmlTypeCode.None,
            /* Item                        */ XmlTypeCode.Item,
            /* Node                        */ XmlTypeCode.Item,
            /* Document                    */ XmlTypeCode.Node,
            /* Element                     */ XmlTypeCode.Node,
            /* Attribute                   */ XmlTypeCode.Node,
            /* Namespace                   */ XmlTypeCode.Node,
            /* ProcessingInstruction       */ XmlTypeCode.Node,
            /* Comment                     */ XmlTypeCode.Node,
            /* Text                        */ XmlTypeCode.Node,
            /* AnyAtomicType               */ XmlTypeCode.Item,
            /* UntypedAtomic               */ XmlTypeCode.AnyAtomicType,
            /* String                      */ XmlTypeCode.AnyAtomicType,
            /* Boolean                     */ XmlTypeCode.AnyAtomicType,
            /* Decimal                     */ XmlTypeCode.AnyAtomicType,
            /* Float                       */ XmlTypeCode.AnyAtomicType,
            /* Double                      */ XmlTypeCode.AnyAtomicType,
            /* Duration                    */ XmlTypeCode.AnyAtomicType,
            /* DateTime                    */ XmlTypeCode.AnyAtomicType,
            /* Time                        */ XmlTypeCode.AnyAtomicType,
            /* Date                        */ XmlTypeCode.AnyAtomicType,
            /* GYearMonth                  */ XmlTypeCode.AnyAtomicType,
            /* GYear                       */ XmlTypeCode.AnyAtomicType,
            /* GMonthDay                   */ XmlTypeCode.AnyAtomicType,
            /* GDay                        */ XmlTypeCode.AnyAtomicType,
            /* GMonth                      */ XmlTypeCode.AnyAtomicType,
            /* HexBinary                   */ XmlTypeCode.AnyAtomicType,
            /* Base64Binary                */ XmlTypeCode.AnyAtomicType,
            /* AnyUri                      */ XmlTypeCode.AnyAtomicType,
            /* QName                       */ XmlTypeCode.AnyAtomicType,
            /* Notation                    */ XmlTypeCode.AnyAtomicType,
            /* NormalizedString            */ XmlTypeCode.String,
            /* Token                       */ XmlTypeCode.NormalizedString,
            /* Language                    */ XmlTypeCode.Token,
            /* NmToken                     */ XmlTypeCode.Token,
            /* Name                        */ XmlTypeCode.Token,
            /* NCName                      */ XmlTypeCode.Name,
            /* Id                          */ XmlTypeCode.NCName,
            /* Idref                       */ XmlTypeCode.NCName,
            /* Entity                      */ XmlTypeCode.NCName,
            /* Integer                     */ XmlTypeCode.Decimal,
            /* NonPositiveInteger          */ XmlTypeCode.Integer,
            /* NegativeInteger             */ XmlTypeCode.NonPositiveInteger,
            /* Long                        */ XmlTypeCode.Integer,
            /* Int                         */ XmlTypeCode.Long,
            /* Short                       */ XmlTypeCode.Int,
            /* Byte                        */ XmlTypeCode.Short,
            /* NonNegativeInteger          */ XmlTypeCode.Integer,
            /* UnsignedLong                */ XmlTypeCode.NonNegativeInteger,
            /* UnsignedInt                 */ XmlTypeCode.UnsignedLong,
            /* UnsignedShort               */ XmlTypeCode.UnsignedInt,
            /* UnsignedByte                */ XmlTypeCode.UnsignedShort,
            /* PositiveInteger             */ XmlTypeCode.NonNegativeInteger,
            /* YearMonthDuration           */ XmlTypeCode.Duration,
            /* DayTimeDuration             */ XmlTypeCode.Duration,
        };

        private static readonly string[] s_typeNames = {
            /* None                        */ "none",
            /* Item                        */ "item",
            /* Node                        */ "node",
            /* Document                    */ "document",
            /* Element                     */ "element",
            /* Attribute                   */ "attribute",
            /* Namespace                   */ "namespace",
            /* ProcessingInstruction       */ "processing-instruction",
            /* Comment                     */ "comment",
            /* Text                        */ "text",
            /* AnyAtomicType               */ "xdt:anyAtomicType",
            /* UntypedAtomic               */ "xdt:untypedAtomic",
            /* String                      */ "xs:string",
            /* Boolean                     */ "xs:boolean",
            /* Decimal                     */ "xs:decimal",
            /* Float                       */ "xs:float",
            /* Double                      */ "xs:double",
            /* Duration                    */ "xs:duration",
            /* DateTime                    */ "xs:dateTime",
            /* Time                        */ "xs:time",
            /* Date                        */ "xs:date",
            /* GYearMonth                  */ "xs:gYearMonth",
            /* GYear                       */ "xs:gYear",
            /* GMonthDay                   */ "xs:gMonthDay",
            /* GDay                        */ "xs:gDay",
            /* GMonth                      */ "xs:gMonth",
            /* HexBinary                   */ "xs:hexBinary",
            /* Base64Binary                */ "xs:base64Binary",
            /* AnyUri                      */ "xs:anyUri",
            /* QName                       */ "xs:QName",
            /* Notation                    */ "xs:NOTATION",
            /* NormalizedString            */ "xs:normalizedString",
            /* Token                       */ "xs:token",
            /* Language                    */ "xs:language",
            /* NmToken                     */ "xs:NMTOKEN",
            /* Name                        */ "xs:Name",
            /* NCName                      */ "xs:NCName",
            /* Id                          */ "xs:ID",
            /* Idref                       */ "xs:IDREF",
            /* Entity                      */ "xs:ENTITY",
            /* Integer                     */ "xs:integer",
            /* NonPositiveInteger          */ "xs:nonPositiveInteger",
            /* NegativeInteger             */ "xs:negativeInteger",
            /* Long                        */ "xs:long",
            /* Int                         */ "xs:int",
            /* Short                       */ "xs:short",
            /* Byte                        */ "xs:byte",
            /* NonNegativeInteger          */ "xs:nonNegativeInteger",
            /* UnsignedLong                */ "xs:unsignedLong",
            /* UnsignedInt                 */ "xs:unsignedInt",
            /* UnsignedShort               */ "xs:unsignedShort",
            /* UnsignedByte                */ "xs:unsignedByte",
            /* PositiveInteger             */ "xs:positiveInteger",
            /* YearMonthDuration           */ "xdt:yearMonthDuration",
            /* DayTimeDuration             */ "xdt:dayTimeDuration",
        };
        #endregion

        /// <summary>
        /// Implements an NxN bit matrix.
        /// </summary>
        private sealed class BitMatrix
        {
            private ulong[] _bits;

            /// <summary>
            /// Create NxN bit matrix, where N = count.
            /// </summary>
            public BitMatrix(int count)
            {
                Debug.Assert(count < 64, "BitMatrix currently only handles up to 64x64 matrix.");
                _bits = new ulong[count];
            }

            //            /// <summary>
            //            /// Return the number of rows and columns in the matrix.
            //            /// </summary>
            //            public int Size {
            //                get { return bits.Length; }
            //            }
            //
            /// <summary>
            /// Get or set a bit in the matrix at position (index1, index2).
            /// </summary>
            public bool this[int index1, int index2]
            {
                get
                {
                    Debug.Assert(index1 < _bits.Length && index2 < _bits.Length, "Index out of range.");
                    return (_bits[index1] & ((ulong)1 << index2)) != 0;
                }
                set
                {
                    Debug.Assert(index1 < _bits.Length && index2 < _bits.Length, "Index out of range.");
                    if (value == true)
                    {
                        _bits[index1] |= (ulong)1 << index2;
                    }
                    else
                    {
                        _bits[index1] &= ~((ulong)1 << index2);
                    }
                }
            }

            /// <summary>
            /// Strongly typed indexer.
            /// </summary>
            public bool this[XmlTypeCode index1, XmlTypeCode index2]
            {
                get
                {
                    return this[(int)index1, (int)index2];
                }
                //                set {
                //                    this[(int)index1, (int)index2] = value;
                //                }
            }
        }
    }
}
