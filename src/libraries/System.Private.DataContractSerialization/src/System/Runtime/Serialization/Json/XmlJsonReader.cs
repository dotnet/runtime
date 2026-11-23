// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal sealed partial class XmlJsonReader : XmlBaseReader, IXmlJsonReaderInitializer
    {
        private const int MaxTextChunk = 2048;

        private static ReadOnlySpan<byte> CharTypes =>
            [
                CharType.None, //   0 (.)
                CharType.None, //   1 (.)
                CharType.None, //   2 (.)
                CharType.None, //   3 (.)
                CharType.None, //   4 (.)
                CharType.None, //   5 (.)
                CharType.None, //   6 (.)
                CharType.None, //   7 (.)
                CharType.None, //   8 (.)
                CharType.None, //   9 (.)
                CharType.None, //   A (.)
                CharType.None, //   B (.)
                CharType.None, //   C (.)
                CharType.None, //   D (.)
                CharType.None, //   E (.)
                CharType.None, //   F (.)
                CharType.None, //  10 (.)
                CharType.None, //  11 (.)
                CharType.None, //  12 (.)
                CharType.None, //  13 (.)
                CharType.None, //  14 (.)
                CharType.None, //  15 (.)
                CharType.None, //  16 (.)
                CharType.None, //  17 (.)
                CharType.None, //  18 (.)
                CharType.None, //  19 (.)
                CharType.None, //  1A (.)
                CharType.None, //  1B (.)
                CharType.None, //  1C (.)
                CharType.None, //  1D (.)
                CharType.None, //  1E (.)
                CharType.None, //  1F (.)
                CharType.None, //  20 ( )
                CharType.None, //  21 (!)
                CharType.None, //  22 (")
                CharType.None, //  23 (#)
                CharType.None, //  24 ($)
                CharType.None, //  25 (%)
                CharType.None, //  26 (&)
                CharType.None, //  27 (')
                CharType.None, //  28 (()
                CharType.None, //  29 ())
                CharType.None, //  2A (*)
                CharType.None, //  2B (+)
                CharType.None, //  2C (,)
                CharType.None | CharType.Name, //  2D (-)
                CharType.None | CharType.Name, //  2E (.)
                CharType.None, //  2F (/)
                CharType.None | CharType.Name, //  30 (0)
                CharType.None | CharType.Name, //  31 (1)
                CharType.None | CharType.Name, //  32 (2)
                CharType.None | CharType.Name, //  33 (3)
                CharType.None | CharType.Name, //  34 (4)
                CharType.None | CharType.Name, //  35 (5)
                CharType.None | CharType.Name, //  36 (6)
                CharType.None | CharType.Name, //  37 (7)
                CharType.None | CharType.Name, //  38 (8)
                CharType.None | CharType.Name, //  39 (9)
                CharType.None, //  3A (:)
                CharType.None, //  3B (;)
                CharType.None, //  3C (<)
                CharType.None, //  3D (=)
                CharType.None, //  3E (>)
                CharType.None, //  3F (?)
                CharType.None, //  40 (@)
                CharType.None | CharType.FirstName | CharType.Name, //  41 (A)
                CharType.None | CharType.FirstName | CharType.Name, //  42 (B)
                CharType.None | CharType.FirstName | CharType.Name, //  43 (C)
                CharType.None | CharType.FirstName | CharType.Name, //  44 (D)
                CharType.None | CharType.FirstName | CharType.Name, //  45 (E)
                CharType.None | CharType.FirstName | CharType.Name, //  46 (F)
                CharType.None | CharType.FirstName | CharType.Name, //  47 (G)
                CharType.None | CharType.FirstName | CharType.Name, //  48 (H)
                CharType.None | CharType.FirstName | CharType.Name, //  49 (I)
                CharType.None | CharType.FirstName | CharType.Name, //  4A (J)
                CharType.None | CharType.FirstName | CharType.Name, //  4B (K)
                CharType.None | CharType.FirstName | CharType.Name, //  4C (L)
                CharType.None | CharType.FirstName | CharType.Name, //  4D (M)
                CharType.None | CharType.FirstName | CharType.Name, //  4E (N)
                CharType.None | CharType.FirstName | CharType.Name, //  4F (O)
                CharType.None | CharType.FirstName | CharType.Name, //  50 (P)
                CharType.None | CharType.FirstName | CharType.Name, //  51 (Q)
                CharType.None | CharType.FirstName | CharType.Name, //  52 (R)
                CharType.None | CharType.FirstName | CharType.Name, //  53 (S)
                CharType.None | CharType.FirstName | CharType.Name, //  54 (T)
                CharType.None | CharType.FirstName | CharType.Name, //  55 (U)
                CharType.None | CharType.FirstName | CharType.Name, //  56 (V)
                CharType.None | CharType.FirstName | CharType.Name, //  57 (W)
                CharType.None | CharType.FirstName | CharType.Name, //  58 (X)
                CharType.None | CharType.FirstName | CharType.Name, //  59 (Y)
                CharType.None | CharType.FirstName | CharType.Name, //  5A (Z)
                CharType.None, //  5B ([)
                CharType.None, //  5C (\)
                CharType.None, //  5D (])
                CharType.None, //  5E (^)
                CharType.None | CharType.FirstName | CharType.Name, //  5F (_)
                CharType.None, //  60 (`)
                CharType.None | CharType.FirstName | CharType.Name, //  61 (a)
                CharType.None | CharType.FirstName | CharType.Name, //  62 (b)
                CharType.None | CharType.FirstName | CharType.Name, //  63 (c)
                CharType.None | CharType.FirstName | CharType.Name, //  64 (d)
                CharType.None | CharType.FirstName | CharType.Name, //  65 (e)
                CharType.None | CharType.FirstName | CharType.Name, //  66 (f)
                CharType.None | CharType.FirstName | CharType.Name, //  67 (g)
                CharType.None | CharType.FirstName | CharType.Name, //  68 (h)
                CharType.None | CharType.FirstName | CharType.Name, //  69 (i)
                CharType.None | CharType.FirstName | CharType.Name, //  6A (j)
                CharType.None | CharType.FirstName | CharType.Name, //  6B (k)
                CharType.None | CharType.FirstName | CharType.Name, //  6C (l)
                CharType.None | CharType.FirstName | CharType.Name, //  6D (m)
                CharType.None | CharType.FirstName | CharType.Name, //  6E (n)
                CharType.None | CharType.FirstName | CharType.Name, //  6F (o)
                CharType.None | CharType.FirstName | CharType.Name, //  70 (p)
                CharType.None | CharType.FirstName | CharType.Name, //  71 (q)
                CharType.None | CharType.FirstName | CharType.Name, //  72 (r)
                CharType.None | CharType.FirstName | CharType.Name, //  73 (s)
                CharType.None | CharType.FirstName | CharType.Name, //  74 (t)
                CharType.None | CharType.FirstName | CharType.Name, //  75 (u)
                CharType.None | CharType.FirstName | CharType.Name, //  76 (v)
                CharType.None | CharType.FirstName | CharType.Name, //  77 (w)
                CharType.None | CharType.FirstName | CharType.Name, //  78 (x)
                CharType.None | CharType.FirstName | CharType.Name, //  79 (y)
                CharType.None | CharType.FirstName | CharType.Name, //  7A (z)
                CharType.None, //  7B ({)
                CharType.None, //  7C (|)
                CharType.None, //  7D (})
                CharType.None, //  7E (~)
                CharType.None, //  7F (.)
                CharType.None | CharType.FirstName | CharType.Name, //  80 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  81 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  82 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  83 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  84 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  85 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  86 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  87 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  88 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  89 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  8A (.)
                CharType.None | CharType.FirstName | CharType.Name, //  8B (.)
                CharType.None | CharType.FirstName | CharType.Name, //  8C (.)
                CharType.None | CharType.FirstName | CharType.Name, //  8D (.)
                CharType.None | CharType.FirstName | CharType.Name, //  8E (.)
                CharType.None | CharType.FirstName | CharType.Name, //  8F (.)
                CharType.None | CharType.FirstName | CharType.Name, //  90 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  91 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  92 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  93 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  94 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  95 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  96 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  97 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  98 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  99 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  9A (.)
                CharType.None | CharType.FirstName | CharType.Name, //  9B (.)
                CharType.None | CharType.FirstName | CharType.Name, //  9C (.)
                CharType.None | CharType.FirstName | CharType.Name, //  9D (.)
                CharType.None | CharType.FirstName | CharType.Name, //  9E (.)
                CharType.None | CharType.FirstName | CharType.Name, //  9F (.)
                CharType.None | CharType.FirstName | CharType.Name, //  A0 (.)
                CharType.None | CharType.FirstName | CharType.Name, //  A1 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  A2 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  A3 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  A4 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  A5 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  A6 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  A7 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  A8 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  A9 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  AA (?)
                CharType.None | CharType.FirstName | CharType.Name, //  AB (?)
                CharType.None | CharType.FirstName | CharType.Name, //  AC (?)
                CharType.None | CharType.FirstName | CharType.Name, //  AD (.)
                CharType.None | CharType.FirstName | CharType.Name, //  AE (?)
                CharType.None | CharType.FirstName | CharType.Name, //  AF (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B0 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B1 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B2 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B3 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B4 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B5 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B6 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B7 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B8 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  B9 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  BA (?)
                CharType.None | CharType.FirstName | CharType.Name, //  BB (?)
                CharType.None | CharType.FirstName | CharType.Name, //  BC (?)
                CharType.None | CharType.FirstName | CharType.Name, //  BD (?)
                CharType.None | CharType.FirstName | CharType.Name, //  BE (?)
                CharType.None | CharType.FirstName | CharType.Name, //  BF (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C0 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C1 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C2 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C3 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C4 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C5 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C6 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C7 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C8 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  C9 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  CA (?)
                CharType.None | CharType.FirstName | CharType.Name, //  CB (?)
                CharType.None | CharType.FirstName | CharType.Name, //  CC (?)
                CharType.None | CharType.FirstName | CharType.Name, //  CD (?)
                CharType.None | CharType.FirstName | CharType.Name, //  CE (?)
                CharType.None | CharType.FirstName | CharType.Name, //  CF (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D0 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D1 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D2 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D3 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D4 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D5 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D6 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D7 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D8 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  D9 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  DA (?)
                CharType.None | CharType.FirstName | CharType.Name, //  DB (?)
                CharType.None | CharType.FirstName | CharType.Name, //  DC (?)
                CharType.None | CharType.FirstName | CharType.Name, //  DD (?)
                CharType.None | CharType.FirstName | CharType.Name, //  DE (?)
                CharType.None | CharType.FirstName | CharType.Name, //  DF (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E0 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E1 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E2 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E3 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E4 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E5 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E6 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E7 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E8 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  E9 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  EA (?)
                CharType.None | CharType.FirstName | CharType.Name, //  EB (?)
                CharType.None | CharType.FirstName | CharType.Name, //  EC (?)
                CharType.None | CharType.FirstName | CharType.Name, //  ED (?)
                CharType.None | CharType.FirstName | CharType.Name, //  EE (?)
                CharType.None | CharType.FirstName | CharType.Name, //  EF (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F0 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F1 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F2 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F3 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F4 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F5 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F6 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F7 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F8 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  F9 (?)
                CharType.None | CharType.FirstName | CharType.Name, //  FA (?)
                CharType.None | CharType.FirstName | CharType.Name, //  FB (?)
                CharType.None | CharType.FirstName | CharType.Name, //  FC (?)
                CharType.None | CharType.FirstName | CharType.Name, //  FD (?)
                CharType.None | CharType.FirstName | CharType.Name, //  FE (?)
                CharType.None | CharType.FirstName | CharType.Name, //  FF (?)
            ];
        private bool _buffered;
        private byte[]? _charactersToSkipOnNextRead;
        private JsonComplexTextMode _complexTextMode = JsonComplexTextMode.None;
        private bool _expectingFirstElementInNonPrimitiveChild;
        private int _maxBytesPerRead;
        private OnXmlDictionaryReaderClose? _onReaderClose;
        private bool _readServerTypeElement;
        private int _scopeDepth;
        private JsonNodeType[]? _scopes;

        private enum JsonComplexTextMode
        {
            QuotedText,
            NumericalText,
            None
        };

        public override bool CanCanonicalize
        {
            get
            {
                return false;
            }
        }

        public override string Value
        {
            get
            {
                if (IsAttributeValue && !this.IsLocalName(JsonGlobals.typeString))
                {
                    return UnescapeJsonString(base.Value);
                }
                return base.Value;
            }
        }

        private bool IsAttributeValue
        {
            get
            {
                return (this.Node.NodeType == XmlNodeType.Attribute || this.Node is XmlAttributeTextNode);
            }
        }

        private bool IsReadingCollection
        {
            get
            {
                return ((_scopeDepth > 0) && (_scopes![_scopeDepth] == JsonNodeType.Collection));
            }
        }

        private bool IsReadingComplexText
        {
            get
            {
                return ((!this.Node.IsAtomicValue) &&
                    (this.Node.NodeType == XmlNodeType.Text));
            }
        }

        public override void Close()
        {
            CloseCallback();
            base.Close();
        }

        internal override async ValueTask CloseAsync()
        {
            try
            {
                CloseCallback();
            }
            finally
            {
                await base.CloseAsync().ConfigureAwait(false);
            }
        }

        private void CloseCallback()
        {
            OnXmlDictionaryReaderClose? onClose = _onReaderClose;
            _onReaderClose = null;
            ResetState();
            if (onClose != null)
            {
                try
                {
                    onClose(this);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(SR.GenericCallbackException, e);
                }
            }
        }

        public override void EndCanonicalization()
        {
            throw new NotSupportedException();
        }

        public override string GetAttribute(int index)
        {
            return UnescapeJsonString(base.GetAttribute(index));
        }

        public override string? GetAttribute(string localName, string? namespaceUri)
        {
            if (localName != JsonGlobals.typeString)
            {
                return UnescapeJsonString(base.GetAttribute(localName, namespaceUri));
            }
            return base.GetAttribute(localName, namespaceUri);
        }
        public override string? GetAttribute(string name)
        {
            if (name != JsonGlobals.typeString)
            {
                return UnescapeJsonString(base.GetAttribute(name));
            }
            return base.GetAttribute(name);
        }

        public override string? GetAttribute(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            if (XmlDictionaryString.GetString(localName) != JsonGlobals.typeString)
            {
                return UnescapeJsonString(base.GetAttribute(localName, namespaceUri));
            }
            return base.GetAttribute(localName, namespaceUri);
        }

        internal override ValueTask<bool> ReadAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<bool>(cancellationToken) :
                ReadCore<AsyncXmlStreamIO>(cancellationToken);

        internal bool TryReadBufferedPrimitiveElement<T>(out T value)
        {
            Debug.Assert(_charactersToSkipOnNextRead is not null);
            value = default!;
            if (Node.NodeType != XmlNodeType.Element || _scopeDepth == 0 || IsReadingComplexText || IsReadingCollection ||
                _readServerTypeElement || _expectingFirstElementInNonPrimitiveChild ||
                _charactersToSkipOnNextRead[0] != 0 || _charactersToSkipOnNextRead[1] != 0)
                return false;

            if (!_buffered)
                BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, deferCompaction: true);
            byte[] buffer = BufferReader.GetBuffer(out int offset, out int limit);
            while (offset < limit && IsWhitespace(buffer[offset]))
                offset++;
            if (offset == limit)
                return false;

            byte ch = buffer[offset];
            int end;
            if (ch == JsonGlobals.QuoteByte)
            {
                int textLimit = offset + 1 + Math.Min(limit - offset - 1, MaxTextChunk);
                int length = ComputeQuotedTextLengthUntilEndQuote(buffer, offset + 1, textLimit, out bool escaped);
                if (escaped || length == textLimit - offset - 1)
                    return false;
                end = offset + length + 2;
            }
            else if (ch is (byte)'t' or (byte)'f')
            {
                end = offset + (ch == (byte)'t' ? 4 : 5);
            }
            else if (ch == (byte)'-' || ch is >= (byte)'0' and <= (byte)'9' || ch is (byte)'I' or (byte)'N')
            {
                int textLimit = offset + Math.Min(limit - offset, MaxTextChunk);
                int length = ComputeNumericalTextLength(buffer, offset, textLimit);
                if (length == textLimit - offset)
                    return false;
                end = offset + length;
            }
            else
            {
                return false;
            }

            while (end < limit && IsWhitespace(buffer[end]))
                end++;
            if (end >= limit || buffer[end] is not (JsonGlobals.MemberSeparatorByte or JsonGlobals.EndObjectByte or JsonGlobals.EndCollectionByte))
                return false;

            // Both the value and its terminating delimiter are buffered, so these parsers cannot refill.
            BufferReader.Offset = offset;
            ReadNodeContentSync(ch);
            if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(decimal))
            {
                if (!TryReadAtomicNumber(out value))
                {
                    value = ReadContentAsCoreSync<T>();
                    return true;
                }
            }
            else if (typeof(T) == typeof(string))
            {
                string text = Node.Value.GetString();
                if (text.Length > Quotas.MaxStringContentLength)
                    XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, Quotas.MaxStringContentLength);
                value = (T)(object)text;
            }
            else
            {
                value = ConvertAtomicValue<T>(Node.Value);
            }
            // Advancing within the proven buffered region does not require opening another quota window.
            BufferReader.Offset = end;
            ReadNodeContentSync(buffer[end]);
            Debug.Assert(Node.NodeType == XmlNodeType.EndElement);
            return true;
        }

        private ValueTask<bool> ReadCore<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            CheckReadMode<TMode>(cancellationToken);
            if (this.Node.CanMoveToElement)
            {
                // If we're positioned on an attribute or attribute text on an empty element, we need to move back
                // to the element in order to get the correct setting of ExitScope
                MoveToElement();
            }

            if (this.Node.ReadState == ReadState.Closed)
            {
                return new ValueTask<bool>(false);
            }
            if (this.Node.ExitScope)
            {
                ExitScope();
            }
            if (!_buffered)
            {
                BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, typeof(TMode) == typeof(AsyncXmlStreamIO));
            }

            if (TryReadBufferedNode(out byte ch))
                return new ValueTask<bool>(ReadNodeContentSync(ch));

            return ReadNodeCore<TMode>(cancellationToken);
        }

        private bool TryReadBufferedNode(out byte ch)
        {
            Debug.Assert(_charactersToSkipOnNextRead is not null);
            ch = 0;
            if (_scopeDepth == 0 || IsReadingCollection)
                return false;

            byte[] buffer = BufferReader.GetBuffer(out int offset, out int limit);
            if (IsReadingComplexText)
            {
                if (_complexTextMode != JsonComplexTextMode.QuotedText || !IsQuotedTextBuffered(buffer, offset, limit))
                    return false;
                ch = buffer[offset];
                return true;
            }
            while (offset < limit && IsWhitespace(buffer[offset]))
                offset++;
            bool skippedDelimiter = offset < limit &&
                (_charactersToSkipOnNextRead[0] == buffer[offset] || _charactersToSkipOnNextRead[1] == buffer[offset]);
            if (skippedDelimiter)
                offset++;
            while (offset < limit && IsWhitespace(buffer[offset]))
                offset++;
            if (offset == limit)
                return false;

            ch = buffer[offset];
            bool buffered;
            if (ch == JsonGlobals.QuoteByte)
            {
                if (_readServerTypeElement || _expectingFirstElementInNonPrimitiveChild || Node.NodeType == XmlNodeType.EndElement)
                {
                    buffered = IsStartElementBuffered(buffer, offset, limit);
                }
                else
                {
                    buffered = IsQuotedTextBuffered(buffer, offset + 1, limit);
                }
            }
            else if (ch is (byte)'t' or (byte)'f')
            {
                buffered = limit - offset > (ch == (byte)'t' ? 4 : 5);
            }
            else if (ch == (byte)'n')
            {
                int next = offset + 4;
                while (next < limit && IsWhitespace(buffer[next]))
                    next++;
                buffered = next < limit;
            }
            else if (ch == (byte)'-' || ch is >= (byte)'0' and <= (byte)'9' || ch is (byte)'I' or (byte)'N')
            {
                int textLimit = offset + Math.Min(limit - offset, MaxTextChunk);
                buffered = ComputeNumericalTextLength(buffer, offset, textLimit) < textLimit - offset;
            }
            else
            {
                buffered = ch == JsonGlobals.MemberSeparatorByte ||
                    ch == JsonGlobals.EndObjectByte && !_expectingFirstElementInNonPrimitiveChild;
            }

            if (!buffered)
                return false;
            BufferReader.Offset = offset;
            if (skippedDelimiter)
            {
                _charactersToSkipOnNextRead[0] = 0;
                _charactersToSkipOnNextRead[1] = 0;
            }
            return true;
        }

        private static bool IsQuotedTextBuffered(byte[] buffer, int offset, int limit)
        {
            int textLimit = offset + Math.Min(limit - offset, MaxTextChunk);
            int length = ComputeQuotedTextLengthUntilEndQuote(buffer, offset, textLimit, out bool escaped);
            if (!escaped)
                return length < textLimit - offset;
            if (length != 0)
                return true;
            if (buffer[offset] == 0xEF)
                return limit - offset >= 3;

            // Unicode escapes may consume a surrogate pair and inspect the following byte.
            return limit - offset >= 3 && (buffer[offset + 1] != (byte)'u' || limit - offset >= 13);
        }

        private static bool IsStartElementBuffered(byte[] buffer, int offset, int limit)
        {
            const int ElementLookahead = 128;
            if (limit - offset < 2)
                return false;

            int nameLimit = offset + Math.Min(limit - offset, ElementLookahead);
            int length = ComputeQuotedTextLengthUntilEndQuote(buffer, offset + 1, nameLimit, out bool escaped);
            if (escaped || length == nameLimit - offset - 1)
                return false;
            offset += length + 2;
            while (offset < nameLimit && IsWhitespace(buffer[offset]))
                offset++;
            if (offset == nameLimit || buffer[offset++] != JsonGlobals.NameValueSeparatorByte)
                return false;
            while (offset < nameLimit && IsWhitespace(buffer[offset]))
                offset++;
            return offset < nameLimit && buffer[offset] != JsonGlobals.ObjectByte;
        }

        private async ValueTask<bool> ReadNodeCore<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            Debug.Assert(_charactersToSkipOnNextRead != null);
            byte ch;
            int nextByte;

            // Skip whitespace before checking EOF
            // Complex text check necessary because whitespace could be part of really long
            //    quoted text that's read using multiple Read() calls.
            // This also ensures that we deal with the whitespace-only input case properly by not
            //    floating a root element at all.
            if (!IsReadingComplexText ||
                (_complexTextMode == JsonComplexTextMode.NumericalText &&
                await BufferReader.EndOfFileCore<TMode>(cancellationToken).ConfigureAwait(false)))
            {
                await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);

                if ((nextByte = await TryGetByte<TMode>(cancellationToken).ConfigureAwait(false)) >= 0)
                {
                    ch = (byte)nextByte;
                    if (_charactersToSkipOnNextRead[0] == ch || _charactersToSkipOnNextRead[1] == ch)
                    {
                        BufferReader.SkipByte();
                        _charactersToSkipOnNextRead[0] = 0;
                        _charactersToSkipOnNextRead[1] = 0;
                    }
                }

                await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);

                if ((nextByte = await TryGetByte<TMode>(cancellationToken).ConfigureAwait(false)) >= 0)
                {
                    ch = (byte)nextByte;
                    if (ch == JsonGlobals.EndCollectionByte && IsReadingCollection)
                    {
                        BufferReader.SkipByte();
                        await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);
                        ExitJsonScope();
                    }
                }

                if (await BufferReader.EndOfFileCore<TMode>(cancellationToken).ConfigureAwait(false))
                {
                    if (_scopeDepth > 0)
                    {
                        MoveToEndElement();
                        return true;
                    }
                    else
                    {
                        MoveToEndOfFile();
                        return false;
                    }
                }
            }

            ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);

            return await ReadNodeContent<TMode>(ch, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<bool> ReadNodeContent<TMode>(byte ch, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            Debug.Assert(_charactersToSkipOnNextRead is not null);
            int nextByte;
            if (_scopeDepth == 0)
            {
                await ReadNonExistentElementName<TMode>(StringHandleConstStringType.Root, cancellationToken).ConfigureAwait(false);
            }
            else if (IsReadingComplexText)
            {
                switch (_complexTextMode)
                {
                    case JsonComplexTextMode.NumericalText:
                        await ReadNumericalText<TMode>(cancellationToken).ConfigureAwait(false);
                        break;
                    case JsonComplexTextMode.QuotedText:
                        if (ch == (byte)'\\')
                        {
                            await ReadEscapedCharacter<TMode>(true, cancellationToken).ConfigureAwait(false); //  moveToText
                        }
                        else
                        {
                            await ReadQuotedText<TMode>(true, cancellationToken).ConfigureAwait(false); //  moveToText
                        }
                        break;
                    case JsonComplexTextMode.None:
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)ch)));
                        break;
                }
            }
            else if (IsReadingCollection)
            {
                await ReadNonExistentElementName<TMode>(StringHandleConstStringType.Item, cancellationToken).ConfigureAwait(false);
            }
            else if (ch == JsonGlobals.EndCollectionByte)
            {
                BufferReader.SkipByte();
                MoveToEndElement();
                ExitJsonScope();
            }
            else if (ch == JsonGlobals.ObjectByte)
            {
                BufferReader.SkipByte();
                await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);
                ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                if (ch == JsonGlobals.EndObjectByte)
                {
                    BufferReader.SkipByte();
                    await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);
                    if ((nextByte = await TryGetByte<TMode>(cancellationToken).ConfigureAwait(false)) >= 0)
                    {
                        ch = (byte)nextByte;
                        if (ch == JsonGlobals.MemberSeparatorByte)
                        {
                            BufferReader.SkipByte();
                        }
                    }
                    else
                    {
                        _charactersToSkipOnNextRead[0] = JsonGlobals.MemberSeparatorByte;
                    }
                    MoveToEndElement();
                }
                else
                {
                    EnterJsonScope(JsonNodeType.Object);
                    await ParseStartElement<TMode>(cancellationToken).ConfigureAwait(false);
                }
            }
            else if (ch == JsonGlobals.EndObjectByte)
            {
                BufferReader.SkipByte();
                if (_expectingFirstElementInNonPrimitiveChild)
                {
                    await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);
                    ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                    if ((ch == JsonGlobals.MemberSeparatorByte) ||
                        (ch == JsonGlobals.EndObjectByte))
                    {
                        BufferReader.SkipByte();
                    }
                    else
                    {
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter,
                            (char)ch)));
                    }
                    _expectingFirstElementInNonPrimitiveChild = false;
                }
                MoveToEndElement();
            }
            else if (ch == JsonGlobals.MemberSeparatorByte)
            {
                BufferReader.SkipByte();
                MoveToEndElement();
            }
            else if (ch == JsonGlobals.QuoteByte)
            {
                if (_readServerTypeElement)
                {
                    _readServerTypeElement = false;
                    EnterJsonScope(JsonNodeType.Object);
                    await ParseStartElement<TMode>(cancellationToken).ConfigureAwait(false);
                }
                else if (this.Node.NodeType == XmlNodeType.Element)
                {
                    if (_expectingFirstElementInNonPrimitiveChild)
                    {
                        EnterJsonScope(JsonNodeType.Object);
                        await ParseStartElement<TMode>(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        BufferReader.SkipByte();
                        await ReadQuotedText<TMode>(true, cancellationToken).ConfigureAwait(false); //  moveToText
                    }
                }
                else if (this.Node.NodeType == XmlNodeType.EndElement)
                {
                    EnterJsonScope(JsonNodeType.Element);
                    await ParseStartElement<TMode>(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    XmlExceptionHelper.ThrowXmlException(this,
                        new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter,
                        JsonGlobals.QuoteChar)));
                }
            }
            else if (ch == (byte)'f')
            {
                await BufferReader.EnsureBytes<TMode>(5, cancellationToken).ConfigureAwait(false);
                int offset;
                byte[] buffer = BufferReader.GetBuffer(out offset, out _);
                if (buffer[offset + 1] != (byte)'a' ||
                    buffer[offset + 2] != (byte)'l' ||
                    buffer[offset + 3] != (byte)'s' ||
                    buffer[offset + 4] != (byte)'e')
                {
                    XmlExceptionHelper.ThrowTokenExpected(this, "false", Encoding.UTF8.GetString(buffer, offset, 5));
                }
                BufferReader.Advance(5);

                if ((nextByte = await TryGetByte<TMode>(cancellationToken).ConfigureAwait(false)) >= 0)
                {
                    ch = (byte)nextByte;
                    if (!IsWhitespace(ch) && ch != JsonGlobals.MemberSeparatorByte && ch != JsonGlobals.EndObjectChar && ch != JsonGlobals.EndCollectionByte)
                    {
                        XmlExceptionHelper.ThrowTokenExpected(this, "false", Encoding.UTF8.GetString(buffer, offset, 4) + (char)ch);
                    }
                }
                MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, 5);
            }
            else if (ch == (byte)'t')
            {
                await BufferReader.EnsureBytes<TMode>(4, cancellationToken).ConfigureAwait(false);
                int offset;
                byte[] buffer = BufferReader.GetBuffer(out offset, out _);
                if (buffer[offset + 1] != (byte)'r' ||
                    buffer[offset + 2] != (byte)'u' ||
                    buffer[offset + 3] != (byte)'e')
                {
                    XmlExceptionHelper.ThrowTokenExpected(this, "true", Encoding.UTF8.GetString(buffer, offset, 4));
                }
                BufferReader.Advance(4);

                if ((nextByte = await TryGetByte<TMode>(cancellationToken).ConfigureAwait(false)) >= 0)
                {
                    ch = (byte)nextByte;
                    if (!IsWhitespace(ch) && ch != JsonGlobals.MemberSeparatorByte && ch != JsonGlobals.EndObjectChar && ch != JsonGlobals.EndCollectionByte)
                    {
                        XmlExceptionHelper.ThrowTokenExpected(this, "true", Encoding.UTF8.GetString(buffer, offset, 4) + (char)ch);
                    }
                }
                MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, 4);
            }
            else if (ch == (byte)'n')
            {
                await BufferReader.EnsureBytes<TMode>(4, cancellationToken).ConfigureAwait(false);
                int offset;
                byte[] buffer = BufferReader.GetBuffer(out offset, out _);
                if (buffer[offset + 1] != (byte)'u' ||
                    buffer[offset + 2] != (byte)'l' ||
                    buffer[offset + 3] != (byte)'l')
                {
                    XmlExceptionHelper.ThrowTokenExpected(this, "null", Encoding.UTF8.GetString(buffer, offset, 4));
                }
                BufferReader.Advance(4);
                await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);

                if ((nextByte = await TryGetByte<TMode>(cancellationToken).ConfigureAwait(false)) >= 0)
                {
                    ch = (byte)nextByte;
                    if (ch == JsonGlobals.MemberSeparatorByte || ch == JsonGlobals.EndObjectChar)
                    {
                        BufferReader.SkipByte();
                    }
                    else if (ch != JsonGlobals.EndCollectionByte)
                    {
                        XmlExceptionHelper.ThrowTokenExpected(this, "null", Encoding.UTF8.GetString(buffer, offset, 4) + (char)ch);
                    }
                }
                else
                {
                    _charactersToSkipOnNextRead[0] = JsonGlobals.MemberSeparatorByte;
                    _charactersToSkipOnNextRead[1] = JsonGlobals.EndObjectByte;
                }
                MoveToEndElement();
            }
            else if ((ch == (byte)'-') ||
                (((byte)'0' <= ch) && (ch <= (byte)'9')) ||
                (ch == (byte)'I') ||
                (ch == (byte)'N'))
            {
                await ReadNumericalText<TMode>(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                XmlExceptionHelper.ThrowXmlException(this,
                    new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)ch)));
            }

            return true;
        }

        public override decimal ReadContentAsDecimal()
        {
            return ReadContentAsCoreSync<decimal>();
        }

        public override int ReadContentAsInt()
        {
            return ReadContentAsCoreSync<int>();
        }

        public override long ReadContentAsLong()
        {
            return ReadContentAsCoreSync<long>();
        }

        protected override async ValueTask<T> ReadContentAsCore<T, TMode>(CancellationToken cancellationToken)
        {
            if (typeof(T) != typeof(int) && typeof(T) != typeof(long) && typeof(T) != typeof(decimal))
            {
                return await base.ReadContentAsCore<T, TMode>(cancellationToken).ConfigureAwait(false);
            }

            CheckReadMode<TMode>(cancellationToken);
            if (TryReadAtomicNumber(out T number))
            {
                await SkipValue<TMode>(Node, cancellationToken).ConfigureAwait(false);
                return number;
            }

            string value = typeof(TMode) == typeof(SyncXmlStreamIO) ? ReadContentAsString() :
                await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (typeof(T) == typeof(int))
            {
                return (T)(object)ParseInt(value, NumberStyles.Float);
            }

            try
            {
                return typeof(T) == typeof(long) ?
                    (T)(object)long.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo) :
                    (T)(object)decimal.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
            {
                throw XmlExceptionHelper.CreateConversionException(value, typeof(T) == typeof(long) ? "Int64" : "decimal", exception);
            }
        }

        private bool TryReadAtomicNumber<T>(out T value)
        {
            if (TryGetAtomicValue(out ValueHandle content) && content.TryGetUtf8Bytes(out ReadOnlySpan<byte> bytes) &&
                bytes.Length <= Quotas.MaxStringContentLength)
            {
                if (typeof(T) == typeof(int) && int.TryParse(bytes, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out int intValue))
                {
                    value = (T)(object)intValue;
                    return true;
                }
                if (typeof(T) == typeof(long) && long.TryParse(bytes, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out long longValue))
                {
                    value = (T)(object)longValue;
                    return true;
                }
                if (typeof(T) == typeof(decimal) && decimal.TryParse(bytes, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out decimal decimalValue))
                {
                    value = (T)(object)decimalValue;
                    return true;
                }
            }

            value = default!;
            return false;
        }

        public override int ReadValueAsBase64(byte[] buffer, int offset, int count)
        {
            if (IsAttributeValue)
            {
                ArgumentNullException.ThrowIfNull(buffer);
                ArgumentOutOfRangeException.ThrowIfNegative(offset);
                if (offset > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length));
                }
                ArgumentOutOfRangeException.ThrowIfNegative(count);
                if (count > buffer.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset));
                }

                return 0;
            }

            return base.ReadValueAsBase64(buffer, offset, count);
        }

        public override int ReadValueChunk(char[] chars, int offset, int count)
        {
            if (IsAttributeValue)
            {
                ArgumentNullException.ThrowIfNull(chars);
                ArgumentOutOfRangeException.ThrowIfNegative(offset);
                if (offset > chars.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, chars.Length));
                }
                ArgumentOutOfRangeException.ThrowIfNegative(count);
                if (count > chars.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, chars.Length - offset));
                }
                int actual;

                string value = UnescapeJsonString(this.Node.ValueAsString);
                actual = Math.Min(count, value.Length);
                if (actual > 0)
                {
                    value.CopyTo(0, chars, offset, actual);
                    if (this.Node.QNameType == QNameType.Xmlns)
                    {
                        this.Node.Namespace.Uri.SetValue(0, 0);
                    }
                    else
                    {
                        this.Node.Value.SetValue(ValueHandleType.UTF8, 0, 0);
                    }
                }
                return actual;
            }

            return base.ReadValueChunk(chars, offset, count);
        }

        public void SetInput(byte[] buffer, int offset, int count, Encoding? encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose? onClose)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.JsonOffsetExceedsBufferSize, buffer.Length));
            }
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.JsonSizeExceedsRemainingBufferSpace, buffer.Length - offset));
            }
            MoveToInitial(quotas, onClose);

            ArraySegment<byte> seg = JsonEncodingStreamWrapper.ProcessBuffer(buffer, offset, count, encoding);
            BufferReader.SetBuffer(seg.Array!, seg.Offset, seg.Count, null, null);
            _buffered = true;
            ResetState();
        }

        public void SetInput(Stream stream, Encoding? encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose? onClose)
        {
            ArgumentNullException.ThrowIfNull(stream);

            MoveToInitial(quotas, onClose);

            stream = new JsonEncodingStreamWrapper(stream, encoding, true);

            BufferReader.SetBuffer(stream, null, null);
            _buffered = false;
            ResetState();
        }

        internal async ValueTask SetInputAsync(Stream stream, Encoding? encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose? onClose, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream);

            cancellationToken.ThrowIfCancellationRequested();
            MoveToInitial(quotas, onClose);
            stream = await JsonEncodingStreamWrapper.CreateReaderAsync(stream, encoding, cancellationToken).ConfigureAwait(false);
            BufferReader.SetBuffer(stream, null, null);
            _buffered = false;
            ResetState();
        }

        public override void StartCanonicalization(Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            throw new NotSupportedException();
        }

        internal static void CheckArray(Array array, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, array.Length));
            }
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > array.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, array.Length - offset));
            }
        }

        private static int BreakText(byte[] buffer, int offset, int length)
        {
            // See if we might be breaking a utf8 sequence
            if (length > 0 && (buffer[offset + length - 1] & 0x80) == 0x80)
            {
                // Find the lead char of the utf8 sequence (0x11xxxxxx)
                int originalLength = length;
                do
                {
                    length--;
                } while (length > 0 && (buffer[offset + length] & 0xC0) != 0xC0);
                // Couldn't find the lead char
                if (length == 0)
                {
                    return originalLength; // Invalid utf8 sequence - can't break
                }
                // Count how many bytes follow the lead char
                byte b = unchecked((byte)(buffer[offset + length] << 2));
                int byteCount = 2;
                while ((b & 0x80) == 0x80)
                {
                    b = (byte)(b << 1);
                    byteCount++;
                    // There shouldn't be more than 3 bytes following the lead char
                    if (byteCount > 4)
                    {
                        return originalLength; // Invalid utf8 sequence - can't break
                    }
                }
                if (length + byteCount == originalLength)
                {
                    return originalLength; // sequence fits exactly
                }
            }
            return length;
        }

        private static int ComputeNumericalTextLength(byte[] buffer, int offset, int offsetMax)
        {
            int beginOffset = offset;
            while (offset < offsetMax)
            {
                byte ch = buffer[offset];
                if (ch == JsonGlobals.MemberSeparatorByte || ch == JsonGlobals.EndObjectByte || ch == JsonGlobals.EndCollectionByte
                    || IsWhitespace(ch))
                {
                    break;
                }
                offset++;
            }
            return offset - beginOffset;
        }

        private static int ComputeQuotedTextLengthUntilEndQuote(byte[] buffer, int offset, int offsetMax, out bool escaped)
        {
            // Assumes that for quoted text "someText", the first " has been consumed.
            // For original text "someText", buffer passed in is someText".
            // This method returns return 8 for someText" (s, o, m, e, T, e, x, t).
            int beginOffset = offset;
            escaped = false;

            while (offset < offsetMax)
            {
                byte ch = buffer[offset];
                if (ch < 0x20)
                {
                    throw new FormatException(SR.Format(SR.InvalidCharacterEncountered, (char)ch));
                }
                else if (ch == (byte)'\\' || ch == 0xEF)
                {
                    escaped = true;
                    break;
                }
                else if (ch == JsonGlobals.QuoteByte)
                {
                    break;
                }

                offset++;
            }

            return offset - beginOffset;
        }


        // From JSON spec:
        // ws = *(
        //    %x20 /              ; Space
        //    %x09 /              ; Horizontal tab
        //    %x0A /              ; Line feed or New line
        //    %x0D                ; Carriage return
        // )
        private static bool IsWhitespace(byte ch)
        {
            return ((ch == 0x20) || (ch == 0x09) || (ch == 0x0A) || (ch == 0x0D));
        }

        private static char ParseChar(ReadOnlySpan<char> value, NumberStyles style)
        {
            int intValue = ParseInt(value, style);
            try
            {
                return Convert.ToChar(intValue);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(), "char", exception);
            }
        }

        private static int ParseInt(ReadOnlySpan<char> value, NumberStyles style)
        {
            try
            {
                return int.Parse(value, style, NumberFormatInfo.InvariantInfo);
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(), "Int32", exception);
            }
        }

        private async ValueTask BufferElement<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int elementOffset = BufferReader.Offset;
            const int byteCount = 128;
            bool done = false;
            byte quoteChar = 0;
            bool escaped = false;
            while (!done)
            {
                int offset;
                int offsetMax;
                byte[] buffer = BufferReader.GetBuffer(out offset, out offsetMax);
                if (offset == offsetMax)
                {
                    offsetMax = await BufferReader.GetBuffer<TMode>(byteCount, cancellationToken).ConfigureAwait(false);
                    buffer = BufferReader.GetBuffer(out offset, out _);
                }
                for (int i = offset; i < offsetMax && !done; i++)
                {
                    byte b = buffer[i];
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (b == '\\')
                    {
                        escaped = true;
                    }
                    else if (quoteChar == 0)
                    {
                        if (b == (byte)'\'' || b == JsonGlobals.QuoteByte)
                        {
                            quoteChar = b;
                        }
                        if (b == JsonGlobals.NameValueSeparatorByte)
                        {
                            done = true;
                        }
                    }
                    else
                    {
                        if (b == quoteChar)
                        {
                            quoteChar = 0;
                        }
                    }
                }
                if (!done)
                {
                    BufferReader.Advance(offsetMax - offset);
                    await BufferReader.EnsureBytes<TMode>(1, cancellationToken).ConfigureAwait(false);
                }
            }
            BufferReader.Offset = elementOffset;
        }

        private void EnterJsonScope(JsonNodeType currentNodeType)
        {
            _scopeDepth++;
            if (_scopes == null)
            {
                _scopes = new JsonNodeType[4];
            }
            else if (_scopes.Length == _scopeDepth)
            {
                JsonNodeType[] newScopes = new JsonNodeType[_scopeDepth * 2];
                Array.Copy(_scopes, newScopes, _scopeDepth);
                _scopes = newScopes;
            }
            _scopes[_scopeDepth] = currentNodeType;
        }

        private JsonNodeType ExitJsonScope()
        {
            JsonNodeType nodeTypeToReturn = _scopes![_scopeDepth];
            _scopes[_scopeDepth] = JsonNodeType.None;
            _scopeDepth--;
            return nodeTypeToReturn;
        }

        private new void MoveToEndElement()
        {
            ExitJsonScope();
            base.MoveToEndElement();
        }

        private void MoveToInitial(XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose)
        {
            MoveToInitial(quotas);
            _maxBytesPerRead = quotas.MaxBytesPerRead;
            _onReaderClose = onClose;
        }

        private async ValueTask ParseAndSetLocalName<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            XmlElementNode elementNode = EnterScope();
            elementNode.NameOffset = BufferReader.Offset;

            do
            {
                if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) == '\\')
                {
                    await ReadEscapedCharacter<TMode>(false, cancellationToken).ConfigureAwait(false); //  moveToText
                }
                else
                {
                    await ReadQuotedText<TMode>(false, cancellationToken).ConfigureAwait(false); //  moveToText
                }
            } while (_complexTextMode == JsonComplexTextMode.QuotedText);

            int actualOffset = BufferReader.Offset - 1; //  -1 to ignore " at end of local name
            elementNode.LocalName.SetValue(elementNode.NameOffset, actualOffset - elementNode.NameOffset);
            elementNode.NameLength = actualOffset - elementNode.NameOffset;
            elementNode.Namespace.Uri.SetValue(elementNode.NameOffset, 0);
            elementNode.Prefix.SetValue(PrefixHandleType.Empty);
            elementNode.IsEmptyElement = false;
            elementNode.ExitScope = false;
            elementNode.BufferOffset = actualOffset;

            int currentCharacter = (int)BufferReader.GetByte(elementNode.NameOffset);
            if ((CharTypes[currentCharacter] & CharType.FirstName) == 0)
            {
                SetJsonNameWithMapping(elementNode);
            }
            else
            {
                for (int i = 0, offset = elementNode.NameOffset; i < elementNode.NameLength; i++, offset++)
                {
                    currentCharacter = (int)BufferReader.GetByte(offset);
                    if ((CharTypes[currentCharacter] & CharType.Name) == 0 || currentCharacter >= 0x80)
                    {
                        SetJsonNameWithMapping(elementNode);
                        break;
                    }
                }
            }
        }

        private async ValueTask ParseStartElement<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (!_buffered)
            {
                await BufferElement<TMode>(cancellationToken).ConfigureAwait(false);
            }

            _expectingFirstElementInNonPrimitiveChild = false;

            byte ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            if (ch == JsonGlobals.QuoteByte)
            {
                BufferReader.SkipByte();

                await ParseAndSetLocalName<TMode>(cancellationToken).ConfigureAwait(false);

                await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);
                await SkipExpectedByteInBufferReader<TMode>(JsonGlobals.NameValueSeparatorByte, cancellationToken).ConfigureAwait(false);
                await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);


                if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) == JsonGlobals.ObjectByte)
                {
                    BufferReader.SkipByte();
                    _expectingFirstElementInNonPrimitiveChild = true;
                }
                await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // " and } are the only two valid characters that may follow a {
                XmlExceptionHelper.ThrowTokenExpected(this, "\"", (char)ch);
            }
        }

        private async ValueTask ReadAttributes<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            XmlAttributeNode attribute = AddAttribute();
            attribute.LocalName.SetConstantValue(StringHandleConstStringType.Type);
            attribute.Namespace.Uri.SetValue(0, 0);
            attribute.Prefix.SetValue(PrefixHandleType.Empty);

            await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);
            byte nextByte = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            switch (nextByte)
            {
                case JsonGlobals.QuoteByte:
                    if (!_expectingFirstElementInNonPrimitiveChild)
                    {
                        attribute.Value.SetConstantValue(ValueHandleConstStringType.String);
                    }
                    else
                    {
                        attribute.Value.SetConstantValue(ValueHandleConstStringType.Object);
                        await ReadServerTypeAttribute<TMode>(true, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case (byte)'n':
                    attribute.Value.SetConstantValue(ValueHandleConstStringType.Null);
                    break;
                case (byte)'t':
                case (byte)'f':
                    attribute.Value.SetConstantValue(ValueHandleConstStringType.Boolean);
                    break;
                case JsonGlobals.ObjectByte:
                    attribute.Value.SetConstantValue(ValueHandleConstStringType.Object);
                    await ReadServerTypeAttribute<TMode>(false, cancellationToken).ConfigureAwait(false);
                    break;
                case JsonGlobals.EndObjectByte:
                    if (_expectingFirstElementInNonPrimitiveChild)
                    {
                        attribute.Value.SetConstantValue(ValueHandleConstStringType.Object);
                    }
                    else
                    {
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)nextByte)));
                    }
                    break;
                case JsonGlobals.CollectionByte:
                    attribute.Value.SetConstantValue(ValueHandleConstStringType.Array);
                    BufferReader.SkipByte();
                    EnterJsonScope(JsonNodeType.Collection);
                    break;
                default:
                    if (nextByte == '-' ||
                        (nextByte <= '9' && nextByte >= '0') ||
                        nextByte == 'N' ||
                        nextByte == 'I')
                    {
                        attribute.Value.SetConstantValue(ValueHandleConstStringType.Number);
                    }
                    else
                    {
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)nextByte)));
                    }
                    break;
            }
        }

        private async ValueTask ReadEscapedCharacter<TMode>(bool moveToText, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            BufferReader.SkipByte();
            char ch = (char)await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            if (ch == 'u')
            {
                BufferReader.SkipByte();
                await BufferReader.EnsureBytes<TMode>(5, cancellationToken).ConfigureAwait(false);
                int offset;
                byte[] buffer = BufferReader.GetBuffer(out offset, out _);
                string bufferAsString = Encoding.UTF8.GetString(buffer, offset, 4);
                BufferReader.Advance(4);
                int charValue = ParseChar(bufferAsString, NumberStyles.HexNumber);
                if (char.IsHighSurrogate((char)charValue))
                {
                    byte nextByte = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                    if (nextByte == (byte)'\\')
                    {
                        BufferReader.SkipByte();
                        await SkipExpectedByteInBufferReader<TMode>((byte)'u', cancellationToken).ConfigureAwait(false);
                        await BufferReader.EnsureBytes<TMode>(5, cancellationToken).ConfigureAwait(false);
                        buffer = BufferReader.GetBuffer(out offset, out _);
                        bufferAsString = Encoding.UTF8.GetString(buffer, offset, 4);
                        BufferReader.Advance(4);
                        char lowChar = ParseChar(bufferAsString, NumberStyles.HexNumber);
                        if (!char.IsLowSurrogate(lowChar))
                        {
                            XmlExceptionHelper.ThrowXmlException(this,
                                new XmlException(SR.Format(SR.XmlInvalidLowSurrogate, bufferAsString)));
                        }
                        charValue = new SurrogateChar(lowChar, (char)charValue).Char;
                    }
                }

                if (buffer[offset + 4] == JsonGlobals.QuoteByte)
                {
                    BufferReader.SkipByte();
                    if (moveToText)
                    {
                        MoveToAtomicText().Value.SetCharValue(charValue);
                    }
                    _complexTextMode = JsonComplexTextMode.None;
                }
                else
                {
                    if (moveToText)
                    {
                        MoveToComplexText().Value.SetCharValue(charValue);
                    }
                    _complexTextMode = JsonComplexTextMode.QuotedText;
                }
            }
            else
            {
                switch (ch)
                {
                    case 'b':
                        ch = '\b';
                        break;
                    case 'f':
                        ch = '\f';
                        break;
                    case 'n':
                        ch = '\n';
                        break;
                    case 'r':
                        ch = '\r';
                        break;
                    case 't':
                        ch = '\t';
                        break;
                    case '\"':
                    case '\\':
                    case '/':
                        // Do nothing. These are the actual unescaped values.
                        break;
                    default:
                        XmlExceptionHelper.ThrowXmlException(this,
                            new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, (char)ch)));
                        break;
                }
                BufferReader.SkipByte();
                if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) == JsonGlobals.QuoteByte)
                {
                    BufferReader.SkipByte();
                    if (moveToText)
                    {
                        MoveToAtomicText().Value.SetCharValue(ch);
                    }
                    _complexTextMode = JsonComplexTextMode.None;
                }
                else
                {
                    if (moveToText)
                    {
                        MoveToComplexText().Value.SetCharValue(ch);
                    }
                    _complexTextMode = JsonComplexTextMode.QuotedText;
                }
            }
        }

        private async ValueTask ReadNonExistentElementName<TMode>(StringHandleConstStringType elementName, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            EnterJsonScope(JsonNodeType.Object);
            XmlElementNode elementNode = EnterScope();
            elementNode.LocalName.SetConstantValue(elementName);
            elementNode.Namespace.Uri.SetValue(elementNode.NameOffset, 0);
            elementNode.Prefix.SetValue(PrefixHandleType.Empty);
            elementNode.BufferOffset = BufferReader.Offset;
            elementNode.IsEmptyElement = false;
            elementNode.ExitScope = false;
            await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<int> ReadNonFFFE<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            await BufferReader.EnsureBytes<TMode>(3, cancellationToken).ConfigureAwait(false);
            int off;
            byte[] buff = BufferReader.GetBuffer(out off, out _);
            if (buff[off + 1] == 0xBF && (buff[off + 2] == 0xBE || buff[off + 2] == 0xBF))
            {
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.JsonInvalidFFFE));
            }
            return 3;
        }

        private ValueTask ReadNumericalText<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            byte[] buffer = BufferReader.GetBuffer(out int offset, out int offsetMax);
            int limit = offset + Math.Min(offsetMax - offset, MaxTextChunk);
            int length = ComputeNumericalTextLength(buffer, offset, limit);
            if (length < limit - offset)
            {
                CompleteNumericalText(offset, limit, length);
                return default;
            }

            return ReadNumericalTextCore<TMode>(cancellationToken);
        }

        private async ValueTask ReadNumericalTextCore<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            byte[] buffer;
            int offset;
            int offsetMax;
            int length;

            if (_buffered)
            {
                buffer = BufferReader.GetBuffer(out offset, out offsetMax);
                length = ComputeNumericalTextLength(buffer, offset, offsetMax);
            }
            else
            {
                offsetMax = await BufferReader.GetBuffer<TMode>(MaxTextChunk, cancellationToken).ConfigureAwait(false);
                buffer = BufferReader.GetBuffer(out offset, out _);
                length = ComputeNumericalTextLength(buffer, offset, offsetMax);
                length = BreakText(buffer, offset, length);
            }
            CompleteNumericalText(offset, offsetMax, length);
        }

        private void CompleteNumericalText(int offset, int offsetMax, int length)
        {
            BufferReader.Advance(length);
            if (offset < offsetMax - length || BufferReader.BufferedEndOfFile)
            {
                MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, length);
                _complexTextMode = JsonComplexTextMode.None;
            }
            else
            {
                MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, length);
                _complexTextMode = JsonComplexTextMode.NumericalText;
            }
        }

        private ValueTask ReadQuotedText<TMode>(bool moveToText, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            byte[] buffer = BufferReader.GetBuffer(out int offset, out int offsetMax);
            int limit = offset + Math.Min(offsetMax - offset, MaxTextChunk);
            int length = ComputeQuotedTextLengthUntilEndQuote(buffer, offset, limit, out bool escaped);
            if (!escaped && length < limit - offset)
            {
                CompleteQuotedText(moveToText, offset, length);
                return default;
            }
            if (escaped && length != 0)
            {
                BufferReader.Advance(length);
                if (moveToText)
                    MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, length);
                _complexTextMode = JsonComplexTextMode.QuotedText;
                return default;
            }
            if (escaped && IsQuotedTextBuffered(buffer, offset, offsetMax))
            {
                if (buffer[offset] == (byte)'\\')
                {
                    ReadEscapedCharacterSync(moveToText);
                }
                else
                {
                    length = ReadNonFFFESync();
                    BufferReader.Advance(length);
                    if (moveToText)
                        MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, length);
                    _complexTextMode = JsonComplexTextMode.QuotedText;
                }
                return default;
            }

            return ReadQuotedTextCore<TMode>(moveToText, cancellationToken);
        }

        private void CompleteQuotedText(bool moveToText, int offset, int length)
        {
            if (moveToText)
                MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, length);
            BufferReader.Advance(length + 1);
            _complexTextMode = JsonComplexTextMode.None;
        }

        private async ValueTask ReadQuotedTextCore<TMode>(bool moveToText, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            byte[] buffer;
            int offset;
            int offsetMax;
            int length;
            bool escaped;
            bool endReached;

            if (_buffered)
            {
                buffer = BufferReader.GetBuffer(out offset, out offsetMax);
                length = ComputeQuotedTextLengthUntilEndQuote(buffer, offset, offsetMax, out escaped);
                endReached = offset < offsetMax - length;
            }
            else
            {
                offsetMax = await BufferReader.GetBuffer<TMode>(MaxTextChunk, cancellationToken).ConfigureAwait(false);
                buffer = BufferReader.GetBuffer(out offset, out _);
                length = ComputeQuotedTextLengthUntilEndQuote(buffer, offset, offsetMax, out escaped);
                endReached = offset < offsetMax - length;
                length = BreakText(buffer, offset, length);
            }

            if (escaped && await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) == 0xEF)
            {
                offset = BufferReader.Offset;
                length = await ReadNonFFFE<TMode>(cancellationToken).ConfigureAwait(false);
            }

            if (!escaped && endReached)
            {
                CompleteQuotedText(moveToText, offset, length);
            }
            else
            {
                BufferReader.Advance(length);
                if ((length == 0) && escaped)
                {
                    await ReadEscapedCharacter<TMode>(moveToText, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (moveToText)
                    {
                        MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, length);
                    }
                    _complexTextMode = JsonComplexTextMode.QuotedText;
                }
            }
        }

        private async ValueTask ReadServerTypeAttribute<TMode>(bool consumedObjectChar, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (!consumedObjectChar)
            {
                await SkipExpectedByteInBufferReader<TMode>(JsonGlobals.ObjectByte, cancellationToken).ConfigureAwait(false);
                await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);

                // we only allow " or } after {
                byte ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                if (ch != JsonGlobals.QuoteByte && ch != JsonGlobals.EndObjectByte)
                {
                    XmlExceptionHelper.ThrowTokenExpected(this, "\"", (char)ch);
                }
            }
            else
            {
                await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);
            }

            int offset;
            int offsetMax;
            offsetMax = await BufferReader.GetBuffer<TMode>(8, cancellationToken).ConfigureAwait(false);
            byte[] buffer = BufferReader.GetBuffer(out offset, out _);
            if (offset + 8 <= offsetMax)
            {
                if (buffer[offset + 0] == (byte)'\"' &&
                    buffer[offset + 1] == (byte)'_' &&
                    buffer[offset + 2] == (byte)'_' &&
                    buffer[offset + 3] == (byte)'t' &&
                    buffer[offset + 4] == (byte)'y' &&
                    buffer[offset + 5] == (byte)'p' &&
                    buffer[offset + 6] == (byte)'e' &&
                    buffer[offset + 7] == (byte)'\"')
                {
                    XmlAttributeNode attribute = AddAttribute();

                    attribute.LocalName.SetValue(offset + 1, 6);
                    attribute.Namespace.Uri.SetValue(0, 0);
                    attribute.Prefix.SetValue(PrefixHandleType.Empty);
                    BufferReader.Advance(8);

                    if (!_buffered)
                    {
                        await BufferElement<TMode>(cancellationToken).ConfigureAwait(false);
                    }

                    await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);
                    await SkipExpectedByteInBufferReader<TMode>(JsonGlobals.NameValueSeparatorByte, cancellationToken).ConfigureAwait(false);
                    await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);
                    await SkipExpectedByteInBufferReader<TMode>(JsonGlobals.QuoteByte, cancellationToken).ConfigureAwait(false);

                    BufferReader.GetBuffer(out offset, out _);

                    do
                    {
                        if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) == '\\')
                        {
                            await ReadEscapedCharacter<TMode>(false, cancellationToken).ConfigureAwait(false); //  moveToText
                        }
                        else
                        {
                            await ReadQuotedText<TMode>(false, cancellationToken).ConfigureAwait(false); //  moveToText
                        }
                    } while (_complexTextMode == JsonComplexTextMode.QuotedText);

                    attribute.Value.SetValue(ValueHandleType.UTF8, offset, BufferReader.Offset - 1 - offset);

                    await SkipWhitespaceInBufferReader<TMode>(cancellationToken).ConfigureAwait(false);

                    if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) == JsonGlobals.MemberSeparatorByte)
                    {
                        BufferReader.SkipByte();
                        _readServerTypeElement = true;
                    }
                }
            }
            if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) == JsonGlobals.EndObjectByte)
            {
                BufferReader.SkipByte();
                _readServerTypeElement = false;
                _expectingFirstElementInNonPrimitiveChild = false;
            }
            else
            {
                _readServerTypeElement = true;
            }
        }

        private void ResetState()
        {
            _complexTextMode = JsonComplexTextMode.None;
            _expectingFirstElementInNonPrimitiveChild = false;
            _charactersToSkipOnNextRead = new byte[2];
            _scopeDepth = 0;
            if ((_scopes != null) && (_scopes.Length > JsonGlobals.maxScopeSize))
            {
                _scopes = null;
            }
        }

        private void SetJsonNameWithMapping(XmlElementNode elementNode)
        {
            Namespace ns = AddNamespace();
            ns.Prefix.SetValue(PrefixHandleType.A);
            ns.Uri.SetConstantValue(StringHandleConstStringType.Item);
            AddXmlnsAttribute(ns);

            XmlAttributeNode attribute = AddAttribute();
            attribute.LocalName.SetConstantValue(StringHandleConstStringType.Item);
            attribute.Namespace.Uri.SetValue(0, 0);
            attribute.Prefix.SetValue(PrefixHandleType.Empty);
            attribute.Value.SetValue(ValueHandleType.UTF8, elementNode.NameOffset, elementNode.NameLength);

            elementNode.NameLength = 0;
            elementNode.Prefix.SetValue(PrefixHandleType.A);
            elementNode.LocalName.SetConstantValue(StringHandleConstStringType.Item);
            elementNode.Namespace = ns;
        }

        private async ValueTask SkipExpectedByteInBufferReader<TMode>(byte characterToSkip, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            byte ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            if (ch != characterToSkip)
            {
                XmlExceptionHelper.ThrowTokenExpected(this, ((char)characterToSkip).ToString(), (char)ch);
            }
            BufferReader.SkipByte();
        }

        private ValueTask SkipWhitespaceInBufferReader<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            byte[] buffer = BufferReader.GetBuffer(out int offset, out int offsetMax);
            int start = offset;
            while (offset < offsetMax && IsWhitespace(buffer[offset]))
                offset++;
            BufferReader.Advance(offset - start);
            if (offset < offsetMax)
                return default;

            return SkipWhitespaceCore<TMode>(cancellationToken);
        }

        private async ValueTask SkipWhitespaceCore<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int ch;
            while ((ch = await TryGetByte<TMode>(cancellationToken).ConfigureAwait(false)) >= 0 && IsWhitespace((byte)ch))
            {
                BufferReader.SkipByte();
            }
        }

        private ValueTask<int> TryGetByte<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            return BufferReader.TryGetByte<TMode>(cancellationToken);
        }

        [return: NotNullIfNotNull(nameof(val))]
        private string? UnescapeJsonString(string? val)
        {
            if (val == null)
            {
                return null;
            }

            StringBuilder? sb = null;
            int startIndex = 0, count = 0;
            for (int i = 0; i < val.Length; i++)
            {
                if (val[i] == '\\')
                {
                    i++;
                    sb ??= new StringBuilder();
                    sb.Append(val, startIndex, count);
                    Debug.Assert(i < val.Length, "Found that an '\' was the last character in a string. ReadServerTypeAttriute validates that the escape sequence is valid when it calls ReadQuotedText and ReadEscapedCharacter");
                    if (i >= val.Length)
                    {
                        XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, val[i])));
                    }
                    switch (val[i])
                    {
                        case '"':
                        case '\'':
                        case '/':
                        case '\\':
                            sb.Append(val[i]);
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            if ((i + 3) >= val.Length)
                            {
                                XmlExceptionHelper.ThrowXmlException(this,
                                    new XmlException(SR.Format(SR.JsonEncounteredUnexpectedCharacter, val[i])));
                            }
                            sb.Append(ParseChar(val.AsSpan(i + 1, 4), NumberStyles.HexNumber));
                            i += 4;
                            break;
                    }
                    startIndex = i + 1;
                    count = 0;
                }
                else
                {
                    count++;
                }
            }
            if (sb == null)
            {
                return val;
            }
            if (count > 0)
            {
                sb.Append(val, startIndex, count);
            }

            return sb.ToString();
        }

        protected override XmlSigningNodeWriter CreateSigningNodeWriter()
        {
            throw new NotSupportedException(SR.Format(SR.JsonMethodNotSupported, "CreateSigningNodeWriter"));
        }

        private static class CharType
        {
            public const byte FirstName = 0x01;
            public const byte Name = 0x02;
            public const byte None = 0x00;
        }
    }
}
