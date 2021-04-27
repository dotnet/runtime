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

        [Obsolete(Obsoletions.RemotingApisMessage, DiagnosticId = Obsoletions.RemotingApisDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public object GetLifetimeService()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Remoting);
        }

        [Obsolete(Obsoletions.RemotingApisMessage, DiagnosticId = Obsoletions.RemotingApisDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
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
