// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld.Debugger
{
    using System.Xml.XPath;

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
