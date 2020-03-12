// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json
{
    /// <summary>
    /// // Holds relevant state when deserializing objects with parameterized constructors.
    /// Lives on the current ReadStackFrame.
    /// </summary>
    internal struct ConstructorArgumentState
    {
        // Cache for parsed constructor arguments.
        public object? Arguments;

        // Data extension for objects with parameterized ctors and an extension data property.
        public object? DataExtension;

        // When deserializing objects with parameterized ctors, the properties we find on the first pass.
        public ValueTuple<JsonPropertyInfo, JsonReaderState, long, byte[]?>[]? FoundProperties;

        // When deserializing objects with parameterized ctors asynchronously, the properties we find on the first pass.
        public ValueTuple<JsonPropertyInfo, JsonReaderState, byte[], byte[]?>[]? FoundPropertiesAsync;

        // When deserializing objects with parameterized ctors, the number of object properties we find on the first pass (excluding extension data).
        public int FoundPropertyCount;

        // Current constructor parameter value.
        public JsonParameterInfo? JsonParameterInfo;

        // Current parameter index used for reading from the parameter cache in JsonClassInfo.
        public int ParameterIndex;

        public List<ParameterRef>? ParameterRefCache;

        // The starting position of an object property when supporting continuation. This is cached for
        // each property and used to deserialize the property after the object is constructed.
        public JsonReaderState ReaderState;

        public void Reset()
        {
            ParameterIndex = 0;
            Arguments = null;
            DataExtension = null;
            FoundProperties = null;
            FoundPropertiesAsync = null;
            FoundPropertyCount = 0;
            ParameterRefCache = null;
        }
    }
}
