// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface: IDeserializationEventListener
**
**
** Purpose: Implemented by any class that wants to indicate that
**          it wishes to receive deserialization events.
**
**
===========================================================*/

using System;

namespace System.Runtime.Serialization
{
    // Interface does not need to be marked with the serializable attribute    
    public interface IDeserializationCallback
    {
        void OnDeserialization(Object sender);
    }
}
