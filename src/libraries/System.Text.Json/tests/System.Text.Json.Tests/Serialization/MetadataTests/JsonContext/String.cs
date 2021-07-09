// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private JsonTypeInfo<string> _String;
        public JsonTypeInfo<string> String
        {
            get
            {
                if (_String == null)
                {
                    JsonConverter customConverter;
                    if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(string))) != null)
                    {
                        _String = JsonMetadataServices.CreateValueInfo<string>(Options, customConverter);
                    }
                    else
                    {
                        _String = JsonMetadataServices.CreateValueInfo<string> (Options, JsonMetadataServices.StringConverter);
                    }
                }

                return _String;
            }
        }
    }
}
