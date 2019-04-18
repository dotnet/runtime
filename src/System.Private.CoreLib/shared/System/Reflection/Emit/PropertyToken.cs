// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Reflection.Emit
{
    public struct PropertyToken
    {
        public static readonly PropertyToken Empty = new PropertyToken();

        internal PropertyToken(int propertyToken)
        {
            Token = propertyToken;
        }

        public int Token { get; }

        public override int GetHashCode() => Token;

        public override bool Equals(object? obj) => obj is PropertyToken pt && Equals(pt);

        public bool Equals(PropertyToken obj) => obj.Token == Token;

        public static bool operator ==(PropertyToken a, PropertyToken b) => a.Equals(b);

        public static bool operator !=(PropertyToken a, PropertyToken b) => !(a == b);
    }
}
