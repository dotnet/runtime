// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public partial class ImmutableSortedDictionaryTest : ImmutableDictionaryTestBase
    {
        internal override IBinaryTree GetRootNode<TKey, TValue>(IImmutableDictionary<TKey, TValue> dictionary)
        {
            return ((ImmutableSortedDictionary<TKey, TValue>)dictionary).Root;
        }
    }
}
