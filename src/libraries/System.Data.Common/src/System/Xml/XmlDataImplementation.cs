// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 618 // ignore obsolete warning about XmlDataDocument

namespace System.Xml
{
    internal sealed class XmlDataImplementation : XmlImplementation
    {
        public XmlDataImplementation() : base() { }
        public override XmlDocument CreateDocument() => new XmlDataDocument(this);
    }
}
