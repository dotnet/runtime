// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(RuntimeTypeMetadataUpdateHandler))]

namespace System.Reflection.Metadata
{
    /// <summary>Metadata update handler used to clear a Type's reflection cache in response to a metadata update notification.</summary>
    internal static class RuntimeTypeMetadataUpdateHandler
    {
        /// <summary>Clear type caches in response to an update notification.</summary>
        /// <param name="types">The specific types to be cleared, or null to clear everything.</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Clearing the caches on a Type isn't affected if a Type is trimmed, or has any of its members trimmed.")]
        public static void BeforeUpdate(Type[]? types)
        {
            if (RequiresClearingAllTypes(types))
            {
                // TODO: This should ideally be in a QCall in the runtime.  As written here:
                // - This materializes all types in the app, creating RuntimeTypes for them.
                // - This force loads all assembly dependencies, which might not work well for packages that may depend on resolve events firing at the right moment.
                // - This does not cover instantiated generic types.

                // Clear the caches on all loaded types.
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!SkipAssembly(assembly))
                    {
                        try
                        {
                            foreach (Type type in assembly.GetTypes())
                            {
                                ClearCache(type);
                            }
                        }
                        catch (ReflectionTypeLoadException) { }
                    }
                }
            }
            else
            {
                // Clear the caches on just the specified types.
                foreach (Type type in types)
                {
                    ClearCache(type);
                }
            }
        }

        /// <summary>When clearing all types, determines whether we should skip types from the specified assembly.</summary>
        private static bool SkipAssembly(Assembly assembly) =>
            // Ideally we'd skip all of the core libraries, which can't be edited.
            // But we can easily skip corelib.
            typeof(object).Assembly == assembly;

        /// <summary>Clears the cache on the specified type.</summary>
        private static void ClearCache(Type type) => (type as RuntimeType)?.ClearCache();

        /// <summary>Determines whether we need to clear the caches on all types or just the ones specified.</summary>
        /// <param name="types">The types to examine.</param>
        /// <returns>true if we need to clear all types; false if we can clear just the ones specified.</returns>
        private static bool RequiresClearingAllTypes([NotNullWhen(false)] Type[]? types)
        {
            // If we were handed null, assume we need to clear everything.
            if (types is null)
            {
                return true;
            }

            // If any type isn't sealed, we may need to clear a derived type,
            // so clear everything.
            foreach (Type type in types)
            {
                if (!type.IsSealed)
                {
                    return true;
                }
            }

            // We were handed types, and they were all sealed, so we can just clear them.
            return false;
        }
    }
}
