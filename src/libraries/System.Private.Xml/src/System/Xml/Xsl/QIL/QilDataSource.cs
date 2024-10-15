// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Schema;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil DataSource operator.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal sealed class QilDataSource : QilBinary
    {
        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilDataSource(QilNodeType nodeType, QilNode name, QilNode baseUri) : base(nodeType, name, baseUri)
        {
        }


        //-----------------------------------------------
        // QilDataSource methods
        //-----------------------------------------------

        public QilNode Name
        {
            get { return Left; }
            set { Left = value; }
        }

        public QilNode BaseUri
        {
            get { return Right; }
            set { Right = value; }
        }
    }
}
