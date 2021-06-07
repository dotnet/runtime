// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text.Json;

[assembly: MetadataUpdateHandler(typeof(JsonSerializerOptionsUpdateHandler))]

namespace System.Text.Json
{
    /// <summary>Handler used to clear JsonSerializerOptions reflection cache upon a metadata update.</summary>
    internal static class JsonSerializerOptionsUpdateHandler
    {
        public static void ClearCache(Type[]? types)
        {
            // Ignore the types, and just clear out all reflection caches from serializer options.
            foreach (KeyValuePair<JsonSerializerOptions, object?> options in JsonSerializerOptions.TrackedOptionsInstances.All)
            {
                options.Key.ClearClasses();
            }
        }
    }
}
