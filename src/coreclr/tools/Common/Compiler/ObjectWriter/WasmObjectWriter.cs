using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Wasm object file format writer.
    /// </summary>
    internal sealed class WasmObjectWriter : ObjectWriter
    {
        private const uint InvalidIndex = uint.MaxValue;
        private const uint InvalidOffset = uint.MaxValue;

        public WasmObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment) => throw new NotImplementedException();
        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, int sectionIndex, Stream sectionStream)
        {
        }

        private protected override void EmitObjectFile(Stream outputFileStream) => throw new NotImplementedException();
        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList) => throw new NotImplementedException();
        private protected override void EmitSymbolTable(IDictionary<string, SymbolDefinition> definedSymbols, SortedSet<string> undefinedSymbols) => throw new NotImplementedException();
       
    }

}
