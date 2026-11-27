// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Locates the compact unwind records of a multi-file compilation, which describe managed frames instead of
    /// the <see cref="CompactUnwindTableNode"/> of a single-file compilation.
    /// </summary>
    /// <remarks>
    /// Methods of multi-file compilations are COMDAT sections that the linker keeps only once across the object
    /// files, so the object writer emits a record for each frame into a <see cref="SectionName"/> section of the
    /// COMDAT group of the method. The linker merges these sections and defines the symbols at the start and end of
    /// the merged section, so every module of the image refers to the same records, in no particular order.
    /// Layout (all fields little endian, kept in sync with UnixNativeCodeManager):
    ///   int32  relative pointer to the first record
    ///   int32  relative pointer to the end of the last record
    /// Record, 4 byte aligned:
    ///   int32  relative pointer to the start of the code
    ///   uint32 length of the code
    ///   int32  relative pointer to the LSDA
    ///   int32  relative pointer to the descriptor, in the format of <see cref="CompactUnwindTableNode"/>
    /// </remarks>
    public sealed class CompactUnwindRecordsNode : ObjectNode, ISymbolDefinitionNode
    {
        public const string SectionName = "__cunwind_records";

        public const int RecordSize = 16;

        public static Utf8String StartSymbolName => new Utf8String("__start_" + SectionName);

        public static Utf8String StopSymbolName => new Utf8String("__stop_" + SectionName);

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__compact_unwind_records"u8);
        }

        public int Offset => 0;
        public override bool IsShareable => false;
        public override bool StaticDependenciesAreComputed => true;
        public override int ClassCode => 0x5b0e7a29;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialAlignment(4);
            builder.AddSymbol(this);

            builder.EmitReloc(factory.ExternDataSymbol(StartSymbolName), RelocType.IMAGE_REL_BASED_RELPTR32);
            builder.EmitReloc(factory.ExternDataSymbol(StopSymbolName), RelocType.IMAGE_REL_BASED_RELPTR32);

            return builder.ToObjectData();
        }
    }
}
