// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface: IObjectReference
**
**
** Purpose: Implemented by objects that are actually references
**          to a different object which can't be discovered until
**          this one is completely restored.  During the fixup stage,
**          any object implementing IObjectReference is asked for it's
**          "real" object and that object is inserted into the graph.
**
**
===========================================================*/

using System;

namespace System.Runtime.Serialization
{
    // Interface does not need to be marked with the serializable attribute
    public interface IObjectReference
    {
        Object GetRealObject(StreamingContext context);
    }
}


