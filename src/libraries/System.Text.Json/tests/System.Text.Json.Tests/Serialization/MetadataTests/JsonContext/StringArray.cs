// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private JsonTypeInfo<string[]> _StringArray;
        public JsonTypeInfo<string[]> StringArray
        {
            get
            {
                if (_StringArray == null)
                {
                    JsonConverter customConverter;
                    if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(string[]))) != null)
                    {
                        _StringArray = JsonMetadataServices.CreateValueInfo<string[]>(Options, customConverter);
                    }
                    else
                    {
                        _StringArray = JsonMetadataServices.CreateArrayInfo<string>(Options, this.String, default, serializeFunc: null);
                    }
                }

                return _StringArray;
            }
        }
    }
}
