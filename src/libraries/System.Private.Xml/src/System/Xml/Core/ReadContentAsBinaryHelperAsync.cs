// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Xml
{
    internal sealed partial class ReadContentAsBinaryHelper
    {
        // Internal methods

        internal async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
        {
            // check arguments
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
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

            switch (_state)
            {
                case State.None:
                    if (!_reader.CanReadContentAs())
                    {
                        throw _reader.CreateReadContentAsException(nameof(ReadContentAsBase64));
                    }
                    if (!await InitAsync().ConfigureAwait(false))
                    {
                        return 0;
                    }
                    break;
                case State.InReadContent:
                    // if we have a correct decoder, go read
                    if (_decoder == _base64Decoder)
                    {
                        // read more binary data
                        return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
                    }
                    break;
                case State.InReadElementContent:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }

            Debug.Assert(_state == State.InReadContent);

            // setup base64 decoder
            InitBase64Decoder();

            // read more binary data
            return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
        }

        internal async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            // check arguments
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
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

            switch (_state)
            {
                case State.None:
                    if (!_reader.CanReadContentAs())
                    {
                        throw _reader.CreateReadContentAsException(nameof(ReadContentAsBinHex));
                    }
                    if (!await InitAsync().ConfigureAwait(false))
                    {
                        return 0;
                    }
                    break;
                case State.InReadContent:
                    // if we have a correct decoder, go read
                    if (_decoder == _binHexDecoder)
                    {
                        // read more binary data
                        return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
                    }
                    break;
                case State.InReadElementContent:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }

            Debug.Assert(_state == State.InReadContent);

            // setup binhex decoder
            InitBinHexDecoder();

            // read more binary data
            return await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
        }

        internal async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
        {
            // check arguments
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
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

            switch (_state)
            {
                case State.None:
                    if (_reader.NodeType != XmlNodeType.Element)
                    {
                        throw _reader.CreateReadElementContentAsException(nameof(ReadElementContentAsBase64));
                    }
                    if (!await InitOnElementAsync().ConfigureAwait(false))
                    {
                        return 0;
                    }
                    break;
                case State.InReadContent:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                case State.InReadElementContent:
                    // if we have a correct decoder, go read
                    if (_decoder == _base64Decoder)
                    {
                        // read more binary data
                        return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
                    }
                    break;
                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }

            Debug.Assert(_state == State.InReadElementContent);

            // setup base64 decoder
            InitBase64Decoder();

            // read more binary data
            return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
        }

        internal async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            // check arguments
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
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

            switch (_state)
            {
                case State.None:
                    if (_reader.NodeType != XmlNodeType.Element)
                    {
                        throw _reader.CreateReadElementContentAsException(nameof(ReadElementContentAsBinHex));
                    }
                    if (!await InitOnElementAsync().ConfigureAwait(false))
                    {
                        return 0;
                    }
                    break;
                case State.InReadContent:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                case State.InReadElementContent:
                    // if we have a correct decoder, go read
                    if (_decoder == _binHexDecoder)
                    {
                        // read more binary data
                        return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
                    }
                    break;
                default:
                    Debug.Fail($"Unexpected state {_state}");
                    return 0;
            }

            Debug.Assert(_state == State.InReadElementContent);

            // setup binhex decoder
            InitBinHexDecoder();

            // read more binary data
            return await ReadElementContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
        }

        internal async Task FinishAsync()
        {
            if (_state != State.None)
            {
                while (await MoveToNextContentNodeAsync(true).ConfigureAwait(false))
                    ;
                if (_state == State.InReadElementContent)
                {
                    if (_reader.NodeType != XmlNodeType.EndElement)
                    {
                        throw new XmlException(SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
                    }
                    // move off the EndElement
                    await _reader.ReadAsync().ConfigureAwait(false);
                }
            }
            Reset();
        }

        // Private methods
        private async Task<bool> InitAsync()
        {
            // make sure we are on a content node
            if (!await MoveToNextContentNodeAsync(false).ConfigureAwait(false))
            {
                return false;
            }

            _state = State.InReadContent;
            _isEnd = false;
            return true;
        }

        private async Task<bool> InitOnElementAsync()
        {
            Debug.Assert(_reader.NodeType == XmlNodeType.Element);
            bool isEmpty = _reader.IsEmptyElement;

            // move to content or off the empty element
            await _reader.ReadAsync().ConfigureAwait(false);
            if (isEmpty)
            {
                return false;
            }

            // make sure we are on a content node
            if (!await MoveToNextContentNodeAsync(false).ConfigureAwait(false))
            {
                if (_reader.NodeType != XmlNodeType.EndElement)
                {
                    throw new XmlException(SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
                }
                // move off end element
                await _reader.ReadAsync().ConfigureAwait(false);
                return false;
            }
            _state = State.InReadElementContent;
            _isEnd = false;
            return true;
        }

        private async Task<int> ReadContentAsBinaryAsync(byte[] buffer, int index, int count)
        {
            Debug.Assert(_decoder != null);

            if (_isEnd)
            {
                Reset();
                return 0;
            }
            _decoder.SetNextOutputBuffer(buffer, index, count);

            while (true)
            {
                // use streaming ReadValueChunk if the reader supports it
                if (_canReadValueChunk)
                {
                    while (true)
                    {
                        if (_valueOffset < _valueChunkLength)
                        {
                            int decodedCharsCount = _decoder.Decode(_valueChunk!, _valueOffset, _valueChunkLength - _valueOffset);
                            _valueOffset += decodedCharsCount;
                        }
                        if (_decoder.IsFull)
                        {
                            return _decoder.DecodedCount;
                        }
                        Debug.Assert(_valueOffset == _valueChunkLength);
                        if ((_valueChunkLength = await _reader.ReadValueChunkAsync(_valueChunk!, 0, ChunkSize).ConfigureAwait(false)) == 0)
                        {
                            break;
                        }
                        _valueOffset = 0;
                    }
                }
                else
                {
                    // read what is reader.Value
                    string value = await _reader.GetValueAsync().ConfigureAwait(false);
                    int decodedCharsCount = _decoder.Decode(value, _valueOffset, value.Length - _valueOffset);
                    _valueOffset += decodedCharsCount;

                    if (_decoder.IsFull)
                    {
                        return _decoder.DecodedCount;
                    }
                }

                _valueOffset = 0;

                // move to next textual node in the element content; throw on sub elements
                if (!await MoveToNextContentNodeAsync(true).ConfigureAwait(false))
                {
                    _isEnd = true;
                    return _decoder.DecodedCount;
                }
            }
        }

        private async Task<int> ReadElementContentAsBinaryAsync(byte[] buffer, int index, int count)
        {
            if (count == 0)
            {
                return 0;
            }
            // read binary
            int decoded = await ReadContentAsBinaryAsync(buffer, index, count).ConfigureAwait(false);
            if (decoded > 0)
            {
                return decoded;
            }

            // if 0 bytes returned check if we are on a closing EndElement, throw exception if not
            if (_reader.NodeType != XmlNodeType.EndElement)
            {
                throw new XmlException(SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
            }

            // move off the EndElement
            await _reader.ReadAsync().ConfigureAwait(false);
            _state = State.None;
            return 0;
        }

        private async Task<bool> MoveToNextContentNodeAsync(bool moveIfOnContentNode)
        {
            do
            {
                switch (_reader.NodeType)
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
                        if (_reader.CanResolveEntity)
                        {
                            _reader.ResolveEntity();
                            break;
                        }
                        goto default;
                    default:
                        return false;
                }
                moveIfOnContentNode = false;
            } while (await _reader.ReadAsync().ConfigureAwait(false));
            return false;
        }
    }
}
