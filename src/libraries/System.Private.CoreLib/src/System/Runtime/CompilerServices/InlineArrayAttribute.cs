// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that the instance's storage is sequentially replicated "length" times.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute can be used to annotate a <see langword="struct"/> type with a single field.
    /// The runtime will replicate that field as many times as specified in the actual type layout.
    /// </para>
    /// <para>
    /// Here's an example of how an inline array type with 8 <see cref="float"/> values can be declared:
    /// <code lang="csharp">
    /// [InlineArray(8)]
    /// struct Float8InlineArray
    /// {
    ///     private float _value;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// The compiler will also automatically generate an indexer property, and support casting the type
    /// to <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/> for safe enumeration of all elements.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        /// <summary>Creates a new <see cref="InlineArrayAttribute"/> instance with the specified parameters.</summary>
        /// <param name="length">The number of sequential fields to replicate in the inline array type.</param>
        public InlineArrayAttribute(int length)
        {
            Length = length;
        }

        /// <summary>Gets the number of sequential fields to replicate in the inline array type.</summary>
        public int Length { get; }
    }
}
