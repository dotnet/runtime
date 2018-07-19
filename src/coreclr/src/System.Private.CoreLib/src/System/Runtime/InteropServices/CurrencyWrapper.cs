// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Wrapper that is converted to a variant with VT_CURRENCY.
    /// </summary>
    public sealed class CurrencyWrapper
    {
        public CurrencyWrapper(decimal obj)
        {
            m_WrappedObject = obj;
        }

        public CurrencyWrapper(object obj)
        {
            if (!(obj is decimal))
            {
                throw new ArgumentException(SR.Arg_MustBeDecimal, nameof(obj));
            }

            m_WrappedObject = (decimal)obj;
        }

        public decimal WrappedObject => m_WrappedObject;

        private decimal m_WrappedObject;
    }
}
