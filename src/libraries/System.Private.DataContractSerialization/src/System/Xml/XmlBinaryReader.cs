// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml
{
    public interface IXmlBinaryReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count,
                            IXmlDictionary? dictionary,
                            XmlDictionaryReaderQuotas quotas,
                            XmlBinaryReaderSession? session,
                            OnXmlDictionaryReaderClose? onClose);
        void SetInput(Stream stream,
                             IXmlDictionary? dictionary,
                             XmlDictionaryReaderQuotas quotas,
                             XmlBinaryReaderSession? session,
                             OnXmlDictionaryReaderClose? onClose);
    }

    internal sealed partial class XmlBinaryReader : XmlBaseReader, IXmlBinaryReaderInitializer
    {
        private bool _isTextWithEndElement;
        private bool _buffered;
        private ArrayState _arrayState;
        private int _arrayCount;
        private int _maxBytesPerRead;
        private XmlBinaryNodeType _arrayNodeType;
        private OnXmlDictionaryReaderClose? _onClose;

        public XmlBinaryReader()
        {
        }

        public void SetInput(byte[] buffer, int offset, int count,
                            IXmlDictionary? dictionary,
                            XmlDictionaryReaderQuotas quotas,
                            XmlBinaryReaderSession? session,
                            OnXmlDictionaryReaderClose? onClose)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset));
            MoveToInitial(quotas, onClose);
            BufferReader.SetBuffer(buffer, offset, count, dictionary, session);
            _buffered = true;
        }

        public void SetInput(Stream stream,
                             IXmlDictionary? dictionary,
                            XmlDictionaryReaderQuotas quotas,
                            XmlBinaryReaderSession? session,
                            OnXmlDictionaryReaderClose? onClose)
        {
            ArgumentNullException.ThrowIfNull(stream);

            MoveToInitial(quotas, onClose);
            BufferReader.SetBuffer(stream, dictionary, session);
            _buffered = false;
        }

        private void MoveToInitial(XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose)
        {
            MoveToInitial(quotas);
            _maxBytesPerRead = quotas.MaxBytesPerRead;
            _arrayState = ArrayState.None;
            _onClose = onClose;
            _isTextWithEndElement = false;
        }

        public override void Close()
        {
            base.Close();
            CloseCallback();
        }

        internal override async ValueTask CloseAsync()
        {
            try
            {
                await base.CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                CloseCallback();
            }
        }

        private void CloseCallback()
        {
            OnXmlDictionaryReaderClose? onClose = _onClose;
            _onClose = null;
            onClose?.Invoke(this);
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
            return ReadPrimitiveElementContentSync<bool>();
        }

        internal ValueTask<string> ReadStringElementAsync(CancellationToken cancellationToken)
        {
            CheckReadMode<AsyncXmlStreamIO>(cancellationToken);
            if (Node.NodeType == XmlNodeType.Element && CanOptimizeReadElementContent())
            {
                if (!_buffered)
                    BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, deferCompaction: true);

                byte[] buffer = BufferReader.GetBuffer(out int offset, out int limit);
                string? value = null;
                if (limit - offset >= 2)
                {
                    XmlBinaryNodeType nodeType = (XmlBinaryNodeType)buffer[offset];
                    if (nodeType == XmlBinaryNodeType.Chars8TextWithEndElement && buffer[offset + 1] <= limit - offset - 2)
                    {
                        SkipNodeType();
                        value = BufferReader.ReadUTF8String(ReadUInt8());
                    }
                    else if (nodeType == XmlBinaryNodeType.DictionaryTextWithEndElement && buffer[offset + 1] < 0x80)
                    {
                        SkipNodeType();
                        value = BufferReader.GetDictionaryString(ReadDictionaryKey()).Value;
                    }
                }

                if (value is not null)
                {
                    ExitScope();
                    ValueTask<bool> next = ReadNode<AsyncXmlStreamIO>(cancellationToken);
                    if (!next.IsCompletedSuccessfully)
                        return AwaitStringElementRead(next, value);

                    next.GetAwaiter().GetResult();
                    return new ValueTask<string>(CheckStringContentLength(value));
                }
            }

            return ReadStringElementFallbackAsync(cancellationToken);
        }

        private string CheckStringContentLength(string value)
        {
            if (value.Length > Quotas.MaxStringContentLength)
                XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, Quotas.MaxStringContentLength);

            return value;
        }

        private async ValueTask<string> AwaitStringElementRead(ValueTask<bool> next, string value)
        {
            await next.ConfigureAwait(false);
            return CheckStringContentLength(value);
        }

        private async ValueTask<string> ReadStringElementFallbackAsync(CancellationToken cancellationToken) =>
            (string)await base.ReadElementContentAsAsync(typeof(string), null, cancellationToken).ConfigureAwait(false);

        public override int ReadElementContentAsInt()
        {
            return ReadPrimitiveElementContentSync<int>();
        }

        private bool CanOptimizeReadElementContent()
        {
            return (_arrayState == ArrayState.None && !Signing);
        }

        public override float ReadElementContentAsFloat()
        {
            return ReadPrimitiveElementContentSync<float>();
        }

        public override double ReadElementContentAsDouble()
        {
            return ReadPrimitiveElementContentSync<double>();
        }

        public override decimal ReadElementContentAsDecimal()
        {
            return ReadPrimitiveElementContentSync<decimal>();
        }

        public override DateTime ReadElementContentAsDateTime()
        {
            return ReadPrimitiveElementContentSync<DateTime>();
        }

        internal override async ValueTask<object> ReadElementContentAsAsync(Type returnType, IXmlNamespaceResolver? namespaceResolver, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(returnType);
            if (returnType == typeof(bool))
                return await ReadPrimitiveElementContent<bool, AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);
            if (returnType == typeof(int))
                return await ReadPrimitiveElementContent<int, AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);
            if (returnType == typeof(long))
                return await ReadPrimitiveElementContent<long, AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);
            if (returnType == typeof(float))
                return await ReadPrimitiveElementContent<float, AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);
            if (returnType == typeof(double))
                return await ReadPrimitiveElementContent<double, AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);
            if (returnType == typeof(decimal))
                return await ReadPrimitiveElementContent<decimal, AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);
            if (returnType == typeof(DateTime))
                return await ReadPrimitiveElementContent<DateTime, AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);

            return await base.ReadElementContentAsAsync(returnType, namespaceResolver, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<T> ReadPrimitiveElementContent<T, TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            CheckReadMode<TMode>(cancellationToken);
            if (Node.NodeType != XmlNodeType.Element)
            {
                if (typeof(TMode) == typeof(SyncXmlStreamIO))
                    MoveToStartElement();
                else
                    await MoveToStartElementAsync(cancellationToken).ConfigureAwait(false);
            }
            if (CanOptimizeReadElementContent())
            {
                if (!_buffered)
                {
                    BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, typeof(TMode) == typeof(AsyncXmlStreamIO));
                }
                XmlBinaryNodeType nodeType = await GetNodeType<TMode>(cancellationToken).ConfigureAwait(false);
                int length = GetPrimitiveElementLength<T>(nodeType);
                if (length >= 0)
                {
                    if (typeof(TMode) == typeof(SyncXmlStreamIO))
                        BufferReader.GetBuffer(length + 1, out _);
                    else
                        await BufferReader.EnsureBytesAsync(length + 1, cancellationToken).ConfigureAwait(false);
                    SkipNodeType();
                    T value = ReadPrimitiveElementValue<T>(nodeType);
                    ExitScope();
                    await ReadNode<TMode>(cancellationToken).ConfigureAwait(false);
                    return value;
                }
            }
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
                return ReadPrimitiveElementFallback<T>();

            return (T)await base.ReadElementContentAsAsync(typeof(T), null, cancellationToken).ConfigureAwait(false);
        }

        internal ValueTask<T> ReadPrimitiveElementAsync<T>(CancellationToken cancellationToken)
        {
            if (TryReadBufferedPrimitiveElement(cancellationToken, out T value, out ValueTask<bool> next))
            {
                if (next.IsCompletedSuccessfully)
                {
                    next.GetAwaiter().GetResult();
                    return new ValueTask<T>(value);
                }

                return AwaitPrimitiveElementRead(next, value);
            }

            return ReadPrimitiveElementContent<T, AsyncXmlStreamIO>(cancellationToken);
        }

        internal bool TryReadBufferedPrimitiveElement<T>(CancellationToken cancellationToken, out T value, out ValueTask<bool> next)
        {
            CheckReadMode<AsyncXmlStreamIO>(cancellationToken);
            if (TryReadBufferedPrimitiveElement(out value))
            {
                next = ReadNode<AsyncXmlStreamIO>(cancellationToken);
                return true;
            }

            next = default;
            return false;
        }

        internal T ReadPrimitiveElement<T>()
        {
            CheckReadMode<SyncXmlStreamIO>(CancellationToken.None);
            if (TryReadBufferedPrimitiveElement(out T value))
            {
                Read();
                return value;
            }

            return ReadPrimitiveElementContentSync<T>();
        }

        private bool TryReadBufferedPrimitiveElement<T>(out T value)
        {
            if (Node.NodeType == XmlNodeType.Element && CanOptimizeReadElementContent())
            {
                if (!_buffered)
                    BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, deferCompaction: true);
                byte[] buffer = BufferReader.GetBuffer(out int offset, out int offsetMax);
                if (offset < offsetMax)
                {
                    XmlBinaryNodeType nodeType = (XmlBinaryNodeType)buffer[offset];
                    int length = GetPrimitiveElementLength<T>(nodeType);
                    if (length >= 0 && length < offsetMax - offset)
                    {
                        SkipNodeType();
                        value = ReadPrimitiveElementValue<T>(nodeType);
                        ExitScope();
                        return true;
                    }
                }
            }

            value = default!;
            return false;
        }

        private static async ValueTask<T> AwaitPrimitiveElementRead<T>(ValueTask<bool> next, T value)
        {
            await next.ConfigureAwait(false);
            return value;
        }

        private static int GetPrimitiveElementLength<T>(XmlBinaryNodeType nodeType)
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
            {
                return nodeType switch
                {
                    XmlBinaryNodeType.ZeroTextWithEndElement or XmlBinaryNodeType.OneTextWithEndElement => 0,
                    XmlBinaryNodeType.Int8TextWithEndElement => ValueHandleLength.Int8,
                    XmlBinaryNodeType.Int16TextWithEndElement => ValueHandleLength.Int16,
                    XmlBinaryNodeType.Int32TextWithEndElement => ValueHandleLength.Int32,
                    XmlBinaryNodeType.Int64TextWithEndElement when typeof(T) == typeof(long) => ValueHandleLength.Int64,
                    _ => -1
                };
            }
            if (typeof(T) == typeof(bool))
            {
                return nodeType switch
                {
                    XmlBinaryNodeType.TrueTextWithEndElement or XmlBinaryNodeType.FalseTextWithEndElement => 0,
                    XmlBinaryNodeType.BoolTextWithEndElement => ValueHandleLength.Int8,
                    _ => -1
                };
            }
            if (typeof(T) == typeof(float) && nodeType == XmlBinaryNodeType.FloatTextWithEndElement)
                return ValueHandleLength.Single;
            if (typeof(T) == typeof(double) && nodeType == XmlBinaryNodeType.DoubleTextWithEndElement)
                return ValueHandleLength.Double;
            if (typeof(T) == typeof(decimal) && nodeType == XmlBinaryNodeType.DecimalTextWithEndElement)
                return ValueHandleLength.Decimal;
            if (typeof(T) == typeof(DateTime) && nodeType == XmlBinaryNodeType.DateTimeTextWithEndElement)
                return ValueHandleLength.DateTime;
            return -1;
        }

        private T ReadPrimitiveElementValue<T>(XmlBinaryNodeType nodeType)
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
            {
                long value = nodeType switch
                {
                    XmlBinaryNodeType.ZeroTextWithEndElement => 0,
                    XmlBinaryNodeType.OneTextWithEndElement => 1,
                    XmlBinaryNodeType.Int8TextWithEndElement => BufferReader.ReadInt8(),
                    XmlBinaryNodeType.Int16TextWithEndElement => BufferReader.ReadInt16(),
                    XmlBinaryNodeType.Int32TextWithEndElement => BufferReader.ReadInt32(),
                    _ => BufferReader.ReadInt64()
                };
                return typeof(T) == typeof(int) ? (T)(object)(int)value : (T)(object)value;
            }
            if (typeof(T) == typeof(bool))
                return (T)(object)(nodeType == XmlBinaryNodeType.TrueTextWithEndElement ||
                    nodeType == XmlBinaryNodeType.BoolTextWithEndElement && BufferReader.ReadUInt8() != 0);
            if (typeof(T) == typeof(float))
                return (T)(object)BufferReader.ReadSingle();
            if (typeof(T) == typeof(double))
                return (T)(object)BufferReader.ReadDouble();
            if (typeof(T) == typeof(decimal))
                return (T)(object)BufferReader.ReadDecimal();

            Debug.Assert(typeof(T) == typeof(DateTime));
            return (T)(object)BufferReader.ReadDateTime();
        }

        private T ReadPrimitiveElementFallback<T>()
        {
            if (typeof(T) == typeof(bool))
                return (T)(object)base.ReadElementContentAsBoolean();
            if (typeof(T) == typeof(int))
                return (T)(object)base.ReadElementContentAsInt();
            if (typeof(T) == typeof(long))
                return (T)(object)base.ReadElementContentAsLong();
            if (typeof(T) == typeof(float))
                return (T)(object)base.ReadElementContentAsFloat();
            if (typeof(T) == typeof(double))
                return (T)(object)base.ReadElementContentAsDouble();
            if (typeof(T) == typeof(decimal))
                return (T)(object)base.ReadElementContentAsDecimal();

            Debug.Assert(typeof(T) == typeof(DateTime));
            return (T)(object)base.ReadElementContentAsDateTime();
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

        internal override ValueTask<bool> ReadAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellationToken) :
                ReadCore<AsyncXmlStreamIO>(cancellationToken);

        private ValueTask<bool> ReadCore<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            CheckReadMode<TMode>(cancellationToken);
            if (this.Node.ReadState == ReadState.Closed)
                return new ValueTask<bool>(false);

            if (Signing)
                return ReadSignedNode<TMode>(cancellationToken);

            return ReadUnsignedNode<TMode>(cancellationToken);
        }

        private async ValueTask<bool> ReadSignedNode<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            await SignNode<TMode>(cancellationToken).ConfigureAwait(false);
            return await ReadUnsignedNode<TMode>(cancellationToken).ConfigureAwait(false);
        }

        private ValueTask<bool> ReadUnsignedNode<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (_isTextWithEndElement)
            {
                _isTextWithEndElement = false;
                MoveToEndElement();
                return new ValueTask<bool>(true);
            }
            if (_arrayState == ArrayState.Content)
            {
                if (_arrayCount != 0)
                {
                    MoveToArrayElement();
                    return new ValueTask<bool>(true);
                }
                _arrayState = ArrayState.None;
            }
            if (this.Node.ExitScope)
            {
                ExitScope();
            }
            return ReadNode<TMode>(cancellationToken);
        }

        private bool ReadNode()
        {
            return ReadNodeSync();
        }

        private ValueTask<bool> ReadNode<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (!_buffered)
                BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, typeof(TMode) == typeof(AsyncXmlStreamIO));

            if (typeof(TMode) == typeof(AsyncXmlStreamIO) && _arrayState == ArrayState.None && TryReadBufferedNode())
                return new ValueTask<bool>(true);

            return ReadNodeCore<TMode>(cancellationToken);
        }

        private bool TryReadBufferedNode()
        {
            byte[] buffer = BufferReader.GetBuffer(out int offset, out int offsetMax);
            if (offset >= offsetMax)
                return false;

            XmlBinaryNodeType nodeType = (XmlBinaryNodeType)buffer[offset];
            if (nodeType != XmlBinaryNodeType.EndElement)
            {
                bool name = nodeType == XmlBinaryNodeType.ShortElement ||
                    nodeType is >= XmlBinaryNodeType.PrefixElementA and <= XmlBinaryNodeType.PrefixElementZ;
                bool dictionaryName = nodeType == XmlBinaryNodeType.ShortDictionaryElement ||
                    nodeType is >= XmlBinaryNodeType.PrefixDictionaryElementA and <= XmlBinaryNodeType.PrefixDictionaryElementZ;
                if ((!name && !dictionaryName) || offsetMax - offset < 3 || buffer[offset + 1] >= 0x80)
                    return false;

                int contentOffset = offset + 2;
                if (name)
                    contentOffset += buffer[offset + 1];
                if (contentOffset >= offsetMax)
                    return false;
                XmlBinaryNodeType next = (XmlBinaryNodeType)buffer[contentOffset];
                if (next is >= XmlBinaryNodeType.MinAttribute and <= XmlBinaryNodeType.MaxAttribute &&
                    !AreAttributesBuffered(buffer.AsSpan(contentOffset, offsetMax - contentOffset)))
                    return false;
                if (next is < XmlBinaryNodeType.MinAttribute or > XmlBinaryNodeType.MaxAttribute)
                {
                    SkipNodeType();
                    XmlElementNode elementNode = EnterScope();
                    PrefixHandleType prefix = nodeType is XmlBinaryNodeType.ShortElement or XmlBinaryNodeType.ShortDictionaryElement
                        ? PrefixHandleType.Empty
                        : PrefixHandle.GetAlphaPrefix((int)nodeType - (int)(dictionaryName
                            ? XmlBinaryNodeType.PrefixDictionaryElementA : XmlBinaryNodeType.PrefixElementA));
                    elementNode.Prefix.SetValue(prefix);
                    if (dictionaryName)
                        elementNode.LocalName.SetValue(ReadDictionaryKey());
                    else
                        elementNode.LocalName.SetValue(offset + 2, buffer[offset + 1]);
                    BufferReader.Offset = contentOffset;
                    elementNode.Namespace = LookupNamespace(prefix);
                    elementNode.BufferOffset = contentOffset;
                    return true;
                }
            }

            // Both the name and attribute lookahead are buffered, so the shared parser cannot refill.
            SkipNodeType();
            return ReadNodeContentSync(nodeType);
        }

        private static bool AreAttributesBuffered(ReadOnlySpan<byte> buffer)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                XmlBinaryNodeType nodeType = (XmlBinaryNodeType)buffer[offset++];
                if (nodeType is < XmlBinaryNodeType.MinAttribute or > XmlBinaryNodeType.MaxAttribute)
                    return true;

                bool dictionaryName = nodeType is XmlBinaryNodeType.ShortDictionaryAttribute or
                    XmlBinaryNodeType.DictionaryAttribute or XmlBinaryNodeType.ShortDictionaryXmlnsAttribute or
                    XmlBinaryNodeType.DictionaryXmlnsAttribute or
                    >= XmlBinaryNodeType.PrefixDictionaryAttributeA and <= XmlBinaryNodeType.PrefixDictionaryAttributeZ;
                bool namespaceDeclaration = nodeType is XmlBinaryNodeType.ShortXmlnsAttribute or
                    XmlBinaryNodeType.XmlnsAttribute or XmlBinaryNodeType.ShortDictionaryXmlnsAttribute or
                    XmlBinaryNodeType.DictionaryXmlnsAttribute;
                if (nodeType is XmlBinaryNodeType.Attribute or XmlBinaryNodeType.DictionaryAttribute or
                    XmlBinaryNodeType.XmlnsAttribute or XmlBinaryNodeType.DictionaryXmlnsAttribute)
                {
                    if (!TrySkipBufferedName(buffer, ref offset, dictionaryName: false))
                        return false;
                }
                if (!TrySkipBufferedName(buffer, ref offset, dictionaryName))
                    return false;
                if (namespaceDeclaration)
                    continue;

                if (offset >= buffer.Length)
                    return false;
                XmlBinaryNodeType valueType = (XmlBinaryNodeType)buffer[offset++];
                int length = valueType switch
                {
                    XmlBinaryNodeType.EmptyText or XmlBinaryNodeType.ZeroText or XmlBinaryNodeType.OneText or
                        XmlBinaryNodeType.TrueText or XmlBinaryNodeType.FalseText => 0,
                    XmlBinaryNodeType.BoolText or XmlBinaryNodeType.Int8Text => 1,
                    XmlBinaryNodeType.Int16Text => 2,
                    XmlBinaryNodeType.Int32Text or XmlBinaryNodeType.FloatText => 4,
                    XmlBinaryNodeType.Int64Text or XmlBinaryNodeType.UInt64Text or XmlBinaryNodeType.DoubleText or
                        XmlBinaryNodeType.DateTimeText or XmlBinaryNodeType.TimeSpanText => 8,
                    XmlBinaryNodeType.DecimalText or XmlBinaryNodeType.GuidText or XmlBinaryNodeType.UniqueIdText => 16,
                    _ => -1
                };
                if (valueType == XmlBinaryNodeType.DictionaryText)
                {
                    if (!TrySkipBufferedName(buffer, ref offset, dictionaryName: true))
                        return false;
                }
                else
                {
                    if (valueType is XmlBinaryNodeType.Chars8Text or XmlBinaryNodeType.UnicodeChars8Text or XmlBinaryNodeType.Bytes8Text)
                    {
                        if (offset >= buffer.Length)
                            return false;
                        length = buffer[offset++];
                    }
                    if (length < 0 || length > buffer.Length - offset)
                        return false;
                    offset += length;
                }
            }

            return false;
        }

        private static bool TrySkipBufferedName(ReadOnlySpan<byte> buffer, ref int offset, bool dictionaryName)
        {
            if (offset >= buffer.Length || buffer[offset] >= 0x80)
                return false;
            int length = dictionaryName ? 0 : buffer[offset];
            offset++;
            if (length > buffer.Length - offset)
                return false;
            offset += length;
            return true;
        }

        private async ValueTask<bool> ReadNodeCore<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (await BufferReader.EndOfFileCore<TMode>(cancellationToken).ConfigureAwait(false))
            {
                MoveToEndOfFile();
                return false;
            }

            XmlBinaryNodeType nodeType;
            if (_arrayState == ArrayState.None)
            {
                nodeType = await GetNodeType<TMode>(cancellationToken).ConfigureAwait(false);
                SkipNodeType();
            }
            else
            {
                Debug.Assert(_arrayState == ArrayState.Element);
                nodeType = _arrayNodeType;
                _arrayCount--;
                _arrayState = ArrayState.Content;
            }

            return await ReadNodeContent<TMode>(nodeType, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<bool> ReadNodeContent<TMode>(XmlBinaryNodeType nodeType, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            XmlElementNode elementNode;
            PrefixHandleType prefix;
            switch (nodeType)
            {
                case XmlBinaryNodeType.ShortElement:
                    elementNode = EnterScope();
                    elementNode.Prefix.SetValue(PrefixHandleType.Empty);
                    await ReadName<TMode>(elementNode.LocalName, cancellationToken).ConfigureAwait(false);
                    await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
                    elementNode.Namespace = LookupNamespace(PrefixHandleType.Empty);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.Element:
                    elementNode = EnterScope();
                    await ReadName<TMode>(elementNode.Prefix, cancellationToken).ConfigureAwait(false);
                    await ReadName<TMode>(elementNode.LocalName, cancellationToken).ConfigureAwait(false);
                    await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
                    elementNode.Namespace = LookupNamespace(elementNode.Prefix);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.ShortDictionaryElement:
                    elementNode = EnterScope();
                    elementNode.Prefix.SetValue(PrefixHandleType.Empty);
                    await ReadDictionaryName<TMode>(elementNode.LocalName, cancellationToken).ConfigureAwait(false);
                    await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
                    elementNode.Namespace = LookupNamespace(PrefixHandleType.Empty);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.DictionaryElement:
                    elementNode = EnterScope();
                    await ReadName<TMode>(elementNode.Prefix, cancellationToken).ConfigureAwait(false);
                    await ReadDictionaryName<TMode>(elementNode.LocalName, cancellationToken).ConfigureAwait(false);
                    await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
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
                    await ReadName<TMode>(elementNode.LocalName, cancellationToken).ConfigureAwait(false);
                    await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
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
                    await ReadDictionaryName<TMode>(elementNode.LocalName, cancellationToken).ConfigureAwait(false);
                    await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
                    elementNode.Namespace = LookupNamespace(prefix);
                    elementNode.BufferOffset = BufferReader.Offset;
                    return true;
                case XmlBinaryNodeType.EndElement:
                    MoveToEndElement();
                    return true;
                case XmlBinaryNodeType.Comment:
                    await ReadName<TMode>(MoveToComment().Value, cancellationToken).ConfigureAwait(false);
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
                    MoveToAtomicTextWithEndElement().Value.SetValue(await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false) != 0 ? ValueHandleType.True : ValueHandleType.False);
                    if (this.OutsideRootElement)
                        VerifyWhitespace();
                    return true;
                case XmlBinaryNodeType.Chars8TextWithEndElement:
                    if (_buffered)
                        await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialUTF8Text<TMode>(true, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Chars8Text:
                    if (_buffered)
                        await ReadText<TMode>(MoveToComplexText(), ValueHandleType.UTF8, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialUTF8Text<TMode>(false, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Chars16TextWithEndElement:
                    if (_buffered)
                        await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialUTF8Text<TMode>(true, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Chars16Text:
                    if (_buffered)
                        await ReadText<TMode>(MoveToComplexText(), ValueHandleType.UTF8, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialUTF8Text<TMode>(false, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Chars32TextWithEndElement:
                    if (_buffered)
                        await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialUTF8Text<TMode>(true, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Chars32Text:
                    if (_buffered)
                        await ReadText<TMode>(MoveToComplexText(), ValueHandleType.UTF8, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialUTF8Text<TMode>(false, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.UnicodeChars8TextWithEndElement:
                    await ReadUnicodeText<TMode>(true, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.UnicodeChars8Text:
                    await ReadUnicodeText<TMode>(false, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.UnicodeChars16TextWithEndElement:
                    await ReadUnicodeText<TMode>(true, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.UnicodeChars16Text:
                    await ReadUnicodeText<TMode>(false, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.UnicodeChars32TextWithEndElement:
                    await ReadUnicodeText<TMode>(true, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.UnicodeChars32Text:
                    await ReadUnicodeText<TMode>(false, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Bytes8TextWithEndElement:
                    if (_buffered)
                        await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Base64, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialBinaryText<TMode>(true, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Bytes8Text:
                    if (_buffered)
                        await ReadText<TMode>(MoveToComplexText(), ValueHandleType.Base64, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialBinaryText<TMode>(false, await BufferReader.ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Bytes16TextWithEndElement:
                    if (_buffered)
                        await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Base64, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialBinaryText<TMode>(true, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Bytes16Text:
                    if (_buffered)
                        await ReadText<TMode>(MoveToComplexText(), ValueHandleType.Base64, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialBinaryText<TMode>(false, await BufferReader.ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Bytes32TextWithEndElement:
                    if (_buffered)
                        await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Base64, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialBinaryText<TMode>(true, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Bytes32Text:
                    if (_buffered)
                        await ReadText<TMode>(MoveToComplexText(), ValueHandleType.Base64, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    else
                        await ReadPartialBinaryText<TMode>(false, await BufferReader.ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.DictionaryTextWithEndElement:
                    MoveToAtomicTextWithEndElement().Value.SetDictionaryValue(await BufferReader.ReadDictionaryKey<TMode>(cancellationToken).ConfigureAwait(false));
                    return true;
                case XmlBinaryNodeType.UniqueIdTextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.UniqueId, ValueHandleLength.UniqueId, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.GuidTextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Guid, ValueHandleLength.Guid, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.DecimalTextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Decimal, ValueHandleLength.Decimal, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Int8TextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Int8, ValueHandleLength.Int8, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Int16TextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Int16, ValueHandleLength.Int16, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Int32TextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Int32, ValueHandleLength.Int32, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Int64TextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Int64, ValueHandleLength.Int64, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.UInt64TextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.UInt64, ValueHandleLength.UInt64, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.FloatTextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Single, ValueHandleLength.Single, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.DoubleTextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Double, ValueHandleLength.Double, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.TimeSpanTextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.TimeSpan, ValueHandleLength.TimeSpan, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.DateTimeTextWithEndElement:
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.DateTime, ValueHandleLength.DateTime, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.QNameDictionaryTextWithEndElement:
                    await BufferReader.ReadQName<TMode>(MoveToAtomicTextWithEndElement().Value, cancellationToken).ConfigureAwait(false);
                    return true;
                case XmlBinaryNodeType.Array:
                    await ReadArray<TMode>(cancellationToken).ConfigureAwait(false);
                    return true;
                default:
                    await BufferReader.ReadValue<TMode>(nodeType, MoveToComplexText().Value, cancellationToken).ConfigureAwait(false);
                    return true;
            }
        }

        private void VerifyWhitespace()
        {
            if (!this.Node.Value.IsWhitespace())
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
        }

        private async ValueTask ReadAttributes<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            XmlBinaryNodeType nodeType = await GetNodeType<TMode>(cancellationToken).ConfigureAwait(false);
            if (nodeType < XmlBinaryNodeType.MinAttribute || nodeType > XmlBinaryNodeType.MaxAttribute)
                return;
            await ReadAttributes2<TMode>(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask ReadAttributes2<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int startOffset = 0;

            if (_buffered)
                startOffset = BufferReader.Offset;

            while (true)
            {
                XmlAttributeNode attributeNode;
                Namespace nameSpace;
                PrefixHandleType prefix;
                XmlBinaryNodeType nodeType = await GetNodeType<TMode>(cancellationToken).ConfigureAwait(false);
                switch (nodeType)
                {
                    case XmlBinaryNodeType.ShortAttribute:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        attributeNode.Prefix.SetValue(PrefixHandleType.Empty);
                        await ReadName<TMode>(attributeNode.LocalName, cancellationToken).ConfigureAwait(false);
                        await ReadAttributeText<TMode>(attributeNode.AttributeText!, cancellationToken).ConfigureAwait(false);
                        break;
                    case XmlBinaryNodeType.Attribute:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        await ReadName<TMode>(attributeNode.Prefix, cancellationToken).ConfigureAwait(false);
                        await ReadName<TMode>(attributeNode.LocalName, cancellationToken).ConfigureAwait(false);
                        await ReadAttributeText<TMode>(attributeNode.AttributeText!, cancellationToken).ConfigureAwait(false);
                        FixXmlAttribute(attributeNode);
                        break;
                    case XmlBinaryNodeType.ShortDictionaryAttribute:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        attributeNode.Prefix.SetValue(PrefixHandleType.Empty);
                        await ReadDictionaryName<TMode>(attributeNode.LocalName, cancellationToken).ConfigureAwait(false);
                        await ReadAttributeText<TMode>(attributeNode.AttributeText!, cancellationToken).ConfigureAwait(false);
                        break;
                    case XmlBinaryNodeType.DictionaryAttribute:
                        SkipNodeType();
                        attributeNode = AddAttribute();
                        await ReadName<TMode>(attributeNode.Prefix, cancellationToken).ConfigureAwait(false);
                        await ReadDictionaryName<TMode>(attributeNode.LocalName, cancellationToken).ConfigureAwait(false);
                        await ReadAttributeText<TMode>(attributeNode.AttributeText!, cancellationToken).ConfigureAwait(false);
                        break;
                    case XmlBinaryNodeType.XmlnsAttribute:
                        SkipNodeType();
                        nameSpace = AddNamespace();
                        await ReadName<TMode>(nameSpace.Prefix, cancellationToken).ConfigureAwait(false);
                        await ReadName<TMode>(nameSpace.Uri, cancellationToken).ConfigureAwait(false);
                        AddXmlnsAttribute(nameSpace);
                        break;
                    case XmlBinaryNodeType.ShortXmlnsAttribute:
                        SkipNodeType();
                        nameSpace = AddNamespace();
                        nameSpace.Prefix.SetValue(PrefixHandleType.Empty);
                        await ReadName<TMode>(nameSpace.Uri, cancellationToken).ConfigureAwait(false);
                        AddXmlnsAttribute(nameSpace);
                        break;
                    case XmlBinaryNodeType.ShortDictionaryXmlnsAttribute:
                        SkipNodeType();
                        nameSpace = AddNamespace();
                        nameSpace.Prefix.SetValue(PrefixHandleType.Empty);
                        await ReadDictionaryName<TMode>(nameSpace.Uri, cancellationToken).ConfigureAwait(false);
                        AddXmlnsAttribute(nameSpace);
                        break;
                    case XmlBinaryNodeType.DictionaryXmlnsAttribute:
                        SkipNodeType();
                        nameSpace = AddNamespace();
                        await ReadName<TMode>(nameSpace.Prefix, cancellationToken).ConfigureAwait(false);
                        await ReadDictionaryName<TMode>(nameSpace.Uri, cancellationToken).ConfigureAwait(false);
                        AddXmlnsAttribute(nameSpace);
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
                        await ReadDictionaryName<TMode>(attributeNode.LocalName, cancellationToken).ConfigureAwait(false);
                        await ReadAttributeText<TMode>(attributeNode.AttributeText!, cancellationToken).ConfigureAwait(false);
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
                        await ReadName<TMode>(attributeNode.LocalName, cancellationToken).ConfigureAwait(false);
                        await ReadAttributeText<TMode>(attributeNode.AttributeText!, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        if (_buffered && (BufferReader.Offset - startOffset) > _maxBytesPerRead)
                            XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this, _maxBytesPerRead);
                        ProcessAttributes();
                        return;
                }
            }
        }

        private ValueTask ReadText<TMode>(XmlTextNode textNode, ValueHandleType type, int length, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            int offset;
            if (typeof(TMode) != typeof(SyncXmlStreamIO) && BufferReader.TryReadBytes(length, out offset))
            {
                SetTextValue(textNode, type, offset, length);
                return ValueTask.CompletedTask;
            }
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
            {
                offset = ReflectionSerialization.Complete(BufferReader.ReadBytes<TMode>(length, cancellationToken));
                SetTextValue(textNode, type, offset, length);
                return ValueTask.CompletedTask;
            }

            return ReadTextCore<TMode>(textNode, type, length, cancellationToken);
        }

        private async ValueTask ReadTextCore<TMode>(XmlTextNode textNode, ValueHandleType type, int length, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int offset = await BufferReader.ReadBytes<TMode>(length, cancellationToken).ConfigureAwait(false);
            SetTextValue(textNode, type, offset, length);
        }

        private void SetTextValue(XmlTextNode textNode, ValueHandleType type, int offset, int length)
        {
            textNode.Value.SetValue(type, offset, length);
            if (this.OutsideRootElement)
                VerifyWhitespace();
        }

        private async ValueTask ReadPartialUTF8Text<TMode>(bool withEndElement, int length, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            // The maxBytesPerRead includes the quota for the XmlBinaryNodeType.TextNode, so we need
            // to account for that.
            const int maxTextNodeLength = 5;
            int maxLength = Math.Max(_maxBytesPerRead - maxTextNodeLength, 0);
            if (length <= maxLength)
            {
                if (withEndElement)
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.UTF8, length, cancellationToken).ConfigureAwait(false);
                else
                    await ReadText<TMode>(MoveToComplexText(), ValueHandleType.UTF8, length, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // We also need to make sure we have enough room to insert a new XmlBinaryNodeType.TextNode
                // for the split data.
                int actual = Math.Max(maxLength - maxTextNodeLength, 0);
                if (actual == 0)
                    XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this, _maxBytesPerRead);
                int offset = await BufferReader.ReadBytes<TMode>(actual, cancellationToken).ConfigureAwait(false);

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
                if (i < offset)
                    XmlExceptionHelper.ThrowInvalidBinaryFormat(this);

                // Move any split characters so we can insert the node
                int byteCount = (offset + actual - i);
                if (byteCount == actual)
                    XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this, _maxBytesPerRead);

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

        private async ValueTask ReadUnicodeText<TMode>(bool withEndElement, int length, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if ((length & 1) != 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            if (_buffered)
            {
                if (withEndElement)
                {
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Unicode, length, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ReadText<TMode>(MoveToComplexText(), ValueHandleType.Unicode, length, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await ReadPartialUnicodeText<TMode>(withEndElement, length, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask ReadPartialUnicodeText<TMode>(bool withEndElement, int length, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            // The maxBytesPerRead includes the quota for the XmlBinaryNodeType.TextNode, so we need
            // to account for that.
            const int maxTextNodeLength = 5;
            int maxLength = Math.Max(_maxBytesPerRead - maxTextNodeLength, 0);
            if (length <= maxLength)
            {
                if (withEndElement)
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Unicode, length, cancellationToken).ConfigureAwait(false);
                else
                    await ReadText<TMode>(MoveToComplexText(), ValueHandleType.Unicode, length, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // We also need to make sure we have enough room to insert a new XmlBinaryNodeType.TextNode
                // for the split data.
                int actual = Math.Max(maxLength - maxTextNodeLength, 0);

                // Make sure we break on a char boundary
                if ((actual & 1) != 0)
                    actual--;
                if (actual == 0)
                    XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this, _maxBytesPerRead);

                int offset = await BufferReader.ReadBytes<TMode>(actual, cancellationToken).ConfigureAwait(false);

                // We need to make sure we don't split a Unicode surrogate character
                int byteCount = 0;
                char ch = (char)BufferReader.GetInt16(offset + actual - sizeof(char));
                // If the last char is a high surrogate char, then move back
                if (ch >= 0xD800 && ch < 0xDC00)
                    byteCount = sizeof(char);
                if (byteCount == actual)
                    XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this, _maxBytesPerRead);

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

        private async ValueTask ReadPartialBinaryText<TMode>(bool withEndElement, int length, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            const int nodeLength = 5;
            int maxBytesPerRead = Math.Max(_maxBytesPerRead - nodeLength, 0);
            if (length <= maxBytesPerRead)
            {
                if (withEndElement)
                    await ReadText<TMode>(MoveToAtomicTextWithEndElement(), ValueHandleType.Base64, length, cancellationToken).ConfigureAwait(false);
                else
                    await ReadText<TMode>(MoveToComplexText(), ValueHandleType.Base64, length, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                int actual = maxBytesPerRead;
                if (actual == 0)
                    XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this, _maxBytesPerRead);
                if (actual > 3)
                    actual -= (actual % 3);
                await ReadText<TMode>(MoveToComplexText(), ValueHandleType.Base64, actual, cancellationToken).ConfigureAwait(false);
                XmlBinaryNodeType nodeType = (withEndElement ? XmlBinaryNodeType.Bytes32TextWithEndElement : XmlBinaryNodeType.Bytes32Text);
                InsertNode(nodeType, length - actual);
            }
        }

        private void InsertNode(XmlBinaryNodeType nodeType, int length)
        {
            Span<byte> buffer = stackalloc byte[1 + sizeof(int)];
            buffer[0] = (byte)nodeType;
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(1), length);
            BufferReader.InsertBytes(buffer);
        }

        private async ValueTask ReadAttributeText<TMode>(XmlAttributeTextNode textNode, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            XmlBinaryNodeType nodeType = await GetNodeType<TMode>(cancellationToken).ConfigureAwait(false);
            SkipNodeType();
            await BufferReader.ReadValue<TMode>(nodeType, textNode.Value, cancellationToken).ConfigureAwait(false);
        }

        private ValueTask ReadName<TMode>(ValueHandle value, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (typeof(TMode) != typeof(SyncXmlStreamIO) && BufferReader.TryReadName(out int offset, out int length))
            {
                value.SetValue(ValueHandleType.UTF8, offset, length);
                return ValueTask.CompletedTask;
            }
            return ReadNameCore<TMode>(value, cancellationToken);
        }

        private async ValueTask ReadNameCore<TMode>(ValueHandle value, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int length = await BufferReader.ReadMultiByteUInt31<TMode>(cancellationToken).ConfigureAwait(false);
            int offset = await BufferReader.ReadBytes<TMode>(length, cancellationToken).ConfigureAwait(false);
            value.SetValue(ValueHandleType.UTF8, offset, length);
        }

        private ValueTask ReadName<TMode>(StringHandle handle, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (typeof(TMode) != typeof(SyncXmlStreamIO) && BufferReader.TryReadName(out int offset, out int length))
            {
                handle.SetValue(offset, length);
                return ValueTask.CompletedTask;
            }
            return ReadNameCore<TMode>(handle, cancellationToken);
        }

        private async ValueTask ReadNameCore<TMode>(StringHandle handle, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int length = await BufferReader.ReadMultiByteUInt31<TMode>(cancellationToken).ConfigureAwait(false);
            int offset = await BufferReader.ReadBytes<TMode>(length, cancellationToken).ConfigureAwait(false);
            handle.SetValue(offset, length);
        }

        private ValueTask ReadName<TMode>(PrefixHandle prefix, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (typeof(TMode) != typeof(SyncXmlStreamIO) && BufferReader.TryReadName(out int offset, out int length))
            {
                prefix.SetValue(offset, length);
                return ValueTask.CompletedTask;
            }
            return ReadNameCore<TMode>(prefix, cancellationToken);
        }

        private async ValueTask ReadNameCore<TMode>(PrefixHandle prefix, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int length = await BufferReader.ReadMultiByteUInt31<TMode>(cancellationToken).ConfigureAwait(false);
            int offset = await BufferReader.ReadBytes<TMode>(length, cancellationToken).ConfigureAwait(false);
            prefix.SetValue(offset, length);
        }

        private async ValueTask ReadDictionaryName<TMode>(StringHandle s, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int key = await BufferReader.ReadDictionaryKey<TMode>(cancellationToken).ConfigureAwait(false);
            s.SetValue(key);
        }

        private XmlBinaryNodeType GetNodeType()
        {
            return BufferReader.GetNodeType();
        }

        private ValueTask<XmlBinaryNodeType> GetNodeType<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
                return new ValueTask<XmlBinaryNodeType>(GetNodeType());

            return GetNodeTypeAsync(cancellationToken);
        }

        private async ValueTask<XmlBinaryNodeType> GetNodeTypeAsync(CancellationToken cancellationToken)
        {
            return (XmlBinaryNodeType)await BufferReader.GetByte<AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);
        }

        private void SkipNodeType()
        {
            BufferReader.SkipNodeType();
        }

        private int ReadDictionaryKey()
        {
            return BufferReader.ReadDictionaryKey();
        }

        private int ReadUInt8()
        {
            return BufferReader.ReadUInt8();
        }

        private static bool IsValidArrayType(XmlBinaryNodeType nodeType)
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

        private async ValueTask ReadArray<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (await GetNodeType<TMode>(cancellationToken).ConfigureAwait(false) == XmlBinaryNodeType.Array) // Prevent recursion
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            await ReadNode<TMode>(cancellationToken).ConfigureAwait(false); // ReadStartElement
            if (this.Node.NodeType != XmlNodeType.Element)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            if (await GetNodeType<TMode>(cancellationToken).ConfigureAwait(false) == XmlBinaryNodeType.Array) // Prevent recursion
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            await ReadNode<TMode>(cancellationToken).ConfigureAwait(false); // ReadEndElement
            if (this.Node.NodeType != XmlNodeType.EndElement)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            _arrayState = ArrayState.Element;
            _arrayNodeType = await GetNodeType<TMode>(cancellationToken).ConfigureAwait(false);
            if (!IsValidArrayType(_arrayNodeType))
                XmlExceptionHelper.ThrowInvalidBinaryFormat(this);
            SkipNodeType();
            _arrayCount = await BufferReader.ReadMultiByteUInt31<TMode>(cancellationToken).ConfigureAwait(false);
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

        internal int GetArrayLength<T>(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
            where T : unmanaged
        {
            if (Signing || _arrayState != ArrayState.Element ||
                !IsStartElement(localName, namespaceUri) || !IsStartArray(out Type? type) || type != typeof(T))
            {
                return 0;
            }
            if (_buffered)
            {
                BufferReader.GetBuffer(out int offset, out int offsetMax);
                if (_arrayCount > (offsetMax - offset) / GetArrayElementSize<T>())
                {
                    return 0;
                }
            }

            return BitConverter.IsLittleEndian || typeof(T) == typeof(bool) || typeof(T) == typeof(DateTime) ? _arrayCount : 0;
        }

        internal ValueTask ReadArrayAsync<T>(T[] array, int offset, int count, CancellationToken cancellationToken)
            where T : unmanaged
        {
            CheckReadMode<AsyncXmlStreamIO>(cancellationToken);
            Debug.Assert(_arrayState == ArrayState.Element && !Signing && count > 0 && count <= _arrayCount);
            int elementSize = GetArrayElementSize<T>();
            if (!_buffered)
                BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, deferCompaction: true);
            BufferReader.GetBuffer(out int bufferOffset, out int limit);
            if (count <= Math.Min(4096, _maxBytesPerRead) / elementSize && count <= (limit - bufferOffset) / elementSize)
            {
                ReadArrayValues(array, offset, count);
                _arrayCount -= count;
                if (_arrayCount != 0)
                    return default;

                _arrayState = ArrayState.None;
                ExitScope();
                ValueTask<bool> next = ReadNode<AsyncXmlStreamIO>(cancellationToken);
                if (!next.IsCompletedSuccessfully)
                    return AwaitArrayEnd(next);

                next.GetAwaiter().GetResult();
                return default;
            }

            return ReadArrayCoreAsync(array, offset, count, cancellationToken);
        }

        private static async ValueTask AwaitArrayEnd(ValueTask<bool> next) =>
            await next.ConfigureAwait(false);

        private async ValueTask ReadArrayCoreAsync<T>(T[] array, int offset, int count, CancellationToken cancellationToken)
            where T : unmanaged
        {
            int elementSize = GetArrayElementSize<T>();
            int chunkSize = Math.Max(1, Math.Min(4096, _maxBytesPerRead) / elementSize);
            while (count > 0)
            {
                if (!_buffered)
                {
                    BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, deferCompaction: true);
                }
                int chunk = Math.Min(count, chunkSize);
                await BufferReader.EnsureBytes<AsyncXmlStreamIO>(chunk * elementSize, cancellationToken).ConfigureAwait(false);
                ReadArrayValues(array, offset, chunk);
                _arrayCount -= chunk;
                offset += chunk;
                count -= chunk;
            }
            if (_arrayCount == 0)
            {
                _arrayState = ArrayState.None;
                ExitScope();
                await ReadNode<AsyncXmlStreamIO>(cancellationToken).ConfigureAwait(false);
            }
        }

        private static unsafe int GetArrayElementSize<T>() where T : unmanaged => sizeof(T);

        private void ReadArrayValues<T>(T[] array, int offset, int count)
            where T : unmanaged
        {
            if (typeof(T) == typeof(DateTime))
            {
                foreach (ref DateTime item in ((DateTime[])(object)array).AsSpan(offset, count))
                {
                    item = BufferReader.ReadDateTime();
                }
            }
            else
            {
                BufferReader.ReadRawArrayBytes(array.AsSpan(offset, count));
            }
        }

        public override bool IsStartArray([NotNullWhen(true)] out Type? type)
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

        private static void CheckArray(Array array, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > array.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, array.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > array.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, array.Length - offset));
        }

        private int ReadArray(bool[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            BufferReader.ReadRawArrayBytes(array.AsSpan(offset, actual));
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

        private int ReadArray(short[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            BufferReader.ReadRawArrayBytes(array.AsSpan(offset, actual));
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, short[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int16TextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int16TextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private int ReadArray(int[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            BufferReader.ReadRawArrayBytes(array.AsSpan(offset, actual));
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, int[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int32TextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int32TextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private int ReadArray(long[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            BufferReader.ReadRawArrayBytes(array.AsSpan(offset, actual));
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, long[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int64TextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.Int64TextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private int ReadArray(float[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            BufferReader.ReadRawArrayBytes(array.AsSpan(offset, actual));
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, float[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.FloatTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.FloatTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private int ReadArray(double[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            BufferReader.ReadRawArrayBytes(array.AsSpan(offset, actual));
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, double[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DoubleTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DoubleTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        private int ReadArray(decimal[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            BufferReader.ReadRawArrayBytes(array.AsSpan(offset, actual));
            SkipArrayElements(actual);
            return actual;
        }

        public override int ReadArray(string localName, string namespaceUri, decimal[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DecimalTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
        {
            if (BitConverter.IsLittleEndian && IsStartArray(localName, namespaceUri, XmlBinaryNodeType.DecimalTextWithEndElement))
                return ReadArray(array, offset, count);
            return base.ReadArray(localName, namespaceUri, array, offset, count);
        }

        // DateTime
        private int ReadArray(DateTime[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = Math.Min(count, _arrayCount);
            // Try to read in whole array, but don't fail if not possible
            BufferReader.GetBuffer(actual * ValueHandleLength.DateTime, out _, out _);
            ReadArrayValues(array, offset, actual);
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
            if (BitConverter.IsLittleEndian)
            {
                BufferReader.ReadRawArrayBytes(array.AsSpan(offset, actual));
            }
            else
            {
                // Try to read in whole array, but don't fail if not possible
                BufferReader.GetBuffer(actual * ValueHandleLength.Guid, out _, out _);
                foreach (ref Guid item in array.AsSpan(offset, actual))
                {
                    item = BufferReader.ReadGuid();
                }
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
            // Try to read in whole array, but don't fail if not possible
            BufferReader.GetBuffer(actual * ValueHandleLength.TimeSpan, out _, out _);
            foreach (ref TimeSpan item in array.AsSpan(offset, actual))
            {
                item = BufferReader.ReadTimeSpan();
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
