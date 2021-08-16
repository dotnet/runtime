// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

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
            if (this is JsonArray jArray)
            {
                return jArray;
            }

            throw new InvalidOperationException(SR.Format(SR.NodeWrongType, nameof(JsonArray)));
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
            if (this is JsonObject jObject)
            {
                return jObject;
            }

            throw new InvalidOperationException(SR.Format(SR.NodeWrongType, nameof(JsonObject)));
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
            if (this is JsonValue jValue)
            {
                return jValue;
            }

            throw new InvalidOperationException(SR.Format(SR.NodeWrongType, nameof(JsonValue)));
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

            var path = new List<string>();
            GetPath(path, null);

            var sb = new StringBuilder("$");
            for (int i = path.Count - 1; i >= 0; i--)
            {
                sb.Append(path[i]);
            }

            return sb.ToString();
        }

        internal abstract void GetPath(List<string> path, JsonNode? child);

        /// <summary>
        ///   Gets the root <see cref="JsonNode"/>.
        ///   If the current <see cref="JsonNode"/> is a root, <see langword="null"/> is returned.
        /// </summary>
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
    }
}
