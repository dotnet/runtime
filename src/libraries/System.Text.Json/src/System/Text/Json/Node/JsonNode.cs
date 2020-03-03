// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json
{
    /// <summary>
    ///   The base class that represents a single node within a mutable JSON document.
    /// </summary>
    public abstract partial class JsonNode
    {
        private protected JsonNode() { }

        /// <summary>
        ///   Transforms this instance into <see cref="JsonElement"/> representation.
        ///   Operations performed on this instance will modify the returned <see cref="JsonElement"/>.
        /// </summary>
        /// <returns>Mutable <see cref="JsonElement"/> with <see cref="JsonNode"/> underneath.</returns>
        public JsonElement AsJsonElement() => new JsonElement(this);

        /// <summary>
        ///   The <see cref="JsonValueKind"/> that the node is.
        /// </summary>
        public abstract JsonValueKind ValueKind { get; }

        /// <summary>
        ///   Gets the <see cref="JsonNode"/> represented by <paramref name="jsonElement"/>.
        ///   Operations performed on the returned <see cref="JsonNode"/> will modify the <paramref name="jsonElement"/>.
        ///   See also: <seealso cref="JsonElement.IsImmutable"/>.
        /// </summary>
        /// <param name="jsonElement"><see cref="JsonElement"/> to get the <see cref="JsonNode"/> from.</param>
        /// <returns><see cref="JsonNode"/> represented by <paramref name="jsonElement"/>.</returns>
        /// <exception cref="ArgumentException">
        ///   Provided <see cref="JsonElement"/> was not built from <see cref="JsonNode"/>.
        /// </exception>
        public static JsonNode GetNode(JsonElement jsonElement)
        {
            if (jsonElement.IsImmutable)
            {
                throw new ArgumentException(SR.NotNodeJsonElementParent);
            }
            Debug.Assert(jsonElement._parent != null);
            return (JsonNode)jsonElement._parent;
        }

        /// <summary>
        ///    Gets the <see cref="JsonNode"/> represented by the <paramref name="jsonElement"/>.
        ///    Operations performed on the returned <see cref="JsonNode"/> will modify the <paramref name="jsonElement"/>.
        ///    A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="jsonElement"><see cref="JsonElement"/> to get the <see cref="JsonNode"/> from.</param>
        /// <param name="jsonNode"><see cref="JsonNode"/> represented by <paramref name="jsonElement"/>.</param>
        /// <returns>
        ///  <see langword="true"/> if the operation succeded;
        ///  otherwise, <see langword="false"/>
        /// </returns>
        public static bool TryGetNode(JsonElement jsonElement, [NotNullWhen(true)] out JsonNode? jsonNode)
        {
            if (!jsonElement.IsImmutable)
            {
                Debug.Assert(jsonElement._parent != null);
                jsonNode = (JsonNode)jsonElement._parent;
                return true;
            }

            jsonNode = null;
            return false;
        }

        /// <summary>
        ///   Performs a deep copy operation on this instance.
        /// </summary>
        /// <returns><see cref="JsonNode"/> which is a clone of this instance.</returns>
        public abstract JsonNode Clone();

        /// <summary>
        ///   Converts a <see cref="string"/> to a <see cref="JsonString"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <remarks>
        ///   Null value is accepted and will be interpreted as <see cref="JsonNull"/>.
        /// </remarks>
        public static implicit operator JsonNode(string? value)
        {
            if (value == null)
            {
                return JsonNull.Instance;
            }

            return new JsonString(value);
        }

        /// <summary>
        ///   Converts a <see cref="DateTime"/> to a <see cref="JsonString"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(DateTime value) => new JsonString(value);

        /// <summary>
        ///   Converts a <see cref="DateTimeOffset"/> to a <see cref="JsonString"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(DateTimeOffset value) => new JsonString(value);

        /// <summary>
        ///   Converts a <see cref="Guid"/> to a <see cref="JsonString"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(Guid value) => new JsonString(value);

        /// <summary>
        ///   Converts a <see cref="bool"/> to a <see cref="JsonBoolean"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(bool value) => new JsonBoolean(value);

        /// <summary>
        ///   Converts a <see cref="byte"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(byte value) => new JsonNumber(value);

        /// <summary>
        ///   Converts a <see cref="short"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(short value) => new JsonNumber(value);

        /// <summary>
        ///   Converts an <see cref="int"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(int value) => new JsonNumber(value);

        /// <summary>
        ///   Converts a <see cref="long"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(long value) => new JsonNumber(value);

        /// <summary>
        ///    Converts a <see cref="float"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(float value)
        {
            if (!JsonHelpers.IsFinite(value))
            {
                return new JsonString(value.ToString());
            }

            return new JsonNumber(value);
        }

        /// <summary>
        ///    Converts a <see cref="double"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(double value)
        {
            if (!JsonHelpers.IsFinite(value))
            {
                return new JsonString(value.ToString());
            }

            return new JsonNumber(value);
        }

        /// <summary>
        ///    Converts a <see cref="sbyte"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        [CLSCompliant(false)]
        public static implicit operator JsonNode(sbyte value) => new JsonNumber(value);

        /// <summary>
        ///    Converts a <see cref="ushort"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        [CLSCompliant(false)]
        public static implicit operator JsonNode(ushort value) => new JsonNumber(value);

        /// <summary>
        ///    Converts a <see cref="uint"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        [CLSCompliant(false)]
        public static implicit operator JsonNode(uint value) => new JsonNumber(value);

        /// <summary>
        ///    Converts a <see cref="ulong"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        [CLSCompliant(false)]
        public static implicit operator JsonNode(ulong value) => new JsonNumber(value);

        /// <summary>
        ///    Converts a <see cref="decimal"/> to a JSON number.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator JsonNode(decimal value) => new JsonNumber(value);
    }
}
