// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Reflection.Emit
{
    public struct SignatureToken
    {
        public static readonly SignatureToken Empty = new SignatureToken();

        internal SignatureToken(int signatureToken)
        {
            Token = signatureToken;
        }

        public int Token { get; }

        public override int GetHashCode() => Token;

        public override bool Equals(object? obj) => obj is SignatureToken st && Equals(st);

        public bool Equals(SignatureToken obj) => obj.Token == Token;

        public static bool operator ==(SignatureToken a, SignatureToken b) => a.Equals(b);

        public static bool operator !=(SignatureToken a, SignatureToken b) => !(a == b);
    }
}
