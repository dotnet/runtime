// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Security;
using System.Threading;
using System.Xml.Schema;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Xml
{
    internal sealed partial class XmlTextReaderImpl : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
    {
        private void CheckAsyncCall()
        {
            if (!_useAsync)
            {
                throw new InvalidOperationException(SR.Xml_ReaderAsyncNotSetException);
            }
        }

        public override Task<string> GetValueAsync()
        {
            CheckAsyncCall();
            if (_parsingFunction >= ParsingFunction.PartialTextValue)
            {
                return _GetValueAsync();
            }
            return Task.FromResult(_curNode.StringValue);
        }

        private async Task<string> _GetValueAsync()
        {
            if (_parsingFunction >= ParsingFunction.PartialTextValue)
            {
                if (_parsingFunction == ParsingFunction.PartialTextValue)
                {
                    await FinishPartialValueAsync().ConfigureAwait(false);
                    _parsingFunction = _nextParsingFunction;
                }
                else
                {
                    await FinishOtherValueIteratorAsync().ConfigureAwait(false);
                }
            }
            return _curNode.StringValue;
        }

        private Task FinishInitAsync()
        {
            switch (_laterInitParam!.initType)
            {
                case InitInputType.UriString:
                    return FinishInitUriStringAsync();
                case InitInputType.Stream:
                    return FinishInitStreamAsync();
                case InitInputType.TextReader:
                    return FinishInitTextReaderAsync();
                default:
                    //should never hit here
                    Debug.Fail("Invalid InitInputType");
                    return Task.CompletedTask;
            }
        }


        private async Task FinishInitUriStringAsync()
        {
            Stream stream = (Stream)(await _laterInitParam!.inputUriResolver!.GetEntityAsync(_laterInitParam.inputbaseUri!, string.Empty, typeof(Stream)).ConfigureAwait(false));

            if (stream == null)
            {
                throw new XmlException(SR.Xml_CannotResolveUrl, _laterInitParam.inputUriStr);
            }

            Encoding? enc = null;
            // get Encoding from XmlParserContext
            if (_laterInitParam.inputContext != null)
            {
                enc = _laterInitParam.inputContext.Encoding;
            }

            try
            {
                // init ParsingState
                Debug.Assert(_reportedBaseUri != null);
                await InitStreamInputAsync(_laterInitParam.inputbaseUri, _reportedBaseUri, stream, null, 0, enc).ConfigureAwait(false);

                _reportedEncoding = _ps.encoding;

                // parse DTD
                if (_laterInitParam.inputContext != null && _laterInitParam.inputContext.HasDtdInfo)
                {
                    await ProcessDtdFromParserContextAsync(_laterInitParam.inputContext).ConfigureAwait(false);
                }
            }
            catch
            {
                stream.Dispose();
                throw;
            }
            _laterInitParam = null;
        }


        private async Task FinishInitStreamAsync()
        {
            Encoding? enc = null;

            // get Encoding from XmlParserContext
            if (_laterInitParam!.inputContext != null)
            {
                enc = _laterInitParam.inputContext.Encoding;
            }

            // init ParsingState
            Debug.Assert(_reportedBaseUri != null);
            await InitStreamInputAsync(_laterInitParam.inputbaseUri, _reportedBaseUri, _laterInitParam!.inputStream!, _laterInitParam.inputBytes, _laterInitParam.inputByteCount, enc).ConfigureAwait(false);

            _reportedEncoding = _ps.encoding;

            // parse DTD
            if (_laterInitParam.inputContext != null && _laterInitParam.inputContext.HasDtdInfo)
            {
                await ProcessDtdFromParserContextAsync(_laterInitParam.inputContext).ConfigureAwait(false);
            }
            _laterInitParam = null;
        }

        private async Task FinishInitTextReaderAsync()
        {
            // init ParsingState
            await InitTextReaderInputAsync(_reportedBaseUri!, _laterInitParam!.inputTextReader!).ConfigureAwait(false);

            _reportedEncoding = _ps.encoding;

            // parse DTD
            if (_laterInitParam.inputContext != null && _laterInitParam.inputContext.HasDtdInfo)
            {
                await ProcessDtdFromParserContextAsync(_laterInitParam.inputContext).ConfigureAwait(false);
            }

            _laterInitParam = null;
        }

        // Reads next node from the input data
        public override Task<bool> ReadAsync()
        {
            CheckAsyncCall();

            if (_laterInitParam != null)
            {
                return FinishInitAsync().CallBoolTaskFuncWhenFinishAsync(thisRef => thisRef.ReadAsync(), this);
            }

            while (true)
            {
                switch (_parsingFunction)
                {
                    case ParsingFunction.ElementContent:
                        return ParseElementContentAsync();
                    case ParsingFunction.DocumentContent:
                        return ParseDocumentContentAsync();
                    // Needed only for XmlTextReader
                    //XmlTextReader can't execute Async method.
                    case ParsingFunction.OpenUrl:
                        Debug.Fail($"Unexpected parsing function {_parsingFunction}");
                        break;
                    case ParsingFunction.SwitchToInteractive:
                        Debug.Assert(!_ps.appendMode);
                        _readState = ReadState.Interactive;
                        _parsingFunction = _nextParsingFunction;
                        continue;
                    case ParsingFunction.SwitchToInteractiveXmlDecl:
                        return ReadAsync_SwitchToInteractiveXmlDecl();
                    case ParsingFunction.ResetAttributesRootLevel:
                        ResetAttributes();
                        _curNode = _nodes[_index];
                        _parsingFunction = (_index == 0) ? ParsingFunction.DocumentContent : ParsingFunction.ElementContent;
                        continue;
                    case ParsingFunction.MoveToElementContent:
                        ResetAttributes();
                        _index++;
                        _curNode = AddNode(_index, _index);
                        _parsingFunction = ParsingFunction.ElementContent;
                        continue;
                    case ParsingFunction.PopElementContext:
                        PopElementContext();
                        _parsingFunction = _nextParsingFunction;
                        Debug.Assert(_parsingFunction == ParsingFunction.ElementContent ||
                                      _parsingFunction == ParsingFunction.DocumentContent);
                        continue;
                    case ParsingFunction.PopEmptyElementContext:
                        _curNode = _nodes[_index];
                        Debug.Assert(_curNode.type == XmlNodeType.Element);
                        _curNode.IsEmptyElement = false;
                        ResetAttributes();
                        PopElementContext();
                        _parsingFunction = _nextParsingFunction;
                        continue;
                    // Needed only for XmlTextReader (reporting of entities)
                    case ParsingFunction.EntityReference:
                        _parsingFunction = _nextParsingFunction;
                        return ParseEntityReferenceAsync().ReturnTrueTaskWhenFinishAsync();
                    case ParsingFunction.ReportEndEntity:
                        SetupEndEntityNodeInContent();
                        _parsingFunction = _nextParsingFunction;
                        return AsyncHelper.DoneTaskTrue;
                    case ParsingFunction.AfterResolveEntityInContent:
                        _curNode = AddNode(_index, _index);
                        _reportedEncoding = _ps.encoding;
                        _reportedBaseUri = _ps.baseUriStr;
                        _parsingFunction = _nextParsingFunction;
                        continue;
                    case ParsingFunction.AfterResolveEmptyEntityInContent:
                        _curNode = AddNode(_index, _index);
                        _curNode.SetValueNode(XmlNodeType.Text, string.Empty);
                        _curNode.SetLineInfo(_ps.lineNo, _ps.LinePos);
                        _reportedEncoding = _ps.encoding;
                        _reportedBaseUri = _ps.baseUriStr;
                        _parsingFunction = _nextParsingFunction;
                        return AsyncHelper.DoneTaskTrue;
                    case ParsingFunction.InReadAttributeValue:
                        FinishAttributeValueIterator();
                        _curNode = _nodes[_index];
                        continue;
                    // Needed only for XmlTextReader (ReadChars, ReadBase64, ReadBinHex)
                    case ParsingFunction.InIncrementalRead:
                        FinishIncrementalRead();
                        return AsyncHelper.DoneTaskTrue;
                    case ParsingFunction.FragmentAttribute:
                        return Task.FromResult(ParseFragmentAttribute());
                    case ParsingFunction.XmlDeclarationFragment:
                        ParseXmlDeclarationFragment();
                        _parsingFunction = ParsingFunction.GoToEof;
                        return AsyncHelper.DoneTaskTrue;
                    case ParsingFunction.GoToEof:
                        OnEof();
                        return AsyncHelper.DoneTaskFalse;
                    case ParsingFunction.Error:
                    case ParsingFunction.Eof:
                    case ParsingFunction.ReaderClosed:
                        return AsyncHelper.DoneTaskFalse;
                    case ParsingFunction.NoData:
                        ThrowWithoutLineInfo(SR.Xml_MissingRoot);
                        return AsyncHelper.DoneTaskFalse;
                    case ParsingFunction.PartialTextValue:
                        return SkipPartialTextValueAsync().CallBoolTaskFuncWhenFinishAsync(thisRef => thisRef.ReadAsync(), this);
                    case ParsingFunction.InReadValueChunk:
                        return FinishReadValueChunkAsync().CallBoolTaskFuncWhenFinishAsync(thisRef => thisRef.ReadAsync(), this);
                    case ParsingFunction.InReadContentAsBinary:
                        return FinishReadContentAsBinaryAsync().CallBoolTaskFuncWhenFinishAsync(thisRef => thisRef.ReadAsync(), this);
                    case ParsingFunction.InReadElementContentAsBinary:
                        return FinishReadElementContentAsBinaryAsync().CallBoolTaskFuncWhenFinishAsync(thisRef => thisRef.ReadAsync(), this);
                    default:
                        Debug.Fail($"Unexpected parsing function {_parsingFunction}");
                        break;
                }
            }
        }

        private Task<bool> ReadAsync_SwitchToInteractiveXmlDecl()
        {
            _readState = ReadState.Interactive;
            _parsingFunction = _nextParsingFunction;
            Task<bool> task = ParseXmlDeclarationAsync(false);
            if (task.IsSuccess())
            {
                return ReadAsync_SwitchToInteractiveXmlDecl_Helper(task.Result);
            }
            else
            {
                return _ReadAsync_SwitchToInteractiveXmlDecl(task);
            }
        }

        private async Task<bool> _ReadAsync_SwitchToInteractiveXmlDecl(Task<bool> task)
        {
            bool result = await task.ConfigureAwait(false);
            return await ReadAsync_SwitchToInteractiveXmlDecl_Helper(result).ConfigureAwait(false);
        }

        private Task<bool> ReadAsync_SwitchToInteractiveXmlDecl_Helper(bool finish)
        {
            if (finish)
            {
                _reportedEncoding = _ps.encoding;
                return AsyncHelper.DoneTaskTrue;
            }
            else
            {
                _reportedEncoding = _ps.encoding;
                return ReadAsync();
            }
        }


        // Skips the current node. If on element, skips to the end tag of the element.
        public override async Task SkipAsync()
        {
            CheckAsyncCall();
            if (_readState != ReadState.Interactive)
                return;

            if (InAttributeValueIterator)
            {
                FinishAttributeValueIterator();
                _curNode = _nodes[_index];
            }
            else
            {
                switch (_parsingFunction)
                {
                    case ParsingFunction.InReadAttributeValue:
                        Debug.Fail($"Unexpected parsing function {_parsingFunction}");
                        break;
                    // Needed only for XmlTextReader (ReadChars, ReadBase64, ReadBinHex)
                    case ParsingFunction.InIncrementalRead:
                        FinishIncrementalRead();
                        break;
                    case ParsingFunction.PartialTextValue:
                        await SkipPartialTextValueAsync().ConfigureAwait(false);
                        break;
                    case ParsingFunction.InReadValueChunk:
                        await FinishReadValueChunkAsync().ConfigureAwait(false);
                        break;
                    case ParsingFunction.InReadContentAsBinary:
                        await FinishReadContentAsBinaryAsync().ConfigureAwait(false);
                        break;
                    case ParsingFunction.InReadElementContentAsBinary:
                        await FinishReadElementContentAsBinaryAsync().ConfigureAwait(false);
                        break;
                }
            }

            switch (_curNode.type)
            {
                // skip subtree
                case XmlNodeType.Element:
                    if (_curNode.IsEmptyElement)
                    {
                        break;
                    }
                    int initialDepth = _index;
                    _parsingMode = ParsingMode.SkipContent;
                    // skip content
                    while (await _outerReader.ReadAsync().ConfigureAwait(false) && _index > initialDepth) ;
                    Debug.Assert(_curNode.type == XmlNodeType.EndElement);
                    Debug.Assert(_parsingFunction != ParsingFunction.Eof);
                    _parsingMode = ParsingMode.Full;
                    break;
                case XmlNodeType.Attribute:
                    _outerReader.MoveToElement();
                    goto case XmlNodeType.Element;
            }
            // move to following sibling node
            await _outerReader.ReadAsync().ConfigureAwait(false);
            return;
        }

        private async Task<int> ReadContentAsBase64_AsyncHelper(Task<bool> task, byte[] buffer, int index, int count)
        {
            bool result = await task.ConfigureAwait(false);
            if (!result)
            {
                return 0;
            }
            else
            {
                // setup base64 decoder
                InitBase64Decoder();

                // read binary data
                return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
            }
        }

        // Reads and concatenates content nodes, base64-decodes the results and copies the decoded bytes into the provided buffer
        public override Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
        {
            CheckAsyncCall();
            ArgumentNullException.ThrowIfNull(buffer);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // if not the first call to ReadContentAsBase64
            if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
            {
                // and if we have a correct decoder
                if (_incReadDecoder == _base64Decoder)
                {
                    // read more binary data
                    return ReadContentAsBinaryAsync(buffer, index, count);
                }
            }
            // first call of ReadContentAsBase64 -> initialize (move to first text child (for elements) and initialize incremental read state)
            else
            {
                if (_readState != ReadState.Interactive)
                {
                    return AsyncHelper.DoneTaskZero;
                }
                if (_parsingFunction == ParsingFunction.InReadElementContentAsBinary)
                {
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                }
                if (!XmlReader.CanReadContentAs(_curNode.type))
                {
                    throw CreateReadContentAsException(nameof(ReadContentAsBase64));
                }

                Task<bool> task = InitReadContentAsBinaryAsync();
                if (task.IsSuccess())
                {
                    if (!task.Result)
                    {
                        return AsyncHelper.DoneTaskZero;
                    }
                }
                else
                {
                    return ReadContentAsBase64_AsyncHelper(task, buffer, index, count);
                }
            }

            // setup base64 decoder
            InitBase64Decoder();

            // read binary data
            return ReadContentAsBinaryAsync(buffer, index, count);
        }

        // Reads and concatenates content nodes, binhex-decodes the results and copies the decoded bytes into the provided buffer
        public override async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            CheckAsyncCall();
            ArgumentNullException.ThrowIfNull(buffer);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // if not the first call to ReadContentAsBinHex
            if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
            {
                // and if we have a correct decoder
                if (_incReadDecoder == _binHexDecoder)
                {
                    // read more binary data
                    return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
                }
            }
            // first call of ReadContentAsBinHex -> initialize (move to first text child (for elements) and initialize incremental read state)
            else
            {
                if (_readState != ReadState.Interactive)
                {
                    return 0;
                }
                if (_parsingFunction == ParsingFunction.InReadElementContentAsBinary)
                {
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                }
                if (!XmlReader.CanReadContentAs(_curNode.type))
                {
                    throw CreateReadContentAsException(nameof(ReadContentAsBinHex));
                }

                if (!await InitReadContentAsBinaryAsync().ConfigureAwait(false))
                {
                    return 0;
                }
            }

            // setup binhex decoder (when in first ReadContentAsBinHex call or when mixed with ReadContentAsBase64)
            InitBinHexDecoder();

            // read binary data
            return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
        }

        private async Task<int> ReadElementContentAsBase64Async_Helper(Task<bool> task, byte[] buffer, int index, int count)
        {
            bool result = await task.ConfigureAwait(false);
            if (!result)
            {
                return 0;
            }
            else
            {
                // setup base64 decoder
                InitBase64Decoder();

                // read binary data
                return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
            }
        }

        // Reads and concatenates content of an element, base64-decodes the results and copies the decoded bytes into the provided buffer
        public override Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
        {
            CheckAsyncCall();
            ArgumentNullException.ThrowIfNull(buffer);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // if not the first call to ReadContentAsBase64
            if (_parsingFunction == ParsingFunction.InReadElementContentAsBinary)
            {
                // and if we have a correct decoder
                if (_incReadDecoder == _base64Decoder)
                {
                    // read more binary data
                    return ReadElementContentAsBinaryAsync(buffer, index, count);
                }
            }
            // first call of ReadElementContentAsBase64 -> initialize
            else
            {
                if (_readState != ReadState.Interactive)
                {
                    return AsyncHelper.DoneTaskZero;
                }
                if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
                {
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                }
                if (_curNode.type != XmlNodeType.Element)
                {
                    throw CreateReadElementContentAsException(nameof(ReadElementContentAsBinHex));
                }

                Task<bool> task = InitReadElementContentAsBinaryAsync();
                if (task.IsSuccess())
                {
                    if (!task.Result)
                    {
                        return AsyncHelper.DoneTaskZero;
                    }
                }
                else
                {
                    return ReadElementContentAsBase64Async_Helper(task, buffer, index, count);
                }
            }

            // setup base64 decoder
            InitBase64Decoder();

            // read binary data
            return ReadElementContentAsBinaryAsync(buffer, index, count);
        }

        // Reads and concatenates content of an element, binhex-decodes the results and copies the decoded bytes into the provided buffer
        public override async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            CheckAsyncCall();
            ArgumentNullException.ThrowIfNull(buffer);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // if not the first call to ReadContentAsBinHex
            if (_parsingFunction == ParsingFunction.InReadElementContentAsBinary)
            {
                // and if we have a correct decoder
                if (_incReadDecoder == _binHexDecoder)
                {
                    // read more binary data
                    return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
                }
            }
            // first call of ReadContentAsBinHex -> initialize
            else
            {
                if (_readState != ReadState.Interactive)
                {
                    return 0;
                }
                if (_parsingFunction == ParsingFunction.InReadContentAsBinary)
                {
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                }
                if (_curNode.type != XmlNodeType.Element)
                {
                    throw CreateReadElementContentAsException(nameof(ReadElementContentAsBinHex));
                }
                if (!await InitReadElementContentAsBinaryAsync().ConfigureAwait(false))
                {
                    return 0;
                }
            }

            // setup binhex decoder (when in first ReadContentAsBinHex call or when mixed with ReadContentAsBase64)
            InitBinHexDecoder();

            // read binary data
            return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
        }

        // Iterates over Value property and copies it into the provided buffer
        public override async Task<int> ReadValueChunkAsync(char[] buffer, int index, int count)
        {
            CheckAsyncCall();
            // throw on elements
            if (!XmlReader.HasValueInternal(_curNode.type))
            {
                throw new InvalidOperationException(SR.Format(SR.Xml_InvalidReadValueChunk, _curNode.type));
            }
            ArgumentNullException.ThrowIfNull(buffer);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // first call of ReadValueChunk -> initialize incremental read state
            if (_parsingFunction != ParsingFunction.InReadValueChunk)
            {
                if (_readState != ReadState.Interactive)
                {
                    return 0;
                }
                if (_parsingFunction == ParsingFunction.PartialTextValue)
                {
                    _incReadState = IncrementalReadState.ReadValueChunk_OnPartialValue;
                }
                else
                {
                    _incReadState = IncrementalReadState.ReadValueChunk_OnCachedValue;
                    _nextNextParsingFunction = _nextParsingFunction;
                    _nextParsingFunction = _parsingFunction;
                }
                _parsingFunction = ParsingFunction.InReadValueChunk;
                _readValueOffset = 0;
            }

            if (count == 0)
            {
                return 0;
            }

            // read what is already cached in curNode
            int readCount = 0;
            int read = _curNode.CopyTo(_readValueOffset, buffer, index + readCount, count - readCount);
            readCount += read;
            _readValueOffset += read;

            if (readCount == count)
            {
                // take care of surrogate pairs spanning between buffers
                char ch = buffer[index + count - 1];
                if (XmlCharType.IsHighSurrogate(ch))
                {
                    readCount--;
                    _readValueOffset--;
                    if (readCount == 0)
                    {
                        Throw(SR.Xml_NotEnoughSpaceForSurrogatePair);
                    }
                }
                return readCount;
            }

            // if on partial value, read the rest of it
            if (_incReadState == IncrementalReadState.ReadValueChunk_OnPartialValue)
            {
                _curNode.SetValue(string.Empty);

                // read next chunk of text
                bool endOfValue = false;
                int startPos = 0;
                int endPos = 0;
                while (readCount < count && !endOfValue)
                {
                    int orChars = 0;
                    var tuple_0 = await ParseTextAsync(orChars).ConfigureAwait(false);
                    startPos = tuple_0.Item1;
                    endPos = tuple_0.Item2;

                    endOfValue = tuple_0.Item4;

                    int copyCount = count - readCount;
                    if (copyCount > endPos - startPos)
                    {
                        copyCount = endPos - startPos;
                    }
                    BlockCopyChars(_ps.chars, startPos, buffer, (index + readCount), copyCount);

                    readCount += copyCount;
                    startPos += copyCount;
                }

                _incReadState = endOfValue ? IncrementalReadState.ReadValueChunk_OnCachedValue : IncrementalReadState.ReadValueChunk_OnPartialValue;

                if (readCount == count)
                {
                    char ch = buffer[index + count - 1];
                    if (XmlCharType.IsHighSurrogate(ch))
                    {
                        readCount--;
                        startPos--;
                        if (readCount == 0)
                        {
                            Throw(SR.Xml_NotEnoughSpaceForSurrogatePair);
                        }
                    }
                }

                _readValueOffset = 0;
                _curNode.SetValue(_ps.chars, startPos, endPos - startPos);
            }
            return readCount;
        }

        internal Task<int> DtdParserProxy_ReadDataAsync()
        {
            CheckAsyncCall();
            return this.ReadDataAsync();
        }

        internal async Task<int> DtdParserProxy_ParseNumericCharRefAsync(StringBuilder? internalSubsetBuilder)
        {
            CheckAsyncCall();

            var tuple_1 = await this.ParseNumericCharRefAsync(true, internalSubsetBuilder).ConfigureAwait(false);
            return tuple_1.Item2;
        }

        internal Task<int> DtdParserProxy_ParseNamedCharRefAsync(bool expand, StringBuilder? internalSubsetBuilder)
        {
            CheckAsyncCall();
            return this.ParseNamedCharRefAsync(expand, internalSubsetBuilder);
        }

        internal async Task DtdParserProxy_ParsePIAsync(StringBuilder? sb)
        {
            CheckAsyncCall();
            if (sb == null)
            {
                ParsingMode pm = _parsingMode;
                _parsingMode = ParsingMode.SkipNode;
                await ParsePIAsync(null).ConfigureAwait(false);
                _parsingMode = pm;
            }
            else
            {
                await ParsePIAsync(sb).ConfigureAwait(false);
            }
        }

        internal async Task DtdParserProxy_ParseCommentAsync(StringBuilder? sb)
        {
            CheckAsyncCall();
            Debug.Assert(_parsingMode == ParsingMode.Full);

            try
            {
                if (sb == null)
                {
                    ParsingMode savedParsingMode = _parsingMode;
                    _parsingMode = ParsingMode.SkipNode;
                    await ParseCDataOrCommentAsync(XmlNodeType.Comment).ConfigureAwait(false);
                    _parsingMode = savedParsingMode;
                }
                else
                {
                    NodeData originalCurNode = _curNode;

                    _curNode = AddNode(_index + _attrCount + 1, _index);
                    await ParseCDataOrCommentAsync(XmlNodeType.Comment).ConfigureAwait(false);
                    _curNode.CopyTo(0, sb);

                    _curNode = originalCurNode;
                }
            }
            catch (XmlException e)
            {
                if (e.ResString == SR.Xml_UnexpectedEOF && _ps.entity != null)
                {
                    SendValidationEvent(XmlSeverityType.Error, SR.Sch_ParEntityRefNesting, null, _ps.LineNo, _ps.LinePos);
                }
                else
                {
                    throw;
                }
            }
        }

        internal async Task<(int, bool)> DtdParserProxy_PushEntityAsync(IDtdEntityInfo entity)
        {
            CheckAsyncCall();
            int entityId;

            bool retValue;
            if (entity.IsExternal)
            {
                if (IsResolverNull)
                {
                    entityId = -1;

                    return (entityId, false);
                }
                retValue = await PushExternalEntityAsync(entity).ConfigureAwait(false);
            }
            else
            {
                PushInternalEntity(entity);
                retValue = true;
            }
            entityId = _ps.entityId;

            return (entityId, retValue);
        }

        // SxS: The caller did not provide any SxS sensitive name or resource. No resource is being exposed either.
        // It is OK to suppress SxS warning.
        internal async Task<bool> DtdParserProxy_PushExternalSubsetAsync(string? systemId, string? publicId)
        {
            CheckAsyncCall();
            Debug.Assert(_parsingStatesStackTop == -1);
            Debug.Assert((systemId != null && systemId.Length > 0) || (publicId != null && publicId.Length > 0));

            if (IsResolverNull)
            {
                return false;
            }

            // Resolve base URI
            if (_ps.baseUri == null && !string.IsNullOrEmpty(_ps.baseUriStr))
            {
                _ps.baseUri = _xmlResolver!.ResolveUri(null, _ps.baseUriStr);
            }
            await PushExternalEntityOrSubsetAsync(publicId, systemId, _ps.baseUri, null).ConfigureAwait(false);

            _ps.entity = null;
            _ps.entityId = 0;

            Debug.Assert(_ps.appendMode);
            int initialPos = _ps.charPos;
            if (_v1Compat)
            {
                await EatWhitespacesAsync(null).ConfigureAwait(false);
            }
            if (!await ParseXmlDeclarationAsync(true).ConfigureAwait(false))
            {
                _ps.charPos = initialPos;
            }

            return true;
        }

        private Task InitStreamInputAsync(Uri baseUri, Stream stream, Encoding? encoding)
        {
            Debug.Assert(baseUri != null);
            return InitStreamInputAsync(baseUri, baseUri.ToString(), stream, null, 0, encoding);
        }

        private async Task InitStreamInputAsync(Uri? baseUri, string baseUriStr, Stream stream, byte[]? bytes, int byteCount, Encoding? encoding)
        {
            Debug.Assert(_ps.charPos == 0 && _ps.charsUsed == 0 && _ps.textReader == null);
            Debug.Assert(baseUriStr != null);
            Debug.Assert(baseUri == null || (baseUri.ToString().Equals(baseUriStr)));

            _ps.stream = stream;
            _ps.baseUri = baseUri;
            _ps.baseUriStr = baseUriStr;

            // take over the byte buffer allocated in XmlReader.Create, if available
            int bufferSize;
            if (bytes != null)
            {
                _ps.bytes = bytes;
                _ps.bytesUsed = byteCount;
                bufferSize = _ps.bytes.Length;
            }
            else
            {
                // allocate the byte buffer

                if (_laterInitParam != null && _laterInitParam.useAsync)
                {
                    bufferSize = AsyncBufferSize;
                }
                else
                {
                    bufferSize = XmlReader.CalcBufferSize(stream);
                }

                if (_ps.bytes == null || _ps.bytes.Length < bufferSize)
                {
                    _ps.bytes = new byte[bufferSize];
                }
            }

            // allocate char buffer
            if (_ps.chars == null || _ps.chars.Length < bufferSize + 1)
            {
                _ps.chars = new char[bufferSize + 1];
            }

            // make sure we have at least 4 bytes to detect the encoding (no preamble of System.Text supported encoding is longer than 4 bytes)
            _ps.bytePos = 0;
            if (_ps.bytesUsed < 4 && _ps.bytes.Length - _ps.bytesUsed > 0)
            {
                int bytesToRead = Math.Min(4, _ps.bytes.Length - _ps.bytesUsed);
                int read = await stream.ReadAtLeastAsync(_ps.bytes.AsMemory(_ps.bytesUsed), bytesToRead, throwOnEndOfStream: false).ConfigureAwait(false);
                if (read < bytesToRead)
                {
                    _ps.isStreamEof = true;
                }
                _ps.bytesUsed += read;
            }

            // detect & setup encoding
            if (encoding == null)
            {
                encoding = DetectEncoding();
            }
            SetupEncoding(encoding);

            // eat preamble
            EatPreamble();

            _documentStartBytePos = _ps.bytePos;

            _ps.eolNormalized = !_normalize;

            // decode first characters
            _ps.appendMode = true;
            await ReadDataAsync().ConfigureAwait(false);
        }
        private Task InitTextReaderInputAsync(string baseUriStr, TextReader input)
        {
            return InitTextReaderInputAsync(baseUriStr, null, input);
        }

        private Task InitTextReaderInputAsync(string baseUriStr, Uri? baseUri, TextReader input)
        {
            Debug.Assert(_ps.charPos == 0 && _ps.charsUsed == 0 && _ps.stream == null);
            Debug.Assert(baseUriStr != null);

            _ps.textReader = input;
            _ps.baseUriStr = baseUriStr;
            _ps.baseUri = baseUri;

            if (_ps.chars == null)
            {
                int bufferSize;

                if (_laterInitParam != null && _laterInitParam.useAsync)
                {
                    bufferSize = XmlReader.AsyncBufferSize;
                }
                else
                {
                    bufferSize = XmlReader.DefaultBufferSize;
                }

                _ps.chars = new char[bufferSize + 1];
            }

            _ps.encoding = Encoding.Unicode;
            _ps.eolNormalized = !_normalize;

            // read first characters
            _ps.appendMode = true;
            return ReadDataAsync();
        }

        private Task ProcessDtdFromParserContextAsync(XmlParserContext context)
        {
            Debug.Assert(context != null && context.HasDtdInfo);

            switch (_dtdProcessing)
            {
                case DtdProcessing.Prohibit:
                    ThrowWithoutLineInfo(SR.Xml_DtdIsProhibitedEx);
                    break;
                case DtdProcessing.Ignore:
                    // do nothing
                    break;
                case DtdProcessing.Parse:
                    return ParseDtdFromParserContextAsync();

                default:
                    Debug.Fail("Unhandled DtdProcessing enumeration value.");
                    break;
            }

            return Task.CompletedTask;
        }

        // Switches the reader's encoding
        private Task SwitchEncodingAsync(Encoding newEncoding)
        {
            if ((newEncoding.WebName != _ps.encoding!.WebName || _ps.decoder is SafeAsciiDecoder) && !_afterResetState)
            {
                Debug.Assert(_ps.stream != null);
                UnDecodeChars();
                _ps.appendMode = false;
                SetupEncoding(newEncoding);
                return ReadDataAsync();
            }

            return Task.CompletedTask;
        }

        private Task SwitchEncodingToUTF8Async()
        {
            return SwitchEncodingAsync(new UTF8Encoding(true, true));
        }

        // Reads more data to the character buffer, discarding already parsed chars / decoded bytes.
        private async Task<int> ReadDataAsync()
        {
            // Append Mode:  Append new bytes and characters to the buffers, do not rewrite them. Allocate new buffers
            //               if the current ones are full
            // Rewrite Mode: Reuse the buffers. If there is less than half of the char buffer left for new data, move
            //               the characters that has not been parsed yet to the front of the buffer. Same for bytes.

            if (_ps.isEof)
            {
                return 0;
            }

            int charsRead;
            if (_ps.appendMode)
            {
                // the character buffer is full -> allocate a new one
                if (_ps.charsUsed == _ps.chars.Length - 1)
                {
                    // invalidate node values kept in buffer - applies to attribute values only
                    for (int i = 0; i < _attrCount; i++)
                    {
                        _nodes[_index + i + 1].OnBufferInvalidated();
                    }

                    char[] newChars = new char[_ps.chars.Length * 2];
                    BlockCopyChars(_ps.chars, 0, newChars, 0, _ps.chars.Length);
                    _ps.chars = newChars;
                }

                if (_ps.stream != null)
                {
                    // the byte buffer is full -> allocate a new one
                    if (_ps.bytesUsed - _ps.bytePos < MaxByteSequenceLen)
                    {
                        if (_ps.bytes!.Length - _ps.bytesUsed < MaxByteSequenceLen)
                        {
                            byte[] newBytes = new byte[_ps.bytes.Length * 2];
                            BlockCopy(_ps.bytes, 0, newBytes, 0, _ps.bytesUsed);
                            _ps.bytes = newBytes;
                        }
                    }
                }

                charsRead = _ps.chars.Length - _ps.charsUsed - 1;
                if (charsRead > ApproxXmlDeclLength)
                {
                    charsRead = ApproxXmlDeclLength;
                }
            }
            else
            {
                int charsLen = _ps.chars.Length;
                if (charsLen - _ps.charsUsed <= charsLen / 2)
                {
                    // invalidate node values kept in buffer - applies to attribute values only
                    for (int i = 0; i < _attrCount; i++)
                    {
                        _nodes[_index + i + 1].OnBufferInvalidated();
                    }

                    // move unparsed characters to front, unless the whole buffer contains unparsed characters
                    int copyCharsCount = _ps.charsUsed - _ps.charPos;
                    if (copyCharsCount < charsLen - 1)
                    {
                        _ps.lineStartPos = _ps.lineStartPos - _ps.charPos;
                        if (copyCharsCount > 0)
                        {
                            BlockCopyChars(_ps.chars, _ps.charPos, _ps.chars, 0, copyCharsCount);
                        }
                        _ps.charPos = 0;
                        _ps.charsUsed = copyCharsCount;
                    }
                    else
                    {
                        char[] newChars = new char[_ps.chars.Length * 2];
                        BlockCopyChars(_ps.chars, 0, newChars, 0, _ps.chars.Length);
                        _ps.chars = newChars;
                    }
                }

                if (_ps.stream != null)
                {
                    // move undecoded bytes to the front to make some space in the byte buffer
                    int bytesLeft = _ps.bytesUsed - _ps.bytePos;
                    if (bytesLeft <= MaxBytesToMove)
                    {
                        if (bytesLeft == 0)
                        {
                            _ps.bytesUsed = 0;
                        }
                        else
                        {
                            BlockCopy(_ps.bytes!, _ps.bytePos, _ps.bytes!, 0, bytesLeft);
                            _ps.bytesUsed = bytesLeft;
                        }
                        _ps.bytePos = 0;
                    }
                }
                charsRead = _ps.chars.Length - _ps.charsUsed - 1;
            }

            if (_ps.stream != null)
            {
                if (!_ps.isStreamEof)
                {
                    // read new bytes
                    if (_ps.bytePos == _ps.bytesUsed && _ps.bytes!.Length - _ps.bytesUsed > 0)
                    {
                        int read = await _ps.stream.ReadAsync(_ps.bytes.AsMemory(_ps.bytesUsed)).ConfigureAwait(false);
                        if (read == 0)
                        {
                            _ps.isStreamEof = true;
                        }
                        _ps.bytesUsed += read;
                    }
                }

                int originalBytePos = _ps.bytePos;

                // decode chars
                charsRead = GetChars(charsRead);
                if (charsRead == 0 && _ps.bytePos != originalBytePos)
                {
                    // GetChars consumed some bytes but it was not enough bytes to form a character -> try again
                    return await ReadDataAsync().ConfigureAwait(false);
                }
            }
            else if (_ps.textReader != null)
            {
                // read chars
                charsRead = await _ps.textReader.ReadAsync(_ps.chars.AsMemory(_ps.charsUsed, _ps.chars.Length - _ps.charsUsed - 1)).ConfigureAwait(false);
                _ps.charsUsed += charsRead;
            }
            else
            {
                charsRead = 0;
            }

            RegisterConsumedCharacters(charsRead, InEntity);

            if (charsRead == 0)
            {
                Debug.Assert(_ps.charsUsed < _ps.chars.Length);
                _ps.isEof = true;
            }
            _ps.chars[_ps.charsUsed] = (char)0;
            return charsRead;
        }

        // Parses the xml or text declaration and switched encoding if needed
        private async Task<bool> ParseXmlDeclarationAsync(bool isTextDecl)
        {
            while (_ps.charsUsed - _ps.charPos < 6)
            {  // minimum "<?xml "
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    goto NoXmlDecl;
                }
            }

            if (!XmlConvert.StrEqual(_ps.chars, _ps.charPos, 5, XmlDeclarationBeginning) ||
                 XmlCharType.IsNameSingleChar(_ps.chars[_ps.charPos + 5]))
            {
                goto NoXmlDecl;
            }

            if (!isTextDecl)
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos + 2);
                _curNode.SetNamedNode(XmlNodeType.XmlDeclaration, _xml);
            }
            _ps.charPos += 5;

            // parsing of text declarations cannot change global stringBuilder or curNode as we may be in the middle of a text node
            Debug.Assert(_stringBuilder.Length == 0 || isTextDecl);
            StringBuilder sb = isTextDecl ? new StringBuilder() : _stringBuilder;

            // parse version, encoding & standalone attributes
            int xmlDeclState = 0;   // <?xml (0) version='1.0' (1) encoding='__' (2) standalone='__' (3) ?>
            Encoding? encoding = null;

            while (true)
            {
                int originalSbLen = sb.Length;
                int wsCount = await EatWhitespacesAsync(xmlDeclState == 0 ? null : sb).ConfigureAwait(false);

                // end of xml declaration
                if (_ps.chars[_ps.charPos] == '?')
                {
                    sb.Length = originalSbLen;

                    if (_ps.chars[_ps.charPos + 1] == '>')
                    {
                        if (xmlDeclState == 0)
                        {
                            Throw(isTextDecl ? SR.Xml_InvalidTextDecl : SR.Xml_InvalidXmlDecl);
                        }

                        _ps.charPos += 2;
                        if (!isTextDecl)
                        {
                            _curNode.SetValue(sb.ToString());
                            sb.Length = 0;

                            _nextParsingFunction = _parsingFunction;
                            _parsingFunction = ParsingFunction.ResetAttributesRootLevel;
                        }

                        // switch to encoding specified in xml declaration
                        if (encoding == null)
                        {
                            if (isTextDecl)
                            {
                                Throw(SR.Xml_InvalidTextDecl);
                            }
                            // Needed only for XmlTextReader
                            if (_afterResetState)
                            {
                                // check for invalid encoding switches to default encoding
                                string encodingName = _ps.encoding!.WebName;
                                if (encodingName != "utf-8" && encodingName != "utf-16" &&
                                     encodingName != "utf-16BE" && !(_ps.encoding is Ucs4Encoding))
                                {
                                    Throw(SR.Xml_EncodingSwitchAfterResetState, (_ps.encoding.GetByteCount("A") == 1) ? "UTF-8" : "UTF-16");
                                }
                            }
                            if (_ps.decoder is SafeAsciiDecoder)
                            {
                                await SwitchEncodingToUTF8Async().ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await SwitchEncodingAsync(encoding).ConfigureAwait(false);
                        }
                        _ps.appendMode = false;
                        return true;
                    }
                    else if (_ps.charPos + 1 == _ps.charsUsed)
                    {
                        goto ReadData;
                    }
                    else
                    {
                        ThrowUnexpectedToken("'>'");
                    }
                }

                if (wsCount == 0 && xmlDeclState != 0)
                {
                    ThrowUnexpectedToken("?>");
                }

                // read attribute name
                int nameEndPos = await ParseNameAsync().ConfigureAwait(false);

                NodeData? attr = null;
                switch (_ps.chars[_ps.charPos])
                {
                    case 'v':
                        if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos, "version") && xmlDeclState == 0)
                        {
                            if (!isTextDecl)
                            {
                                attr = AddAttributeNoChecks("version", 1);
                            }
                            break;
                        }
                        goto default;
                    case 'e':
                        if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos, "encoding") &&
                            (xmlDeclState == 1 || (isTextDecl && xmlDeclState == 0)))
                        {
                            if (!isTextDecl)
                            {
                                attr = AddAttributeNoChecks("encoding", 1);
                            }
                            xmlDeclState = 1;
                            break;
                        }
                        goto default;
                    case 's':
                        if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos, "standalone") &&
                             (xmlDeclState == 1 || xmlDeclState == 2) && !isTextDecl)
                        {
                            if (!isTextDecl)
                            {
                                attr = AddAttributeNoChecks("standalone", 1);
                            }
                            xmlDeclState = 2;
                            break;
                        }
                        goto default;
                    default:
                        Throw(isTextDecl ? SR.Xml_InvalidTextDecl : SR.Xml_InvalidXmlDecl);
                        break;
                }
                if (!isTextDecl)
                {
                    attr!.SetLineInfo(_ps.LineNo, _ps.LinePos);
                }
                sb.Append(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos);
                _ps.charPos = nameEndPos;

                // parse equals and quote char;
                if (_ps.chars[_ps.charPos] != '=')
                {
                    await EatWhitespacesAsync(sb).ConfigureAwait(false);
                    if (_ps.chars[_ps.charPos] != '=')
                    {
                        ThrowUnexpectedToken("=");
                    }
                }
                sb.Append('=');
                _ps.charPos++;

                char quoteChar = _ps.chars[_ps.charPos];
                if (quoteChar != '"' && quoteChar != '\'')
                {
                    await EatWhitespacesAsync(sb).ConfigureAwait(false);
                    quoteChar = _ps.chars[_ps.charPos];
                    if (quoteChar != '"' && quoteChar != '\'')
                    {
                        ThrowUnexpectedToken("\"", "'");
                    }
                }
                sb.Append(quoteChar);
                _ps.charPos++;
                if (!isTextDecl)
                {
                    attr!.quoteChar = quoteChar;
                    attr.SetLineInfo2(_ps.LineNo, _ps.LinePos);
                }

                // parse attribute value
                int pos = _ps.charPos;
                char[] chars;
            Continue:
                chars = _ps.chars;

                while (XmlCharType.IsAttributeValueChar(chars[pos]))
                {
                    pos++;
                }

                if (_ps.chars[pos] == quoteChar)
                {
                    switch (xmlDeclState)
                    {
                        // version
                        case 0:
                            // VersionNum  ::=  '1.0'        (XML Fourth Edition and earlier)
                            if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, pos - _ps.charPos, "1.0"))
                            {
                                if (!isTextDecl)
                                {
                                    attr!.SetValue(_ps.chars, _ps.charPos, pos - _ps.charPos);
                                }
                                xmlDeclState = 1;
                            }
                            else
                            {
                                string badVersion = new string(_ps.chars, _ps.charPos, pos - _ps.charPos);
                                Throw(SR.Xml_InvalidVersionNumber, badVersion);
                            }
                            break;
                        case 1:
                            string encName = new string(_ps.chars, _ps.charPos, pos - _ps.charPos);
                            encoding = CheckEncoding(encName);
                            if (!isTextDecl)
                            {
                                attr!.SetValue(encName);
                            }
                            xmlDeclState = 2;
                            break;
                        case 2:
                            if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, pos - _ps.charPos, "yes"))
                            {
                                _standalone = true;
                            }
                            else if (XmlConvert.StrEqual(_ps.chars, _ps.charPos, pos - _ps.charPos, "no"))
                            {
                                _standalone = false;
                            }
                            else
                            {
                                Debug.Assert(!isTextDecl);
                                Throw(SR.Xml_InvalidXmlDecl, _ps.LineNo, _ps.LinePos - 1);
                            }
                            if (!isTextDecl)
                            {
                                attr!.SetValue(_ps.chars, _ps.charPos, pos - _ps.charPos);
                            }
                            xmlDeclState = 3;
                            break;
                        default:
                            Debug.Fail($"Unexpected xmlDeclState {xmlDeclState}");
                            break;
                    }
                    sb.Append(chars, _ps.charPos, pos - _ps.charPos);
                    sb.Append(quoteChar);
                    _ps.charPos = pos + 1;
                    continue;
                }
                else if (pos == _ps.charsUsed)
                {
                    if (await ReadDataAsync().ConfigureAwait(false) != 0)
                    {
                        goto Continue;
                    }
                    else
                    {
                        Throw(SR.Xml_UnclosedQuote);
                    }
                }
                else
                {
                    Throw(isTextDecl ? SR.Xml_InvalidTextDecl : SR.Xml_InvalidXmlDecl);
                }

            ReadData:
                if (_ps.isEof || await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(SR.Xml_UnexpectedEOF1);
                }
            }

        NoXmlDecl:
            // no xml declaration
            if (!isTextDecl)
            {
                _parsingFunction = _nextParsingFunction;
            }
            // Needed only for XmlTextReader
            if (_afterResetState)
            {
                // check for invalid encoding switches to default encoding
                string encodingName = _ps.encoding!.WebName;
                if (encodingName != "utf-8" && encodingName != "utf-16" &&
                    encodingName != "utf-16BE" && !(_ps.encoding is Ucs4Encoding))
                {
                    Throw(SR.Xml_EncodingSwitchAfterResetState, (_ps.encoding.GetByteCount("A") == 1) ? "UTF-8" : "UTF-16");
                }
            }
            if (_ps.decoder is SafeAsciiDecoder)
            {
                await SwitchEncodingToUTF8Async().ConfigureAwait(false);
            }
            _ps.appendMode = false;
            return false;
        }



        // Parses the document content, no async keyword for perf optimize
        private Task<bool> ParseDocumentContentAsync()
        {
            while (true)
            {
                bool needMoreChars = false;
                int pos = _ps.charPos;
                char[] chars = _ps.chars;

                // some tag
                if (chars[pos] == '<')
                {
                    needMoreChars = true;
                    if (_ps.charsUsed - pos < 4) // minimum  "<a/>"
                        return ParseDocumentContentAsync_ReadData(needMoreChars);
                    pos++;
                    switch (chars[pos])
                    {
                        // processing instruction
                        case '?':
                            _ps.charPos = pos + 1;
                            return ParsePIAsync().ContinueBoolTaskFuncWhenFalseAsync(thisRef => thisRef.ParseDocumentContentAsync(), this);
                        case '!':
                            pos++;
                            if (_ps.charsUsed - pos < 2) // minimum characters expected "--"
                                return ParseDocumentContentAsync_ReadData(needMoreChars);
                            // comment
                            if (chars[pos] == '-')
                            {
                                if (chars[pos + 1] == '-')
                                {
                                    _ps.charPos = pos + 2;
                                    return ParseCommentAsync().ContinueBoolTaskFuncWhenFalseAsync(thisRef => thisRef.ParseDocumentContentAsync(), this);
                                }
                                else
                                {
                                    ThrowUnexpectedToken(pos + 1, "-");
                                }
                            }
                            // CDATA section
                            else if (chars[pos] == '[')
                            {
                                if (_fragmentType != XmlNodeType.Document)
                                {
                                    pos++;
                                    if (_ps.charsUsed - pos < 6)
                                    {
                                        return ParseDocumentContentAsync_ReadData(needMoreChars);
                                    }
                                    if (XmlConvert.StrEqual(chars, pos, 6, "CDATA["))
                                    {
                                        _ps.charPos = pos + 6;
                                        return ParseCDataAsync().CallBoolTaskFuncWhenFinishAsync(thisRef => thisRef.ParseDocumentContentAsync_CData(), this);
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos, "CDATA[");
                                    }
                                }
                                else
                                {
                                    Throw(_ps.charPos, SR.Xml_InvalidRootData);
                                }
                            }
                            // DOCTYPE declaration
                            else
                            {
                                if (_fragmentType == XmlNodeType.Document || _fragmentType == XmlNodeType.None)
                                {
                                    _fragmentType = XmlNodeType.Document;
                                    _ps.charPos = pos;
                                    return ParseDoctypeDeclAsync().ContinueBoolTaskFuncWhenFalseAsync(thisRef => thisRef.ParseDocumentContentAsync(), this);
                                }
                                else
                                {
                                    if (ParseUnexpectedToken(pos) == "DOCTYPE")
                                    {
                                        Throw(SR.Xml_BadDTDLocation);
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos, "<!--", "<[CDATA[");
                                    }
                                }
                            }
                            break;
                        case '/':
                            Throw(pos + 1, SR.Xml_UnexpectedEndTag);
                            break;
                        // document element start tag
                        default:
                            if (_rootElementParsed)
                            {
                                if (_fragmentType == XmlNodeType.Document)
                                {
                                    Throw(pos, SR.Xml_MultipleRoots);
                                }
                                if (_fragmentType == XmlNodeType.None)
                                {
                                    _fragmentType = XmlNodeType.Element;
                                }
                            }
                            _ps.charPos = pos;
                            _rootElementParsed = true;
                            return ParseElementAsync().ReturnTrueTaskWhenFinishAsync();
                    }
                }
                else if (chars[pos] == '&')
                {
                    return ParseDocumentContentAsync_ParseEntity();
                }
                // end of buffer
                else if (pos == _ps.charsUsed || (_v1Compat && chars[pos] == 0x0))
                {
                    return ParseDocumentContentAsync_ReadData(needMoreChars);
                }
                // something else -> root level whitespace
                else
                {
                    if (_fragmentType == XmlNodeType.Document)
                    {
                        return ParseRootLevelWhitespaceAsync().ContinueBoolTaskFuncWhenFalseAsync(thisRef => thisRef.ParseDocumentContentAsync(), this);
                    }
                    else
                    {
                        return ParseDocumentContentAsync_WhiteSpace();
                    }
                }

                Debug.Assert(pos == _ps.charsUsed && !_ps.isEof);
            }
        }

        private Task<bool> ParseDocumentContentAsync_CData()
        {
            if (_fragmentType == XmlNodeType.None)
            {
                _fragmentType = XmlNodeType.Element;
            }
            return AsyncHelper.DoneTaskTrue;
        }

        private async Task<bool> ParseDocumentContentAsync_ParseEntity()
        {
            int pos = _ps.charPos;

            if (_fragmentType == XmlNodeType.Document)
            {
                Throw(pos, SR.Xml_InvalidRootData);
                return false;
            }
            else
            {
                if (_fragmentType == XmlNodeType.None)
                {
                    _fragmentType = XmlNodeType.Element;
                }

                var tuple_3 = await HandleEntityReferenceAsync(false, EntityExpandType.OnlyGeneral).ConfigureAwait(false);

                switch (tuple_3.Item2)
                {
                    // Needed only for XmlTextReader (reporting of entities)
                    case EntityType.Unexpanded:
                        if (_parsingFunction == ParsingFunction.EntityReference)
                        {
                            _parsingFunction = _nextParsingFunction;
                        }
                        await ParseEntityReferenceAsync().ConfigureAwait(false);
                        return true;
                    case EntityType.CharacterDec:
                    case EntityType.CharacterHex:
                    case EntityType.CharacterNamed:
                        if (await ParseTextAsync().ConfigureAwait(false))
                        {
                            return true;
                        }
                        return await ParseDocumentContentAsync().ConfigureAwait(false);
                    default:
                        return await ParseDocumentContentAsync().ConfigureAwait(false);
                }
            }
        }

        private Task<bool> ParseDocumentContentAsync_WhiteSpace()
        {
            Task<bool> task = ParseTextAsync();
            if (task.IsSuccess())
            {
                if (task.Result)
                {
                    if (_fragmentType == XmlNodeType.None && _curNode.type == XmlNodeType.Text)
                    {
                        _fragmentType = XmlNodeType.Element;
                    }
                    return AsyncHelper.DoneTaskTrue;
                }
                else
                {
                    return ParseDocumentContentAsync();
                }
            }
            else
            {
                return _ParseDocumentContentAsync_WhiteSpace(task);
            }
        }

        private async Task<bool> _ParseDocumentContentAsync_WhiteSpace(Task<bool> task)
        {
            if (await task.ConfigureAwait(false))
            {
                if (_fragmentType == XmlNodeType.None && _curNode.type == XmlNodeType.Text)
                {
                    _fragmentType = XmlNodeType.Element;
                }
                return true;
            }
            return await ParseDocumentContentAsync().ConfigureAwait(false);
        }

        private async Task<bool> ParseDocumentContentAsync_ReadData(bool needMoreChars)
        {
            // read new characters into the buffer
            if (await ReadDataAsync().ConfigureAwait(false) != 0)
            {
                return await ParseDocumentContentAsync().ConfigureAwait(false);
            }
            else
            {
                if (needMoreChars)
                {
                    Throw(SR.Xml_InvalidRootData);
                }

                if (InEntity)
                {
                    if (HandleEntityEnd(true))
                    {
                        SetupEndEntityNodeInContent();
                        return true;
                    }
                    return await ParseDocumentContentAsync().ConfigureAwait(false);
                }
                Debug.Assert(_index == 0);

                if (!_rootElementParsed && _fragmentType == XmlNodeType.Document)
                {
                    ThrowWithoutLineInfo(SR.Xml_MissingRoot);
                }
                if (_fragmentType == XmlNodeType.None)
                {
                    _fragmentType = _rootElementParsed ? XmlNodeType.Document : XmlNodeType.Element;
                }
                OnEof();
                return false;
            }
        }


        // Parses element content
        private Task<bool> ParseElementContentAsync()
        {
            while (true)
            {
                int pos = _ps.charPos;
                char[] chars = _ps.chars;

                switch (chars[pos])
                {
                    // some tag
                    case '<':
                        switch (chars[pos + 1])
                        {
                            // processing instruction
                            case '?':
                                _ps.charPos = pos + 2;
                                return ParsePIAsync().ContinueBoolTaskFuncWhenFalseAsync(thisRef => thisRef.ParseElementContentAsync(), this);
                            case '!':
                                pos += 2;
                                if (_ps.charsUsed - pos < 2)
                                    return ParseElementContent_ReadData();
                                // comment
                                if (chars[pos] == '-')
                                {
                                    if (chars[pos + 1] == '-')
                                    {
                                        _ps.charPos = pos + 2;
                                        return ParseCommentAsync().ContinueBoolTaskFuncWhenFalseAsync(thisRef => thisRef.ParseElementContentAsync(), this);
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos + 1, "-");
                                    }
                                }
                                // CDATA section
                                else if (chars[pos] == '[')
                                {
                                    pos++;
                                    if (_ps.charsUsed - pos < 6)
                                    {
                                        return ParseElementContent_ReadData();
                                    }
                                    if (XmlConvert.StrEqual(chars, pos, 6, "CDATA["))
                                    {
                                        _ps.charPos = pos + 6;
                                        return ParseCDataAsync().ReturnTrueTaskWhenFinishAsync();
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos, "CDATA[");
                                    }
                                }
                                else
                                {
                                    if (ParseUnexpectedToken(pos) == "DOCTYPE")
                                    {
                                        Throw(SR.Xml_BadDTDLocation);
                                    }
                                    else
                                    {
                                        ThrowUnexpectedToken(pos, "<!--", "<[CDATA[");
                                    }
                                }
                                break;
                            // element end tag
                            case '/':
                                _ps.charPos = pos + 2;
                                return ParseEndElementAsync().ReturnTrueTaskWhenFinishAsync();
                            default:
                                // end of buffer
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    return ParseElementContent_ReadData();
                                }
                                else
                                {
                                    // element start tag
                                    _ps.charPos = pos + 1;
                                    return ParseElementAsync().ReturnTrueTaskWhenFinishAsync();
                                }
                        }
                        break;
                    case '&':
                        return ParseTextAsync().ContinueBoolTaskFuncWhenFalseAsync(thisRef => thisRef.ParseElementContentAsync(), this);
                    default:
                        // end of buffer
                        if (pos == _ps.charsUsed)
                        {
                            return ParseElementContent_ReadData();
                        }
                        else
                        {
                            // text node, whitespace or entity reference
                            return ParseTextAsync().ContinueBoolTaskFuncWhenFalseAsync(thisRef => thisRef.ParseElementContentAsync(), this);
                        }
                }
            }
        }

        private async Task<bool> ParseElementContent_ReadData()
        {
            // read new characters into the buffer
            if (await ReadDataAsync().ConfigureAwait(false) == 0)
            {
                if (_ps.charsUsed - _ps.charPos != 0)
                {
                    ThrowUnclosedElements();
                }
                if (!InEntity)
                {
                    if (_index == 0 && _fragmentType != XmlNodeType.Document)
                    {
                        OnEof();
                        return false;
                    }
                    ThrowUnclosedElements();
                }
                if (HandleEntityEnd(true))
                {
                    SetupEndEntityNodeInContent();
                    return true;
                }
            }
            return await ParseElementContentAsync().ConfigureAwait(false);
        }

        // Parses the element start tag
        private Task ParseElementAsync()
        {
            int pos = _ps.charPos;
            char[] chars = _ps.chars;
            int colonPos = -1;

            _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);

        // PERF: we intentionally don't call ParseQName here to parse the element name unless a special
        // case occurs (like end of buffer, invalid name char)
        ContinueStartName:
            // check element name start char
            if (XmlCharType.IsStartNCNameSingleChar(chars[pos]))
            {
                pos++;
            }
            else
            {
                goto ParseQNameSlow;
            }

        ContinueName:
            // parse element name
            while (true)
            {
                if (XmlCharType.IsNCNameSingleChar(chars[pos]))
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }

            // colon -> save prefix end position and check next char if it's name start char
            if (chars[pos] == ':')
            {
                if (colonPos != -1)
                {
                    if (_supportNamespaces)
                    {
                        Throw(pos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(':', '\0'));
                    }
                    else
                    {
                        pos++;
                        goto ContinueName;
                    }
                }
                else
                {
                    colonPos = pos;
                    pos++;
                    goto ContinueStartName;
                }
            }
            else if (pos + 1 < _ps.charsUsed)
            {
                goto SetElement;
            }

        ParseQNameSlow:
            Task<(int, int)> parseQNameTask = ParseQNameAsync();
            return ParseElementAsync_ContinueWithSetElement(parseQNameTask);

        SetElement:
            return ParseElementAsync_SetElement(colonPos, pos);
        }

        private Task ParseElementAsync_ContinueWithSetElement(Task<(int, int)> task)
        {
            if (task.IsSuccess())
            {
                var tuple_4 = task.Result;
                int colonPos = tuple_4.Item1;
                int pos = tuple_4.Item2;
                return ParseElementAsync_SetElement(colonPos, pos);
            }
            else
            {
                return _ParseElementAsync_ContinueWithSetElement(task);
            }
        }

        private async Task _ParseElementAsync_ContinueWithSetElement(Task<(int, int)> task)
        {
            var tuple_4 = await task.ConfigureAwait(false);
            int colonPos = tuple_4.Item1;
            int pos = tuple_4.Item2;
            await ParseElementAsync_SetElement(colonPos, pos).ConfigureAwait(false);
        }

        private Task ParseElementAsync_SetElement(int colonPos, int pos)
        {
            char[] chars = _ps.chars;

            // push namespace context
            _namespaceManager!.PushScope();

            // init the NodeData class
            if (colonPos == -1 || !_supportNamespaces)
            {
                _curNode.SetNamedNode(XmlNodeType.Element,
                                      _nameTable.Add(chars, _ps.charPos, pos - _ps.charPos));
            }
            else
            {
                int startPos = _ps.charPos;
                int prefixLen = colonPos - startPos;
                if (prefixLen == _lastPrefix.Length && XmlConvert.StrEqual(chars, startPos, prefixLen, _lastPrefix))
                {
                    _curNode.SetNamedNode(XmlNodeType.Element,
                                          _nameTable.Add(chars, colonPos + 1, pos - colonPos - 1),
                                          _lastPrefix,
                                          null);
                }
                else
                {
                    _curNode.SetNamedNode(XmlNodeType.Element,
                                          _nameTable.Add(chars, colonPos + 1, pos - colonPos - 1),
                                          _nameTable.Add(chars, _ps.charPos, prefixLen),
                                          null);
                    _lastPrefix = _curNode.prefix;
                }
            }

            char ch = chars[pos];
            // whitespace after element name -> there are probably some attributes
            bool isWs;

            isWs = XmlCharType.IsWhiteSpace(ch);

            _ps.charPos = pos;
            if (isWs)
            {
                return ParseAttributesAsync();
            }
            // no attributes
            else
            {
                return ParseElementAsync_NoAttributes();
            }
        }

        private Task ParseElementAsync_NoAttributes()
        {
            int pos = _ps.charPos;
            char[] chars = _ps.chars;
            char ch = chars[pos];
            // non-empty element
            if (ch == '>')
            {
                _ps.charPos = pos + 1;
                _parsingFunction = ParsingFunction.MoveToElementContent;
            }
            // empty element
            else if (ch == '/')
            {
                if (pos + 1 == _ps.charsUsed)
                {
                    _ps.charPos = pos;
                    return ParseElementAsync_ReadData(pos);
                }
                if (chars[pos + 1] == '>')
                {
                    _curNode.IsEmptyElement = true;
                    _nextParsingFunction = _parsingFunction;
                    _parsingFunction = ParsingFunction.PopEmptyElementContext;
                    _ps.charPos = pos + 2;
                }
                else
                {
                    ThrowUnexpectedToken(pos, ">");
                }
            }
            // something else after the element name
            else
            {
                Throw(pos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(chars, _ps.charsUsed, pos));
            }

            // add default attributes & strip spaces in attributes with type other than CDATA
            if (_addDefaultAttributesAndNormalize)
            {
                AddDefaultAttributesAndNormalize();
            }

            // lookup element namespace
            ElementNamespaceLookup();

            return Task.CompletedTask;
        }

        private async Task ParseElementAsync_ReadData(int pos)
        {
            if (await ReadDataAsync().ConfigureAwait(false) == 0)
            {
                Throw(pos, SR.Xml_UnexpectedEOF, ">");
            }

            await ParseElementAsync_NoAttributes().ConfigureAwait(false);
        }

        private Task ParseEndElementAsync()
        {
            NodeData startTagNode = _nodes[_index - 1];

            int prefLen = startTagNode.prefix.Length;
            int locLen = startTagNode.localName.Length;

            if (_ps.charsUsed - _ps.charPos < prefLen + locLen + 1)
            {
                return _ParseEndElmentAsync();
            }

            return ParseEndElementAsync_CheckNameAndParse();
        }

        private async Task _ParseEndElmentAsync()
        {
            await ParseEndElmentAsync_PrepareData().ConfigureAwait(false);
            await ParseEndElementAsync_CheckNameAndParse().ConfigureAwait(false);
        }

        private async Task ParseEndElmentAsync_PrepareData()
        {
            // check if the end tag name equals start tag name
            NodeData startTagNode = _nodes[_index - 1];

            int prefLen = startTagNode.prefix.Length;
            int locLen = startTagNode.localName.Length;

            while (_ps.charsUsed - _ps.charPos < prefLen + locLen + 1)
            {
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    break;
                }
            }
        }

        private Task ParseEndElementAsync_CheckNameAndParse()
        {
            NodeData startTagNode = _nodes[_index - 1];
            int prefLen = startTagNode.prefix.Length;
            int locLen = startTagNode.localName.Length;

            int nameLen;
            char[] chars = _ps.chars;
            if (startTagNode.prefix.Length == 0)
            {
                if (!XmlConvert.StrEqual(chars, _ps.charPos, locLen, startTagNode.localName))
                {
                    return ThrowTagMismatchAsync(startTagNode);
                }
                nameLen = locLen;
            }
            else
            {
                int colonPos = _ps.charPos + prefLen;
                if (!XmlConvert.StrEqual(chars, _ps.charPos, prefLen, startTagNode.prefix) ||
                        chars[colonPos] != ':' ||
                        !XmlConvert.StrEqual(chars, colonPos + 1, locLen, startTagNode.localName))
                {
                    return ThrowTagMismatchAsync(startTagNode);
                }
                nameLen = locLen + prefLen + 1;
            }
            LineInfo endTagLineInfo = new LineInfo(_ps.lineNo, _ps.LinePos);
            return ParseEndElementAsync_Finish(nameLen, startTagNode, endTagLineInfo);
        }

        private enum ParseEndElementParseFunction
        {
            CheckEndTag,
            ReadData,
            Done
        }

        private ParseEndElementParseFunction _parseEndElement_NextFunc;

        private Task ParseEndElementAsync_Finish(int nameLen, NodeData startTagNode, LineInfo endTagLineInfo)
        {
            Task task = ParseEndElementAsync_CheckEndTag(nameLen, startTagNode, endTagLineInfo);
            while (true)
            {
                if (!task.IsSuccess())
                {
                    return ParseEndElementAsync_Finish(task, nameLen, startTagNode, endTagLineInfo);
                }

                switch (_parseEndElement_NextFunc)
                {
                    case ParseEndElementParseFunction.CheckEndTag:
                        task = ParseEndElementAsync_CheckEndTag(nameLen, startTagNode, endTagLineInfo);
                        break;
                    case ParseEndElementParseFunction.ReadData:
                        task = ParseEndElementAsync_ReadData();
                        break;
                    case ParseEndElementParseFunction.Done:
                        return task;
                }
            }
        }

        private async Task ParseEndElementAsync_Finish(Task task, int nameLen, NodeData startTagNode, LineInfo endTagLineInfo)
        {
            while (true)
            {
                await task.ConfigureAwait(false);
                switch (_parseEndElement_NextFunc)
                {
                    case ParseEndElementParseFunction.CheckEndTag:
                        task = ParseEndElementAsync_CheckEndTag(nameLen, startTagNode, endTagLineInfo);
                        break;
                    case ParseEndElementParseFunction.ReadData:
                        task = ParseEndElementAsync_ReadData();
                        break;
                    case ParseEndElementParseFunction.Done:
                        return;
                }
            }
        }

        private Task ParseEndElementAsync_CheckEndTag(int nameLen, NodeData startTagNode, LineInfo endTagLineInfo)
        {
            int pos;
            char[] chars;
            while (true)
            {
                pos = _ps.charPos + nameLen;
                chars = _ps.chars;

                if (pos == _ps.charsUsed)
                {
                    _parseEndElement_NextFunc = ParseEndElementParseFunction.ReadData;
                    return Task.CompletedTask;
                }

                bool tagMismatch = false;

                if (XmlCharType.IsNCNameSingleChar(chars[pos]) || (chars[pos] == ':'))
                {
                    tagMismatch = true;
                }

                if (tagMismatch)
                {
                    return ThrowTagMismatchAsync(startTagNode);
                }

                // eat whitespace
                if (chars[pos] != '>')
                {
                    char tmpCh;
                    while (XmlCharType.IsWhiteSpace(tmpCh = chars[pos]))
                    {
                        pos++;
                        switch (tmpCh)
                        {
                            case (char)0xA:
                                OnNewLine(pos);
                                continue;
                            case (char)0xD:
                                if (chars[pos] == (char)0xA)
                                {
                                    pos++;
                                }
                                else if (pos == _ps.charsUsed && !_ps.isEof)
                                {
                                    break;
                                }
                                OnNewLine(pos);
                                continue;
                        }
                    }
                }

                if (chars[pos] == '>')
                {
                    break;
                }
                else if (pos == _ps.charsUsed)
                {
                    _parseEndElement_NextFunc = ParseEndElementParseFunction.ReadData;
                    return Task.CompletedTask;
                }
                else
                {
                    ThrowUnexpectedToken(pos, ">");
                }

                Debug.Fail("We should never get to this point.");
            }

            Debug.Assert(_index > 0);
            _index--;
            _curNode = _nodes[_index];

            // set the element data
            Debug.Assert(_curNode == startTagNode);
            startTagNode.lineInfo = endTagLineInfo;
            startTagNode.type = XmlNodeType.EndElement;
            _ps.charPos = pos + 1;

            // set next parsing function
            _nextParsingFunction = (_index > 0) ? _parsingFunction : ParsingFunction.DocumentContent;
            _parsingFunction = ParsingFunction.PopElementContext;

            _parseEndElement_NextFunc = ParseEndElementParseFunction.Done;
            return Task.CompletedTask;
        }

        private async Task ParseEndElementAsync_ReadData()
        {
            if (await ReadDataAsync().ConfigureAwait(false) == 0)
            {
                ThrowUnclosedElements();
            }
            _parseEndElement_NextFunc = ParseEndElementParseFunction.CheckEndTag;
            return;
        }

        private async Task ThrowTagMismatchAsync(NodeData startTag)
        {
            if (startTag.type == XmlNodeType.Element)
            {
                // parse the bad name

                var tuple_5 = await ParseQNameAsync().ConfigureAwait(false);

                int endPos = tuple_5.Item2;

                string[] args = new string[4];
                args[0] = startTag.GetNameWPrefix(_nameTable);
                args[1] = startTag.lineInfo.lineNo.ToString(CultureInfo.InvariantCulture);
                args[2] = startTag.lineInfo.linePos.ToString(CultureInfo.InvariantCulture);
                args[3] = new string(_ps.chars, _ps.charPos, endPos - _ps.charPos);
                Throw(SR.Xml_TagMismatchEx, args);
            }
            else
            {
                Debug.Assert(startTag.type == XmlNodeType.EntityReference);
                Throw(SR.Xml_UnexpectedEndTag);
            }
        }

        // Reads the attributes
        private async Task ParseAttributesAsync()
        {
            int pos = _ps.charPos;
            char[] chars = _ps.chars;
            NodeData? attr = null;

            Debug.Assert(_attrCount == 0);

            while (true)
            {
                // eat whitespace
                int lineNoDelta = 0;
                char tmpch0;

                while (XmlCharType.IsWhiteSpace(tmpch0 = chars[pos]))
                {
                    if (tmpch0 == (char)0xA)
                    {
                        OnNewLine(pos + 1);
                        lineNoDelta++;
                    }
                    else if (tmpch0 == (char)0xD)
                    {
                        if (chars[pos + 1] == (char)0xA)
                        {
                            OnNewLine(pos + 2);
                            lineNoDelta++;
                            pos++;
                        }
                        else if (pos + 1 != _ps.charsUsed)
                        {
                            OnNewLine(pos + 1);
                            lineNoDelta++;
                        }
                        else
                        {
                            _ps.charPos = pos;
                            goto ReadData;
                        }
                    }
                    pos++;
                }

                char tmpch1;
                int startNameCharSize = 0;

                if (XmlCharType.IsStartNCNameSingleChar(tmpch1 = chars[pos]))
                {
                    startNameCharSize = 1;
                }

                if (startNameCharSize == 0)
                {
                    // element end
                    if (tmpch1 == '>')
                    {
                        Debug.Assert(_curNode.type == XmlNodeType.Element);
                        _ps.charPos = pos + 1;
                        _parsingFunction = ParsingFunction.MoveToElementContent;
                        goto End;
                    }
                    // empty element end
                    else if (tmpch1 == '/')
                    {
                        Debug.Assert(_curNode.type == XmlNodeType.Element);
                        if (pos + 1 == _ps.charsUsed)
                        {
                            goto ReadData;
                        }
                        if (chars[pos + 1] == '>')
                        {
                            _ps.charPos = pos + 2;
                            _curNode.IsEmptyElement = true;
                            _nextParsingFunction = _parsingFunction;
                            _parsingFunction = ParsingFunction.PopEmptyElementContext;
                            goto End;
                        }
                        else
                        {
                            ThrowUnexpectedToken(pos + 1, ">");
                        }
                    }
                    else if (pos == _ps.charsUsed)
                    {
                        goto ReadData;
                    }
                    else if (tmpch1 != ':' || _supportNamespaces)
                    {
                        Throw(pos, SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(chars, _ps.charsUsed, pos));
                    }
                }

                if (pos == _ps.charPos)
                {
                    ThrowExpectingWhitespace(pos);
                }
                _ps.charPos = pos;

                // save attribute name line position
                int attrNameLinePos = _ps.LinePos;

#if DEBUG
                int attrNameLineNo = _ps.LineNo;
#endif

                // parse attribute name
                int colonPos = -1;

                // PERF: we intentionally don't call ParseQName here to parse the element name unless a special
                // case occurs (like end of buffer, invalid name char)
                pos += startNameCharSize; // start name char has already been checked

            // parse attribute name
            ContinueParseName:
                char tmpch2;

                while (true)
                {
                    if (XmlCharType.IsNCNameSingleChar(tmpch2 = chars[pos]))
                    {
                        pos++;
                    }
                    else
                    {
                        break;
                    }
                }

                // colon -> save prefix end position and check next char if it's name start char
                if (tmpch2 == ':')
                {
                    if (colonPos != -1)
                    {
                        if (_supportNamespaces)
                        {
                            Throw(pos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(':', '\0'));
                        }
                        else
                        {
                            pos++;
                            goto ContinueParseName;
                        }
                    }
                    else
                    {
                        colonPos = pos;
                        pos++;

                        if (XmlCharType.IsStartNCNameSingleChar(chars[pos]))
                        {
                            pos++;
                            goto ContinueParseName;
                        }

                        // else fallback to full name parsing routine

                        var tuple_6 = await ParseQNameAsync().ConfigureAwait(false);
                        colonPos = tuple_6.Item1;

                        pos = tuple_6.Item2;

                        chars = _ps.chars;
                    }
                }
                else if (pos + 1 >= _ps.charsUsed)
                {
                    var tuple_7 = await ParseQNameAsync().ConfigureAwait(false);
                    colonPos = tuple_7.Item1;

                    pos = tuple_7.Item2;

                    chars = _ps.chars;
                }

                attr = AddAttribute(pos, colonPos);
                attr.SetLineInfo(_ps.LineNo, attrNameLinePos);

#if DEBUG
                Debug.Assert(attrNameLineNo == _ps.LineNo);
#endif

                // parse equals and quote char;
                if (chars[pos] != '=')
                {
                    _ps.charPos = pos;
                    await EatWhitespacesAsync(null).ConfigureAwait(false);
                    pos = _ps.charPos;
                    if (chars[pos] != '=')
                    {
                        ThrowUnexpectedToken("=");
                    }
                }
                pos++;

                char quoteChar = chars[pos];
                if (quoteChar != '"' && quoteChar != '\'')
                {
                    _ps.charPos = pos;
                    await EatWhitespacesAsync(null).ConfigureAwait(false);
                    pos = _ps.charPos;
                    quoteChar = chars[pos];
                    if (quoteChar != '"' && quoteChar != '\'')
                    {
                        ThrowUnexpectedToken("\"", "'");
                    }
                }
                pos++;
                _ps.charPos = pos;

                attr.quoteChar = quoteChar;
                attr.SetLineInfo2(_ps.LineNo, _ps.LinePos);

                // parse attribute value
                char tmpch3;
                while (XmlCharType.IsAttributeValueChar(tmpch3 = chars[pos]))
                {
                    pos++;
                }

                if (tmpch3 == quoteChar)
                {
#if DEBUG
                    if (_normalize)
                    {
                        string val = new string(chars, _ps.charPos, pos - _ps.charPos);
                        Debug.Assert(val == XmlComplianceUtil.CDataNormalize(val), "The attribute value is not CDATA normalized!");
                    }
#endif
                    attr.SetValue(chars, _ps.charPos, pos - _ps.charPos);
                    pos++;
                    _ps.charPos = pos;
                }
                else
                {
                    await ParseAttributeValueSlowAsync(pos, quoteChar, attr).ConfigureAwait(false);
                    pos = _ps.charPos;
                    chars = _ps.chars;
                }

                // handle special attributes:
                if (attr.prefix.Length == 0)
                {
                    // default namespace declaration
                    if (Ref.Equal(attr.localName, _xmlNs))
                    {
                        OnDefaultNamespaceDecl(attr);
                    }
                }
                else
                {
                    // prefixed namespace declaration
                    if (Ref.Equal(attr.prefix, _xmlNs))
                    {
                        OnNamespaceDecl(attr);
                    }
                    // xml: attribute
                    else if (Ref.Equal(attr.prefix, _xml))
                    {
                        OnXmlReservedAttribute(attr);
                    }
                }
                continue;

            ReadData:
                _ps.lineNo -= lineNoDelta;
                if (await ReadDataAsync().ConfigureAwait(false) != 0)
                {
                    pos = _ps.charPos;
                    chars = _ps.chars;
                }
                else
                {
                    ThrowUnclosedElements();
                }
            }

        End:
            if (_addDefaultAttributesAndNormalize)
            {
                AddDefaultAttributesAndNormalize();
            }
            // lookup namespaces: element
            ElementNamespaceLookup();

            // lookup namespaces: attributes
            if (_attrNeedNamespaceLookup)
            {
                AttributeNamespaceLookup();
                _attrNeedNamespaceLookup = false;
            }

            // check duplicate attributes
            if (_attrDuplWalkCount >= MaxAttrDuplWalkCount)
            {
                AttributeDuplCheck();
            }
        }

        private async Task ParseAttributeValueSlowAsync(int curPos, char quoteChar, NodeData attr)
        {
            int pos = curPos;
            char[] chars = _ps.chars;
            int attributeBaseEntityId = _ps.entityId;
            // Needed only for XmlTextReader (reporting of entities)
            int valueChunkStartPos = 0;
            LineInfo valueChunkLineInfo = new LineInfo(_ps.lineNo, _ps.LinePos);
            NodeData? lastChunk = null;

            Debug.Assert(_stringBuilder.Length == 0);

            while (true)
            {
                // parse the rest of the attribute value
                while (XmlCharType.IsAttributeValueChar(chars[pos]))
                {
                    pos++;
                }

                if (pos - _ps.charPos > 0)
                {
                    _stringBuilder.Append(chars, _ps.charPos, pos - _ps.charPos);
                    _ps.charPos = pos;
                }

                if (chars[pos] == quoteChar && attributeBaseEntityId == _ps.entityId)
                {
                    break;
                }
                else
                {
                    switch (chars[pos])
                    {
                        // eol
                        case (char)0xA:
                            pos++;
                            OnNewLine(pos);
                            if (_normalize)
                            {
                                _stringBuilder.Append((char)0x20);  // CDATA normalization of 0xA
                                _ps.charPos++;
                            }
                            continue;
                        case (char)0xD:
                            if (chars[pos + 1] == (char)0xA)
                            {
                                pos += 2;
                                if (_normalize)
                                {
                                    _stringBuilder.Append(_ps.eolNormalized ? "\u0020\u0020" : "\u0020"); // CDATA normalization of 0xD 0xA
                                    _ps.charPos = pos;
                                }
                            }
                            else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                            {
                                pos++;
                                if (_normalize)
                                {
                                    _stringBuilder.Append((char)0x20);  // CDATA normalization of 0xD and 0xD 0xA
                                    _ps.charPos = pos;
                                }
                            }
                            else
                            {
                                goto ReadData;
                            }
                            OnNewLine(pos);
                            continue;
                        // tab
                        case (char)0x9:
                            pos++;
                            if (_normalize)
                            {
                                _stringBuilder.Append((char)0x20);  // CDATA normalization of 0x9
                                _ps.charPos++;
                            }
                            continue;
                        case '"':
                        case '\'':
                        case '>':
                            pos++;
                            continue;
                        // attribute values cannot contain '<'
                        case '<':
                            Throw(pos, SR.Xml_BadAttributeChar, XmlException.BuildCharExceptionArgs('<', '\0'));
                            break;
                        // entity referece
                        case '&':
                            if (pos - _ps.charPos > 0)
                            {
                                _stringBuilder.Append(chars, _ps.charPos, pos - _ps.charPos);
                            }
                            _ps.charPos = pos;

                            // Needed only for XmlTextReader (reporting of entities)
                            int enclosingEntityId = _ps.entityId;
                            LineInfo entityLineInfo = new LineInfo(_ps.lineNo, _ps.LinePos + 1);

                            var tuple_8 = await HandleEntityReferenceAsync(true, EntityExpandType.All).ConfigureAwait(false);
                            pos = tuple_8.Item1;

                            switch (tuple_8.Item2)
                            {
                                case EntityType.CharacterDec:
                                case EntityType.CharacterHex:
                                case EntityType.CharacterNamed:
                                    break;
                                // Needed only for XmlTextReader (reporting of entities)
                                case EntityType.Unexpanded:
                                    if (_parsingMode == ParsingMode.Full && _ps.entityId == attributeBaseEntityId)
                                    {
                                        // construct text value chunk
                                        int valueChunkLen = _stringBuilder.Length - valueChunkStartPos;
                                        if (valueChunkLen > 0)
                                        {
                                            NodeData textChunk = new NodeData();
                                            textChunk.lineInfo = valueChunkLineInfo;
                                            textChunk.depth = attr.depth + 1;
                                            textChunk.SetValueNode(XmlNodeType.Text, _stringBuilder.ToString(valueChunkStartPos, valueChunkLen));
                                            AddAttributeChunkToList(attr, textChunk, ref lastChunk);
                                        }

                                        // parse entity name
                                        _ps.charPos++;
                                        string entityName = await ParseEntityNameAsync().ConfigureAwait(false);

                                        // construct entity reference chunk
                                        NodeData entityChunk = new NodeData();
                                        entityChunk.lineInfo = entityLineInfo;
                                        entityChunk.depth = attr.depth + 1;
                                        entityChunk.SetNamedNode(XmlNodeType.EntityReference, entityName);
                                        AddAttributeChunkToList(attr, entityChunk, ref lastChunk);

                                        // append entity ref to the attribute value
                                        _stringBuilder.Append('&');
                                        _stringBuilder.Append(entityName);
                                        _stringBuilder.Append(';');

                                        // update info for the next attribute value chunk
                                        valueChunkStartPos = _stringBuilder.Length;
                                        valueChunkLineInfo.Set(_ps.LineNo, _ps.LinePos);

                                        _fullAttrCleanup = true;
                                    }
                                    else
                                    {
                                        _ps.charPos++;
                                        await ParseEntityNameAsync().ConfigureAwait(false);
                                    }
                                    pos = _ps.charPos;
                                    break;

                                case EntityType.ExpandedInAttribute:
                                    if (_parsingMode == ParsingMode.Full && enclosingEntityId == attributeBaseEntityId)
                                    {
                                        // construct text value chunk
                                        int valueChunkLen = _stringBuilder.Length - valueChunkStartPos;
                                        if (valueChunkLen > 0)
                                        {
                                            NodeData textChunk = new NodeData();
                                            textChunk.lineInfo = valueChunkLineInfo;
                                            textChunk.depth = attr.depth + 1;
                                            textChunk.SetValueNode(XmlNodeType.Text, _stringBuilder.ToString(valueChunkStartPos, valueChunkLen));
                                            AddAttributeChunkToList(attr, textChunk, ref lastChunk);
                                        }

                                        // construct entity reference chunk
                                        NodeData entityChunk = new NodeData();
                                        entityChunk.lineInfo = entityLineInfo;
                                        entityChunk.depth = attr.depth + 1;
                                        entityChunk.SetNamedNode(XmlNodeType.EntityReference, _ps.entity!.Name);
                                        AddAttributeChunkToList(attr, entityChunk, ref lastChunk);

                                        _fullAttrCleanup = true;

                                        // Note: info for the next attribute value chunk will be updated once we
                                        // get out of the expanded entity
                                    }
                                    pos = _ps.charPos;
                                    break;
                                default:
                                    pos = _ps.charPos;
                                    break;
                            }
                            chars = _ps.chars;
                            continue;
                        default:
                            // end of buffer
                            if (pos == _ps.charsUsed)
                            {
                                goto ReadData;
                            }
                            // surrogate chars
                            else
                            {
                                char ch = chars[pos];
                                if (XmlCharType.IsHighSurrogate(ch))
                                {
                                    if (pos + 1 == _ps.charsUsed)
                                    {
                                        goto ReadData;
                                    }
                                    pos++;
                                    if (XmlCharType.IsLowSurrogate(chars[pos]))
                                    {
                                        pos++;
                                        continue;
                                    }
                                }
                                ThrowInvalidChar(chars, _ps.charsUsed, pos);
                                break;
                            }
                    }
                }

            ReadData:
                // read new characters into the buffer
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    if (_ps.charsUsed - _ps.charPos > 0)
                    {
                        if (_ps.chars[_ps.charPos] != (char)0xD)
                        {
                            Debug.Fail("We should never get to this point.");
                            Throw(SR.Xml_UnexpectedEOF1);
                        }
                        Debug.Assert(_ps.isEof);
                    }
                    else
                    {
                        if (!InEntity)
                        {
                            if (_fragmentType == XmlNodeType.Attribute)
                            {
                                if (attributeBaseEntityId != _ps.entityId)
                                {
                                    Throw(SR.Xml_EntityRefNesting);
                                }
                                break;
                            }
                            Throw(SR.Xml_UnclosedQuote);
                        }

                        if (HandleEntityEnd(true))
                        {
                            Debug.Fail("no EndEntity reporting while parsing attributes");
                            Throw(SR.Xml_InternalError);
                        }
                        // update info for the next attribute value chunk
                        if (attributeBaseEntityId == _ps.entityId)
                        {
                            valueChunkStartPos = _stringBuilder.Length;
                            valueChunkLineInfo.Set(_ps.LineNo, _ps.LinePos);
                        }
                    }
                }

                pos = _ps.charPos;
                chars = _ps.chars;
            }

            // Needed only for XmlTextReader (reporting of entities)
            if (attr.nextAttrValueChunk != null)
            {
                // construct last text value chunk
                int valueChunkLen = _stringBuilder.Length - valueChunkStartPos;
                if (valueChunkLen > 0)
                {
                    NodeData textChunk = new NodeData();
                    textChunk.lineInfo = valueChunkLineInfo;
                    textChunk.depth = attr.depth + 1;
                    textChunk.SetValueNode(XmlNodeType.Text, _stringBuilder.ToString(valueChunkStartPos, valueChunkLen));
                    AddAttributeChunkToList(attr, textChunk, ref lastChunk);
                }
            }

            _ps.charPos = pos + 1;

            attr.SetValue(_stringBuilder.ToString());
            _stringBuilder.Length = 0;
        }

        private Task<bool> ParseTextAsync()
        {
            int startPos;
            int endPos;
            int orChars = 0;

            // skip over the text if not in full parsing mode
            if (_parsingMode != ParsingMode.Full)
            {
                return _ParseTextAsync(null);
            }

            _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
            Debug.Assert(_stringBuilder.Length == 0);

            // the whole value is in buffer

            ValueTask<(int, int, int, bool)> parseTextTask = ParseTextAsync(orChars).Preserve();
            bool fullValue;
            if (!parseTextTask.IsCompletedSuccessfully)
            {
                return _ParseTextAsync(parseTextTask.AsTask());
            }
            else
            {
                var tuple_10 = parseTextTask.Result;
                startPos = tuple_10.Item1;
                endPos = tuple_10.Item2;
                orChars = tuple_10.Item3;
                fullValue = tuple_10.Item4;
            }

            if (fullValue)
            {
                if (endPos - startPos == 0)
                {
                    return ParseTextAsync_IgnoreNode();
                }
                XmlNodeType nodeType = GetTextNodeType(orChars);
                if (nodeType == XmlNodeType.None)
                {
                    return ParseTextAsync_IgnoreNode();
                }
                Debug.Assert(endPos - startPos > 0);
                _curNode.SetValueNode(nodeType, _ps.chars, startPos, endPos - startPos);
                return AsyncHelper.DoneTaskTrue;
            }
            // only piece of the value was returned
            else
            {
                return _ParseTextAsync(parseTextTask.AsTask());
            }
        }

        // Parses text or whitespace node.
        // Returns true if a node has been parsed and its data set to curNode.
        // Returns false when a whitespace has been parsed and ignored (according to current whitespace handling) or when parsing mode is not Full.
        // Also returns false if there is no text to be parsed.
        private async Task<bool> _ParseTextAsync(Task<(int, int, int, bool)>? parseTask)
        {
            int startPos;
            int endPos;
            int orChars = 0;

            if (parseTask != null)
                goto Parse;

            // skip over the text if not in full parsing mode
            if (_parsingMode != ParsingMode.Full)
            {
                (int, int, int, bool) tuple_9;
                do
                {
                    tuple_9 = await ParseTextAsync(orChars).ConfigureAwait(false);
                    orChars = tuple_9.Item3;
                } while (!tuple_9.Item4);

                goto IgnoredNode;
            }

            _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
            Debug.Assert(_stringBuilder.Length == 0);

            parseTask = ParseTextAsync(orChars).AsTask();

        Parse:
            var tuple_10 = await parseTask.ConfigureAwait(false);
            startPos = tuple_10.Item1;
            endPos = tuple_10.Item2;
            orChars = tuple_10.Item3;

            if (tuple_10.Item4)
            {
                if (endPos - startPos == 0)
                {
                    goto IgnoredNode;
                }
                XmlNodeType nodeType = GetTextNodeType(orChars);
                if (nodeType == XmlNodeType.None)
                {
                    goto IgnoredNode;
                }
                Debug.Assert(endPos - startPos > 0);
                _curNode.SetValueNode(nodeType, _ps.chars, startPos, endPos - startPos);
                return true;
            }
            // only piece of the value was returned
            else
            {
                // V1 compatibility mode -> cache the whole value
                if (_v1Compat)
                {
                    (int, int, int, bool) tuple_11;

                    do
                    {
                        if (endPos - startPos > 0)
                        {
                            _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                        }

                        tuple_11 = await ParseTextAsync(orChars).ConfigureAwait(false);
                        startPos = tuple_11.Item1;
                        endPos = tuple_11.Item2;
                        orChars = tuple_11.Item3;
                    } while (!tuple_11.Item4);

                    if (endPos - startPos > 0)
                    {
                        _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                    }

                    Debug.Assert(_stringBuilder.Length > 0);

                    XmlNodeType nodeType = GetTextNodeType(orChars);
                    if (nodeType == XmlNodeType.None)
                    {
                        _stringBuilder.Length = 0;
                        goto IgnoredNode;
                    }

                    _curNode.SetValueNode(nodeType, _stringBuilder.ToString());
                    _stringBuilder.Length = 0;
                    return true;
                }
                // V2 reader -> do not cache the whole value yet, read only up to 4kB to decide whether the value is a whitespace
                else
                {
                    bool fullValue;

                    // if it's a partial text value, not a whitespace -> return
                    if (orChars > 0x20)
                    {
                        Debug.Assert(endPos - startPos > 0);
                        _curNode.SetValueNode(XmlNodeType.Text, _ps.chars, startPos, endPos - startPos);
                        _nextParsingFunction = _parsingFunction;
                        _parsingFunction = ParsingFunction.PartialTextValue;
                        return true;
                    }

                    // partial whitespace -> read more data (up to 4kB) to decide if it is a whitespace or a text node
                    if (endPos - startPos > 0)
                    {
                        _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                    }
                    do
                    {
                        var tuple_12 = await ParseTextAsync(orChars).ConfigureAwait(false);
                        startPos = tuple_12.Item1;
                        endPos = tuple_12.Item2;
                        orChars = tuple_12.Item3;

                        fullValue = tuple_12.Item4;

                        if (endPos - startPos > 0)
                        {
                            _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                        }
                    } while (!fullValue && orChars <= 0x20 && _stringBuilder.Length < MinWhitespaceLookahedCount);

                    // determine the value node type
                    XmlNodeType nodeType = (_stringBuilder.Length < MinWhitespaceLookahedCount) ? GetTextNodeType(orChars) : XmlNodeType.Text;
                    if (nodeType == XmlNodeType.None)
                    {
                        // ignored whitespace -> skip over the rest of the value unless we already read it all
                        _stringBuilder.Length = 0;
                        if (!fullValue)
                        {
                            (int, int, int, bool) tuple_13;
                            do
                            {
                                tuple_13 = await ParseTextAsync(orChars).ConfigureAwait(false);
                                orChars = tuple_13.Item3;
                            } while (!tuple_13.Item4);
                        }
                        goto IgnoredNode;
                    }
                    // set value to curNode
                    _curNode.SetValueNode(nodeType, _stringBuilder.ToString());
                    _stringBuilder.Length = 0;

                    // change parsing state if the full value was not parsed
                    if (!fullValue)
                    {
                        _nextParsingFunction = _parsingFunction;
                        _parsingFunction = ParsingFunction.PartialTextValue;
                    }
                    return true;
                }
            }

        IgnoredNode:
            return await ParseTextAsync_IgnoreNode().ConfigureAwait(false);
        }

        private Task<bool> ParseTextAsync_IgnoreNode()
        {
            // Needed only for XmlTextReader (reporting of entities)

            // ignored whitespace at the end of manually resolved entity
            if (_parsingFunction == ParsingFunction.ReportEndEntity)
            {
                SetupEndEntityNodeInContent();
                _parsingFunction = _nextParsingFunction;
                return AsyncHelper.DoneTaskTrue;
            }
            else if (_parsingFunction == ParsingFunction.EntityReference)
            {
                _parsingFunction = _nextNextParsingFunction;
                return ParseEntityReferenceAsync().ReturnTrueTaskWhenFinishAsync();
            }
            return AsyncHelper.DoneTaskFalse;
        }

        // Parses a chunk of text starting at ps.charPos.
        //   startPos .... start position of the text chunk that has been parsed (can differ from ps.charPos before the call)
        //   endPos ...... end position of the text chunk that has been parsed (can differ from ps.charPos after the call)
        //   ourOrChars .. all parsed character bigger or equal to 0x20 or-ed (|) into a single int. It can be used for whitespace detection
        //                 (the text has a non-whitespace character if outOrChars > 0x20).
        // Returns true when the whole value has been parsed. Return false when it needs to be called again to get a next chunk of value.


        private readonly struct ParseTextState
        {
            public readonly int outOrChars;
            public readonly char[] chars;
            public readonly int pos;
            public readonly int rcount;
            public readonly int rpos;
            public readonly int orChars;
            public readonly char c;

            public ParseTextState(int outOrChars, char[] chars, int pos, int rcount, int rpos, int orChars, char c)
            {
                this.outOrChars = outOrChars;
                this.chars = chars;
                this.pos = pos;
                this.rcount = rcount;
                this.rpos = rpos;
                this.orChars = orChars;
                this.c = c;
            }
        }

        private enum ParseTextFunction
        {
            ParseText,
            Entity,
            Surrogate,
            ReadData,
            NoValue,
            PartialValue,
        }

        private ParseTextFunction _parseText_NextFunction;

        private ParseTextState _lastParseTextState;

        private readonly Task<(int, int, int, bool)> _parseText_dummyTask = Task.FromResult((0, 0, 0, false));

        //To avoid stackoverflow like ParseText->ParseEntity->ParText->..., use a loop and parsing function to implement such call.
        private ValueTask<(int, int, int, bool)> ParseTextAsync(int outOrChars)
        {
            Task<(int, int, int, bool)> task = ParseTextAsync(outOrChars, _ps.chars, _ps.charPos, 0, -1, outOrChars, (char)0);
            while (true)
            {
                if (!task.IsSuccess())
                {
                    return new ValueTask<(int, int, int, bool)>(ParseTextAsync_AsyncFunc(task));
                }

                outOrChars = _lastParseTextState.outOrChars;
                char[] chars = _lastParseTextState.chars;
                int pos = _lastParseTextState.pos;
                int rcount = _lastParseTextState.rcount;
                int rpos = _lastParseTextState.rpos;
                int orChars = _lastParseTextState.orChars;
                char c = _lastParseTextState.c;

                switch (_parseText_NextFunction)
                {
                    case ParseTextFunction.ParseText:
                        task = ParseTextAsync(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        break;
                    case ParseTextFunction.Entity:
                        task = ParseTextAsync_ParseEntity(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        break;
                    case ParseTextFunction.ReadData:
                        task = ParseTextAsync_ReadData(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        break;
                    case ParseTextFunction.Surrogate:
                        task = ParseTextAsync_Surrogate(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        break;
                    case ParseTextFunction.NoValue:
                        return new ValueTask<(int, int, int, bool)>(ParseText_NoValue(outOrChars, pos));
                    case ParseTextFunction.PartialValue:
                        return new ValueTask<(int, int, int, bool)>(ParseText_PartialValue(pos, rcount, rpos, orChars, c));
                }
            }
        }

        private async Task<(int, int, int, bool)> ParseTextAsync_AsyncFunc(Task<(int, int, int, bool)> task)
        {
            while (true)
            {
                await task.ConfigureAwait(false);

                int outOrChars = _lastParseTextState.outOrChars;
                char[] chars = _lastParseTextState.chars;
                int pos = _lastParseTextState.pos;
                int rcount = _lastParseTextState.rcount;
                int rpos = _lastParseTextState.rpos;
                int orChars = _lastParseTextState.orChars;
                char c = _lastParseTextState.c;

                switch (_parseText_NextFunction)
                {
                    case ParseTextFunction.ParseText:
                        task = ParseTextAsync(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        break;
                    case ParseTextFunction.Entity:
                        task = ParseTextAsync_ParseEntity(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        break;
                    case ParseTextFunction.ReadData:
                        task = ParseTextAsync_ReadData(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        break;
                    case ParseTextFunction.Surrogate:
                        task = ParseTextAsync_Surrogate(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        break;
                    case ParseTextFunction.NoValue:
                        return ParseText_NoValue(outOrChars, pos);
                    case ParseTextFunction.PartialValue:
                        return ParseText_PartialValue(pos, rcount, rpos, orChars, c);
                }
            }
        }

        private Task<(int, int, int, bool)> ParseTextAsync(int outOrChars, char[] chars, int pos, int rcount, int rpos, int orChars, char c)
        {
            while (true)
            {
                // parse text content
                while (XmlCharType.IsTextChar(c = chars[pos]))
                {
                    orChars |= (int)c;
                    pos++;
                }

                switch (c)
                {
                    case (char)0x9:
                        pos++;
                        continue;
                    // eol
                    case (char)0xA:
                        pos++;
                        OnNewLine(pos);
                        continue;
                    case (char)0xD:
                        if (chars[pos + 1] == (char)0xA)
                        {
                            if (!_ps.eolNormalized && _parsingMode == ParsingMode.Full)
                            {
                                if (pos - _ps.charPos > 0)
                                {
                                    if (rcount == 0)
                                    {
                                        rcount = 1;
                                        rpos = pos;
                                    }
                                    else
                                    {
                                        ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                        rpos = pos - rcount;
                                        rcount++;
                                    }
                                }
                                else
                                {
                                    _ps.charPos++;
                                }
                            }
                            pos += 2;
                        }
                        else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                        {
                            if (!_ps.eolNormalized)
                            {
                                chars[pos] = (char)0xA;             // EOL normalization of 0xD
                            }
                            pos++;
                        }
                        else
                        {
                            _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                            _parseText_NextFunction = ParseTextFunction.ReadData;
                            return _parseText_dummyTask;
                        }
                        OnNewLine(pos);
                        continue;
                    // some tag
                    case '<':
                        _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        _parseText_NextFunction = ParseTextFunction.PartialValue;
                        return _parseText_dummyTask;
                    // entity reference
                    case '&':
                        _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        _parseText_NextFunction = ParseTextFunction.Entity;
                        return _parseText_dummyTask;
                    case ']':
                        if (_ps.charsUsed - pos < 3 && !_ps.isEof)
                        {
                            _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                            _parseText_NextFunction = ParseTextFunction.ReadData;
                            return _parseText_dummyTask;
                        }
                        if (chars[pos + 1] == ']' && chars[pos + 2] == '>')
                        {
                            Throw(pos, SR.Xml_CDATAEndInText);
                        }
                        orChars |= ']';
                        pos++;
                        continue;
                    default:
                        // end of buffer
                        if (pos == _ps.charsUsed)
                        {
                            _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                            _parseText_NextFunction = ParseTextFunction.ReadData;
                            return _parseText_dummyTask;
                        }
                        // surrogate chars
                        else
                        {
                            _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                            _parseText_NextFunction = ParseTextFunction.Surrogate;
                            return _parseText_dummyTask;
                        }
                }
            }
        }

        private async Task<(int, int, int, bool)> ParseTextAsync_ParseEntity(int outOrChars, char[] chars, int pos, int rcount, int rpos, int orChars, char c)
        {
            // try to parse char entity inline
            int charRefEndPos, charCount;
            EntityType entityType;
            if ((charRefEndPos = ParseCharRefInline(pos, out charCount, out entityType)) > 0)
            {
                if (rcount > 0)
                {
                    ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                }
                rpos = pos - rcount;
                rcount += (charRefEndPos - pos - charCount);
                pos = charRefEndPos;

                if (!XmlCharType.IsWhiteSpace(chars[charRefEndPos - charCount]) ||
                     (_v1Compat && entityType == EntityType.CharacterDec))
                {
                    orChars |= 0xFF;
                }
            }
            else
            {
                if (pos > _ps.charPos)
                {
                    _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                    _parseText_NextFunction = ParseTextFunction.PartialValue;
                    return _parseText_dummyTask.Result;
                }

                var tuple_14 = await HandleEntityReferenceAsync(false, EntityExpandType.All).ConfigureAwait(false);
                pos = tuple_14.Item1;

                switch (tuple_14.Item2)
                {
                    // Needed only for XmlTextReader (reporting of entities)
                    case EntityType.Unexpanded:
                        // make sure we will report EntityReference after the text node
                        _nextParsingFunction = _parsingFunction;
                        _parsingFunction = ParsingFunction.EntityReference;
                        // end the value (returns nothing)
                        _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        _parseText_NextFunction = ParseTextFunction.NoValue;
                        return _parseText_dummyTask.Result;
                    case EntityType.CharacterDec:
                        if (!_v1Compat)
                        {
                            goto case EntityType.CharacterHex;
                        }
                        orChars |= 0xFF;
                        break;
                    case EntityType.CharacterHex:
                    case EntityType.CharacterNamed:
                        if (!XmlCharType.IsWhiteSpace(_ps.chars[pos - 1]))
                        {
                            orChars |= 0xFF;
                        }
                        break;
                    default:
                        pos = _ps.charPos;
                        break;
                }
                chars = _ps.chars;
            }

            _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
            _parseText_NextFunction = ParseTextFunction.ParseText;
            return _parseText_dummyTask.Result;
        }

        private async Task<(int, int, int, bool)> ParseTextAsync_Surrogate(int outOrChars, char[] chars, int pos, int rcount, int rpos, int orChars, char c)
        {
            char ch = chars[pos];
            if (XmlCharType.IsHighSurrogate(ch))
            {
                if (pos + 1 == _ps.charsUsed)
                {
                    _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                    _parseText_NextFunction = ParseTextFunction.ReadData;
                    return _parseText_dummyTask.Result;
                }
                pos++;
                if (XmlCharType.IsLowSurrogate(chars[pos]))
                {
                    pos++;
                    orChars |= ch;
                    _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                    _parseText_NextFunction = ParseTextFunction.ParseText;
                    return _parseText_dummyTask.Result;
                }
            }
            int offset = pos - _ps.charPos;
            if (await ZeroEndingStreamAsync(pos).ConfigureAwait(false))
            {
                chars = _ps.chars;
                pos = _ps.charPos + offset;
                _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                _parseText_NextFunction = ParseTextFunction.PartialValue;
                return _parseText_dummyTask.Result;
            }
            else
            {
                ThrowInvalidChar(_ps.chars, _ps.charsUsed, _ps.charPos + offset);
            }
            //should never hit here
            throw new XmlException(SR.Xml_InternalError);
        }

        private async Task<(int, int, int, bool)> ParseTextAsync_ReadData(int outOrChars, char[] chars, int pos, int rcount, int rpos, int orChars, char c)
        {
            if (pos > _ps.charPos)
            {
                _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                _parseText_NextFunction = ParseTextFunction.PartialValue;
                return _parseText_dummyTask.Result;
            }
            // read new characters into the buffer
            if (await ReadDataAsync().ConfigureAwait(false) == 0)
            {
                if (_ps.charsUsed - _ps.charPos > 0)
                {
                    if (_ps.chars[_ps.charPos] != (char)0xD && _ps.chars[_ps.charPos] != ']')
                    {
                        Throw(SR.Xml_UnexpectedEOF1);
                    }
                    Debug.Assert(_ps.isEof);
                }
                else
                {
                    if (!InEntity)
                    {
                        // end the value (returns nothing)
                        _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        _parseText_NextFunction = ParseTextFunction.NoValue;
                        return _parseText_dummyTask.Result;
                    }

                    if (HandleEntityEnd(true))
                    {
                        // report EndEntity after the text node
                        _nextParsingFunction = _parsingFunction;
                        _parsingFunction = ParsingFunction.ReportEndEntity;
                        // end the value (returns nothing)
                        _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
                        _parseText_NextFunction = ParseTextFunction.NoValue;
                        return _parseText_dummyTask.Result;
                    }
                }
            }
            pos = _ps.charPos;
            chars = _ps.chars;
            _lastParseTextState = new ParseTextState(outOrChars, chars, pos, rcount, rpos, orChars, c);
            _parseText_NextFunction = ParseTextFunction.ParseText;
            return _parseText_dummyTask.Result;
        }

        private static (int, int, int, bool) ParseText_NoValue(int outOrChars, int pos)
        {
            return (pos, pos, outOrChars, true);
        }

        private (int, int, int, bool) ParseText_PartialValue(int pos, int rcount, int rpos, int orChars, char c)
        {
            if (_parsingMode == ParsingMode.Full && rcount > 0)
            {
                ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
            }
            int startPos = _ps.charPos;
            int endPos = pos - rcount;
            _ps.charPos = pos;
            int outOrChars = orChars;

            return (startPos, endPos, outOrChars, c == '<');
        }


        // When in ParsingState.PartialTextValue, this method parses and caches the rest of the value and stores it in curNode.
        private async Task FinishPartialValueAsync()
        {
            Debug.Assert(_stringBuilder.Length == 0);
            Debug.Assert(_parsingFunction == ParsingFunction.PartialTextValue ||
                          (_parsingFunction == ParsingFunction.InReadValueChunk && _incReadState == IncrementalReadState.ReadValueChunk_OnPartialValue));

            _curNode.CopyTo(_readValueOffset, _stringBuilder);

            int startPos;
            int endPos;
            int orChars = 0;

            var tuple_15 = await ParseTextAsync(orChars).ConfigureAwait(false);
            startPos = tuple_15.Item1;
            endPos = tuple_15.Item2;
            orChars = tuple_15.Item3;

            while (!tuple_15.Item4)
            {
                _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);

                tuple_15 = await ParseTextAsync(orChars).ConfigureAwait(false);
                startPos = tuple_15.Item1;
                endPos = tuple_15.Item2;
                orChars = tuple_15.Item3;
            }
            _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);

            Debug.Assert(_stringBuilder.Length > 0);
            _curNode.SetValue(_stringBuilder.ToString());
            _stringBuilder.Length = 0;
        }

        private async Task FinishOtherValueIteratorAsync()
        {
            switch (_parsingFunction)
            {
                case ParsingFunction.InReadAttributeValue:
                    // do nothing, correct value is already in curNode
                    break;
                case ParsingFunction.InReadValueChunk:
                    if (_incReadState == IncrementalReadState.ReadValueChunk_OnPartialValue)
                    {
                        await FinishPartialValueAsync().ConfigureAwait(false);
                        _incReadState = IncrementalReadState.ReadValueChunk_OnCachedValue;
                    }
                    else
                    {
                        if (_readValueOffset > 0)
                        {
                            _curNode.SetValue(_curNode.StringValue.Substring(_readValueOffset));
                            _readValueOffset = 0;
                        }
                    }
                    break;
                case ParsingFunction.InReadContentAsBinary:
                case ParsingFunction.InReadElementContentAsBinary:
                    switch (_incReadState)
                    {
                        case IncrementalReadState.ReadContentAsBinary_OnPartialValue:
                            await FinishPartialValueAsync().ConfigureAwait(false);
                            _incReadState = IncrementalReadState.ReadContentAsBinary_OnCachedValue;
                            break;
                        case IncrementalReadState.ReadContentAsBinary_OnCachedValue:
                            if (_readValueOffset > 0)
                            {
                                _curNode.SetValue(_curNode.StringValue.Substring(_readValueOffset));
                                _readValueOffset = 0;
                            }
                            break;
                        case IncrementalReadState.ReadContentAsBinary_End:
                            _curNode.SetValue(string.Empty);
                            break;
                    }
                    break;
            }
        }

        // When in ParsingState.PartialTextValue, this method skips over the rest of the partial value.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private async Task SkipPartialTextValueAsync()
        {
            Debug.Assert(_parsingFunction == ParsingFunction.PartialTextValue || _parsingFunction == ParsingFunction.InReadValueChunk ||
                          _parsingFunction == ParsingFunction.InReadContentAsBinary || _parsingFunction == ParsingFunction.InReadElementContentAsBinary);
            int orChars = 0;

            _parsingFunction = _nextParsingFunction;

            (int, int, int, bool) tuple_16;
            do
            {
                tuple_16 = await ParseTextAsync(orChars).ConfigureAwait(false);
                orChars = tuple_16.Item3;
            } while (!tuple_16.Item4);
        }

        private Task FinishReadValueChunkAsync()
        {
            Debug.Assert(_parsingFunction == ParsingFunction.InReadValueChunk);

            _readValueOffset = 0;
            if (_incReadState == IncrementalReadState.ReadValueChunk_OnPartialValue)
            {
                Debug.Assert((_index > 0) ? _nextParsingFunction == ParsingFunction.ElementContent : _nextParsingFunction == ParsingFunction.DocumentContent);
                return SkipPartialTextValueAsync();
            }
            else
            {
                _parsingFunction = _nextParsingFunction;
                _nextParsingFunction = _nextNextParsingFunction;

                return Task.CompletedTask;
            }
        }

        private async Task FinishReadContentAsBinaryAsync()
        {
            Debug.Assert(_parsingFunction == ParsingFunction.InReadContentAsBinary || _parsingFunction == ParsingFunction.InReadElementContentAsBinary);

            _readValueOffset = 0;
            if (_incReadState == IncrementalReadState.ReadContentAsBinary_OnPartialValue)
            {
                Debug.Assert((_index > 0) ? _nextParsingFunction == ParsingFunction.ElementContent : _nextParsingFunction == ParsingFunction.DocumentContent);
                await SkipPartialTextValueAsync().ConfigureAwait(false);
            }
            else
            {
                _parsingFunction = _nextParsingFunction;
                _nextParsingFunction = _nextNextParsingFunction;
            }
            if (_incReadState != IncrementalReadState.ReadContentAsBinary_End)
            {
                while (await MoveToNextContentNodeAsync(true).ConfigureAwait(false)) ;
            }
        }

        private async Task FinishReadElementContentAsBinaryAsync()
        {
            await FinishReadContentAsBinaryAsync().ConfigureAwait(false);

            if (_curNode.type != XmlNodeType.EndElement)
            {
                Throw(SR.Xml_InvalidNodeType, _curNode.type.ToString());
            }
            // move off the end element
            await _outerReader.ReadAsync().ConfigureAwait(false);
        }

        private async Task<bool> ParseRootLevelWhitespaceAsync()
        {
            Debug.Assert(_stringBuilder.Length == 0);

            XmlNodeType nodeType = GetWhitespaceType();

            if (nodeType == XmlNodeType.None)
            {
                await EatWhitespacesAsync(null).ConfigureAwait(false);
                if (_ps.chars[_ps.charPos] == '<' || _ps.charsUsed - _ps.charPos == 0 || await ZeroEndingStreamAsync(_ps.charPos).ConfigureAwait(false))
                {
                    return false;
                }
            }
            else
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
                await EatWhitespacesAsync(_stringBuilder).ConfigureAwait(false);
                if (_ps.chars[_ps.charPos] == '<' || _ps.charsUsed - _ps.charPos == 0 || await ZeroEndingStreamAsync(_ps.charPos).ConfigureAwait(false))
                {
                    if (_stringBuilder.Length > 0)
                    {
                        _curNode.SetValueNode(nodeType, _stringBuilder.ToString());
                        _stringBuilder.Length = 0;
                        return true;
                    }
                    return false;
                }
            }

            if (XmlCharType.IsCharData(_ps.chars[_ps.charPos]))
            {
                Throw(SR.Xml_InvalidRootData);
            }
            else
            {
                ThrowInvalidChar(_ps.chars, _ps.charsUsed, _ps.charPos);
            }
            return false;
        }

        private async Task ParseEntityReferenceAsync()
        {
            Debug.Assert(_ps.chars[_ps.charPos] == '&');
            _ps.charPos++;

            _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
            _curNode.SetNamedNode(XmlNodeType.EntityReference, await ParseEntityNameAsync().ConfigureAwait(false));
        }

        private async Task<(int, EntityType)> HandleEntityReferenceAsync(bool isInAttributeValue, EntityExpandType expandType)
        {
            int charRefEndPos;

            Debug.Assert(_ps.chars[_ps.charPos] == '&');

            if (_ps.charPos + 1 == _ps.charsUsed)
            {
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(SR.Xml_UnexpectedEOF1);
                }
            }

            // numeric characters reference
            if (_ps.chars[_ps.charPos + 1] == '#')
            {
                EntityType entityType;

                var tuple_17 = await ParseNumericCharRefAsync(expandType != EntityExpandType.OnlyGeneral, null).ConfigureAwait(false);
                entityType = tuple_17.Item1;

                charRefEndPos = tuple_17.Item2;

                Debug.Assert(entityType == EntityType.CharacterDec || entityType == EntityType.CharacterHex);

                return (charRefEndPos, entityType);
            }
            // named reference
            else
            {
                // named character reference
                charRefEndPos = await ParseNamedCharRefAsync(expandType != EntityExpandType.OnlyGeneral, null).ConfigureAwait(false);
                if (charRefEndPos >= 0)
                {
                    return (charRefEndPos, EntityType.CharacterNamed);
                }

                // general entity reference
                // Needed only for XmlTextReader (reporting of entities)
                // NOTE: XmlValidatingReader compatibility mode: expand all entities in attribute values
                // general entity reference
                if (expandType == EntityExpandType.OnlyCharacter ||
                     (_entityHandling != EntityHandling.ExpandEntities &&
                       (!isInAttributeValue || !_validatingReaderCompatFlag)))
                {
                    return (charRefEndPos, EntityType.Unexpanded);
                }
                int endPos;

                _ps.charPos++;
                int savedLinePos = _ps.LinePos;
                try
                {
                    endPos = await ParseNameAsync().ConfigureAwait(false);
                }
                catch (XmlException)
                {
                    Throw(SR.Xml_ErrorParsingEntityName, _ps.LineNo, savedLinePos);

                    return (charRefEndPos, EntityType.Skipped);
                }

                // check ';'
                if (_ps.chars[endPos] != ';')
                {
                    ThrowUnexpectedToken(endPos, ";");
                }

                int entityLinePos = _ps.LinePos;
                string entityName = _nameTable.Add(_ps.chars, _ps.charPos, endPos - _ps.charPos);
                _ps.charPos = endPos + 1;
                charRefEndPos = -1;

                EntityType entType = await HandleGeneralEntityReferenceAsync(entityName, isInAttributeValue, false, entityLinePos).ConfigureAwait(false);
                _reportedBaseUri = _ps.baseUriStr;
                _reportedEncoding = _ps.encoding;

                return (charRefEndPos, entType);
            }
        }

        // returns true == continue parsing
        // return false == unexpanded external entity, stop parsing and return
        private async Task<EntityType> HandleGeneralEntityReferenceAsync(string name, bool isInAttributeValue, bool pushFakeEntityIfNullResolver, int entityStartLinePos)
        {
            IDtdEntityInfo? entity = null;

            if (_dtdInfo == null && _fragmentParserContext != null && _fragmentParserContext.HasDtdInfo && _dtdProcessing == DtdProcessing.Parse)
            {
                await ParseDtdFromParserContextAsync().ConfigureAwait(false);
            }

            if (_dtdInfo == null ||
                 ((entity = _dtdInfo.LookupEntity(name)) == null))
            {
                // Needed only for XmlTextReader (when used from XmlDocument)
                if (_disableUndeclaredEntityCheck)
                {
                    SchemaEntity schemaEntity = new SchemaEntity(new XmlQualifiedName(name), false);
                    schemaEntity.Text = string.Empty;
                    entity = schemaEntity;
                }
                else
                    Throw(SR.Xml_UndeclaredEntity, name, _ps.LineNo, entityStartLinePos);
            }

            if (entity.IsUnparsedEntity)
            {
                // Needed only for XmlTextReader (when used from XmlDocument)
                if (_disableUndeclaredEntityCheck)
                {
                    SchemaEntity schemaEntity = new SchemaEntity(new XmlQualifiedName(name), false);
                    schemaEntity.Text = string.Empty;
                    entity = schemaEntity;
                }
                else
                    Throw(SR.Xml_UnparsedEntityRef, name, _ps.LineNo, entityStartLinePos);
            }

            if (_standalone && entity.IsDeclaredInExternal)
            {
                Throw(SR.Xml_ExternalEntityInStandAloneDocument, entity.Name, _ps.LineNo, entityStartLinePos);
            }

            if (entity.IsExternal)
            {
                if (isInAttributeValue)
                {
                    Throw(SR.Xml_ExternalEntityInAttValue, name, _ps.LineNo, entityStartLinePos);
                    return EntityType.Skipped;
                }

                if (_parsingMode == ParsingMode.SkipContent)
                {
                    return EntityType.Skipped;
                }

                if (IsResolverNull)
                {
                    if (pushFakeEntityIfNullResolver)
                    {
                        await PushExternalEntityAsync(entity).ConfigureAwait(false);
                        _curNode.entityId = _ps.entityId;
                        return EntityType.FakeExpanded;
                    }
                    return EntityType.Skipped;
                }
                else
                {
                    await PushExternalEntityAsync(entity).ConfigureAwait(false);
                    _curNode.entityId = _ps.entityId;
                    return (isInAttributeValue && _validatingReaderCompatFlag) ? EntityType.ExpandedInAttribute : EntityType.Expanded;
                }
            }
            else
            {
                if (_parsingMode == ParsingMode.SkipContent)
                {
                    return EntityType.Skipped;
                }

                PushInternalEntity(entity);

                _curNode.entityId = _ps.entityId;
                return (isInAttributeValue && _validatingReaderCompatFlag) ? EntityType.ExpandedInAttribute : EntityType.Expanded;
            }
        }

        private Task<bool> ParsePIAsync()
        {
            return ParsePIAsync(null);
        }

        // Parses processing instruction; if piInDtdStringBuilder != null, the processing instruction is in DTD and
        // it will be saved in the passed string builder (target, whitespace & value).
        private async Task<bool> ParsePIAsync(StringBuilder? piInDtdStringBuilder)
        {
            if (_parsingMode == ParsingMode.Full)
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
            }

            Debug.Assert(_stringBuilder.Length == 0);

            // parse target name
            int nameEndPos = await ParseNameAsync().ConfigureAwait(false);
            string target = _nameTable.Add(_ps.chars, _ps.charPos, nameEndPos - _ps.charPos);

            if (string.Equals(target, "xml", StringComparison.OrdinalIgnoreCase))
            {
                Throw(target.Equals("xml") ? SR.Xml_XmlDeclNotFirst : SR.Xml_InvalidPIName, target);
            }
            _ps.charPos = nameEndPos;

            if (piInDtdStringBuilder == null)
            {
                if (!_ignorePIs && _parsingMode == ParsingMode.Full)
                {
                    _curNode.SetNamedNode(XmlNodeType.ProcessingInstruction, target);
                }
            }
            else
            {
                piInDtdStringBuilder.Append(target);
            }

            // check mandatory whitespace
            char ch = _ps.chars[_ps.charPos];
            Debug.Assert(_ps.charPos < _ps.charsUsed);
            if (await EatWhitespacesAsync(piInDtdStringBuilder).ConfigureAwait(false) == 0)
            {
                if (_ps.charsUsed - _ps.charPos < 2)
                {
                    await ReadDataAsync().ConfigureAwait(false);
                }
                if (ch != '?' || _ps.chars[_ps.charPos + 1] != '>')
                {
                    Throw(SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(_ps.chars, _ps.charsUsed, _ps.charPos));
                }
            }

            // scan processing instruction value
            int startPos, endPos;

            var tuple_18 = await ParsePIValueAsync().ConfigureAwait(false);
            startPos = tuple_18.Item1;
            endPos = tuple_18.Item2;

            if (tuple_18.Item3)
            {
                if (piInDtdStringBuilder == null)
                {
                    if (_ignorePIs)
                    {
                        return false;
                    }
                    if (_parsingMode == ParsingMode.Full)
                    {
                        _curNode.SetValue(_ps.chars, startPos, endPos - startPos);
                    }
                }
                else
                {
                    piInDtdStringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                }
            }
            else
            {
                StringBuilder sb;
                if (piInDtdStringBuilder == null)
                {
                    if (_ignorePIs || _parsingMode != ParsingMode.Full)
                    {
                        (int, int, bool) tuple_19;
                        do
                        {
                            tuple_19 = await ParsePIValueAsync().ConfigureAwait(false);
                        } while (!tuple_19.Item3);

                        return false;
                    }
                    sb = _stringBuilder;
                    Debug.Assert(_stringBuilder.Length == 0);
                }
                else
                {
                    sb = piInDtdStringBuilder;
                }

                (int, int, bool) tuple_20;

                do
                {
                    sb.Append(_ps.chars, startPos, endPos - startPos);

                    tuple_20 = await ParsePIValueAsync().ConfigureAwait(false);
                    startPos = tuple_20.Item1;
                    endPos = tuple_20.Item2;
                } while (!tuple_20.Item3);

                sb.Append(_ps.chars, startPos, endPos - startPos);

                if (piInDtdStringBuilder == null)
                {
                    _curNode.SetValue(_stringBuilder.ToString());
                    _stringBuilder.Length = 0;
                }
            }
            return true;
        }

        private async Task<(int, int, bool)> ParsePIValueAsync()
        {
            int outStartPos;
            int outEndPos;

            // read new characters into the buffer
            if (_ps.charsUsed - _ps.charPos < 2)
            {
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(_ps.charsUsed, SR.Xml_UnexpectedEOF, "PI");
                }
            }

            int pos = _ps.charPos;
            char[] chars = _ps.chars;
            int rcount = 0;
            int rpos = -1;

            while (true)
            {
                char tmpch;

                while (XmlCharType.IsTextChar(tmpch = chars[pos]) && tmpch != '?')
                {
                    pos++;
                }

                switch (chars[pos])
                {
                    // possibly end of PI
                    case '?':
                        if (chars[pos + 1] == '>')
                        {
                            if (rcount > 0)
                            {
                                Debug.Assert(!_ps.eolNormalized);
                                ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                outEndPos = pos - rcount;
                            }
                            else
                            {
                                outEndPos = pos;
                            }
                            outStartPos = _ps.charPos;
                            _ps.charPos = pos + 2;

                            return (outStartPos, outEndPos, true);
                        }
                        else if (pos + 1 == _ps.charsUsed)
                        {
                            goto ReturnPartial;
                        }
                        else
                        {
                            pos++;
                            continue;
                        }
                    // eol
                    case (char)0xA:
                        pos++;
                        OnNewLine(pos);
                        continue;
                    case (char)0xD:
                        if (chars[pos + 1] == (char)0xA)
                        {
                            if (!_ps.eolNormalized && _parsingMode == ParsingMode.Full)
                            {
                                // EOL normalization of 0xD 0xA
                                if (pos - _ps.charPos > 0)
                                {
                                    if (rcount == 0)
                                    {
                                        rcount = 1;
                                        rpos = pos;
                                    }
                                    else
                                    {
                                        ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                        rpos = pos - rcount;
                                        rcount++;
                                    }
                                }
                                else
                                {
                                    _ps.charPos++;
                                }
                            }
                            pos += 2;
                        }
                        else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                        {
                            if (!_ps.eolNormalized)
                            {
                                chars[pos] = (char)0xA;             // EOL normalization of 0xD
                            }
                            pos++;
                        }
                        else
                        {
                            goto ReturnPartial;
                        }
                        OnNewLine(pos);
                        continue;
                    case '<':
                    case '&':
                    case ']':
                    case (char)0x9:
                        pos++;
                        continue;
                    default:
                        // end of buffer
                        if (pos == _ps.charsUsed)
                        {
                            goto ReturnPartial;
                        }
                        // surrogate characters
                        else
                        {
                            char ch = chars[pos];
                            if (XmlCharType.IsHighSurrogate(ch))
                            {
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    goto ReturnPartial;
                                }
                                pos++;
                                if (XmlCharType.IsLowSurrogate(chars[pos]))
                                {
                                    pos++;
                                    continue;
                                }
                            }
                            ThrowInvalidChar(chars, _ps.charsUsed, pos);
                            break;
                        }
                }
            }

        ReturnPartial:
            if (rcount > 0)
            {
                ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                outEndPos = pos - rcount;
            }
            else
            {
                outEndPos = pos;
            }
            outStartPos = _ps.charPos;
            _ps.charPos = pos;

            return (outStartPos, outEndPos, false);
        }

        private async Task<bool> ParseCommentAsync()
        {
            if (_ignoreComments)
            {
                ParsingMode oldParsingMode = _parsingMode;
                _parsingMode = ParsingMode.SkipNode;
                await ParseCDataOrCommentAsync(XmlNodeType.Comment).ConfigureAwait(false);
                _parsingMode = oldParsingMode;
                return false;
            }
            else
            {
                await ParseCDataOrCommentAsync(XmlNodeType.Comment).ConfigureAwait(false);
                return true;
            }
        }

        private Task ParseCDataAsync()
        {
            return ParseCDataOrCommentAsync(XmlNodeType.CDATA);
        }

        // Parses CDATA section or comment
        private async Task ParseCDataOrCommentAsync(XmlNodeType type)
        {
            int startPos, endPos;

            if (_parsingMode == ParsingMode.Full)
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);
                Debug.Assert(_stringBuilder.Length == 0);

                var tuple_21 = await ParseCDataOrCommentTupleAsync(type).ConfigureAwait(false);
                startPos = tuple_21.Item1;
                endPos = tuple_21.Item2;

                if (tuple_21.Item3)
                {
                    _curNode.SetValueNode(type, _ps.chars, startPos, endPos - startPos);
                }
                else
                {
                    (int, int, bool) tuple_22;

                    do
                    {
                        _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);

                        tuple_22 = await ParseCDataOrCommentTupleAsync(type).ConfigureAwait(false);
                        startPos = tuple_22.Item1;
                        endPos = tuple_22.Item2;
                    } while (!tuple_22.Item3);

                    _stringBuilder.Append(_ps.chars, startPos, endPos - startPos);
                    _curNode.SetValueNode(type, _stringBuilder.ToString());
                    _stringBuilder.Length = 0;
                }
            }
            else
            {
                (int, int, bool) tuple_23;
                do
                {
                    tuple_23 = await ParseCDataOrCommentTupleAsync(type).ConfigureAwait(false);
                } while (!tuple_23.Item3);
            }
        }

        // Parses a chunk of CDATA section or comment. Returns true when the end of CDATA or comment was reached.

        private async Task<(int, int, bool)> ParseCDataOrCommentTupleAsync(XmlNodeType type)
        {
            int outStartPos;
            int outEndPos;

            if (_ps.charsUsed - _ps.charPos < 3)
            {
                // read new characters into the buffer
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(SR.Xml_UnexpectedEOF, (type == XmlNodeType.Comment) ? "Comment" : "CDATA");
                }
            }

            int pos = _ps.charPos;
            char[] chars = _ps.chars;
            int rcount = 0;
            int rpos = -1;
            char stopChar = (type == XmlNodeType.Comment) ? '-' : ']';

            while (true)
            {
                char tmpch;
                while (XmlCharType.IsTextChar(tmpch = chars[pos]) && tmpch != stopChar)
                {
                    pos++;
                }

                // posibbly end of comment or cdata section
                if (chars[pos] == stopChar)
                {
                    if (chars[pos + 1] == stopChar)
                    {
                        if (chars[pos + 2] == '>')
                        {
                            if (rcount > 0)
                            {
                                Debug.Assert(!_ps.eolNormalized);
                                ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                outEndPos = pos - rcount;
                            }
                            else
                            {
                                outEndPos = pos;
                            }
                            outStartPos = _ps.charPos;
                            _ps.charPos = pos + 3;

                            return (outStartPos, outEndPos, true);
                        }
                        else if (pos + 2 == _ps.charsUsed)
                        {
                            goto ReturnPartial;
                        }
                        else if (type == XmlNodeType.Comment)
                        {
                            Throw(pos, SR.Xml_InvalidCommentChars);
                        }
                    }
                    else if (pos + 1 == _ps.charsUsed)
                    {
                        goto ReturnPartial;
                    }
                    pos++;
                    continue;
                }
                else
                {
                    switch (chars[pos])
                    {
                        // eol
                        case (char)0xA:
                            pos++;
                            OnNewLine(pos);
                            continue;
                        case (char)0xD:
                            if (chars[pos + 1] == (char)0xA)
                            {
                                // EOL normalization of 0xD 0xA - shift the buffer
                                if (!_ps.eolNormalized && _parsingMode == ParsingMode.Full)
                                {
                                    if (pos - _ps.charPos > 0)
                                    {
                                        if (rcount == 0)
                                        {
                                            rcount = 1;
                                            rpos = pos;
                                        }
                                        else
                                        {
                                            ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                                            rpos = pos - rcount;
                                            rcount++;
                                        }
                                    }
                                    else
                                    {
                                        _ps.charPos++;
                                    }
                                }
                                pos += 2;
                            }
                            else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                            {
                                if (!_ps.eolNormalized)
                                {
                                    chars[pos] = (char)0xA;             // EOL normalization of 0xD
                                }
                                pos++;
                            }
                            else
                            {
                                goto ReturnPartial;
                            }
                            OnNewLine(pos);
                            continue;
                        case '<':
                        case '&':
                        case ']':
                        case (char)0x9:
                            pos++;
                            continue;
                        default:
                            // end of buffer
                            if (pos == _ps.charsUsed)
                            {
                                goto ReturnPartial;
                            }
                            // surrogate characters
                            char ch = chars[pos];
                            if (XmlCharType.IsHighSurrogate(ch))
                            {
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    goto ReturnPartial;
                                }
                                pos++;
                                if (XmlCharType.IsLowSurrogate(chars[pos]))
                                {
                                    pos++;
                                    continue;
                                }
                            }
                            ThrowInvalidChar(chars, _ps.charsUsed, pos);
                            break;
                    }
                }

            ReturnPartial:
                if (rcount > 0)
                {
                    ShiftBuffer(rpos + rcount, rpos, pos - rpos - rcount);
                    outEndPos = pos - rcount;
                }
                else
                {
                    outEndPos = pos;
                }
                outStartPos = _ps.charPos;

                _ps.charPos = pos;

                return (outStartPos, outEndPos, false);
            }
        }

        // Parses DOCTYPE declaration
        private async Task<bool> ParseDoctypeDeclAsync()
        {
            if (_dtdProcessing == DtdProcessing.Prohibit)
            {
                ThrowWithoutLineInfo(_v1Compat ? SR.Xml_DtdIsProhibited : SR.Xml_DtdIsProhibitedEx);
            }

            // parse 'DOCTYPE'
            while (_ps.charsUsed - _ps.charPos < 8)
            {
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    Throw(SR.Xml_UnexpectedEOF, "DOCTYPE");
                }
            }
            if (!XmlConvert.StrEqual(_ps.chars, _ps.charPos, 7, "DOCTYPE"))
            {
                ThrowUnexpectedToken((!_rootElementParsed && _dtdInfo == null) ? "DOCTYPE" : "<!--");
            }
            if (!XmlCharType.IsWhiteSpace(_ps.chars[_ps.charPos + 7]))
            {
                ThrowExpectingWhitespace(_ps.charPos + 7);
            }

            if (_dtdInfo != null)
            {
                Throw(_ps.charPos - 2, SR.Xml_MultipleDTDsProvided);  // position just before <!DOCTYPE
            }
            if (_rootElementParsed)
            {
                Throw(_ps.charPos - 2, SR.Xml_DtdAfterRootElement);
            }

            _ps.charPos += 8;

            await EatWhitespacesAsync(null).ConfigureAwait(false);

            // Parse DTD
            if (_dtdProcessing == DtdProcessing.Parse)
            {
                _curNode.SetLineInfo(_ps.LineNo, _ps.LinePos);

                await ParseDtdAsync().ConfigureAwait(false);

                _nextParsingFunction = _parsingFunction;
                _parsingFunction = ParsingFunction.ResetAttributesRootLevel;
                return true;
            }
            // Skip DTD
            else
            {
                Debug.Assert(_dtdProcessing == DtdProcessing.Ignore);

                await SkipDtdAsync().ConfigureAwait(false);
                return false;
            }
        }

        private async Task ParseDtdAsync()
        {
            IDtdParser dtdParser = DtdParser.Create();

            _dtdInfo = await dtdParser.ParseInternalDtdAsync(new DtdParserProxy(this), true).ConfigureAwait(false);

            if ((_validatingReaderCompatFlag || !_v1Compat) && (_dtdInfo.HasDefaultAttributes || _dtdInfo.HasNonCDataAttributes))
            {
                _addDefaultAttributesAndNormalize = true;
            }

            _curNode.SetNamedNode(XmlNodeType.DocumentType, _dtdInfo.Name.ToString(), string.Empty, null);
            _curNode.SetValue(_dtdInfo.InternalDtdSubset);
        }

        private async Task SkipDtdAsync()
        {

            // parse dtd name

            var tuple_24 = await ParseQNameAsync().ConfigureAwait(false);

            int pos = tuple_24.Item2;

            _ps.charPos = pos;

            // check whitespace
            await EatWhitespacesAsync(null).ConfigureAwait(false);

            // PUBLIC Id
            if (_ps.chars[_ps.charPos] == 'P')
            {
                // make sure we have enough characters
                while (_ps.charsUsed - _ps.charPos < 6)
                {
                    if (await ReadDataAsync().ConfigureAwait(false) == 0)
                    {
                        Throw(SR.Xml_UnexpectedEOF1);
                    }
                }
                // check 'PUBLIC'
                if (!XmlConvert.StrEqual(_ps.chars, _ps.charPos, 6, "PUBLIC"))
                {
                    ThrowUnexpectedToken("PUBLIC");
                }
                _ps.charPos += 6;

                // check whitespace
                if (await EatWhitespacesAsync(null).ConfigureAwait(false) == 0)
                {
                    ThrowExpectingWhitespace(_ps.charPos);
                }

                // parse PUBLIC value
                await SkipPublicOrSystemIdLiteralAsync().ConfigureAwait(false);

                // check whitespace
                if (await EatWhitespacesAsync(null).ConfigureAwait(false) == 0)
                {
                    ThrowExpectingWhitespace(_ps.charPos);
                }

                // parse SYSTEM value
                await SkipPublicOrSystemIdLiteralAsync().ConfigureAwait(false);

                await EatWhitespacesAsync(null).ConfigureAwait(false);
            }
            else if (_ps.chars[_ps.charPos] == 'S')
            {
                // make sure we have enough characters
                while (_ps.charsUsed - _ps.charPos < 6)
                {
                    if (await ReadDataAsync().ConfigureAwait(false) == 0)
                    {
                        Throw(SR.Xml_UnexpectedEOF1);
                    }
                }
                // check 'SYSTEM'
                if (!XmlConvert.StrEqual(_ps.chars, _ps.charPos, 6, "SYSTEM"))
                {
                    ThrowUnexpectedToken("SYSTEM");
                }
                _ps.charPos += 6;

                // check whitespace
                if (await EatWhitespacesAsync(null).ConfigureAwait(false) == 0)
                {
                    ThrowExpectingWhitespace(_ps.charPos);
                }

                // parse SYSTEM value
                await SkipPublicOrSystemIdLiteralAsync().ConfigureAwait(false);

                await EatWhitespacesAsync(null).ConfigureAwait(false);
            }
            else if (_ps.chars[_ps.charPos] != '[' && _ps.chars[_ps.charPos] != '>')
            {
                Throw(SR.Xml_ExpectExternalOrClose);
            }

            // internal DTD
            if (_ps.chars[_ps.charPos] == '[')
            {
                _ps.charPos++;

                await SkipUntilAsync(']', true).ConfigureAwait(false);

                await EatWhitespacesAsync(null).ConfigureAwait(false);
                if (_ps.chars[_ps.charPos] != '>')
                {
                    ThrowUnexpectedToken(">");
                }
            }
            else if (_ps.chars[_ps.charPos] == '>')
            {
                _curNode.SetValue(string.Empty);
            }
            else
            {
                Throw(SR.Xml_ExpectSubOrClose);
            }
            _ps.charPos++;
        }

        private Task SkipPublicOrSystemIdLiteralAsync()
        {
            // check quote char
            char quoteChar = _ps.chars[_ps.charPos];
            if (quoteChar != '"' && quoteChar != '\'')
            {
                ThrowUnexpectedToken("\"", "'");
            }

            _ps.charPos++;
            return SkipUntilAsync(quoteChar, false);
        }

        private async Task SkipUntilAsync(char stopChar, bool recognizeLiterals)
        {
            bool inLiteral = false;
            bool inComment = false;
            bool inPI = false;
            char literalQuote = '"';

            char[] chars = _ps.chars;
            int pos = _ps.charPos;

            while (true)
            {
                char ch;

                while (XmlCharType.IsAttributeValueChar(ch = chars[pos]) && ch != stopChar && ch != '-' && ch != '?')
                {
                    pos++;
                }

                // closing stopChar outside of literal and ignore/include sections -> save value & return
                if (ch == stopChar && !inLiteral)
                {
                    _ps.charPos = pos + 1;
                    return;
                }

                // handle the special character
                _ps.charPos = pos;
                switch (ch)
                {
                    // eol
                    case (char)0xA:
                        pos++;
                        OnNewLine(pos);
                        continue;
                    case (char)0xD:
                        if (chars[pos + 1] == (char)0xA)
                        {
                            pos += 2;
                        }
                        else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                        {
                            pos++;
                        }
                        else
                        {
                            goto ReadData;
                        }
                        OnNewLine(pos);
                        continue;

                    // comment, PI
                    case '<':
                        // processing instruction
                        if (chars[pos + 1] == '?')
                        {
                            if (recognizeLiterals && !inLiteral && !inComment)
                            {
                                inPI = true;
                                pos += 2;
                                continue;
                            }
                        }
                        // comment
                        else if (chars[pos + 1] == '!')
                        {
                            if (pos + 3 >= _ps.charsUsed && !_ps.isEof)
                            {
                                goto ReadData;
                            }
                            if (chars[pos + 2] == '-' && chars[pos + 3] == '-')
                            {
                                if (recognizeLiterals && !inLiteral && !inPI)
                                {
                                    inComment = true;
                                    pos += 4;
                                    continue;
                                }
                            }
                        }
                        // need more data
                        else if (pos + 1 >= _ps.charsUsed && !_ps.isEof)
                        {
                            goto ReadData;
                        }
                        pos++;
                        continue;
                    case '-':
                        // end of comment
                        if (inComment)
                        {
                            if (pos + 2 >= _ps.charsUsed && !_ps.isEof)
                            {
                                goto ReadData;
                            }
                            if (chars[pos + 1] == '-' && chars[pos + 2] == '>')
                            {
                                inComment = false;
                                pos += 2;
                                continue;
                            }
                        }
                        pos++;
                        continue;

                    case '?':
                        // end of processing instruction
                        if (inPI)
                        {
                            if (pos + 1 >= _ps.charsUsed && !_ps.isEof)
                            {
                                goto ReadData;
                            }
                            if (chars[pos + 1] == '>')
                            {
                                inPI = false;
                                pos += 1;
                                continue;
                            }
                        }
                        pos++;
                        continue;

                    case (char)0x9:
                    case '>':
                    case ']':
                    case '&':
                        pos++;
                        continue;
                    case '"':
                    case '\'':
                        if (inLiteral)
                        {
                            if (literalQuote == ch)
                            {
                                inLiteral = false;
                            }
                        }
                        else
                        {
                            if (recognizeLiterals && !inComment && !inPI)
                            {
                                inLiteral = true;
                                literalQuote = ch;
                            }
                        }
                        pos++;
                        continue;
                    default:
                        // end of buffer
                        if (pos == _ps.charsUsed)
                        {
                            goto ReadData;
                        }
                        // surrogate chars
                        else
                        {
                            char tmpCh = chars[pos];
                            if (XmlCharType.IsHighSurrogate(tmpCh))
                            {
                                if (pos + 1 == _ps.charsUsed)
                                {
                                    goto ReadData;
                                }
                                pos++;
                                if (XmlCharType.IsLowSurrogate(chars[pos]))
                                {
                                    pos++;
                                    continue;
                                }
                            }
                            ThrowInvalidChar(chars, _ps.charsUsed, pos);
                            break;
                        }
                }

            ReadData:
                // read new characters into the buffer
                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    if (_ps.charsUsed - _ps.charPos > 0)
                    {
                        if (_ps.chars[_ps.charPos] != (char)0xD)
                        {
                            Debug.Fail("We should never get to this point.");
                            Throw(SR.Xml_UnexpectedEOF1);
                        }
                        Debug.Assert(_ps.isEof);
                    }
                    else
                    {
                        Throw(SR.Xml_UnexpectedEOF1);
                    }
                }
                chars = _ps.chars;
                pos = _ps.charPos;
            }
        }

        private async Task<int> EatWhitespacesAsync(StringBuilder? sb)
        {
            int pos = _ps.charPos;
            int wsCount = 0;
            char[] chars = _ps.chars;

            while (true)
            {
                while (true)
                {
                    switch (chars[pos])
                    {
                        case (char)0xA:
                            pos++;
                            OnNewLine(pos);
                            continue;
                        case (char)0xD:
                            if (chars[pos + 1] == (char)0xA)
                            {
                                int tmp1 = pos - _ps.charPos;
                                if (sb != null && !_ps.eolNormalized)
                                {
                                    if (tmp1 > 0)
                                    {
                                        sb.Append(chars, _ps.charPos, tmp1);
                                        wsCount += tmp1;
                                    }
                                    _ps.charPos = pos + 1;
                                }
                                pos += 2;
                            }
                            else if (pos + 1 < _ps.charsUsed || _ps.isEof)
                            {
                                if (!_ps.eolNormalized)
                                {
                                    chars[pos] = (char)0xA;             // EOL normalization of 0xD
                                }
                                pos++;
                            }
                            else
                            {
                                goto ReadData;
                            }
                            OnNewLine(pos);
                            continue;
                        case (char)0x9:
                        case (char)0x20:
                            pos++;
                            continue;
                        default:
                            if (pos == _ps.charsUsed)
                            {
                                goto ReadData;
                            }
                            else
                            {
                                int tmp2 = pos - _ps.charPos;
                                if (tmp2 > 0)
                                {
                                    if (sb != null)
                                    {
                                        sb.Append(_ps.chars, _ps.charPos, tmp2);
                                    }
                                    _ps.charPos = pos;
                                    wsCount += tmp2;
                                }
                                return wsCount;
                            }
                    }
                }

            ReadData:
                int tmp3 = pos - _ps.charPos;
                if (tmp3 > 0)
                {
                    if (sb != null)
                    {
                        sb.Append(_ps.chars, _ps.charPos, tmp3);
                    }
                    _ps.charPos = pos;
                    wsCount += tmp3;
                }

                if (await ReadDataAsync().ConfigureAwait(false) == 0)
                {
                    if (_ps.charsUsed - _ps.charPos == 0)
                    {
                        return wsCount;
                    }
                    if (_ps.chars[_ps.charPos] != (char)0xD)
                    {
                        Debug.Fail("We should never get to this point.");
                        Throw(SR.Xml_UnexpectedEOF1);
                    }
                    Debug.Assert(_ps.isEof);
                }
                pos = _ps.charPos;
                chars = _ps.chars;
            }
        }

        // Parses numeric character entity reference (e.g. &#32; &#x20;).
        //      - replaces the last one or two character of the entity reference (';' and the character before) with the referenced
        //        character or surrogates pair (if expand == true)
        //      - returns position of the end of the character reference, that is of the character next to the original ';'
        //      - if (expand == true) then ps.charPos is changed to point to the replaced character

        private async Task<(EntityType, int)> ParseNumericCharRefAsync(bool expand, StringBuilder? internalSubsetBuilder)
        {
            EntityType entityType;

            while (true)
            {
                int newPos;
                int charCount;
                switch (newPos = ParseNumericCharRefInline(_ps.charPos, expand, internalSubsetBuilder, out charCount, out entityType))
                {
                    case -2:
                        // read new characters in the buffer
                        if (await ReadDataAsync().ConfigureAwait(false) == 0)
                        {
                            Throw(SR.Xml_UnexpectedEOF);
                        }
                        Debug.Assert(_ps.chars[_ps.charPos] == '&');
                        continue;
                    default:
                        if (expand)
                        {
                            _ps.charPos = newPos - charCount;
                        }

                        return (entityType, newPos);
                }
            }
        }

        // Parses named character entity reference (&amp; &apos; &lt; &gt; &quot;).
        // Returns -1 if the reference is not a character entity reference.
        // Otherwise
        //      - replaces the last character of the entity reference (';') with the referenced character (if expand == true)
        //      - returns position of the end of the character reference, that is of the character next to the original ';'
        //      - if (expand == true) then ps.charPos is changed to point to the replaced character
        private async Task<int> ParseNamedCharRefAsync(bool expand, StringBuilder? internalSubsetBuilder)
        {
            while (true)
            {
                int newPos;
                switch (newPos = ParseNamedCharRefInline(_ps.charPos, expand, internalSubsetBuilder))
                {
                    case -1:
                        return -1;
                    case -2:
                        // read new characters in the buffer
                        if (await ReadDataAsync().ConfigureAwait(false) == 0)
                        {
                            return -1;
                        }
                        Debug.Assert(_ps.chars[_ps.charPos] == '&');
                        continue;
                    default:
                        if (expand)
                        {
                            _ps.charPos = newPos - 1;
                        }
                        return newPos;
                }
            }
        }

        private async Task<int> ParseNameAsync()
        {
            var tuple_25 = await ParseQNameAsync(false, 0).ConfigureAwait(false);
            return tuple_25.Item2;
        }

        private Task<(int, int)> ParseQNameAsync()
        {
            return ParseQNameAsync(true, 0);
        }

        private async Task<(int, int)> ParseQNameAsync(bool isQName, int startOffset)
        {
            int colonPos;

            int colonOffset = -1;
            int pos = _ps.charPos + startOffset;

        ContinueStartName:
            char[] chars = _ps.chars;

            // start name char
            if (XmlCharType.IsStartNCNameSingleChar(chars[pos]))
            {
                pos++;
            }
            else
            {
                if (pos + 1 >= _ps.charsUsed)
                {
                    var tuple_27 = await ReadDataInNameAsync(pos).ConfigureAwait(false);
                    pos = tuple_27.Item1;

                    if (tuple_27.Item2)
                    {
                        goto ContinueStartName;
                    }
                    Throw(pos, SR.Xml_UnexpectedEOF, "Name");
                }
                else if (chars[pos] != ':' || _supportNamespaces)
                {
                    Throw(pos, SR.Xml_BadStartNameChar, XmlException.BuildCharExceptionArgs(chars, _ps.charsUsed, pos));
                }
            }

        ContinueName:
            // parse name
            while (true)
            {
                if (XmlCharType.IsNCNameSingleChar(chars[pos]))
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }

            // colon
            if (chars[pos] == ':')
            {
                if (_supportNamespaces)
                {
                    if (colonOffset != -1 || !isQName)
                    {
                        Throw(pos, SR.Xml_BadNameChar, XmlException.BuildCharExceptionArgs(':', '\0'));
                    }
                    colonOffset = pos - _ps.charPos;
                    pos++;
                    goto ContinueStartName;
                }
                else
                {
                    colonOffset = pos - _ps.charPos;
                    pos++;
                    goto ContinueName;
                }
            }
            // end of buffer
            else if (pos == _ps.charsUsed)
            {
                var tuple_28 = await ReadDataInNameAsync(pos).ConfigureAwait(false);
                pos = tuple_28.Item1;

                if (tuple_28.Item2)
                {
                    chars = _ps.chars;
                    goto ContinueName;
                }
                Throw(pos, SR.Xml_UnexpectedEOF, "Name");
            }

            // end of name
            colonPos = (colonOffset == -1) ? -1 : _ps.charPos + colonOffset;

            return (colonPos, pos);
        }

        private async Task<(int, bool)> ReadDataInNameAsync(int pos)
        {
            int offset = pos - _ps.charPos;
            bool newDataRead = (await ReadDataAsync().ConfigureAwait(false) != 0);
            pos = _ps.charPos + offset;

            return (pos, newDataRead);
        }

        private async Task<string> ParseEntityNameAsync()
        {
            int endPos;
            try
            {
                endPos = await ParseNameAsync().ConfigureAwait(false);
            }
            catch (XmlException)
            {
                Throw(SR.Xml_ErrorParsingEntityName);
                return null;
            }

            // check ';'
            if (_ps.chars[endPos] != ';')
            {
                Throw(SR.Xml_ErrorParsingEntityName);
            }

            string entityName = _nameTable.Add(_ps.chars, _ps.charPos, endPos - _ps.charPos);
            _ps.charPos = endPos + 1;
            return entityName;
        }

        // This method resolves and opens an external DTD subset or an external entity based on its SYSTEM or PUBLIC ID.
        // SxS: This method may expose a name if a resource in baseUri (ref) parameter.
        private async Task PushExternalEntityOrSubsetAsync(string? publicId, string? systemId, Uri? baseUri, string? entityName)
        {
            Uri uri;

            // First try opening the external reference by PUBLIC Id
            if (!string.IsNullOrEmpty(publicId))
            {
                try
                {
                    uri = _xmlResolver!.ResolveUri(baseUri, publicId);
                    if (await OpenAndPushAsync(uri).ConfigureAwait(false))
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    // Intentionally empty - swallow all exception related to PUBLIC ID and try opening the entity via the SYSTEM ID
                }
            }

            // Then try SYSTEM Id
            uri = _xmlResolver!.ResolveUri(baseUri, systemId);
            try
            {
                if (await OpenAndPushAsync(uri).ConfigureAwait(false))
                {
                    return;
                }
                // resolver returned null, throw exception outside this try-catch
            }
            catch (Exception e)
            {
                if (_v1Compat)
                {
                    throw;
                }
                string innerMessage;
                innerMessage = e.Message;
                Throw(new XmlException(entityName == null ? SR.Xml_ErrorOpeningExternalDtd : SR.Xml_ErrorOpeningExternalEntity, new string[] { uri.ToString(), innerMessage }, e, 0, 0));
            }

            if (entityName == null)
            {
                ThrowWithoutLineInfo(SR.Xml_CannotResolveExternalSubset, new string?[] { publicId ?? string.Empty, systemId }, null);
            }
            else
            {
                Throw(_dtdProcessing == DtdProcessing.Ignore ? SR.Xml_CannotResolveEntityDtdIgnored : SR.Xml_CannotResolveEntity, entityName);
            }

            return;
        }

        // This method opens the URI as a TextReader or Stream, pushes new ParsingStateState on the stack and calls InitStreamInput or InitTextReaderInput.
        // Returns:
        //    - true when everything went ok.
        //    - false when XmlResolver.GetEntity returned null
        // Propagates any exceptions from the XmlResolver indicating when the URI cannot be opened.
        private async Task<bool> OpenAndPushAsync(Uri uri)
        {
            Debug.Assert(_xmlResolver != null);

            // First try to get the data as a TextReader
            if (_xmlResolver.SupportsType(uri, typeof(TextReader)))
            {
                TextReader textReader = (TextReader)await _xmlResolver.GetEntityAsync(uri, null, typeof(TextReader)).ConfigureAwait(false);
                if (textReader == null)
                {
                    return false;
                }

                PushParsingState();
                await InitTextReaderInputAsync(uri.ToString(), uri, textReader).ConfigureAwait(false);
            }
            else
            {
                // Then try get it as a Stream
                Debug.Assert(_xmlResolver.SupportsType(uri, typeof(Stream)), "Stream must always be a supported type in XmlResolver");

                Stream stream = (Stream)await _xmlResolver.GetEntityAsync(uri, null, typeof(Stream)).ConfigureAwait(false);
                if (stream == null)
                {
                    return false;
                }

                PushParsingState();
                await InitStreamInputAsync(uri, stream, null).ConfigureAwait(false);
            }
            return true;
        }

        // returns true if real entity has been pushed, false if fake entity (=empty content entity)
        // SxS: The method neither takes any name of resource directly nor it exposes any resource to the caller.
        // Entity info was created based on source document. It's OK to suppress the SxS warning
        private async Task<bool> PushExternalEntityAsync(IDtdEntityInfo entity)
        {
            Debug.Assert(entity.IsExternal);

            if (!IsResolverNull)
            {
                Uri? entityBaseUri = null;
                // Resolve base URI
                if (!string.IsNullOrEmpty(entity.BaseUriString))
                {
                    entityBaseUri = _xmlResolver!.ResolveUri(null, entity.BaseUriString);
                }
                await PushExternalEntityOrSubsetAsync(entity.PublicId, entity.SystemId, entityBaseUri, entity.Name).ConfigureAwait(false);

                RegisterEntity(entity);

                Debug.Assert(_ps.appendMode);
                int initialPos = _ps.charPos;
                if (_v1Compat)
                {
                    await EatWhitespacesAsync(null).ConfigureAwait(false);
                }
                if (!await ParseXmlDeclarationAsync(true).ConfigureAwait(false))
                {
                    _ps.charPos = initialPos;
                }
                return true;
            }
            else
            {
                Encoding? enc = _ps.encoding;

                PushParsingState();
                InitStringInput(entity.SystemId!, enc, string.Empty);

                RegisterEntity(entity);

                RegisterConsumedCharacters(0, true);

                return false;
            }
        }

        // This method is used to enable parsing of zero-terminated streams. The old XmlTextReader implementation used
        // to parse such streams, we this one needs to do that as well.
        // If the last characters decoded from the stream is 0 and the stream is in EOF state, this method will remove
        // the character from the parsing buffer (decrements ps.charsUsed).
        // Note that this method calls ReadData() which may change the value of ps.chars and ps.charPos.
        private async Task<bool> ZeroEndingStreamAsync(int pos)
        {
            if (_v1Compat && pos == _ps.charsUsed - 1 && _ps.chars[pos] == (char)0 && await ReadDataAsync().ConfigureAwait(false) == 0 && _ps.isStreamEof)
            {
                _ps.charsUsed--;
                return true;
            }
            return false;
        }

        private async Task ParseDtdFromParserContextAsync()
        {
            Debug.Assert(_dtdInfo == null && _fragmentParserContext != null && _fragmentParserContext.HasDtdInfo);

            IDtdParser dtdParser = DtdParser.Create();

            // Parse DTD
            _dtdInfo = await dtdParser.ParseFreeFloatingDtdAsync(_fragmentParserContext.BaseURI, _fragmentParserContext.DocTypeName, _fragmentParserContext.PublicId, _fragmentParserContext.SystemId, _fragmentParserContext.InternalSubset, new DtdParserProxy(this)).ConfigureAwait(false);

            if ((_validatingReaderCompatFlag || !_v1Compat) && (_dtdInfo.HasDefaultAttributes || _dtdInfo.HasNonCDataAttributes))
            {
                _addDefaultAttributesAndNormalize = true;
            }
        }

        private async Task<bool> InitReadContentAsBinaryAsync()
        {
            Debug.Assert(_parsingFunction != ParsingFunction.InReadContentAsBinary);

            if (_parsingFunction == ParsingFunction.InReadValueChunk)
            {
                throw new InvalidOperationException(SR.Xml_MixingReadValueChunkWithBinary);
            }
            if (_parsingFunction == ParsingFunction.InIncrementalRead)
            {
                throw new InvalidOperationException(SR.Xml_MixingV1StreamingWithV2Binary);
            }

            if (!XmlReader.IsTextualNode(_curNode.type))
            {
                if (!await MoveToNextContentNodeAsync(false).ConfigureAwait(false))
                {
                    return false;
                }
            }

            SetupReadContentAsBinaryState(ParsingFunction.InReadContentAsBinary);
            _incReadLineInfo.Set(_curNode.LineNo, _curNode.LinePos);
            return true;
        }

        private async Task<bool> InitReadElementContentAsBinaryAsync()
        {
            Debug.Assert(_parsingFunction != ParsingFunction.InReadElementContentAsBinary);
            Debug.Assert(_curNode.type == XmlNodeType.Element);

            bool isEmpty = _curNode.IsEmptyElement;

            // move to content or off the empty element
            await _outerReader.ReadAsync().ConfigureAwait(false);
            if (isEmpty)
            {
                return false;
            }

            // make sure we are on a content node
            if (!await MoveToNextContentNodeAsync(false).ConfigureAwait(false))
            {
                if (_curNode.type != XmlNodeType.EndElement)
                {
                    Throw(SR.Xml_InvalidNodeType, _curNode.type.ToString());
                }
                // move off end element
                await _outerReader.ReadAsync().ConfigureAwait(false);
                return false;
            }
            SetupReadContentAsBinaryState(ParsingFunction.InReadElementContentAsBinary);
            _incReadLineInfo.Set(_curNode.LineNo, _curNode.LinePos);
            return true;
        }

        private async Task<bool> MoveToNextContentNodeAsync(bool moveIfOnContentNode)
        {
            do
            {
                switch (_curNode.type)
                {
                    case XmlNodeType.Attribute:
                        return !moveIfOnContentNode;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.CDATA:
                        if (!moveIfOnContentNode)
                        {
                            return true;
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                    case XmlNodeType.EndEntity:
                        // skip comments, pis and end entity nodes
                        break;
                    case XmlNodeType.EntityReference:
                        _outerReader.ResolveEntity();
                        break;
                    default:
                        return false;
                }
                moveIfOnContentNode = false;
            } while (await _outerReader.ReadAsync().ConfigureAwait(false));
            return false;
        }

        private async Task<int> ReadContentAsBinaryAsync(byte[] buffer, int index, int count)
        {
            Debug.Assert(_incReadDecoder != null);

            if (_incReadState == IncrementalReadState.ReadContentAsBinary_End)
            {
                return 0;
            }

            _incReadDecoder.SetNextOutputBuffer(buffer, index, count);

            while (true)
            {
                // read what is already cached in curNode
                int charsRead = 0;
                try
                {
                    charsRead = _curNode.CopyToBinary(_incReadDecoder, _readValueOffset);
                }
                // add line info to the exception
                catch (XmlException e)
                {
                    _curNode.AdjustLineInfo(_readValueOffset, _ps.eolNormalized, ref _incReadLineInfo);
                    ReThrow(e, _incReadLineInfo.lineNo, _incReadLineInfo.linePos);
                }
                _readValueOffset += charsRead;

                if (_incReadDecoder.IsFull)
                {
                    return _incReadDecoder.DecodedCount;
                }

                // if on partial value, read the rest of it
                if (_incReadState == IncrementalReadState.ReadContentAsBinary_OnPartialValue)
                {
                    _curNode.SetValue(string.Empty);

                    // read next chunk of text
                    bool endOfValue = false;
                    int startPos = 0;
                    int endPos = 0;
                    while (!_incReadDecoder.IsFull && !endOfValue)
                    {
                        int orChars = 0;

                        // store current line info and parse more text
                        _incReadLineInfo.Set(_ps.LineNo, _ps.LinePos);

                        var tuple_36 = await ParseTextAsync(orChars).ConfigureAwait(false);
                        startPos = tuple_36.Item1;
                        endPos = tuple_36.Item2;

                        endOfValue = tuple_36.Item4;

                        try
                        {
                            charsRead = _incReadDecoder.Decode(_ps.chars, startPos, endPos - startPos);
                        }
                        // add line info to the exception
                        catch (XmlException e)
                        {
                            ReThrow(e, _incReadLineInfo.lineNo, _incReadLineInfo.linePos);
                        }
                        startPos += charsRead;
                    }
                    _incReadState = endOfValue ? IncrementalReadState.ReadContentAsBinary_OnCachedValue : IncrementalReadState.ReadContentAsBinary_OnPartialValue;
                    _readValueOffset = 0;

                    if (_incReadDecoder.IsFull)
                    {
                        _curNode.SetValue(_ps.chars, startPos, endPos - startPos);
                        // adjust line info for the chunk that has been already decoded
                        AdjustLineInfo(_ps.chars, startPos - charsRead, startPos, _ps.eolNormalized, ref _incReadLineInfo);
                        _curNode.SetLineInfo(_incReadLineInfo.lineNo, _incReadLineInfo.linePos);
                        return _incReadDecoder.DecodedCount;
                    }
                }

                // reset to normal state so we can call Read() to move forward
                ParsingFunction tmp = _parsingFunction;
                _parsingFunction = _nextParsingFunction;
                _nextParsingFunction = _nextNextParsingFunction;

                // move to next textual node in the element content; throw on sub elements
                if (!await MoveToNextContentNodeAsync(true).ConfigureAwait(false))
                {
                    SetupReadContentAsBinaryState(tmp);
                    _incReadState = IncrementalReadState.ReadContentAsBinary_End;
                    return _incReadDecoder.DecodedCount;
                }
                SetupReadContentAsBinaryState(tmp);
                _incReadLineInfo.Set(_curNode.LineNo, _curNode.LinePos);
            }
        }

        private async Task<int> ReadElementContentAsBinaryAsync(byte[] buffer, int index, int count)
        {
            if (count == 0)
            {
                return 0;
            }
            int decoded = await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
            if (decoded > 0)
            {
                return decoded;
            }

            // if 0 bytes returned check if we are on a closing EndElement, throw exception if not
            if (_curNode.type != XmlNodeType.EndElement)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, _curNode.type.ToString(), this as IXmlLineInfo);
            }

            // reset state
            _parsingFunction = _nextParsingFunction;
            _nextParsingFunction = _nextNextParsingFunction;
            Debug.Assert(_parsingFunction != ParsingFunction.InReadElementContentAsBinary);

            // move off the EndElement
            await _outerReader.ReadAsync().ConfigureAwait(false);
            return 0;
        }
    }
}
