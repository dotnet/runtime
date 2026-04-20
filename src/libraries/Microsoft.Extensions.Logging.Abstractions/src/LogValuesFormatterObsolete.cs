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
    [Obsolete("This type is retained only for compatibility. The recommended alternative is Microsoft.Extensions.Diagnostics.Testing.", error: true)]
    public sealed class LogValuesFormatter
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

        /// <inheritdoc cref="Microsoft.Extensions.Logging.LogValuesFormatter.OriginalFormat"/>
        public string OriginalFormat => _inner.OriginalFormat;

        /// <inheritdoc cref="Microsoft.Extensions.Logging.LogValuesFormatter.ValueNames"/>
        public List<string> ValueNames => _inner.ValueNames;

        /// <inheritdoc cref="Microsoft.Extensions.Logging.LogValuesFormatter.Format(object?[])"/>
        public string Format(object?[]? values) => _inner.Format(values);

        /// <inheritdoc cref="Microsoft.Extensions.Logging.LogValuesFormatter.GetValue(object?[], int)"/>
        public KeyValuePair<string, object?> GetValue(object?[] values, int index) => _inner.GetValue(values, index);

        /// <inheritdoc cref="Microsoft.Extensions.Logging.LogValuesFormatter.GetValues(object[])"/>
        public IEnumerable<KeyValuePair<string, object?>> GetValues(object[] values) => _inner.GetValues(values);
    }
}
