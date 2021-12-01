// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a value indicating whether the name of the associated property is
    /// parenthesized in the properties window.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ParenthesizePropertyNameAttribute : Attribute
    {
        /// <summary>
        /// Sets the System.ComponentModel.Design.ParenthesizePropertyName
        /// attribute by default to <see langword='false'/>.
        /// </summary>
        public static readonly ParenthesizePropertyNameAttribute Default = new ParenthesizePropertyNameAttribute();

        public ParenthesizePropertyNameAttribute() : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.Design.ParenthesizePropertyNameAttribute
        /// class, using the specified value to indicate whether the attribute is
        /// marked for display with parentheses.
        /// </summary>
        public ParenthesizePropertyNameAttribute(bool needParenthesis)
        {
            NeedParenthesis = needParenthesis;
        }

        /// <summary>
        /// Gets a value indicating whether the attribute is placed in parentheses when
        /// listed in the properties window.
        /// </summary>
        public bool NeedParenthesis { get; }

        /// <summary>
        /// Compares the specified object to this object and tests for equality.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is ParenthesizePropertyNameAttribute other && other.NeedParenthesis == NeedParenthesis;

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
