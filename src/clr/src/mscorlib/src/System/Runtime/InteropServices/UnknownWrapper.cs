// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
