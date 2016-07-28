// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld.Debugger
{
    using System;
    using System.Xml;
    using System.Xml.XPath;

    internal interface IStackFrame
    {
        XPathNavigator Instruction { get; }
        XPathNodeIterator NodeSet { get; }
        // Variables:
        int GetVariablesCount();
        XPathNavigator GetVariable(int varIndex);
        object GetVariableValue(int varIndex);
    }

    internal interface IXsltProcessor
    {
        int StackDepth { get; }
        IStackFrame GetStackFrame(int depth);
    }

    internal interface IXsltDebugger
    {
        string GetBuiltInTemplatesUri();
        void OnInstructionCompile(XPathNavigator styleSheetNavigator);
        void OnInstructionExecute(IXsltProcessor xsltProcessor);
    }
}
