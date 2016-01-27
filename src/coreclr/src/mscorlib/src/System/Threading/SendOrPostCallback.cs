// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
