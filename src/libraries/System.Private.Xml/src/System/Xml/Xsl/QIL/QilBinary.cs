// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil operator having two children.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal class QilBinary : QilNode
    {
        private QilNode _left, _right;


        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilBinary(QilNodeType nodeType, QilNode left, QilNode right) : base(nodeType)
        {
            _left = left;
            _right = right;
        }


        //-----------------------------------------------
        // IList<QilNode> methods -- override
        //-----------------------------------------------

        public override int Count
        {
            get { return 2; }
        }

        public override QilNode this[int index]
        {
            get
            {
                return index switch
                {
                    0 => _left,
                    1 => _right,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0: _left = value; break;
                    case 1: _right = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }


        //-----------------------------------------------
        // QilBinary methods
        //-----------------------------------------------

        public QilNode Left
        {
            get { return _left; }
            set { _left = value; }
        }

        public QilNode Right
        {
            get { return _right; }
            set { _right = value; }
        }
    }
}
