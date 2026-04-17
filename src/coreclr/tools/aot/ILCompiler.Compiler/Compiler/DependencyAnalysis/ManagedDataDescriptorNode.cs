// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Emits a ContractDescriptor for managed type layouts that the cDAC reader
    /// can consume as a sub-descriptor. ILC knows managed type layouts at compile time,
    /// so it can emit field offsets that would otherwise require runtime metadata resolution.
    ///
    /// Types are discovered by scanning MetadataManager.GetTypesWithEETypes() for types
    /// annotated with [CdacType], ensuring only types that actually have a MethodTable
    /// in the binary are included.
    /// </summary>
    public class ManagedDataDescriptorNode : ObjectNode, ISymbolDefinitionNode
    {
        private const string CdacTypeAttributeNamespace = "System.Runtime.CompilerServices";
        private const string CdacTypeAttributeName = "CdacTypeAttribute";
        private const string CdacFieldAttributeName = "CdacFieldAttribute";

        public const string SymbolName = "DotNetManagedContractDescriptor";

        public override ObjectNodeSection GetSection(NodeFactory factory) =>
            factory.Target.IsWindows ? ObjectNodeSection.ReadOnlyDataSection : ObjectNodeSection.DataSection;

        public override bool StaticDependenciesAreComputed => true;
        public override bool IsShareable => false;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.NodeMangler.ExternVariable(new Utf8String(SymbolName)));
        }

        public int Offset => 0;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            string json = BuildJsonDescriptor(factory);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            // Header layout: magic(8) + flags(4) + desc_size(4) + desc_ptr(ptr) + count(4) + pad(4) + data_ptr(ptr)
            int headerSize = 8 + 4 + 4 + factory.Target.PointerSize + 4 + 4 + factory.Target.PointerSize;

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            // uint64_t magic
            builder.EmitLong(0x0043414443434e44L); // "DNCCDAC\0"

            // uint32_t flags (bit 0 must be set; bit 1 indicates 32-bit pointers)
            uint flags = (uint)(0x01 | (factory.Target.PointerSize == 4 ? 0x02 : 0x00));
            builder.EmitUInt(flags);

            // uint32_t descriptor_size
            builder.EmitInt(jsonBytes.Length);

            // char* descriptor — points to inline JSON after the header
            builder.EmitPointerReloc(this, headerSize);

            // uint32_t pointer_data_count = 0
            builder.EmitInt(0);

            // uint32_t pad0
            builder.EmitInt(0);

            // void** pointer_data = null
            builder.EmitZeroPointer();

            // Emit JSON bytes inline, null-terminated
            builder.EmitBytes(jsonBytes);
            builder.EmitByte(0);

            return builder.ToObjectData();
        }

        private static string BuildJsonDescriptor(NodeFactory factory)
        {
            var sb = new StringBuilder();
            sb.Append("{\"version\":0,\"types\":{");

            bool firstType = true;
            foreach (TypeDesc type in factory.MetadataManager.GetTypesWithEETypes())
            {
                if (type is not EcmaType ecmaType)
                    continue;

                if (!ecmaType.HasCustomAttribute(CdacTypeAttributeNamespace, CdacTypeAttributeName))
                    continue;

                if (!firstType)
                    sb.Append(',');
                firstType = false;

                EmitTypeJson(sb, ecmaType);
            }

            sb.Append("},\"globals\":{}}");
            return sb.ToString();
        }

        private static void EmitTypeJson(StringBuilder sb, EcmaType type)
        {
            // Use 0 (indeterminate) for reference types
            int typeSize = type.IsValueType ? type.InstanceFieldSize.AsInt : 0;

            sb.Append('"').Append(type.GetName()).Append("\":[");
            sb.Append(typeSize);
            sb.Append(",{");

            bool firstField = true;
            foreach (FieldDesc field in type.GetFields())
            {
                if (field.IsStatic || field is not EcmaField ecmaField)
                    continue;

                if (!ecmaField.HasCustomAttribute(CdacTypeAttributeNamespace, CdacFieldAttributeName))
                    continue;

                if (!firstField)
                    sb.Append(',');
                firstField = false;

                sb.Append('"').Append(field.GetName()).Append("\":");
                sb.Append(field.Offset.AsInt);
            }

            sb.Append("}]");
        }

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

#if !SUPPORT_JIT
        public override int ClassCode => 0x4d444e01;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return 0; // Singleton
        }
#endif
    }
}
