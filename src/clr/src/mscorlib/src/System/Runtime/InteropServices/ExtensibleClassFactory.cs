// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Methods used to customize the creation of managed objects that
**          extend from unmanaged objects.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;

    using System;
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ExtensibleClassFactory
    {
    
        // Prevent instantiation.
        private ExtensibleClassFactory() {}
    
        // Register a delegate that will be called whenever an instance of a managed
        // type that extends from an unmanaged type needs to allocate the aggregated
        // unmanaged object. This delegate is expected to allocate and aggregate the
        // unmanaged object and is called in place of a CoCreateInstance. This
        // routine must be called in the context of the static initializer for the
        // class for which the callbacks will be made. 
        // It is not legal to register this callback from a class that has any
        // parents that have already registered a callback.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void RegisterObjectCreationCallback(ObjectCreationDelegate callback);
    }
}
