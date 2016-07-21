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

namespace System.Runtime.Serialization {
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System;
    using System.Reflection;

    [System.Runtime.InteropServices.ComVisible(true)]
    public interface ISerializable {
        [System.Security.SecurityCritical]  // auto-generated_required
        void GetObjectData(SerializationInfo info, StreamingContext context);
    }

}




