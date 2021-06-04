// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 618 // ignore obsolete warning about XmlDataDocument

using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    internal sealed class XmlDataImplementation : XmlImplementation
    {
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        public XmlDataImplementation() : base() { }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override XmlDocument CreateDocument() => new XmlDataDocument(this);
    }
}
