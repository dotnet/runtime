// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**              
**
** Purpose: Defines the root type for all marshal by reference aka
**          AppDomain bound types
**          
**
**
===========================================================*/
namespace System
{
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class MarshalByRefObject 
    {
        protected MarshalByRefObject() { }
        public object GetLifetimeService()
        {
            throw new PlatformNotSupportedException();
        }
        public virtual object InitializeLifetimeService()
        {
            throw new PlatformNotSupportedException();
        }
        protected MarshalByRefObject MemberwiseClone(bool cloneIdentity)
        {
            MarshalByRefObject mbr = (MarshalByRefObject)base.MemberwiseClone();
            return mbr;
        }
    }
}
