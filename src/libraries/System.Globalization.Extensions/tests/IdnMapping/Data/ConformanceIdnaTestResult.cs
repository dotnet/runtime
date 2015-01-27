// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace System.Globalization.Extensions.Tests
{
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

        public ConformanceIdnaTestResult(string entry)
            : this(entry, null)
        { }

        public ConformanceIdnaTestResult(string entry, string fallbackValue)
        {
            if (string.IsNullOrWhiteSpace(entry))
                SetValues(fallbackValue);
            else
                SetValues(entry);
        }

        private void SetValues(string entry)
        {
            if (entry.Trim().StartsWith("[", StringComparison.Ordinal))
            {
                Success = false;
                Value = entry.Trim();
            }
            else
            {
                Success = true;
                Value = entry.Trim();
            }
        }
    }
}
