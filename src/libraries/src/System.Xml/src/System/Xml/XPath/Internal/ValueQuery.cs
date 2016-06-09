// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;
    using System.Xml.Xsl;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal abstract class ValueQuery : Query
    {
        public ValueQuery() { }
        protected ValueQuery(ValueQuery other) : base(other) { }
        public sealed override void Reset() { }
        public sealed override XPathNavigator Current { get { throw XPathException.Create(Res.Xp_NodeSetExpected); } }
        public sealed override int CurrentPosition { get { throw XPathException.Create(Res.Xp_NodeSetExpected); } }
        public sealed override int Count { get { throw XPathException.Create(Res.Xp_NodeSetExpected); } }
        public sealed override XPathNavigator Advance() { throw XPathException.Create(Res.Xp_NodeSetExpected); }
    }
}
