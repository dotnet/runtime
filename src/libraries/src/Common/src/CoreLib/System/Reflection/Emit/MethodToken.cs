// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit
{
    public struct MethodToken
    {
        public static readonly MethodToken Empty = new MethodToken();

        internal MethodToken(int methodToken)
        {
            Token = methodToken;
        }

        public int Token { get; }

        public override int GetHashCode() => Token;

        public override bool Equals(object? obj) => obj is MethodToken mt && Equals(mt);

        public bool Equals(MethodToken obj) => obj.Token == Token;

        public static bool operator ==(MethodToken a, MethodToken b) => a.Equals(b);

        public static bool operator !=(MethodToken a, MethodToken b) => !(a == b);
    }
}
