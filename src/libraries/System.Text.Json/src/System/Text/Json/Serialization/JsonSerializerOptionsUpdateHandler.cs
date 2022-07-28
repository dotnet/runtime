// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

[assembly: MetadataUpdateHandler(typeof(JsonSerializerOptionsUpdateHandler))]

namespace System.Text.Json
{
    /// <summary>Handler used to clear JsonSerializerOptions reflection cache upon a metadata update.</summary>
    internal static class JsonSerializerOptionsUpdateHandler
    {
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        public static void ClearCache(Type[]? types)
        {
            // Ignore the types, and just clear out all reflection caches from serializer options.
            foreach (KeyValuePair<JsonSerializerOptions, object?> options in JsonSerializerOptions.TrackedOptionsInstances.All)
            {
                options.Key.ClearCaches();
            }

            // Flush the shared caching contexts
            JsonSerializerOptions.TrackedCachingContexts.Clear();

            // Flush the dynamic method cache
            ReflectionEmitCachingMemberAccessor.Clear();
        }
    }
}
