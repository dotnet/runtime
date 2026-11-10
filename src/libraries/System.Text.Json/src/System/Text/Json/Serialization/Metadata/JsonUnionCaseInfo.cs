// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents metadata for a single case type within a union type.
    /// </summary>
    public sealed class JsonUnionCaseInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonUnionCaseInfo"/> with the specified case type.
        /// </summary>
        /// <param name="caseType">The type representing this union case.</param>
        /// <exception cref="ArgumentNullException"><paramref name="caseType"/> is <see langword="null"/>.</exception>
        public JsonUnionCaseInfo(Type caseType)
        {
            ArgumentNullException.ThrowIfNull(caseType);
            CaseType = caseType;
        }

        /// <summary>
        /// Gets the type representing this union case.
        /// </summary>
        public Type CaseType { get; }

        /// <summary>
        /// Gets a value indicating whether this case can hold a <see langword="null"/> value.
        /// </summary>
        /// <remarks>
        /// Set to <see langword="true"/> when the union constructor parameter for this case
        /// was declared as a nullable reference type or as <see cref="Nullable{T}"/>.
        /// When a JSON <see langword="null"/> is encountered during deserialization,
        /// the first union case whose <see cref="IsNullable"/> is <see langword="true"/>
        /// is selected and its case constructor is invoked with <see langword="null"/>.
        /// </remarks>
        public bool IsNullable { get; init; }
    }
}
