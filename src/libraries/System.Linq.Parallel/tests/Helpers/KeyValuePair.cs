// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Linq.Parallel.Tests
{
    internal static class KeyValuePair
    {
        internal static KeyValuePair<T, U> Create<T, U>(T first, U second)
        {
            return new KeyValuePair<T, U>(first, second);
        }
    }
}
