// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
**
**
** Purpose: Represents a Label to the ILGenerator class.
**
**
===========================================================*/

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    // The Label class is an opaque representation of a label used by the
    // ILGenerator class.  The token is used to mark where labels occur in the IL
    // stream and then the necessary offsets are put back in the code when the ILGenerator
    // is passed to the MethodWriter.
    // Labels are created by using ILGenerator.CreateLabel and their position is set
    // by using ILGenerator.MarkLabel.
    public readonly struct Label : IEquatable<Label>
    {
        internal readonly int m_label;

        internal Label(int label) => m_label = label;

        internal int GetLabelValue() => m_label;

        public override int GetHashCode() => m_label;

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is Label other && Equals(other);

        public bool Equals(Label obj) =>
            obj.m_label == m_label;

        public static bool operator ==(Label a, Label b) => a.Equals(b);

        public static bool operator !=(Label a, Label b) => !(a == b);
    }
}
