// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.FileProviders.Physical.Internal
{
    public class TestClock : IClock
    {
        public DateTime UtcNow { get; private set; } = DateTime.UtcNow;

        public void Increment()
        {
            UtcNow = UtcNow.Add(PhysicalFilesWatcher.DefaultPollingInterval);
        }
    }
}
