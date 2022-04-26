// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.InteropServices
{
    // Wrapper that is converted to a variant with VT_CURRENCY.
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("CurrencyWrapper and support for marshalling to the VARIANT type may be unavailable in future releases.")]
    public sealed class CurrencyWrapper
    {
        public CurrencyWrapper(decimal obj)
        {
            WrappedObject = obj;
        }

        public CurrencyWrapper(object obj)
        {
            if (!(obj is decimal))
                throw new ArgumentException(SR.Arg_MustBeDecimal, nameof(obj));

            WrappedObject = (decimal)obj;
        }

        public decimal WrappedObject { get; }
    }
}
