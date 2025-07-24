// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics
{
    internal sealed class W3CPropagator : DistributedContextPropagator
    {
        internal static DistributedContextPropagator Instance { get; } = new W3CPropagator();

        private const int MaxBaggageEntriesToEmit = 64;     // Suggested by W3C specs
        private const int MaxBaggageEncodedLength = 8192;   // Suggested by W3C specs
        private const int MaxTraceStateEncodedLength = 256; // Suggested by W3C specs

        private const char Equal = '=';
        private const char Percent = '%';
        private const char Replacement = '\uFFFD'; // �

        public override IReadOnlyCollection<string> Fields { get; } = new ReadOnlyCollection<string>(new[] { TraceParent, TraceState, Baggage, CorrelationContext });

        public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
        {
            if (activity is null || setter is null || activity.IdFormat != ActivityIdFormat.W3C)
            {
                return;
            }

            string? id = activity.Id;
            if (id is null)
            {
                return;
            }

            setter(carrier, TraceParent, id);
            if (activity.TraceStateString is { Length: > 0 } traceState)
            {
                InjectTraceState(traceState, carrier, setter);
            }

            InjectW3CBaggage(carrier, activity.Baggage, setter);
        }

        public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
        {
            if (getter is null)
            {
                traceId = null;
                traceState = null;
                return;
            }

            getter(carrier, TraceParent, out traceId, out _);
            if (IsInvalidTraceParent(traceId))
            {
                traceId = null;
            }

            getter(carrier, TraceState, out string? traceStateValue, out _);
            traceState = ValidateTraceState(traceStateValue);
        }

        public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter)
        {
            if (getter is null)
            {
                return null;
            }

            getter(carrier, Baggage, out string? theBaggage, out _);
            if (theBaggage is null)
            {
                getter(carrier, CorrelationContext, out theBaggage, out _);
            }

            TryExtractBaggage(theBaggage, out IEnumerable<KeyValuePair<string, string?>>? baggage);

            return baggage;
        }

        internal static bool TryExtractBaggage(string? baggageString, out IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            baggage = null;
            List<KeyValuePair<string, string?>>? baggageList = null;

            if (string.IsNullOrEmpty(baggageString))
            {
                return true;
            }

            ReadOnlySpan<char> baggageSpan = baggageString;

            do
            {
                int entrySeparator = baggageSpan.IndexOf(Comma);
                ReadOnlySpan<char> currentEntry = entrySeparator >= 0 ? baggageSpan.Slice(0, entrySeparator) : baggageSpan;

                int keyValueSeparator = currentEntry.IndexOf(Equal);
                if (keyValueSeparator <= 0 || keyValueSeparator >= currentEntry.Length - 1)
                {
                    break; // invalid format
                }

                ReadOnlySpan<char> keySpan = currentEntry.Slice(0, keyValueSeparator);
                ReadOnlySpan<char> valueSpan = currentEntry.Slice(keyValueSeparator + 1);

                if (TryDecodeBaggageKey(keySpan, out string? key) && TryDecodeBaggageValue(valueSpan, out string value))
                {
                    baggageList ??= new List<KeyValuePair<string, string?>>();
                    baggageList.Add(new KeyValuePair<string, string?>(key, value));
                }

                baggageSpan = entrySeparator >= 0 ? baggageSpan.Slice(entrySeparator + 1) : ReadOnlySpan<char>.Empty;
            } while (baggageSpan.Length > 0);

            // reverse order for asp.net compatibility.
            baggageList?.Reverse();

            baggage = baggageList;
            return baggageList != null;
        }

        // list  = list-member 0*31( OWS "," OWS list-member )
        // list-member = (key "=" value) / OWS
        //
        // key = ( lcalpha / DIGIT ) 0*255 ( keychar )
        // keychar    = lcalpha / DIGIT / "_" / "-"/ "*" / "/" / "@"
        // lcalpha    = %x61-7A ; a-z
        //
        // value    = 0*255(chr) nblk-chr
        // nblk-chr = %x21-2B / %x2D-3C / %x3E-7E
        // chr      = %x20 / nblk-chr

        internal static string? ValidateTraceState(string? traceState)
        {
            if (string.IsNullOrEmpty(traceState))
            {
                return null; // invalid format
            }

            int processed = 0;

            while (processed < traceState.Length)
            {
                ReadOnlySpan<char> traceStateSpan = traceState.AsSpan(processed);
                int commaIndex = traceStateSpan.IndexOf(Comma);
                ReadOnlySpan<char> entry = commaIndex >= 0 ? traceStateSpan.Slice(0, commaIndex) : traceStateSpan;
                int delta = entry.Length + (commaIndex >= 0 ? 1 : 0); // +1 for the comma

                if (processed + delta > MaxTraceStateEncodedLength)
                {
                    break; // entry exceeds max length
                }

                int equalIndex = entry.IndexOf(Equal);
                if (equalIndex <= 0 || equalIndex >= entry.Length - 1)
                {
                    break; // invalid format
                }

                if (IsInvalidTraceStateKey(Trim(entry.Slice(0, equalIndex))) || IsInvalidTraceStateValue(TrimSpaceOnly(entry.Slice(equalIndex + 1))))
                {
                    break; // entry exceeds max length or invalid key/value, skip the whole trace state entries.
                }

                processed += delta;
            }

            if (processed > 0)
            {
                if (traceState[processed - 1] == Comma)
                {
                    processed--; // remove the last comma
                }

                if (processed > 0)
                {
                    return processed >= traceState.Length ? traceState : traceState.AsSpan(0, processed).ToString();
                }
            }

            return null;
        }

        internal static void InjectTraceState(string traceState, object? carrier, PropagatorSetterCallback setter)
        {
            Debug.Assert(traceState != null, "traceState cannot be null");
            Debug.Assert(setter != null, "setter cannot be null");

            string? traceStateValue = ValidateTraceState(traceState);
            if (traceStateValue is not null)
            {
                setter(carrier, TraceState, traceStateValue);
            }
        }

        internal static void InjectW3CBaggage(object? carrier, IEnumerable<KeyValuePair<string, string?>> baggage, PropagatorSetterCallback setter)
        {
            using (IEnumerator<KeyValuePair<string, string?>> e = baggage.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    ValueStringBuilder encodedBaggage = new ValueStringBuilder(stackalloc char[256]);

                    int entriesCount = 0;
                    int lastGoodLength = 0;

                    do
                    {
                        KeyValuePair<string, string?> item = e.Current;

                        if (EncodeBaggageKey(item.Key, ref encodedBaggage))
                        {
                            encodedBaggage.Append(Space);
                            encodedBaggage.Append(Equal);
                            encodedBaggage.Append(Space);
                            if (!string.IsNullOrEmpty(item.Value))
                            {
                                EncodeBaggageValue(item.Value, ref encodedBaggage);
                            }
                            encodedBaggage.Append(CommaWithSpace);

                            entriesCount++;

                            if (encodedBaggage.Length < MaxBaggageEncodedLength)
                            {
                                lastGoodLength = encodedBaggage.Length;
                            }
                        }
                    } while (e.MoveNext() && entriesCount < MaxBaggageEntriesToEmit && encodedBaggage.Length < MaxBaggageEncodedLength);

                    if (lastGoodLength - 2 > 0)
                    {
                        setter(carrier, Baggage, encodedBaggage.AsSpan(0, lastGoodLength - 2).ToString());
                    }

                    encodedBaggage.Dispose();
                }
            }
        }

        private static bool TryDecodeBaggageKey(ReadOnlySpan<char> keySpan, out string key)
        {
            key = null!;
            keySpan = Trim(keySpan);

            if (keySpan.IsEmpty || IsInvalidBaggageKey(keySpan))
            {
                return false;
            }

            key = keySpan.ToString();
            return true;
        }

        private static bool TryDecodeBaggageValue(ReadOnlySpan<char> valueSpan, out string value)
        {
            value = null!;
            valueSpan = Trim(valueSpan);

            using ValueStringBuilder vsb = new ValueStringBuilder(stackalloc char[128]);

            for (int i = 0; i < valueSpan.Length; i++)
            {
                char c = valueSpan[i];
                if (c > 0x7F)
                {
                    return false; // we expect only ascii characters
                }

                if (c != Percent) // none escaped
                {
                    vsb.Append(c);
                    continue;
                }

                if (!TryDecodeEscapedByte(valueSpan.Slice(i), out byte b0))
                {
                    return false;
                }

                if (b0 <= 0x7F)
                {
                    vsb.Append((char)b0);
                    i += 2;
                    continue;
                }

                // 2-byte sequence: 110xxxxx 10xxxxxx
                if ((uint)(b0 - 0xC2) <=  (0xDF - 0xC2))
                {
                    if (i + 5 >= valueSpan.Length || valueSpan[i + 3] != Percent || !TryDecodeEscapedByte(valueSpan.Slice(i + 3), out byte b1) || (b1 & 0xC0) != 0x80)
                    {
                        // Malformed utf-8 sequence. emit U+FFFD and continue
                        vsb.Append(Replacement);
                        i += 2;
                        continue;
                    }

                    vsb.Append((char)(((b0 & 0x1F) << 6) | (b1 & 0x3F)));
                    i += 5;
                    continue;
                }

                // 3-byte sequence: 1110xxxx 10xxxxxx 10xxxxxx
                if ((uint)(b0 - 0xE0) <= (0xEF - 0xE0))
                {
                    if (i + 8 >= valueSpan.Length ||
                        valueSpan[i + 3] != Percent ||
                        valueSpan[i + 6] != Percent ||
                        !TryDecodeEscapedByte(valueSpan.Slice(i + 3), out byte b1) ||
                        !TryDecodeEscapedByte(valueSpan.Slice(i + 6), out byte b2) ||
                        (b0 == 0xE0 && b1 < 0xA0) || (b0 == 0xED && b1 >= 0xA0))
                    {
                        // Malformed utf-8 sequence. emit U+FFFD and continue
                        vsb.Append(Replacement);
                        i += 2;
                        continue;
                    }

                    vsb.Append((char)(((b0 & 0x0F) << 12) | ((b1 & 0x3F) << 6) | (b2 & 0x3F)));
                    i += 8;
                    continue;
                }

                // 4-byte sequence: 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
                if ((uint)(b0 - 0xF0) <= (0xF4 - 0xF0))
                {
                    if (i + 11 >= valueSpan.Length ||
                        valueSpan[i + 3] != Percent ||
                        valueSpan[i + 6] != Percent ||
                        valueSpan[i + 9] != Percent ||
                        !TryDecodeEscapedByte(valueSpan.Slice(i + 3), out byte b1) ||
                        !TryDecodeEscapedByte(valueSpan.Slice(i + 6), out byte b2) ||
                        !TryDecodeEscapedByte(valueSpan.Slice(i + 9), out byte b3) ||
                        (b1 & 0xC0) != 0x80 || (b2 & 0xC0) != 0x80 || (b3 & 0xC0) != 0x80)
                    {
                        // Malformed utf-8 sequence. emit U+FFFD and continue
                        vsb.Append(Replacement);
                        i += 2;
                        continue;
                    }

                    int cp = (((b0 & 0x07) << 18) | ((b1 & 0x3F) << 12) | ((b2 & 0x3F) << 6) | ((b3 & 0x3F)));

                    if (cp < 0x10000 || cp > 0x10FFFF)
                    {
                        // Malformed utf-8 sequence. emit U+FFFD and continue
                        vsb.Append(Replacement);
                        i += 2;
                        continue;
                    }

                    cp -= 0x10000;
                    vsb.Append((char)((cp >> 10) + 0xD800));
                    vsb.Append((char)((cp & 0x3FF) + 0xDC00));

                    i += 11;
                    continue;
                }

                // invalid byte sequence. emit U+FFFD and continue
                vsb.Append(Replacement);
                i += 2;
            }

            value = vsb.ToString();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryDecodeEscapedByte(ReadOnlySpan<char> span, out byte value)
        {
            Debug.Assert(span.Length > 0 && span[0] == Percent);

            if (span.Length < 3 || !TryDecodeHexDigit(span[1], out byte byte1) || !TryDecodeHexDigit(span[2], out byte byte2))
            {
                value = 0;
                return false;
            }

            value = (byte)(((uint)byte1 << 4) + byte2);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryDecodeHexDigit(char c, out byte value)
        {
            value = (byte)HexConverter.FromChar((int)c);
            return value != 0xFF; // invalid hex digit
        }

        // Allowed baggage key characters:
        //  tchar = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA
        //  DIGIT = 0-9
        //  ALPHA = A-Z / a-z
#if NET
        private const string BaggageKeyValidCharacters = "!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~";
        private static readonly SearchValues<char> s_validBaggageKeyChars = SearchValues.Create(BaggageKeyValidCharacters);

        private static bool IsInvalidBaggageKey(ReadOnlySpan<char> span) => span.ContainsAnyExcept(s_validBaggageKeyChars);
#else
        private static ulong[] s_invalidBaggageKeyCharsMask = [0xFC009305FFFFFFFF, 0x2800000038000001];

        private static bool IsInvalidBaggageKey(ReadOnlySpan<char> span)
        {
            foreach (char c in span)
            {
                // key support only ascii characters according to the W3C specs
                if (c >= 0x07F || ((s_invalidBaggageKeyCharsMask[c >> 6] & (ulong)((ulong)1 << ((int)c & 63))) != 0))
                {
                    return true;
                }
            }

            return false; // valid key
        }
#endif

        // key = ( lcalpha / DIGIT ) 0*255 ( keychar )
        // keychar    = lcalpha / DIGIT / "_" / "-"/ "*" / "/" / "@"
        // lcalpha    = %x61-7A ; a-z

#if NET
        private const string TraceStateKeyValidChars = "*-/@_abcdefghijklmnopqrstuvwxyz";
        private static readonly SearchValues<char> s_validTraceStateChars = SearchValues.Create(TraceStateKeyValidChars);

        private static bool IsInvalidTraceStateKey(ReadOnlySpan<char> key) => key.IsEmpty || (key[0] < 'a' || key[0] > 'z') || key.ContainsAnyExcept(s_validTraceStateChars);

        private const string TraceStateValueValidChars = "!\"#$%&'()*+-./0123456789:;<>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        private static readonly SearchValues<char> s_validTraceStateValueChars = SearchValues.Create(TraceStateValueValidChars);

        private static bool IsInvalidTraceStateValue(ReadOnlySpan<char> value) => value.IsEmpty || value.ContainsAnyExcept(s_validTraceStateValueChars);
#else
        private static ulong[] ValidTraceStateKeyCharsMask = [0x0000A40000000000, 0x07FFFFFE80000001];

        private static bool IsInvalidTraceStateKey(ReadOnlySpan<char> key)
        {
            if (key.IsEmpty || (key[0] < 'a' || key[0] > 'z')) // Key has to start with a lowercase letter
            {
                return true; // invalid key character, skip current entry
            }

            foreach (char c in key)
            {
                if (c >= 0x07F || (ValidTraceStateKeyCharsMask[c >> 6] & (ulong)((ulong)1 << ((int)c & 63))) == 0)
                {
                    return true; // invalid key character
                }
            }

            return false; // valid key
        }

        // value = 0 * 255(chr) nblk-chr
        // nblk-chr = % x21 - 2B / % x2D - 3C / % x3E - 7E
        // chr = % x20 / nblk - chr
        private static ulong[] s_traceStateValueMask = { 0xDFFFEFFE00000000, 0x7FFFFFFFFFFFFFFF };

        private static bool IsInvalidTraceStateValue(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return true;
            }

            foreach (char c in value)
            {
                if (c >= 0x07F || (s_traceStateValueMask[c >> 6] & (ulong)((ulong)1 << ((int)c & 63))) == 0)
                {
                    return true; // invalid key character, skip current entry
                }
            }

            return false; // valid key
        }
#endif

        // baggage-string         =  list-member 0*179( OWS "," OWS list-member )
        // list-member            =  key OWS "=" OWS value *( OWS ";" OWS property )
        // property               =  key OWS "=" OWS value
        // property               =/ key OWS
        // key                    =  token ; as defined in RFC 7230, Section 3.2.6
        // value                  =  *baggage-octet
        // baggage-octet          =  %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E
        //                         ; US-ASCII characters excluding CTLs,
        //                         ; whitespace, DQUOTE, comma, semicolon,
        //                         ; and backslash
        // OWS                    =  *( SP / HTAB ) ; optional white space, as defined in RFC 7230, Section 3.2.3

        /// <summary>
        /// Encode the baggage entry key according to the W3C Specification https://www.w3.org/TR/baggage/#key and https://datatracker.ietf.org/doc/html/rfc7230#section-3.2.6.
        /// </summary>
        /// <param name="key">The baggage entry key to encode.</param>
        /// <param name="vsb">The string builder to store the encoded key.</param>
        /// <returns>True if the key encoded correctly, False other wise.</returns>
        /// <remarks>
        /// Though the baggage header is a [UTF-8] encoded string, key is limited to the ASCII code points (code point in the range U+0000 NULL to U+007F DELETE, inclusive) allowed by the definition of token in [RFC7230].
        /// This is due to the implementation details of stable implementations prior to the writing of this specification.
        /// Allowed characters: HTAB / SP /%x21 / %x23-5B / %x5D-7E
        /// </remarks>
        internal static bool EncodeBaggageKey(ReadOnlySpan<char> key, ref ValueStringBuilder vsb)
        {
            key = Trim(key);

            if (key.IsEmpty || IsInvalidBaggageKey(key))
            {
                return false;
            }

            vsb.Append(key);
            return true;
        }

        internal static void EncodeBaggageValue(ReadOnlySpan<char> value, ref ValueStringBuilder vsb)
        {
            value = Trim(value);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (!NeedToEscapeBaggageValueCharacter(c))
                {
                    vsb.Append(c);
                    continue;
                }

                if (c <= 0x7Fu) // still in ascii range
                {
                    EmitEscapedByte((byte)c, ref vsb);
                    continue;
                }

                if (c <= 0x7FFu)
                {
                    // Scalar 00000yyy yyxxxxxx -> bytes [ 110yyyyy 10xxxxxx ]
                    EmitEscapedByte((byte)((c + (0b110u << 11)) >> 6), ref vsb);
                    EmitEscapedByte((byte)((c & 0x3Fu) + 0x80u), ref vsb);
                    continue;
                }

                if (char.IsSurrogate(c))
                {
                    if (i < value.Length - 1 && char.IsSurrogatePair((char)c, value[i + 1]))
                    {
                        // Scalar 000uuuuu zzzzyyyy yyxxxxxx -> bytes [ 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx ]
                        uint v = (uint)char.ConvertToUtf32((char)c, value[i + 1]);

                        EmitEscapedByte((byte)((v + (0b11110 << 21)) >> 18), ref vsb);
                        EmitEscapedByte((byte)(((v & (0x3Fu << 12)) >> 12) + 0x80u), ref vsb);
                        EmitEscapedByte((byte)(((v & (0x3Fu << 6)) >> 6) + 0x80u), ref vsb);
                        EmitEscapedByte((byte)((v & 0x3Fu) + 0x80u), ref vsb);
                        i++;
                    }
                    else
                    {
                        // Wrong surrogate: emit 0xFFFD which has the UTF-8 encoding bytes 0xEF, 0xBF, and 0xBD.
                        EmitEscapedByte(0xEF, ref vsb);
                        EmitEscapedByte(0xBF, ref vsb);
                        EmitEscapedByte(0xBD, ref vsb);
                    }
                    continue;
                }

                // Scalar zzzzyyyy yyxxxxxx -> bytes [ 1110zzzz 10yyyyyy 10xxxxxx ]
                EmitEscapedByte((byte)((c + (0b1110 << 16)) >> 12), ref vsb);
                EmitEscapedByte((byte)(((c & (0x3Fu << 6)) >> 6) + 0x80u), ref vsb);
                EmitEscapedByte((byte)((c & 0x3Fu) + 0x80u), ref vsb);
            }
        }

        // baggage-octet =  %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E (Exclude the `%` %x25)
        // are the characters don't need to get escaped
        private static ulong[] s_baggageOctet = [0xF7FFEFDA00000000, 0x7FFFFFFFEFFFFFFF];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedToEscapeBaggageValueCharacter(char c)
        {
            if (c >= 0x07F) // non-escapable characters are in ASCII range
            {
                return true;
            }

            return (s_baggageOctet[c >> 6] & (ulong)((ulong)1 << ((int)c & 63))) == 0;
        }

        // HEXDIGLC = DIGIT / "a" / "b" / "c" / "d" / "e" / "f"; lowercase hex character
        // value           = version "-" version-format
        // version = 2HEXDIGLC; this document assumes version 00. Version ff is forbidden
        // version - format = trace - id "-" parent - id "-" trace - flags
        // trace - id = 32HEXDIGLC; 16 bytes array identifier. All zeroes forbidden
        // parent-id        = 16HEXDIGLC  ; 8 bytes array identifier. All zeroes forbidden
        // trace-flags      = 2HEXDIGLC   ; 8 bit flags.
        //         .         .         .         .         .         .
        // Example 00-0af7651916cd43dd8448eb211c80319c-b9c7c989f97918e1-01
        private static bool IsInvalidTraceParent(string? traceParent)
        {
            if (string.IsNullOrEmpty(traceParent) || traceParent.Length < 55)
            {
                return true;
            }

            if ((traceParent[0] == 'f' && traceParent[1] == 'f') || IsInvalidTraceParentCharacter(traceParent[0]) || IsInvalidTraceParentCharacter(traceParent[1]))
            {
                return true;
            }

            if (traceParent[0] == '0' && traceParent[1] == '0')
            {
                // version 00 should have exactly 55 characters
                if (traceParent.Length != 55)
                {
                    return true; // invalid length for version 00
                }
            }
            else
            {
                // If a higher version is detected, the implementation SHOULD try to parse it by trying the following:
                //      o If the size of the header is shorter than 55 characters, the vendor should not parse the header and should restart the trace.
                //      o Parse trace-id (from the first dash through the next 32 characters). Vendors MUST check that the 32 characters are hex, and that they are followed by a dash (-).
                //      o Parse parent-id (from the second dash at the 35th position through the next 16 characters). Vendors MUST check that the 16 characters are hex and followed by a dash.
                //      o Parse the sampled bit of flags (2 characters from the third dash). Vendors MUST check that the 2 characters are either the end of the string or a dash.
                if (traceParent.Length > 55 && traceParent[55] != '-')
                {
                    return true; // invalid format for version other than 00
                }
            }

            if (traceParent[2] != '-' || traceParent[35] != '-' || traceParent[52] != '-')
            {
                return true;
            }

            bool isAllZeroes = true;
            for (int i = 3; i < 35; i++)
            {
                if (IsInvalidTraceParentCharacter(traceParent[i]))
                {
                    return true;
                }

                isAllZeroes &= traceParent[i] == '0';
            }

            if (isAllZeroes)
            {
                return true; // all zeroes forbidden
            }

            isAllZeroes = true;
            for (int i = 36; i < 52; i++)
            {
                if (IsInvalidTraceParentCharacter(traceParent[i]))
                {
                    return true;
                }

                isAllZeroes &= traceParent[i] == '0';
            }

            if (isAllZeroes)
            {
                return true; // all zeroes forbidden
            }

            if (IsInvalidTraceParentCharacter(traceParent[53]) || IsInvalidTraceParentCharacter(traceParent[54]))
            {
                return true;
            }

            return false;
        }

        // '0' .. '9' and 'a' .. 'f' are valid characters in the traceparent header.
        private static ulong[] s_traceParentMask = [0x03FF000000000000, 0x0000007E00000000];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInvalidTraceParentCharacter(char c)
        {
            if (c >= 0x07F)
            {
                return true;
            }

            return (s_traceParentMask[c >> 6] & (ulong)((ulong)1 << ((int)c & 63))) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EmitEscapedByte(byte b, ref ValueStringBuilder vsb)
        {
            const string hexChars = "0123456789ABCDEF";

            vsb.Append(Percent);
            vsb.Append(hexChars[(b >> 4) & 0x0F]);
            vsb.Append(hexChars[b & 0x0F]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> TrimSpaceOnly(ReadOnlySpan<char> span) => span.Trim(Space);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Trim(ReadOnlySpan<char> span) => span.Trim(" \t");
    }
}
