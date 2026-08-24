// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Extensions.Logging.Internal
{
    /// <summary>
    /// Formatter to convert the named format items like {NamedformatItem} to <see cref="string.Format(IFormatProvider, string, object)"/> format.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is retained only for compatibility. The recommended alternative is the Microsoft.Extensions.Diagnostics.Testing package.", error: true)]
    public class LogValuesFormatter
    {
        private readonly Microsoft.Extensions.Logging.LogValuesFormatter _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogValuesFormatter"/> class.
        /// </summary>
        /// <param name="format">The named format string.</param>
        public LogValuesFormatter(string format)
        {
            _inner = new Microsoft.Extensions.Logging.LogValuesFormatter(format);
        }

        /// <summary>Gets the original format string.</summary>
        public string OriginalFormat => _inner.OriginalFormat;

        /// <summary>Gets the list of named format parameter names.</summary>
        public List<string> ValueNames => _inner.ValueNames;

        /// <summary>Formats the given values using the format string.</summary>
        public string Format(object?[]? values) => _inner.Format(values);

        /// <summary>Gets the key/value pair for the format item at the specified index.</summary>
        public KeyValuePair<string, object?> GetValue(object?[] values, int index) => _inner.GetValue(values, index);

        /// <summary>Gets an enumerable of key/value pairs for all format items.</summary>
        public IEnumerable<KeyValuePair<string, object?>> GetValues(object[] values) => _inner.GetValues(values);
    }
}
