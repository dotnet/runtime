// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Build.Bundle
{
    /// <summary>
    /// FileType: Identifies the type of file embedded into the bundle.
    /// 
    /// The bundler differentiates a few kinds of files via the manifest,
    /// with respect to the way in which they'll be used by the runtime.
    ///
    /// - Runtime Configuration files: Processed directly from memory
    /// - Assemblies: Loaded directly from memory.
    ///               Currently, these are only pure-managed assemblies.
    ///               Future versions should include R2R assemblies here.
    /// - Other files: Files that will be extracted out to disk. 
    ///                These include native binaries.
    /// </summary>
    public enum FileType : byte
    {
        Application,        // Represents the main app, also an assembly
        Assembly,           // IL Assemblies, which will be processed from bundle
        Ready2Run,          // R2R assemblies, currently unused, spilled to disk.
        DepsJson,           // Configuration file, processed from bundle
        RuntimeConfigJson,  // Configuration file, processed from bundle
        Extract            // Files spilled to disk by the host
    };
}

