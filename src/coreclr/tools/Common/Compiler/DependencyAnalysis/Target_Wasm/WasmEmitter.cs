// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using ILCompiler.ObjectWriter;

namespace ILCompiler.DependencyAnalysis.Wasm
{
    public struct WasmEmitter(NodeFactory factory, bool relocsOnly)
    {
        public ObjectDataBuilder Builder = new ObjectDataBuilder(factory, relocsOnly);
    }
}
