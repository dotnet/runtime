// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit
{
    public struct EventToken
    {
        public static readonly EventToken Empty = new EventToken();

        internal EventToken(int eventToken)
        {
            Token = eventToken;
        }

        public int Token { get; }

        public override int GetHashCode() => Token;

        public override bool Equals(object? obj) => obj is EventToken et && Equals(et);

        public bool Equals(EventToken obj) => obj.Token == Token;

        public static bool operator ==(EventToken a, EventToken b) => a.Equals(b);

        public static bool operator !=(EventToken a, EventToken b) => !(a == b);
    }
}
