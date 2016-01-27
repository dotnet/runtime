// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Wrapper that is converted to a variant with VT_UNKNOWN.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
   
    using System;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class UnknownWrapper
    {
        public UnknownWrapper(Object obj)
        {
            m_WrappedObject = obj;
        }

        public Object WrappedObject 
        {
            get 
            {
                return m_WrappedObject;
            }
        }

        private Object m_WrappedObject;
    }
}
