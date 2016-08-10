// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;
    using System.Collections;

    internal sealed class RecordBuilder
    {
        private int _outputState;
        private RecordBuilder _next;

        private RecordOutput _output;

        // Atomization:
        private XmlNameTable _nameTable;
        private OutKeywords _atoms;

        // Namespace manager for output
        private OutputScopeManager _scopeManager;

        // Main node + Fields Collection
        private BuilderInfo _mainNode = new BuilderInfo();
        private ArrayList _attributeList = new ArrayList();
        private int _attributeCount;
        private ArrayList _namespaceList = new ArrayList();
        private int _namespaceCount;
        private BuilderInfo _dummy = new BuilderInfo();

        // Current position in the list
        private BuilderInfo _currentInfo;
        // Builder state
        private bool _popScope;
        private int _recordState;
        private int _recordDepth;

        private const int NoRecord = 0;      // No part of a new record was generated (old record was cleared out)
        private const int SomeRecord = 1;      // Record was generated partially        (can be eventually record)
        private const int HaveRecord = 2;      // Record was fully generated

        private const char s_Minus = '-';
        private const string s_Space = " ";
        private const string s_SpaceMinus = " -";
        private const char s_Question = '?';
        private const char s_Greater = '>';
        private const string s_SpaceGreater = " >";

        private const string PrefixFormat = "xp_{0}";

        internal RecordBuilder(RecordOutput output, XmlNameTable nameTable)
        {
            Debug.Assert(output != null);
            _output = output;
            _nameTable = nameTable != null ? nameTable : new NameTable();
            _atoms = new OutKeywords(_nameTable);
            _scopeManager = new OutputScopeManager(_nameTable, _atoms);
        }

        //
        // Internal properties
        //

        internal int OutputState
        {
            get { return _outputState; }
            set { _outputState = value; }
        }

        internal RecordBuilder Next
        {
            get { return _next; }
            set { _next = value; }
        }

        internal RecordOutput Output
        {
            get { return _output; }
        }

        internal BuilderInfo MainNode
        {
            get { return _mainNode; }
        }

        internal ArrayList AttributeList
        {
            get { return _attributeList; }
        }

        internal int AttributeCount
        {
            get { return _attributeCount; }
        }

        internal OutputScopeManager Manager
        {
            get { return _scopeManager; }
        }

        private void ValueAppend(string s, bool disableOutputEscaping)
        {
            _currentInfo.ValueAppend(s, disableOutputEscaping);
        }

        private bool CanOutput(int state)
        {
            Debug.Assert(_recordState != HaveRecord);

            // If we have no record cached or the next event doesn't start new record, we are OK

            if (_recordState == NoRecord || (state & StateMachine.BeginRecord) == 0)
            {
                return true;
            }
            else
            {
                _recordState = HaveRecord;
                FinalizeRecord();
                SetEmptyFlag(state);
                return _output.RecordDone(this) == Processor.OutputResult.Continue;
            }
        }

        internal Processor.OutputResult BeginEvent(int state, XPathNodeType nodeType, string prefix, string name, string nspace, bool empty, Object htmlProps, bool search)
        {
            if (!CanOutput(state))
            {
                return Processor.OutputResult.Overflow;
            }

            Debug.Assert(_recordState == NoRecord || (state & StateMachine.BeginRecord) == 0);

            AdjustDepth(state);
            ResetRecord(state);
            PopElementScope();

            prefix = (prefix != null) ? _nameTable.Add(prefix) : _atoms.Empty;
            name = (name != null) ? _nameTable.Add(name) : _atoms.Empty;
            nspace = (nspace != null) ? _nameTable.Add(nspace) : _atoms.Empty;

            switch (nodeType)
            {
                case XPathNodeType.Element:
                    _mainNode.htmlProps = htmlProps as HtmlElementProps;
                    _mainNode.search = search;
                    BeginElement(prefix, name, nspace, empty);
                    break;
                case XPathNodeType.Attribute:
                    BeginAttribute(prefix, name, nspace, htmlProps, search);
                    break;
                case XPathNodeType.Namespace:
                    BeginNamespace(name, nspace);
                    break;
                case XPathNodeType.Text:
                    break;
                case XPathNodeType.ProcessingInstruction:
                    if (BeginProcessingInstruction(prefix, name, nspace) == false)
                    {
                        return Processor.OutputResult.Error;
                    }
                    break;
                case XPathNodeType.Comment:
                    BeginComment();
                    break;
                case XPathNodeType.Root:
                    break;
                case XPathNodeType.Whitespace:
                case XPathNodeType.SignificantWhitespace:
                case XPathNodeType.All:
                    break;
            }

            return CheckRecordBegin(state);
        }

        internal Processor.OutputResult TextEvent(int state, string text, bool disableOutputEscaping)
        {
            if (!CanOutput(state))
            {
                return Processor.OutputResult.Overflow;
            }

            Debug.Assert(_recordState == NoRecord || (state & StateMachine.BeginRecord) == 0);

            AdjustDepth(state);
            ResetRecord(state);
            PopElementScope();

            if ((state & StateMachine.BeginRecord) != 0)
            {
                _currentInfo.Depth = _recordDepth;
                _currentInfo.NodeType = XmlNodeType.Text;
            }

            ValueAppend(text, disableOutputEscaping);

            return CheckRecordBegin(state);
        }

        internal Processor.OutputResult EndEvent(int state, XPathNodeType nodeType)
        {
            if (!CanOutput(state))
            {
                return Processor.OutputResult.Overflow;
            }

            AdjustDepth(state);
            PopElementScope();
            _popScope = (state & StateMachine.PopScope) != 0;

            if ((state & StateMachine.EmptyTag) != 0 && _mainNode.IsEmptyTag == true)
            {
                return Processor.OutputResult.Continue;
            }

            ResetRecord(state);

            if ((state & StateMachine.BeginRecord) != 0)
            {
                if (nodeType == XPathNodeType.Element)
                {
                    EndElement();
                }
            }

            return CheckRecordEnd(state);
        }

        internal void Reset()
        {
            if (_recordState == HaveRecord)
            {
                _recordState = NoRecord;
            }
        }

        internal void TheEnd()
        {
            if (_recordState == SomeRecord)
            {
                _recordState = HaveRecord;
                FinalizeRecord();
                _output.RecordDone(this);
            }
            _output.TheEnd();
        }

        //
        // Utility implementation methods
        //

        private int FindAttribute(string name, string nspace, ref string prefix)
        {
            Debug.Assert(_attributeCount <= _attributeList.Count);

            for (int attrib = 0; attrib < _attributeCount; attrib++)
            {
                Debug.Assert(_attributeList[attrib] != null && _attributeList[attrib] is BuilderInfo);

                BuilderInfo attribute = (BuilderInfo)_attributeList[attrib];

                if (Ref.Equal(attribute.LocalName, name))
                {
                    if (Ref.Equal(attribute.NamespaceURI, nspace))
                    {
                        return attrib;
                    }
                    if (Ref.Equal(attribute.Prefix, prefix))
                    {
                        // prefix conflict. Should be renamed.
                        prefix = string.Empty;
                    }
                }
            }

            return -1;
        }

        private void BeginElement(string prefix, string name, string nspace, bool empty)
        {
            Debug.Assert(_attributeCount == 0);

            _currentInfo.NodeType = XmlNodeType.Element;
            _currentInfo.Prefix = prefix;
            _currentInfo.LocalName = name;
            _currentInfo.NamespaceURI = nspace;
            _currentInfo.Depth = _recordDepth;
            _currentInfo.IsEmptyTag = empty;

            _scopeManager.PushScope(name, nspace, prefix);
        }

        private void EndElement()
        {
            Debug.Assert(_attributeCount == 0);
            OutputScope elementScope = _scopeManager.CurrentElementScope;

            _currentInfo.NodeType = XmlNodeType.EndElement;
            _currentInfo.Prefix = elementScope.Prefix;
            _currentInfo.LocalName = elementScope.Name;
            _currentInfo.NamespaceURI = elementScope.Namespace;
            _currentInfo.Depth = _recordDepth;
        }

        private int NewAttribute()
        {
            if (_attributeCount >= _attributeList.Count)
            {
                Debug.Assert(_attributeCount == _attributeList.Count);
                _attributeList.Add(new BuilderInfo());
            }
            return _attributeCount++;
        }

        private void BeginAttribute(string prefix, string name, string nspace, Object htmlAttrProps, bool search)
        {
            int attrib = FindAttribute(name, nspace, ref prefix);

            if (attrib == -1)
            {
                attrib = NewAttribute();
            }

            Debug.Assert(_attributeList[attrib] != null && _attributeList[attrib] is BuilderInfo);

            BuilderInfo attribute = (BuilderInfo)_attributeList[attrib];
            attribute.Initialize(prefix, name, nspace);
            attribute.Depth = _recordDepth;
            attribute.NodeType = XmlNodeType.Attribute;
            attribute.htmlAttrProps = htmlAttrProps as HtmlAttributeProps;
            attribute.search = search;
            _currentInfo = attribute;
        }

        private void BeginNamespace(string name, string nspace)
        {
            bool thisScope = false;
            if (Ref.Equal(name, _atoms.Empty))
            {
                if (Ref.Equal(nspace, _scopeManager.DefaultNamespace))
                {
                    // Main Node is OK
                }
                else if (Ref.Equal(_mainNode.NamespaceURI, _atoms.Empty))
                {
                    // http://www.w3.org/1999/11/REC-xslt-19991116-errata/ E25 
                    // Should throw an error but ingnoring it in Everett. 
                    // Would be a breaking change
                }
                else
                {
                    DeclareNamespace(nspace, name);
                }
            }
            else
            {
                string nspaceDeclared = _scopeManager.ResolveNamespace(name, out thisScope);
                if (nspaceDeclared != null)
                {
                    if (!Ref.Equal(nspace, nspaceDeclared))
                    {
                        if (!thisScope)
                        {
                            DeclareNamespace(nspace, name);
                        }
                    }
                }
                else
                {
                    DeclareNamespace(nspace, name);
                }
            }
            _currentInfo = _dummy;
            _currentInfo.NodeType = XmlNodeType.Attribute;
        }

        private bool BeginProcessingInstruction(string prefix, string name, string nspace)
        {
            _currentInfo.NodeType = XmlNodeType.ProcessingInstruction;
            _currentInfo.Prefix = prefix;
            _currentInfo.LocalName = name;
            _currentInfo.NamespaceURI = nspace;
            _currentInfo.Depth = _recordDepth;
            return true;
        }

        private void BeginComment()
        {
            _currentInfo.NodeType = XmlNodeType.Comment;
            _currentInfo.Depth = _recordDepth;
        }

        private void AdjustDepth(int state)
        {
            switch (state & StateMachine.DepthMask)
            {
                case StateMachine.DepthUp:
                    _recordDepth++;
                    break;
                case StateMachine.DepthDown:
                    _recordDepth--;
                    break;
                default:
                    break;
            }
        }

        private void ResetRecord(int state)
        {
            Debug.Assert(_recordState == NoRecord || _recordState == SomeRecord);

            if ((state & StateMachine.BeginRecord) != 0)
            {
                _attributeCount = 0;
                _namespaceCount = 0;
                _currentInfo = _mainNode;

                _currentInfo.Initialize(_atoms.Empty, _atoms.Empty, _atoms.Empty);
                _currentInfo.NodeType = XmlNodeType.None;
                _currentInfo.IsEmptyTag = false;
                _currentInfo.htmlProps = null;
                _currentInfo.htmlAttrProps = null;
            }
        }

        private void PopElementScope()
        {
            if (_popScope)
            {
                _scopeManager.PopScope();
                _popScope = false;
            }
        }

        private Processor.OutputResult CheckRecordBegin(int state)
        {
            Debug.Assert(_recordState == NoRecord || _recordState == SomeRecord);

            if ((state & StateMachine.EndRecord) != 0)
            {
                _recordState = HaveRecord;
                FinalizeRecord();
                SetEmptyFlag(state);
                return _output.RecordDone(this);
            }
            else
            {
                _recordState = SomeRecord;
                return Processor.OutputResult.Continue;
            }
        }

        private Processor.OutputResult CheckRecordEnd(int state)
        {
            Debug.Assert(_recordState == NoRecord || _recordState == SomeRecord);

            if ((state & StateMachine.EndRecord) != 0)
            {
                _recordState = HaveRecord;
                FinalizeRecord();
                SetEmptyFlag(state);
                return _output.RecordDone(this);
            }
            else
            {
                // For end event, if there is no end token, don't force token
                return Processor.OutputResult.Continue;
            }
        }

        private void SetEmptyFlag(int state)
        {
            Debug.Assert(_mainNode != null);

            if ((state & StateMachine.BeginChild) != 0)
            {
                _mainNode.IsEmptyTag = false;
            }
        }


        private void AnalyzeSpaceLang()
        {
            Debug.Assert(_mainNode.NodeType == XmlNodeType.Element);

            for (int attr = 0; attr < _attributeCount; attr++)
            {
                Debug.Assert(_attributeList[attr] is BuilderInfo);
                BuilderInfo info = (BuilderInfo)_attributeList[attr];

                if (Ref.Equal(info.Prefix, _atoms.Xml))
                {
                    OutputScope scope = _scopeManager.CurrentElementScope;

                    if (Ref.Equal(info.LocalName, _atoms.Lang))
                    {
                        scope.Lang = info.Value;
                    }
                    else if (Ref.Equal(info.LocalName, _atoms.Space))
                    {
                        scope.Space = TranslateXmlSpace(info.Value);
                    }
                }
            }
        }

        private void FixupElement()
        {
            Debug.Assert(_mainNode.NodeType == XmlNodeType.Element);

            if (Ref.Equal(_mainNode.NamespaceURI, _atoms.Empty))
            {
                _mainNode.Prefix = _atoms.Empty;
            }

            if (Ref.Equal(_mainNode.Prefix, _atoms.Empty))
            {
                if (Ref.Equal(_mainNode.NamespaceURI, _scopeManager.DefaultNamespace))
                {
                    // Main Node is OK
                }
                else
                {
                    DeclareNamespace(_mainNode.NamespaceURI, _mainNode.Prefix);
                }
            }
            else
            {
                bool thisScope = false;
                string nspace = _scopeManager.ResolveNamespace(_mainNode.Prefix, out thisScope);
                if (nspace != null)
                {
                    if (!Ref.Equal(_mainNode.NamespaceURI, nspace))
                    {
                        if (thisScope)
                        {    // Prefix conflict
                            _mainNode.Prefix = GetPrefixForNamespace(_mainNode.NamespaceURI);
                        }
                        else
                        {
                            DeclareNamespace(_mainNode.NamespaceURI, _mainNode.Prefix);
                        }
                    }
                }
                else
                {
                    DeclareNamespace(_mainNode.NamespaceURI, _mainNode.Prefix);
                }
            }

            OutputScope elementScope = _scopeManager.CurrentElementScope;
            elementScope.Prefix = _mainNode.Prefix;
        }

        private void FixupAttributes(int attributeCount)
        {
            for (int attr = 0; attr < attributeCount; attr++)
            {
                Debug.Assert(_attributeList[attr] is BuilderInfo);
                BuilderInfo info = (BuilderInfo)_attributeList[attr];


                if (Ref.Equal(info.NamespaceURI, _atoms.Empty))
                {
                    info.Prefix = _atoms.Empty;
                }
                else
                {
                    if (Ref.Equal(info.Prefix, _atoms.Empty))
                    {
                        info.Prefix = GetPrefixForNamespace(info.NamespaceURI);
                    }
                    else
                    {
                        bool thisScope = false;
                        string nspace = _scopeManager.ResolveNamespace(info.Prefix, out thisScope);
                        if (nspace != null)
                        {
                            if (!Ref.Equal(info.NamespaceURI, nspace))
                            {
                                if (thisScope)
                                { // prefix conflict
                                    info.Prefix = GetPrefixForNamespace(info.NamespaceURI);
                                }
                                else
                                {
                                    DeclareNamespace(info.NamespaceURI, info.Prefix);
                                }
                            }
                        }
                        else
                        {
                            DeclareNamespace(info.NamespaceURI, info.Prefix);
                        }
                    }
                }
            }
        }

        private void AppendNamespaces()
        {
            for (int i = _namespaceCount - 1; i >= 0; i--)
            {
                BuilderInfo attribute = (BuilderInfo)_attributeList[NewAttribute()];
                attribute.Initialize((BuilderInfo)_namespaceList[i]);
            }
        }

        private void AnalyzeComment()
        {
            Debug.Assert(_mainNode.NodeType == XmlNodeType.Comment);
            Debug.Assert((object)_currentInfo == (object)_mainNode);

            StringBuilder newComment = null;
            string comment = _mainNode.Value;
            bool minus = false;
            int index = 0, begin = 0;

            for (; index < comment.Length; index++)
            {
                switch (comment[index])
                {
                    case s_Minus:
                        if (minus)
                        {
                            if (newComment == null)
                                newComment = new StringBuilder(comment, begin, index, 2 * comment.Length);
                            else
                                newComment.Append(comment, begin, index - begin);

                            newComment.Append(s_SpaceMinus);
                            begin = index + 1;
                        }
                        minus = true;
                        break;
                    default:
                        minus = false;
                        break;
                }
            }

            if (newComment != null)
            {
                if (begin < comment.Length)
                    newComment.Append(comment, begin, comment.Length - begin);

                if (minus)
                    newComment.Append(s_Space);

                _mainNode.Value = newComment.ToString();
            }
            else if (minus)
            {
                _mainNode.ValueAppend(s_Space, false);
            }
        }

        private void AnalyzeProcessingInstruction()
        {
            Debug.Assert(_mainNode.NodeType == XmlNodeType.ProcessingInstruction || _mainNode.NodeType == XmlNodeType.XmlDeclaration);
            //Debug.Assert((object) this.currentInfo == (object) this.mainNode);

            StringBuilder newPI = null;
            string pi = _mainNode.Value;
            bool question = false;
            int index = 0, begin = 0;

            for (; index < pi.Length; index++)
            {
                switch (pi[index])
                {
                    case s_Question:
                        question = true;
                        break;
                    case s_Greater:
                        if (question)
                        {
                            if (newPI == null)
                            {
                                newPI = new StringBuilder(pi, begin, index, 2 * pi.Length);
                            }
                            else
                            {
                                newPI.Append(pi, begin, index - begin);
                            }
                            newPI.Append(s_SpaceGreater);
                            begin = index + 1;
                        }
                        question = false;
                        break;
                    default:
                        question = false;
                        break;
                }
            }

            if (newPI != null)
            {
                if (begin < pi.Length)
                {
                    newPI.Append(pi, begin, pi.Length - begin);
                }
                _mainNode.Value = newPI.ToString();
            }
        }

        private void FinalizeRecord()
        {
            switch (_mainNode.NodeType)
            {
                case XmlNodeType.Element:
                    // Save count since FixupElement can add attribute...
                    int attributeCount = _attributeCount;

                    FixupElement();
                    FixupAttributes(attributeCount);
                    AnalyzeSpaceLang();
                    AppendNamespaces();
                    break;
                case XmlNodeType.Comment:
                    AnalyzeComment();
                    break;
                case XmlNodeType.ProcessingInstruction:
                    AnalyzeProcessingInstruction();
                    break;
            }
        }

        private int NewNamespace()
        {
            if (_namespaceCount >= _namespaceList.Count)
            {
                Debug.Assert(_namespaceCount == _namespaceList.Count);
                _namespaceList.Add(new BuilderInfo());
            }
            return _namespaceCount++;
        }

        private void DeclareNamespace(string nspace, string prefix)
        {
            int index = NewNamespace();

            Debug.Assert(_namespaceList[index] != null && _namespaceList[index] is BuilderInfo);

            BuilderInfo ns = (BuilderInfo)_namespaceList[index];
            if (prefix == _atoms.Empty)
            {
                ns.Initialize(_atoms.Empty, _atoms.Xmlns, _atoms.XmlnsNamespace);
            }
            else
            {
                ns.Initialize(_atoms.Xmlns, prefix, _atoms.XmlnsNamespace);
            }
            ns.Depth = _recordDepth;
            ns.NodeType = XmlNodeType.Attribute;
            ns.Value = nspace;

            _scopeManager.PushNamespace(prefix, nspace);
        }

        private string DeclareNewNamespace(string nspace)
        {
            string prefix = _scopeManager.GeneratePrefix(PrefixFormat);
            DeclareNamespace(nspace, prefix);
            return prefix;
        }

        internal string GetPrefixForNamespace(string nspace)
        {
            string prefix = null;

            if (_scopeManager.FindPrefix(nspace, out prefix))
            {
                Debug.Assert(prefix != null && prefix.Length > 0);
                return prefix;
            }
            else
            {
                return DeclareNewNamespace(nspace);
            }
        }

        private static XmlSpace TranslateXmlSpace(string space)
        {
            if (space == "default")
            {
                return XmlSpace.Default;
            }
            else if (space == "preserve")
            {
                return XmlSpace.Preserve;
            }
            else
            {
                return XmlSpace.None;
            }
        }
    }
}
