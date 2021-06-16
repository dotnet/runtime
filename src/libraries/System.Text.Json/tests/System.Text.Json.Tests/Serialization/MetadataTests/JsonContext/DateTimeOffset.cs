// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private JsonTypeInfo<DateTimeOffset> _DateTimeOffset;
        public JsonTypeInfo<DateTimeOffset> DateTimeOffset
        {
            get
            {
                if (_DateTimeOffset == null)
                {
                    JsonConverter customConverter;
                    if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(DateTimeOffset))) != null)
                    {
                        _DateTimeOffset = JsonMetadataServices.CreateValueInfo<DateTimeOffset>(Options, customConverter);
                    }
                    else
                    {
                        _DateTimeOffset = JsonMetadataServices.CreateValueInfo<DateTimeOffset>(Options, JsonMetadataServices.DateTimeOffsetConverter);
                    }
                }

                return _DateTimeOffset;
            }
        }
    }
}
