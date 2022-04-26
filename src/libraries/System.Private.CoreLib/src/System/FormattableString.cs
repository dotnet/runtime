// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: implementation of the FormattableString
** class.
**
===========================================================*/

using System.Diagnostics.CodeAnalysis;

namespace System
{
    /// <summary>
    /// A composite format string along with the arguments to be formatted. An instance of this
    /// type may result from the use of the C# or VB language primitive "interpolated string".
    /// </summary>
    public abstract class FormattableString : IFormattable
    {
        /// <summary>
        /// The composite format string.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
        public abstract string Format { get; }

        /// <summary>
        /// Returns an object array that contains zero or more objects to format. Clients should not
        /// mutate the contents of the array.
        /// </summary>
        public abstract object?[] GetArguments();

        /// <summary>
        /// The number of arguments to be formatted.
        /// </summary>
        public abstract int ArgumentCount { get; }

        /// <summary>
        /// Returns one argument to be formatted from argument position <paramref name="index"/>.
        /// </summary>
        public abstract object? GetArgument(int index);

        /// <summary>
        /// Format to a string using the given culture.
        /// </summary>
        public abstract string ToString(IFormatProvider? formatProvider);

        string IFormattable.ToString(string? ignored, IFormatProvider? formatProvider) =>
            ToString(formatProvider);

        /// <summary>
        /// Format the given object in the invariant culture. This static method may be
        /// imported in C# by
        /// <code>
        /// using static System.FormattableString;
        /// </code>.
        /// Within the scope
        /// of that import directive an interpolated string may be formatted in the
        /// invariant culture by writing, for example,
        /// <code>
        /// Invariant($"{{ lat = {latitude}; lon = {longitude} }}")
        /// </code>
        /// </summary>
        public static string Invariant(FormattableString formattable)
        {
            ArgumentNullException.ThrowIfNull(formattable);
            return formattable.ToString(Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Format the given object in the current culture. This static method may be
        /// imported in C# by
        /// <code>
        /// using static System.FormattableString;
        /// </code>.
        /// Within the scope
        /// of that import directive an interpolated string may be formatted in the
        /// current culture by writing, for example,
        /// <code>
        /// CurrentCulture($"{{ lat = {latitude}; lon = {longitude} }}")
        /// </code>
        /// </summary>
        public static string CurrentCulture(FormattableString formattable)
        {
            ArgumentNullException.ThrowIfNull(formattable);
            return formattable.ToString(Globalization.CultureInfo.CurrentCulture);
        }

        public override string ToString() =>
            ToString(Globalization.CultureInfo.CurrentCulture);
    }
}
