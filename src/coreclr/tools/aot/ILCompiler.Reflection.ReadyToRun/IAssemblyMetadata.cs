// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// This interface represents MSIL information for a single component assembly.
    /// </summary>
    public interface IAssemblyMetadata
    {
        PEReader ImageReader { get; }

        MetadataReader MetadataReader { get;  }
    }
}
