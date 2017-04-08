// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;

namespace System.Xml.Xsl
{
    /// <summary>
    /// Cardinality of part of XmlQueryType
    /// struct is being used because enum doesn't allow members
    /// </summary>
    internal struct XmlQueryCardinality
    {
        private int _value;

        #region ctor
        /// <summary>
        /// Private constructor
        /// </summary>
        private XmlQueryCardinality(int value)
        {
            Debug.Assert(0x00 <= value && value <= 0x07);
            _value = value;
        }
        #endregion

        #region enum
        /// <summary>
        /// exactly zero (empty)
        /// </summary>
        public static XmlQueryCardinality None
        {
            get { return new XmlQueryCardinality(0x00); }
        }

        /// <summary>
        /// exactly zero (empty)
        /// </summary>
        public static XmlQueryCardinality Zero
        {
            get { return new XmlQueryCardinality(0x01); }
        }

        /// <summary>
        /// exactly one
        /// </summary>
        public static XmlQueryCardinality One
        {
            get { return new XmlQueryCardinality(0x02); }
        }

        /// <summary>
        /// zero or one (not more)
        /// </summary>
        public static XmlQueryCardinality ZeroOrOne
        {
            get { return new XmlQueryCardinality(0x03); }
        }

        /// <summary>
        /// strictly more than one
        /// </summary>
        public static XmlQueryCardinality More
        {
            get { return new XmlQueryCardinality(0x04); }
        }

        /// <summary>
        /// not one (strictly zero or strictly more)
        /// </summary>
        public static XmlQueryCardinality NotOne
        {
            get { return new XmlQueryCardinality(0x05); }
        }

        /// <summary>
        /// one or more (not empty)
        /// </summary>
        public static XmlQueryCardinality OneOrMore
        {
            get { return new XmlQueryCardinality(0x06); }
        }

        /// <summary>
        /// zero or more (any cardinality)
        /// </summary>
        public static XmlQueryCardinality ZeroOrMore
        {
            get { return new XmlQueryCardinality(0x07); }
        }
        #endregion

        #region ==
        /// <summary>
        /// Strongly-typed Equals that returns true if this type and "other" type are equivalent.
        /// </summary>
        public bool Equals(XmlQueryCardinality other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Overload == operator to call Equals rather than do reference equality.
        /// </summary>
        public static bool operator ==(XmlQueryCardinality left, XmlQueryCardinality right)
        {
            return left._value == right._value;
        }

        /// <summary>
        /// Overload != operator to call Equals rather than do reference inequality.
        /// </summary>
        public static bool operator !=(XmlQueryCardinality left, XmlQueryCardinality right)
        {
            return left._value != right._value;
        }

        /// <summary>
        /// True if "other" is an XmlQueryCardinality, and this type is the exact same static type.
        /// </summary>
        public override bool Equals(object other)
        {
            if (other is XmlQueryCardinality)
            {
                return Equals((XmlQueryCardinality)other);
            }
            return false;
        }

        /// <summary>
        /// Return hash code of this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return _value;
        }
        #endregion

        #region algebra
        /// <summary>
        /// Return union with other
        /// </summary>
        public static XmlQueryCardinality operator |(XmlQueryCardinality left, XmlQueryCardinality right)
        {
            return new XmlQueryCardinality(left._value | right._value);
        }

        /// <summary>
        /// Return this product other
        /// </summary>
        public static XmlQueryCardinality operator *(XmlQueryCardinality left, XmlQueryCardinality right)
        {
            return s_cardinalityProduct[left._value, right._value];
        }

        /// <summary>
        /// Return sum with other
        /// </summary>
        public static XmlQueryCardinality operator +(XmlQueryCardinality left, XmlQueryCardinality right)
        {
            return s_cardinalitySum[left._value, right._value];
        }

#if NEVER
        /// <summary>
        /// Returns true if this cardinality is guaranteed to be a subset of "other".
        /// </summary>
        private bool IsSubset(XmlQueryCardinality other) {
            return (this.value & ~other.value) == 0;
        }
#endif

        /// <summary>
        /// Returns true is left is subset of right.
        /// </summary>
        public static bool operator <=(XmlQueryCardinality left, XmlQueryCardinality right)
        {
            return (left._value & ~right._value) == 0;
        }

        /// <summary>
        /// Returns true is right is subset of left.
        /// /// </summary>
        public static bool operator >=(XmlQueryCardinality left, XmlQueryCardinality right)
        {
            return (right._value & ~left._value) == 0;
        }

        /// <summary>
        /// Compute the cardinality of a subset of a set of the given cardinality.
        /// </summary>
        /// <param name="c">the cardinality of a set</param>
        /// <returns>the cardinality of a subset</returns>
        public XmlQueryCardinality AtMost()
        {
            //  Fill downward to zero
            return new XmlQueryCardinality(_value | (_value >> 1) | (_value >> 2));
        }

        /// <summary>
        /// Returns true if every non-None subset of this cardinality is disjoint with "other" cardinality.
        /// Here is the behavior for None, which is the inverse of the None behavior for IsSubset:
        ///   None op  None = false
        ///   None op ~None = false
        ///  ~None op  None = true
        /// </summary>
        public bool NeverSubset(XmlQueryCardinality other)
        {
            return _value != 0 && (_value & other._value) == 0;
        }

        /// <summary>
        /// Table of cardinality products.
        /// </summary>
        private static readonly XmlQueryCardinality[,] s_cardinalityProduct = {
                          //   None  Zero  One         ZeroOrOne   More    NotOne  OneOrMore   ZeroOrMore
            /* None       */ { None, Zero, None      , Zero      , None  , Zero  , None      , Zero       },
            /* Zero       */ { Zero, Zero, Zero      , Zero      , Zero  , Zero  , Zero      , Zero       },
            /* One        */ { None, Zero, One       , ZeroOrOne , More  , NotOne, OneOrMore , ZeroOrMore },
            /* ZeroOrOne  */ { Zero, Zero, ZeroOrOne , ZeroOrOne , NotOne, NotOne, ZeroOrMore, ZeroOrMore },
            /* More       */ { None, Zero, More      , NotOne    , More  , NotOne, More      , NotOne     },
            /* NotOne     */ { Zero, Zero, NotOne    , NotOne    , NotOne, NotOne, NotOne    , NotOne     },
            /* OneOrMore  */ { None, Zero, OneOrMore , ZeroOrMore, More  , NotOne, OneOrMore , ZeroOrMore },
            /* ZeroOrMore */ { Zero, Zero, ZeroOrMore, ZeroOrMore, NotOne, NotOne, ZeroOrMore, ZeroOrMore }
        };

        /// <summary>
        /// Table of cardinality sums.
        /// </summary>
        private static readonly XmlQueryCardinality[,] s_cardinalitySum = {
                          //   None        Zero        One        ZeroOrOne   More  NotOne      OneOrMore  ZeroOrMore
            /* None       */ { None      , Zero      , One      , ZeroOrOne , More, NotOne    , OneOrMore, ZeroOrMore},
            /* Zero       */ { Zero      , Zero      , One      , ZeroOrOne , More, NotOne    , OneOrMore, ZeroOrMore},
            /* One        */ { One       , One       , More     , OneOrMore , More, OneOrMore , More     , OneOrMore },
            /* ZeroOrOne  */ { ZeroOrOne , ZeroOrOne , OneOrMore, ZeroOrMore, More, ZeroOrMore, OneOrMore, ZeroOrMore},
            /* More       */ { More      , More      , More     , More      , More, More      , More     , More      },
            /* NotOne     */ { NotOne    , NotOne    , OneOrMore, ZeroOrMore, More, NotOne    , OneOrMore, ZeroOrMore},
            /* OneOrMore  */ { OneOrMore , OneOrMore , More     , OneOrMore , More, OneOrMore , More     , OneOrMore },
            /* ZeroOrMore */ { ZeroOrMore, ZeroOrMore, OneOrMore, ZeroOrMore, More, ZeroOrMore, OneOrMore, ZeroOrMore}
        };
        #endregion

        #region Serialization
        /// <summary>
        /// String representation.
        /// </summary>
        private static readonly string[] s_toString = {
            /* None       */ ""  ,
            /* Zero       */ "?" ,
            /* One        */ ""  ,
            /* ZeroOrOne  */ "?" ,
            /* More       */ "+" ,
            /* NotOne     */ "*" ,
            /* OneOrMore  */ "+" ,
            /* ZeroOrMore */ "*"
        };

        /// <summary>
        /// Serialization
        /// </summary>
        private static readonly string[] s_serialized = {
            /* None       */ "None",
            /* Zero       */ "Zero",
            /* One        */ "One",
            /* ZeroOrOne  */ "ZeroOrOne",
            /* More       */ "More",
            /* NotOne     */ "NotOne",
            /* OneOrMore  */ "OneOrMore",
            /* ZeroOrMore */ "ZeroOrMore"
        };

        /// <summary>
        /// Return the string representation of a cardinality, normalized to either ?, +, *, or "" (card 1).
        /// </summary>
        public string ToString(string format)
        {
            if (format == "S")
            {
                return s_serialized[_value];
            }
            else
            {
                return ToString();
            }
        }

        /// <summary>
        /// Return the string representation of a cardinality, normalized to either ?, +, *, or "" (card 1).
        /// </summary>
        public override string ToString()
        {
            return s_toString[_value];
        }

        /// <summary>
        /// Serialize the object to BinaryWriter.
        /// </summary>
        public void GetObjectData(BinaryWriter writer)
        {
            writer.Write((byte)_value);
        }

        /// <summary>
        /// Deserialize the object from BinaryReader.
        /// </summary>
        public XmlQueryCardinality(BinaryReader reader) : this(reader.ReadByte())
        {
        }
        #endregion
    }
}
