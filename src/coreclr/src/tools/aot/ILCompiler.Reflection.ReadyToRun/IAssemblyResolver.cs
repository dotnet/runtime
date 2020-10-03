// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace ILCompiler.Reflection.ReadyToRun
{
    public interface IAssemblyResolver
    {
        IAssemblyMetadata FindAssembly(MetadataReader metadataReader, AssemblyReferenceHandle assemblyReferenceHandle, string parentFile);
        IAssemblyMetadata FindAssembly(string simpleName, string parentFile);
        // TODO (refactoring) - signature formatting options should be independent of assembly resolver
        bool Naked { get; }
        bool SignatureBinary { get; }
        bool InlineSignatureBinary { get; }
    }
}
