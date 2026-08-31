// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that a class or struct is a union type, enabling compiler support for union behaviors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Any class or struct annotated with this attribute is recognized by the C# compiler as a union type.
    /// Union types may support behaviors such as implicit conversions from case types, pattern matching
    /// that unwraps the union's contents, and switch exhaustiveness checking.
    /// </para>
    /// </remarks>
    /// <seealso cref="IUnion" />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class UnionAttribute : Attribute
    {
    }
}
