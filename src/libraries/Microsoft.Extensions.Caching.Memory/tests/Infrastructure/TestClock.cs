// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Internal
{
    public class TestClock : ISystemClock
    {
        public TestClock()
        {
            UtcNow = new DateTime(2013, 6, 15, 12, 34, 56, 789);
        }

        public DateTimeOffset UtcNow { get; set; }

        public void Add(TimeSpan timeSpan)
        {
            UtcNow = UtcNow + timeSpan;
        }
    }
}
