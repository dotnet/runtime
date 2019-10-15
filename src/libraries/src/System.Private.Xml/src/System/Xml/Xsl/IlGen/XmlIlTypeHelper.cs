// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen
{
    /// <summary>
    /// Static QilExpression type helper methods.
    /// </summary>
    internal class XmlILTypeHelper
    {
        // Not creatable
        private XmlILTypeHelper()
        {
        }

        /// <summary>
        /// Return the default Clr data type that will be used to store instances of the QilNode's type.
        /// </summary>
        public static Type GetStorageType(XmlQueryType qyTyp)
        {
            Type storageType;

            if (qyTyp.IsSingleton)
            {
                storageType = s_typeCodeToStorage[(int)qyTyp.TypeCode];

                // Non-strict items must store the type along with the value, so use XPathItem
                if (!qyTyp.IsStrict && storageType != typeof(XPathNavigator))
                    return typeof(XPathItem);
            }
            else
            {
                storageType = s_typeCodeToCachedStorage[(int)qyTyp.TypeCode];

                // Non-strict items must store the type along with the value, so use XPathItem
                if (!qyTyp.IsStrict && storageType != typeof(IList<XPathNavigator>))
                    return typeof(IList<XPathItem>);
            }

            return storageType;
        }

        private static readonly Type[] s_typeCodeToStorage = {
            typeof(XPathItem),                          // XmlTypeCode.None
            typeof(XPathItem),                          // XmlTypeCode.Item
            typeof(XPathNavigator),                     // XmlTypeCode.Node
            typeof(XPathNavigator),                     // XmlTypeCode.Document
            typeof(XPathNavigator),                     // XmlTypeCode.Element
            typeof(XPathNavigator),                     // XmlTypeCode.Attribute
            typeof(XPathNavigator),                     // XmlTypeCode.Namespace
            typeof(XPathNavigator),                     // XmlTypeCode.ProcessingInstruction
            typeof(XPathNavigator),                     // XmlTypeCode.Comment
            typeof(XPathNavigator),                     // XmlTypeCode.Text
            typeof(XPathItem),                          // XmlTypeCode.AnyAtomicType
            typeof(string),                             // XmlTypeCode.UntypedAtomic
            typeof(string),                             // XmlTypeCode.String
            typeof(bool),                               // XmlTypeCode.Boolean
            typeof(decimal),                            // XmlTypeCode.Decimal
            typeof(float),                              // XmlTypeCode.Float
            typeof(double),                             // XmlTypeCode.Double
            typeof(string),                             // XmlTypeCode.Duration
            typeof(DateTime),                           // XmlTypeCode.DateTime
            typeof(DateTime),                           // XmlTypeCode.Time
            typeof(DateTime),                           // XmlTypeCode.Date
            typeof(DateTime),                           // XmlTypeCode.GYearMonth
            typeof(DateTime),                           // XmlTypeCode.GYear
            typeof(DateTime),                           // XmlTypeCode.GMonthDay
            typeof(DateTime),                           // XmlTypeCode.GDay
            typeof(DateTime),                           // XmlTypeCode.GMonth
            typeof(byte[]),                             // XmlTypeCode.HexBinary
            typeof(byte[]),                             // XmlTypeCode.Base64Binary
            typeof(string),                             // XmlTypeCode.AnyUri
            typeof(XmlQualifiedName),                   // XmlTypeCode.QName
            typeof(XmlQualifiedName),                   // XmlTypeCode.Notation
            typeof(string),                             // XmlTypeCode.NormalizedString
            typeof(string),                             // XmlTypeCode.Token
            typeof(string),                             // XmlTypeCode.Language
            typeof(string),                             // XmlTypeCode.NmToken
            typeof(string),                             // XmlTypeCode.Name
            typeof(string),                             // XmlTypeCode.NCName
            typeof(string),                             // XmlTypeCode.Id
            typeof(string),                             // XmlTypeCode.Idref
            typeof(string),                             // XmlTypeCode.Entity
            typeof(long),                               // XmlTypeCode.Integer
            typeof(decimal),                            // XmlTypeCode.NonPositiveInteger
            typeof(decimal),                            // XmlTypeCode.NegativeInteger
            typeof(long),                               // XmlTypeCode.Long
            typeof(int),                                // XmlTypeCode.Int
            typeof(int),                                // XmlTypeCode.Short
            typeof(int),                                // XmlTypeCode.Byte
            typeof(decimal),                            // XmlTypeCode.NonNegativeInteger
            typeof(decimal),                            // XmlTypeCode.UnsignedLong
            typeof(long),                               // XmlTypeCode.UnsignedInt
            typeof(int),                                // XmlTypeCode.UnsignedShort
            typeof(int),                                // XmlTypeCode.UnsignedByte
            typeof(decimal),                            // XmlTypeCode.PositiveInteger
            typeof(TimeSpan),                           // XmlTypeCode.YearMonthDuration
            typeof(TimeSpan),                           // XmlTypeCode.DayTimeDuration
        };

        private static readonly Type[] s_typeCodeToCachedStorage = {
            typeof(IList<XPathItem>),                   // XmlTypeCode.None
            typeof(IList<XPathItem>),                   // XmlTypeCode.Item
            typeof(IList<XPathNavigator>),              // XmlTypeCode.Node
            typeof(IList<XPathNavigator>),              // XmlTypeCode.Document
            typeof(IList<XPathNavigator>),              // XmlTypeCode.Element
            typeof(IList<XPathNavigator>),              // XmlTypeCode.Attribute
            typeof(IList<XPathNavigator>),              // XmlTypeCode.Namespace
            typeof(IList<XPathNavigator>),              // XmlTypeCode.ProcessingInstruction
            typeof(IList<XPathNavigator>),              // XmlTypeCode.Comment
            typeof(IList<XPathNavigator>),              // XmlTypeCode.Text
            typeof(IList<XPathItem>),                   // XmlTypeCode.AnyAtomicType
            typeof(IList<string>),                      // XmlTypeCode.UntypedAtomic
            typeof(IList<string>),                      // XmlTypeCode.String
            typeof(IList<bool>),                        // XmlTypeCode.Boolean
            typeof(IList<decimal>),                     // XmlTypeCode.Decimal
            typeof(IList<float>),                       // XmlTypeCode.Float
            typeof(IList<double>),                      // XmlTypeCode.Double
            typeof(IList<string>),                      // XmlTypeCode.Duration
            typeof(IList<DateTime>),                    // XmlTypeCode.DateTime
            typeof(IList<DateTime>),                    // XmlTypeCode.Time
            typeof(IList<DateTime>),                    // XmlTypeCode.Date
            typeof(IList<DateTime>),                    // XmlTypeCode.GYearMonth
            typeof(IList<DateTime>),                    // XmlTypeCode.GYear
            typeof(IList<DateTime>),                    // XmlTypeCode.GMonthDay
            typeof(IList<DateTime>),                    // XmlTypeCode.GDay
            typeof(IList<DateTime>),                    // XmlTypeCode.GMonth
            typeof(IList<byte[]>),                      // XmlTypeCode.HexBinary
            typeof(IList<byte[]>),                      // XmlTypeCode.Base64Binary
            typeof(IList<string>),                      // XmlTypeCode.AnyUri
            typeof(IList<XmlQualifiedName>),            // XmlTypeCode.QName
            typeof(IList<XmlQualifiedName>),            // XmlTypeCode.Notation
            typeof(IList<string>),                      // XmlTypeCode.NormalizedString
            typeof(IList<string>),                      // XmlTypeCode.Token
            typeof(IList<string>),                      // XmlTypeCode.Language
            typeof(IList<string>),                      // XmlTypeCode.NmToken
            typeof(IList<string>),                      // XmlTypeCode.Name
            typeof(IList<string>),                      // XmlTypeCode.NCName
            typeof(IList<string>),                      // XmlTypeCode.Id
            typeof(IList<string>),                      // XmlTypeCode.Idref
            typeof(IList<string>),                      // XmlTypeCode.Entity
            typeof(IList<long>),                        // XmlTypeCode.Integer
            typeof(IList<decimal>),                     // XmlTypeCode.NonPositiveInteger
            typeof(IList<decimal>),                     // XmlTypeCode.NegativeInteger
            typeof(IList<long>),                        // XmlTypeCode.Long
            typeof(IList<int>),                         // XmlTypeCode.Int
            typeof(IList<int>),                         // XmlTypeCode.Short
            typeof(IList<int>),                         // XmlTypeCode.Byte
            typeof(IList<decimal>),                     // XmlTypeCode.NonNegativeInteger
            typeof(IList<decimal>),                     // XmlTypeCode.UnsignedLong
            typeof(IList<long>),                        // XmlTypeCode.UnsignedInt
            typeof(IList<int>),                         // XmlTypeCode.UnsignedShort
            typeof(IList<int>),                         // XmlTypeCode.UnsignedByte
            typeof(IList<decimal>),                     // XmlTypeCode.PositiveInteger
            typeof(IList<TimeSpan>),                    // XmlTypeCode.YearMonthDuration
            typeof(IList<TimeSpan>),                    // XmlTypeCode.DayTimeDuration
        };
    }
}
