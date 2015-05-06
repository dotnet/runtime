// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public override abstract bool Equals(object o);

        public override abstract int GetHashCode();

        public override abstract string ToString();

        public static bool operator ==(IdentityReference left, IdentityReference right)
        {
            object l = left;
            object r = right;

            if (l == null && r == null)
            {
                return true;
            }
            else if (l == null || r == null)
            {
                return false;
            }
            else
            {
                return left.Equals(right);
            }
        }

        public static bool operator !=(IdentityReference left, IdentityReference right)
        {
            return !(left == right);
        }
    }
}
