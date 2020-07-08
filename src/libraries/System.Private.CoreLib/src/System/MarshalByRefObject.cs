// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [ComVisible(true)]
    public abstract class MarshalByRefObject
    {
        protected MarshalByRefObject()
        {
        }

        public object GetLifetimeService()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Remoting);
        }

        public virtual object InitializeLifetimeService()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Remoting);
        }

        protected MarshalByRefObject MemberwiseClone(bool cloneIdentity)
        {
            MarshalByRefObject mbr = (MarshalByRefObject)base.MemberwiseClone();
            return mbr;
        }
    }
}
