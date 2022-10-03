// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for floating-point constants.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface IFloatingPointConstants<TSelf>
        : INumberBase<TSelf>
        where TSelf : IFloatingPointConstants<TSelf>
    {
        /// <summary>Gets the mathematical constant <c>e</c>.</summary>
        static abstract TSelf E { get; }

        /// <summary>Gets the mathematical constant <c>pi</c>.</summary>
        static abstract TSelf Pi { get; }

        /// <summary>Gets the mathematical constant <c>tau</c>.</summary>
        static abstract TSelf Tau { get; }
    }
}
