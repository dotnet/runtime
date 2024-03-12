// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes
{
    /// <summary>
    ///   The base class that represents a single node within a mutable JSON document.
    /// </summary>
    /// <seealso cref="JsonSerializerOptions.UnknownTypeHandling"/> to specify that a type
    /// declared as an <see cref="object"/> should be deserialized as a <see cref="JsonNode"/>.
    public abstract partial class JsonNode
    {
        private JsonNode? _parent;
        private JsonNodeOptions? _options;

        /// <summary>
        ///   Options to control the behavior.
        /// </summary>
        public JsonNodeOptions? Options
        {
            get
            {
                if (!_options.HasValue && Parent != null)
                {
                    // Remember the parent options; if node is re-parented later we still want to keep the
                    // original options since they may have affected the way the node was created as is the case
                    // with JsonObject's case-insensitive dictionary.
                    _options = Parent.Options;
                }

                return _options;
            }
        }

        internal JsonNode(JsonNodeOptions? options = null)
        {
            _options = options;
        }

        /// <summary>
        ///   Casts to the derived <see cref="JsonArray"/> type.
        /// </summary>
        /// <returns>
        ///   A <see cref="JsonArray"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   The node is not a <see cref="JsonArray"/>.
        /// </exception>
        public JsonArray AsArray()
        {
            JsonArray? jArray = this as JsonArray;

            if (jArray is null)
            {
                ThrowHelper.ThrowInvalidOperationException_NodeWrongType(nameof(JsonArray));
            }

            return jArray;
        }

        /// <summary>
        ///   Casts to the derived <see cref="JsonObject"/> type.
        /// </summary>
        /// <returns>
        ///   A <see cref="JsonObject"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   The node is not a <see cref="JsonObject"/>.
        /// </exception>
        public JsonObject AsObject()
        {
            JsonObject? jObject = this as JsonObject;

            if (jObject is null)
            {
                ThrowHelper.ThrowInvalidOperationException_NodeWrongType(nameof(JsonObject));
            }

            return jObject;
        }

        /// <summary>
        ///   Casts to the derived <see cref="JsonValue"/> type.
        /// </summary>
        /// <returns>
        ///   A <see cref="JsonValue"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   The node is not a <see cref="JsonValue"/>.
        /// </exception>
        public JsonValue AsValue()
        {
            JsonValue? jValue = this as JsonValue;

            if (jValue is null)
            {
                ThrowHelper.ThrowInvalidOperationException_NodeWrongType(nameof(JsonValue));
            }

            return jValue;
        }

        /// <summary>
        ///   Gets the parent <see cref="JsonNode"/>.
        ///   If there is no parent, <see langword="null"/> is returned.
        ///   A parent can either be a <see cref="JsonObject"/> or a <see cref="JsonArray"/>.
        /// </summary>
        public JsonNode? Parent
        {
            get
            {
                return _parent;
            }
            internal set
            {
                _parent = value;
            }
        }

        /// <summary>
        ///   Gets the JSON path.
        /// </summary>
        /// <returns>The JSON Path value.</returns>
        public string GetPath()
        {
            if (Parent == null)
            {
                return "$";
            }

            var path = new ValueStringBuilder(stackalloc char[JsonConstants.StackallocCharThreshold]);
            path.Append('$');
            GetPath(ref path, null);
            return path.ToString();
        }

        internal abstract void GetPath(ref ValueStringBuilder path, JsonNode? child);

        /// <summary>
        ///   Gets the root <see cref="JsonNode"/>.
        /// </summary>
        /// <remarks>
        ///   The current node is returned if it is a root.
        /// </remarks>
        public JsonNode Root
        {
            get
            {
                JsonNode? parent = Parent;
                if (parent == null)
                {
                    return this;
                }

                while (parent.Parent != null)
                {
                    parent = parent.Parent;
                }

                return parent;
            }
        }

        /// <summary>
        ///   Gets the value for the current <see cref="JsonValue"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value to obtain from the <see cref="JsonValue"/>.</typeparam>
        /// <returns>A value converted from the <see cref="JsonValue"/> instance.</returns>
        /// <remarks>
        ///   {T} can be the type or base type of the underlying value.
        ///   If the underlying value is a <see cref="JsonElement"/> then {T} can also be the type of any primitive
        ///   value supported by current <see cref="JsonElement"/>.
        ///   Specifying the <see cref="object"/> type for {T} will always succeed and return the underlying value as <see cref="object"/>.<br />
        ///   The underlying value of a <see cref="JsonValue"/> after deserialization is an instance of <see cref="JsonElement"/>,
        ///   otherwise it's the value specified when the <see cref="JsonValue"/> was created.
        /// </remarks>
        /// <seealso cref="System.Text.Json.Nodes.JsonValue.TryGetValue"></seealso>
        /// <exception cref="FormatException">
        ///   The current <see cref="JsonNode"/> cannot be represented as a {T}.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The current <see cref="JsonNode"/> is not a <see cref="JsonValue"/> or
        ///   is not compatible with {T}.
        /// </exception>
        public virtual T GetValue<T>() =>
            throw new InvalidOperationException(SR.Format(SR.NodeWrongType, nameof(JsonValue)));

        /// <summary>
        ///   Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is less than 0 or <paramref name="index"/> is greater than the number of properties.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The current <see cref="JsonNode"/> is not a <see cref="JsonArray"/>.
        /// </exception>
        public JsonNode? this[int index]
        {
            get
            {
                return AsArray().GetItem(index);
            }
            set
            {
                AsArray().SetItem(index, value);
            }
        }

        /// <summary>
        ///   Gets or sets the element with the specified property name.
        ///   If the property is not found, <see langword="null"/> is returned.
        /// </summary>
        /// <param name="propertyName">The name of the property to return.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The current <see cref="JsonNode"/> is not a <see cref="JsonObject"/>.
        /// </exception>
        public JsonNode? this[string propertyName]
        {
            get
            {
                return AsObject().GetItem(propertyName);
            }
            set
            {
                AsObject().SetItem(propertyName, value);
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="JsonNode"/>. All children nodes are recursively cloned.
        /// </summary>
        /// <returns>A new cloned instance of the current node.</returns>
        public JsonNode DeepClone() => DeepCloneCore();

        internal abstract JsonNode DeepCloneCore();

        /// <summary>
        /// Returns <see cref="JsonValueKind"/> of current instance.
        /// </summary>
        public JsonValueKind GetValueKind() => GetValueKindCore();

        internal abstract JsonValueKind GetValueKindCore();

        /// <summary>
        /// Returns property name of the current node from the parent object.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The current parent is not a <see cref="JsonObject"/>.
        /// </exception>
        public string GetPropertyName()
        {
            JsonObject? parentObject = _parent as JsonObject;

            if (parentObject is null)
            {
                ThrowHelper.ThrowInvalidOperationException_NodeParentWrongType(nameof(JsonObject));
            }

            return parentObject.GetPropertyName(this);
        }

        /// <summary>
        /// Returns index of the current node from the parent <see cref="JsonArray" />.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The current parent is not a <see cref="JsonArray"/>.
        /// </exception>
        public int GetElementIndex()
        {
            JsonArray? parentArray = _parent as JsonArray;

            if (parentArray is null)
            {
                ThrowHelper.ThrowInvalidOperationException_NodeParentWrongType(nameof(JsonArray));
            }

            return parentArray.GetElementIndex(this);
        }

        /// <summary>
        /// Compares the values of two nodes, including the values of all descendant nodes.
        /// </summary>
        /// <param name="node1">The <see cref="JsonNode"/> to compare.</param>
        /// <param name="node2">The <see cref="JsonNode"/> to compare.</param>
        /// <returns><c>true</c> if the tokens are equal; otherwise <c>false</c>.</returns>
        public static bool DeepEquals(JsonNode? node1, JsonNode? node2)
        {
            if (node1 is null)
            {
                return node2 is null;
            }

            return node1.DeepEqualsCore(node2);
        }

        internal abstract bool DeepEqualsCore(JsonNode? node);

        /// <summary>
        /// Replaces this node with a new value.
        /// </summary>
        /// <typeparam name="T">The type of value to be replaced.</typeparam>
        /// <param name="value">Value that replaces this node.</param>
        [RequiresUnreferencedCode(JsonValue.CreateUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonValue.CreateDynamicCodeMessage)]
        public void ReplaceWith<T>(T value)
        {
            JsonNode? node;
            switch (_parent)
            {
                case JsonObject jsonObject:
                    node = ConvertFromValue(value);
                    jsonObject.SetItem(GetPropertyName(), node);
                    return;
                case JsonArray jsonArray:
                    node = ConvertFromValue(value);
                    jsonArray.SetItem(GetElementIndex(), node);
                    return;
            }
        }

        internal void AssignParent(JsonNode parent)
        {
            if (Parent != null)
            {
                ThrowHelper.ThrowInvalidOperationException_NodeAlreadyHasParent();
            }

            JsonNode? p = parent;
            while (p != null)
            {
                if (p == this)
                {
                    ThrowHelper.ThrowInvalidOperationException_NodeCycleDetected();
                }

                p = p.Parent;
            }

            Parent = parent;
        }

        /// <summary>
        /// Adaptation of the equivalent JsonValue.Create factory method extended
        /// to support arbitrary <see cref="JsonElement"/> and <see cref="JsonNode"/> values.
        /// TODO consider making public cf. https://github.com/dotnet/runtime/issues/70427
        /// </summary>
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal static JsonNode? ConvertFromValue<T>(T? value, JsonNodeOptions? options = null)
        {
            if (value is null)
            {
                return null;
            }

            if (value is JsonNode node)
            {
                return node;
            }

            if (value is JsonElement element)
            {
                return JsonNodeConverter.Create(element, options);
            }

            var jsonTypeInfo = (JsonTypeInfo<T>)JsonSerializerOptions.Default.GetTypeInfo(typeof(T));
            return new JsonValueCustomized<T>(value, jsonTypeInfo, options);
        }
    }
}
