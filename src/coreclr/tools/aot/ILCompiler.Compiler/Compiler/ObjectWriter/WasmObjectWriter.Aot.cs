// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysis;
using Internal.Text;
using Internal.TypeSystem.TypesDebugInfo;

namespace ILCompiler.ObjectWriter
{
    internal sealed partial class WasmObjectWriter
    {
        private protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
        {
            throw new PlatformNotSupportedException("NativeAOT WASM debug information is not supported.");
        }

        private protected override void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            Utf8String methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode,
            bool hasSequencePoints)
        {
            // No debug info emission for WASM.
        }

        private protected override void EmitDebugSections(IDictionary<Utf8String, SymbolDefinition> definedSymbols)
        {
            // No debug sections for WASM.
        }

        private protected override void CreateEhSections()
        {
            // No EH sections for WASM (exception handling uses WASM-specific mechanisms).
        }

        private protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            Utf8String currentSymbolName)
        {
            if (nodeWithCodeInfo.EHInfo is not null ||
                nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                Array.Exists(frameInfos, static frameInfo => (frameInfo.Flags & (FrameInfoFlags.Handler | FrameInfoFlags.Filter | FrameInfoFlags.HasEHInfo)) != 0))
            {
                throw new PlatformNotSupportedException("NativeAOT WASM exception handling is not supported.");
            }
        }
    }
}
