// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace System.Globalization.Tests
{
    public enum IdnaTestResultType
    {
        ToUnicode,
        ToAscii
    }

    public class ConformanceIdnaTestResult
    {
        /// <summary>
        /// Determines if the intended result is a success or failure
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// If Success is true, then the value shows the expected value of the test
        /// If Success is false, then the value shows the conversion steps that have issues
        ///
        /// For details, see the explanation in IdnaTest.txt for the Unicode version being tested
        /// </summary>
        public string Value { get; private set; }

        public string? Source { get; private set; }

        public IdnaTestResultType ResultType { get; private set; }

        public string StatusValue { get; private set; }

        public ConformanceIdnaTestResult(string entry, string fallbackValue, IdnaTestResultType resultType = IdnaTestResultType.ToAscii)
            : this(entry, fallbackValue, null, null, useValueForStatus: true, resultType, null)
        {
        }

        public ConformanceIdnaTestResult(string entry, string fallbackValue, string statusValue, string statusFallbackValue, IdnaTestResultType resultType = IdnaTestResultType.ToAscii)
            : this(entry, fallbackValue, statusValue, statusFallbackValue, useValueForStatus: false, resultType, null)
        {
        }

        public ConformanceIdnaTestResult(string entry, string fallbackValue, string statusValue, string statusFallbackValue, string? source, IdnaTestResultType resultType = IdnaTestResultType.ToAscii)
            : this(entry, fallbackValue, statusValue, statusFallbackValue, useValueForStatus: false, resultType, source)
        {
        }

        private ConformanceIdnaTestResult(string entry, string fallbackValue, string statusValue, string statusFallbackValue, bool useValueForStatus, IdnaTestResultType resultType, string? source)
        {
            Source = source;
            ResultType = resultType;
            SetValue(string.IsNullOrEmpty(entry.Trim()) ? fallbackValue : entry);
            SetSuccess(useValueForStatus ?
                            Value :
                            string.IsNullOrEmpty(statusValue.Trim()) ? statusFallbackValue : statusValue);
        }

        private void SetValue(string entry)
        {
            Value = entry.Trim();
        }

        private void SetSuccess(string statusValue)
        {
            StatusValue = statusValue.Trim();

            Success = true;

            if (StatusValue.StartsWith('[') && StatusValue != "[]")
            {
                if (StatusValue == Value)
                {
                    Success = false;
                    return;
                }

                string[] statusCodes = StatusValue[1..^1].Split(',');
                for (int i = 0; i < statusCodes.Length; i++)
                {
                    if (!IsIgnoredError(statusCodes[i].Trim()))
                    {
                        Success = false;
                        break;
                    }
                }
            }
        }

        // Fullwidth Full Stop, Ideographic Full Stop, and Halfwidth Ideographic Full Stop
        private static char[] AllDots = ['.', '\uFF0E', '\u3002', '\uFF61'];

        private const char SoftHyphen = '\u00AD';

        private bool IsIgnorableA4_2Rule()
        {
            if (Source is null)
            {
                return false;
            }

            // Check the label lengths for the ASCII
            int lastIndex = 0;
            int index = Value.IndexOfAny(AllDots);
            while (index >= 0)
            {
                if (index - lastIndex > 63) // 63 max label length
                {
                    return false;
                }

                lastIndex = index + 1;
                index = Value.IndexOfAny(AllDots, lastIndex);
            }

            if (Value.Length - lastIndex > 63)
            {
                return false;
            }

            // Remove Hyphen as it is ignored
            if (Source.IndexOf(SoftHyphen) >= 0)
            {
                Span<char> span = stackalloc char[Source.Length];
                int spanIndex = 0;

                for (int i = 0; i < Source.Length; i++)
                {
                    if (Source[i] != SoftHyphen)
                    {
                        span[spanIndex++] = Source[i];
                    }
                }

                Source = span.Slice(0, spanIndex).ToString();
            }

            // Check the label lengths for the Source
            lastIndex = 0;
            index = Source.IndexOfAny(AllDots);
            while (index >= 0)
            {
                if (index - lastIndex > 63) // 63 max label length
                {
                    return false;
                }

                lastIndex = index + 1;
                index = Source.IndexOfAny(AllDots, lastIndex);
            }

            if (Source.Length - lastIndex > 63)
            {
                return false;
            }

            if (Source[0] is '.') // Leading dot
            {
                return false;
            }

            for (int i = 0; i < Source.Length - 1; i++)
            {
                // Consequence dots
                if ((Source[i] is '.' or '\uFF0E' or '\u3002' or '\uFF61') && (Source[i + 1] is '.' or '\uFF0E' or '\u3002' or '\uFF61'))
                {
                    return false;
                }

                // Check Historical Ranges
                if (Source[i] >= 0x2C00 && Source[i] <= 0x2C5F) // Glagolitic (U+2C00–U+2C5F)
                    return false;

                switch (Source[i])
                {
                    case '\uD800':
                        if (Source[i + 1] >= 0xDFA0 && Source[i + 1] <= 0xDFDF) return false; // Old Persian (U+103A0–U+103DF)
                        if (Source[i + 1] >= 0xDF30 && Source[i + 1] <= 0xDF4F) return false; // Gothic (U+10330–U+1034F)
                        if (Source[i + 1] >= 0xDC00 && Source[i + 1] <= 0xDC7F) return false; // Linear B (U+10000–U+1007F)
                        break;
                    case '\uD802':
                        if (Source[i + 1] >= 0xDD00  && Source[i + 1] <= 0xDD1F) return false; // Phoenician (U+10900–U+1091F)
                        break;
                    case '\uD803':
                        if (Source[i + 1] >= 0xDEA0 && Source[i + 1] <= 0xDEAF) return false; // Elymaic (U+10EA0–U+10EAF)
                        break;
                    case '\uD808':
                        if (Source[i + 1] >= 0xDC00 && Source[i + 1] <= 0xDFFF) return false; // Cuneiform (U+12000–U+123FF)
                        break;
                    case '\uD838':
                        if (Source[i + 1] >= 0xDC00 && Source[i + 1] <= 0xDCDF) return false; // Indic Siyaq Numbers (U+1E800–U+1E8DF)
                        break;
                }
            }

            return true;
        }

        private bool IsIgnoredError(string statusCode)
        {
            // We don't validate for BIDI rule so we can ignore BIDI codes
            // If we're validating ToAscii we ignore rule V2 (UIDNA_ERROR_HYPHEN_3_4) for compatibility with windows.
            return statusCode.StartsWith('B') || (ResultType == IdnaTestResultType.ToAscii && statusCode == "V2") || (statusCode.StartsWith("A4_2") && IsIgnorableA4_2Rule());
        }
    }
}
