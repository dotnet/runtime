// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;

namespace ILCompiler.Reflection.ReadyToRun
{
    public interface IAssemblyResolver
    {
        MetadataReader FindAssembly(MetadataReader metadataReader, AssemblyReferenceHandle assemblyReferenceHandle, string parentFile);
        MetadataReader FindAssembly(string simpleName, string parentFile);
        // TODO (refactoring) - signature formatting options should be independent of assembly resolver
        bool Naked { get; }
        bool SignatureBinary { get; }
        bool InlineSignatureBinary { get; }
    }
}
