// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class OperandQuery : ValueQuery
    {
        internal object val;

        public OperandQuery(object val)
        {
            this.val = val;
        }

        public override object Evaluate(XPathNodeIterator nodeIterator)
        {
            return val;
        }
        public override XPathResultType StaticType { get { return GetXPathType(val); } }
        public override XPathNodeIterator Clone() { return this; }
    }
}
