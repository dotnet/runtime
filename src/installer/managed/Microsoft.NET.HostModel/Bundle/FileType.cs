// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// FileType: Identifies the type of file embedded into the bundle.
    ///
    /// The bundler differentiates a few kinds of files via the manifest,
    /// with respect to the way in which they'll be used by the runtime.
    /// </summary>
    public enum FileType : byte
    {
        Unknown,           // Type not determined.
        Assembly,          // IL and R2R Assemblies
        NativeBinary,      // NativeBinaries
        DepsJson,          // .deps.json configuration file
        RuntimeConfigJson, // .runtimeconfig.json configuration file
        Symbols            // PDB Files
    };
}
