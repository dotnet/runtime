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
    /// </summary>
    public enum FileType : byte
    {
        IL,                // IL Assemblies
        Ready2Run,         // R2R assemblies
        DepsJson,          // .deps.json configuration file
        RuntimeConfigJson, // .runtimeconfig.json configuration file
        Other              // Other files, including native binaries
    };
}

