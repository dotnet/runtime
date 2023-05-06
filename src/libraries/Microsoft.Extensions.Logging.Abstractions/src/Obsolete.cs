// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.Abstractions.Internal
{
    [System.Obsolete("TODO")]
    public partial class NullScope : IDisposable
    {
        internal NullScope() { }

        public static NullScope Instance { get; } = new NullScope();

        public void Dispose() { }
    }

    [System.Obsolete("TODO")]
    public partial class TypeNameHelper
    {
        public TypeNameHelper() { }

        public static string GetTypeDisplayName(System.Type type) =>
            Microsoft.Extensions.Internal.TypeNameHelper.GetTypeDisplayName(type);
    }
}

namespace Microsoft.Extensions.Logging.Internal
{
    [System.Obsolete("TODO")]
    public partial class FormattedLogValues : IReadOnlyList<KeyValuePair<string, object>>
    {
        public FormattedLogValues(string format, params object[] values) { }

        public int Count { get { throw new NotImplementedException(); } }

        public KeyValuePair<string, object> this[int index] { get { throw new NotImplementedException(); } }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() { throw new NotImplementedException(); }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() { throw new NotImplementedException(); }
    }

    [System.Obsolete("TODO")]
    public partial class LogValuesFormatter
    {
        public LogValuesFormatter(string format) { }

        public string OriginalFormat { get { throw new NotImplementedException(); } }

        public List<string> ValueNames { get { throw new NotImplementedException(); } }

        public string Format(object[] values) { throw new NotImplementedException(); }

        public KeyValuePair<string, object> GetValue(object[] values, int index) { throw new NotImplementedException(); }

        public IEnumerable<KeyValuePair<string, object>> GetValues(object[] values) { throw new NotImplementedException(); }
    }
}
