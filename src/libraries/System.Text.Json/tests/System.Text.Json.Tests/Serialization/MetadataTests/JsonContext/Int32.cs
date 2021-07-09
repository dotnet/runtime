// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private JsonTypeInfo<int> _Int32;
        public JsonTypeInfo<int> Int32
        {
            get
            {
                if (_Int32 == null)
                {
                    JsonConverter customConverter;
                    if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(int))) != null)
                    {
                        _Int32 = JsonMetadataServices.CreateValueInfo<int>(Options, customConverter);
                    }
                    else
                    {
                        _Int32 = JsonMetadataServices.CreateValueInfo<int>(Options, JsonMetadataServices.Int32Converter);
                    }
                }

                return _Int32;
            }
        }
    }
}
