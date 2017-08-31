// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Runtime.InteropServices;
    [StructLayout(LayoutKind.Sequential)]
    public struct CharacterRange
    {
        private int _first;
        private int _length;

        /// <summary>
        /// Initializes a new instance of the <see cref='CharacterRange'/> class with the specified coordinates.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
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
            get
            {
                return _first;
            }
            set
            {
                _first = value;
            }
        }

        /// <summary>
        /// Gets the Length of this <see cref='CharacterRange'/>.
        /// </summary>
        public int Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(CharacterRange))
                return false;

            CharacterRange cr = (CharacterRange)obj;
            return ((_first == cr.First) && (_length == cr.Length));
        }

        public static bool operator ==(CharacterRange cr1, CharacterRange cr2)
        {
            return ((cr1.First == cr2.First) && (cr1.Length == cr2.Length));
        }

        public static bool operator !=(CharacterRange cr1, CharacterRange cr2)
        {
            return !(cr1 == cr2);
        }

        public override int GetHashCode()
        {
            return unchecked(_first << 8 + _length);
        }
    }
}

