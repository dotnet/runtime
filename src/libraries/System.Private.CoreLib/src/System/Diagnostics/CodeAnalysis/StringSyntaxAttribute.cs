// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Specifies the syntax used in a string.</summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    sealed class StringSyntaxAttribute : Attribute
    {
        /// <summary>Initializes the <see cref="StringSyntaxAttribute"/> with the identifier of the syntax used.</summary>
        /// <param name="syntax">The syntax identifier.</param>
        public StringSyntaxAttribute(string syntax)
        {
            Syntax = syntax;
            Arguments = Array.Empty<object?>();
        }

        /// <summary>Initializes the <see cref="StringSyntaxAttribute"/> with the identifier of the syntax used.</summary>
        /// <param name="syntax">The syntax identifier.</param>
        /// <param name="arguments">Optional arguments associated with the specific syntax employed.</param>
        public StringSyntaxAttribute(string syntax, params object?[] arguments)
        {
            Syntax = syntax;
            Arguments = arguments;
        }

        /// <summary>Gets the identifier of the syntax used.</summary>
        public string Syntax { get; }

        /// <summary>Optional arguments associated with the specific syntax employed.</summary>
        public object?[] Arguments { get; }

        /// <summary>The syntax identifier for strings containing date and time format specifiers.</summary>
        public const string DateTimeFormat = nameof(DateTimeFormat);

        /// <summary>The syntax identifier for strings containing JavaScript Object Notation (JSON).</summary>
        public const string Json = nameof(Json);

        /// <summary>The syntax identifier for strings containing regular expressions.</summary>
        public const string Regex = nameof(Regex);
    }
}
