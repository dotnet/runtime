// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public class AsyncResult : IAsyncResult
{
    public object AsyncState
    {
        get { throw new NotImplementedException(); }
    }

    public System.Threading.WaitHandle AsyncWaitHandle
    {
        get { throw new NotImplementedException(); }
    }

    public bool CompletedSynchronously
    {
        get { throw new NotImplementedException(); }
    }

    public bool IsCompleted
    {
        get { throw new NotImplementedException(); }
    }
}
