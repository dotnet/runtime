// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface: ISurrogateSelector
**
**
** Purpose: A user-supplied class for doing the type to surrogate
**          mapping.
**
**
===========================================================*/
namespace System.Runtime.Serialization {

    using System.Runtime.Remoting;
    using System.Security.Permissions;
    using System;
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISurrogateSelector {
        // Interface does not need to be marked with the serializable attribute
        // Specifies the next ISurrogateSelector to be examined for surrogates if the current
        // instance doesn't have a surrogate for the given type and assembly in the given context.
        [System.Security.SecurityCritical]  // auto-generated_required
        void ChainSelector(ISurrogateSelector selector);
    
        // Returns the appropriate surrogate for the given type in the given context.
        [System.Security.SecurityCritical]  // auto-generated_required
        ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector);
    
    
        // Return the next surrogate in the chain. Returns null if no more exist.
        [System.Security.SecurityCritical]  // auto-generated_required
        ISurrogateSelector GetNextSelector();
    }
}
