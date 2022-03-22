// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Defines a mechanism for getting the multiplicative identity of a given type.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TResult">The type that contains the multiplicative identify of <typeparamref name="TSelf" />.</typeparam>
    public interface IMultiplicativeIdentity<TSelf, TResult>
        where TSelf : IMultiplicativeIdentity<TSelf, TResult>
    {
        /// <summary>Gets the multiplicative identity of the current type.</summary>
        static abstract TResult MultiplicativeIdentity { get; }
    }
}
