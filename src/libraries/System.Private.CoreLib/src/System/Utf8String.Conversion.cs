// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace System
{
    public sealed partial class Utf8String
    {
        /// <summary>
        /// Returns a value stating whether this <see cref="Utf8String"/> instance is normalized
        /// using the specified Unicode normalization form.
        /// </summary>
        /// <param name="normalizationForm">The <see cref="NormalizationForm"/> to check.</param>
        /// <returns><see langword="true"/> if this <see cref="Utf8String"/> instance represents text
        /// normalized under <paramref name="normalizationForm"/>, otherwise <see langword="false"/>.</returns>
        public bool IsNormalized(NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            // TODO_UTF8STRING: Avoid allocations in this code path.

            return ToString().IsNormalized(normalizationForm);
        }

        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8String"/> instance
        /// normalized using the specified Unicode normalization form.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8String"/> is left unchanged by this operation.
        /// </remarks>
        public Utf8String Normalize(NormalizationForm normalizationForm = NormalizationForm.FormC) => this.AsSpanSkipNullCheck().Normalize(normalizationForm);

        /// <summary>
        /// Converts this <see cref="Utf8String"/> to a <see langword="char[]"/>.
        /// </summary>
        public char[] ToCharArray() => this.AsSpanSkipNullCheck().ToCharArray();

        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8String"/> instance
        /// converted to lowercase using <paramref name="culture"/>.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8String"/> is left unchanged by this operation. Note that the returned
        /// <see cref="Utf8String"/> instance may be longer or shorter (in terms of UTF-8 byte count) than the
        /// input <see cref="Utf8String"/>.
        /// </remarks>
        public Utf8String ToLower(CultureInfo culture) => this.AsSpanSkipNullCheck().ToLower(culture);

        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8String"/> instance
        /// converted to lowercase using the invariant culture.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8String"/> is left unchanged by this operation. For more information on the
        /// invariant culture, see the <see cref="CultureInfo.InvariantCulture"/> property. Note that the returned
        /// <see cref="Utf8String"/> instance may be longer or shorter (in terms of UTF-8 byte count) than the
        /// input <see cref="Utf8String"/>.
        /// </remarks>
        public Utf8String ToLowerInvariant() => this.AsSpanSkipNullCheck().ToLowerInvariant();

        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8String"/> instance
        /// converted to uppercase using <paramref name="culture"/>.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8String"/> is left unchanged by this operation. Note that the returned
        /// <see cref="Utf8String"/> instance may be longer or shorter (in terms of UTF-8 byte count) than the
        /// input <see cref="Utf8String"/>.
        /// </remarks>
        public Utf8String ToUpper(CultureInfo culture) => this.AsSpanSkipNullCheck().ToUpper(culture);

        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8String"/> instance
        /// converted to uppercase using the invariant culture.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8String"/> is left unchanged by this operation. For more information on the
        /// invariant culture, see the <see cref="CultureInfo.InvariantCulture"/> property. Note that the returned
        /// <see cref="Utf8String"/> instance may be longer or shorter (in terms of UTF-8 byte count) than the
        /// input <see cref="Utf8String"/>.
        /// </remarks>
        public Utf8String ToUpperInvariant() => this.AsSpanSkipNullCheck().ToUpperInvariant();
    }
}
