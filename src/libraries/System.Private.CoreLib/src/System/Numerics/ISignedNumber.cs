// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines a number type which can represent both positive and negative values.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface ISignedNumber<TSelf>
        where TSelf : INumberBase<TSelf>, ISignedNumber<TSelf>
    {
        /// <summary>Gets the value <c>-1</c> for the type.</summary>
        static abstract TSelf NegativeOne { get; }
    }
}
