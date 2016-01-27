// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface: ISurrogate
**
**
** Purpose: The interface implemented by an object which
**          supports surrogates.
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
    public interface ISerializationSurrogate {
    // Interface does not need to be marked with the serializable attribute
        // Returns a SerializationInfo completely populated with all of the data needed to reinstantiate the
        // the object at the other end of serialization.  
        //
        [System.Security.SecurityCritical]  // auto-generated_required
        void GetObjectData(Object obj, SerializationInfo info, StreamingContext context);
    
        // Reinflate the object using all of the information in data.  The information in
        // members is used to find the particular field or property which needs to be set.
        // 
        [System.Security.SecurityCritical]  // auto-generated_required
        Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector);
    }
}
