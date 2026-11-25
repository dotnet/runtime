// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Defines a value that a type in a type map entry provides through <see cref="TypeMapping.TryGetValue{TTypeMapGroup, TValue}(string, out TValue)"/>
    /// and <see cref="TypeMapping.TryGetValue{TTypeMapGroup, TValue}(Type, out TValue)"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <remarks>
    /// <para>
    /// A mapped type (the target type of an external type map entry or the proxy type of a proxy type map entry) can implement
    /// this interface for several <typeparamref name="TValue"/> types. A lookup asks for one of them.
    /// Mapped types that are interfaces or generic type definitions don't provide values.
    /// </para>
    /// <para>
    /// The mapped type itself is never exposed, only the value is. The value is as visible to reflection as the code of
    /// <see cref="Value"/> makes it: for example a <c>typeof</c> expression in the getter makes that type visible.
    /// The getter should be a simple expression without side effects: it can be evaluated ahead of time, not run at all, or run any number of times,
    /// concurrently and on any thread.
    /// </para>
    /// </remarks>
    public interface ITypeMapValue<TValue>
    {
        /// <summary>
        /// Gets the value that the mapped type provides.
        /// </summary>
        /// <value>The value of the type map entry.</value>
        static abstract TValue Value { get; }
    }
}
