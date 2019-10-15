// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Xsl;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil operators that introduce iterators (Loop, Filter, etc.).
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal class QilLoop : QilBinary
    {
        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilLoop(QilNodeType nodeType, QilNode variable, QilNode body) : base(nodeType, variable, body)
        {
        }


        //-----------------------------------------------
        // QilLoop methods
        //-----------------------------------------------

        public QilIterator Variable
        {
            get { return (QilIterator)Left; }
            set { Left = value; }
        }

        public QilNode Body
        {
            get { return Right; }
            set { Right = value; }
        }
    }
}
