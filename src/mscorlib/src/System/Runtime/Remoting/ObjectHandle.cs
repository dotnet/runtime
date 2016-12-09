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

namespace System.Runtime.Remoting
{
    using System;
    using System.Runtime.InteropServices;

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ObjectHandle: 
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
    }
}
