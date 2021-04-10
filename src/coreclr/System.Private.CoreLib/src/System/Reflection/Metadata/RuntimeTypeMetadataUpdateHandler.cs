// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(RuntimeTypeMetadataUpdateHandler))]

namespace System.Reflection.Metadata
{
    /// <summary>Metadata update handler used to clear a Type's reflection cache in response to a metadata update notification.</summary>
    internal static class RuntimeTypeMetadataUpdateHandler
    {
        public static void BeforeUpdate(Type? type)
        {
            if (type is RuntimeType rt)
            {
                rt.ClearCache();
            }

            // TODO: https://github.com/dotnet/runtime/issues/50938
            // Do we need to clear the cache on other types, e.g. ones derived from this one?
            // Do we need to clear a cache on any other kinds of types?
        }
    }
}
