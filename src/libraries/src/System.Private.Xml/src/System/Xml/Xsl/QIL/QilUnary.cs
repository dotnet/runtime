// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil operator having one child.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal class QilUnary : QilNode
    {
        private QilNode _child;


        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilUnary(QilNodeType nodeType, QilNode child) : base(nodeType)
        {
            _child = child;
        }


        //-----------------------------------------------
        // IList<QilNode> methods -- override
        //-----------------------------------------------

        public override int Count
        {
            get { return 1; }
        }

        public override QilNode this[int index]
        {
            get { if (index != 0) throw new IndexOutOfRangeException(); return _child; }
            set { if (index != 0) throw new IndexOutOfRangeException(); _child = value; }
        }


        //-----------------------------------------------
        // QilUnary methods
        //-----------------------------------------------

        public QilNode Child
        {
            get { return _child; }
            set { _child = value; }
        }
    }
}
