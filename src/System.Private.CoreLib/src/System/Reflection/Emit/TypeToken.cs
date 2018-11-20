// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit
{
    public struct TypeToken
    {
        public static readonly TypeToken Empty = new TypeToken();

        private readonly int _token;

        internal TypeToken(int typeToken)
        {
            _token = typeToken;
        }

        public int Token => _token;

        public override int GetHashCode() => Token;

        public override bool Equals(object obj) => obj is TypeToken tt && Equals(tt);

        public bool Equals(TypeToken obj) => obj.Token == Token;

        public static bool operator ==(TypeToken a, TypeToken b) => a.Equals(b);

        public static bool operator !=(TypeToken a, TypeToken b) => !(a == b);
    }
}

