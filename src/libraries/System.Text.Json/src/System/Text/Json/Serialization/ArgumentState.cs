// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

using FoundProperties = System.ValueTuple<System.Text.Json.Serialization.Metadata.JsonPropertyInfo, System.Text.Json.JsonReaderState, long, byte[]?, string?>;
using FoundPropertiesAsync = System.ValueTuple<System.Text.Json.Serialization.Metadata.JsonPropertyInfo, object?, string?>;

namespace System.Text.Json
{
    /// <summary>
    /// Holds relevant state when deserializing objects with parameterized constructors.
    /// Lives on the current ReadStackFrame.
    /// </summary>
    internal sealed class ArgumentState
    {
        // Cache for parsed constructor arguments.
        public object Arguments = null!;

        // When deserializing objects with parameterized ctors, the properties we find on the first pass.
        public FoundProperties[]? FoundProperties;

        // When deserializing objects with parameterized ctors asynchronously, the properties we find on the first pass.
        public FoundPropertiesAsync[]? FoundPropertiesAsync;
        public int FoundPropertyCount;

        // Current constructor parameter value.
        public JsonParameterInfo? JsonParameterInfo;

        // For performance, we order the parameters by the first deserialize and PropertyIndex helps find the right slot quicker.
        public int ParameterIndex;
        public List<ParameterRef>? ParameterRefCache;

        // Used when deserializing KeyValuePair instances.
        public bool FoundKey;
        public bool FoundValue;
    }
}
