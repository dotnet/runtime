// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines the base of other number types.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface INumberBase<TSelf>
        : IAdditionOperators<TSelf, TSelf, TSelf>,
          IAdditiveIdentity<TSelf, TSelf>,
          IDecrementOperators<TSelf>,
          IDivisionOperators<TSelf, TSelf, TSelf>,
          IEqualityOperators<TSelf, TSelf>,     // implies IEquatable<TSelf>
          IIncrementOperators<TSelf>,
          IMultiplicativeIdentity<TSelf, TSelf>,
          IMultiplyOperators<TSelf, TSelf, TSelf>,
          ISpanFormattable,                     // implies IFormattable
          ISubtractionOperators<TSelf, TSelf, TSelf>,
          IUnaryPlusOperators<TSelf, TSelf>,
          IUnaryNegationOperators<TSelf, TSelf>
        where TSelf : INumberBase<TSelf>
    {
        /// <summary>Gets the value <c>1</c> for the type.</summary>
        static abstract TSelf One { get; }

        /// <summary>Gets the value <c>0</c> for the type.</summary>
        static abstract TSelf Zero { get; }
    }
}
