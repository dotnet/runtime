// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    ///   The base class that represents a single node within a mutable JSON document.
    /// </summary>
    public abstract class JsonNode
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
        /// </summary>
        /// <param name="jsonElement"><see cref="JsonElement"/> to get the <see cref="JsonNode"/> from.</param>
        /// <returns><see cref="JsonNode"/> represented by <paramref name="jsonElement"/>.</returns>
        public static JsonNode GetNode(JsonElement jsonElement) => (JsonNode)jsonElement._parent;

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
        public static bool TryGetNode(JsonElement jsonElement, out JsonNode jsonNode)
        {
            if (!jsonElement.IsImmutable)
            {
                jsonNode = (JsonNode)jsonElement._parent;
                return true;
            }

            jsonNode = null;
            return false;
        }

        /// <summary>
        ///   Parses a string representiong JSON document into <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="json">JSON to parse.</param>
        /// <returns><see cref="JsonNode"/> representation of <paramref name="json"/>.</returns>
        public static JsonNode Parse(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                return DeepCopy(document.RootElement);
            }
        }

        /// <summary>
        ///   Performs a deep copy operation on this instance.
        /// </summary>
        /// <returns><see cref="JsonNode"/> which is a clone of this instance.</returns>
        public abstract JsonNode Clone();

        /// <summary>
        ///   Performs a deep copy operation on <paramref name="jsonElement"/> returning corresponding modifiable tree structure of JSON nodes.
        ///   Operations performed on returned <see cref="JsonNode"/> does not modify <paramref name="jsonElement"/>.
        /// </summary>
        /// <param name="jsonElement"><see cref="JsonElement"/> to copy.</param>
        /// <returns><see cref="JsonNode"/>  representing <paramref name="jsonElement"/>.</returns>
        public static JsonNode DeepCopy(JsonElement jsonElement)
        {
            if (!jsonElement.IsImmutable)
            {
                return GetNode(jsonElement).Clone();
            }

            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Object:
                    JsonObject jsonObject = new JsonObject();
                    foreach (JsonProperty property in jsonElement.EnumerateObject())
                    {
                        jsonObject.Add(property.Name, DeepCopy(property.Value));
                    }
                    return jsonObject;
                case JsonValueKind.Array:
                    JsonArray jsonArray = new JsonArray();
                    foreach (JsonElement element in jsonElement.EnumerateArray())
                    {
                        jsonArray.Add(DeepCopy(element));
                    }
                    return jsonArray;
                case JsonValueKind.Number:
                    return new JsonNumber(jsonElement.GetRawText());
                case JsonValueKind.String:
                    return new JsonString(jsonElement.GetString());
                case JsonValueKind.True:
                    return new JsonBoolean(true);
                case JsonValueKind.False:
                    return new JsonBoolean(false);
                case JsonValueKind.Null:
                    return null;
                default:
                    Debug.Assert(jsonElement.ValueKind == JsonValueKind.Undefined, "No handler for JsonValueKind.{jsonElement.ValueKind}");
                    throw ThrowHelper.GetJsonElementWrongTypeException(JsonValueKind.Undefined, jsonElement.ValueKind);
            }
        }
    }
}
