// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Interface: IAsyncResult
**
** Purpose: Interface to encapsulate the results of an async
**          operation
**
===========================================================*/

using System.Threading;

namespace System
{
    public interface IAsyncResult
    {
        bool IsCompleted { get; }

        WaitHandle AsyncWaitHandle { get; }


        object? AsyncState { get; }

        bool CompletedSynchronously { get; }
    }
}
