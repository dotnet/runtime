// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Emits a ContractDescriptor for managed type layouts that the cDAC reader
    /// can consume as a sub-descriptor. ILC knows managed type layouts at compile time,
    /// so it can emit field offsets that would otherwise require runtime metadata resolution.
    ///
    /// The NativeAOT runtime C++ code declares an extern pointer to this symbol and references
    /// it via CDAC_GLOBAL_SUB_DESCRIPTOR in datadescriptor.inc, enabling the cDAC reader to
    /// merge managed type information into its unified type map.
    /// </summary>
    /// <remarks>
    /// The emitted structure matches the ContractDescriptor format:
    /// <code>
    /// struct ContractDescriptor {
    ///     uint64_t magic;           // 0x0043414443434e44 "DNCCDAC\0"
    ///     uint32_t flags;           // Platform flags
    ///     uint32_t descriptor_size; // JSON blob size
    ///     char* descriptor;         // Pointer to JSON string
    ///     uint32_t pointer_data_count;
    ///     uint32_t pad0;
    ///     void** pointer_data;      // Pointer to auxiliary data array
    /// };
    /// </code>
    /// The JSON descriptor follows the cDAC contract descriptor schema:
    /// <code>
    /// { "version": 0, "types": { "TypeName": [size, { "Field": offset }] }, "globals": {} }
    /// </code>
    /// </remarks>
    public class ManagedDataDescriptorNode : ObjectNode, ISymbolDefinitionNode
    {
        public const string SymbolName = "DotNetManagedContractDescriptor";

        private readonly List<ManagedTypeDescriptor> _typeDescriptors = new List<ManagedTypeDescriptor>();

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

        /// <summary>
        /// Register a managed type to be included in the descriptor.
        /// </summary>
        /// <param name="descriptorTypeName">The cDAC type name (e.g., "ManagedIdDispenser")</param>
        /// <param name="type">The resolved managed type from ILC's type system</param>
        /// <param name="fieldMappings">Optional field name remapping: cDAC field name → managed field name.
        /// If null, all instance fields are included with their original names.</param>
        public void AddType(string descriptorTypeName, MetadataType type, Dictionary<string, string> fieldMappings = null)
        {
            _typeDescriptors.Add(new ManagedTypeDescriptor(descriptorTypeName, type, fieldMappings));
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            // uint64_t magic
            builder.EmitLong(0x0043414443434e44L); // "DNCCDAC\0"

            // uint32_t flags (bit 0 must be set; bit 1 indicates 32-bit pointers)
            uint flags = (uint)(0x01 | (factory.Target.PointerSize == 4 ? 0x02 : 0x00));
            builder.EmitUInt(flags);

            // uint32_t descriptor_size
            builder.EmitInt(_jsonBytesLength);

            // char* descriptor — pointer to JSON blob (separate compilation root)
            builder.EmitPointerReloc(_jsonBlobNode);

            // uint32_t pointer_data_count = 0
            builder.EmitInt(0);

            // uint32_t pad0
            builder.EmitInt(0);

            // void** pointer_data = null
            builder.EmitZeroPointer();

            return builder.ToObjectData();
        }

        /// <summary>
        /// Build the JSON and create the blob node. Must be called before the node
        /// is added to the dependency graph.
        /// </summary>
        public void FinalizeDescriptor()
        {
            string jsonDescriptor = BuildJsonDescriptor();
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonDescriptor);
            _jsonBytesLength = jsonBytes.Length;

            byte[] nullTerminated = new byte[jsonBytes.Length + 1];
            Array.Copy(jsonBytes, nullTerminated, jsonBytes.Length);
            _jsonBlobNode = new BlobNode(
                new Utf8String("__ManagedContractDescriptorJsonBlob"),
                ObjectNodeSection.ReadOnlyDataSection,
                nullTerminated,
                alignment: 1);
        }

        /// <summary>
        /// The blob node containing the JSON data. Add this as a separate compilation root.
        /// </summary>
        public BlobNode JsonBlobNode => _jsonBlobNode;

        private BlobNode _jsonBlobNode;
        private int _jsonBytesLength;

        private string BuildJsonDescriptor()
        {
            var sb = new StringBuilder();
            sb.Append("{\"version\":0,\"types\":{");

            bool firstType = true;
            foreach (var desc in _typeDescriptors)
            {
                if (!firstType)
                    sb.Append(',');
                firstType = false;

                EmitTypeJson(sb, desc);
            }

            sb.Append("},\"globals\":{}}");
            return sb.ToString();
        }

        private static void EmitTypeJson(StringBuilder sb, ManagedTypeDescriptor desc)
        {
            MetadataType type = desc.Type;

            // Use 0 (indeterminate) for reference types — their "size" from cDAC perspective
            // is not meaningful since they're GC-managed objects.
            int typeSize = type.IsValueType ? type.InstanceFieldSize.AsInt : 0;

            // JSON format: "TypeName": [size, { "Field1": offset, "Field2": offset }]
            sb.Append('"').Append(desc.DescriptorName).Append("\":[");
            sb.Append(typeSize);
            sb.Append(",{");

            bool firstField = true;
            foreach (FieldDesc field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                string fieldName = field.GetName();
                string cdacFieldName;
                if (desc.FieldMappings is not null)
                {
                    // Check if any cDAC name maps to this managed field name
                    cdacFieldName = null;
                    foreach (var kvp in desc.FieldMappings)
                    {
                        if (kvp.Value == fieldName)
                        {
                            cdacFieldName = kvp.Key;
                            break;
                        }
                    }
                    if (cdacFieldName is null)
                        continue;
                }
                else
                {
                    cdacFieldName = fieldName;
                }

                if (!firstField)
                    sb.Append(',');
                firstField = false;

                sb.Append('"').Append(cdacFieldName).Append("\":");
                sb.Append(field.Offset.AsInt);
            }

            sb.Append("}]");
        }

#if !SUPPORT_JIT
        public override int ClassCode => 0x4d444e01;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return 0; // Singleton
        }
#endif

        private readonly struct ManagedTypeDescriptor
        {
            public readonly string DescriptorName;
            public readonly MetadataType Type;
            public readonly Dictionary<string, string> FieldMappings;

            public ManagedTypeDescriptor(string descriptorName, MetadataType type, Dictionary<string, string> fieldMappings)
            {
                DescriptorName = descriptorName;
                Type = type;
                FieldMappings = fieldMappings;
            }
        }
    }
}
