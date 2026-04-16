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
    /// <para>
    /// Implementing <see cref="IUnion" /> is optional but recommended, as it provides runtime access to
    /// the union's value and is automatically implemented by compiler-generated union declarations.
    /// </para>
    /// </remarks>
    /// <example>
    /// The following example shows a manually declared union type:
    /// <code language="csharp">
    /// [Union]
    /// public struct Pet : IUnion
    /// {
    ///     public Pet(Cat value) =&gt; Value = value;
    ///     public Pet(Dog value) =&gt; Value = value;
    ///     public object? Value { get; }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IUnion" />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class UnionAttribute : Attribute
    {
    }
}
