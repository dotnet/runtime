// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// A function invocation node which represents a call to an late bound function.
    /// </summary>
    internal sealed class QilInvokeLateBound : QilBinary
    {
        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilInvokeLateBound(QilNodeType nodeType, QilNode name, QilNode arguments) : base(nodeType, name, arguments)
        {
        }


        //-----------------------------------------------
        // QilInvokeLateBound methods
        //-----------------------------------------------

        public QilName Name
        {
            get { return (QilName)Left; }
            set { Left = value; }
        }

        public QilList Arguments
        {
            get { return (QilList)Right; }
            set { Right = value; }
        }
    }
}
