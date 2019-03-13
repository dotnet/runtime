// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Xml
{
    internal partial class ReadContentAsBinaryHelper
    {
        // Private enums
        private enum State
        {
            None,
            InReadContent,
            InReadElementContent,
        }

        // Fields 
        private XmlReader _reader;
        private State _state;
        private int _valueOffset;
        private bool _isEnd;

        private bool _canReadValueChunk;
        private char[] _valueChunk;
        private int _valueChunkLength;

        private IncrementalReadDecoder _decoder;
        private Base64Decoder _base64Decoder;
        private BinHexDecoder _binHexDecoder;

        // Constants
        private const int ChunkSize = 256;

        // Constructor
        internal ReadContentAsBinaryHelper(XmlReader reader)
        {
            _reader = reader;
            _canReadValueChunk = reader.CanReadValueChunk;

            if (_canReadValueChunk)
            {
                _valueChunk = new char[ChunkSize];
            }
        }

        // Static methods 
        internal static ReadContentAsBinaryHelper CreateOrReset(ReadContentAsBinaryHelper helper, XmlReader reader)
        {
            if (helper == null)
            {
                return new ReadContentAsBinaryHelper(reader);
            }
            else
            {
                helper.Reset();
                return helper;
            }
        }

        // Internal methods 

        internal int ReadContentAsBase64(byte[] buffer, int index, int count)
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
                    if (!Init())
                    {
                        return 0;
                    }
                    break;
                case State.InReadContent:
                    // if we have a correct decoder, go read
                    if (_decoder == _base64Decoder)
                    {
                        // read more binary data
                        return ReadContentAsBinary(buffer, index, count);
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
            return ReadContentAsBinary(buffer, index, count);
        }

        internal int ReadContentAsBinHex(byte[] buffer, int index, int count)
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
                    if (!Init())
                    {
                        return 0;
                    }
                    break;
                case State.InReadContent:
                    // if we have a correct decoder, go read
                    if (_decoder == _binHexDecoder)
                    {
                        // read more binary data
                        return ReadContentAsBinary(buffer, index, count);
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
            return ReadContentAsBinary(buffer, index, count);
        }

        internal int ReadElementContentAsBase64(byte[] buffer, int index, int count)
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
                    if (!InitOnElement())
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
                        return ReadElementContentAsBinary(buffer, index, count);
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
            return ReadElementContentAsBinary(buffer, index, count);
        }

        internal int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
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
                    if (!InitOnElement())
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
                        return ReadElementContentAsBinary(buffer, index, count);
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
            return ReadElementContentAsBinary(buffer, index, count);
        }

        internal void Finish()
        {
            if (_state != State.None)
            {
                while (MoveToNextContentNode(true)) { }
                if (_state == State.InReadElementContent)
                {
                    if (_reader.NodeType != XmlNodeType.EndElement)
                    {
                        throw new XmlException(SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
                    }
                    // move off the EndElement
                    _reader.Read();
                }
            }
            Reset();
        }

        internal void Reset()
        {
            _state = State.None;
            _isEnd = false;
            _valueOffset = 0;
        }

        // Private methods
        private bool Init()
        {
            // make sure we are on a content node
            if (!MoveToNextContentNode(false))
            {
                return false;
            }

            _state = State.InReadContent;
            _isEnd = false;
            return true;
        }

        private bool InitOnElement()
        {
            Debug.Assert(_reader.NodeType == XmlNodeType.Element);
            bool isEmpty = _reader.IsEmptyElement;

            // move to content or off the empty element
            _reader.Read();
            if (isEmpty)
            {
                return false;
            }

            // make sure we are on a content node
            if (!MoveToNextContentNode(false))
            {
                if (_reader.NodeType != XmlNodeType.EndElement)
                {
                    throw new XmlException(SR.Xml_InvalidNodeType, _reader.NodeType.ToString(), _reader as IXmlLineInfo);
                }
                // move off end element
                _reader.Read();
                return false;
            }
            _state = State.InReadElementContent;
            _isEnd = false;
            return true;
        }

        private void InitBase64Decoder()
        {
            if (_base64Decoder == null)
            {
                _base64Decoder = new Base64Decoder();
            }
            else
            {
                _base64Decoder.Reset();
            }
            _decoder = _base64Decoder;
        }

        private void InitBinHexDecoder()
        {
            if (_binHexDecoder == null)
            {
                _binHexDecoder = new BinHexDecoder();
            }
            else
            {
                _binHexDecoder.Reset();
            }
            _decoder = _binHexDecoder;
        }

        private int ReadContentAsBinary(byte[] buffer, int index, int count)
        {
            Debug.Assert(_decoder != null);

            if (_isEnd)
            {
                Reset();
                return 0;
            }
            _decoder.SetNextOutputBuffer(buffer, index, count);

            for (;;)
            {
                // use streaming ReadValueChunk if the reader supports it
                if (_canReadValueChunk)
                {
                    for (;;)
                    {
                        if (_valueOffset < _valueChunkLength)
                        {
                            int decodedCharsCount = _decoder.Decode(_valueChunk, _valueOffset, _valueChunkLength - _valueOffset);
                            _valueOffset += decodedCharsCount;
                        }
                        if (_decoder.IsFull)
                        {
                            return _decoder.DecodedCount;
                        }
                        Debug.Assert(_valueOffset == _valueChunkLength);
                        if ((_valueChunkLength = _reader.ReadValueChunk(_valueChunk, 0, ChunkSize)) == 0)
                        {
                            break;
                        }
                        _valueOffset = 0;
                    }
                }
                else
                {
                    // read what is reader.Value
                    string value = _reader.Value;
                    int decodedCharsCount = _decoder.Decode(value, _valueOffset, value.Length - _valueOffset);
                    _valueOffset += decodedCharsCount;

                    if (_decoder.IsFull)
                    {
                        return _decoder.DecodedCount;
                    }
                }

                _valueOffset = 0;

                // move to next textual node in the element content; throw on sub elements
                if (!MoveToNextContentNode(true))
                {
                    _isEnd = true;
                    return _decoder.DecodedCount;
                }
            }
        }

        private int ReadElementContentAsBinary(byte[] buffer, int index, int count)
        {
            if (count == 0)
            {
                return 0;
            }
            // read binary
            int decoded = ReadContentAsBinary(buffer, index, count);
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
            _reader.Read();
            _state = State.None;
            return 0;
        }

        private bool MoveToNextContentNode(bool moveIfOnContentNode)
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
            } while (_reader.Read());
            return false;
        }
    }
}
