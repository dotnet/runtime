// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil atomic value literal (of any type).
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal class QilLiteral : QilNode
    {
        private object _value;


        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilLiteral(QilNodeType nodeType, object value) : base(nodeType)
        {
            Value = value;
        }


        //-----------------------------------------------
        // QilLiteral methods
        //-----------------------------------------------

        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public static implicit operator string (QilLiteral literal)
        {
            return (string)literal._value;
        }

        public static implicit operator int (QilLiteral literal)
        {
            return (int)literal._value;
        }

        public static implicit operator long (QilLiteral literal)
        {
            return (long)literal._value;
        }

        public static implicit operator double (QilLiteral literal)
        {
            return (double)literal._value;
        }

        public static implicit operator decimal (QilLiteral literal)
        {
            return (decimal)literal._value;
        }

        public static implicit operator XmlQueryType(QilLiteral literal)
        {
            return (XmlQueryType)literal._value;
        }
    }
}
