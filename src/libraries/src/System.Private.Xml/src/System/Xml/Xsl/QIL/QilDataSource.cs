// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil DataSource operator.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal class QilDataSource : QilBinary
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
