// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl.XsltOld.Debugger;

namespace System.Xml.Xsl.XsltOld
{
    internal abstract class Event
    {
        public virtual void ReplaceNamespaceAlias(Compiler compiler) { }
        public abstract bool Output(Processor processor, ActionFrame frame);
    }
}
