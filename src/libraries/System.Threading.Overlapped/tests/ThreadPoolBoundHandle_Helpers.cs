// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public partial class ThreadPoolBoundHandleTests : FileCleanupTestBase
{
    private List<SafeHandle> _handlesToDispose = new List<SafeHandle>();

    struct BlittableType
    {
        public int i;
    }

    struct NonBlittableType
    {
        public string s;
    }

    private ThreadPoolBoundHandle CreateThreadPoolBoundHandle([CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
    {
        return CreateThreadPoolBoundHandle((SafeHandle)null, memberName, lineNumber);
    }

    private ThreadPoolBoundHandle CreateThreadPoolBoundHandle(SafeHandle handle, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
    {
        if (handle == null)
        {
            handle = HandleFactory.CreateAsyncFileHandleForWrite(GetTestFilePath(null, memberName, lineNumber));

            // ThreadPoolBoundHandle does not take ownership of the handle;
            // we must close it
            _handlesToDispose.Add(handle);
        }

        return ThreadPoolBoundHandle.BindHandle(handle);
    }

    protected override void Dispose(bool disposing)
    {
        if (_handlesToDispose != null)
        {
            if (disposing)
            {
                foreach(SafeHandle handle in _handlesToDispose)
                {
                    handle.Dispose();
                }

                _handlesToDispose = null;
            }
        }

        base.Dispose(disposing);
    }
}
