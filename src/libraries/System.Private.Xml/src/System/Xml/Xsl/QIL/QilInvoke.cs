// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Xsl;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// A function invocation node which represents a call to a Qil functions.
    /// </summary>
    internal class QilInvoke : QilBinary
    {
        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilInvoke(QilNodeType nodeType, QilNode function, QilNode arguments) : base(nodeType, function, arguments)
        {
        }


        //-----------------------------------------------
        // QilInvoke methods
        //-----------------------------------------------

        public QilFunction Function
        {
            get { return (QilFunction)Left; }
            set { Left = value; }
        }

        public QilList Arguments
        {
            get { return (QilList)Right; }
            set { Right = value; }
        }
    }
}
