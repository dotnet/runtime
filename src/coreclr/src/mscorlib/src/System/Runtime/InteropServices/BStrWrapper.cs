// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Wrapper that is converted to a variant with VT_BSTR.
**
**
=============================================================================*/


using System;
using System.Security;

namespace System.Runtime.InteropServices
{
    public sealed class BStrWrapper
    {
        public BStrWrapper(String value)
        {
            m_WrappedObject = value;
        }

        public BStrWrapper(Object value)
        {
            m_WrappedObject = (String)value;
        }

        public String WrappedObject
        {
            get
            {
                return m_WrappedObject;
            }
        }

        private String m_WrappedObject;
    }
}
