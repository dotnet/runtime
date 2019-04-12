// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.NET.HostModel.Bundle
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
        Assembly,           // IL Assemblies, which will be processed from bundle
        Ready2Run,          // R2R assemblies, currently unused, spilled to disk.
        DepsJson,           // Configuration file, processed from bundle
        RuntimeConfigJson,  // Configuration file, processed from bundle
        Extract             // Files spilled to disk by the host
    };
}

