// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Used to denote the target of a <see cref="GotoExpression"/>.
    /// </summary>
    public sealed class LabelTarget
    {
        internal LabelTarget(Type type, string? name)
        {
            Type = type;
            Name = name;
        }

        /// <summary>
        /// Gets the name of the label.
        /// </summary>
        /// <remarks>The label's name is provided for information purposes only.</remarks>
        public string? Name { get; }

        /// <summary>
        /// The type of value that is passed when jumping to the label
        /// (or System.Void if no value should be passed).
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="object"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="object"/>.</returns>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? "UnamedLabel" : Name;
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="LabelTarget"/> representing a label with void type and no name.
        /// </summary>
        /// <returns>The new <see cref="LabelTarget"/>.</returns>
        public static LabelTarget Label()
        {
            return Label(typeof(void), name: null);
        }

        /// <summary>
        /// Creates a <see cref="LabelTarget"/> representing a label with void type and the given name.
        /// </summary>
        /// <param name="name">The name of the label.</param>
        /// <returns>The new <see cref="LabelTarget"/>.</returns>
        public static LabelTarget Label(string? name)
        {
            return Label(typeof(void), name);
        }

        /// <summary>
        /// Creates a <see cref="LabelTarget"/> representing a label with the given type.
        /// </summary>
        /// <param name="type">The type of value that is passed when jumping to the label.</param>
        /// <returns>The new <see cref="LabelTarget"/>.</returns>
        public static LabelTarget Label(Type type)
        {
            return Label(type, name: null);
        }

        /// <summary>
        /// Creates a <see cref="LabelTarget"/> representing a label with the given type and name.
        /// </summary>
        /// <param name="type">The type of value that is passed when jumping to the label.</param>
        /// <param name="name">The name of the label.</param>
        /// <returns>The new <see cref="LabelTarget"/>.</returns>
        public static LabelTarget Label(Type type, string? name)
        {
            ArgumentNullException.ThrowIfNull(type);
            TypeUtils.ValidateType(type, nameof(type));
            return new LabelTarget(type, name);
        }
    }
}
