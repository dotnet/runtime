// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace SourceGenerators.Tests
{
    internal static class LiveReferencePack
    {
        /// <summary>
        /// Get the metadata references for the reference assemblies from the live build.
        /// </summary>
        /// <returns>The metadata references</returns>
        /// <remarks>
        /// This function assumes the references are in a live-ref-pack subfolder next to the
        /// containing assembly. Test projects can set TestRunRequiresLiveRefPack to copy the
        /// live references to a live-ref-pack subfolder in their output directory.
        /// </remarks>
        public static ImmutableArray<MetadataReference> GetMetadataReferences()
        {
            string testDirectory = Path.GetDirectoryName(typeof(LiveReferencePack).Assembly.Location)!;
            return Directory.EnumerateFiles(Path.Combine(testDirectory, "live-ref-pack"))
                .Select<string, MetadataReference>(f => MetadataReference.CreateFromFile(f))
                .ToImmutableArray();
        }
    }
}
