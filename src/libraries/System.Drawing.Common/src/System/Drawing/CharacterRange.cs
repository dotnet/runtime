// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CharacterRange
    {
        private int _first;
        private int _length;

        /// <summary>
        /// Initializes a new instance of the <see cref='CharacterRange'/> class with the specified coordinates.
        /// </summary>
        public CharacterRange(int First, int Length)
        {
            _first = First;
            _length = Length;
        }

        /// <summary>
        /// Gets the First character position of this <see cref='CharacterRange'/>.
        /// </summary>
        public int First
        {
            readonly get => _first;
            set => _first = value;
        }

        /// <summary>
        /// Gets the Length of this <see cref='CharacterRange'/>.
        /// </summary>
        public int Length
        {
            readonly get => _length;
            set => _length = value;
        }

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is CharacterRange cr))
            {
                return false;
            }

            return First == cr.First && Length == cr.Length;
        }

        public static bool operator ==(CharacterRange cr1, CharacterRange cr2) => cr1.Equals(cr2);

        public static bool operator !=(CharacterRange cr1, CharacterRange cr2) => !(cr1 == cr2);

        public override readonly int GetHashCode() => HashCode.Combine(First, Length);
    }
}
