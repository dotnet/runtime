// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static partial class EventArgsTests
    {
        [Fact]
        public static void Ctor()
        {
            Assert.NotNull(new EventArgs());
            Assert.NotNull(EventArgs.Empty);
        }
    }
}
