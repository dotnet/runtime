// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        private JsonTypeInfo<Dictionary<string, HighLowTemps>> _Dictionary;
        public JsonTypeInfo<Dictionary<string, HighLowTemps>> Dictionary
        {
            get
            {
                if (_Dictionary == null)
                {
                    JsonConverter customConverter;
                    if (Options.Converters.Count > 0 && (customConverter = GetRuntimeProvidedCustomConverter(typeof(Dictionary<string, HighLowTemps>))) != null)
                    {
                        _Dictionary = JsonMetadataServices.CreateValueInfo<Dictionary<string, HighLowTemps>>(Options, customConverter);
                    }
                    else
                    {
                        _Dictionary = JsonMetadataServices.CreateDictionaryInfo<Dictionary<string, HighLowTemps>, string, HighLowTemps>(Options, () => new Dictionary<string, HighLowTemps>(), this.String, this.HighLowTemps, default, serializeFunc: null);
                    }
                }

                return _Dictionary;
            }
        }
    }
}
