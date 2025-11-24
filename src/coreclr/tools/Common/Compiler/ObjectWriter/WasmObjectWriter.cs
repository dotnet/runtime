using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Wasm object file format writer.
    /// </summary>
    internal sealed class WasmObjectWriter : ObjectWriter
    {
        // must be stored in little-endian order
        private const uint WasmMagicNumber = 0x6d736100;

        // must be stored in little-endian order
        private const uint WasmVersion = 0x1;
        private Dictionary<int, SectionData> _sectionIndexToData = new();
        
        public WasmObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
        }

        public override void EmitObject(Stream outputFileStream, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
        {
            // (module)
            SectionData data = new();
            SectionWriter headerWriter = new(this, 0, data);
            headerWriter.WriteLittleEndian(WasmMagicNumber);
            headerWriter.WriteLittleEndian(WasmVersion);

            data.GetReadStream().CopyTo(outputFileStream);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment) => throw new NotImplementedException();

        private void CreateSection(WasmSectionType sectionType, string comdatName, string symbolName, int sectionIndex, Stream sectionStream)
        {

        }

        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, int sectionIndex, Stream sectionStream)
        {
            throw new NotImplementedException();
        }

        public enum WasmSectionType
        {
            Custom = 0,
            Type = 1,
            Import = 2,
            Function = 3,
            Table = 4,
            Memory = 5,
            Global = 6,
            Export = 7,
            Start = 8,
            Element = 9,
            Code = 10,
            Data = 11,
            DataCount = 12,
            Tag = 13,
        }

        public class WasmSection
        {
            public WasmSectionType Type { get; }
            public string Name { get; }
            public WasmSection(WasmSectionType type, string name)
            {
                Type = type;
                Name = name;
            }
        }

        private Dictionary<string, int> _sectionNameToSectionIndex = new();

        private protected override void EmitObjectFile(Stream outputFileStream)
        {

        }
        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList) => throw new NotImplementedException();
        private protected override void EmitSymbolTable(IDictionary<string, SymbolDefinition> definedSymbols, SortedSet<string> undefinedSymbols) => throw new NotImplementedException();
       
    }

}
