// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Serialization; // For SR
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;


namespace System.Xml
{
    public interface IXmlTextReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, Encoding? encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose);
        void SetInput(Stream stream, Encoding? encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose);
    }

    internal sealed partial class XmlUTF8TextReader : XmlBaseReader, IXmlLineInfo, IXmlTextReaderInitializer
    {
        private const int MaxTextChunk = 2048;

        private readonly PrefixHandle _prefix;
        private readonly StringHandle _localName;
        private int[]? _rowOffsets;
        private OnXmlDictionaryReaderClose? _onClose;
        private bool _buffered;
        private int _maxBytesPerRead;
        private static readonly SearchValues<byte> s_textChars = CreateSearchValues(CharType.Text);
        private static readonly SearchValues<byte> s_attributeTextChars = CreateSearchValues(CharType.AttributeText);

        private static SearchValues<byte> CreateSearchValues(byte charType)
        {
            Span<byte> values = stackalloc byte[256];
            int count = 0;
            ReadOnlySpan<byte> charTypeMap = CharTypeMap;
            for (int i = 0; i < charTypeMap.Length; i++)
            {
                if ((charTypeMap[i] & charType) != 0)
                {
                    values[count++] = (byte)i;
                }
            }
            return SearchValues.Create(values.Slice(0, count));
        }

        private static ReadOnlySpan<byte> CharTypeMap => // 256
            [
                /*  0 (.) */
                         CharType.None,
                /*  1 (.) */
                         CharType.None,
                /*  2 (.) */
                         CharType.None,
                /*  3 (.) */
                         CharType.None,
                /*  4 (.) */
                         CharType.None,
                /*  5 (.) */
                         CharType.None,
                /*  6 (.) */
                         CharType.None,
                /*  7 (.) */
                         CharType.None,
                /*  8 (.) */
                         CharType.None,
                /*  9 (.) */
                         CharType.None|CharType.Comment|CharType.Whitespace|CharType.Text|CharType.SpecialWhitespace,
                /*  A (.) */
                         CharType.None|CharType.Comment|CharType.Whitespace|CharType.Text|CharType.SpecialWhitespace,
                /*  B (.) */
                         CharType.None,
                /*  C (.) */
                         CharType.None,
                /*  D (.) */
                         CharType.None|CharType.Comment|CharType.Whitespace,
                /*  E (.) */
                         CharType.None,
                /*  F (.) */
                         CharType.None,
                /* 10 (.) */
                         CharType.None,
                /* 11 (.) */
                         CharType.None,
                /* 12 (.) */
                         CharType.None,
                /* 13 (.) */
                         CharType.None,
                /* 14 (.) */
                         CharType.None,
                /* 15 (.) */
                         CharType.None,
                /* 16 (.) */
                         CharType.None,
                /* 17 (.) */
                         CharType.None,
                /* 18 (.) */
                         CharType.None,
                /* 19 (.) */
                         CharType.None,
                /* 1A (.) */
                         CharType.None,
                /* 1B (.) */
                         CharType.None,
                /* 1C (.) */
                         CharType.None,
                /* 1D (.) */
                         CharType.None,
                /* 1E (.) */
                         CharType.None,
                /* 1F (.) */
                         CharType.None,
                /* 20 ( ) */
                         CharType.None|CharType.Comment|CharType.Whitespace|CharType.Text|CharType.AttributeText|CharType.SpecialWhitespace,
                /* 21 (!) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 22 (") */
                         CharType.None|CharType.Comment|CharType.Text,
                /* 23 (#) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 24 ($) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 25 (%) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 26 (&) */
                         CharType.None|CharType.Comment,
                /* 27 (') */
                         CharType.None|CharType.Comment|CharType.Text,
                /* 28 (() */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 29 ()) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 2A (*) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 2B (+) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 2C (,) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 2D (-) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 2E (.) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 2F (/) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 30 (0) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 31 (1) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 32 (2) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 33 (3) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 34 (4) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 35 (5) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 36 (6) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 37 (7) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 38 (8) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 39 (9) */
                         CharType.None|CharType.Comment|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 3A (:) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 3B (;) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 3C (<) */
                         CharType.None|CharType.Comment,
                /* 3D (=) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 3E (>) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 3F (?) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 40 (@) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 41 (A) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 42 (B) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 43 (C) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 44 (D) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 45 (E) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 46 (F) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 47 (G) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 48 (H) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 49 (I) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 4A (J) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 4B (K) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 4C (L) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 4D (M) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 4E (N) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 4F (O) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 50 (P) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 51 (Q) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 52 (R) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 53 (S) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 54 (T) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 55 (U) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 56 (V) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 57 (W) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 58 (X) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 59 (Y) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 5A (Z) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 5B ([) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 5C (\) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 5D (]) */
                         CharType.None|CharType.Comment|CharType.AttributeText,
                /* 5E (^) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 5F (_) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 60 (`) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 61 (a) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 62 (b) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 63 (c) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 64 (d) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 65 (e) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 66 (f) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 67 (g) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 68 (h) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 69 (i) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 6A (j) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 6B (k) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 6C (l) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 6D (m) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 6E (n) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 6F (o) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 70 (p) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 71 (q) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 72 (r) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 73 (s) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 74 (t) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 75 (u) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 76 (v) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 77 (w) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 78 (x) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 79 (y) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 7A (z) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 7B ({) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 7C (|) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 7D (}) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 7E (~) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 7F (.) */
                         CharType.None|CharType.Comment|CharType.Text|CharType.AttributeText,
                /* 80 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 81 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 82 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 83 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 84 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 85 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 86 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 87 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 88 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 89 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 8A (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 8B (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 8C (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 8D (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 8E (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 8F (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 90 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 91 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 92 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 93 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 94 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 95 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 96 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 97 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 98 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 99 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 9A (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 9B (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 9C (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 9D (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 9E (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* 9F (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A0 (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A1 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A2 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A3 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A4 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A5 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A6 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A7 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A8 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* A9 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* AA (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* AB (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* AC (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* AD (.) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* AE (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* AF (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B0 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B1 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B2 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B3 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B4 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B5 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B6 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B7 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B8 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* B9 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* BA (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* BB (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* BC (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* BD (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* BE (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* BF (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C0 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C1 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C2 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C3 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C4 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C5 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C6 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C7 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C8 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* C9 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* CA (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* CB (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* CC (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* CD (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* CE (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* CF (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D0 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D1 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D2 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D3 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D4 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D5 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D6 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D7 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D8 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* D9 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* DA (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* DB (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* DC (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* DD (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* DE (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* DF (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E0 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E1 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E2 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E3 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E4 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E5 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E6 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E7 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E8 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* E9 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* EA (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* EB (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* EC (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* ED (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* EE (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* EF (?) */
                         CharType.None|CharType.FirstName|CharType.Name,
                /* F0 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* F1 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* F2 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* F3 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* F4 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* F5 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* F6 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* F7 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* F8 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* F9 (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* FA (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* FB (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* FC (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* FD (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* FE (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
                /* FF (?) */
                         CharType.None|CharType.Comment|CharType.FirstName|CharType.Name|CharType.Text|CharType.AttributeText,
            ];

        public XmlUTF8TextReader()
        {
            _prefix = new PrefixHandle(BufferReader);
            _localName = new StringHandle(BufferReader);
        }

        public void SetInput(byte[] buffer, int offset, int count, Encoding? encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset));
            MoveToInitial(quotas, onClose);
            ArraySegment<byte> seg = EncodingStreamWrapper.ProcessBuffer(buffer, offset, count, encoding);
            BufferReader.SetBuffer(seg.Array!, seg.Offset, seg.Count, null, null);
            _buffered = true;
        }

        public void SetInput(Stream stream, Encoding? encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose)
        {
            ArgumentNullException.ThrowIfNull(stream);

            MoveToInitial(quotas, onClose);
            stream = new EncodingStreamWrapper(stream, encoding);
            BufferReader.SetBuffer(stream, null, null);
            _buffered = false;
        }

        internal async ValueTask SetInputAsync(Stream stream, Encoding? encoding, XmlDictionaryReaderQuotas quotas,
            OnXmlDictionaryReaderClose? onClose, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream);

            cancellationToken.ThrowIfCancellationRequested();
            MoveToInitial(quotas, onClose);
            stream = await EncodingStreamWrapper.CreateReaderAsync(stream, encoding, cancellationToken).ConfigureAwait(false);
            BufferReader.SetBuffer(stream, null, null);
            _buffered = false;
        }

        private void MoveToInitial(XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose)
        {
            MoveToInitial(quotas);
            _maxBytesPerRead = quotas.MaxBytesPerRead;
            _onClose = onClose;
        }

        public override void Close()
        {
            _rowOffsets = null;
            base.Close();
            CloseCallback();
        }

        internal override async ValueTask CloseAsync()
        {
            _rowOffsets = null;
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

        private async ValueTask SkipWhitespace<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            while (!await BufferReader.EndOfFileCore<TMode>(cancellationToken).ConfigureAwait(false))
            {
                byte ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                if ((CharTypeMap[ch] & CharType.Whitespace) == 0)
                    break;
                BufferReader.SkipByte();
            }
        }

        private async ValueTask ReadDeclaration<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (!_buffered)
                await BufferElement<TMode>(cancellationToken).ConfigureAwait(false);
            await BufferReader.EnsureBytes<TMode>(5, cancellationToken).ConfigureAwait(false);
            int offset;
            byte[] buffer = BufferReader.GetBuffer(out offset, out _);
            if (buffer[offset + 0] != (byte)'?' ||
                buffer[offset + 1] != (byte)'x' ||
                buffer[offset + 2] != (byte)'m' ||
                buffer[offset + 3] != (byte)'l' ||
                (CharTypeMap[buffer[offset + 4]] & CharType.Whitespace) == 0)
            {
                XmlExceptionHelper.ThrowProcessingInstructionNotSupported(this);
            }
            // If anything came before the "<?xml ?>" it's an error.
            if (this.Node.ReadState != ReadState.Initial)
            {
                XmlExceptionHelper.ThrowDeclarationNotFirst(this);
            }
            BufferReader.Advance(5);

            int localNameOffset = offset + 1;
            int localNameLength = 3;

            int valueOffset = BufferReader.Offset;
            await SkipWhitespace<TMode>(cancellationToken).ConfigureAwait(false);
            await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
            int valueLength = BufferReader.Offset - valueOffset;

            // Backoff the spaces
            while (valueLength > 0)
            {
                byte ch = BufferReader.GetByte(valueOffset + valueLength - 1);
                if ((CharTypeMap[ch] & CharType.Whitespace) == 0)
                    break;
                valueLength--;
            }

            await BufferReader.EnsureBytes<TMode>(2, cancellationToken).ConfigureAwait(false);
            buffer = BufferReader.GetBuffer(out offset, out _);
            if (buffer[offset + 0] != (byte)'?' ||
                buffer[offset + 1] != (byte)'>')
            {
                XmlExceptionHelper.ThrowTokenExpected(this, "?>", Encoding.UTF8.GetString(buffer, offset, 2));
            }
            BufferReader.Advance(2);
            XmlDeclarationNode declarationNode = MoveToDeclaration();
            declarationNode.LocalName.SetValue(localNameOffset, localNameLength);
            declarationNode.Value.SetValue(ValueHandleType.UTF8, valueOffset, valueLength);
        }

        private void VerifyNCName(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                XmlExceptionHelper.ThrowXmlException(this, SR.InvalidLocalNameEmpty, null);
            }

            try
            {
                XmlConvert.VerifyNCName(s);
            }
            catch (XmlException exception)
            {
                XmlExceptionHelper.ThrowXmlException(this, exception);
            }
        }

        private void ReadQualifiedName(PrefixHandle prefix, StringHandle localName)
        {
            int offset;
            int offsetMax;
            byte[] buffer = BufferReader.GetBuffer(out offset, out offsetMax);

            int ch;
            int anyChar = 0;
            int prefixChar = 0;
            int prefixOffset = offset;
            if (offset < offsetMax)
            {
                ch = buffer[offset];
                prefixChar = ch;
                if ((CharTypeMap[ch] & CharType.FirstName) == 0)
                    anyChar |= 0x80;
                anyChar |= ch;
                offset++;
                while (offset < offsetMax)
                {
                    ch = buffer[offset];
                    if ((CharTypeMap[ch] & CharType.Name) == 0)
                        break;
                    anyChar |= ch;
                    offset++;
                }
            }
            else
            {
                anyChar |= 0x80;
                ch = 0;
            }
            if (ch == ':')
            {
                int prefixLength = offset - prefixOffset;
                if (prefixLength == 1 && prefixChar >= 'a' && prefixChar <= 'z')
                    prefix.SetValue(PrefixHandle.GetAlphaPrefix(prefixChar - 'a'));
                else
                    prefix.SetValue(prefixOffset, prefixLength);

                offset++;
                int localNameOffset = offset;
                if (offset < offsetMax)
                {
                    ch = buffer[offset];
                    if ((CharTypeMap[ch] & CharType.FirstName) == 0)
                        anyChar |= 0x80;
                    anyChar |= ch;
                    offset++;
                    while (offset < offsetMax)
                    {
                        ch = buffer[offset];
                        if ((CharTypeMap[ch] & CharType.Name) == 0)
                            break;
                        anyChar |= ch;
                        offset++;
                    }
                }
                else
                {
                    anyChar |= 0x80;
                }
                localName.SetValue(localNameOffset, offset - localNameOffset);
                if (anyChar >= 0x80)
                {
                    VerifyNCName(prefix.GetString());
                    VerifyNCName(localName.GetString());
                }
            }
            else
            {
                prefix.SetValue(PrefixHandleType.Empty);
                localName.SetValue(prefixOffset, offset - prefixOffset);
                if (anyChar >= 0x80)
                {
                    VerifyNCName(localName.GetString());
                }
            }
            BufferReader.Advance(offset - prefixOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadAttributeText(ReadOnlySpan<byte> buffer)
        {
            if (Vector128.IsHardwareAccelerated)
            {
                int end = buffer.IndexOfAnyExcept(s_attributeTextChars);
                return end < 0 ? buffer.Length : end;
            }

            ReadOnlySpan<byte> charTypeMap = CharTypeMap;
            int offset = 0;
            while (offset < buffer.Length && (charTypeMap[buffer[offset]] & CharType.AttributeText) != 0)
            {
                offset++;
            }
            return offset;
        }

        private async ValueTask ReadAttributes<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int startOffset = 0;
            if (_buffered)
                startOffset = BufferReader.Offset;

            while (true)
            {
                byte ch;
                ReadQualifiedName(_prefix, _localName);
                if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) != '=')
                {
                    await SkipWhitespace<TMode>(cancellationToken).ConfigureAwait(false);
                    if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) != '=')
                        XmlExceptionHelper.ThrowTokenExpected(this, "=", (char)await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false));
                }
                BufferReader.SkipByte();
                byte quoteChar = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                if (quoteChar != '"' && quoteChar != '\'')
                {
                    await SkipWhitespace<TMode>(cancellationToken).ConfigureAwait(false);
                    quoteChar = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                    if (quoteChar != '"' && quoteChar != '\'')
                        XmlExceptionHelper.ThrowTokenExpected(this, "\"", (char)quoteChar);
                }
                BufferReader.SkipByte();
                bool escaped = false;
                int valueOffset = BufferReader.Offset;
                while (true)
                {
                    int offset, offsetMax;
                    byte[] buffer = BufferReader.GetBuffer(out offset, out offsetMax);
                    int length = ReadAttributeText(buffer.AsSpan(offset, offsetMax - offset));
                    BufferReader.Advance(length);
                    ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                    if (ch == quoteChar)
                        break;
                    if (ch == '&')
                    {
                        await ReadCharRef<TMode>(cancellationToken).ConfigureAwait(false);
                        escaped = true;
                    }
                    else if (ch == '\'' || ch == '"')
                    {
                        BufferReader.SkipByte();
                    }
                    else if (ch == '\n' || ch == '\r' || ch == '\t')
                    {
                        BufferReader.SkipByte();
                        escaped = true;
                    }
                    else if (ch == 0xEF)
                    {
                        await ReadNonFFFE<TMode>(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        XmlExceptionHelper.ThrowTokenExpected(this, ((char)quoteChar).ToString(), (char)ch);
                    }
                }
                int valueLength = BufferReader.Offset - valueOffset;

                XmlAttributeNode attributeNode;
                if (_prefix.IsXmlns)
                {
                    Namespace ns = AddNamespace();
                    _localName.ToPrefixHandle(ns.Prefix);
                    ns.Uri.SetValue(valueOffset, valueLength, escaped);
                    attributeNode = AddXmlnsAttribute(ns);
                }
                else if (_prefix.IsEmpty && _localName.IsXmlns)
                {
                    Namespace ns = AddNamespace();
                    ns.Prefix.SetValue(PrefixHandleType.Empty);
                    ns.Uri.SetValue(valueOffset, valueLength, escaped);
                    attributeNode = AddXmlnsAttribute(ns);
                }
                else if (_prefix.IsXml)
                {
                    attributeNode = AddXmlAttribute();
                    attributeNode.Prefix.SetValue(_prefix);
                    attributeNode.LocalName.SetValue(_localName);
                    attributeNode.Value.SetValue((escaped ? ValueHandleType.EscapedUTF8 : ValueHandleType.UTF8), valueOffset, valueLength);
                    FixXmlAttribute(attributeNode);
                }
                else
                {
                    attributeNode = AddAttribute();
                    attributeNode.Prefix.SetValue(_prefix);
                    attributeNode.LocalName.SetValue(_localName);
                    attributeNode.Value.SetValue((escaped ? ValueHandleType.EscapedUTF8 : ValueHandleType.UTF8), valueOffset, valueLength);
                }

                attributeNode.QuoteChar = (char)quoteChar;
                BufferReader.SkipByte();

                ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);

                bool space = false;
                while ((CharTypeMap[ch] & CharType.Whitespace) != 0)
                {
                    space = true;
                    BufferReader.SkipByte();
                    ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                }

                if (ch == '>' || ch == '/' || ch == '?')
                    break;

                if (!space)
                    XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlSpaceBetweenAttributes));
            }

            if (_buffered && (BufferReader.Offset - startOffset) > _maxBytesPerRead)
                XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(this, _maxBytesPerRead);

            ProcessAttributes();
        }

        // NOTE: Call only if 0xEF has been seen in the stream AND there are three valid bytes to check (buffer[offset], buffer[offset + 1], buffer[offset + 2]).
        // 0xFFFE and 0xFFFF are not valid characters per Unicode specification. The first byte in the UTF8 representation is 0xEF.
        private static bool IsNextCharacterNonFFFE(byte[] buffer, int offset)
        {
            Debug.Assert(buffer[offset] == 0xEF, "buffer[offset] MUST be 0xEF.");

            if (buffer[offset + 1] == 0xBF && (buffer[offset + 2] == 0xBE || buffer[offset + 2] == 0xBF))
            {
                // 0xFFFE : 0xEF 0xBF 0xBE
                // 0xFFFF : 0xEF 0xBF 0xBF
                // we know that buffer[offset] is already 0xEF, don't bother checking it.
                return false;
            }

            // no bad characters
            return true;
        }

        private async ValueTask ReadNonFFFE<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            await BufferReader.EnsureBytes<TMode>(3, cancellationToken).ConfigureAwait(false);
            int off;
            byte[] buff = BufferReader.GetBuffer(out off, out _);
            if (buff[off + 1] == 0xBF && (buff[off + 2] == 0xBE || buff[off + 2] == 0xBF))
            {
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlInvalidFFFE));
            }
            BufferReader.Advance(3);
        }

        private async ValueTask BufferElement<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (IsElementBuffered(requirePlainText: false))
                return;

            int elementOffset = BufferReader.Offset;
            const int byteCount = 128;
            bool done = false;
            byte quoteChar = 0;
            while (!done)
            {
                int offset;
                int offsetMax;
                offsetMax = await BufferReader.GetBuffer<TMode>(byteCount, cancellationToken).ConfigureAwait(false);
                byte[] buffer = BufferReader.GetBuffer(out offset, out _);
                for (int i = offset; i < offsetMax && !done; i++)
                {
                    byte b = buffer[i];
                    if (quoteChar == 0)
                    {
                        if (b == '\'' || b == '"')
                            quoteChar = b;
                        if (b == '>')
                            done = true;
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

        private bool IsElementBuffered(bool requirePlainText)
        {
            byte[] buffer = BufferReader.GetBuffer(out int offset, out int offsetMax);
            byte quote = 0;
            for (int i = offset; i < offsetMax; i++)
            {
                byte ch = buffer[i];
                // Escaped or malformed content must retain the asynchronous refill path.
                if (requirePlainText && (ch == '&' || ch >= 0x80))
                    return false;
                if (quote != 0)
                {
                    if (ch == quote)
                        quote = 0;
                }
                else if (ch is (byte)'\'' or (byte)'"')
                {
                    quote = ch;
                }
                else if (ch == '>')
                {
                    return true;
                }
            }
            return false;
        }

        private bool TryReadBufferedNode()
        {
            byte[] buffer = BufferReader.GetBuffer(out int offset, out int offsetMax);
            if (offset >= offsetMax)
                return false;

            byte ch = buffer[offset];
            if (ch == '<')
            {
                if (offsetMax - offset < 2)
                    return false;
                byte next = buffer[offset + 1];
                if (next is (byte)'!' or (byte)'?')
                    return false;
                if (next == '/' ? !IsEndElementBuffered(buffer, offset + 2, offsetMax) :
                    !IsElementBuffered(requirePlainText: true))
                    return false;

                // The complete plain-text tag is in memory; these generated methods cannot refill.
                BufferReader.SkipByte();
                if (next == '/')
                {
                    BufferReader.SkipByte();
                    ReadEndElementContentSync();
                }
                else
                    ReadStartElementContentSync();
                return true;
            }
            if (OutsideRootElement || (CharTypeMap[ch] & CharType.Text) == 0)
                return false;

            int limit = _buffered ? offsetMax : Math.Min(offsetMax - offset, MaxTextChunk) + offset;
            int length = ReadText(buffer.AsSpan(offset, limit - offset));
            if (offset + length == limit && limit == offsetMax && offsetMax - offset < MaxTextChunk)
                return false;

            if (!_buffered)
                length = BreakText(buffer, offset, length);
            BufferReader.Advance(length);
            if (offset < limit - 1 - length && buffer[offset + length] == '<' && buffer[offset + length + 1] != '!')
                MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, length);
            else
                MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, length);
            return true;
        }

        internal bool TryReadBufferedPrimitiveElement<T>(out T value)
        {
            value = default!;
            if (Node.NodeType != XmlNodeType.Element || Node.IsEmptyElement || Signing)
                return false;
            if (!_buffered)
                BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, deferCompaction: true);

            byte[] buffer = BufferReader.GetBuffer(out int offset, out int offsetMax);
            int limit = _buffered ? offsetMax : offset + Math.Min(offsetMax - offset, MaxTextChunk);
            int length = ReadText(buffer.AsSpan(offset, limit - offset));
            int end = offset + length;
            if (length == 0 || limit - end < 3 || buffer[end] != '<' || buffer[end + 1] != '/' ||
                !IsEndElementBuffered(buffer, end + 2, limit))
                return false;

            ValueHandle content = MoveToAtomicText().Value;
            content.SetValue(ValueHandleType.UTF8, offset, length);
            value = ConvertAtomicValue<T>(content);
            BufferReader.Offset = end + 2;
            ReadEndElementContentSync();
            return true;
        }

        private bool IsEndElementBuffered(byte[] buffer, int offset, int limit)
        {
            int nameLength = ElementNode.NameLength;
            if (limit - offset <= nameLength ||
                !buffer.AsSpan(offset, nameLength).SequenceEqual(buffer.AsSpan(ElementNode.NameOffset, nameLength)))
                return false;

            offset += nameLength;
            while (offset < limit && (CharTypeMap[buffer[offset]] & CharType.Whitespace) != 0)
                offset++;
            return offset < limit && buffer[offset] == '>';
        }

        private async ValueTask ReadStartElement<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (!_buffered)
                await BufferElement<TMode>(cancellationToken).ConfigureAwait(false);
            await ReadStartElementContent<TMode>(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask ReadStartElementContent<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            XmlElementNode elementNode = EnterScope();
            elementNode.NameOffset = BufferReader.Offset;
            ReadQualifiedName(elementNode.Prefix, elementNode.LocalName);
            elementNode.NameLength = BufferReader.Offset - elementNode.NameOffset;
            byte ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            while ((CharTypeMap[ch] & CharType.Whitespace) != 0)
            {
                BufferReader.SkipByte();
                ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            }
            if (ch != '>' && ch != '/')
            {
                await ReadAttributes<TMode>(cancellationToken).ConfigureAwait(false);
                ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            }
            elementNode.Namespace = LookupNamespace(elementNode.Prefix);
            bool isEmptyElement = false;
            if (ch == '/')
            {
                isEmptyElement = true;
                BufferReader.SkipByte();
            }
            elementNode.IsEmptyElement = isEmptyElement;
            elementNode.ExitScope = isEmptyElement;
            if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) != '>')
                XmlExceptionHelper.ThrowTokenExpected(this, ">", (char)await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false));
            BufferReader.SkipByte();
            elementNode.BufferOffset = BufferReader.Offset;
        }

        private async ValueTask ReadEndElement<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            BufferReader.SkipByte();
            if (!_buffered)
                await BufferElement<TMode>(cancellationToken).ConfigureAwait(false);
            await ReadEndElementContent<TMode>(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask ReadEndElementContent<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            XmlElementNode elementNode = this.ElementNode;
            int nameOffset = elementNode.NameOffset;
            int nameLength = elementNode.NameLength;
            int offset;
            await BufferReader.EnsureBytes<TMode>(nameLength, cancellationToken).ConfigureAwait(false);
            byte[] buffer = BufferReader.GetBuffer(out offset, out _);
            for (int i = 0; i < nameLength; i++)
            {
                if (buffer[offset + i] != buffer[nameOffset + i])
                {
                    ReadQualifiedName(_prefix, _localName);
                    XmlExceptionHelper.ThrowTagMismatch(this, elementNode.Prefix.GetString(), elementNode.LocalName.GetString(), _prefix.GetString(), _localName.GetString());
                }
            }
            BufferReader.Advance(nameLength);
            if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) != '>')
            {
                await SkipWhitespace<TMode>(cancellationToken).ConfigureAwait(false);
                if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) != '>')
                    XmlExceptionHelper.ThrowTokenExpected(this, ">", (char)await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false));
            }
            BufferReader.SkipByte();
            MoveToEndElement();
        }

        private async ValueTask ReadComment<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            BufferReader.SkipByte();
            if (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) != '-')
                XmlExceptionHelper.ThrowTokenExpected(this, "--", (char)await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false));
            BufferReader.SkipByte();
            int commentOffset = BufferReader.Offset;
            while (true)
            {
                while (true)
                {
                    byte b = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                    if (b == '-')
                        break;
                    if ((CharTypeMap[b] & CharType.Comment) == 0)
                    {
                        if (b == 0xEF)
                            await ReadNonFFFE<TMode>(cancellationToken).ConfigureAwait(false);
                        else
                            XmlExceptionHelper.ThrowInvalidXml(this, b);
                    }
                    else
                    {
                        BufferReader.SkipByte();
                    }
                }

                int offset;
                await BufferReader.EnsureBytes<TMode>(3, cancellationToken).ConfigureAwait(false);
                byte[] buffer = BufferReader.GetBuffer(out offset, out _);
                if (buffer[offset + 0] == (byte)'-' &&
                    buffer[offset + 1] == (byte)'-')
                {
                    if (buffer[offset + 2] == (byte)'>')
                        break;
                    XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlInvalidCommentChars));
                }
                BufferReader.SkipByte();
            }
            int commentLength = BufferReader.Offset - commentOffset;
            MoveToComment().Value.SetValue(ValueHandleType.UTF8, commentOffset, commentLength);
            BufferReader.Advance(3);
        }

        private async ValueTask ReadCData<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            await BufferReader.EnsureBytes<TMode>(7, cancellationToken).ConfigureAwait(false);
            int offset;
            byte[] buffer = BufferReader.GetBuffer(out offset, out _);
            if (buffer[offset + 0] != (byte)'[' ||
                buffer[offset + 1] != (byte)'C' ||
                buffer[offset + 2] != (byte)'D' ||
                buffer[offset + 3] != (byte)'A' ||
                buffer[offset + 4] != (byte)'T' ||
                buffer[offset + 5] != (byte)'A' ||
                buffer[offset + 6] != (byte)'[')
            {
                XmlExceptionHelper.ThrowTokenExpected(this, "[CDATA[", Encoding.UTF8.GetString(buffer, offset, 7));
            }
            BufferReader.Advance(7);
            int cdataOffset = BufferReader.Offset;
            while (true)
            {
                byte b;
                while (true)
                {
                    b = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                    if (b == ']')
                        break;

                    if (b == 0xEF)
                        await ReadNonFFFE<TMode>(cancellationToken).ConfigureAwait(false);
                    else
                        BufferReader.SkipByte();
                }
                await BufferReader.EnsureBytes<TMode>(3, cancellationToken).ConfigureAwait(false);
                buffer = BufferReader.GetBuffer(out offset, out _);
                if (buffer[offset + 0] == (byte)']' &&
                    buffer[offset + 1] == (byte)']' &&
                    buffer[offset + 2] == (byte)'>')
                    break;
                BufferReader.SkipByte();
            }
            int cdataLength = BufferReader.Offset - cdataOffset;
            MoveToCData().Value.SetValue(ValueHandleType.UTF8, cdataOffset, cdataLength);
            BufferReader.Advance(3);
        }

        private async ValueTask<int> ReadCharRef<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            Debug.Assert(BufferReader.GetByte(BufferReader.Offset) == '&');
            int charEntityOffset = BufferReader.Offset;
            BufferReader.SkipByte();
            while (await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) != ';')
                BufferReader.SkipByte();
            BufferReader.SkipByte();
            int charEntityLength = BufferReader.Offset - charEntityOffset;
            BufferReader.Offset = charEntityOffset;
            int ch = BufferReader.GetCharEntity(charEntityOffset, charEntityLength);
            BufferReader.Advance(charEntityLength);
            return ch;
        }


        private async ValueTask ReadWhitespace<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            byte[] buffer;
            int offset;
            int offsetMax;
            int length;

            if (_buffered)
            {
                buffer = BufferReader.GetBuffer(out offset, out offsetMax);
                length = ReadWhitespace(buffer, offset, offsetMax);
            }
            else
            {
                offsetMax = await BufferReader.GetBuffer<TMode>(MaxTextChunk, cancellationToken).ConfigureAwait(false);
                buffer = BufferReader.GetBuffer(out offset, out _);
                length = ReadWhitespace(buffer, offset, offsetMax);
                length = BreakText(buffer, offset, length);
            }
            BufferReader.Advance(length);

            MoveToWhitespaceText().Value.SetValue(ValueHandleType.UTF8, offset, length);
        }

        private static int ReadWhitespace(byte[] buffer, int offset, int offsetMax)
        {
            ReadOnlySpan<byte> charTypeMap = XmlUTF8TextReader.CharTypeMap;
            int wsOffset = offset;
            while (offset < offsetMax && (charTypeMap[buffer[offset]] & CharType.SpecialWhitespace) != 0)
                offset++;
            return offset - wsOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadText(ReadOnlySpan<byte> buffer)
        {
            if (Vector128.IsHardwareAccelerated)
            {
                int end = buffer.IndexOfAnyExcept(s_textChars);
                return end < 0 ? buffer.Length : end;
            }

            ReadOnlySpan<byte> charTypeMap = CharTypeMap;
            int offset = 0;
            while (offset < buffer.Length && (charTypeMap[buffer[offset]] & CharType.Text) != 0)
            {
                offset++;
            }
            return offset;
        }

        // Read Unicode codepoints 0xFvvv
        private int ReadTextAndWatchForInvalidCharacters(byte[] buffer, int offset, int offsetMax)
        {
            ReadOnlySpan<byte> charTypeMap = XmlUTF8TextReader.CharTypeMap;
            int textOffset = offset;

            while (offset < offsetMax && ((charTypeMap[buffer[offset]] & CharType.Text) != 0 || buffer[offset] == 0xEF))
            {
                if (buffer[offset] != 0xEF)
                {
                    offset++;
                }
                else
                {
                    // Ensure that we have three bytes (buffer[offset], buffer[offset + 1], buffer[offset + 2])
                    // available for IsNextCharacterNonFFFE to check.
                    if (offset + 2 < offsetMax)
                    {
                        if (IsNextCharacterNonFFFE(buffer, offset))
                        {
                            // if first byte is 0xEF, UTF8 mandates a 3-byte character representation of this Unicode code point
                            offset += 3;
                        }
                        else
                        {
                            XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlInvalidFFFE));
                        }
                    }
                    else
                    {
                        if (BufferReader.Offset < offset)
                        {
                            // We have read some characters already
                            // Let the outer ReadText advance the bufferReader and return text node to caller
                            break;
                        }
                        else
                        {
                            XmlExceptionHelper.ThrowUnexpectedEndOfFile(this);
                        }
                    }
                }
            }
            return offset - textOffset;
        }

        // bytes   bits    UTF-8 representation
        // -----   ----    -----------------------------------
        // 1        7      0vvvvvvv
        // 2       11      110vvvvv 10vvvvvv
        // 3       16      1110vvvv 10vvvvvv 10vvvvvv
        // 4       21      11110vvv 10vvvvvv 10vvvvvv 10vvvvvv
        // -----   ----    -----------------------------------

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
                }
                while (length > 0 && (buffer[offset + length] & 0xC0) != 0xC0);
                // Couldn't find the lead char
                if (length == 0)
                    return originalLength; // Invalid utf8 sequence - can't break
                // Count how many bytes follow the lead char
                byte b = unchecked((byte)(buffer[offset + length] << 2));
                int byteCount = 2;
                while ((b & 0x80) == 0x80)
                {
                    b = unchecked((byte)(b << 1));
                    byteCount++;
                    // There shouldn't be more than 3 bytes following the lead char
                    if (byteCount > 4)
                        return originalLength; // Invalid utf8 sequence - can't break
                }
                if (length + byteCount == originalLength)
                    return originalLength; // sequence fits exactly
            }
            return length;
        }

        private async ValueTask ReadText<TMode>(bool hasLeadingByteOf0xEF, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (hasLeadingByteOf0xEF)
                await BufferReader.EnsureBytes<TMode>(3, cancellationToken).ConfigureAwait(false);
            byte[] buffer;
            int offset;
            int offsetMax;
            int length;

            if (_buffered)
            {
                buffer = BufferReader.GetBuffer(out offset, out offsetMax);
                if (hasLeadingByteOf0xEF)
                {
                    length = ReadTextAndWatchForInvalidCharacters(buffer, offset, offsetMax);
                }
                else
                {
                    length = ReadText(buffer.AsSpan(offset, offsetMax - offset));
                }
            }
            else
            {
                offsetMax = await BufferReader.GetBuffer<TMode>(MaxTextChunk, cancellationToken).ConfigureAwait(false);
                buffer = BufferReader.GetBuffer(out offset, out _);
                if (hasLeadingByteOf0xEF)
                {
                    length = ReadTextAndWatchForInvalidCharacters(buffer, offset, offsetMax);
                }
                else
                {
                    length = ReadText(buffer.AsSpan(offset, offsetMax - offset));
                }
                length = BreakText(buffer, offset, length);
            }
            BufferReader.Advance(length);

            if (offset < offsetMax - 1 - length && (buffer[offset + length] == (byte)'<' && buffer[offset + length + 1] != (byte)'!'))
            {
                MoveToAtomicText().Value.SetValue(ValueHandleType.UTF8, offset, length);
            }
            else
            {
                MoveToComplexText().Value.SetValue(ValueHandleType.UTF8, offset, length);
            }
        }

        private async ValueTask ReadEscapedText<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int ch = await ReadCharRef<TMode>(cancellationToken).ConfigureAwait(false);
            if (ch < 256 && (CharTypeMap[ch] & CharType.Whitespace) != 0)
                MoveToWhitespaceText().Value.SetCharValue(ch);
            else
                MoveToComplexText().Value.SetCharValue(ch);
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

            if (this.Node.CanMoveToElement)
            {
                // If we're positioned on an attribute or attribute text on an empty element, we need to move back
                // to the element in order to get the correct setting of ExitScope
                MoveToElement();
            }

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
            if (this.Node.ExitScope)
            {
                ExitScope();
            }
            if (!_buffered)
                BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead, typeof(TMode) == typeof(AsyncXmlStreamIO));

            if (typeof(TMode) == typeof(AsyncXmlStreamIO) && TryReadBufferedNode())
                return new ValueTask<bool>(true);

            return ReadNode<TMode>(cancellationToken);
        }

        private async ValueTask<bool> ReadNode<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (await BufferReader.EndOfFileCore<TMode>(cancellationToken).ConfigureAwait(false))
            {
                MoveToEndOfFile();
                return false;
            }
            byte ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            if (ch == (byte)'<')
            {
                BufferReader.SkipByte();
                ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                if (ch == (byte)'/')
                    await ReadEndElement<TMode>(cancellationToken).ConfigureAwait(false);
                else if (ch == (byte)'!')
                {
                    BufferReader.SkipByte();
                    ch = await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                    if (ch == '-')
                    {
                        await ReadComment<TMode>(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        if (OutsideRootElement)
                            XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlCDATAInvalidAtTopLevel));

                        await ReadCData<TMode>(cancellationToken).ConfigureAwait(false);
                    }
                }
                else if (ch == (byte)'?')
                    await ReadDeclaration<TMode>(cancellationToken).ConfigureAwait(false);
                else
                    await ReadStartElement<TMode>(cancellationToken).ConfigureAwait(false);
            }
            else if ((CharTypeMap[ch] & CharType.SpecialWhitespace) != 0)
            {
                await ReadWhitespace<TMode>(cancellationToken).ConfigureAwait(false);
            }
            else if (OutsideRootElement && ch != '\r')
            {
                XmlExceptionHelper.ThrowInvalidRootData(this);
            }
            else if ((CharTypeMap[ch] & CharType.Text) != 0)
            {
                await ReadText<TMode>(false, cancellationToken).ConfigureAwait(false);
            }
            else if (ch == '&')
            {
                await ReadEscapedText<TMode>(cancellationToken).ConfigureAwait(false);
            }
            else if (ch == '\r')
            {
                BufferReader.SkipByte();

                if (!await BufferReader.EndOfFileCore<TMode>(cancellationToken).ConfigureAwait(false) &&
                    await BufferReader.GetByte<TMode>(cancellationToken).ConfigureAwait(false) == '\n')
                    await ReadWhitespace<TMode>(cancellationToken).ConfigureAwait(false);
                else
                    MoveToComplexText().Value.SetCharValue('\n');
            }
            else if (ch == ']')
            {
                int offset;
                await BufferReader.EnsureBytes<TMode>(3, cancellationToken).ConfigureAwait(false);
                byte[] buffer = BufferReader.GetBuffer(out offset, out _);
                if (buffer[offset + 0] == (byte)']' &&
                    buffer[offset + 1] == (byte)']' &&
                    buffer[offset + 2] == (byte)'>')
                {
                    XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlCloseCData));
                }

                BufferReader.SkipByte();
                MoveToComplexText().Value.SetCharValue(']');  // Need to get past the ']' and keep going.
            }
            else if (ch == 0xEF)  // Watch for invalid characters 0xfffe and 0xffff
            {
                await ReadText<TMode>(true, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                XmlExceptionHelper.ThrowInvalidXml(this, ch);
            }
            return true;
        }

        protected override XmlSigningNodeWriter CreateSigningNodeWriter()
        {
            return new XmlSigningNodeWriter(true);
        }

        public bool HasLineInfo()
        {
            return true;
        }

        public int LineNumber
        {
            get
            {
                int row;
                GetPosition(out row, out _);
                return row;
            }
        }

        public int LinePosition
        {
            get
            {
                int column;
                GetPosition(out _, out column);
                return column;
            }
        }

        private void GetPosition(out int row, out int column)
        {
            _rowOffsets ??= BufferReader.GetRows();

            int offset = BufferReader.Offset;

            int j = 0;
            while (j < _rowOffsets.Length - 1 && _rowOffsets[j + 1] < offset)
                j++;

            row = j + 1;
            column = offset - _rowOffsets[j] + 1;
        }

        private static class CharType
        {
            public const byte None = 0x00;
            public const byte FirstName = 0x01;
            public const byte Name = 0x02;
            public const byte Whitespace = 0x04;
            public const byte Text = 0x08;
            public const byte AttributeText = 0x10;
            public const byte SpecialWhitespace = 0x20;
            public const byte Comment = 0x40;
        }
    }
}
