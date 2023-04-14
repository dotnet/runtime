// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System
{
    // NOTE: This is a workaround for current inlining limitations of some backend code generators.
    // We would prefer to not have this interface at all and instead just use TChar.CreateTruncuating.
    // Once inlining is improved on these hot code paths in formatting, we can remove this interface.

    /// <summary>Internal interface used to unify char and byte in formatting operations.</summary>
    internal interface IUtfChar<TSelf> :
        IBinaryInteger<TSelf>
        where TSelf : unmanaged, IUtfChar<TSelf>
    {
        /// <summary>Casts the specified value to this type.</summary>
        public static abstract TSelf CastFrom(byte value);

        /// <summary>Casts the specified value to this type.</summary>
        public static abstract TSelf CastFrom(char value);

        /// <summary>Casts the specified value to this type.</summary>
        public static abstract TSelf CastFrom(int value);

        /// <summary>Casts the specified value to this type.</summary>
        public static abstract TSelf CastFrom(uint value);

        /// <summary>Casts the specified value to this type.</summary>
        public static abstract TSelf CastFrom(ulong value);
    }
}
