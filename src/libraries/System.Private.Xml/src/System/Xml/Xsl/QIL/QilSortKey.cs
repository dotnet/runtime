// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil SortKey operator.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal sealed class QilSortKey : QilBinary
    {
        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilSortKey(QilNodeType nodeType, QilNode key, QilNode collation) : base(nodeType, key, collation)
        {
        }


        //-----------------------------------------------
        // QilSortKey methods
        //-----------------------------------------------

        public QilNode Key
        {
            get { return Left; }
            set { Left = value; }
        }

        public QilNode Collation
        {
            get { return Right; }
            set { Right = value; }
        }
    }
}
