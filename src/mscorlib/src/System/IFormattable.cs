// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System {
    
    using System;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
#if CONTRACTS_FULL
    [ContractClass(typeof(IFormattableContract))]
#endif // CONTRACTS_FULL
    public interface IFormattable
    {
        [Pure]
        String ToString(String format, IFormatProvider formatProvider);
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IFormattable))]
    internal abstract class IFormattableContract : IFormattable
    {
       String IFormattable.ToString(String format, IFormatProvider formatProvider)
       {
           Contract.Ensures(Contract.Result<String>() != null);
 	       throw new NotImplementedException();
       }
    }
#endif // CONTRACTS_FULL
}
