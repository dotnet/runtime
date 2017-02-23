// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface: ISerializable
**
**
** Purpose: Implemented by any object that needs to control its
**          own serialization.
**
**
===========================================================*/

using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System;
using System.Reflection;

namespace System.Runtime.Serialization
{
    public interface ISerializable
    {
        void GetObjectData(SerializationInfo info, StreamingContext context);
    }
}




