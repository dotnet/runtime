// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** ObjectHandle wraps object references. A Handle allows a 
** marshal by value object to be returned through an 
** indirection allowing the caller to control when the
** object is loaded into their domain.
**
** 
===========================================================*/

namespace System.Runtime.Remoting{

    using System;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
#if FEATURE_REMOTING
    using System.Runtime.Remoting.Activation;
    using System.Runtime.Remoting.Lifetime;
#endif

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ObjectHandle: 
#if FEATURE_REMOTING
        MarshalByRefObject, 
#endif
        IObjectHandle 
    {
        private Object WrappedObject;
        
        private ObjectHandle()
        {
        }

        public ObjectHandle(Object o)
        {
            WrappedObject = o;
        }

        public Object Unwrap()
        {
            return WrappedObject;
        }

        // ObjectHandle has a finite lifetime. For now the default
        // lifetime is being used, this can be changed in this method to
        // specify a custom lifetime.
#if FEATURE_REMOTING
        [System.Security.SecurityCritical]  // auto-generated_required
        public override Object InitializeLifetimeService()
        {
            BCLDebug.Trace("REMOTE", "ObjectHandle.InitializeLifetimeService");

            //
            // If the wrapped object has implemented InitializeLifetimeService to return null,
            // we don't want to go to the base class (which will result in a lease being
            // requested from the MarshalByRefObject, which starts up the LeaseManager,
            // which starts up the ThreadPool, adding three threads to the process.
            // We check if the wrapped object is a MarshalByRef object, and call InitializeLifetimeServices on it
            // and if it returns null, we return null. Otherwise we fall back to the old behavior.
            //

            MarshalByRefObject mbr = WrappedObject as MarshalByRefObject;
            if (mbr != null) {
                Object o = mbr.InitializeLifetimeService();
                if (o == null)
                    return null;
            }
            ILease lease = (ILease)base.InitializeLifetimeService();
            return lease;
        }
#endif // FEATURE_REMOTING
    }
}
