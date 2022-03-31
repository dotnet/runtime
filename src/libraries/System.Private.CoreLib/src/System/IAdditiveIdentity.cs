// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Defines a mechanism for getting the additive identity of a given type.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TResult">The type that contains the additive identify of <typeparamref name="TSelf" />.</typeparam>
    public interface IAdditiveIdentity<TSelf, TResult>
        where TSelf : IAdditiveIdentity<TSelf, TResult>
    {
        /// <summary>Gets the additive identity of the current type.</summary>
        static abstract TResult AdditiveIdentity { get; }
    }
}
