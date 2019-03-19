// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace System.Xml
{
    internal sealed partial class XmlSubtreeReader : XmlWrappingReader, IXmlLineInfo, IXmlNamespaceResolver
    {
        public override Task<string> GetValueAsync()
        {
            if (_useCurNode)
            {
                return Task.FromResult(_curNode.value);
            }
            else
            {
                return reader.GetValueAsync();
            }
        }

        public override async Task<bool> ReadAsync()
        {
            switch (_state)
            {
                case State.Initial:
                    _useCurNode = false;
                    _state = State.Interactive;
                    ProcessNamespaces();
                    return true;

                case State.Interactive:
                    _curNsAttr = -1;
                    _useCurNode = false;
                    reader.MoveToElement();
                    Debug.Assert(reader.Depth >= _initialDepth);
                    if (reader.Depth == _initialDepth)
                    {
                        if (reader.NodeType == XmlNodeType.EndElement ||
                            (reader.NodeType == XmlNodeType.Element && reader.IsEmptyElement))
                        {
                            _state = State.EndOfFile;
                            SetEmptyNode();
                            return false;
                        }
                        Debug.Assert(reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement);
                    }
                    if (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        ProcessNamespaces();
                        return true;
                    }
                    else
                    {
                        SetEmptyNode();
                        return false;
                    }

                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return false;

                case State.PopNamespaceScope:
                    _nsManager.PopScope();
                    goto case State.ClearNsAttributes;

                case State.ClearNsAttributes:
                    _nsAttrCount = 0;
                    _state = State.Interactive;
                    goto case State.Interactive;

                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                    if (!await FinishReadElementContentAsBinaryAsync().ConfigureAwait(false))
                    {
                        return false;
                    }
                    return await ReadAsync().ConfigureAwait(false);

                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                    if (!await FinishReadContentAsBinaryAsync().ConfigureAwait(false))
                    {
                        return false;
                    }
                    return await ReadAsync().ConfigureAwait(false);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return false;
            }
        }

        public override async Task SkipAsync()
        {
            switch (_state)
            {
                case State.Initial:
                    await ReadAsync().ConfigureAwait(false);
                    return;

                case State.Interactive:
                    _curNsAttr = -1;
                    _useCurNode = false;
                    reader.MoveToElement();
                    Debug.Assert(reader.Depth >= _initialDepth);
                    if (reader.Depth == _initialDepth)
                    {
                        if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                        {
                            // we are on root of the subtree -> skip to the end element and set to Eof state
                            if (await reader.ReadAsync().ConfigureAwait(false))
                            {
                                while (reader.NodeType != XmlNodeType.EndElement && reader.Depth > _initialDepth)
                                {
                                    await reader.SkipAsync().ConfigureAwait(false);
                                }
                            }
                        }
                        Debug.Assert(reader.NodeType == XmlNodeType.EndElement ||
                                      reader.NodeType == XmlNodeType.Element && reader.IsEmptyElement ||
                                      reader.ReadState != ReadState.Interactive);
                        _state = State.EndOfFile;
                        SetEmptyNode();
                        return;
                    }

                    if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                    {
                        _nsManager.PopScope();
                    }
                    await reader.SkipAsync().ConfigureAwait(false);
                    ProcessNamespaces();

                    Debug.Assert(reader.Depth >= _initialDepth);
                    return;

                case State.Closed:
                case State.EndOfFile:
                    return;

                case State.PopNamespaceScope:
                    _nsManager.PopScope();
                    goto case State.ClearNsAttributes;

                case State.ClearNsAttributes:
                    _nsAttrCount = 0;
                    _state = State.Interactive;
                    goto case State.Interactive;

                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                    if (await FinishReadElementContentAsBinaryAsync().ConfigureAwait(false))
                    {
                        await SkipAsync().ConfigureAwait(false);
                    }
                    break;

                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                    if (await FinishReadContentAsBinaryAsync().ConfigureAwait(false))
                    {
                        await SkipAsync().ConfigureAwait(false);
                    }
                    break;

                case State.Error:
                    return;

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return;
            }
        }

        public override async Task<object> ReadContentAsObjectAsync()
        {
            try
            {
                InitReadContentAsType("ReadContentAsObject");
                object value = await reader.ReadContentAsObjectAsync().ConfigureAwait(false);
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override async Task<string> ReadContentAsStringAsync()
        {
            try
            {
                InitReadContentAsType("ReadContentAsString");
                string value = await reader.ReadContentAsStringAsync().ConfigureAwait(false);
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override async Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            try
            {
                InitReadContentAsType("ReadContentAs");
                object value = await reader.ReadContentAsAsync(returnType, namespaceResolver).ConfigureAwait(false);
                FinishReadContentAsType();
                return value;
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        public override async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return 0;

                case State.ClearNsAttributes:
                case State.PopNamespaceScope:
                    switch (NodeType)
                    {
                        case XmlNodeType.Element:
                            throw CreateReadContentAsException(nameof(ReadContentAsBase64));
                        case XmlNodeType.EndElement:
                            return 0;
                        case XmlNodeType.Attribute:
                            if (_curNsAttr != -1 && reader.CanReadBinaryContent)
                            {
                                CheckBuffer(buffer, index, count);
                                if (count == 0)
                                {
                                    return 0;
                                }
                                if (_nsIncReadOffset == 0)
                                {
                                    // called first time on this ns attribute
                                    if (_binDecoder != null && _binDecoder is Base64Decoder)
                                    {
                                        _binDecoder.Reset();
                                    }
                                    else
                                    {
                                        _binDecoder = new Base64Decoder();
                                    }
                                }
                                if (_nsIncReadOffset == _curNode.value.Length)
                                {
                                    return 0;
                                }
                                _binDecoder.SetNextOutputBuffer(buffer, index, count);
                                _nsIncReadOffset += _binDecoder.Decode(_curNode.value, _nsIncReadOffset, _curNode.value.Length - _nsIncReadOffset);
                                return _binDecoder.DecodedCount;
                            }
                            goto case XmlNodeType.Text;
                        case XmlNodeType.Text:
                            Debug.Assert(AttributeCount > 0);
                            return await reader.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(false);
                        default:
                            Debug.Fail($"Unexpected state {_state}");
                            return 0;
                    }

                case State.Interactive:
                    _state = State.ReadContentAsBase64;
                    goto case State.ReadContentAsBase64;

                case State.ReadContentAsBase64:
                    int read = await reader.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(false);
                    if (read == 0)
                    {
                        _state = State.Interactive;
                        ProcessNamespaces();
                    }
                    return read;

                case State.ReadContentAsBinHex:
                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }
        }

        public override async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return 0;

                case State.Interactive:
                case State.PopNamespaceScope:
                case State.ClearNsAttributes:
                    if (!await InitReadElementContentAsBinaryAsync(State.ReadElementContentAsBase64).ConfigureAwait(false))
                    {
                        return 0;
                    }
                    goto case State.ReadElementContentAsBase64;

                case State.ReadElementContentAsBase64:
                    int read = await reader.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(false);
                    if (read > 0 || count == 0)
                    {
                        return read;
                    }
                    if (NodeType != XmlNodeType.EndElement)
                    {
                        throw new XmlException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
                    }

                    // pop namespace scope
                    _state = State.Interactive;
                    ProcessNamespaces();

                    // set eof state or move off the end element
                    if (reader.Depth == _initialDepth)
                    {
                        _state = State.EndOfFile;
                        SetEmptyNode();
                    }
                    else
                    {
                        await ReadAsync().ConfigureAwait(false);
                    }
                    return 0;

                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                case State.ReadElementContentAsBinHex:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }
        }

        public override async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return 0;

                case State.ClearNsAttributes:
                case State.PopNamespaceScope:
                    switch (NodeType)
                    {
                        case XmlNodeType.Element:
                            throw CreateReadContentAsException(nameof(ReadContentAsBinHex));
                        case XmlNodeType.EndElement:
                            return 0;
                        case XmlNodeType.Attribute:
                            if (_curNsAttr != -1 && reader.CanReadBinaryContent)
                            {
                                CheckBuffer(buffer, index, count);
                                if (count == 0)
                                {
                                    return 0;
                                }
                                if (_nsIncReadOffset == 0)
                                {
                                    // called first time on this ns attribute
                                    if (_binDecoder != null && _binDecoder is BinHexDecoder)
                                    {
                                        _binDecoder.Reset();
                                    }
                                    else
                                    {
                                        _binDecoder = new BinHexDecoder();
                                    }
                                }
                                if (_nsIncReadOffset == _curNode.value.Length)
                                {
                                    return 0;
                                }
                                _binDecoder.SetNextOutputBuffer(buffer, index, count);
                                _nsIncReadOffset += _binDecoder.Decode(_curNode.value, _nsIncReadOffset, _curNode.value.Length - _nsIncReadOffset);
                                return _binDecoder.DecodedCount;
                            }
                            goto case XmlNodeType.Text;
                        case XmlNodeType.Text:
                            Debug.Assert(AttributeCount > 0);
                            return await reader.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);
                        default:
                            Debug.Fail($"Unexpected state {_state}");
                            return 0;
                    }

                case State.Interactive:
                    _state = State.ReadContentAsBinHex;
                    goto case State.ReadContentAsBinHex;

                case State.ReadContentAsBinHex:
                    int read = await reader.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);
                    if (read == 0)
                    {
                        _state = State.Interactive;
                        ProcessNamespaces();
                    }
                    return read;

                case State.ReadContentAsBase64:
                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }
        }

        public override async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return 0;

                case State.Interactive:
                case State.PopNamespaceScope:
                case State.ClearNsAttributes:
                    if (!await InitReadElementContentAsBinaryAsync(State.ReadElementContentAsBinHex).ConfigureAwait(false))
                    {
                        return 0;
                    }
                    goto case State.ReadElementContentAsBinHex;
                case State.ReadElementContentAsBinHex:
                    int read = await reader.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);
                    if (read > 0 || count == 0)
                    {
                        return read;
                    }
                    if (NodeType != XmlNodeType.EndElement)
                    {
                        throw new XmlException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
                    }

                    // pop namespace scope
                    _state = State.Interactive;
                    ProcessNamespaces();

                    // set eof state or move off the end element
                    if (reader.Depth == _initialDepth)
                    {
                        _state = State.EndOfFile;
                        SetEmptyNode();
                    }
                    else
                    {
                        await ReadAsync().ConfigureAwait(false);
                    }
                    return 0;

                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                case State.ReadElementContentAsBase64:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }
        }

        public override Task<int> ReadValueChunkAsync(char[] buffer, int index, int count)
        {
            switch (_state)
            {
                case State.Initial:
                case State.EndOfFile:
                case State.Closed:
                case State.Error:
                    return AsyncHelper.DoneTaskZero;

                case State.ClearNsAttributes:
                case State.PopNamespaceScope:
                    // ReadValueChunk implementation on added xmlns attributes
                    if (_curNsAttr != -1 && reader.CanReadValueChunk)
                    {
                        CheckBuffer(buffer, index, count);
                        int copyCount = _curNode.value.Length - _nsIncReadOffset;
                        if (copyCount > count)
                        {
                            copyCount = count;
                        }
                        if (copyCount > 0)
                        {
                            _curNode.value.CopyTo(_nsIncReadOffset, buffer, index, copyCount);
                        }
                        _nsIncReadOffset += copyCount;
                        return Task.FromResult(copyCount);
                    }
                    // Otherwise fall back to the case State.Interactive.
                    // No need to clean ns attributes or pop scope because the reader when ReadValueChunk is called
                    // - on Element errors
                    // - on EndElement errors
                    // - on Attribute does not move
                    // and that's all where State.ClearNsAttributes or State.PopnamespaceScope can be set
                    goto case State.Interactive;

                case State.Interactive:
                    return reader.ReadValueChunkAsync(buffer, index, count);

                case State.ReadElementContentAsBase64:
                case State.ReadElementContentAsBinHex:
                case State.ReadContentAsBase64:
                case State.ReadContentAsBinHex:
                    throw new InvalidOperationException(SR.Xml_MixingReadValueChunkWithBinary);

                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return AsyncHelper.DoneTaskZero;
            }
        }

        private async Task<bool> InitReadElementContentAsBinaryAsync(State binaryState)
        {
            if (NodeType != XmlNodeType.Element)
            {
                throw reader.CreateReadElementContentAsException(nameof(ReadElementContentAsBase64));
            }

            bool isEmpty = IsEmptyElement;

            // move to content or off the empty element
            if (!await ReadAsync().ConfigureAwait(false) || isEmpty)
            {
                return false;
            }
            // special-case child element and end element
            switch (NodeType)
            {
                case XmlNodeType.Element:
                    throw new XmlException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
                case XmlNodeType.EndElement:
                    // pop scope & move off end element
                    ProcessNamespaces();
                    await ReadAsync().ConfigureAwait(false);
                    return false;
            }

            Debug.Assert(_state == State.Interactive);
            _state = binaryState;
            return true;
        }

        private async Task<bool> FinishReadElementContentAsBinaryAsync()
        {
            Debug.Assert(_state == State.ReadElementContentAsBase64 || _state == State.ReadElementContentAsBinHex);

            byte[] bytes = new byte[256];
            if (_state == State.ReadElementContentAsBase64)
            {
                while (await reader.ReadContentAsBase64Async(bytes, 0, 256).ConfigureAwait(false) > 0) ;
            }
            else
            {
                while (await reader.ReadContentAsBinHexAsync(bytes, 0, 256).ConfigureAwait(false) > 0) ;
            }

            if (NodeType != XmlNodeType.EndElement)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
            }

            // pop namespace scope
            _state = State.Interactive;
            ProcessNamespaces();

            // check eof
            if (reader.Depth == _initialDepth)
            {
                _state = State.EndOfFile;
                SetEmptyNode();
                return false;
            }
            // move off end element
            return await ReadAsync().ConfigureAwait(false);
        }

        private async Task<bool> FinishReadContentAsBinaryAsync()
        {
            Debug.Assert(_state == State.ReadContentAsBase64 || _state == State.ReadContentAsBinHex);

            byte[] bytes = new byte[256];
            if (_state == State.ReadContentAsBase64)
            {
                while (await reader.ReadContentAsBase64Async(bytes, 0, 256).ConfigureAwait(false) > 0) ;
            }
            else
            {
                while (await reader.ReadContentAsBinHexAsync(bytes, 0, 256).ConfigureAwait(false) > 0) ;
            }

            _state = State.Interactive;
            ProcessNamespaces();

            // check eof
            if (reader.Depth == _initialDepth)
            {
                _state = State.EndOfFile;
                SetEmptyNode();
                return false;
            }
            return true;
        }
    }
}

