// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: Wrapper that is converted to a variant with VT_BYREF | VT_VARIANT.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
   
    using System;

    [Serializable]

    public sealed class VariantWrapper
    {
        public VariantWrapper(Object obj)
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
