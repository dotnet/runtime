// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit
{  
    /// <summary>
    /// The ParameterToken class is an opaque representation of the Token returned
    /// by the Metadata to represent the parameter.
    /// </summary>
    public struct ParameterToken
    {
        public static readonly ParameterToken Empty = new ParameterToken();

        internal ParameterToken(int parameterToken)
        {
            Token = parameterToken;
        }

        public int Token { get; }

        public override int GetHashCode() => Token;

        public override bool Equals(object? obj) => obj is ParameterToken pt && Equals(pt);

        public bool Equals(ParameterToken obj) => obj.Token == Token;

        public static bool operator ==(ParameterToken a, ParameterToken b) => a.Equals(b);

        public static bool operator !=(ParameterToken a, ParameterToken b) => !(a == b);
    }
}
