// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Wrapper that is converted to a variant with VT_CURRENCY.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
   
    using System;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class CurrencyWrapper
    {
        public CurrencyWrapper(Decimal obj)
        {
            m_WrappedObject = obj;
        }

        public CurrencyWrapper(Object obj)
        {            
            if (!(obj is Decimal))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDecimal"), "obj");
            m_WrappedObject = (Decimal)obj;
        }

        public Decimal WrappedObject 
        {
            get 
            {
                return m_WrappedObject;
            }
        }

        private Decimal m_WrappedObject;
    }
}
