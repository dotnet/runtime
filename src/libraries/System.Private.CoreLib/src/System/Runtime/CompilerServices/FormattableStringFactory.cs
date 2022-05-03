// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: implementation of the FormattableStringFactory
** class.
**
===========================================================*/

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// A factory type used by compilers to create instances of the type <see cref="FormattableString"/>.
    /// </summary>
    public static class FormattableStringFactory
    {
        /// <summary>
        /// Create a <see cref="FormattableString"/> from a composite format string and object
        /// array containing zero or more objects to format.
        /// </summary>
        public static FormattableString Create([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] arguments)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(arguments);

            return new ConcreteFormattableString(format, arguments);
        }

        private sealed class ConcreteFormattableString : FormattableString
        {
            private readonly string _format;
            private readonly object?[] _arguments;

            internal ConcreteFormattableString(string format, object?[] arguments)
            {
                _format = format;
                _arguments = arguments;
            }

            public override string Format => _format;
            public override object?[] GetArguments() { return _arguments; }
            public override int ArgumentCount => _arguments.Length;
            public override object? GetArgument(int index) { return _arguments[index]; }
            public override string ToString(IFormatProvider? formatProvider) { return string.Format(formatProvider, _format, _arguments); }
        }
    }
}
