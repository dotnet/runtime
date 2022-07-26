// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines a number type which can only represent positive values, that is it cannot represent negative values.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface IUnsignedNumber<TSelf>
        : INumberBase<TSelf>
        where TSelf : IUnsignedNumber<TSelf>
    {
    }
}
