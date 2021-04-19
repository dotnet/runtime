// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Node
{
    /// <summary>
    /// Represents a mutable JSON value.
    /// </summary>
    public abstract partial class JsonValue : JsonNode
    {
        private protected JsonValue(JsonNodeOptions? options = null) : base(options) { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <returns>
        ///   The new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </returns>
        /// <typeparam name="T">The type of value to be added.</typeparam>
        /// <param name="value">The value to add.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create<T>(T? value, JsonNodeOptions? options = null)
        {
            if (value == null)
            {
                return null;
            }

            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Null)
                {
                    return null;
                }

                if (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)
                {
                    throw new InvalidOperationException(SR.NodeElementCannotBeObjectOrArray);
                }
            }

            return new JsonValue<T>(value, options);
        }

        internal override void GetPath(List<string> path, JsonNode? child)
        {
            Debug.Assert(child == null);

            if (Parent != null)
            {
                Parent.GetPath(path, this);
            }
        }

        /// <summary>
        ///   Tries to obtain the current JSON value and returns a value that indicates whether the operation succeeded.
        /// </summary>
        /// <typeparam name="T">The type of value to obtain.</typeparam>
        /// <param name="value">When this method returns, contains the parsed value.</param>
        /// <returns><see langword="true"/> if the value can be successfully obtained; otherwise, <see langword="false"/>.</returns>
        public abstract bool TryGetValue<[DynamicallyAccessedMembers(JsonHelpers.MembersAccessedOnRead)]T>([NotNullWhen(true)] out T? value);
    }
}
