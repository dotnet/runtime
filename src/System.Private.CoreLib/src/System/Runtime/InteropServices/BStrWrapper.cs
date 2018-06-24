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
        public BStrWrapper(string value)
        {
            m_WrappedObject = value;
        }

        public BStrWrapper(object value)
        {
            m_WrappedObject = (string)value;
        }

        public string WrappedObject
        {
            get
            {
                return m_WrappedObject;
            }
        }

        private string m_WrappedObject;
    }
}
