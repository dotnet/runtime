// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld.Debugger
{
    internal interface IXsltProcessor
    {
    }

    internal interface IXsltDebugger
    {
        string GetBuiltInTemplatesUri();
        void OnInstructionCompile(XPathNavigator styleSheetNavigator);
        void OnInstructionExecute(IXsltProcessor xsltProcessor);
    }
}
