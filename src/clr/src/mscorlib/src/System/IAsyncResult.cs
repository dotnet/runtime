// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Interface: IAsyncResult
**
** Purpose: Interface to encapsulate the results of an async
**          operation
**
===========================================================*/
namespace System {
    
    using System;
    using System.Threading;
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IAsyncResult
    {
        bool IsCompleted { get; }

        WaitHandle AsyncWaitHandle { get; }


        Object     AsyncState      { get; }

        bool       CompletedSynchronously { get; }
   
    
    }

}
