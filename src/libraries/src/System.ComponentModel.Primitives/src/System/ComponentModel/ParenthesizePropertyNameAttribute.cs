// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides a value indicating whether the name of the associated property is parenthesized in the
    ///       properties window.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ParenthesizePropertyNameAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Sets the System.ComponentModel.Design.ParenthesizePropertyName
        ///       attribute by default to <see langword='false'/>.
        ///    </para>
        /// </summary>
        public static readonly ParenthesizePropertyNameAttribute Default = new ParenthesizePropertyNameAttribute();

        public ParenthesizePropertyNameAttribute() : this(false)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the System.ComponentModel.Design.ParenthesizePropertyNameAttribute 
        /// class, using the specified value to indicate whether the attribute is
        /// marked for display with parentheses.</para>
        /// </summary>
        public ParenthesizePropertyNameAttribute(bool needParenthesis) => NeedParenthesis = needParenthesis;

        /// <summary>
        ///    <para>
        ///       Gets a value indicating whether the attribute is
        ///       placed in parentheses when listed in the properties window.
        ///    </para>
        /// </summary>
        public bool NeedParenthesis { get; }

        /// <summary>
        ///    <para>Compares the specified object to this object and tests for equality.</para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            ParenthesizePropertyNameAttribute other = obj as ParenthesizePropertyNameAttribute;
            return other?.NeedParenthesis == NeedParenthesis;
        }

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
