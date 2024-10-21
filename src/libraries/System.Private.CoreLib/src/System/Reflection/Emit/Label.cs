// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    /// <summary>
    /// Represents a label in the instruction stream. Used in conjunction with the <see cref="ILGenerator"/> class.
    /// </summary>
    /// <remarks>
    /// The Label class is an opaque representation of a label used by the
    /// <see cref="ILGenerator"/> class.  The token is used to mark where labels occur in the IL
    /// stream. Labels are created by using <see cref="ILGenerator.DefineLabel"/> and their position is set
    /// by using <see cref="ILGenerator.MarkLabel"/>.
    /// </remarks>
    public readonly struct Label : IEquatable<Label>
    {
        internal readonly int m_label;

        internal Label(int label) => m_label = label;

        /// <summary>
        /// Gets the label unique id assigned by the ILGenerator.
        /// </summary>
        public int Id => m_label;

        public override int GetHashCode() => m_label;

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is Label other && Equals(other);

        public bool Equals(Label obj) =>
            obj.m_label == m_label;

        public static bool operator ==(Label a, Label b) => a.Equals(b);

        public static bool operator !=(Label a, Label b) => !(a == b);
    }
}
