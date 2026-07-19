// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

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
    /// annotated with [DataContract], ensuring only types that actually have a MethodTable
    /// in the binary are included.
    /// </summary>
    public class ManagedDataDescriptorNode : ObjectNode, ISymbolDefinitionNode
    {
        private const string DataContractAttributeNamespace = "System.Diagnostics";
        private const string DataContractAttributeName = "DataContractAttribute";

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

            byte[] jsonBytes = BuildJsonDescriptor(factory);

            // Header layout: magic(8) + flags(4) + desc_size(4) + desc_ptr(ptr) + pointer_data_count(4) + pad(4) + pointer_data(ptr)
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
            builder.EmitUInt((uint)jsonBytes.Length);

            // char* descriptor — points to inline JSON after the header
            builder.EmitPointerReloc(this, headerSize);

            // uint32_t pointer_data_count = 0
            builder.EmitUInt(0);

            // uint32_t pad0
            builder.EmitUInt(0);

            // void** pointer_data = null
            builder.EmitZeroPointer();

            // Emit JSON bytes inline, null-terminated
            Debug.Assert(builder.CountBytes == headerSize);
            builder.EmitBytes(jsonBytes);
            builder.EmitByte(0);

            return builder.ToObjectData();
        }

        /// <summary>
        /// Build the JSON descriptor using the compact format expected by the cDAC reader's
        /// ContractDescriptorParser. Types are objects with an optional "!" size sigil and
        /// field-name properties mapped to their offsets.
        /// </summary>
        private static byte[] BuildJsonDescriptor(NodeFactory factory)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteNumber("version", 0);
                writer.WriteString("baseline", "empty");

                writer.WriteStartObject("types");
                foreach (TypeDesc type in factory.MetadataManager.GetTypesWithEETypes())
                {
                    if (type is not EcmaType ecmaType)
                        continue;

                    if (!ecmaType.HasCustomAttribute(DataContractAttributeNamespace, DataContractAttributeName))
                        continue;

                    WriteType(writer, ecmaType);
                }
                writer.WriteEndObject();

                writer.WriteStartObject("globals");
                writer.WriteEndObject();

                writer.WriteStartObject("contracts");
                writer.WriteEndObject();

                writer.WriteEndObject();
            }

            return stream.ToArray();
        }

        private static void WriteType(Utf8JsonWriter writer, EcmaType type)
        {
            writer.WriteStartObject(GetFullTypeName(type));

            if (type.IsValueType)
            {
                writer.WriteNumber("!", type.InstanceFieldSize.AsInt);
            }

            foreach (FieldDesc field in type.GetFields())
            {
                if (field.IsStatic || field is not EcmaField ecmaField)
                    continue;

                if (!ecmaField.HasCustomAttribute(DataContractAttributeNamespace, DataContractAttributeName))
                    continue;

                writer.WriteNumber(field.GetName(), field.Offset.AsInt);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Returns a fully-qualified type name for cDAC descriptors
        /// (e.g., "System.Threading.Thread" or "System.Foo.Outer+Inner").
        /// </summary>
        private static string GetFullTypeName(MetadataType type)
        {
            if (type.ContainingType is not null)
                return $"{GetFullTypeName(type.ContainingType)}+{type.GetName()}";

            string ns = type.GetNamespace();
            string name = type.GetName();

            if (string.IsNullOrEmpty(ns))
                return name;

            return $"{ns}.{name}";
        }

        public override int ClassCode => 0x4d444e01;
    }
}
