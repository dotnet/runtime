// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            w.WriteAttributeString("value", Convert.ToString(val, CultureInfo.InvariantCulture));
            w.WriteEndElement();
        }
    }
}
