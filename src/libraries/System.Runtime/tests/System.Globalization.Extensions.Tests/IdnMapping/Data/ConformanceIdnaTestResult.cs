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

        public IdnaTestResultType ResultType { get; private set; }

        public string StatusValue { get; private set; }

        public ConformanceIdnaTestResult(string entry, string fallbackValue, IdnaTestResultType resultType = IdnaTestResultType.ToAscii)
            : this(entry, fallbackValue, null, null, useValueForStatus: true, resultType)
        {
        }

        public ConformanceIdnaTestResult(string entry, string fallbackValue, string statusValue, string statusFallbackValue, IdnaTestResultType resultType = IdnaTestResultType.ToAscii)
            : this(entry, fallbackValue, statusValue, statusFallbackValue, useValueForStatus: false, resultType)
        {
        }

        private ConformanceIdnaTestResult(string entry, string fallbackValue, string statusValue, string statusFallbackValue, bool useValueForStatus, IdnaTestResultType resultType)
        {
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

        private bool IsIgnoredError(string statusCode)
        {
            // We don't validate for BIDI rule so we can ignore BIDI codes
            // If we're validating ToAscii we ignore rule V2 (UIDNA_ERROR_HYPHEN_3_4) for compatibility with windows.
            return statusCode.StartsWith('B') || (ResultType == IdnaTestResultType.ToAscii && statusCode == "V2");
        }
    }
}
