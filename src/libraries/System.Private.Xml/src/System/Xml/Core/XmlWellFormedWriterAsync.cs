// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;

// OpenIssue : is it better to cache the current namespace decls for each elem
//  as the current code does, or should it just always walk the namespace stack?

namespace System.Xml
{
    internal sealed partial class XmlWellFormedWriter : XmlWriter
    {
        public override Task WriteStartDocumentAsync()
        {
            return WriteStartDocumentImplAsync(XmlStandalone.Omit);
        }

        public override Task WriteStartDocumentAsync(bool standalone)
        {
            return WriteStartDocumentImplAsync(standalone ? XmlStandalone.Yes : XmlStandalone.No);
        }

        public override async Task WriteEndDocumentAsync()
        {
            try
            {
                // auto-close all elements
                while (_elemTop > 0)
                {
                    await WriteEndElementAsync().ConfigureAwait(false);
                }
                State prevState = _currentState;
                await AdvanceStateAsync(Token.EndDocument).ConfigureAwait(false);

                if (prevState != State.AfterRootEle)
                {
                    throw new ArgumentException(SR.Xml_NoRoot);
                }
                if (_rawWriter == null)
                {
                    await _writer.WriteEndDocumentAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
        {
            try
            {
                if (name == null || name.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyName);
                }

                XmlConvert.VerifyQName(name, ExceptionType.XmlException);

                if (_conformanceLevel == ConformanceLevel.Fragment)
                {
                    throw new InvalidOperationException(SR.Xml_DtdNotAllowedInFragment);
                }

                await AdvanceStateAsync(Token.Dtd).ConfigureAwait(false);
                if (_dtdWritten)
                {
                    _currentState = State.Error;
                    throw new InvalidOperationException(SR.Xml_DtdAlreadyWritten);
                }

                if (_conformanceLevel == ConformanceLevel.Auto)
                {
                    _conformanceLevel = ConformanceLevel.Document;
                    _stateTable = s_stateTableDocument;
                }

                int i;

                // check characters
                if (_checkCharacters)
                {
                    if (pubid != null)
                    {
                        if ((i = XmlCharType.IsPublicId(pubid)) >= 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(pubid, i)), nameof(pubid));
                        }
                    }
                    if (sysid != null)
                    {
                        if ((i = XmlCharType.IsOnlyCharData(sysid)) >= 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(sysid, i)), nameof(sysid));
                        }
                    }
                    if (subset != null)
                    {
                        if ((i = XmlCharType.IsOnlyCharData(subset)) >= 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(subset, i)), nameof(subset));
                        }
                    }
                }

                // write doctype
                await _writer.WriteDocTypeAsync(name, pubid, sysid, subset).ConfigureAwait(false);
                _dtdWritten = true;
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        //check if any exception before return the task
        private Task TryReturnTask(Task task)
        {
            if (task.IsSuccess())
            {
                return Task.CompletedTask;
            }
            else
            {
                return _TryReturnTask(task);
            }
        }

        private async Task _TryReturnTask(Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        //call nextTaskFun after task finish. Check exception.
        private Task SequenceRun<TArg>(Task task, Func<TArg, Task> nextTaskFun, TArg arg)
        {
            if (task.IsSuccess())
            {
                return TryReturnTask(nextTaskFun(arg));
            }
            else
            {
                return _SequenceRun(task, nextTaskFun, arg);
            }
        }

        private async Task _SequenceRun<TArg>(Task task, Func<TArg, Task> nextTaskFun, TArg arg)
        {
            try
            {
                await task.ConfigureAwait(false);
                await nextTaskFun(arg).ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override Task WriteStartElementAsync(string? prefix, string localName, string? ns)
        {
            try
            {
                // check local name
                if (localName == null || localName.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyLocalName);
                }
                CheckNCName(localName);

                Task task = AdvanceStateAsync(Token.StartElement);
                if (task.IsSuccess())
                {
                    return WriteStartElementAsync_NoAdvanceState(prefix, localName, ns);
                }
                else
                {
                    return WriteStartElementAsync_NoAdvanceState(task, prefix, localName, ns);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private Task WriteStartElementAsync_NoAdvanceState(string? prefix, string localName, string? ns)
        {
            try
            {
                // lookup prefix / namespace
                if (prefix == null)
                {
                    if (ns != null)
                    {
                        prefix = LookupPrefix(ns);
                    }
                    if (prefix == null)
                    {
                        prefix = string.Empty;
                    }
                }
                else if (prefix.Length > 0)
                {
                    CheckNCName(prefix);
                    if (ns == null)
                    {
                        ns = LookupNamespace(prefix);
                    }
                    if (ns == null || (ns != null && ns.Length == 0))
                    {
                        throw new ArgumentException(SR.Xml_PrefixForEmptyNs);
                    }
                }
                if (ns == null)
                {
                    ns = LookupNamespace(prefix);
                    if (ns == null)
                    {
                        Debug.Assert(prefix.Length == 0);
                        ns = string.Empty;
                    }
                }

                if (_elemTop == 0 && _rawWriter != null)
                {
                    // notify the underlying raw writer about the root level element
                    _rawWriter.OnRootElement(_conformanceLevel);
                }

                // write start tag
                Task task = _writer.WriteStartElementAsync(prefix, localName, ns);
                if (task.IsSuccess())
                {
                    WriteStartElementAsync_FinishWrite(prefix, localName, ns);
                }
                else
                {
                    return WriteStartElementAsync_FinishWrite(task, prefix, localName, ns);
                }
                return Task.CompletedTask;
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private async Task WriteStartElementAsync_NoAdvanceState(Task task, string? prefix, string localName, string? ns)
        {
            try
            {
                await task.ConfigureAwait(false);
                await WriteStartElementAsync_NoAdvanceState(prefix, localName, ns).ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private void WriteStartElementAsync_FinishWrite(string prefix, string localName, string ns)
        {
            try
            {
                // push element on stack and add/check namespace
                int top = ++_elemTop;
                if (top == _elemScopeStack.Length)
                {
                    ElementScope[] newStack = new ElementScope[top * 2];
                    Array.Copy(_elemScopeStack, newStack, top);
                    _elemScopeStack = newStack;
                }
                _elemScopeStack[top].Set(prefix, localName, ns, _nsTop);

                PushNamespaceImplicit(prefix, ns);

                if (_attrCount >= MaxAttrDuplWalkCount)
                {
                    _attrHashTable!.Clear();
                }
                _attrCount = 0;
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private async Task WriteStartElementAsync_FinishWrite(Task t, string prefix, string localName, string ns)
        {
            try
            {
                await t.ConfigureAwait(false);
                WriteStartElementAsync_FinishWrite(prefix, localName, ns);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override Task WriteEndElementAsync()
        {
            try
            {
                Task task = AdvanceStateAsync(Token.EndElement);

                return SequenceRun(task, thisRef => thisRef.WriteEndElementAsync_NoAdvanceState(), this);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private Task WriteEndElementAsync_NoAdvanceState()
        {
            try
            {
                int top = _elemTop;
                if (top == 0)
                {
                    throw new XmlException(SR.Xml_NoStartTag, string.Empty);
                }
                Task task;
                // write end tag
                if (_rawWriter != null)
                {
                    task = _elemScopeStack[top].WriteEndElementAsync(_rawWriter);
                }
                else
                {
                    task = _writer.WriteEndElementAsync();
                }

                return SequenceRun(task, thisRef => thisRef.WriteEndElementAsync_FinishWrite(), this);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private Task WriteEndElementAsync_FinishWrite()
        {
            try
            {
                int top = _elemTop;
                // pop namespaces
                int prevNsTop = _elemScopeStack[top].prevNSTop;
                if (_useNsHashtable && prevNsTop < _nsTop)
                {
                    PopNamespaces(prevNsTop + 1, _nsTop);
                }
                _nsTop = prevNsTop;
                _elemTop = --top;

                // check "one root element" condition for ConformanceLevel.Document
                if (top == 0)
                {
                    if (_conformanceLevel == ConformanceLevel.Document)
                    {
                        _currentState = State.AfterRootEle;
                    }
                    else
                    {
                        _currentState = State.TopLevel;
                    }
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
            return Task.CompletedTask;
        }

        public override Task WriteFullEndElementAsync()
        {
            try
            {
                Task task = AdvanceStateAsync(Token.EndElement);

                return SequenceRun(task, thisRef => thisRef.WriteFullEndElementAsync_NoAdvanceState(), this);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private Task WriteFullEndElementAsync_NoAdvanceState()
        {
            try
            {
                int top = _elemTop;
                if (top == 0)
                {
                    throw new XmlException(SR.Xml_NoStartTag, string.Empty);
                }
                Task task;
                // write end tag
                if (_rawWriter != null)
                {
                    task = _elemScopeStack[top].WriteFullEndElementAsync(_rawWriter);
                }
                else
                {
                    task = _writer.WriteFullEndElementAsync();
                }

                return SequenceRun(task, thisRef => thisRef.WriteEndElementAsync_FinishWrite(), this);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        protected internal override Task WriteStartAttributeAsync(string? prefix, string localName, string? namespaceName)
        {
            try
            {
                // check local name
                if (localName == null || localName.Length == 0)
                {
                    if (prefix == "xmlns")
                    {
                        localName = "xmlns";
                        prefix = string.Empty;
                    }
                    else
                    {
                        throw new ArgumentException(SR.Xml_EmptyLocalName);
                    }
                }
                CheckNCName(localName);

                Task task = AdvanceStateAsync(Token.StartAttribute);
                if (task.IsSuccess())
                {
                    return WriteStartAttributeAsync_NoAdvanceState(prefix, localName, namespaceName);
                }
                else
                {
                    return WriteStartAttributeAsync_NoAdvanceState(task, prefix, localName, namespaceName);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private Task WriteStartAttributeAsync_NoAdvanceState(string? prefix, string localName, string? namespaceName)
        {
            try
            {
                // lookup prefix / namespace
                if (prefix == null)
                {
                    if (namespaceName != null)
                    {
                        // special case prefix=null/localname=xmlns
                        if (!(localName == "xmlns" && namespaceName == XmlReservedNs.NsXmlNs))
                            prefix = LookupPrefix(namespaceName);
                    }
                    if (prefix == null)
                    {
                        prefix = string.Empty;
                    }
                }

                if (namespaceName == null)
                {
                    if (prefix.Length > 0)
                    {
                        namespaceName = LookupNamespace(prefix);
                    }
                    if (namespaceName == null)
                    {
                        namespaceName = string.Empty;
                    }
                }

                if (prefix.Length == 0)
                {
                    if (localName[0] == 'x' && localName == "xmlns")
                    {
                        if (namespaceName.Length > 0 && namespaceName != XmlReservedNs.NsXmlNs)
                        {
                            throw new ArgumentException(SR.Xml_XmlnsPrefix);
                        }
                        _curDeclPrefix = string.Empty;
                        SetSpecialAttribute(SpecialAttribute.DefaultXmlns);
                        goto SkipPushAndWrite;
                    }
                    else if (namespaceName.Length > 0)
                    {
                        prefix = LookupPrefix(namespaceName);
                        if (prefix == null || prefix.Length == 0)
                        {
                            prefix = GeneratePrefix();
                        }
                    }
                }
                else
                {
                    if (prefix[0] == 'x')
                    {
                        if (prefix == "xmlns")
                        {
                            if (namespaceName.Length > 0 && namespaceName != XmlReservedNs.NsXmlNs)
                            {
                                throw new ArgumentException(SR.Xml_XmlnsPrefix);
                            }
                            _curDeclPrefix = localName;
                            SetSpecialAttribute(SpecialAttribute.PrefixedXmlns);
                            goto SkipPushAndWrite;
                        }
                        else if (prefix == "xml")
                        {
                            if (namespaceName.Length > 0 && namespaceName != XmlReservedNs.NsXml)
                            {
                                throw new ArgumentException(SR.Xml_XmlPrefix);
                            }
                            switch (localName)
                            {
                                case "space":
                                    SetSpecialAttribute(SpecialAttribute.XmlSpace);
                                    goto SkipPushAndWrite;
                                case "lang":
                                    SetSpecialAttribute(SpecialAttribute.XmlLang);
                                    goto SkipPushAndWrite;
                            }
                        }
                    }

                    CheckNCName(prefix);

                    if (namespaceName.Length == 0)
                    {
                        // attributes cannot have default namespace
                        prefix = string.Empty;
                    }
                    else
                    {
                        string? definedNs = LookupLocalNamespace(prefix);
                        if (definedNs != null && definedNs != namespaceName)
                        {
                            prefix = GeneratePrefix();
                        }
                    }
                }

                if (prefix.Length != 0)
                {
                    PushNamespaceImplicit(prefix, namespaceName);
                }

            SkipPushAndWrite:

                // add attribute to the list and check for duplicates
                AddAttribute(prefix, localName, namespaceName);

                if (_specAttr == SpecialAttribute.No)
                {
                    // write attribute name
                    return TryReturnTask(_writer.WriteStartAttributeAsync(prefix, localName, namespaceName));
                }
                return Task.CompletedTask;
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private async Task WriteStartAttributeAsync_NoAdvanceState(Task task, string? prefix, string localName, string? namespaceName)
        {
            try
            {
                await task.ConfigureAwait(false);
                await WriteStartAttributeAsync_NoAdvanceState(prefix, localName, namespaceName).ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }


        protected internal override Task WriteEndAttributeAsync()
        {
            try
            {
                Task task = AdvanceStateAsync(Token.EndAttribute);
                return SequenceRun(task, thisRef => thisRef.WriteEndAttributeAsync_NoAdvance(), this);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private Task WriteEndAttributeAsync_NoAdvance()
        {
            try
            {
                if (_specAttr != SpecialAttribute.No)
                {
                    return WriteEndAttributeAsync_SepcialAtt();
                }
                else
                {
                    return TryReturnTask(_writer.WriteEndAttributeAsync());
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private async Task WriteEndAttributeAsync_SepcialAtt()
        {
            Debug.Assert(_attrValueCache != null);
            try
            {
                string value;

                switch (_specAttr)
                {
                    case SpecialAttribute.DefaultXmlns:
                        value = _attrValueCache.StringValue;
                        if (PushNamespaceExplicit(string.Empty, value))
                        { // returns true if the namespace declaration should be written out
                            if (_rawWriter != null)
                            {
                                if (_rawWriter.SupportsNamespaceDeclarationInChunks)
                                {
                                    await _rawWriter.WriteStartNamespaceDeclarationAsync(string.Empty).ConfigureAwait(false);
                                    await _attrValueCache.ReplayAsync(_rawWriter).ConfigureAwait(false);
                                    await _rawWriter.WriteEndNamespaceDeclarationAsync().ConfigureAwait(false);
                                }
                                else
                                {
                                    await _rawWriter.WriteNamespaceDeclarationAsync(string.Empty, value).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                await _writer.WriteStartAttributeAsync(string.Empty, "xmlns", XmlReservedNs.NsXmlNs).ConfigureAwait(false);
                                await _attrValueCache.ReplayAsync(_writer).ConfigureAwait(false);
                                await _writer.WriteEndAttributeAsync().ConfigureAwait(false);
                            }
                        }

                        _curDeclPrefix = null;
                        break;
                    case SpecialAttribute.PrefixedXmlns:
                        value = _attrValueCache.StringValue;
                        if (value.Length == 0)
                        {
                            throw new ArgumentException(SR.Xml_PrefixForEmptyNs);
                        }
                        if (value == XmlReservedNs.NsXmlNs || (value == XmlReservedNs.NsXml && _curDeclPrefix != "xml"))
                        {
                            throw new ArgumentException(SR.Xml_CanNotBindToReservedNamespace);
                        }

                        Debug.Assert(_curDeclPrefix != null);
                        if (PushNamespaceExplicit(_curDeclPrefix, value))
                        { // returns true if the namespace declaration should be written out
                            if (_rawWriter != null)
                            {
                                if (_rawWriter.SupportsNamespaceDeclarationInChunks)
                                {
                                    await _rawWriter.WriteStartNamespaceDeclarationAsync(_curDeclPrefix).ConfigureAwait(false);
                                    await _attrValueCache.ReplayAsync(_rawWriter).ConfigureAwait(false);
                                    await _rawWriter.WriteEndNamespaceDeclarationAsync().ConfigureAwait(false);
                                }
                                else
                                {
                                    await _rawWriter.WriteNamespaceDeclarationAsync(_curDeclPrefix, value).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                await _writer.WriteStartAttributeAsync("xmlns", _curDeclPrefix, XmlReservedNs.NsXmlNs).ConfigureAwait(false);
                                await _attrValueCache.ReplayAsync(_writer).ConfigureAwait(false);
                                await _writer.WriteEndAttributeAsync().ConfigureAwait(false);
                            }
                        }
                        _curDeclPrefix = null;
                        break;
                    case SpecialAttribute.XmlSpace:
                        _attrValueCache.Trim();
                        value = _attrValueCache.StringValue;

                        if (value == "default")
                        {
                            _elemScopeStack[_elemTop].xmlSpace = XmlSpace.Default;
                        }
                        else if (value == "preserve")
                        {
                            _elemScopeStack[_elemTop].xmlSpace = XmlSpace.Preserve;
                        }
                        else
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_InvalidXmlSpace, value));
                        }
                        await _writer.WriteStartAttributeAsync("xml", "space", XmlReservedNs.NsXml).ConfigureAwait(false);
                        await _attrValueCache.ReplayAsync(_writer).ConfigureAwait(false);
                        await _writer.WriteEndAttributeAsync().ConfigureAwait(false);
                        break;
                    case SpecialAttribute.XmlLang:
                        value = _attrValueCache.StringValue;
                        _elemScopeStack[_elemTop].xmlLang = value;
                        await _writer.WriteStartAttributeAsync("xml", "lang", XmlReservedNs.NsXml).ConfigureAwait(false);
                        await _attrValueCache.ReplayAsync(_writer).ConfigureAwait(false);
                        await _writer.WriteEndAttributeAsync().ConfigureAwait(false);
                        break;
                }
                _specAttr = SpecialAttribute.No;
                _attrValueCache.Clear();
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteCDataAsync(string? text)
        {
            try
            {
                if (text == null)
                {
                    text = string.Empty;
                }

                await AdvanceStateAsync(Token.CData).ConfigureAwait(false);
                await _writer.WriteCDataAsync(text).ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteCommentAsync(string? text)
        {
            try
            {
                if (text == null)
                {
                    text = string.Empty;
                }

                await AdvanceStateAsync(Token.Comment).ConfigureAwait(false);
                await _writer.WriteCommentAsync(text).ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteProcessingInstructionAsync(string name, string? text)
        {
            try
            {
                // check name
                if (name == null || name.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyName);
                }
                CheckNCName(name);

                // check text
                if (text == null)
                {
                    text = string.Empty;
                }

                // xml declaration is a special case (not a processing instruction, but we allow WriteProcessingInstruction as a convenience)
                if (name.Length == 3 && string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase))
                {
                    if (_currentState != State.Start)
                    {
                        throw new ArgumentException(_conformanceLevel == ConformanceLevel.Document ? SR.Xml_DupXmlDecl : SR.Xml_CannotWriteXmlDecl);
                    }

                    _xmlDeclFollows = true;
                    await AdvanceStateAsync(Token.PI).ConfigureAwait(false);

                    if (_rawWriter != null)
                    {
                        // Translate PI into an xml declaration
                        await _rawWriter.WriteXmlDeclarationAsync(text).ConfigureAwait(false);
                    }
                    else
                    {
                        await _writer.WriteProcessingInstructionAsync(name, text).ConfigureAwait(false);
                    }
                }
                else
                {
                    await AdvanceStateAsync(Token.PI).ConfigureAwait(false);
                    await _writer.WriteProcessingInstructionAsync(name, text).ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteEntityRefAsync(string name)
        {
            try
            {
                // check name
                if (name == null || name.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyName);
                }

                CheckNCName(name);

                await AdvanceStateAsync(Token.Text).ConfigureAwait(false);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteEntityRef(name);
                }
                else
                {
                    await _writer.WriteEntityRefAsync(name).ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteCharEntityAsync(char ch)
        {
            try
            {
                if (char.IsSurrogate(ch))
                {
                    throw new ArgumentException(SR.Xml_InvalidSurrogateMissingLowChar);
                }

                await AdvanceStateAsync(Token.Text).ConfigureAwait(false);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteCharEntity(ch);
                }
                else
                {
                    await _writer.WriteCharEntityAsync(ch).ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            try
            {
                if (!char.IsSurrogatePair(highChar, lowChar))
                {
                    throw XmlConvert.CreateInvalidSurrogatePairException(lowChar, highChar);
                }

                await AdvanceStateAsync(Token.Text).ConfigureAwait(false);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteSurrogateCharEntity(lowChar, highChar);
                }
                else
                {
                    await _writer.WriteSurrogateCharEntityAsync(lowChar, highChar).ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteWhitespaceAsync(string? ws)
        {
            try
            {
                if (ws == null)
                {
                    ws = string.Empty;
                }

                if (!XmlCharType.IsOnlyWhitespace(ws))
                {
                    throw new ArgumentException(SR.Xml_NonWhitespace);
                }

                await AdvanceStateAsync(Token.Whitespace).ConfigureAwait(false);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteWhitespace(ws);
                }
                else
                {
                    await _writer.WriteWhitespaceAsync(ws).ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override Task WriteStringAsync(string? text)
        {
            try
            {
                if (text == null)
                {
                    return Task.CompletedTask;
                }

                Task task = AdvanceStateAsync(Token.Text);

                if (task.IsSuccess())
                {
                    return WriteStringAsync_NoAdvanceState(text);
                }
                else
                {
                    return WriteStringAsync_NoAdvanceState(task, text);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private Task WriteStringAsync_NoAdvanceState(string text)
        {
            try
            {
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteString(text);
                    return Task.CompletedTask;
                }
                else
                {
                    return TryReturnTask(_writer.WriteStringAsync(text));
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private async Task WriteStringAsync_NoAdvanceState(Task task, string text)
        {
            try
            {
                await task.ConfigureAwait(false);
                await WriteStringAsync_NoAdvanceState(text).ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            try
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }
                if (count > buffer.Length - index)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                await AdvanceStateAsync(Token.Text).ConfigureAwait(false);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteChars(buffer, index, count);
                }
                else
                {
                    await _writer.WriteCharsAsync(buffer, index, count).ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteRawAsync(char[] buffer, int index, int count)
        {
            try
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }
                if (count > buffer.Length - index)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                await AdvanceStateAsync(Token.RawData).ConfigureAwait(false);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteRaw(buffer, index, count);
                }
                else
                {
                    await _writer.WriteRawAsync(buffer, index, count).ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteRawAsync(string data)
        {
            try
            {
                if (data == null)
                {
                    return;
                }

                await AdvanceStateAsync(Token.RawData).ConfigureAwait(false);
                if (SaveAttrValue)
                {
                    _attrValueCache!.WriteRaw(data);
                }
                else
                {
                    await _writer.WriteRawAsync(data).ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            try
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }
                if (count > buffer.Length - index)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                Task task = AdvanceStateAsync(Token.Base64);

                if (task.IsSuccess())
                {
                    return TryReturnTask(_writer.WriteBase64Async(buffer, index, count));
                }
                else
                {
                    return WriteBase64Async_NoAdvanceState(task, buffer, index, count);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private async Task WriteBase64Async_NoAdvanceState(Task task, byte[] buffer, int index, int count)
        {
            try
            {
                await task.ConfigureAwait(false);
                await _writer.WriteBase64Async(buffer, index, count).ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task FlushAsync()
        {
            try
            {
                await _writer.FlushAsync().ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteQualifiedNameAsync(string localName, string? ns)
        {
            try
            {
                if (localName == null || localName.Length == 0)
                {
                    throw new ArgumentException(SR.Xml_EmptyLocalName);
                }
                CheckNCName(localName);

                await AdvanceStateAsync(Token.Text).ConfigureAwait(false);
                string? prefix = string.Empty;
                if (ns != null && ns.Length != 0)
                {
                    prefix = LookupPrefix(ns);
                    if (prefix == null)
                    {
                        if (_currentState != State.Attribute)
                        {
                            throw new ArgumentException(SR.Format(SR.Xml_UndefNamespace, ns));
                        }
                        prefix = GeneratePrefix();
                        PushNamespaceImplicit(prefix, ns);
                    }
                }
                // if this is a special attribute, then just convert this to text
                // otherwise delegate to raw-writer
                if (SaveAttrValue || _rawWriter == null)
                {
                    if (prefix.Length != 0)
                    {
                        await WriteStringAsync(prefix).ConfigureAwait(false);
                        await WriteStringAsync(":").ConfigureAwait(false);
                    }
                    await WriteStringAsync(localName).ConfigureAwait(false);
                }
                else
                {
                    await _rawWriter.WriteQualifiedNameAsync(prefix, localName, ns).ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        public override async Task WriteBinHexAsync(byte[] buffer, int index, int count)
        {
            if (IsClosedOrErrorState)
            {
                throw new InvalidOperationException(SR.Xml_ClosedOrError);
            }
            try
            {
                await AdvanceStateAsync(Token.Text).ConfigureAwait(false);
                await base.WriteBinHexAsync(buffer, index, count).ConfigureAwait(false);
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        private async Task WriteStartDocumentImplAsync(XmlStandalone standalone)
        {
            try
            {
                await AdvanceStateAsync(Token.StartDocument).ConfigureAwait(false);

                if (_conformanceLevel == ConformanceLevel.Auto)
                {
                    _conformanceLevel = ConformanceLevel.Document;
                    _stateTable = s_stateTableDocument;
                }
                else if (_conformanceLevel == ConformanceLevel.Fragment)
                {
                    throw new InvalidOperationException(SR.Xml_CannotStartDocumentOnFragment);
                }

                if (_rawWriter != null)
                {
                    if (!_xmlDeclFollows)
                    {
                        await _rawWriter.WriteXmlDeclarationAsync(standalone).ConfigureAwait(false);
                    }
                }
                else
                {
                    // We do not pass the standalone value here
                    await _writer.WriteStartDocumentAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                _currentState = State.Error;
                throw;
            }
        }

        //call taskFun and change state in sequence
        private Task AdvanceStateAsync_ReturnWhenFinish(Task task, State newState)
        {
            if (task.IsSuccess())
            {
                _currentState = newState;
                return Task.CompletedTask;
            }
            else
            {
                return _AdvanceStateAsync_ReturnWhenFinish(task, newState);
            }
        }

        private async Task _AdvanceStateAsync_ReturnWhenFinish(Task task, State newState)
        {
            await task.ConfigureAwait(false);
            _currentState = newState;
        }

        private Task AdvanceStateAsync_ContinueWhenFinish(Task task, State newState, Token token)
        {
            if (task.IsSuccess())
            {
                _currentState = newState;
                return AdvanceStateAsync(token);
            }
            else
            {
                return _AdvanceStateAsync_ContinueWhenFinish(task, newState, token);
            }
        }

        private async Task _AdvanceStateAsync_ContinueWhenFinish(Task task, State newState, Token token)
        {
            await task.ConfigureAwait(false);
            _currentState = newState;
            await AdvanceStateAsync(token).ConfigureAwait(false);
        }

        // Advance the state machine
        private Task AdvanceStateAsync(Token token)
        {
            if ((int)_currentState >= (int)State.Closed)
            {
                if (_currentState == State.Closed || _currentState == State.Error)
                {
                    throw new InvalidOperationException(SR.Xml_ClosedOrError);
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.Xml_WrongToken, tokenName[(int)token], GetStateName(_currentState)));
                }
            }
        Advance:
            State newState = _stateTable[((int)token << 4) + (int)_currentState];
            //                         [ (int)token * 16 + (int)currentState ];

            Task task;
            if ((int)newState >= (int)State.Error)
            {
                switch (newState)
                {
                    case State.Error:
                        ThrowInvalidStateTransition(token, _currentState);
                        break;

                    case State.StartContent:
                        return AdvanceStateAsync_ReturnWhenFinish(StartElementContentAsync(), State.Content);

                    case State.StartContentEle:
                        return AdvanceStateAsync_ReturnWhenFinish(StartElementContentAsync(), State.Element);

                    case State.StartContentB64:
                        return AdvanceStateAsync_ReturnWhenFinish(StartElementContentAsync(), State.B64Content);

                    case State.StartDoc:
                        return AdvanceStateAsync_ReturnWhenFinish(WriteStartDocumentAsync(), State.Document);

                    case State.StartDocEle:
                        return AdvanceStateAsync_ReturnWhenFinish(WriteStartDocumentAsync(), State.Element);

                    case State.EndAttrSEle:
                        task = SequenceRun(WriteEndAttributeAsync(), thisRef => thisRef.StartElementContentAsync(), this);
                        return AdvanceStateAsync_ReturnWhenFinish(task, State.Element);

                    case State.EndAttrEEle:
                        task = SequenceRun(WriteEndAttributeAsync(), thisRef => thisRef.StartElementContentAsync(), this);
                        return AdvanceStateAsync_ReturnWhenFinish(task, State.Content);
                    case State.EndAttrSCont:
                        task = SequenceRun(WriteEndAttributeAsync(), thisRef => thisRef.StartElementContentAsync(), this);
                        return AdvanceStateAsync_ReturnWhenFinish(task, State.Content);

                    case State.EndAttrSAttr:
                        return AdvanceStateAsync_ReturnWhenFinish(WriteEndAttributeAsync(), State.Attribute);

                    case State.PostB64Cont:
                        if (_rawWriter != null)
                        {
                            return AdvanceStateAsync_ContinueWhenFinish(_rawWriter.WriteEndBase64Async(), State.Content, token);
                        }
                        _currentState = State.Content;
                        goto Advance;

                    case State.PostB64Attr:
                        if (_rawWriter != null)
                        {
                            return AdvanceStateAsync_ContinueWhenFinish(_rawWriter.WriteEndBase64Async(), State.Attribute, token);
                        }
                        _currentState = State.Attribute;
                        goto Advance;

                    case State.PostB64RootAttr:
                        if (_rawWriter != null)
                        {
                            return AdvanceStateAsync_ContinueWhenFinish(_rawWriter.WriteEndBase64Async(), State.RootLevelAttr, token);
                        }
                        _currentState = State.RootLevelAttr;
                        goto Advance;

                    case State.StartFragEle:
                        StartFragment();
                        newState = State.Element;
                        break;

                    case State.StartFragCont:
                        StartFragment();
                        newState = State.Content;
                        break;

                    case State.StartFragB64:
                        StartFragment();
                        newState = State.B64Content;
                        break;

                    case State.StartRootLevelAttr:
                        return AdvanceStateAsync_ReturnWhenFinish(WriteEndAttributeAsync(), State.RootLevelAttr);


                    default:
                        Debug.Fail("We should not get to this point.");
                        break;
                }
            }

            _currentState = newState;
            return Task.CompletedTask;
        }

        // write namespace declarations
        private async Task StartElementContentAsync_WithNS()
        {
            int start = _elemScopeStack[_elemTop].prevNSTop;
            for (int i = _nsTop; i > start; i--)
            {
                if (_nsStack[i].kind == NamespaceKind.NeedToWrite)
                {
                    await _nsStack[i].WriteDeclAsync(_writer, _rawWriter).ConfigureAwait(false);
                }
            }

            if (_rawWriter != null)
            {
                _rawWriter.StartElementContent();
            }
        }

        private Task StartElementContentAsync()
        {
            if (_nsTop > _elemScopeStack[_elemTop].prevNSTop)
            {
                return StartElementContentAsync_WithNS();
            }

            if (_rawWriter != null)
            {
                _rawWriter.StartElementContent();
            }

            return Task.CompletedTask;
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            if (_currentState != State.Closed)
            {
                try
                {
                    if (_writeEndDocumentOnClose)
                    {
                        while (_currentState != State.Error && _elemTop > 0)
                        {
                            await WriteEndElementAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (_currentState != State.Error && _elemTop > 0)
                        {
                            //finish the start element tag '>'
                            try
                            {
                                await AdvanceStateAsync(Token.EndElement).ConfigureAwait(false);
                            }
                            catch
                            {
                                _currentState = State.Error;
                                throw;
                            }
                        }
                    }

                    if (InBase64 && _rawWriter != null)
                    {
                        await _rawWriter.WriteEndBase64Async().ConfigureAwait(false);
                    }

                    await _writer.FlushAsync().ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        if (_rawWriter != null)
                        {
                            await _rawWriter.DisposeAsyncCore(WriteState).ConfigureAwait(false);
                        }
                        else
                        {
                            await _writer.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        _currentState = State.Closed;
                    }
                }
            }
        }
    }
}
