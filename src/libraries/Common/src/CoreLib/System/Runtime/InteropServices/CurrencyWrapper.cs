// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // Wrapper that is converted to a variant with VT_CURRENCY.
    public sealed class CurrencyWrapper
    {
        public CurrencyWrapper(Decimal obj)
        {
            WrappedObject = obj;
        }

        public CurrencyWrapper(Object obj)
        {
            if (!(obj is Decimal))
                throw new ArgumentException(SR.Arg_MustBeDecimal, nameof(obj));

            WrappedObject = (Decimal)obj;
        }

        public Decimal WrappedObject { get; }
    }
}
