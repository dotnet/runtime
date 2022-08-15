// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for power functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface IPowerFunctions<TSelf>
        : INumberBase<TSelf>
        where TSelf : IPowerFunctions<TSelf>
    {
        /// <summary>Computes a value raised to a given power.</summary>
        /// <param name="x">The value which is raised to the power of <paramref name="x" />.</param>
        /// <param name="y">The power to which <paramref name="x" /> is raised.</param>
        /// <returns><paramref name="x" /> raised to the power of <paramref name="y" />.</returns>
        static abstract TSelf Pow(TSelf x, TSelf y);
    }
}
