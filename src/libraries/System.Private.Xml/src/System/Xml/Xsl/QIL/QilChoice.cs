// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Xsl;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil choice operator.
    /// </summary>
    /// <remarks>
    /// Don't construct QIL nodes directly; instead, use the <see cref="QilFactory">QilFactory</see>.
    /// </remarks>
    internal sealed class QilChoice : QilBinary
    {
        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilChoice(QilNodeType nodeType, QilNode expression, QilNode branches) : base(nodeType, expression, branches)
        {
        }


        //-----------------------------------------------
        // QilChoice methods
        //-----------------------------------------------

        public QilNode Expression
        {
            get { return Left; }
            set { Left = value; }
        }

        public QilList Branches
        {
            get { return (QilList)Right; }
            set { Right = value; }
        }
    }
}
