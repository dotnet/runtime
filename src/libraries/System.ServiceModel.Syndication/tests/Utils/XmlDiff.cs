// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace System.ServiceModel.Syndication.Tests
{
    internal enum DiffType
    {
        None,
        Success,
        Element,
        Whitespace,
        Comment,
        PI,
        Text,
        CData,
        Attribute,
        NS,
        Prefix,
        SourceExtra,
        TargetExtra,
        NodeType
    }

    public class XmlDiff
    {
        private XmlDiffDocument _SourceDoc;
        private XmlDiffDocument _TargetDoc;

        private XmlTextWriter _Writer;     // Writer to write out the result
        private StringBuilder _Output;

        // Option Flags
        private XmlDiffOption _XmlDiffOption = XmlDiffOption.None;

        public XmlDiff()
        {
            _XmlDiffOption = XmlDiffOption.IgnoreEmptyElement |
                             XmlDiffOption.IgnoreWhitespace |
                             XmlDiffOption.IgnoreAttributeOrder |
                             XmlDiffOption.IgnoreNS |
                             XmlDiffOption.IgnorePrefix |
                             XmlDiffOption.IgnoreDTD |
                             XmlDiffOption.IgnoreChildOrder;
        }

        public XmlDiffOption Option
        {
            get
            {
                return _XmlDiffOption;
            }
            set
            {
                _XmlDiffOption = value;
            }
        }

        internal bool IgnoreEmptyElement
        {
            get { return ((_XmlDiffOption & XmlDiffOption.IgnoreEmptyElement) != 0); }
        }

        internal bool IgnoreComments
        {
            get { return ((_XmlDiffOption & XmlDiffOption.IgnoreComments) != 0); }
        }

        internal bool IgnoreAttributeOrder
        {
            get { return ((_XmlDiffOption & XmlDiffOption.IgnoreAttributeOrder) != 0); }
        }

        internal bool IgnoreWhitespace
        {
            get { return ((_XmlDiffOption & XmlDiffOption.IgnoreWhitespace) != 0); }
        }

        internal bool IgnoreNS
        {
            get { return ((_XmlDiffOption & XmlDiffOption.IgnoreNS) != 0); }
        }

        internal bool IgnorePrefix
        {
            get { return ((_XmlDiffOption & XmlDiffOption.IgnorePrefix) != 0); }
        }

        internal bool IgnoreDTD
        {
            get { return ((_XmlDiffOption & XmlDiffOption.IgnoreDTD) != 0); }
        }

        internal bool IgnoreChildOrder
        {
            get { return ((_XmlDiffOption & XmlDiffOption.IgnoreChildOrder) != 0); }
        }

        internal bool ConcatenateAdjacentTextNodes
        {
            get { return ((_XmlDiffOption & XmlDiffOption.ConcatenateAdjacentTextNodes) != 0); }
        }

        internal bool TreatWhitespaceTextAsWSNode
        {
            get { return ((_XmlDiffOption & XmlDiffOption.TreatWhitespaceTextAsWSNode) != 0); }
        }

        internal bool ParseAttributeValuesAsQName
        {
            get { return ((_XmlDiffOption & XmlDiffOption.ParseAttributeValuesAsQName) != 0); }
        }

        internal bool DontWriteMatchingNodesToOutput
        {
            get { return ((_XmlDiffOption & XmlDiffOption.DontWriteMatchingNodesToOutput) != 0); }
        }

        internal bool DontWriteAnythingToOutput
        {
            get { return ((_XmlDiffOption & XmlDiffOption.DontWriteAnythingToOutput) != 0); }
        }

        private void InitFiles()
        {
            _SourceDoc = new XmlDiffDocument();
            _SourceDoc.Option = _XmlDiffOption;
            _TargetDoc = new XmlDiffDocument();
            _TargetDoc.Option = this.Option;
            _Output = new StringBuilder(String.Empty);
        }

        public bool Compare(String source, String target)
        {
            InitFiles();
            _SourceDoc.Load(source);
            _TargetDoc.Load(target);
            return Diff();
        }

        public bool Compare(XmlReader source, XmlReader target)
        {
            InitFiles();
            _SourceDoc.Load(source);
            _TargetDoc.Load(target);
            return Diff();
        }

        public bool Compare(XmlReader source, XmlReader target, XmlDiffAdvancedOptions advOptions)
        {
            InitFiles();

            _SourceDoc.Load(source);
            _TargetDoc.Load(target);
            XPathNavigator nav = _SourceDoc.CreateNavigator();

            if (!string.IsNullOrEmpty(advOptions.IgnoreChildOrderExpr))
            {
                XPathExpression expr = GenerateXPathExpression(
                    advOptions.IgnoreChildOrderExpr,
                    advOptions,
                    nav);

                _SourceDoc.SortChildren(expr);
                _TargetDoc.SortChildren(expr);
            }

            if (advOptions.IgnoreNodesExpr != null && advOptions.IgnoreNodesExpr != "")
            {
                XPathExpression expr = GenerateXPathExpression(
                    advOptions.IgnoreNodesExpr,
                    advOptions,
                    nav);

                _SourceDoc.IgnoreNodes(expr);
                _TargetDoc.IgnoreNodes(expr);
            }

            if (advOptions.IgnoreValuesExpr != null && advOptions.IgnoreValuesExpr != "")
            {
                XPathExpression expr = GenerateXPathExpression(
                    advOptions.IgnoreValuesExpr,
                    advOptions,
                    nav);

                _SourceDoc.IgnoreValues(expr);
                _TargetDoc.IgnoreValues(expr);
            }

            return Diff();
        }

        private static XPathExpression GenerateXPathExpression(string expression, XmlDiffAdvancedOptions advOptions, XPathNavigator nav)
        {
            XPathExpression expr = nav.Compile(expression);

            if (advOptions.HadAddedNamespace())
            {
                XmlNamespaceManager xnm = new XmlNamespaceManager(nav.NameTable);

                foreach (KeyValuePair<string, string> each in advOptions.AddedNamespaces)
                {
                    xnm.AddNamespace(each.Key, each.Value);
                }

                expr.SetContext(xnm);
            }

            return expr;
        }


        private bool Diff()
        {
            bool flag = false;
            _Writer = new XmlTextWriter(new StringWriter(_Output));
            _Writer.WriteStartElement(String.Empty, "Root", String.Empty);

            flag = CompareChildren(_SourceDoc, _TargetDoc);

            _Writer.WriteEndElement();
            _Writer.Close();
            return flag;
        }

        //  This function is being called recursively to compare the children of a certain node.
        //  When calling this function the navigator should be pointing at the parent node whose children
        //  we wants to compare and return the navigator back to the same node before exiting from it.
        private bool CompareChildren(XmlDiffNode sourceNode, XmlDiffNode targetNode)
        {
            XmlDiffNode sourceChild = sourceNode.FirstChild;
            XmlDiffNode targetChild = targetNode.FirstChild;

            bool flag = true;
            bool tempFlag = true;

            DiffType result;

            // Loop to compare all the child elements of the parent node
            while (sourceChild != null || targetChild != null)
            {
                //Console.WriteLine( (sourceChild!=null)?(sourceChild.NodeType.ToString()):"null" );
                //Console.WriteLine( (targetChild!=null)?(targetChild.NodeType.ToString()):"null" );
                if (sourceChild != null)
                {
                    if (targetChild != null)
                    {
                        // Both Source and Target Read successful
                        if ((result = CompareNodes(sourceChild, targetChild)) == DiffType.Success)
                        {
                            // Child nodes compared successfully, write out the result
                            WriteResult(sourceChild, targetChild, DiffType.Success);
                            // Check whether this Node has  Children, if it does call CompareChildren recursively
                            if (sourceChild.FirstChild != null)
                            {
                                tempFlag = CompareChildren(sourceChild, targetChild);
                            }
                            else if (targetChild.FirstChild != null)
                            {
                                WriteResult(null, targetChild, DiffType.TargetExtra);
                                tempFlag = false;
                            }
                            // set the compare flag
                            flag = (flag && tempFlag);

                            // Writing out End Element to the result
                            if (sourceChild.NodeType == XmlDiffNodeType.Element && !(sourceChild is XmlDiffEmptyElement))
                            {
                                XmlDiffElement sourceElem = sourceChild as XmlDiffElement;
                                XmlDiffElement targetElem = targetChild as XmlDiffElement;
                                Debug.Assert(sourceElem != null);
                                Debug.Assert(targetElem != null);
                                if (!DontWriteMatchingNodesToOutput && !DontWriteAnythingToOutput)
                                {
                                    _Writer.WriteStartElement(String.Empty, "Node", String.Empty);
                                    _Writer.WriteAttributeString(String.Empty, "SourceLineNum", String.Empty, sourceElem.EndLineNumber.ToString());
                                    _Writer.WriteAttributeString(String.Empty, "SourceLinePos", String.Empty, sourceElem.EndLinePosition.ToString());
                                    _Writer.WriteAttributeString(String.Empty, "TargetLineNum", String.Empty, targetElem.EndLineNumber.ToString());
                                    _Writer.WriteAttributeString(String.Empty, "TargetLinePos", String.Empty, targetElem.EndLinePosition.ToString());
                                    _Writer.WriteStartElement(String.Empty, "Diff", String.Empty);
                                    _Writer.WriteEndElement();

                                    _Writer.WriteStartElement(String.Empty, "Lexical-equal", String.Empty);
                                    _Writer.WriteCData("</" + sourceElem.Name + ">");
                                    _Writer.WriteEndElement();
                                    _Writer.WriteEndElement();
                                }
                            }

                            // Move to Next child
                            sourceChild = sourceChild.NextSibling;
                            targetChild = targetChild.NextSibling;
                        }
                        else
                        {
                            // Child nodes not matched, start the recovery process
                            bool recoveryFlag = false;

                            // Try to match the source node with the target nodes at the same level
                            XmlDiffNode backupTargetChild = targetChild.NextSibling;
                            while (!recoveryFlag && backupTargetChild != null)
                            {
                                if (CompareNodes(sourceChild, backupTargetChild) == DiffType.Success)
                                {
                                    recoveryFlag = true;
                                    do
                                    {
                                        WriteResult(null, targetChild, DiffType.TargetExtra);
                                        targetChild = targetChild.NextSibling;
                                    } while (targetChild != backupTargetChild);
                                    break;
                                }
                                backupTargetChild = backupTargetChild.NextSibling;
                            }

                            // If not recovered, try to match the target node with the source nodes at the same level
                            if (!recoveryFlag)
                            {
                                XmlDiffNode backupSourceChild = sourceChild.NextSibling;
                                while (!recoveryFlag && backupSourceChild != null)
                                {
                                    if (CompareNodes(backupSourceChild, targetChild) == DiffType.Success)
                                    {
                                        recoveryFlag = true;
                                        do
                                        {
                                            WriteResult(sourceChild, null, DiffType.SourceExtra);
                                            sourceChild = sourceChild.NextSibling;
                                        } while (sourceChild != backupSourceChild);
                                        break;
                                    }
                                    backupSourceChild = backupSourceChild.NextSibling;
                                }
                            }

                            // If still not recovered, write both of them as different nodes and move on
                            if (!recoveryFlag)
                            {
                                WriteResult(sourceChild, targetChild, result);

                                // Check whether this Node has  Children, if it does call CompareChildren recursively
                                if (sourceChild.FirstChild != null)
                                {
                                    tempFlag = CompareChildren(sourceChild, targetChild);
                                }
                                else if (targetChild.FirstChild != null)
                                {
                                    WriteResult(null, targetChild, DiffType.TargetExtra);
                                    tempFlag = false;
                                }

                                // Writing out End Element to the result
                                bool bSourceNonEmpElemEnd = (sourceChild.NodeType == XmlDiffNodeType.Element && !(sourceChild is XmlDiffEmptyElement));
                                bool bTargetNonEmpElemEnd = (targetChild.NodeType == XmlDiffNodeType.Element && !(targetChild is XmlDiffEmptyElement));
                                if (bSourceNonEmpElemEnd || bTargetNonEmpElemEnd)
                                {
                                    XmlDiffElement sourceElem = sourceChild as XmlDiffElement;
                                    XmlDiffElement targetElem = targetChild as XmlDiffElement;
                                    if (!DontWriteAnythingToOutput)
                                    {
                                        _Writer.WriteStartElement(String.Empty, "Node", String.Empty);
                                        _Writer.WriteAttributeString(String.Empty, "SourceLineNum", String.Empty, (sourceElem != null) ? sourceElem.EndLineNumber.ToString() : "-1");
                                        _Writer.WriteAttributeString(String.Empty, "SourceLinePos", String.Empty, (sourceElem != null) ? sourceElem.EndLinePosition.ToString() : "-1");
                                        _Writer.WriteAttributeString(String.Empty, "TargetLineNum", String.Empty, (targetElem != null) ? targetElem.EndLineNumber.ToString() : "-1");
                                        _Writer.WriteAttributeString(String.Empty, "TargetLinePos", String.Empty, (targetElem != null) ? targetElem.EndLineNumber.ToString() : "-1");
                                        _Writer.WriteStartElement(String.Empty, "Diff", String.Empty);
                                        _Writer.WriteAttributeString(String.Empty, "DiffType", String.Empty, GetDiffType(result));
                                        if (bSourceNonEmpElemEnd)
                                        {
                                            _Writer.WriteStartElement(String.Empty, "File1", String.Empty);
                                            _Writer.WriteCData("</" + sourceElem.Name + ">");
                                            _Writer.WriteEndElement();
                                        }

                                        if (bTargetNonEmpElemEnd)
                                        {
                                            _Writer.WriteStartElement(String.Empty, "File2", String.Empty);
                                            _Writer.WriteCData("</" + targetElem.Name + ">");
                                            _Writer.WriteEndElement();
                                        }

                                        _Writer.WriteEndElement();

                                        _Writer.WriteStartElement(String.Empty, "Lexical-equal", String.Empty);
                                        _Writer.WriteEndElement();
                                        _Writer.WriteEndElement();
                                    }
                                }

                                sourceChild = sourceChild.NextSibling;
                                targetChild = targetChild.NextSibling;
                            }

                            flag = false;
                        }
                    }
                    else
                    {
                        // SourceRead NOT NULL targetRead is NULL
                        WriteResult(sourceChild, null, DiffType.SourceExtra);
                        flag = false;
                        sourceChild = sourceChild.NextSibling;
                    }
                }
                else if (targetChild != null)
                {
                    // SourceRead is NULL and targetRead is NOT NULL
                    WriteResult(null, targetChild, DiffType.TargetExtra);
                    flag = false;
                    targetChild = targetChild.NextSibling;
                }
                else
                {
                    //Both SourceRead and TargetRead is NULL
                    Debug.Assert(false, "Impossible Situation for comparison");
                }
            }
            return flag;
        }

        /// <summary>
        /// Combines multiple adjacent text nodes into a single node.
        /// Sometimes the binary reader/writer will break text nodes in multiple
        /// nodes; i.e., if the user calls
        /// <code>writer.WriteString(&quot;foo&quot;); writer.WriteString(&quot;bar&quot;)</code>
        /// on the writer and then iterate calling reader.Read() on the document, the binary
        /// reader will report two text nodes (&quot;foo&quot;, &quot;bar&quot;), while the
        /// other readers will report a single text node (&quot;foobar&quot;) – notice that
        /// this is not a problem; calling ReadString, or Read[Element]ContentAsString in
        /// all readers will return &quot;foobar&quot;.
        /// </summary>
        /// <param name="node">the node to be normalized. Any siblings of
        /// type XmlDiffCharacterData will be removed, with their values appended
        /// to the value of this node</param>
        private void DoConcatenateAdjacentTextNodes(XmlDiffCharacterData node)
        {
            if (node.NextSibling == null) return;
            if (!(node.NextSibling is XmlDiffCharacterData)) return;
            StringBuilder sb = new StringBuilder();
            sb.Append(node.Value);
            XmlDiffCharacterData nextNode = (node.NextSibling as XmlDiffCharacterData);
            while (nextNode != null)
            {
                sb.Append(nextNode.Value);
                XmlDiffCharacterData curNextNode = nextNode;
                nextNode = (nextNode.NextSibling as XmlDiffCharacterData);
                curNextNode.ParentNode.DeleteChild(curNextNode);
            }
            node.Value = sb.ToString();
        }

        // This function compares the two nodes passed to it, depending upon the options set by the user.
        private DiffType CompareNodes(XmlDiffNode sourceNode, XmlDiffNode targetNode)
        {
            if (sourceNode.NodeType != targetNode.NodeType)
            {
                return DiffType.NodeType;
            }
            switch (sourceNode.NodeType)
            {
                case XmlDiffNodeType.Element:
                    XmlDiffElement sourceElem = sourceNode as XmlDiffElement;
                    XmlDiffElement targetElem = targetNode as XmlDiffElement;
                    Debug.Assert(sourceElem != null);
                    Debug.Assert(targetElem != null);
                    if (!IgnoreNS)
                    {
                        if (sourceElem.NamespaceURI != targetElem.NamespaceURI)
                        {
                            return DiffType.NS;
                        }
                    }
                    if (!IgnorePrefix)
                    {
                        if (sourceElem.Prefix != targetElem.Prefix)
                        {
                            return DiffType.Prefix;
                        }
                    }

                    if (sourceElem.LocalName != targetElem.LocalName)
                    {
                        return DiffType.Element;
                    }
                    if (!IgnoreEmptyElement)
                    {
                        if ((sourceElem is XmlDiffEmptyElement) != (targetElem is XmlDiffEmptyElement))
                        {
                            return DiffType.Element;
                        }
                    }
                    if (!CompareAttributes(sourceElem, targetElem))
                    {
                        return DiffType.Attribute;
                    }
                    break;
                case XmlDiffNodeType.Text:
                case XmlDiffNodeType.Comment:
                case XmlDiffNodeType.WS:
                    XmlDiffCharacterData sourceText = sourceNode as XmlDiffCharacterData;
                    XmlDiffCharacterData targetText = targetNode as XmlDiffCharacterData;
                    Debug.Assert(sourceText != null);
                    Debug.Assert(targetText != null);

                    if (ConcatenateAdjacentTextNodes)
                    {
                        DoConcatenateAdjacentTextNodes(sourceText);
                        DoConcatenateAdjacentTextNodes(targetText);
                    }

                    if (IgnoreWhitespace)
                    {
                        if (sourceText.Value.Trim() == targetText.Value.Trim())
                        {
                            return DiffType.Success;
                        }
                    }
                    else
                    {
                        if (sourceText.Value == targetText.Value)
                        {
                            return DiffType.Success;
                        }
                    }
                    if (sourceText.NodeType == XmlDiffNodeType.Text || sourceText.NodeType == XmlDiffNodeType.WS) //should ws nodes also as text nodes???
                    {
                        return DiffType.Text;
                    }
                    else if (sourceText.NodeType == XmlDiffNodeType.Comment)
                    {
                        return DiffType.Comment;
                    }
                    else if (sourceText.NodeType == XmlDiffNodeType.CData)
                    {
                        return DiffType.CData;
                    }
                    else
                    {
                        return DiffType.None;
                    }
                case XmlDiffNodeType.PI:
                    XmlDiffProcessingInstruction sourcePI = sourceNode as XmlDiffProcessingInstruction;
                    XmlDiffProcessingInstruction targetPI = targetNode as XmlDiffProcessingInstruction;
                    Debug.Assert(sourcePI != null);
                    Debug.Assert(targetPI != null);
                    if (sourcePI.Name != targetPI.Name || sourcePI.Value != targetPI.Value)
                    {
                        return DiffType.PI;
                    }
                    break;
                default:
                    break;
            }

            return DiffType.Success;
        }

        // This function writes the result in XML format so that it can be used by other applications to display the diff
        private void WriteResult(XmlDiffNode sourceNode, XmlDiffNode targetNode, DiffType result)
        {
            if (DontWriteAnythingToOutput) return;

            if (result != DiffType.Success || !DontWriteMatchingNodesToOutput)
            {
                _Writer.WriteStartElement(String.Empty, "Node", String.Empty);
                _Writer.WriteAttributeString(String.Empty, "SourceLineNum", String.Empty, (sourceNode != null) ? sourceNode.LineNumber.ToString() : "-1");
                _Writer.WriteAttributeString(String.Empty, "SourceLinePos", String.Empty, (sourceNode != null) ? sourceNode.LinePosition.ToString() : "-1");
                _Writer.WriteAttributeString(String.Empty, "TargetLineNum", String.Empty, (targetNode != null) ? targetNode.LineNumber.ToString() : "-1");
                _Writer.WriteAttributeString(String.Empty, "TargetLinePos", String.Empty, (targetNode != null) ? targetNode.LinePosition.ToString() : "-1");
            }
            if (result == DiffType.Success)
            {
                if (!DontWriteMatchingNodesToOutput)
                {
                    _Writer.WriteStartElement(String.Empty, "Diff", String.Empty);
                    _Writer.WriteEndElement();

                    _Writer.WriteStartElement(String.Empty, "Lexical-equal", String.Empty);
                    if (sourceNode.NodeType == XmlDiffNodeType.CData)
                    {
                        _Writer.WriteString("<![CDATA[");
                        _Writer.WriteCData(GetNodeText(sourceNode, result));
                        _Writer.WriteString("]]>");
                    }
                    else
                    {
                        _Writer.WriteCData(GetNodeText(sourceNode, result));
                    }
                    _Writer.WriteEndElement();
                }
            }
            else
            {
                _Writer.WriteStartElement(String.Empty, "Diff", String.Empty);
                _Writer.WriteAttributeString(String.Empty, "DiffType", String.Empty, GetDiffType(result));

                if (sourceNode != null)
                {
                    _Writer.WriteStartElement(String.Empty, "File1", String.Empty);
                    if (sourceNode.NodeType == XmlDiffNodeType.CData)
                    {
                        _Writer.WriteString("<![CDATA[");
                        _Writer.WriteCData(GetNodeText(sourceNode, result));
                        _Writer.WriteString("]]>");
                    }
                    else
                    {
                        _Writer.WriteString(GetNodeText(sourceNode, result));
                    }
                    _Writer.WriteEndElement();
                }

                if (targetNode != null)
                {
                    _Writer.WriteStartElement(String.Empty, "File2", String.Empty);
                    if (targetNode.NodeType == XmlDiffNodeType.CData)
                    {
                        _Writer.WriteString("<![CDATA[");
                        _Writer.WriteCData(GetNodeText(targetNode, result));
                        _Writer.WriteString("]]>");
                    }
                    else
                    {
                        _Writer.WriteString(GetNodeText(targetNode, result));
                    }
                    _Writer.WriteEndElement();
                }

                _Writer.WriteEndElement();

                _Writer.WriteStartElement(String.Empty, "Lexical-equal", String.Empty);
                _Writer.WriteEndElement();
            }
            if (result != DiffType.Success || !DontWriteMatchingNodesToOutput)
            {
                _Writer.WriteEndElement();
            }
        }

        // This is a helper function for WriteResult.  It gets the Xml representation of the different node we wants
        // to write out and all it's children.
        private String GetNodeText(XmlDiffNode diffNode, DiffType result)
        {
            string text = string.Empty;
            switch (diffNode.NodeType)
            {
                case XmlDiffNodeType.Element:
                    if (result == DiffType.SourceExtra || result == DiffType.TargetExtra)
                        return diffNode.OuterXml;
                    StringWriter str = new StringWriter();
                    XmlTextWriter writer = new XmlTextWriter(str);
                    XmlDiffElement diffElem = diffNode as XmlDiffElement;
                    Debug.Assert(diffNode != null);
                    writer.WriteStartElement(diffElem.Prefix, diffElem.LocalName, diffElem.NamespaceURI);
                    XmlDiffAttribute diffAttr = diffElem.FirstAttribute;
                    while (diffAttr != null)
                    {
                        writer.WriteAttributeString(diffAttr.Prefix, diffAttr.LocalName, diffAttr.NamespaceURI, diffAttr.Value);
                        diffAttr = (XmlDiffAttribute)diffAttr.NextSibling;
                    }
                    if (diffElem is XmlDiffEmptyElement)
                    {
                        writer.WriteEndElement();
                        text = str.ToString();
                    }
                    else
                    {
                        text = str.ToString();
                        text += ">";
                    }
                    writer.Close();
                    break;
                case XmlDiffNodeType.CData:
                    text = ((XmlDiffCharacterData)diffNode).Value;
                    break;
                default:
                    text = diffNode.OuterXml;
                    break;
            }
            return text;
        }

        // This function is used to compare the attributes of an element node according to the options set by the user.
        private bool CompareAttributes(XmlDiffElement sourceElem, XmlDiffElement targetElem)
        {
            Debug.Assert(sourceElem != null);
            Debug.Assert(targetElem != null);
            if (sourceElem.AttributeCount != targetElem.AttributeCount)
                return false;

            if (sourceElem.AttributeCount == 0)
                return true;

            XmlDiffAttribute sourceAttr = sourceElem.FirstAttribute;
            XmlDiffAttribute targetAttr = targetElem.FirstAttribute;

            const string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";

            if (!IgnoreAttributeOrder)
            {
                while (sourceAttr != null && targetAttr != null)
                {
                    if (!IgnoreNS)
                    {
                        if (sourceAttr.NamespaceURI != targetAttr.NamespaceURI)
                        {
                            return false;
                        }
                    }

                    if (!IgnorePrefix)
                    {
                        if (sourceAttr.Prefix != targetAttr.Prefix)
                        {
                            return false;
                        }
                    }

                    if (sourceAttr.NamespaceURI == xmlnsNamespace && targetAttr.NamespaceURI == xmlnsNamespace)
                    {
                        if (!IgnorePrefix && (sourceAttr.LocalName != targetAttr.LocalName))
                        {
                            return false;
                        }
                        if (!IgnoreNS && (sourceAttr.Value != targetAttr.Value))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (sourceAttr.LocalName != targetAttr.LocalName)
                        {
                            return false;
                        }
                        if (sourceAttr.Value != targetAttr.Value)
                        {
                            if (ParseAttributeValuesAsQName)
                            {
                                if (sourceAttr.ValueAsQName != null)
                                {
                                    if (!sourceAttr.ValueAsQName.Equals(targetAttr.ValueAsQName)) return false;
                                }
                                else
                                {
                                    if (targetAttr.ValueAsQName != null) return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    sourceAttr = (XmlDiffAttribute)(sourceAttr.NextSibling);
                    targetAttr = (XmlDiffAttribute)(targetAttr.NextSibling);
                }
            }
            else
            {
                Hashtable sourceAttributeMap = new Hashtable();
                Hashtable targetAttributeMap = new Hashtable();

                while (sourceAttr != null && targetAttr != null)
                {
                    if (IgnorePrefix && sourceAttr.NamespaceURI == xmlnsNamespace)
                    {
                        // do nothing
                    }
                    else
                    {
                        string localNameAndNamespace = sourceAttr.LocalName + "<&&>" + sourceAttr.NamespaceURI;
                        sourceAttributeMap.Add(localNameAndNamespace, sourceAttr);
                    }
                    if (IgnorePrefix && targetAttr.NamespaceURI == xmlnsNamespace)
                    {
                        // do nothing
                    }
                    else
                    {
                        string localNameAndNamespace = targetAttr.LocalName + "<&&>" + targetAttr.NamespaceURI;
                        targetAttributeMap.Add(localNameAndNamespace, targetAttr);
                    }

                    sourceAttr = (XmlDiffAttribute)(sourceAttr.NextSibling);
                    targetAttr = (XmlDiffAttribute)(targetAttr.NextSibling);
                }

                if (sourceAttributeMap.Count != targetAttributeMap.Count)
                {
                    return false;
                }

                foreach (string sourceKey in sourceAttributeMap.Keys)
                {
                    if (!targetAttributeMap.ContainsKey(sourceKey))
                    {
                        return false;
                    }
                    sourceAttr = (XmlDiffAttribute)sourceAttributeMap[sourceKey];
                    targetAttr = (XmlDiffAttribute)targetAttributeMap[sourceKey];

                    if (!IgnorePrefix)
                    {
                        if (sourceAttr.Prefix != targetAttr.Prefix)
                        {
                            return false;
                        }
                    }

                    if (sourceAttr.Value != targetAttr.Value)
                    {
                        if (ParseAttributeValuesAsQName)
                        {
                            if (sourceAttr.ValueAsQName != null)
                            {
                                if (!sourceAttr.ValueAsQName.Equals(targetAttr.ValueAsQName)) return false;
                            }
                            else
                            {
                                if (targetAttr.ValueAsQName != null) return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public string ToXml()
        {
            if (_Output != null)
                return _Output.ToString();
            return string.Empty;
        }

        public void ToXml(string fileName)
        {
            FileStream file;
            file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            StreamWriter writer = new StreamWriter(file);
            writer.Write("<?xml-stylesheet type='text/xsl' href='diff.xsl'?>");
            writer.Write(ToXml());
            writer.Close();
            file.Close();
        }

        public void ToXml(Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream);
            writer.Write("<?xml-stylesheet type='text/xsl' href='diff.xsl'?>");
            writer.Write(ToXml());
            writer.Close();
        }

        private String GetDiffType(DiffType type)
        {
            switch (type)
            {
                case DiffType.None:
                    return String.Empty;
                case DiffType.Element:
                    return "1";
                case DiffType.Whitespace:
                    return "2";
                case DiffType.Comment:
                    return "3";
                case DiffType.PI:
                    return "4";
                case DiffType.Text:
                    return "5";
                case DiffType.Attribute:
                    return "6";
                case DiffType.NS:
                    return "7";
                case DiffType.Prefix:
                    return "8";
                case DiffType.SourceExtra:
                    return "9";
                case DiffType.TargetExtra:
                    return "10";
                case DiffType.NodeType:
                    return "11";
                case DiffType.CData:
                    return "12";
                default:
                    return String.Empty;
            }
        }
    }
}
