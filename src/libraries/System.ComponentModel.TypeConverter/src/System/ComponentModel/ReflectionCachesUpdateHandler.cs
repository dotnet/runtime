// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(ReflectionCachesUpdateHandler))]

namespace System.ComponentModel
{
    internal static class ReflectionCachesUpdateHandler
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The actual properties retrieved by GetProperties do not matter.")]
        public static void BeforeUpdate(Type[]? types)
        {
            // ReflectTypeDescriptionProvider maintains global caches on top of reflection.
            // Clear those.
            ReflectTypeDescriptionProvider.ClearReflectionCaches();

            // Each type descriptor may also cache reflection-based state that it gathered
            // from ReflectTypeDescriptionProvider.  Clear those as well.
            if (types is not null)
            {
                foreach (Type type in types)
                {
                    TypeDescriptor.Refresh(type);
                }
            }
            else
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    TypeDescriptor.GetProperties(assembly); // must call before calling Refresh
                    TypeDescriptor.Refresh(assembly);
                }
            }
        }
    }
}
