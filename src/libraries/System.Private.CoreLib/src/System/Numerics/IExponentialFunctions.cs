// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for exponential functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface IExponentialFunctions<TSelf>
        where TSelf : IExponentialFunctions<TSelf>
    {
        /// <summary>Computes <c>E</c> raised to a given power.</summary>
        /// <param name="x">The power to which <c>E</c> is raised.</param>
        /// <returns><c>E</c> raised to the power of <paramref name="x" />.</returns>
        static abstract TSelf Exp(TSelf x);

        // The following methods are approved but not yet implemented in the libraries
        // * static abstract TSelf ExpM1(TSelf x);
        // * static abstract TSelf Exp2(TSelf x);
        // * static abstract TSelf Exp2M1(TSelf x);
        // * static abstract TSelf Exp10(TSelf x);
        // * static abstract TSelf Exp10M1(TSelf x);
    }
}
