// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace System.Collections.Immutable.Tests
{
    public abstract class SimpleElementImmutablesTestBase : ImmutablesTestBase
    {
        protected abstract IEnumerable<T> GetEnumerableOf<T>(params T[] contents);

        protected IEnumerable<T> GetEnumerableOf<T>(IEnumerable<T> contents)
        {
            return GetEnumerableOf(contents.ToArray());
        }
    }
}
