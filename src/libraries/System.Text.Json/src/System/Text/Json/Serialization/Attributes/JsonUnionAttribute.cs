// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Customizes the System.Text.Json behavior of a union type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A type is recognized by System.Text.Json as a union when it carries
    /// <c>System.Runtime.CompilerServices.UnionAttribute</c> (the marker emitted by
    /// the C# compiler for union declarations). Applying
    /// <see cref="JsonUnionAttribute"/> on its own does not make a type a union — the
    /// attribute is silently ignored when no union marker is present, and the
    /// type is serialized as a regular object.
    /// </para>
    /// <para>
    /// On a recognized union, this attribute is purely a customization knob — currently
    /// it lets the user plug in a per-type <see cref="TypeClassifier"/>. Recognized unions
    /// without this attribute receive the default JSON-value-based classifier.
    /// </para>
    /// <para>
    /// Case types are discovered automatically from the union's public single-parameter
    /// constructors. For types where convention-based discovery does not work, use
    /// contract customization to populate <see cref="Metadata.JsonTypeInfo.UnionCases"/>
    /// and set the deconstructor/constructor delegates on
    /// <see cref="Metadata.JsonTypeInfo{T}"/>.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class JsonUnionAttribute : JsonAttribute
    {
        /// <summary>
        /// Gets or sets the type of a <see cref="JsonTypeClassifierFactory"/> implementation
        /// used to classify JSON payloads during deserialization.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <see langword="null"/>, the built-in JSON value-shape classifier is used.
        /// It dispatches using the current JSON token kind rather than by inspecting object
        /// property names or other payload contents.
        /// </para>
        /// <para>
        /// The specified type must derive from <see cref="JsonTypeClassifierFactory"/>
        /// and have a public parameterless constructor.
        /// </para>
        /// <para>
        /// The classifier is not invoked for JSON <see cref="JsonTokenType.Null"/>
        /// payloads. The null token is dispatched directly to the union's constructor delegate,
        /// which yields the canonical null union when at least one case is nullable
        /// and otherwise throws a <see cref="JsonException"/>. See
        /// <see cref="JsonTypeClassifier"/> for details.
        /// </para>
        /// </remarks>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type? TypeClassifier { get; set; }
    }
}
