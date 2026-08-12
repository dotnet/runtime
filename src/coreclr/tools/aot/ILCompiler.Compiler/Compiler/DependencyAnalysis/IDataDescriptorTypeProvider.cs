// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    internal interface IDataDescriptorTypeProvider
    {
        MetadataType GetDataDescriptorType(CompilerTypeSystemContext context);
    }
}
