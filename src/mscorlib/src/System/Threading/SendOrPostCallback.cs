// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
/*============================================================
**
**
**
** Purpose: Represents a method to be called when a message is to be dispatched to a synchronization context. 
**
** 
===========================================================*/

namespace System.Threading
{    
    public delegate void SendOrPostCallback(Object state);
}
