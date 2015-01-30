// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
#if FEATURE_SERIALIZATION
        [System.Security.SecurityCritical]  // auto-generated_required
        void GetObjectData(SerializationInfo info, StreamingContext context);
#endif
    }

}




