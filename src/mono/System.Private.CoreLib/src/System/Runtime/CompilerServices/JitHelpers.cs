// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    internal static class JitHelpers
    {
        [Intrinsic]
        public static bool EnumEquals<T>(T x, T y) where T : struct, Enum => x.Equals(y);

        [Intrinsic]
        public static int EnumCompareTo<T>(T x, T y) where T : struct, Enum => x.CompareTo(y);

        [Intrinsic]
        internal static void DisableInline() => throw new NotImplementedException();
    }
}
