// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for logarithmic functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface ILogarithmicFunctions<TSelf>
        : IFloatingPointConstants<TSelf>
        where TSelf : ILogarithmicFunctions<TSelf>
    {
        /// <summary>Computes the natural (<c>base-E</c>) logarithm of a value.</summary>
        /// <param name="x">The value whose natural logarithm is to be computed.</param>
        /// <returns><c>log<sub>e</sub>(<paramref name="x" />)</c></returns>
        static abstract TSelf Log(TSelf x);

        /// <summary>Computes the logarithm of a value in the specified base.</summary>
        /// <param name="x">The value whose logarithm is to be computed.</param>
        /// <param name="newBase">The base in which the logarithm is to be computed.</param>
        /// <returns><c>log<sub><paramref name="newBase" /></sub>(<paramref name="x" />)</c></returns>
        static abstract TSelf Log(TSelf x, TSelf newBase);

        /// <summary>Computes the natural (<c>base-E</c>) logarithm of a value plus one.</summary>
        /// <param name="x">The value to which one is added before computing the natural logarithm.</param>
        /// <returns><c>log<sub>e</sub>(<paramref name="x" /> + 1)</c></returns>
        static virtual TSelf LogP1(TSelf x) => TSelf.Log(x + TSelf.One);

        /// <summary>Computes the base-2 logarithm of a value.</summary>
        /// <param name="x">The value whose base-2 logarithm is to be computed.</param>
        /// <returns><c>log<sub>2</sub>(<paramref name="x" />)</c></returns>
        static abstract TSelf Log2(TSelf x);

        /// <summary>Computes the base-2 logarithm of a value plus one.</summary>
        /// <param name="x">The value to which one is added before computing the base-2 logarithm.</param>
        /// <returns><c>log<sub>2</sub>(<paramref name="x" /> + 1)</c></returns>
        static virtual TSelf Log2P1(TSelf x) => TSelf.Log2(x + TSelf.One);

        /// <summary>Computes the base-10 logarithm of a value.</summary>
        /// <param name="x">The value whose base-10 logarithm is to be computed.</param>
        /// <returns><c>log<sub>10</sub>(<paramref name="x" />)</c></returns>
        static abstract TSelf Log10(TSelf x);

        /// <summary>Computes the base-10 logarithm of a value plus one.</summary>
        /// <param name="x">The value to which one is added before computing the base-10 logarithm.</param>
        /// <returns><c>log<sub>10</sub>(<paramref name="x" /> + 1)</c></returns>
        static virtual TSelf Log10P1(TSelf x) => TSelf.Log10(x + TSelf.One);
    }
}
