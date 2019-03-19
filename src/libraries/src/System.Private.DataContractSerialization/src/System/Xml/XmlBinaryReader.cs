// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization;

namespace System.Xml
{
    public interface IXmlBinaryReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count,
                            IXmlDictionary dictionary,
                            XmlDictionaryReaderQuotas quotas,
                            XmlBinaryReaderSession session,
                            OnXmlDictionaryReaderClose onClose);
        void SetInput(Stream stream,
                             IXmlDictionary dictionary,
                             XmlDictionaryReaderQuotas quotas,
                             XmlBinaryReaderSession session,
                             OnXmlDictionaryReaderClose onClose);
    }

    internal class XmlBinaryReader : XmlBaseReader, IXmlBinaryReaderInitializer
    {
        private bool _isTextWithEndElement;
        private bool _buffered;
        private ArrayState _arrayState;
        private int _arrayCount;
        private int _maxBytesPerRead;
        private XmlBinaryNodeType _arrayNodeType;

        public XmlBinaryReader()
        {
        }

        public void SetInput(byte[] buffer, int offset, int count,
                            IXmlDictionary dictionary,
                            XmlDictionaryReaderQuotas quotas,
                            XmlBinaryReaderSession session,
                            OnXmlDictionaryReaderClose onClose)
        {
            if (buffer == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(buffer));
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));
            if (offset > buffer.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > buffer.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
            MoveToInitial(quotas, session, null);
            BufferReader.SetBuffer(buffer, offset, count, dictionary, session);
            _buffered = true;
        }

        public void SetInput(Stream stream,
                             IXmlDictionary dictionary,
                            XmlDictionaryReaderQuotas quotas,
                            XmlBinaryReaderSession session,
                            OnXmlDictionaryReaderClose onClose)
        {
            if (stream == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(stream));
            MoveToInitial(quotas, session, null);
            BufferReader.SetBuffer(stream, dictionary, session);
            _buffered = false;
        }

        private void MoveToInitial(XmlDictionaryReaderQuotas quotas, XmlBinaryReaderSession session, OnXmlDictionaryReaderClose onClose)
        {
            MoveToInitial(quotas);
            _maxBytesPerRead = quotas.MaxBytesPerRead;
            _arrayState = ArrayState.None;
            _isTextWithEndElement = false;
        }

        public override void Close()
        {
            base.Close();
        }

        public override string ReadElementContentAsString()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (!CanOptimizeReadElementContent())
                return base.ReadElementContentAsString();
            string value;
            switch (GetNodeType())
            {
                case XmlBinaryNodeType.Chars8TextWithEndElement:
                    SkipNodeType();
                    value = BufferReader.ReadUTF8String(ReadUInt8());
                    ReadTextWithEndElement();
                    break;
                case XmlBinaryNodeType.DictionaryTextWithEndElement:
                    SkipNodeType();
                    value = BufferReader.GetDictionaryString(ReadDictionaryKey()).Value;
                    ReadTextWithEndElement();
                    break;
                default:
                    value = base.ReadElementContentAsString();
                    break;
            }
            if (value.Length > Quotas.MaxStringContentLength)
                XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, Quotas.MaxStringContentLength);
            return value;
        }

        public override bool ReadElementContentAsBoolean()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (!CanOptimizeReadElementContent())
                return base.ReadElementContentAsBoolean();
            bool value;
            switch (GetNodeType())
            {
                case XmlBinaryNodeType.TrueTextWithEndElement:
                    SkipNodeType();
                    value = true;
                    ReadTextWithEndElement();
                    break;
                case XmlBinaryNodeType.FalseTextWithEndElement:
                    SkipNodeType();
                    value = false;
                    ReadTextWithEndElement();
                    break;
                case XmlBinaryNodeType.BoolTextWithEndElement:
                    SkipNodeType();
                    value = (BufferReader.ReadUInt8() != 0);
                    ReadTextWithEndElement();
                    break;
                default:
                    value = base.ReadElementContentAsBoolean();
                    break;
            }
            return value;
        }

        public override int ReadElementContentAsInt()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (!CanOptimizeReadElementContent())
                return base.ReadElementContentAsInt();
            int value;
            switch (GetNodeType())
            {
                case XmlBinaryNodeType.ZeroTextWithEndElement:
                    SkipNodeType();
                    value = 0;
                    ReadTextWithEndElement();
                    break;
                case XmlBinaryNodeType.OneTextWithEndElement:
                    SkipNodeType();
                    value = 1;
                    ReadTextWithEndElement();
                    break;
                case XmlBinaryNodeType.Int8TextWithEndElement:
                    SkipNodeType();
                    value = BufferReader.ReadInt8();
                    ReadTextWithEndElement();
                    break;
                case XmlBinaryNodeType.Int16TextWithEndElement:
                    SkipNodeType();
                    value = BufferReader.ReadInt16();
                    ReadTextWithEndElement();
                    break;
                case XmlBinaryNodeType.Int32TextWithEndElement:
                    SkipNodeType();
                    value = BufferReader.ReadInt32();
                    ReadTextWithEndElement();
                    break;
                default:
                    value = base.ReadElementContentAsInt();
                    break;
            }
            return value;
        }

        private bool CanOptimizeReadElementContent()
        {
            return (_arrayState == ArrayState.None && !Signing);
        }

        public override float ReadElementContentAsFloat()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.FloatTextWithEndElement)
            {
                SkipNodeType();
                float value = BufferReader.ReadSingle();
                ReadTextWithEndElement();
                return value;
            }
            return base.ReadElementContentAsFloat();
        }

        public override double ReadElementContentAsDouble()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.DoubleTextWithEndElement)
            {
                SkipNodeType();
                double value = BufferReader.ReadDouble();
                ReadTextWithEndElement();
                return value;
            }
            return base.ReadElementContentAsDouble();
        }

        public override decimal ReadElementContentAsDecimal()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.DecimalTextWithEndElement)
            {
                SkipNodeType();
                decimal value = BufferReader.ReadDecimal();
                ReadTextWithEndElement();
                return value;
            }
            return base.ReadElementContentAsDecimal();
        }

        public override DateTime ReadElementContentAsDateTime()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.DateTimeTextWithEndElement)
            {
                SkipNodeType();
                DateTime value = BufferReader.ReadDateTime();
                ReadTextWithEndElement();
                return value;
            }
            return base.ReadElementContentAsDateTime();
        }

        public override TimeSpan ReadElementContentAsTimeSpan()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.TimeSpanTextWithEndElement)
            {
                SkipNodeType();
                TimeSpan value = BufferReader.ReadTimeSpan();
                ReadTextWithEndElement();
                return value;
            }
            return base.ReadElementContentAsTimeSpan();
        }

        public override Guid ReadElementContentAsGuid()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.GuidTextWithEndElement)
            {
                SkipNodeType();
                Guid value = BufferReader.ReadGuid();
                ReadTextWithEndElement();
                return value;
            }
            return base.ReadElementContentAsGuid();
        }

        public override UniqueId ReadElementContentAsUniqueId()
        {
            if (this.Node.NodeType != XmlNodeType.Element)
                MoveToStartElement();
            if (CanOptimizeReadElementContent() && GetNodeType() == XmlBinaryNodeType.UniqueIdTextWithEndElement)
            {
                SkipNodeType();
                UniqueId value = BufferReader.ReadUniqueId();
                ReadTextWithEndElement();
                return value;
            }
            return base.ReadElementContentAsUniqueId();
        }

        public override bool TryGetBase64ContentLength(out int length)
        {
            length = 0;
            if (!_buffered)
                return false;
            if (_arrayState != ArrayState.None)
                return false;
            int totalLength;
            if (!this.Node.Value.TryGetByteArrayLength(out totalLength))
                return false;
            int offset = BufferReader.Offset;
            try
            {
                bool done = false;
                while (!done && !BufferReader.EndOfFile)
                {
                    XmlBinaryNodeType nodeType = GetNodeType();
                    SkipNodeType();
                    int actual;
                    switch (nodeType)
                    {
                        case XmlBinaryNodeType.Bytes8TextWithEndElement:
                            actual = BufferReader.ReadUInt8();
                            done = true;
                            break;
                        case XmlBinaryNodeType.Bytes16TextWithEndElement:
                            actual = BufferReader.ReadUInt16();
                            done = true;
                            break;
                        case XmlBinaryNodeType.Bytes32TextWithEndElement:
                            actual = BufferReader.ReadUInt31();
                            done = true;
                            break;
                        case XmlBinaryNodeType.EndElement:
                            actual = 0;
                            done = true;
                            break;
                        case XmlBinaryNodeType.Bytes8Text:
                            actual = BufferReader.ReadUInt8();
                            break;
                        case XmlBinaryNodeType.Bytes16Text:
                            actual = BufferReader.ReadUInt16();
                            break;
                        case XmlBinaryNodeType.Bytes32Text:
                            actual = BufferReader.ReadUInt31();
                            break;
                        default:
                            // Non-optimal or unexpected node - fallback
                            return false;
                    }
                    BufferReader.Advance(actual);
                    if (totalLength > int.MaxValue - actual)
                        return false;
                    totalLength += actual;
                }
                length = totalLength;
                return true;
            }
            finally
            {
                BufferReader.Offset = offset;
            }
        }

        private void ReadTextWithEndElement()
        {
            ExitScope();
            ReadNode();
        }

        private XmlAtomicTextNode MoveToAtomicTextWithEndElement()
        {
            _isTextWithEndElement = true;
            return MoveToAtomicText();
        }

        public override bool Read()
        {
            if (this.Node.ReadState == ReadState.Closed)
                return false;

            SignNode();
            if (_isTextWithEndElement)
            {
                _isTextWithEndElement = false;
                MoveToEndElement();
                return true;
            }
            if (_arrayState == ArrayState.Content)
            {
                if (_arrayCount != 0)
                {
                    MoveToArrayElement();
                    return true;
                }
                _arrayState = ArrayState.None;
            }
            if (this.Node.ExitScope)
            {
                ExitScope();
            }
            return ReadNode();
        }

        private bool ReadNode()
        {
            if (!_buffered)
                BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead);

            if (BufferReader.EndOfFile)
            {
                MoveToEndOfFile();
                return false;
            }

            XmlBinaryNodeType nodeType;
            if (_arrayState == ArrayState.None)
            {
                nodeType = GetNodeType();
                SkipNodeType();
            }
            else
            {
                DiagnosticUtility.DebugAssert(_arrayState == ArrayState.Element, "");
                nodeType = _arrayNodeType;
                _arrayCount--;
                _arrayState = ArrayState.Content;
            }

            XmlElementNode elementNode;
            PrefixHandleType prefix;
            switch (nodeType)
            {
                case XmlBinaryNodeType.ShortElement:
                    elementNode = EnterScope();
                    elementNode.Prefix.SetValue(PrefixHandleType.Empty);
                    ReadName(elementNode.LocalName);
                    ReadAttributes();
                    elementNode.Namespace = LookupNamespace(PrefixHandleType.Empty);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.Element:
                    elementNode = EnterScope();
                    ReadName(elementNode.Prefix);
                    ReadName(elementNode.LocalName);
                    ReadAttributes();
                    elementNode.Namespace = LookupNamespace(elementNode.Prefix);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.ShortDictionaryElement:
                    elementNode = EnterScope();
                    elementNode.Prefix.SetValue(PrefixHandleType.Empty);
                    ReadDictionaryName(elementNode.LocalName);
                    ReadAttributes();
                    elementNode.Namespace = LookupNamespace(PrefixHandleType.Empty);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.DictionaryElement:
                    elementNode = EnterScope();
                    ReadName(elementNode.Prefix);
                    ReadDictionaryName(elementNode.LocalName);
                    ReadAttributes();
                    elementNode.Namespace = LookupNamespace(elementNode.Prefix);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.PrefixElementA:
                case XmlBinaryNodeType.PrefixElementB:
                case XmlBinaryNodeType.PrefixElementC:
                case XmlBinaryNodeType.PrefixElementD:
                case XmlBinaryNodeType.PrefixElementE:
                case XmlBinaryNodeType.PrefixElementF:
                case XmlBinaryNodeType.PrefixElementG:
                case XmlBinaryNodeType.PrefixElementH:
                case XmlBinaryNodeType.PrefixElementI:
                case XmlBinaryNodeType.PrefixElementJ:
                case XmlBinaryNodeType.PrefixElementK:
                case XmlBinaryNodeType.PrefixElementL:
                case XmlBinaryNodeType.PrefixElementM:
                case XmlBinaryNodeType.PrefixElementN:
                case XmlBinaryNodeType.PrefixElementO:
                case XmlBinaryNodeType.PrefixElementP:
                case XmlBinaryNodeType.PrefixElementQ:
                case XmlBinaryNodeType.PrefixElementR:
                case XmlBinaryNodeType.PrefixElementS:
                case XmlBinaryNodeType.PrefixElementT:
                case XmlBinaryNodeType.PrefixElementU:
                case XmlBinaryNodeType.PrefixElementV:
                case XmlBinaryNodeType.PrefixElementW:
                case XmlBinaryNodeType.PrefixElementX:
                case XmlBinaryNodeType.PrefixElementY:
                case XmlBinaryNodeType.PrefixElementZ:
                    elementNode = EnterScope();
                    prefix = PrefixHandle.GetAlphaPrefix((int)nodeType - (int)XmlBinaryNodeType.PrefixElementA);
                    elementNode.Prefix.SetValue(prefix);
                    ReadName(elementNode.LocalName);
                    ReadAttributes();
                    elementNode.Namespace = LookupNamespace(prefix);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.PrefixDictionaryElementA:
                case XmlBinaryNodeType.PrefixDictionaryElementB:
                case XmlBinaryNodeType.PrefixDictionaryElementC:
                case XmlBinaryNodeType.PrefixDictionaryElementD:
                case XmlBinaryNodeType.PrefixDictionaryElementE:
                case XmlBinaryNodeType.PrefixDictionaryElementF:
                case XmlBinaryNodeType.PrefixDictionaryElementG:
                case XmlBinaryNodeType.PrefixDictionaryElementH:
                case XmlBinaryNodeType.PrefixDictionaryElementI:
                case XmlBinaryNodeType.PrefixDictionaryElementJ:
                case XmlBinaryNodeType.PrefixDictionaryElementK:
                case XmlBinaryNodeType.PrefixDictionaryElementL:
                case XmlBinaryNodeType.PrefixDictionaryElementM:
                case XmlBinaryNodeType.PrefixDictionaryElementN:
                case XmlBinaryNodeType.PrefixDictionaryElementO:
                case XmlBinaryNodeType.PrefixDictionaryElementP:
                case XmlBinaryNodeType.PrefixDictionaryElementQ:
                case XmlBinaryNodeType.PrefixDictionaryElementR:
                case XmlBinaryNodeType.PrefixDictionaryElementS:
                case XmlBinaryNodeType.PrefixDictionaryElementT:
                case XmlBinaryNodeType.PrefixDictionaryElementU:
                case XmlBinaryNodeType.PrefixDictionaryElementV:
                case XmlBinaryNodeType.PrefixDictionaryElementW:
                case XmlBinaryNodeType.PrefixDictionaryElementX:
                case XmlBinaryNodeType.PrefixDictionaryElementY:
                case XmlBinaryNodeType.PrefixDictionaryElementZ:
                    elementNode = EnterScope();
                    prefix = PrefixHandle.GetAlphaPrefix((int)nodeType - (int)XmlBinaryNodeType.PrefixDictionaryElementA);
                    elementNode.Prefix.SetValue(prefix);
                    ReadDictionaryName(elementNode.LocalName);
                    ReadAttributes();
                    elementNode.Namespace = LookupNamespace(prefix);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.EndElement:
                    MoveToEndElement();
                    return true;
                case XmlBinaryNodeType.Comment:
                    ReadName(MoveToComment().Value);
                    return true;
                case XmlBinaryNodeType.EmptyTextWithEndElement:
                    MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.Empty);
                    if (this.OutsideRootElement)
                        VerifyWhitespace();
                    return true;
                case XmlBinaryNodeType.ZeroTextWithEndElement:
                    MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.Zero);
                    if (this.OutsideRootElement)
                        VerifyWhitespace();
                    return true;
                case XmlBinaryNodeType.OneTextWithEndElement:
                    MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.One);
                    if (this.OutsideRootElement)
                        VerifyWhitespace();
                    return true;
                case XmlBinaryNodeType.TrueTextWithEndElement:
                    MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.True);
                    if (this.OutsideRootElement)
                        VerifyWhitespace();
                    return true;
                case XmlBinaryNodeType.FalseTextWithEndElement:
                    MoveToAtomicTextWithEndElement().Value.SetValue(ValueHandleType.False);
                    if (this.OutsideRootElement)
                        VerifyWhitespace();
                    return true;
                case XmlBinaryNodeType.BoolTextWithEndElement:
                    MoveToAtomicTextWithEndElement().Value.SetValue(ReadUInt8() != 0 ? ValueHandleType.True : ValueHandleType.False);
                    if (this.OutsideRootElement)
                        VerifyWhitespace();
                    return true;
                case XmlBinaryNodeType.Chars8TextWithEndElement:
                    if (_buffered)
                        ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, ReadUInt8());
                    else
                        ReadPartialUTF8Text(true, ReadUInt8());
                    return true;
                case XmlBinaryNodeType.Chars8Text:
                    if (_buffered)
                        ReadText(MoveToComplexText(), ValueHandleType.UTF8, ReadUInt8());
                    else
                        ReadPartialUTF8Text(false, ReadUInt8());
                    return true;
                case XmlBinaryNodeType.Chars16TextWithEndElement:
                    if (_buffered)
                        ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, ReadUInt16());
                    else
                        ReadPartialUTF8Text(true, ReadUInt16());
                    return true;
                case XmlBinaryNodeType.Chars16Text:
                    if (_buffered)
                        ReadText(MoveToComplexText(), ValueHandleType.UTF8, ReadUInt16());
                    else
                        ReadPartialUTF8Text(false, ReadUInt16());
                    return true;
                case XmlBinaryNodeType.Chars32TextWithEndElement:
                    if (_buffered)
                        ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, ReadUInt31());
                    else
                        ReadPartialUTF8Text(true, ReadUInt31());
                    return true;
                case XmlBinaryNodeType.Chars32Text:
                    if (_buffered)
                        ReadText(MoveToComplexText(), ValueHandleType.UTF8, ReadUInt31());
                    else
                        ReadPartialUTF8Text(false, ReadUInt31());
                    return true;
                case XmlBinaryNodeType.UnicodeChars8TextWithEndElement:
                    ReadUnicodeText(true, ReadUInt8());
                    return true;
                case XmlBinaryNodeType.UnicodeChars8Text:
                    ReadUnicodeText(false, ReadUInt8());
                    return true;
                case XmlBinaryNodeType.UnicodeChars16TextWithEndElement:
                    ReadUnicodeText(true, ReadUInt16());
                    return true;
                case XmlBinaryNodeType.UnicodeChars16Text:
                    ReadUnicodeText(false, ReadUInt16());
                    return true;
                case XmlBinaryNodeType.UnicodeChars32TextWithEndElement:
                    ReadUnicodeText(true, ReadUInt31());
                    return true;
                case XmlBinaryNodeType.UnicodeChars32Text:
                    ReadUnicodeText(false, ReadUInt31());
                    return true;
                case XmlBinaryNodeType.Bytes8TextWithEndElement:
                    if (_buffered)
                        ReadBinaryText(MoveToAtomicTextWithEndElement(), ReadUInt8());
                    else
                        ReadPartialBinaryText(true, ReadUInt8());
                    return true;
                case XmlBinaryNodeType.Bytes8Text:
                    if (_buffered)
                        ReadBinaryText(MoveToComplexText(), ReadUInt8());
                    else
                        ReadPartialBinaryText(false, ReadUInt8());
                    return true;
                case XmlBinaryNodeType.Bytes16TextWithEndElement:
                    if (_buffered)
                        ReadBinaryText(MoveToAtomicTextWithEndElement(), ReadUInt16());
                    else
                        ReadPartialBinaryText(true, ReadUInt16());
                    return true;
                case XmlBinaryNodeType.Bytes16Text:
                    if (_buffered)
                        ReadBinaryText(MoveToComplexText(), ReadUInt16());
                    else
                        ReadPartialBinaryText(false, ReadUInt16());
                    return true;
                case XmlBinaryNodeType.Bytes32TextWithEndElement:
                    if (_buffered)
                        ReadBinaryText(MoveToAtomicTextWithEndElement(), ReadUInt31());
                    else
                        ReadPartialBinaryText(true, ReadUInt31());
                    return true;
                case XmlBinaryNodeType.Bytes32Text:
                    if (_buffered)
                        ReadBinaryText(MoveToComplexText(), ReadUInt31());
                    else
                        ReadPartialBinaryText(false, ReadUInt31());
                    return true;
                case XmlBinaryNodeType.DictionaryTextWithEndElement:
                    MoveToAtomicTextWithEndElement().Value.SetDictionaryValue(ReadDictionaryKey());
                    return true;
                case XmlBinaryNodeType.UniqueIdTextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UniqueId, ValueHandleLength.UniqueId);
                    return true;
                case XmlBinaryNodeType.GuidTextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Guid, ValueHandleLength.Guid);
                    return true;
                case XmlBinaryNodeType.DecimalTextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Decimal, ValueHandleLength.Decimal);
                    return true;
                case XmlBinaryNodeType.Int8TextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Int8, ValueHandleLength.Int8);
                    return true;
                case XmlBinaryNodeType.Int16TextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Int16, ValueHandleLength.Int16);
                    return true;
                case XmlBinaryNodeType.Int32TextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Int32, ValueHandleLength.Int32);
                    return true;
                case XmlBinaryNodeType.Int64TextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Int64, ValueHandleLength.Int64);
                    return true;
                case XmlBinaryNodeType.UInt64TextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UInt64, ValueHandleLength.UInt64);
                    return true;
                case XmlBinaryNodeType.FloatTextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Single, ValueHandleLength.Single);
                    return true;
                case XmlBinaryNodeType.DoubleTextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Double, ValueHandleLength.Double);
                    return true;
                case XmlBinaryNodeType.TimeSpanTextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.TimeSpan, ValueHandleLength.TimeSpan);
                    return true;
                case XmlBinaryNodeType.DateTimeTextWithEndElement:
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.DateTime, ValueHandleLength.DateTime);
                    return true;
                case XmlBinaryNodeType.QNameDictionaryTextWithEndElement:
                    BufferReader.ReadQName(MoveToAtomicTextWithEndElement().Value);
                    return true;
                case XmlBinaryNodeType.Array:
                    ReadArray();
                    return true;
                default:
                    BufferReader.ReadValue(nodeType, MoveToComplexText().Value);
                    return true;
            }
        }

        private void VerifyWhitespace()
        {
            if (!this.Node.Value.IsWhitespace())
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
        }

        private void ReadAttributes()
        {
            XmlBinaryNodeType nodeType = GetNodeType();
            if (nodeType < XmlBinaryNodeType.MinAttribute || nodeType > XmlBinaryNodeType.MaxAttribute)
                return;
            ReadAttributes2();
        }

        private void ReadAttributes2()
        {
            int startOffset = 0;
            if (_buffered)
                startOffset = BufferReader.Offset;

            while (true)
            {
                XmlAttributeNode attributeNode;
                Namespace nameSpace;
                PrefixHandleType prefix;
                XmlBinaryNodeType nodeType = GetNodeType();
                switch (nodeType)
                {
                    case XmlBinaryNodeType.ShortAttribute:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        attributeNode.Prefix.SetValue(PrefixHandleType.Empty);
                        ReadName(attributeNode.LocalName);
                        ReadAttributeText(attributeNode.AttributeText);
                        break;
                    case XmlBinaryNodeType.Attribute:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        ReadName(attributeNode.Prefix);
                        ReadName(attributeNode.LocalName);
                        ReadAttributeText(attributeNode.AttributeText);
                        FixXmlAttribute(attributeNode);
                        break;
                    case XmlBinaryNodeType.ShortDictionaryAttribute:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        attributeNode.Prefix.SetValue(PrefixHandleType.Empty);
                        ReadDictionaryName(attributeNode.LocalName);
                        ReadAttributeText(attributeNode.AttributeText);
                        break;
                    case XmlBinaryNodeType.DictionaryAttribute:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        ReadName(attributeNode.Prefix);
                        ReadDictionaryName(attributeNode.LocalName);
                        ReadAttributeText(attributeNode.AttributeText);
                        break;
                    case XmlBinaryNodeType.XmlnsAttribute:
                        SkipNodeType();
                        nameSpace = AddNamespace();
                        ReadName(nameSpace.Prefix);
                        ReadName(nameSpace.Uri);
                        attributeNode = AddXmlnsAttribute(nameSpace);
                        break;
                    case XmlBinaryNodeType.ShortXmlnsAttribute:
                        SkipNodeType();
                        nameSpace = AddNamespace();
                        nameSpace.Prefix.SetValue(PrefixHandleType.Empty);
                        ReadName(nameSpace.Uri);
                        attributeNode = AddXmlnsAttribute(nameSpace);
                        break;
                    case XmlBinaryNodeType.ShortDictionaryXmlnsAttribute:
                        SkipNodeType();
                        nameSpace = AddNamespace();
                        nameSpace.Prefix.SetValue(PrefixHandleType.Empty);
                        ReadDictionaryName(nameSpace.Uri);
                        attributeNode = AddXmlnsAttribute(nameSpace);
                        break;
                    case XmlBinaryNodeType.DictionaryXmlnsAttribute:
                        SkipNodeType();
                        nameSpace = AddNamespace();
                        ReadName(nameSpace.Prefix);
                        ReadDictionaryName(nameSpace.Uri);
                        attributeNode = AddXmlnsAttribute(nameSpace);
                        break;
                    case XmlBinaryNodeType.PrefixDictionaryAttributeA:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeB:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeC:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeD:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeE:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeF:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeG:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeH:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeI:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeJ:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeK:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeL:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeM:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeN:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeO:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeP:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeQ:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeR:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeS:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeT:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeU:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeV:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeW:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeX:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeY:
                    case XmlBinaryNodeType.PrefixDictionaryAttributeZ:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        prefix = PrefixHandle.GetAlphaPrefix((int)nodeType - (int)XmlBinaryNodeType.PrefixDictionaryAttributeA);
                        attributeNode.Prefix.SetValue(prefix);
                        ReadDictionaryName(attributeNode.LocalName);
                        ReadAttributeText(attributeNode.AttributeText);
                        break;
                    case XmlBinaryNodeType.PrefixAttributeA:
                    case XmlBinaryNodeType.PrefixAttributeB:
                    case XmlBinaryNodeType.PrefixAttributeC:
                    case XmlBinaryNodeType.PrefixAttributeD:
                    case XmlBinaryNodeType.PrefixAttributeE:
                    case XmlBinaryNodeType.PrefixAttributeF:
                    case XmlBinaryNodeType.PrefixAttributeG:
                    case XmlBinaryNodeType.PrefixAttributeH:
                    case XmlBinaryNodeType.PrefixAttributeI:
                    case XmlBinaryNodeType.PrefixAttributeJ:
                    case XmlBinaryNodeType.PrefixAttributeK:
                    case XmlBinaryNodeType.PrefixAttributeL:
                    case XmlBinaryNodeType.PrefixAttributeM:
                    case XmlBinaryNodeType.PrefixAttributeN:
                    case XmlBinaryNodeType.PrefixAttributeO:
                    case XmlBinaryNodeType.PrefixAttributeP:
                    case XmlBinaryNodeType.PrefixAttributeQ:
                    case XmlBinaryNodeType.PrefixAttributeR:
                    case XmlBinaryNodeType.PrefixAttributeS:
                    case XmlBinaryNodeType.PrefixAttributeT:
                    case XmlBinaryNodeType.PrefixAttributeU:
                    case XmlBinaryNodeType.PrefixAttributeV:
                    case XmlBinaryNodeType.PrefixAttributeW:
                    case XmlBinaryNodeType.PrefixAttributeX:
                    case XmlBinaryNodeType.PrefixAttributeY:
                    case XmlBinaryNodeType.PrefixAttributeZ:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        prefix = PrefixHandle.GetAlphaPrefix((int)nodeType - (int)XmlBinaryNodeType.PrefixAttributeA);
                        attributeNode.Prefix.SetValue(prefix);
                        ReadName(attributeNode.LocalName);
                        ReadAttributeText(attributeNode.AttributeText);
                        break;
                    default:
                        ProcessAttributes();
                        return;
                }
            }
        }

        private void ReadText(XmlTextNode textNode, ValueHandleType type, int length)
        {
            int offset = BufferReader.ReadBytes(length);
            textNode.Value.SetValue(type, offset, length);
            if (this.OutsideRootElement)
                VerifyWhitespace();
        }

        private void ReadBinaryText(XmlTextNode textNode, int length)
        {
            ReadText(textNode, ValueHandleType.Base64, length);
        }

        private void ReadPartialUTF8Text(bool withEndElement, int length)
        {
            // The maxBytesPerRead includes the quota for the XmlBinaryNodeType.TextNode, so we need
            // to account for that.
            const int maxTextNodeLength = 5;
            int maxLength = Math.Max(_maxBytesPerRead - maxTextNodeLength, 0);
            if (length <= maxLength)
            {
                if (withEndElement)
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, length);
                else
                    ReadText(MoveToComplexText(), ValueHandleType.UTF8, length);
            }
            else
            {
                // We also need to make sure we have enough room to insert a new XmlBinaryNodeType.TextNode
                // for the split data.
                int actual = Math.Max(maxLength - maxTextNodeLength, 0);
                int offset = BufferReader.ReadBytes(actual);

                // We need to make sure we don't split a utf8  character, so scan backwards for a 
                // character boundary.  We'll actually always push off at least one character since 
                // although we find the character boundary, we don't bother to figure out if we have 
                // all the bytes that comprise the character.
                int i;
                for (i = offset + actual - 1; i >= offset; i--)
                {
                    byte b = BufferReader.GetByte(i);
                    // The first byte of UTF8 character sequence has either the high bit off, or the
                    // two high bits set.
                    if ((b & 0x80) == 0 || (b & 0xC0) == 0xC0)
                        break;
                }

                // Move any split characters so we can insert the node
                int byteCount = (offset + actual - i);

                // Include the split characters in the count
                BufferReader.Offset = BufferReader.Offset - byteCount;
                actual -= byteCount;
                MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, actual);
                if (this.OutsideRootElement)
                    VerifyWhitespace();

                XmlBinaryNodeType nodeType = (withEndElement ? XmlBinaryNodeType.Chars32TextWithEndElement : XmlBinaryNodeType.Chars32Text);
                InsertNode(nodeType, length - actual);
            }
        }

        private void ReadUnicodeText(bool withEndElement, int length)
        {
            if ((length & 1) != 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            if (_buffered)
            {
                if (withEndElement)
                {
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Unicode, length);
                }
                else
                {
                    ReadText(MoveToComplexText(), ValueHandleType.Unicode, length);
                }
            }
            else
            {
                ReadPartialUnicodeText(withEndElement, length);
            }
        }

        private void ReadPartialUnicodeText(bool withEndElement, int length)
        {
            // The maxBytesPerRead includes the quota for the XmlBinaryNodeType.TextNode, so we need
            // to account for that.
            const int maxTextNodeLength = 5;
            int maxLength = Math.Max(_maxBytesPerRead - maxTextNodeLength, 0);
            if (length <= maxLength)
            {
                if (withEndElement)
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Unicode, length);
                else
                    ReadText(MoveToComplexText(), ValueHandleType.Unicode, length);
            }
            else
            {
                // We also need to make sure we have enough room to insert a new XmlBinaryNodeType.TextNode
                // for the split data.
                int actual = Math.Max(maxLength - maxTextNodeLength, 0);

                // Make sure we break on a char boundary
                if ((actual & 1) != 0)
                    actual--;

                int offset = BufferReader.ReadBytes(actual);

                // We need to make sure we don't split a Unicode surrogate character
                int byteCount = 0;
                char ch = (char)BufferReader.GetInt16(offset + actual - sizeof(char));
                // If the last char is a high surrogate char, then move back
                if (ch >= 0xD800 && ch < 0xDC00)
                    byteCount = sizeof(char);

                // Include the split characters in the count
                BufferReader.Offset = BufferReader.Offset - byteCount;
                actual -= byteCount;
                MoveToComplexText().Value.SetValue(ValueHandleType.Unicode, offset, actual);
                if (this.OutsideRootElement)
                    VerifyWhitespace();

                XmlBinaryNodeType nodeType = (withEndElement ? XmlBinaryNodeType.UnicodeChars32TextWithEndElement : XmlBinaryNodeType.UnicodeChars32Text);
                InsertNode(nodeType, length - actual);
            }
        }

        private void ReadPartialBinaryText(bool withEndElement, int length)
        {
            const int nodeLength = 5;
            int maxBytesPerRead = Math.Max(_maxBytesPerRead - nodeLength, 0);
            if (length <= maxBytesPerRead)
            {
                if (withEndElement)
                    ReadText(MoveToAtomicTextWithEndElement(), ValueHandleType.Base64, length);
                else
                    ReadText(MoveToComplexText(), ValueHandleType.Base64, length);
            }
            else
            {
                int actual = maxBytesPerRead;
                if (actual > 3)
                    actual -= (actual % 3);
                ReadText(MoveToComplexText(), ValueHandleType.Base64, actual);
                XmlBinaryNodeType nodeType = (withEndElement ? XmlBinaryNodeType.Bytes32TextWithEndElement : XmlBinaryNodeType.Bytes32Text);
                InsertNode(nodeType, length - actual);
            }
        }

        private void InsertNode(XmlBinaryNodeType nodeType, int length)
        {
            byte[] buffer = new byte[5];
            buffer[0] = (byte)nodeType;
            unchecked
            {
                buffer[1] = (byte)length;
                length >>= 8;
                buffer[2] = (byte)length;
                length >>= 8;
                buffer[3] = (byte)length;
            }
            length >>= 8;
            buffer[4] = (byte)length;
            BufferReader.InsertBytes(buffer, 0, buffer.Length);
        }

        private void ReadAttributeText(XmlAttributeTextNode textNode)
        {
            XmlBinaryNodeType nodeType = GetNodeType();
            SkipNodeType();
            BufferReader.ReadValue(nodeType, textNode.Value);
        }

        private void ReadName(ValueHandle value)
        {
            int length = ReadMultiByteUInt31();
            int offset = BufferReader.ReadBytes(length);
            value.SetValue(ValueHandleType.UTF8, offset, length);
        }

        private void ReadName(StringHandle handle)
        {
            int length = ReadMultiByteUInt31();
            int offset = BufferReader.ReadBytes(length);
            handle.SetValue(offset, length);
        }

        private void ReadName(PrefixHandle prefix)
        {
            int length = ReadMultiByteUInt31();
            int offset = BufferReader.ReadBytes(length);
            prefix.SetValue(offset, length);
        }

        private void ReadDictionaryName(StringHandle s)
        {
            int key = ReadDictionaryKey();
            s.SetValue(key);
        }

        private XmlBinaryNodeType GetNodeType()
        {
            return BufferReader.GetNodeType();
        }

        private void SkipNodeType()
        {
            BufferReader.SkipNodeType();
        }

        private int ReadDictionaryKey()
        {
            return BufferReader.ReadDictionaryKey();
        }

        private int ReadMultiByteUInt31()
        {
            return BufferReader.ReadMultiByteUInt31();
        }

        private int ReadUInt8()
        {
            return BufferReader.ReadUInt8();
        }

        private int ReadUInt16()
        {
            return BufferReader.ReadUInt16();
        }

        private int ReadUInt31()
        {
            return BufferReader.ReadUInt31();
        }

        private bool IsValidArrayType(XmlBinaryNodeType nodeType)
        {
            switch (nodeType)
            {
                case XmlBinaryNodeType.BoolTextWithEndElement:
                case XmlBinaryNodeType.Int16TextWithEndElement:
                case XmlBinaryNodeType.Int32TextWithEndElement:
                case XmlBinaryNodeType.Int64TextWithEndElement:
                case XmlBinaryNodeType.FloatTextWithEndElement:
                case XmlBinaryNodeType.DoubleTextWithEndElement:
                case XmlBinaryNodeType.DecimalTextWithEndElement:
                case XmlBinaryNodeType.DateTimeTextWithEndElement:
                case XmlBinaryNodeType.TimeSpanTextWithEndElement:
                case XmlBinaryNodeType.GuidTextWithEndElement:
                    return true;
                default:
                    return false;
            }
        }

        private void ReadArray()
        {
            if (GetNodeType() == XmlBinaryNodeType.Array) // Prevent recursion
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            ReadNode(); // ReadStartElement
            if (this.Node.NodeType != XmlNodeType.Element)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            if (GetNodeType() == XmlBinaryNodeType.Array) // Prevent recursion
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            ReadNode(); // ReadEndElement
            if (this.Node.NodeType != XmlNodeType.EndElement)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            _arrayState = ArrayState.Element;
            _arrayNodeType = GetNodeType();
            if (!IsValidArrayType(_arrayNodeType))
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            SkipNodeType();
            _arrayCount = ReadMultiByteUInt31();
            if (_arrayCount == 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            MoveToArrayElement();
        }

        private void MoveToArrayElement()
        {
            _arrayState = ArrayState.Element;
            MoveToNode(ElementNode);
        }

        private void SkipArrayElements(int count)
        {
            _arrayCount -= count;
            if (_arrayCount == 0)
            {
                _arrayState = ArrayState.None;
                ExitScope();
                ReadNode();
            }
        }

        public override bool IsStartArray(out Type type)
        {
            type = null;
            if (_arrayState != ArrayState.Element)
                return false;
            switch (_arrayNodeType)
            {
                case XmlBinaryNodeType.BoolTextWithEndElement:
                    type = typeof(bool);
                    break;
                case XmlBinaryNodeType.Int16TextWithEndElement:
                    type = typeof(short);
                    break;
                case XmlBinaryNodeType.Int32TextWithEndElement:
                    type = typeof(int);
                    break;
                case XmlBinaryNodeType.Int64TextWithEndElement:
                    type = typeof(long);
                    break;
                case XmlBinaryNodeType.FloatTextWithEndElement:
                    type = typeof(float);
                    break;
                case XmlBinaryNodeType.DoubleTextWithEndElement:
                    type = typeof(double);
                    break;
                case XmlBinaryNodeType.DecimalTextWithEndElement:
                    type = typeof(decimal);
                    break;
                case XmlBinaryNodeType.DateTimeTextWithEndElement:
                    type = typeof(DateTime);
                    break;
                case XmlBinaryNodeType.GuidTextWithEndElement:
                    type = typeof(Guid);
                    break;
                case XmlBinaryNodeType.TimeSpanTextWithEndElement:
                    type = typeof(TimeSpan);
                    break;
                case XmlBinaryNodeType.UniqueIdTextWithEndElement:
                    type = typeof(UniqueId);
                    break;
                default:
                    return false;
            }
            return true;
        }

        public override bool TryGetArrayLength(out int count)
        {
            count = 0;
            if (!_buffered)
                return false;
            if (_arrayState != ArrayState.Element)
                return false;
            count = _arrayCount;
            return true;
        }

        private bool IsStartArray(string localName, string namespaceUri, XmlBinaryNodeType nodeType)
        {
            return IsStartElement(localName, namespaceUri) && _arrayState == ArrayState.Element && _arrayNodeType == nodeType && !Signing;
        }

        private bool IsStartArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, XmlBinaryNodeType nodeType)
        {
            return IsStartElement(localName, namespaceUri) && _arrayState == ArrayState.Element && _arrayNodeType == nodeType && !Signing;
        }

        private void CheckArray(Array array, int offset, int count)
        {
            if (array == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(array)));
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));
            if (offset > array.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, array.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > array.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, array.Length - offset)));
        }

        private unsafe int ReadArray(bool[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            fixed (bool* items = &array[offset])
            {
                BufferReader.UnsafeReadArray((byte*)items, (byte*)&items[actual]);
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, bool[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.BoolTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, bool[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.BoolTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private unsafe int ReadArray(short[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            fixed (short* items = &array[offset])
            {
                BufferReader.UnsafeReadArray((byte*)items, (byte*)&items[actual]);
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, short[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int16TextWithEndElement) && BitConverter.IsLittleEndian)
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int16TextWithEndElement) && BitConverter.IsLittleEndian)
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private unsafe int ReadArray(int[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            fixed (int* items = &array[offset])
            {
                BufferReader.UnsafeReadArray((byte*)items, (byte*)&items[actual]);
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, int[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int32TextWithEndElement) && BitConverter.IsLittleEndian)
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int32TextWithEndElement) && BitConverter.IsLittleEndian)
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private unsafe int ReadArray(long[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            fixed (long* items = &array[offset])
            {
                BufferReader.UnsafeReadArray((byte*)items, (byte*)&items[actual]);
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, long[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int64TextWithEndElement) && BitConverter.IsLittleEndian)
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int64TextWithEndElement) && BitConverter.IsLittleEndian)
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private unsafe int ReadArray(float[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            fixed (float* items = &array[offset])
            {
                BufferReader.UnsafeReadArray((byte*)items, (byte*)&items[actual]);
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, float[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.FloatTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.FloatTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private unsafe int ReadArray(double[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            fixed (double* items = &array[offset])
            {
                BufferReader.UnsafeReadArray((byte*)items, (byte*)&items[actual]);
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, double[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DoubleTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DoubleTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private unsafe int ReadArray(decimal[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            fixed (decimal* items = &array[offset])
            {
                BufferReader.UnsafeReadArray((byte*)items, (byte*)&items[actual]);
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, decimal[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DecimalTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DecimalTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        // DateTime
        private int ReadArray(DateTime[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            for (int i = 0; i < actual; i++)
            {
                array[offset + i] = BufferReader.ReadDateTime();
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, DateTime[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DateTimeTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, DateTime[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DateTimeTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        // Guid
        private int ReadArray(Guid[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            for (int i = 0; i < actual; i++)
            {
                array[offset + i] = BufferReader.ReadGuid();
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, Guid[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.GuidTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, Guid[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.GuidTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        // TimeSpan
        private int ReadArray(TimeSpan[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            for (int i = 0; i < actual; i++)
            {
                array[offset + i] = BufferReader.ReadTimeSpan();
            }
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.TimeSpanTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, TimeSpan[] array, int offset, int count)
        {
            if (IsStartArray(localName, namespaceUri, XmlBinaryNodeType.TimeSpanTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private enum ArrayState
        {
            None,
            Element,
            Content
        }

        protected override XmlSigningNodeWriter CreateSigningNodeWriter()
        {
            return new XmlSigningNodeWriter(false);
        }
    }
}
