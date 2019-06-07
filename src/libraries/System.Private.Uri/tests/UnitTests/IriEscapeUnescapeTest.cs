// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Common.Tests;

using Xunit;

namespace System.Net.Test.Uri.IriTest
{
    /// <summary>
    /// IriEscapeUnescape heap corruption and crash test.
    /// These tests do not check for output correctness although they do validate that normalization is 
    /// locale-independent.
    /// </summary>
    public class IriEscapeUnescapeTest
    {
        [Fact]
        public void Iri_EscapeUnescapeIri_FragmentInvalidCharacters()
        {
            string input = "%F4%80%80%BA";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_EscapedAscii()
        {
            string input = "%%%01%35%36";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_EscapedAsciiFollowedByUnescaped()
        {
            string input = "%ABabc";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_InvalidHexSequence()
        {
            string input = "%AB%FG%GF";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_InvalidUTF8Sequence()
        {
            string input = "%F4%80%80%7F";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_IncompleteEscapedCharacter()
        {
            string input = "%F4%80%80%B";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_ReservedCharacters()
        {
            string input = "/?#??#%[]";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_EscapedReservedCharacters()
        {
            string input = "%2F%3F%23%3F%3F%23%25%5B%5D";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_BidiCharacters()
        {
            string input = "\u200E";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_IncompleteSurrogate()
        {
            string input = "\uDBC0\uDC3A\uDBC0";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_InIriRange_AfterEscapingBufferInitialized()
        {
            string input = "\uDBC0\uDC3A\u00A1";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_BidiCharacter_AfterEscapingBufferInitialized()
        {
            string input = "\uDBC0\uDC3A\u200E";
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_UnicodePlane0()
        {
            EscapeUnescapeTestUnicodePlane(0x0, 0xFFFF, 0x100);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_UnicodePlane1()
        {
            EscapeUnescapeTestUnicodePlane(0x10000, 0x1FFFF, 0x100);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_UnicodePlane2()
        {
            EscapeUnescapeTestUnicodePlane(0x20000, 0x2FFFF, 0x100);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_UnicodePlane3_13()
        {
            EscapeUnescapeTestUnicodePlane(0x30000, 0xDFFFF, 0x500);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_UnicodePlane14()
        {
            EscapeUnescapeTestUnicodePlane(0xE0000, 0xEFFFF, 0x100);
        }

        [Fact]
        public void Iri_EscapeUnescapeIri_UnicodePlane15_16()
        {
            EscapeUnescapeTestUnicodePlane(0xF0000, 0x10FFFF, 0x100);
        }

        private void EscapeUnescapeTestUnicodePlane(int start, int end, int step)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = start; i < end; i += step)
            {
                if (i < 0xFFFF)
                {
                    // ConvertFromUtf32 doesn't allow surrogate codepoint values.
                    // This may generate invalid Unicode sequences when i is between 0xd800 and 0xdfff.
                    sb.Append((char)i);
                }
                else
                {
                    sb.Append(char.ConvertFromUtf32(i));
                }
            }

            string input = sb.ToString();
            EscapeUnescapeAllUriComponentsInDifferentCultures(input);
        }

        private void EscapeUnescapeAllUriComponentsInDifferentCultures(string uriInput)
        {
            UriComponents[] components = new UriComponents[]
            {
                UriComponents.AbsoluteUri,
                UriComponents.Fragment,
                UriComponents.Host,
                UriComponents.HostAndPort,
                UriComponents.HttpRequestUrl,
                UriComponents.KeepDelimiter,
                UriComponents.NormalizedHost,
                UriComponents.Path,
                UriComponents.PathAndQuery,
                UriComponents.Port,
                UriComponents.Query,
                UriComponents.Scheme,
                UriComponents.SchemeAndServer,
                UriComponents.SerializationInfoString,
                UriComponents.StrongAuthority,
                UriComponents.StrongPort,
                UriComponents.UserInfo,
            };

            string[] results_en = new string[components.Length];
            string[] results_zh = new string[components.Length];


            using (ThreadCultureChange helper = new ThreadCultureChange())
            {
                for (int i = 0; i < components.Length; i++)
                {
                    results_en[i] = EscapeUnescapeTestComponent(uriInput, components[i]);
                }

                helper.ChangeCultureInfo("zh-cn");

                for (int i = 0; i < components.Length; i++)
                {
                    results_zh[i] = EscapeUnescapeTestComponent(uriInput, components[i]);
                }

                for (int i = 0; i < components.Length; i++)
                {
                    Assert.True(
                        0 == string.CompareOrdinal(results_en[i], results_zh[i]),
                        "Detected locale differences when processing UriComponents." + components[i]);
                }
            }
        }

        private string EscapeUnescapeTestComponent(string uriInput, UriComponents component)
        {
            string? ret = null;
            HeapCheck hc = new HeapCheck(uriInput);

            unsafe
            {
                fixed (char* pInput = hc.HeapBlock)
                {
                    ret = IriHelper.EscapeUnescapeIri(pInput + hc.Offset, 0, uriInput.Length, component);
                }
            }

            // check for buffer under and overruns
            hc.ValidatePadding();

            return ret;
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_valid_utf_followed_by_invalid_utf_5byte()
        {
            byte[] bytes = new byte[] { 0xF4, 0x80, 0x80, 0xBA, 0xFD, 0x80, 0x80, 0xBA, 0xCD };
            MatchUTF8SequenceTest(bytes, bytes.Length);
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_invalid_utf_followed_by_valid_ucschar()
        {
            byte[] bytes = new byte[] { 0xCA, 0xE4, 0x88, 0xB2, 0, 0, 0, 0, 0, 0, 0, 0 };
            MatchUTF8SequenceTest(bytes, 4);
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_iprivate_4byte()
        {
            // Area-B, Supplemental Private use 0x100000 - 0x10FFFF
            byte[] bytes = new byte[] { 0xF4, 0x80, 0x80, 0xBA, 0, 0, 0, 0, 0, 0, 0, 0 };
            MatchUTF8SequenceTest(bytes, 4);
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_two_iprivate_8byte()
        {
            // Area-B, Supplemental Private use 0x100000 - 0x10FFFF
            byte[] bytes = new byte[] { 0xF0, 0xA0, 0x80, 0x80, 0xF0, 0xA0, 0x80, 0x81, 0, 0, 0, 0 };
            MatchUTF8SequenceTest(bytes, 8);
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_bidichar_3byte()
        {
            byte[] bytes = new byte[] { 0xE2, 0x80, 0x8E, 0, 0, 0, 0, 0, 0 };
            MatchUTF8SequenceTest(bytes, 3);
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_ucschar_3byte()
        {
            // Char Block: 3400..4DBF-CJK Unified Ideographs Extension A
            byte[] bytes = new byte[] { 0xE4, 0x88, 0xB2, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            MatchUTF8SequenceTest(bytes, 3);
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_bidi_followed_by_valid_highsurr_6byte()
        {
            // BIDI: \u200E (E2 80 8E); SURR: \uD801 \uDC02 (F0 90 90 82)
            byte[] bytes = new byte[] { 0xE2, 0x80, 0x8E, 0xE3, 0x82, 0xAF, 0xE3, 0x82, 0xAF, 0xE3, 0x82, 0xAF };
            MatchUTF8SequenceTest(bytes, bytes.Length);
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_bidi_followed_by_invalid_highsurr_6byte()
        {
            // BIDI: \u200E (E2 80 8E); SURR: \uD801 \uDC02 (F0 90 90 82)
            byte[] bytes = new byte[] { 0xE2, 0x80, 0x8E, 0xF0, 0x90, 0x90, 0, 0, 0, 0, 0, 0 };
            MatchUTF8SequenceTest(bytes, 6);
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_decoder_different_from_encoder()
        {
            // Found by fuzzing: 
            // Input string:                %98%C8%D4%F3%D4%A8%7A%CF%DE%41%16
            // Valid Unicode sequences:           %D4      %A8%7A      %41%16

            byte[] bytes = new byte[] { 0x98, 0xC8, 0xD4, 0xF3, 0xD4, 0xA8, 0x7A, 0xCF, 0xDE, 0x41, 0x16 };
            MatchUTF8SequenceTest(bytes, bytes.Length);
        }

        [Fact]
        public void Iri_MatchUTF8Sequence_invalid_ucschars_invalid()
        {
            // %C6%F3%BC%A1%B8%B5
            byte[] bytes = new byte[] { 0xC6, 0xF3, 0xBC, 0xA1, 0xB8, 0xB5 };
            MatchUTF8SequenceTest(bytes, bytes.Length);
        }

        private void MatchUTF8SequenceTest(byte[] inbytes, int numBytes)
        {
            using (ThreadCultureChange helper = new ThreadCultureChange())
            {
                MatchUTF8SequenceOverrunTest(inbytes, numBytes, true, false);
                MatchUTF8SequenceOverrunTest(inbytes, numBytes, true, true);
                MatchUTF8SequenceOverrunTest(inbytes, numBytes, false, false);
                MatchUTF8SequenceOverrunTest(inbytes, numBytes, false, true);

                helper.ChangeCultureInfo("zh-cn");

                MatchUTF8SequenceOverrunTest(inbytes, numBytes, true, false);
                MatchUTF8SequenceOverrunTest(inbytes, numBytes, true, true);
                MatchUTF8SequenceOverrunTest(inbytes, numBytes, false, false);
                MatchUTF8SequenceOverrunTest(inbytes, numBytes, false, true);
            }
        }

        private void MatchUTF8SequenceOverrunTest(byte[] inbytes, int numBytes, bool isQuery, bool iriParsing)
        {
            Encoding noFallbackCharUTF8 = Encoding.GetEncoding(
                Encoding.UTF8.CodePage,
                new EncoderReplacementFallback(""),
                new DecoderReplacementFallback(""));

            char[] chars = noFallbackCharUTF8.GetChars(inbytes, 0, numBytes);

            Assert.False(
                chars.Length == 0,
                "Invalid test: MatchUTF8Sequence cannot be called when no Unicode characters can be decoded.");

            // dest is guaranteed to have space for the escaped version of all characters (in the form of %HH).
            char[] dest = new char[inbytes.Length * 3];
            char[] unescapedChars = new char[inbytes.Length];
            chars.CopyTo(unescapedChars, 0);

            HeapCheck hc = new HeapCheck(dest);

            unsafe
            {
                fixed (char* pInput = hc.HeapBlock)
                {
                    int offset = hc.Offset;
                    UriHelper.MatchUTF8Sequence(
                        pInput,
                        hc.HeapBlock,
                        ref offset,
                        unescapedChars,
                        chars.Length,
                        inbytes,
                        numBytes,
                        isQuery,
                        iriParsing);
                }
            }

            // Check for buffer under and overruns.
            hc.ValidatePadding();
        }

        private class HeapCheck
        {
            private const char paddingValue = (char)0xDEAD;
            private const int padding = 32;
            private int _len;
            private char[] _memblock;

            private HeapCheck(int length)
            {
                _len = length;

                _memblock = new char[_len + padding * 2];
                for (int i = 0; i < _memblock.Length; i++)
                {
                    _memblock[i] = paddingValue;
                }
            }

            public HeapCheck(string input) : this(input.Length)
            {
                input.CopyTo(0, _memblock, padding, _len);
            }

            public HeapCheck(char[] input) : this(input.Length)
            {
                input.CopyTo(_memblock, padding);
            }

            public char[] HeapBlock
            {
                get { return _memblock; }
            }

            public int Offset
            {
                get { return padding; }
            }

            public void ValidatePadding()
            {
                for (int i = 0; i < _memblock.Length; i++)
                {
                    if ((i < padding) || (i >= padding + _len))
                    {
                        Assert.True(
                            (int)paddingValue == (int)_memblock[i],
                            "Heap corruption detected: unexpected padding value at index: " + i +
                            " Data allocated at idx: " + padding + " - " + (_len + padding - 1));
                    }
                }
            }
        }
    }
}
