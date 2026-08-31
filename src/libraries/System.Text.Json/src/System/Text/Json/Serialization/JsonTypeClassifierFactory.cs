// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When implemented in a derived class, creates a <see cref="JsonTypeClassifier"/>
    /// delegate that classifies JSON payloads to determine the target type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This factory is the extension point for users who want to customize how JSON payloads
    /// are matched to union case types or polymorphic derived types during deserialization.
    /// It is referenced by the <see cref="JsonUnionAttribute.TypeClassifier"/> and
    /// <see cref="JsonPolymorphicAttribute.TypeClassifier"/> properties.
    /// </para>
    /// <para>
    /// The factory's <see cref="CreateJsonClassifier"/> method is called once during
    /// <see cref="JsonTypeInfo"/> configuration, and the returned delegate is invoked
    /// on every deserialization call.
    /// </para>
    /// <para>
    /// A single factory may produce type classifiers for multiple declaring types — analogous to
    /// <see cref="JsonConverterFactory"/>. Implementations report supported metadata shapes via
    /// <see cref="CanClassify"/>. For the common case of a factory pinned to a single declaring
    /// type, derive from <see cref="JsonTypeClassifierFactory{T}"/> instead.
    /// </para>
    /// <para>
    /// Factories may be registered globally on <see cref="JsonSerializerOptions.TypeClassifiers"/>
    /// or per-type via <see cref="JsonUnionAttribute.TypeClassifier"/> /
    /// <see cref="JsonPolymorphicAttribute.TypeClassifier"/>. When both are present the attribute wins.
    /// </para>
    /// <para>
    /// For union types, the produced classifier delegate is <b>not</b> invoked when the
    /// payload is a JSON <see cref="JsonTokenType.Null"/> token. The converter handles
    /// null payloads directly: it produces the canonical null union value when at least
    /// one case is nullable, and otherwise throws a <see cref="JsonException"/>. See
    /// <see cref="JsonTypeClassifier"/> for details.
    /// </para>
    /// </remarks>
    public abstract class JsonTypeClassifierFactory
    {
        /// <summary>
        /// When overridden, indicates whether this factory can produce a classifier for
        /// the specified metadata context.
        /// </summary>
        /// <param name="context">The metadata context for the type being configured.</param>
        /// <returns>
        /// <see langword="true"/> if this factory can produce a classifier for
        /// <paramref name="context"/>; otherwise <see langword="false"/>.
        /// </returns>
        public abstract bool CanClassify(JsonTypeClassifierContext context);

        /// <summary>
        /// Creates a delegate that classifies JSON payloads to determine the target type.
        /// </summary>
        /// <param name="context">
        /// An immutable snapshot of metadata including the declaring type, candidate types
        /// (with optional discriminator values), and the discriminator property name.
        /// </param>
        /// <param name="options">The serializer options for the current context.</param>
        /// <returns>A <see cref="JsonTypeClassifier"/> delegate.</returns>
        public abstract JsonTypeClassifier CreateJsonClassifier(
            JsonTypeClassifierContext context,
            JsonSerializerOptions options);
    }

    /// <summary>
    /// Convenience base class for a <see cref="JsonTypeClassifierFactory"/> that produces
    /// type classifiers for a single declaring type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The declaring type that this factory classifies.</typeparam>
    /// <remarks>
    /// Seals <see cref="CanClassify"/> to compare the context declaring type against
    /// <typeparamref name="T"/> using reference equality. Subclasses only need to implement
    /// <see cref="JsonTypeClassifierFactory.CreateJsonClassifier"/>.
    /// </remarks>
    public abstract class JsonTypeClassifierFactory<T> : JsonTypeClassifierFactory
    {
        /// <inheritdoc/>
        public sealed override bool CanClassify(JsonTypeClassifierContext context) => context.DeclaringType == typeof(T);
    }
}
