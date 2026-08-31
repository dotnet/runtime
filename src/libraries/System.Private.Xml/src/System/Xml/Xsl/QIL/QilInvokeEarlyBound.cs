// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// A function invocation node which represents a call to an early bound Clr function.
    /// </summary>
    internal sealed class QilInvokeEarlyBound : QilTernary
    {
        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a new node
        /// </summary>
        public QilInvokeEarlyBound(QilNodeType nodeType, QilNode name, QilNode method, QilNode arguments, XmlQueryType resultType)
            : base(nodeType, name, method, arguments)
        {
            this.xmlType = resultType;
        }


        //-----------------------------------------------
        // QilInvokeEarlyBound methods
        //-----------------------------------------------

        public QilName Name
        {
            get { return (QilName)Left; }
            set { Left = value; }
        }

        public MethodInfo ClrMethod
        {
            get { return (MethodInfo)((QilLiteral)Center).Value!; }
            set { ((QilLiteral)Center).Value = value; }
        }

        public QilList Arguments
        {
            get { return (QilList)Right; }
            set { Right = value; }
        }
    }
}
