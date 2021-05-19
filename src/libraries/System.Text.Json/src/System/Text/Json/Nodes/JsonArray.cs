// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Nodes
{
    /// <summary>
    ///   Represents a mutable JSON array.
    /// </summary>
    /// <remarks>
    /// It is safe to perform multiple concurrent read operations on a <see cref="JsonArray"/>,
    /// but issues can occur if the collection is modified while it's being read.
    /// </remarks>
    [DebuggerDisplay("JsonArray[{List.Count}]")]
    [DebuggerTypeProxy(typeof(DebugView))]
    public sealed partial class JsonArray : JsonNode
    {
        private JsonElement? _jsonElement;
        private List<JsonNode?>? _list;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class that is empty.
        /// </summary>
        /// <param name="options">Options to control the behavior.</param>
        public JsonArray(JsonNodeOptions? options = null) : base(options) { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class that contains items from the specified params array.
        /// </summary>
        /// <param name="options">Options to control the behavior.</param>
        /// <param name="items">The items to add to the new <see cref="JsonArray"/>.</param>
        public JsonArray(JsonNodeOptions options, params JsonNode?[] items) : base(options)
        {
            InitializeFromArray(items);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class that contains items from the specified array.
        /// </summary>
        /// <param name="items">The items to add to the new <see cref="JsonArray"/>.</param>
        public JsonArray(params JsonNode?[] items) : base()
        {
            InitializeFromArray(items);
        }

        private void InitializeFromArray(JsonNode?[] items)
        {
            var list = new List<JsonNode?>(items);

            for (int i = 0; i < items.Length; i++)
            {
                items[i]?.AssignParent(this);
            }

            _list = list;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class that contains items from the specified <see cref="JsonElement"/>.
        /// </summary>
        /// <returns>
        ///   The new instance of the <see cref="JsonArray"/> class that contains items from the specified <see cref="JsonElement"/>.
        /// </returns>
        /// <param name="element">The <see cref="JsonElement"/>.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <exception cref="InvalidOperationException">
        ///   The <paramref name="element"/> is not a <see cref="JsonValueKind.Array"/>.
        /// </exception>
        public static JsonArray? Create(JsonElement element, JsonNodeOptions? options = null)
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                return new JsonArray(element, options);
            }

            throw new InvalidOperationException(SR.Format(SR.NodeElementWrongType, nameof(JsonValueKind.Array)));
        }

        internal JsonArray (JsonElement element, JsonNodeOptions? options = null) : base(options)
        {
            Debug.Assert(element.ValueKind == JsonValueKind.Array);
            _jsonElement = element;
        }

        /// <summary>
        ///   Adds an object to the end of the <see cref="JsonArray"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to be added.</typeparam>
        /// <param name="value">
        ///   The object to be added to the end of the <see cref="JsonArray"/>.
        /// </param>
        public void Add<T>(T? value)
        {
            if (value == null)
            {
                Add(null);
            }
            else
            {
                JsonNode? jNode = value as JsonNode;
                if (jNode == null)
                {
                    jNode = new JsonValue<T>(value);
                }

                // Call the IList.Add() implementation.
                Add(jNode);
            }
        }

        internal List<JsonNode?> List
        {
            get
            {
                CreateNodes();
                Debug.Assert(_list != null);
                return _list;
            }
        }

        internal JsonNode? GetItem(int index)
        {
            return List[index];
        }

        internal void SetItem(int index, JsonNode? value)
        {
            value?.AssignParent(this);
            DetachParent(List[index]);
            List[index] = value;
        }

        internal override void GetPath(List<string> path, JsonNode? child)
        {
            if (child != null)
            {
                int index = List.IndexOf(child);
                path.Add($"[{index}]");
            }

            Parent?.GetPath(path, this);
        }

        /// <inheritdoc/>
        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_jsonElement.HasValue)
            {
                _jsonElement.Value.WriteTo(writer);
            }
            else
            {
                CreateNodes();
                Debug.Assert(_list != null);

                options ??= JsonSerializerOptions.s_defaultOptions;

                writer.WriteStartArray();

                for (int i = 0; i < _list.Count; i++)
                {
                    JsonNodeConverter.Instance.Write(writer, _list[i]!, options);
                }

                writer.WriteEndArray();
            }
        }

        private void CreateNodes()
        {
            if (_list == null)
            {
                List<JsonNode?> list;

                if (_jsonElement == null)
                {
                    list = new List<JsonNode?>();
                }
                else
                {
                    JsonElement jElement = _jsonElement.Value;
                    Debug.Assert(jElement.ValueKind == JsonValueKind.Array);

                    list = new List<JsonNode?>(jElement.GetArrayLength());

                    foreach (JsonElement element in jElement.EnumerateArray())
                    {
                        JsonNode? node = JsonNodeConverter.Create(element, Options);
                        node?.AssignParent(this);
                        list.Add(node);
                    }

                    // Clear since no longer needed.
                    _jsonElement = null;
                }

                _list = list;
            }
        }

        [ExcludeFromCodeCoverage] // Justification = "Design-time"
        private class DebugView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private JsonArray _node;

            public DebugView(JsonArray node)
            {
                _node = node;
            }

            public string Json => _node.ToJsonString();
            public string Path => _node.GetPath();

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            private DebugViewItem[] Items
            {
                get
                {
                    DebugViewItem[] properties = new DebugViewItem[_node.List.Count];

                    for (int i = 0; i < _node.List.Count; i++)
                    {
                        properties[i].Value = _node.List[i];
                    }

                    return properties;
                }
            }

            [DebuggerDisplay("{Display,nq}")]
            private struct DebugViewItem
            {
                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public JsonNode? Value;

                [DebuggerBrowsable(DebuggerBrowsableState.Never)]
                public string Display
                {
                    get
                    {
                        if (Value == null)
                        {
                            return $"null";
                        }

                        if (Value is JsonValue)
                        {
                            return Value.ToJsonString();
                        }

                        if (Value is JsonObject jsonObject)
                        {
                            return $"JsonObject[{jsonObject.Count}]";
                        }

                        JsonArray jsonArray = (JsonArray)Value;
                        return $"JsonArray[{jsonArray.List.Count}]";
                    }
                }
            }
        }
    }
}
