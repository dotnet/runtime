// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace System
{
    /// <summary>
    /// Extensions for string normalization.
    /// </summary>
    public static partial class StringNormalizationExtensions
    {
        /// <summary>
        /// Determines whether the specified string is in a normalized <see cref="NormalizationForm.FormC" />.
        /// </summary>
        /// <param name="strInput">The string to check.</param>
        /// <returns><see langword="true"/> if the specified string is in a normalized form; otherwise, <see langword="false"/>.</returns>
        public static bool IsNormalized(this string strInput)
        {
            return IsNormalized(strInput, NormalizationForm.FormC);
        }

        /// <summary>
        /// Determines whether the specified string is in a normalized form.
        /// </summary>
        /// <param name="strInput">The string to check.</param>
        /// <param name="normalizationForm">The normalization form to use.</param>
        /// <returns><see langword="true"/> if the specified string is in a normalized form; otherwise, <see langword="false"/>.</returns>
        public static bool IsNormalized(this string strInput, NormalizationForm normalizationForm)
        {
            ArgumentNullException.ThrowIfNull(strInput);

            return strInput.IsNormalized(normalizationForm);
        }

        /// <summary>
        /// Determines whether the specified span of characters is in a normalized form.
        /// </summary>
        /// <param name="source">The span of characters to check.</param>
        /// <param name="normalizationForm">The normalization form to use.</param>
        /// <returns><see langword="true"/> if the specified span of characters is in a normalized form; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">The specified character span contains an invalid code point or the normalization form is invalid.</exception>
        public static bool IsNormalized(this ReadOnlySpan<char> source, NormalizationForm normalizationForm = NormalizationForm.FormC) =>
            Normalization.IsNormalized(source, normalizationForm);

        /// <summary>
        /// Normalizes the specified string to the <see cref="NormalizationForm.FormC" />.
        /// </summary>
        /// <param name="strInput">The string to normalize.</param>
        /// <returns>The normalized string in <see cref="NormalizationForm.FormC" />.</returns>
        public static string Normalize(this string strInput)
        {
            // Default to Form C
            return Normalize(strInput, NormalizationForm.FormC);
        }

        /// <summary>
        /// Normalizes the specified string to the specified normalization form.
        /// </summary>
        /// <param name="strInput">The string to normalize.</param>
        /// <param name="normalizationForm">The normalization form to use.</param>
        /// <returns>The normalized string in the specified normalization form.</returns>
        public static string Normalize(this string strInput, NormalizationForm normalizationForm)
        {
            ArgumentNullException.ThrowIfNull(strInput);

            return strInput.Normalize(normalizationForm);
        }

        /// <summary>
        /// Normalizes the specified span of characters to the specified normalization form.
        /// </summary>
        /// <param name="source">The span of characters to normalize.</param>
        /// <param name="destination">The buffer to write the normalized characters to.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters written to <paramref name="destination" />.</param>
        /// <param name="normalizationForm">The normalization form to use.</param>
        /// <returns><see langword="true"/> if the specified span of characters was successfully normalized; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">The specified character span contains an invalid code point or the normalization form is invalid.</exception>
        public static bool TryNormalize(this ReadOnlySpan<char> source, Span<char> destination, out int charsWritten, NormalizationForm normalizationForm = NormalizationForm.FormC) =>
            Normalization.TryNormalize(source, destination, out charsWritten, normalizationForm);

        /// <summary>
        /// Gets the estimated length of the normalized form of the specified string in the <see cref="NormalizationForm.FormC" />.
        /// </summary>
        /// <param name="source">The character span to get the estimated length of the normalized form.</param>
        /// <param name="normalizationForm">The normalization form to use.</param>
        /// <returns>The estimated length of the normalized form of the specified string.</returns>
        /// <exception cref="ArgumentException">The specified character span contains an invalid code point or the normalization form is invalid.</exception>
        public static int GetNormalizedLength(this ReadOnlySpan<char> source, NormalizationForm normalizationForm = NormalizationForm.FormC) =>
            Normalization.GetNormalizedLength(source, normalizationForm);
    }
}
