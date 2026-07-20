// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// This interface represents MSIL information for a single component assembly.
    /// </summary>
    public interface IAssemblyMetadata
    {
        void GetSectionData(int relativeVirtualAddress, Action<BlobReader> action);
        MetadataReader MetadataReader { get;  }
    }
}
