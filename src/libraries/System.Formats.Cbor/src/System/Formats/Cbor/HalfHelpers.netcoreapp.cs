// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Formats.Cbor
{
    internal static partial class HalfHelpers
    {
        public static unsafe float HalfToFloat(Half value)
            => (float)value;

        public static unsafe double HalfToDouble(Half value)
            => (double)value;
    }
}
