// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace ILCompiler.Reflection.ReadyToRun
{
    public interface IAssemblyResolver
    {
        IAssemblyMetadata FindAssembly(MetadataReader metadataReader, AssemblyReferenceHandle assemblyReferenceHandle, string parentFile);
        IAssemblyMetadata FindAssembly(string simpleName, string parentFile);
    }

    public class SignatureFormattingOptions
    {
        public bool Naked { get; set; }
        public bool SignatureBinary { get; set;  }
        public bool InlineSignatureBinary { get; set; }
    }
}
