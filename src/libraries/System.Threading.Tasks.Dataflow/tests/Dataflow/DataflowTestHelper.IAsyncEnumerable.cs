// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Threading.Tasks.Dataflow.Tests
{
    internal static partial class DataflowTestHelpers
    {
        internal static Func<int, IAsyncEnumerable<int>> ToAsyncEnumerable = item => AsyncEnumerable.Repeat(item, 1);
    }
}
