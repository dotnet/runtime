// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    // Don't derive from BaseHeaderParser since parsing is delegated to Uri.TryCreate()
    // which will remove leading and trailing whitespace.
    internal sealed class UriHeaderParser : HttpHeaderParser
    {
        private readonly UriKind _uriKind;

        internal static readonly UriHeaderParser RelativeOrAbsoluteUriParser =
            new UriHeaderParser(UriKind.RelativeOrAbsolute);

        private UriHeaderParser(UriKind uriKind)
            : base(false)
        {
            _uriKind = uriKind;
        }

        public override bool TryParseValue([NotNullWhen(true)] string? value, object? storeValue, ref int index, [NotNullWhen(true)] out object? parsedValue)
        {
            parsedValue = null;

            // Some headers support empty/null values. This one doesn't.
            if (string.IsNullOrEmpty(value) || (index == value.Length))
            {
                return false;
            }

            string uriString = value;
            if (index > 0)
            {
                uriString = value.Substring(index);
            }

            if (!Uri.TryCreate(uriString, _uriKind, out Uri? uri))
            {
                // Some servers send the host names in Utf-8.
                uriString = DecodeUtf8FromString(uriString);

                if (!Uri.TryCreate(uriString, _uriKind, out uri))
                {
                    return false;
                }
            }

            index = value.Length;
            parsedValue = uri;
            return true;
        }

        // The normal client header parser just casts bytes to chars (see GetString).
        // Check if those bytes were actually utf-8 instead of ASCII.
        // If not, just return the input value.
        internal static string DecodeUtf8FromString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            bool possibleUtf8 = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > (char)255)
                {
                    return input; // This couldn't have come from the wire, someone assigned it directly.
                }
                else if (input[i] > (char)127)
                {
                    possibleUtf8 = true;
                    break;
                }
            }
            if (possibleUtf8)
            {
                byte[] rawBytes = new byte[input.Length];
                for (int i = 0; i < input.Length; i++)
                {
                    if (input[i] > (char)255)
                    {
                        return input; // This couldn't have come from the wire, someone assigned it directly.
                    }
                    rawBytes[i] = (byte)input[i];
                }
                try
                {
                    // We don't want '?' replacement characters, just fail.
                    System.Text.Encoding decoder = System.Text.Encoding.GetEncoding("utf-8", System.Text.EncoderFallback.ExceptionFallback,
                        System.Text.DecoderFallback.ExceptionFallback);
                    return decoder.GetString(rawBytes, 0, rawBytes.Length);
                }
                catch (ArgumentException) { } // Not actually Utf-8
            }
            return input;
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is Uri);
            Uri uri = (Uri)value;

            if (uri.IsAbsoluteUri)
            {
                return uri.AbsoluteUri;
            }
            else
            {
                return uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }
        }
    }
}
