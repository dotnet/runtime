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


using System;

namespace System.Runtime.InteropServices
{
    public sealed class UnknownWrapper
    {
        public UnknownWrapper(object obj)
        {
            m_WrappedObject = obj;
        }

        public object WrappedObject
        {
            get
            {
                return m_WrappedObject;
            }
        }

        private object m_WrappedObject;
    }
}
