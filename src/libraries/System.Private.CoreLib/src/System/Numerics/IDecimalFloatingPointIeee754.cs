// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines an IEEE 754 floating-point type that is represented in a base-10 format.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface IDecimalFloatingPointIeee754<TSelf>
        : IFloatingPointIeee754<TSelf>
        where TSelf : IDecimalFloatingPointIeee754<TSelf>
    {
        // 5.3.2
        static abstract TSelf Quantize(TSelf x, TSelf y);
        static abstract TSelf Quantum(TSelf x);

        // 5.5.2
        // TODO put these in BitConverter
/*        TOther EncodeDecimal(TSelf x);
        TSelf DecodeDecimal(TOther x);
        TOther EncodeBinary(TSelf x);
        TSelf DecodeBinary(TOther x);*/

        // 5.7.3
        static abstract bool SameQuantum(TSelf x, TSelf y);
    }
}
