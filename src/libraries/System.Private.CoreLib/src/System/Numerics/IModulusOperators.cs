// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines a mechanism for computing the modulus or remainder of two values.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TOther">The type that will divide <typeparamref name="TSelf" />.</typeparam>
    /// <typeparam name="TResult">The type that contains the modulus or remainder of <typeparamref name="TSelf" /> and <typeparamref name="TOther" />.</typeparam>
    /// <remarks>This type represents the <c>%</c> in C# which is often used to compute the remainder and may differ from an actual modulo operation depending on the type that implements the interface.</remarks>
    public interface IModulusOperators<TSelf, TOther, TResult>
        where TSelf : IModulusOperators<TSelf, TOther, TResult>
    {
        /// <summary>Divides two values together to compute their modulus or remainder.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The modulus or remainder of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        static abstract TResult operator %(TSelf left, TOther right);
    }
}
