// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Numerics.Tensors.Tests
{
    internal static class Helpers
    {
        public static IEnumerable<int> TensorLengthsIncluding0 => Enumerable.Range(0, 257);

        public static IEnumerable<int> TensorLengths => Enumerable.Range(1, 256);
    }
}
