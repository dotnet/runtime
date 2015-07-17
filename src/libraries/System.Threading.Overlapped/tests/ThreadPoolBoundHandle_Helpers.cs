// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    struct BlittableType
    {
        public int i;
    }

    struct NonBlittableType
    {
        public string s;
    }

    private static ThreadPoolBoundHandle CreateThreadPoolBoundHandle()
    {
        return CreateThreadPoolBoundHandle((SafeHandle)null);
    }

    private static ThreadPoolBoundHandle CreateThreadPoolBoundHandle(SafeHandle handle)
    {
        handle = handle ?? HandleFactory.CreateAsyncFileHandleForWrite();

        return ThreadPoolBoundHandle.BindHandle(handle);
    }
}
