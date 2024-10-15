// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Schema;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil operator having two children, the second of which is a literal type.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal sealed class QilTargetType : QilBinary
    {
        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilTargetType(QilNodeType nodeType, QilNode expr, QilNode targetType) : base(nodeType, expr, targetType)
        {
        }


        //-----------------------------------------------
        // QilTargetType methods
        //-----------------------------------------------

        public QilNode Source
        {
            get { return Left; }
            set { Left = value; }
        }

        public XmlQueryType TargetType
        {
            get { return (XmlQueryType)((QilLiteral)Right).Value!; }
            set { ((QilLiteral)Right).Value = value; }
        }
    }
}
