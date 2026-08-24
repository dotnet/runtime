// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// A delegate that classifies a JSON payload to determine the concrete type it corresponds to.
    /// Used by both union types and polymorphic types with custom type classifiers.
    /// </summary>
    /// <param name="reader">A defensive copy of the reader positioned at the start of the JSON value.
    /// The classifier is free to advance this reader for lookahead; the original reader is not affected.</param>
    /// <returns>
    /// The resolved <see cref="Type"/>. Returning <see langword="null"/> indicates a classification
    /// failure and causes a <see cref="System.Text.Json.JsonException"/> to be thrown.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For union types, the classifier is <b>not invoked</b> when the payload is a JSON
    /// <see cref="JsonTokenType.Null"/> token. Per the union semantics, every nullable case
    /// produces the same canonical null-holding union value, so the case-type choice is
    /// irrelevant for null payloads. The converter dispatches null directly to the union's
    /// constructor delegate, which yields the canonical null union when at least one case
    /// is nullable, and otherwise throws a <see cref="System.Text.Json.JsonException"/>.
    /// Classifier authors therefore do not need to handle the <see cref="JsonTokenType.Null"/> token.
    /// </para>
    /// </remarks>
    public delegate Type? JsonTypeClassifier(ref Utf8JsonReader reader);
}
