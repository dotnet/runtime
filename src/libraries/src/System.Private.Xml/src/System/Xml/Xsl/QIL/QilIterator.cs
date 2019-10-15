// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil iterator node (For or Let).
    /// </summary>
    internal class QilIterator : QilReference
    {
        private QilNode _binding;

        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct an iterator
        /// </summary>
        public QilIterator(QilNodeType nodeType, QilNode binding) : base(nodeType)
        {
            Binding = binding;
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
            get { if (index != 0) throw new IndexOutOfRangeException(); return _binding; }
            set { if (index != 0) throw new IndexOutOfRangeException(); _binding = value; }
        }


        //-----------------------------------------------
        // QilIterator methods
        //-----------------------------------------------

        /// <summary>
        /// Expression which is bound to the iterator.
        /// </summary>
        public QilNode Binding
        {
            get { return _binding; }
            set { _binding = value; }
        }
    }
}
