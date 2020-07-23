// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Principal
{
    public abstract class IdentityReference
    {
        internal IdentityReference()
        {
            // this exists to prevent creation of user-derived classes (for now)
        }

        public abstract string Value { get; }

        public abstract bool IsValidTargetType(Type targetType);

        public abstract IdentityReference Translate(Type targetType);

        public abstract override bool Equals(object? o);

        public abstract override int GetHashCode();

        public abstract override string ToString();

        public static bool operator ==(IdentityReference? left, IdentityReference? right)
        {
            object? l = left;
            object? r = right;

            if (l == r)
            {
                return true;
            }
            else if (l == null || r == null)
            {
                return false;
            }
            else
            {
                return left!.Equals(right);
            }
        }

        public static bool operator !=(IdentityReference? left, IdentityReference? right)
        {
            return !(left == right);
        }
    }
}
