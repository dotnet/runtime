// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Base class for WebAssembly object file format writers.
    /// </summary>
    internal abstract partial class WasmObjectWriter : ObjectWriter
    {
        public const int StackPointerGlobalIndex = WasmGlobalImports.StackPointerGlobalIndex;
        public const int ImageBaseGlobalIndex = WasmGlobalImports.ImageBaseGlobalIndex;
        public const int TableBaseGlobalIndex = WasmGlobalImports.TableBaseGlobalIndex;
        public const int AsyncContinuationGlobalIndex = WasmGlobalImports.AsyncContinuationGlobalIndex;

        public const int WebcilSectionAlignment = 16;

        protected WasmObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
        }
    }
}
