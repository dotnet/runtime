// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private JsonTypeInfo<List<DateTimeOffset>> _ListSystemDateTimeOffset;
        public JsonTypeInfo<List<DateTimeOffset>> ListSystemDateTimeOffset
        {
            get
            {
                if (_ListSystemDateTimeOffset == null)
                {
                    JsonConverter customConverter;
                    if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(List<DateTimeOffset>))) != null)
                    {
                        _ListSystemDateTimeOffset = JsonMetadataServices.CreateValueInfo<List<DateTimeOffset>>(Options, customConverter);
                    }
                    else
                    {
                        _ListSystemDateTimeOffset = JsonMetadataServices.CreateListInfo<List<DateTimeOffset>, DateTimeOffset>(Options, () => new List<DateTimeOffset>(), this.DateTimeOffset, default);
                    }
                }

                return _ListSystemDateTimeOffset;
            }
        }
    }
}
