// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Extensions.Logging.Internal
{
    /// <summary>
    /// LogValues to enable formatting options supported by <see cref="string.Format(IFormatProvider, string, object?)"/>.
    /// This also enables using {NamedFormatItem} in the format string.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is retained only for compatibility. The recommended alternative is the Microsoft.Extensions.Diagnostics.Testing package.", error: true)]
    public class FormattedLogValues : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private Microsoft.Extensions.Logging.FormattedLogValues _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="values">The values.</param>
        public FormattedLogValues(string? format, params object?[]? values)
        {
            _inner = new Microsoft.Extensions.Logging.FormattedLogValues(format, values);
        }

        /// <inheritdoc />
        public KeyValuePair<string, object?> this[int index] => _inner[index];

        /// <inheritdoc />
        public int Count => _inner.Count;

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _inner.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public override string ToString() => _inner.ToString();
    }
}
