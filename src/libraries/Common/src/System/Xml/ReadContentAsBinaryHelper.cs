// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Xml
{
    internal partial class ReadContentAsBinaryHelper
    {
        // Private enums
        enum State
        {
            None,
            InReadContent,
            InReadElementContent,
        }

        // Fields 
        XmlReader reader;
        State state;
        int valueOffset;
        bool isEnd;

        bool canReadValueChunk;
        char[] valueChunk;
        int valueChunkLength;

        IncrementalReadDecoder decoder;
        Base64Decoder base64Decoder;
        BinHexDecoder binHexDecoder;

        // Constants
        const int ChunkSize = 256;

        // Constructor
        internal ReadContentAsBinaryHelper(XmlReader reader)
        {
            this.reader = reader;
            this.canReadValueChunk = reader.CanReadValueChunk;

            if (canReadValueChunk)
            {
                valueChunk = new char[ChunkSize];
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

            switch (state)
            {
                case State.None:
                    if (!reader.CanReadContentAs())
                    {
                        throw reader.CreateReadContentAsException("ReadContentAsBase64");
                    }
                    if (!Init())
                    {
                        return 0;
                    }
                    break;
                case State.InReadContent:
                    // if we have a correct decoder, go read
                    if (decoder == base64Decoder)
                    {
                        // read more binary data
                        return ReadContentAsBinary(buffer, index, count);
                    }
                    break;
                case State.InReadElementContent:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                default:
                    Debug.Fail("Unmatched state in switch");
                    return 0;
            }

            Debug.Assert(state == State.InReadContent);

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

            switch (state)
            {
                case State.None:
                    if (!reader.CanReadContentAs())
                    {
                        throw reader.CreateReadContentAsException("ReadContentAsBinHex");
                    }
                    if (!Init())
                    {
                        return 0;
                    }
                    break;
                case State.InReadContent:
                    // if we have a correct decoder, go read
                    if (decoder == binHexDecoder)
                    {
                        // read more binary data
                        return ReadContentAsBinary(buffer, index, count);
                    }
                    break;
                case State.InReadElementContent:
                    throw new InvalidOperationException(SR.Xml_MixingBinaryContentMethods);
                default:
                    Debug.Fail("Unmatched state in switch");
                    return 0;
            }

            Debug.Assert(state == State.InReadContent);

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

            switch (state)
            {
                case State.None:
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        throw reader.CreateReadElementContentAsException("ReadElementContentAsBase64");
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
                    if (decoder == base64Decoder)
                    {
                        // read more binary data
                        return ReadElementContentAsBinary(buffer, index, count);
                    }
                    break;
                default:
                    Debug.Fail("Unmatched state in switch");
                    return 0;
            }

            Debug.Assert(state == State.InReadElementContent);

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

            switch (state)
            {
                case State.None:
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        throw reader.CreateReadElementContentAsException("ReadElementContentAsBinHex");
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
                    if (decoder == binHexDecoder)
                    {
                        // read more binary data
                        return ReadElementContentAsBinary(buffer, index, count);
                    }
                    break;
                default:
                    Debug.Fail("Unmatched state in switch");
                    return 0;
            }

            Debug.Assert(state == State.InReadElementContent);

            // setup binhex decoder
            InitBinHexDecoder();

            // read more binary data
            return ReadElementContentAsBinary(buffer, index, count);
        }

        internal void Finish()
        {
            if (state != State.None)
            {
                while (MoveToNextContentNode(true))
                    ;
                if (state == State.InReadElementContent)
                {
                    if (reader.NodeType != XmlNodeType.EndElement)
                    {
                        throw new XPath.XPathException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
                    }
                    // move off the EndElement
                    reader.Read();
                }
            }
            Reset();
        }

        internal void Reset()
        {
            state = State.None;
            isEnd = false;
            valueOffset = 0;
        }

        // Private methods
        private bool Init()
        {
            // make sure we are on a content node
            if (!MoveToNextContentNode(false))
            {
                return false;
            }

            state = State.InReadContent;
            isEnd = false;
            return true;
        }

        private bool InitOnElement()
        {
            Debug.Assert(reader.NodeType == XmlNodeType.Element);
            bool isEmpty = reader.IsEmptyElement;

            // move to content or off the empty element
            reader.Read();
            if (isEmpty)
            {
                return false;
            }

            // make sure we are on a content node
            if (!MoveToNextContentNode(false))
            {
                if (reader.NodeType != XmlNodeType.EndElement)
                {
                    throw new XPath.XPathException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
                }
                // move off end element
                reader.Read();
                return false;
            }
            state = State.InReadElementContent;
            isEnd = false;
            return true;
        }

        private void InitBase64Decoder()
        {
            if (base64Decoder == null)
            {
                base64Decoder = new Base64Decoder();
            }
            else
            {
                base64Decoder.Reset();
            }
            decoder = base64Decoder;
        }

        private void InitBinHexDecoder()
        {
            if (binHexDecoder == null)
            {
                binHexDecoder = new BinHexDecoder();
            }
            else
            {
                binHexDecoder.Reset();
            }
            decoder = binHexDecoder;
        }

        private int ReadContentAsBinary(byte[] buffer, int index, int count)
        {
            Debug.Assert(decoder != null);

            if (isEnd)
            {
                Reset();
                return 0;
            }
            decoder.SetNextOutputBuffer(buffer, index, count);

            for (; ;)
            {
                // use streaming ReadValueChunk if the reader supports it
                if (canReadValueChunk)
                {
                    for (; ;)
                    {
                        if (valueOffset < valueChunkLength)
                        {
                            int decodedCharsCount = decoder.Decode(valueChunk, valueOffset, valueChunkLength - valueOffset);
                            valueOffset += decodedCharsCount;
                        }
                        if (decoder.IsFull)
                        {
                            return decoder.DecodedCount;
                        }
                        Debug.Assert(valueOffset == valueChunkLength);
                        if ((valueChunkLength = reader.ReadValueChunk(valueChunk, 0, ChunkSize)) == 0)
                        {
                            break;
                        }
                        valueOffset = 0;
                    }
                }
                else
                {
                    // read what is reader.Value
                    string value = reader.Value;
                    int decodedCharsCount = decoder.Decode(value, valueOffset, value.Length - valueOffset);
                    valueOffset += decodedCharsCount;

                    if (decoder.IsFull)
                    {
                        return decoder.DecodedCount;
                    }
                }

                valueOffset = 0;

                // move to next textual node in the element content; throw on sub elements
                if (!MoveToNextContentNode(true))
                {
                    isEnd = true;
                    return decoder.DecodedCount;
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
            if (reader.NodeType != XmlNodeType.EndElement)
            {
                throw new XPath.XPathException(SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
            }

            // move off the EndElement
            reader.Read();
            state = State.None;
            return 0;
        }

        bool MoveToNextContentNode(bool moveIfOnContentNode)
        {
            do
            {
                switch (reader.NodeType)
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
                        if (reader.CanResolveEntity)
                        {
                            reader.ResolveEntity();
                            break;
                        }
                        goto default;
                    default:
                        return false;
                }
                moveIfOnContentNode = false;
            } while (reader.Read());
            return false;
        }
    }
}
