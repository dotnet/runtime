// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    internal interface IWasmFunctionBodyNode
    {
        // A shareable body must not encode its own table-slot or runtime-function identity.
        bool IsShareableWasmFunctionBody => true;

        bool HasCompatibleWasmRuntimeMetadata(IWasmFunctionBodyNode other) => true;
    }
}
