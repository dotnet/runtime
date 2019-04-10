// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: implementation of the FormattableStringFactory
** class.
**
===========================================================*/

#nullable enable
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
        public static FormattableString Create(string format, params object?[] arguments)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

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

            public override string Format { get { return _format; } }
            public override object?[] GetArguments() { return _arguments; }
            public override int ArgumentCount { get { return _arguments.Length; } }
            public override object? GetArgument(int index) { return _arguments[index]; }
            public override string ToString(IFormatProvider? formatProvider) { return string.Format(formatProvider, _format, _arguments); }
        }
    }
}
