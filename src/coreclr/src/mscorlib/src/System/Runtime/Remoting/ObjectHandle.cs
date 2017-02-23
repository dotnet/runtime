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

using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting
{
    public class ObjectHandle
    {
        private Object WrappedObject;

        private ObjectHandle()
        {
        }

        internal ObjectHandle(Object o)
        {
            WrappedObject = o;
        }

        internal Object Unwrap()
        {
            return WrappedObject;
        }
    }
}
