// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    internal readonly partial struct TriState
    {
        private readonly byte _value; // 0 is "default", not false

        public static readonly TriState Default = new TriState(0);
        public static readonly TriState False = new TriState(1);
        public static readonly TriState True = new TriState(2);

        private TriState(byte value)
        {
            _value = value;
        }

        public bool IsDefault
        {
            get { return this == Default; }
        }

        public bool IsFalse
        {
            get { return this == False; }
        }

        public bool IsNotDefault
        {
            get { return this != Default; }
        }

        public bool IsTrue
        {
            get { return this == True; }
        }

        public static bool operator ==(TriState left, TriState right)
        {
            return left._value == right._value;
        }

        public static bool operator !=(TriState left, TriState right)
        {
            return !(left == right);
        }

        public override bool Equals(object o)
        {
            TriState state = (TriState)o;
            return _value == state._value;
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public static implicit operator TriState(bool value)
        {
            return (value) ? True : False;
        }

        public static explicit operator bool (TriState value)
        {
            if (value.IsDefault)
                throw new InvalidCastException(SR.TriStateCompareError);
            else
                return (value == TriState.True);
        }

        /// <summary>
        /// Provides some interesting information about the TriState in String form.
        /// </summary>
        public override string ToString()
        {
            if (this == Default) return "Default";
            else if (this == False) return "False";
            else return "True";
        }
    }
}

