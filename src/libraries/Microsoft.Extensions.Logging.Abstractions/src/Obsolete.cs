// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Abstractions.Internal
{
    [System.Obsolete("TODO")]
    public partial class NullScope
    {
        internal NullScope() { }

        public static NullScope Instance { get { throw new NotImplementedException(); } }

        public void Dispose() { }
    }

    [System.Obsolete("TODO")]
    public partial class TypeNameHelper
    {
        public TypeNameHelper() { }

        public static string GetTypeDisplayName(System.Type type) { throw new NotImplementedException(); }
    }
}

namespace Microsoft.Extensions.Logging.Internal
{
    [System.Obsolete("TODO")]
    public partial class FormattedLogValues
    {
        public FormattedLogValues(string format, params object[] values) { }

        public int Count { get { throw new NotImplementedException(); } }

        public System.Collections.Generic.KeyValuePair<string, object> this[int index] { get { throw new NotImplementedException(); } }

        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator() { throw new NotImplementedException(); }

        public override string ToString() { throw new NotImplementedException(); }
    }

    [System.Obsolete("TODO")]
    public partial class LogValuesFormatter
    {
        public LogValuesFormatter(string format) { }

        public string OriginalFormat { get { throw new NotImplementedException(); } }

        public System.Collections.Generic.List<string> ValueNames { get { throw new NotImplementedException(); } }

        public string Format(object[] values) { throw new NotImplementedException(); }

        public System.Collections.Generic.KeyValuePair<string, object> GetValue(object[] values, int index) { throw new NotImplementedException(); }

        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>> GetValues(object[] values) { throw new NotImplementedException(); }
    }
}
