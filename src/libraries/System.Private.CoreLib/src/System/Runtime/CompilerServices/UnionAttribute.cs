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
    /// Union types support the following behaviors:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// Implicit conversions from each case type to the union type (union conversions).
    /// </description></item>
    /// <item><description>
    /// Pattern matching that automatically unwraps the union's contents, applying patterns to
    /// the underlying value rather than to the union wrapper itself (union matching).
    /// </description></item>
    /// <item><description>
    /// Switch exhaustiveness checking that considers a switch complete when all case types
    /// have been matched, without requiring a fallback case (union exhaustiveness).
    /// </description></item>
    /// </list>
    /// <para>
    /// A union type must follow the basic union pattern: it must have at least one single-parameter
    /// public constructor (or static <c>Create</c> factory method when using a union member provider),
    /// and a public <c>Value</c> property of type <see cref="object" />. The parameter types of these
    /// constructors or factory methods determine the union's case types. Implementing
    /// <see cref="IUnion" /> is optional but recommended, as it provides runtime access to
    /// the union's value and is automatically implemented by compiler-generated union declarations.
    /// </para>
    /// <para>
    /// The C# <c>union</c> declaration syntax automatically generates a struct annotated with this
    /// attribute and implementing <see cref="IUnion" />.
    /// </para>
    /// <para>
    /// For more information, see the
    /// <see href="https://github.com/dotnet/csharplang/blob/main/proposals/unions.md">C# unions proposal</see>.
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
    public class UnionAttribute : Attribute
    {
    }
}
