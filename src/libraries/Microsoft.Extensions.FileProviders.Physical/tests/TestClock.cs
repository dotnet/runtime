// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
