// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal sealed class TypePreinitializationMapNode : ModuleSpecificHeaderTableNode
    {
        private readonly MetadataReader _metadata;

        public TypePreinitializationMapNode(EcmaModule module)
            : base(module)
        {
            _metadata = module.MetadataReader;
        }

        public override int ClassCode => -1815494040;

        protected override string ModuleSpecificName => "__TypePreinitializationMap__";

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
            => !factory.ReadyToRunPreinitializationManager.HasAnyPreinitializedTypesInModule(_module);

        private readonly struct InstantiatedTypeEntry
        {
            public InstantiatedTypeEntry(
                uint typeDefRid,
                byte[] typeSignature,
                ReadyToRunPreinitializationManager.TypePreinitializationRecord record)
            {
                TypeDefRid = typeDefRid;
                TypeSignature = typeSignature;
                Record = record;
            }

            public uint TypeDefRid { get; }
            public byte[] TypeSignature { get; }
            public ReadyToRunPreinitializationManager.TypePreinitializationRecord Record { get; }
        }

        private static int CompareInstantiatedTypeEntries(InstantiatedTypeEntry x, InstantiatedTypeEntry y)
        {
            int ridCompare = x.TypeDefRid.CompareTo(y.TypeDefRid);
            if (ridCompare != 0)
                return ridCompare;

            int minLength = x.TypeSignature.Length < y.TypeSignature.Length ? x.TypeSignature.Length : y.TypeSignature.Length;
            for (int i = 0; i < minLength; i++)
            {
                int byteCompare = x.TypeSignature[i].CompareTo(y.TypeSignature[i]);
                if (byteCompare != 0)
                    return byteCompare;
            }

            return x.TypeSignature.Length.CompareTo(y.TypeSignature.Length);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            uint typeCount = (uint)_metadata.TypeDefinitions.Count;

            List<InstantiatedTypeEntry> instantiatedTypeEntries = new();
            foreach (KeyValuePair<MetadataType, ReadyToRunPreinitializationManager.TypePreinitializationRecord> knownEntry in
                factory.ReadyToRunPreinitializationManager.GetKnownInstantiatedTypeRecordsForModule(_module))
            {
                MetadataType instantiatedType = knownEntry.Key;
                ReadyToRunPreinitializationManager.TypePreinitializationRecord record = knownEntry.Value;

                if (instantiatedType.GetTypeDefinition() is not EcmaType ecmaTypeDefinition)
                    continue;

                uint typeDefRid = (uint)MetadataTokens.GetRowNumber(ecmaTypeDefinition.Handle);

                ArraySignatureBuilder signatureBuilder = new ArraySignatureBuilder();
                signatureBuilder.EmitTypeSignature(instantiatedType, factory.SignatureContext);

                instantiatedTypeEntries.Add(new InstantiatedTypeEntry(
                    typeDefRid,
                    signatureBuilder.ToArray(),
                    record));
            }

            instantiatedTypeEntries.Sort(CompareInstantiatedTypeEntries);

            uint[] instantiationCountsByTypeDefRid = new uint[typeCount + 1];
            foreach (InstantiatedTypeEntry entry in instantiatedTypeEntries)
            {
                if (entry.TypeDefRid <= typeCount)
                    instantiationCountsByTypeDefRid[(int)entry.TypeDefRid]++;
            }

            uint[] instantiationOffsetsByTypeDefRid = new uint[typeCount + 1];
            uint runningInstantiationOffset = 0;
            for (uint rid = 1; rid <= typeCount; rid++)
            {
                instantiationOffsetsByTypeDefRid[(int)rid] = runningInstantiationOffset;
                runningInstantiationOffset += instantiationCountsByTypeDefRid[(int)rid];
            }

            builder.EmitUInt(typeCount);

            foreach (TypeDefinitionHandle typeHandle in _metadata.TypeDefinitions)
            {
                MetadataType type = (MetadataType)_module.GetType(typeHandle);
                var record = factory.ReadyToRunPreinitializationManager.GetTypeRecord(type);
                if (factory.PreinitializationManager.IsPreinitialized(type) && !record.IsPreinitialized)
                {
                    factory.PreinitializationManager.GetPreinitializationInfo(type).SetPostScanFailure(record.FailureReason);
                }

                uint typeDefRid = (uint)MetadataTokens.GetRowNumber(typeHandle);

                // TypeDef row: READYTORUN_TYPE_PREINITIALIZATION_MAP_ENTRY
                builder.EmitUInt(typeDefRid);
                if (type.HasInstantiation)
                {
                    Debug.Assert(record.StaticsDataNode == null);
                    builder.EmitUInt(instantiationOffsetsByTypeDefRid[(int)typeDefRid]);
                    builder.EmitUInt(instantiationCountsByTypeDefRid[(int)typeDefRid]);
                }
                else
                {
                    if (record.StaticsDataNode != null)
                        builder.EmitReloc(record.StaticsDataNode, RelocType.IMAGE_REL_BASED_ADDR32NB);
                    else
                        builder.EmitUInt(0);

                    builder.EmitUInt((uint)record.NonGCDataSize);
                }

                uint flags = record.IsPreinitialized ? (uint)ReadyToRunTypePreinitializationFlags.TypeIsPreinitialized : 0;
                builder.EmitUInt(flags);
            }

            builder.EmitUInt((uint)instantiatedTypeEntries.Count);

            uint currentSignatureOffset = 0;
            foreach (InstantiatedTypeEntry entry in instantiatedTypeEntries)
            {
                // Instantiation row: READYTORUN_TYPE_PREINITIALIZATION_MAP_INSTANTIATION_ENTRY
                builder.EmitUInt(currentSignatureOffset);
                builder.EmitUInt((uint)entry.TypeSignature.Length);

                if (entry.Record.StaticsDataNode != null)
                    builder.EmitReloc(entry.Record.StaticsDataNode, RelocType.IMAGE_REL_BASED_ADDR32NB);
                else
                    builder.EmitUInt(0);

                builder.EmitUInt((uint)entry.Record.NonGCDataSize);

                uint flags = entry.Record.IsPreinitialized ? (uint)ReadyToRunTypePreinitializationFlags.TypeIsPreinitialized : 0;
                builder.EmitUInt(flags);

                currentSignatureOffset += (uint)entry.TypeSignature.Length;
            }

            // Type signatures
            foreach (InstantiatedTypeEntry entry in instantiatedTypeEntries)
            {
                foreach (byte b in entry.TypeSignature)
                    builder.EmitByte(b);
            }

            return builder.ToObjectData();
        }
    }
}
