// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableSortedSetBuilderDebuggerProxyTest : ImmutablesTestBase
    {
        [Fact]
        public void DoesNotCacheContents()
        {
            var set = ImmutableSortedSet<int>.Empty.Add(1);
            var builder = set.ToBuilder();
            var debuggerProxy = new ImmutableSortedSetBuilderDebuggerProxy<int>(builder);
            var contents = debuggerProxy.Contents; // view the contents to trigger caching
            builder.Add(2);
            Assert.Equal(builder.ToArray(), debuggerProxy.Contents);
        }
    }
}
