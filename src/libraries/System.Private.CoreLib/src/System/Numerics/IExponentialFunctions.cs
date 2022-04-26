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
        /// <returns><c>E<sup><paramref name="x" /></sup></c></returns>
        static abstract TSelf Exp(TSelf x);

        /// <summary>Computes <c>E</c> raised to a given power and subtracts one.</summary>
        /// <param name="x">The power to which <c>E</c> is raised.</param>
        /// <returns><c>E<sup><paramref name="x" /></sup> - 1</c></returns>
        static abstract TSelf ExpM1(TSelf x);

        /// <summary>Computes <c>2</c> raised to a given power.</summary>
        /// <param name="x">The power to which <c>2</c> is raised.</param>
        /// <returns><c>2<sup><paramref name="x" /></sup></c></returns>
        static abstract TSelf Exp2(TSelf x);

        /// <summary>Computes <c>2</c> raised to a given power and subtracts one.</summary>
        /// <param name="x">The power to which <c>2</c> is raised.</param>
        /// <returns><c>2<sup><paramref name="x" /></sup> - 1</c></returns>
        static abstract TSelf Exp2M1(TSelf x);

        /// <summary>Computes <c>10</c> raised to a given power.</summary>
        /// <param name="x">The power to which <c>10</c> is raised.</param>
        /// <returns><c>10<sup><paramref name="x" /></sup></c></returns>
        static abstract TSelf Exp10(TSelf x);

        /// <summary>Computes <c>10</c> raised to a given power and subtracts one.</summary>
        /// <param name="x">The power to which <c>10</c> is raised.</param>
        /// <returns><c>10<sup><paramref name="x" /></sup> - 1</c></returns>
        static abstract TSelf Exp10M1(TSelf x);
    }
}
