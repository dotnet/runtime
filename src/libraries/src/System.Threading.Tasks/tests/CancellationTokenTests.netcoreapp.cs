// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static partial class CancellationTokenTests
    {
        [Fact]
        public static void CancellationTokenRegistration_Token_MatchesExpectedValue()
        {
            Assert.Equal(default(CancellationToken), default(CancellationTokenRegistration).Token);

            var cts = new CancellationTokenSource();
            Assert.NotEqual(default(CancellationToken), cts.Token);

            using (var ctr = cts.Token.Register(() => { }))
            {
                Assert.Equal(cts.Token, ctr.Token);
            }
        }
    }
}
