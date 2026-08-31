// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.ProviderBase;

namespace System.Data.OleDb
{
    internal sealed class OleDbConnectionPoolGroupProviderInfo : DbConnectionPoolGroupProviderInfo
    {
        private bool _hasQuoteFix;
        private string? _quotePrefix, _quoteSuffix;

        internal OleDbConnectionPoolGroupProviderInfo()
        {
        }

        internal bool HasQuoteFix
        {
            get { return _hasQuoteFix; }
        }
        internal string? QuotePrefix
        {
            get { return _quotePrefix; }
        }
        internal string? QuoteSuffix
        {
            get { return _quoteSuffix; }
        }

        internal void SetQuoteFix(string prefix, string suffix)
        {
            _quotePrefix = prefix;
            _quoteSuffix = suffix;
            _hasQuoteFix = true;
        }
    }
}
