// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Numerics.Globalization
{
    internal class FormattingSpecification
    {
        public string? CurrencySymbol { get; set; }
        public string DecimalSeparator { get; }
        public string GroupSeparator { get; }

        public bool IsCurrencyParsing { get; }

        public FormattingSpecification(string? currencySymbol,
            string decimalSeparator,
            string groupSeparator)
        {
            CurrencySymbol = currencySymbol;
            DecimalSeparator = decimalSeparator;
            GroupSeparator = groupSeparator;

            IsCurrencyParsing = !string.IsNullOrEmpty(CurrencySymbol);
        }

        public bool TryGetCurrencySymbol([NotNullWhen(true)] out string? currencySymbol)
        {
            currencySymbol = CurrencySymbol;
            return !string.IsNullOrEmpty(currencySymbol);
        }
    }
}
