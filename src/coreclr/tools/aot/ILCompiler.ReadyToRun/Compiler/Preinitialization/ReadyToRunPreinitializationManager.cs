// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ILCompiler.DependencyAnalysis.ReadyToRun;

using Internal.JitInterface;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    internal sealed class ReadyToRunPreinitializationManager
    {
        private readonly NodeFactory _factory;
        private readonly Dictionary<MetadataType, TypePreinitializationRecord> _typeRecords = new();
        private readonly Dictionary<EcmaModule, bool> _moduleHasPreinitializedTypes = new();
        private readonly Dictionary<string, Import> _serializedStringImports = new(StringComparer.Ordinal);
        private readonly Dictionary<TypeDesc, Import> _constructedTypeImports = new();
        private readonly Dictionary<MetadataType, Import> _typeClassInitFlagsImports = new();
        private readonly Dictionary<MetadataType, Import> _typeGCStaticsImports = new();
        private readonly Dictionary<MetadataType, Import> _typeNonGCStaticsImports = new();
        private readonly Dictionary<SerializedFrozenObjectKey, SerializedPreinitializationObjectDataNode> _serializedFrozenObjects = new();
        private readonly Dictionary<MethodImportKey, Import> _exactCallableAddressImports = new();

        public ReadyToRunPreinitializationManager(NodeFactory factory)
        {
            _factory = factory;
        }

        public bool IsTypePreinitialized(MetadataType type)
            => GetTypeRecord(type).IsPreinitialized;

        public bool HasAnyPreinitializedTypesInModule(EcmaModule module)
        {
            lock (_typeRecords)
            {
                if (_moduleHasPreinitializedTypes.TryGetValue(module, out bool hasAny) && hasAny)
                    return hasAny;
            }

            bool found = false;
            foreach (TypeDefinitionHandle typeHandle in module.MetadataReader.TypeDefinitions)
            {
                if (GetTypeRecord((MetadataType)module.GetType(typeHandle)).IsPreinitialized)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                lock (_typeRecords)
                {
                    foreach (KeyValuePair<MetadataType, TypePreinitializationRecord> known in _typeRecords)
                    {
                        if (known.Value.IsPreinitialized && IsTypeInModule(known.Key, module))
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            lock (_typeRecords)
            {
                if (found)
                    _moduleHasPreinitializedTypes[module] = true;
            }

            return found;
        }

        public IReadOnlyList<KeyValuePair<MetadataType, TypePreinitializationRecord>> GetKnownInstantiatedTypeRecordsForModule(EcmaModule module)
        {
            List<KeyValuePair<MetadataType, TypePreinitializationRecord>> result = new();

            lock (_typeRecords)
            {
                foreach (KeyValuePair<MetadataType, TypePreinitializationRecord> known in _typeRecords)
                {
                    MetadataType type = known.Key;
                    if (!known.Value.IsPreinitialized)
                        continue;

                    if (!type.HasInstantiation || type.IsGenericDefinition || type.IsCanonicalSubtype(CanonicalFormKind.Any) || type.IsRuntimeDeterminedSubtype)
                        continue;

                    if (!IsTypeInModule(type, module))
                        continue;

                    result.Add(known);
                }
            }

            return result;
        }

        public TypePreinitializationRecord GetTypeRecord(MetadataType type)
        {
            lock (_typeRecords)
            {
                if (_typeRecords.TryGetValue(type, out TypePreinitializationRecord record))
                {
                    return record;
                }
            }

            TypePreinitializationRecord computed = ComputeTypeRecord(type);
            lock (_typeRecords)
            {
                // It's possible another thread computed the same type record while we were computing, so check again before adding.
                if (_typeRecords.TryGetValue(type, out TypePreinitializationRecord existing))
                {
                    return existing;
                }

                _typeRecords[type] = computed;
                RootStaticsDataNode(computed);
                return computed;
            }
        }

        private void RootStaticsDataNode(TypePreinitializationRecord record)
        {
            if (record?.StaticsDataNode != null)
            {
                _factory.AddCompilationRoot(record.StaticsDataNode, "ReadyToRun preinitialized statics data");
            }
        }

        public Import GetOrCreateSerializedStringImport(string data)
        {
            lock (_typeRecords)
            {
                if (_serializedStringImports.TryGetValue(data, out Import existingImport))
                    return existingImport;
            }

            if (_factory.ManifestMetadataTable == null)
                throw new NotSupportedException("Manifest metadata table is not available for serialized string import emission.");

            int? tokenValue = _factory.ManifestMetadataTable._mutableModule.TryGetStringHandle(data);
            if (!tokenValue.HasValue)
                throw new NotSupportedException($"Unable to emit ReadyToRun string import for preinitialized value \"{data}\".");

            ModuleToken moduleToken = new ModuleToken(
                _factory.ManifestMetadataTable._mutableModule,
                MetadataTokens.Handle(tokenValue.Value));

            Import createdImport = new StringImport(_factory.StringImports, moduleToken);

            lock (_typeRecords)
            {
                if (_serializedStringImports.TryGetValue(data, out Import existingImport))
                    return existingImport;

                _serializedStringImports[data] = createdImport;
                return createdImport;
            }
        }

        public Import GetOrCreateConstructedTypeImport(TypeDesc type)
        {
            lock (_typeRecords)
            {
                if (_constructedTypeImports.TryGetValue(type, out Import existingImport))
                    return existingImport;
            }

            Import createdImport = new Import(
                _factory.PreinitializationImports,
                _factory.TypeSignature(ReadyToRunFixupKind.TypeHandle, type));

            lock (_typeRecords)
            {
                if (_constructedTypeImports.TryGetValue(type, out Import existingImport))
                    return existingImport;

                _constructedTypeImports[type] = createdImport;
                return createdImport;
            }
        }

        public Import GetOrCreateTypeNonGCStaticsImport(MetadataType type)
        {
            lock (_typeRecords)
            {
                if (_typeNonGCStaticsImports.TryGetValue(type, out Import existingImport))
                    return existingImport;
            }

            Import createdImport = new Import(
                _factory.PreinitializationImports,
                _factory.TypeSignature(ReadyToRunFixupKind.StaticBaseNonGC, type));

            lock (_typeRecords)
            {
                if (_typeNonGCStaticsImports.TryGetValue(type, out Import existingImport))
                    return existingImport;

                _typeNonGCStaticsImports[type] = createdImport;
                return createdImport;
            }
        }

        public Import GetOrCreateTypeGCStaticsImport(MetadataType type)
        {
            lock (_typeRecords)
            {
                if (_typeGCStaticsImports.TryGetValue(type, out Import existingImport))
                    return existingImport;
            }

            Import createdImport = new Import(
                _factory.PreinitializationImports,
                _factory.TypeSignature(ReadyToRunFixupKind.StaticBaseGC, type));

            lock (_typeRecords)
            {
                if (_typeGCStaticsImports.TryGetValue(type, out Import existingImport))
                    return existingImport;

                _typeGCStaticsImports[type] = createdImport;
                return createdImport;
            }
        }

        public Import GetOrCreateTypeClassInitFlagsImport(MetadataType type)
        {
            lock (_typeRecords)
            {
                if (_typeClassInitFlagsImports.TryGetValue(type, out Import existingImport))
                    return existingImport;
            }

            Import createdImport = new Import(
                _factory.PreinitializationImports,
                _factory.TypeSignature(ReadyToRunFixupKind.ClassInitFlags, type));

            lock (_typeRecords)
            {
                if (_typeClassInitFlagsImports.TryGetValue(type, out Import existingImport))
                    return existingImport;

                _typeClassInitFlagsImports[type] = createdImport;
                return createdImport;
            }
        }

        public SerializedPreinitializationObjectDataNode GetOrCreateSerializedFrozenObjectDataNode(MetadataType owningType, int allocationSiteId, TypePreinit.ISerializableReference data)
        {
            SerializedFrozenObjectKey key = new SerializedFrozenObjectKey(owningType, allocationSiteId);

            lock (_typeRecords)
            {
                if (_serializedFrozenObjects.TryGetValue(key, out SerializedPreinitializationObjectDataNode existingNode))
                    return existingNode;
            }

            SerializedPreinitializationObjectDataNode createdNode = new SerializedPreinitializationObjectDataNode(owningType, allocationSiteId, data);

            lock (_typeRecords)
            {
                if (_serializedFrozenObjects.TryGetValue(key, out SerializedPreinitializationObjectDataNode existingNode))
                    return existingNode;

                _serializedFrozenObjects[key] = createdNode;
                return createdNode;
            }
        }

        public Import GetOrCreateSerializedRuntimeTypeImport(TypeDesc type)
            => GetOrCreateConstructedTypeImport(type);

        public Import GetOrCreateExactCallableAddressImport(MethodWithToken method, bool isInstantiatingStub)
        {
            MethodImportKey key = new MethodImportKey(
                method,
                isInstantiatingStub);

            lock (_typeRecords)
            {
                if (_exactCallableAddressImports.TryGetValue(key, out Import existingImport))
                    return existingImport;
            }

            Import createdImport = new Import(
                _factory.PreinitializationImports,
                _factory.MethodSignature(ReadyToRunFixupKind.MethodEntry, key.Method, key.IsInstantiatingStub));

            lock (_typeRecords)
            {
                if (_exactCallableAddressImports.TryGetValue(key, out Import existingImport))
                    return existingImport;

                _exactCallableAddressImports[key] = createdImport;
                return createdImport;
            }
        }

        private TypePreinitializationRecord ComputeTypeRecord(MetadataType type)
        {
            if (!_factory.PreinitializationManager.IsPreinitialized(type))
                return TypePreinitializationRecord.NotPreinitialized;

            TypePreinit.PreinitializationInfo preinitInfo = _factory.PreinitializationManager.GetPreinitializationInfo(type);
            if (!preinitInfo.IsPreinitialized)
                return TypePreinitializationRecord.NotPreinitialized;

            bool hasNonNullGCData = false;
            foreach (FieldDesc field in type.GetFields())
            {
                if (!field.IsStatic || field.IsLiteral || field.HasRva || field.IsThreadStatic || !field.HasGCStaticBase)
                    continue;

                if (preinitInfo.GetFieldValue(field) != null)
                    hasNonNullGCData = true;
            }

            TypePreinitializedStaticsDataNode staticsDataNode = null;
            int nonGCDataSize;
            int gcDataSize;
            try
            {
                nonGCDataSize = TypePreinitializedStaticsDataNode.ComputeNonGCStaticsDataSize(type);
                gcDataSize = TypePreinitializedStaticsDataNode.ComputeGCStaticsDataSize(type);

                if (nonGCDataSize > 0 || gcDataSize > 0)
                {
                    staticsDataNode = new TypePreinitializedStaticsDataNode(preinitInfo);
                    ValidateSerializablePreinitializationGraph(staticsDataNode);
                }
            }
            catch (NotSupportedException ex)
            {
                return TypePreinitializationRecord.Unsupported(ex.Message);
            }
            catch (InvalidProgramException ex)
            {
                return TypePreinitializationRecord.Unsupported(ex.Message);
            }

            return new TypePreinitializationRecord(
                isPreinitialized: true,
                nonGCDataSize: nonGCDataSize,
                staticsDataNode: staticsDataNode,
                hasPreinitializedGCData: hasNonNullGCData,
                failureReason: null);
        }

        private void ValidateSerializablePreinitializationGraph(TypePreinitializedStaticsDataNode staticsDataNode)
        {
            Stack<SerializedPreinitializationObjectDataNode> pending = new();
            HashSet<SerializedPreinitializationObjectDataNode> visited = new();

            AddSerializedObjectRelocs(staticsDataNode.GetData(_factory, relocsOnly: true).Relocs, pending, visited);

            while (pending.Count > 0)
            {
                SerializedPreinitializationObjectDataNode node = pending.Pop();

                if (node.HasConditionalStaticDependencies)
                    _ = node.GetConditionalStaticDependencies(_factory);

                AddSerializedObjectRelocs(node.GetData(_factory, relocsOnly: true).Relocs, pending, visited);
            }
        }

        private static void AddSerializedObjectRelocs(
            Relocation[] relocs,
            Stack<SerializedPreinitializationObjectDataNode> pending,
            HashSet<SerializedPreinitializationObjectDataNode> visited)
        {
            if (relocs == null)
                return;

            foreach (Relocation reloc in relocs)
            {
                if (reloc.Target is SerializedPreinitializationObjectDataNode referencedObject
                    && visited.Add(referencedObject))
                {
                    pending.Push(referencedObject);
                }
            }
        }

        private static bool IsTypeInModule(MetadataType type, EcmaModule module)
        {
            if (type.GetTypeDefinition() is not EcmaType ecmaTypeDefinition)
                return false;

            return ecmaTypeDefinition.Module == module;
        }

        private readonly struct SerializedFrozenObjectKey : IEquatable<SerializedFrozenObjectKey>
        {
            public SerializedFrozenObjectKey(MetadataType owningType, int allocationSiteId)
            {
                OwningType = owningType;
                AllocationSiteId = allocationSiteId;
            }

            public MetadataType OwningType { get; }
            public int AllocationSiteId { get; }

            public bool Equals(SerializedFrozenObjectKey other)
                => OwningType == other.OwningType && AllocationSiteId == other.AllocationSiteId;

            public override bool Equals(object obj)
                => obj is SerializedFrozenObjectKey other && Equals(other);

            public override int GetHashCode()
                => HashCode.Combine(OwningType, AllocationSiteId);
        }

        private readonly struct MethodImportKey : IEquatable<MethodImportKey>
        {
            public MethodImportKey(MethodWithToken method, bool isInstantiatingStub)
            {
                Method = method;
                IsInstantiatingStub = isInstantiatingStub;
            }

            public MethodWithToken Method { get; }
            public bool IsInstantiatingStub { get; }

            public bool Equals(MethodImportKey other)
                => Method.Equals(other.Method) && IsInstantiatingStub == other.IsInstantiatingStub;

            public override bool Equals(object obj)
                => obj is MethodImportKey other && Equals(other);

            public override int GetHashCode()
                => HashCode.Combine(Method, IsInstantiatingStub);
        }

        internal sealed class TypePreinitializationRecord
        {
            public static TypePreinitializationRecord NotPreinitialized { get; } =
                new TypePreinitializationRecord(isPreinitialized: false, nonGCDataSize: 0, staticsDataNode: null, hasPreinitializedGCData: false, failureReason: null);

            public static TypePreinitializationRecord Unsupported(string reason) =>
                new TypePreinitializationRecord(isPreinitialized: false, nonGCDataSize: 0, staticsDataNode: null, hasPreinitializedGCData: false, failureReason: reason);

            public TypePreinitializationRecord(bool isPreinitialized, int nonGCDataSize, TypePreinitializedStaticsDataNode staticsDataNode, bool hasPreinitializedGCData, string failureReason)
            {
                IsPreinitialized = isPreinitialized;
                NonGCDataSize = nonGCDataSize;
                StaticsDataNode = staticsDataNode;
                HasPreinitializedGCData = hasPreinitializedGCData;
                FailureReason = failureReason;
            }

            public bool IsPreinitialized { get; }
            public int NonGCDataSize { get; }
            public TypePreinitializedStaticsDataNode StaticsDataNode { get; }
            public bool HasPreinitializedGCData { get; }
            public string FailureReason { get; }
        }
    }
}
