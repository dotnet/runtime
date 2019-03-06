// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization; // For SR
using System.Security;
using System.Text;
using System.Globalization;


namespace System.Xml
{
    public interface IXmlTextReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose);
        void SetInput(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose);
    }

    internal class XmlUTF8TextReader : XmlBaseReader, IXmlLineInfo, IXmlTextReaderInitializer
    {
        private const int MaxTextChunk = 2048;

        private PrefixHandle _prefix;
        private StringHandle _localName;
        private int[] _rowOffsets;
        private OnXmlDictionaryReaderClose _onClose;
        private bool _buffered;
        private int _maxBytesPerRead;
        private static byte[] s_charType = new byte[256]
            {
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
                         CharType.None|CharType.Comment|CharType.Comment|CharType.Whitespace|CharType.Text|CharType.SpecialWhitespace,
                /*  A (.) */
                         CharType.None|CharType.Comment|CharType.Comment|CharType.Whitespace|CharType.Text|CharType.SpecialWhitespace,
                /*  B (.) */
                         CharType.None,
                /*  C (.) */
                         CharType.None,
                /*  D (.) */
                         CharType.None|CharType.Comment|CharType.Comment|CharType.Whitespace,
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
            };

        public XmlUTF8TextReader()
        {
            _prefix = new PrefixHandle(BufferReader);
            _localName = new StringHandle(BufferReader);
        }

        public void SetInput(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            if (buffer == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(buffer)));
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));
            if (offset > buffer.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, buffer.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > buffer.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, buffer.Length - offset)));
            MoveToInitial(quotas, onClose);
            ArraySegment<byte> seg = EncodingStreamWrapper.ProcessBuffer(buffer, offset, count, encoding);
            BufferReader.SetBuffer(seg.Array, seg.Offset, seg.Count, null, null);
            _buffered = true;
        }

        public void SetInput(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            if (stream == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(stream));
            MoveToInitial(quotas, onClose);
            stream = new EncodingStreamWrapper(stream, encoding);
            BufferReader.SetBuffer(stream, null, null);
            _buffered = false;
        }

        private void MoveToInitial(XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            MoveToInitial(quotas);
            _maxBytesPerRead = quotas.MaxBytesPerRead;
            _onClose = onClose;
        }

        public override void Close()
        {
            _rowOffsets = null;
            base.Close();
            OnXmlDictionaryReaderClose onClose = _onClose;
            _onClose = null;
            if (onClose != null)
            {
                try
                {
                    onClose(this);
                }
                catch (Exception e)
                {
                    if (DiagnosticUtility.IsFatal(e)) throw;

                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperCallback(e);
                }
            }
        }

        private void SkipWhitespace()
        {
            while (!BufferReader.EndOfFile && (s_charType[BufferReader.GetByte()] & CharType.Whitespace) != 0)
                BufferReader.SkipByte();
        }

        private void ReadDeclaration()
        {
            if (!_buffered)
                BufferElement();
            int offset;
            byte[] buffer = BufferReader.GetBuffer(5, out offset);
            if (buffer[offset + 0] != (byte)'?' ||
                buffer[offset + 1] != (byte)'x' ||
                buffer[offset + 2] != (byte)'m' ||
                buffer[offset + 3] != (byte)'l' ||
                (s_charType[buffer[offset + 4]] & CharType.Whitespace) == 0)
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
            SkipWhitespace();
            ReadAttributes();
            int valueLength = BufferReader.Offset - valueOffset;

            // Backoff the spaces
            while (valueLength > 0)
            {
                byte ch = BufferReader.GetByte(valueOffset + valueLength - 1);
                if ((s_charType[ch] & CharType.Whitespace) == 0)
                    break;
                valueLength--;
            }

            buffer = BufferReader.GetBuffer(2, out offset);
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

            int ch = 0;
            int anyChar = 0;
            int prefixChar = 0;
            int prefixOffset = offset;
            if (offset < offsetMax)
            {
                ch = buffer[offset];
                prefixChar = ch;
                if ((s_charType[ch] & CharType.FirstName) == 0)
                    anyChar |= 0x80;
                anyChar |= ch;
                offset++;
                while (offset < offsetMax)
                {
                    ch = buffer[offset];
                    if ((s_charType[ch] & CharType.Name) == 0)
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
                    if ((s_charType[ch] & CharType.FirstName) == 0)
                        anyChar |= 0x80;
                    anyChar |= ch;
                    offset++;
                    while (offset < offsetMax)
                    {
                        ch = buffer[offset];
                        if ((s_charType[ch] & CharType.Name) == 0)
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

        private int ReadAttributeText(byte[] buffer, int offset, int offsetMax)
        {
            byte[] charType = XmlUTF8TextReader.s_charType;
            int textOffset = offset;
            while (offset < offsetMax && (charType[buffer[offset]] & CharType.AttributeText) != 0)
                offset++;
            return offset - textOffset;
        }

        private void ReadAttributes()
        {
            int startOffset = 0;
            if (_buffered)
                startOffset = BufferReader.Offset;

            while (true)
            {
                byte ch;
                ReadQualifiedName(_prefix, _localName);
                if (BufferReader.GetByte() != '=')
                {
                    SkipWhitespace();
                    if (BufferReader.GetByte() != '=')
                        XmlExceptionHelper.ThrowTokenExpected(this, "=", (char)BufferReader.GetByte());
                }
                BufferReader.SkipByte();
                byte quoteChar = BufferReader.GetByte();
                if (quoteChar != '"' && quoteChar != '\'')
                {
                    SkipWhitespace();
                    quoteChar = BufferReader.GetByte();
                    if (quoteChar != '"' && quoteChar != '\'')
                        XmlExceptionHelper.ThrowTokenExpected(this, "\"", (char)BufferReader.GetByte());
                }
                BufferReader.SkipByte();
                bool escaped = false;
                int valueOffset = BufferReader.Offset;
                while (true)
                {
                    int offset, offsetMax;
                    byte[] buffer = BufferReader.GetBuffer(out offset, out offsetMax);
                    int length = ReadAttributeText(buffer, offset, offsetMax);
                    BufferReader.Advance(length);
                    ch = BufferReader.GetByte();
                    if (ch == quoteChar)
                        break;
                    if (ch == '&')
                    {
                        ReadCharRef();
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
                        ReadNonFFFE();
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

                ch = BufferReader.GetByte();

                bool space = false;
                while ((s_charType[ch] & CharType.Whitespace) != 0)
                {
                    space = true;
                    BufferReader.SkipByte();
                    ch = BufferReader.GetByte();
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
        private bool IsNextCharacterNonFFFE(byte[] buffer, int offset)
        {
            Fx.Assert(buffer[offset] == 0xEF, "buffer[offset] MUST be 0xEF.");

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

        private void ReadNonFFFE()
        {
            int off;
            byte[] buff = BufferReader.GetBuffer(3, out off);
            if (buff[off + 1] == 0xBF && (buff[off + 2] == 0xBE || buff[off + 2] == 0xBF))
            {
                XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlInvalidFFFE));
            }
            BufferReader.Advance(3);
        }

        private void BufferElement()
        {
            int elementOffset = BufferReader.Offset;
            const int byteCount = 128;
            bool done = false;
            byte quoteChar = 0;
            while (!done)
            {
                int offset;
                int offsetMax;
                byte[] buffer = BufferReader.GetBuffer(byteCount, out offset, out offsetMax);
                if (offset + byteCount != offsetMax)
                    break;
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
                BufferReader.Advance(byteCount);
            }
            BufferReader.Offset = elementOffset;
        }

        private new void ReadStartElement()
        {
            if (!_buffered)
                BufferElement();
            XmlElementNode elementNode = EnterScope();
            elementNode.NameOffset = BufferReader.Offset;
            ReadQualifiedName(elementNode.Prefix, elementNode.LocalName);
            elementNode.NameLength = BufferReader.Offset - elementNode.NameOffset;
            byte ch = BufferReader.GetByte();
            while ((s_charType[ch] & CharType.Whitespace) != 0)
            {
                BufferReader.SkipByte();
                ch = BufferReader.GetByte();
            }
            if (ch != '>' && ch != '/')
            {
                ReadAttributes();
                ch = BufferReader.GetByte();
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
            if (BufferReader.GetByte() != '>')
                XmlExceptionHelper.ThrowTokenExpected(this, ">", (char)BufferReader.GetByte());
            BufferReader.SkipByte();
            elementNode.BufferOffset = BufferReader.Offset;
        }

        private new void ReadEndElement()
        {
            BufferReader.SkipByte();
            XmlElementNode elementNode = this.ElementNode;
            int nameOffset = elementNode.NameOffset;
            int nameLength = elementNode.NameLength;
            int offset;
            byte[] buffer = BufferReader.GetBuffer(nameLength, out offset);
            for (int i = 0; i < nameLength; i++)
            {
                if (buffer[offset + i] != buffer[nameOffset + i])
                {
                    ReadQualifiedName(_prefix, _localName);
                    XmlExceptionHelper.ThrowTagMismatch(this, elementNode.Prefix.GetString(), elementNode.LocalName.GetString(), _prefix.GetString(), _localName.GetString());
                }
            }
            BufferReader.Advance(nameLength);
            if (BufferReader.GetByte() != '>')
            {
                SkipWhitespace();
                if (BufferReader.GetByte() != '>')
                    XmlExceptionHelper.ThrowTokenExpected(this, ">", (char)BufferReader.GetByte());
            }
            BufferReader.SkipByte();
            MoveToEndElement();
        }

        private void ReadComment()
        {
            BufferReader.SkipByte();
            if (BufferReader.GetByte() != '-')
                XmlExceptionHelper.ThrowTokenExpected(this, "--", (char)BufferReader.GetByte());
            BufferReader.SkipByte();
            int commentOffset = BufferReader.Offset;
            while (true)
            {
                while (true)
                {
                    byte b = BufferReader.GetByte();
                    if (b == '-')
                        break;
                    if ((s_charType[b] & CharType.Comment) == 0)
                    {
                        if (b == 0xEF)
                            ReadNonFFFE();
                        else
                            XmlExceptionHelper.ThrowInvalidXml(this, b);
                    }
                    else
                    {
                        BufferReader.SkipByte();
                    }
                }

                int offset;
                byte[] buffer = BufferReader.GetBuffer(3, out offset);
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

        private void ReadCData()
        {
            int offset;
            byte[] buffer = BufferReader.GetBuffer(7, out offset);
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
                    b = BufferReader.GetByte();
                    if (b == ']')
                        break;

                    if (b == 0xEF)
                        ReadNonFFFE();
                    else
                        BufferReader.SkipByte();
                }
                buffer = BufferReader.GetBuffer(3, out offset);
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

        private int ReadCharRef()
        {
            DiagnosticUtility.DebugAssert(BufferReader.GetByte() == '&', "");
            int charEntityOffset = BufferReader.Offset;
            BufferReader.SkipByte();
            while (BufferReader.GetByte() != ';')
                BufferReader.SkipByte();
            BufferReader.SkipByte();
            int charEntityLength = BufferReader.Offset - charEntityOffset;
            BufferReader.Offset = charEntityOffset;
            int ch = BufferReader.GetCharEntity(charEntityOffset, charEntityLength);
            BufferReader.Advance(charEntityLength);
            return ch;
        }


        private void ReadWhitespace()
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
                buffer = BufferReader.GetBuffer(MaxTextChunk, out offset, out offsetMax);
                length = ReadWhitespace(buffer, offset, offsetMax);
                length = BreakText(buffer, offset, length);
            }
            BufferReader.Advance(length);

            MoveToWhitespaceText().Value.SetValue(ValueHandleType.UTF8, offset, length);
        }

        private int ReadWhitespace(byte[] buffer, int offset, int offsetMax)
        {
            byte[] charType = XmlUTF8TextReader.s_charType;
            int wsOffset = offset;
            while (offset < offsetMax && (charType[buffer[offset]] & CharType.SpecialWhitespace) != 0)
                offset++;
            return offset - wsOffset;
        }

        private int ReadText(byte[] buffer, int offset, int offsetMax)
        {
            byte[] charType = XmlUTF8TextReader.s_charType;
            int textOffset = offset;
            while (offset < offsetMax && (charType[buffer[offset]] & CharType.Text) != 0)
                offset++;
            return offset - textOffset;
        }

        // Read Unicode codepoints 0xFvvv
        private int ReadTextAndWatchForInvalidCharacters(byte[] buffer, int offset, int offsetMax)
        {
            byte[] charType = XmlUTF8TextReader.s_charType;
            int textOffset = offset;

            while (offset < offsetMax && ((charType[buffer[offset]] & CharType.Text) != 0 || buffer[offset] == 0xEF))
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
                            // Get enough bytes for us to process next character, then go back to top of while loop
                            int dummy;
                            BufferReader.GetBuffer(3, out dummy);
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

        private int BreakText(byte[] buffer, int offset, int length)
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

        private void ReadText(bool hasLeadingByteOf0xEF)
        {
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
                    length = ReadText(buffer, offset, offsetMax);
                }
            }
            else
            {
                buffer = BufferReader.GetBuffer(MaxTextChunk, out offset, out offsetMax);
                if (hasLeadingByteOf0xEF)
                {
                    length = ReadTextAndWatchForInvalidCharacters(buffer, offset, offsetMax);
                }
                else
                {
                    length = ReadText(buffer, offset, offsetMax);
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

        private void ReadEscapedText()
        {
            int ch = ReadCharRef();
            if (ch < 256 && (s_charType[ch] & CharType.Whitespace) != 0)
                MoveToWhitespaceText().Value.SetCharValue(ch);
            else
                MoveToComplexText().Value.SetCharValue(ch);
        }

        public override bool Read()
        {
            if (this.Node.ReadState == ReadState.Closed)
                return false;

            if (this.Node.CanMoveToElement)
            {
                // If we're positioned on an attribute or attribute text on an empty element, we need to move back
                // to the element in order to get the correct setting of ExitScope
                MoveToElement();
            }

            SignNode();
            if (this.Node.ExitScope)
            {
                ExitScope();
            }
            if (!_buffered)
                BufferReader.SetWindow(ElementNode.BufferOffset, _maxBytesPerRead);

            if (BufferReader.EndOfFile)
            {
                MoveToEndOfFile();
                return false;
            }
            byte ch = BufferReader.GetByte();
            if (ch == (byte)'<')
            {
                BufferReader.SkipByte();
                ch = BufferReader.GetByte();
                if (ch == (byte)'/')
                    ReadEndElement();
                else if (ch == (byte)'!')
                {
                    BufferReader.SkipByte();
                    ch = BufferReader.GetByte();
                    if (ch == '-')
                    {
                        ReadComment();
                    }
                    else
                    {
                        if (OutsideRootElement)
                            XmlExceptionHelper.ThrowXmlException(this, new XmlException(SR.XmlCDATAInvalidAtTopLevel));

                        ReadCData();
                    }
                }
                else if (ch == (byte)'?')
                    ReadDeclaration();
                else
                    ReadStartElement();
            }
            else if ((s_charType[ch] & CharType.SpecialWhitespace) != 0)
            {
                ReadWhitespace();
            }
            else if (OutsideRootElement && ch != '\r')
            {
                XmlExceptionHelper.ThrowInvalidRootData(this);
            }
            else if ((s_charType[ch] & CharType.Text) != 0)
            {
                ReadText(false);
            }
            else if (ch == '&')
            {
                ReadEscapedText();
            }
            else if (ch == '\r')
            {
                BufferReader.SkipByte();

                if (!BufferReader.EndOfFile && BufferReader.GetByte() == '\n')
                    ReadWhitespace();
                else
                    MoveToComplexText().Value.SetCharValue('\n');
            }
            else if (ch == ']')
            {
                int offset;
                byte[] buffer = BufferReader.GetBuffer(3, out offset);
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
                ReadText(true);
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
                int row, column;
                GetPosition(out row, out column);
                return row;
            }
        }

        public int LinePosition
        {
            get
            {
                int row, column;
                GetPosition(out row, out column);
                return column;
            }
        }

        private void GetPosition(out int row, out int column)
        {
            if (_rowOffsets == null)
            {
                _rowOffsets = BufferReader.GetRows();
            }

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
