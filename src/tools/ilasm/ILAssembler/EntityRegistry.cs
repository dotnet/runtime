// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILAssembler
{
    internal sealed class EntityRegistry
    {
        private readonly Dictionary<TableIndex, List<EntityBase>> _seenEntities = new();
        private readonly Dictionary<(TypeDefinitionEntity? ContainingType, string Namespace, string Name), TypeDefinitionEntity> _seenTypeDefs = new();
        private readonly Dictionary<(EntityBase ResolutionScope, string Namespace, string Name), TypeReferenceEntity> _seenTypeRefs = new();
        private readonly Dictionary<AssemblyName, AssemblyReferenceEntity> _seenAssemblyRefs = new();
        private readonly Dictionary<string, ModuleReferenceEntity> _seenModuleRefs = new();
        private readonly Dictionary<BlobBuilder, TypeSpecificationEntity> _seenTypeSpecs = new(new BlobBuilderContentEqualityComparer());
        private readonly Dictionary<BlobBuilder, StandaloneSignatureEntity> _seenStandaloneSignatures = new(new BlobBuilderContentEqualityComparer());
        private readonly Dictionary<string, FileEntity> _seenFiles = new();
        private readonly List<ManifestResourceEntity> _manifestResourceEntities = new();
        private readonly Dictionary<(ExportedTypeEntity? ContainingType, string Namespace, string Name), ExportedTypeEntity> _seenExportedTypes = new();
        private static readonly List<MemberReferenceEntity> _memberReferences = new();

        private sealed class BlobBuilderContentEqualityComparer : IEqualityComparer<BlobBuilder>
        {
            public bool Equals(BlobBuilder x, BlobBuilder y) => x.ContentEquals(y);

            // For simplicity, we'll just use the signature size as the hash code.
            // TODO: Make this better.
            public int GetHashCode(BlobBuilder obj) => obj.Count;
        }

        public enum WellKnownBaseType
        {
            System_Object,
            System_ValueType,
            System_Enum
        }

        public EntityRegistry()
        {
            ModuleType = GetOrCreateTypeDefinition(null, "", "<Module>", moduleType =>
            {
                moduleType.BaseType = null;
            });
        }

        public void WriteContentTo(MetadataBuilder builder, BlobBuilder ilStream)
        {
            // Now that we've seen all of the entities, we can write them out in the correct order.
            // Record the entities in the correct order so they are assigned handles.
            // After this, we'll write out the content of the entities in the correct order.
            foreach (TypeDefinitionEntity type in _seenEntities[TableIndex.TypeDef])
            {
                // Record entries for members defined in list columns
                foreach (var method in type.Methods)
                {
                    RecordEntityInTable(TableIndex.MethodDef, method);
                    foreach (var param in method.Parameters)
                    {
                        // COMPAT: Only record param entries for parameters that have names
                        // or other rows that would refer to it.
                        if (param.Name is not null
                            && param.MarshallingDescriptor.Count != 0
                            && param.HasCustomAttributes)
                        {
                            RecordEntityInTable(TableIndex.Param, param);
                        }
                    }
                }
                foreach (var field in type.Fields)
                {
                    RecordEntityInTable(TableIndex.Field, field);
                }
                foreach (var property in type.Properties)
                {
                    RecordEntityInTable(TableIndex.Property, property);
                }
                foreach (var @event in type.Events)
                {
                    RecordEntityInTable(TableIndex.Event, @event);
                }

                // Record entries in tables that are sorted based on their containing/associated class
                foreach (var impl in type.InterfaceImplementations)
                {
                    RecordEntityInTable(TableIndex.InterfaceImpl, impl);
                }

                foreach (var impl in type.MethodImplementations)
                {
                    RecordEntityInTable(TableIndex.MethodImpl, impl);
                }

                foreach (var genericParam in type.GenericParameters)
                {
                    RecordEntityInTable(TableIndex.GenericParam, genericParam);
                }

                // COMPAT: Record the generic parameter constraints based on the order saved in the TypeDef
                foreach (var constraint in type.GenericParameterConstraints)
                {
                    RecordEntityInTable(TableIndex.GenericParamConstraint, constraint);
                }
            }

            foreach (MemberReferenceEntity memberReferenceEntity in _memberReferences)
            {
                ResolveAndRecordMemberReference(memberReferenceEntity);
            }

            // Now that we've recorded all of the entities that wouldn't have had handles before,
            // we can start writing out the content of the entities.
            builder.AddModule(0, Module.Name is null ? default : builder.GetOrAddString(Module.Name), builder.GetOrAddGuid(Guid.NewGuid()), default, default);

            foreach (TypeReferenceEntity type in _seenEntities[TableIndex.TypeRef])
            {
                EntityBase resolutionScope = type.ResolutionScope;
                builder.AddTypeReference(
                    resolutionScope is FakeTypeEntity fakeScope ? fakeScope.ResolutionScopeColumnHandle : resolutionScope.Handle,
                    builder.GetOrAddString(type.Namespace),
                    builder.GetOrAddString(type.Name));
            }

            for (int i = 0; i < _seenEntities[TableIndex.TypeDef].Count; i++)
            {
                TypeDefinitionEntity type = (TypeDefinitionEntity)_seenEntities[TableIndex.TypeDef][i];
                builder.AddTypeDefinition(
                    type.Attributes,
                    builder.GetOrAddString(type.Name),
                    builder.GetOrAddString(type.Namespace),
                    type.BaseType is null ? default : type.BaseType.Handle,
                    (FieldDefinitionHandle)GetHandleForList(type.Fields, _seenEntities[TableIndex.TypeDef], type => ((TypeDefinitionEntity)type).Fields, i, TableIndex.Field),
                    (MethodDefinitionHandle)GetHandleForList(type.Methods, _seenEntities[TableIndex.TypeDef], type => ((TypeDefinitionEntity)type).Methods, i, TableIndex.MethodDef));

                builder.AddEventMap(
                    (TypeDefinitionHandle)type.Handle,
                    (EventDefinitionHandle)GetHandleForList(type.Events, _seenEntities[TableIndex.TypeDef], type => ((TypeDefinitionEntity)type).Events, i, TableIndex.Event));
                builder.AddPropertyMap(
                    (TypeDefinitionHandle)type.Handle,
                    (PropertyDefinitionHandle)GetHandleForList(type.Properties, _seenEntities[TableIndex.TypeDef], type => ((TypeDefinitionEntity)type).Properties, i, TableIndex.Property));

                // TODO: ClassLayout
                if (type.ContainingType is not null)
                {
                    builder.AddNestedType((TypeDefinitionHandle)type.Handle, (TypeDefinitionHandle)type.ContainingType.Handle);
                }
            }

            foreach (FieldDefinitionEntity fieldDef in _seenEntities[TableIndex.Field])
            {
                builder.AddFieldDefinition(
                    fieldDef.Attributes,
                    builder.GetOrAddString(fieldDef.Name),
                    fieldDef.Signature!.Count == 0 ? default : builder.GetOrAddBlob(fieldDef.Signature));

                // TODO: FieldLayout, FieldRVA
                if (fieldDef.MarshallingDescriptor is not null)
                {
                    builder.AddMarshallingDescriptor(fieldDef.Handle, builder.GetOrAddBlob(fieldDef.MarshallingDescriptor));
                }
            }

            for (int i = 0; i < _seenEntities[TableIndex.MethodDef].Count; i++)
            {
                MethodDefinitionEntity methodDef = (MethodDefinitionEntity)_seenEntities[TableIndex.MethodDef][i];

                int rva = 0;
                if (methodDef.MethodBody.CodeBuilder.Count != 0)
                {
                    rva = ilStream.Count;
                    methodDef.MethodBody.CodeBuilder.WriteContentTo(ilStream);
                }

                builder.AddMethodDefinition(
                    methodDef.MethodAttributes,
                    methodDef.ImplementationAttributes,
                    builder.GetOrAddString(methodDef.Name),
                    builder.GetOrAddBlob(methodDef.MethodSignature!),
                    rva,
                    (ParameterHandle)GetHandleForList(methodDef.Parameters, _seenEntities[TableIndex.MethodDef], method => ((MethodDefinitionEntity)method).Parameters, i, TableIndex.Param));

                if (methodDef.MethodImportInformation is not null)
                {
                    builder.AddMethodImport(
                        (MethodDefinitionHandle)methodDef.Handle,
                        methodDef.MethodImportInformation.Value.Attributes,
                        methodDef.MethodImportInformation.Value.EntryPointName is null ? default : builder.GetOrAddString(methodDef.MethodImportInformation.Value.EntryPointName),
                        (ModuleReferenceHandle)methodDef.MethodImportInformation.Value.ModuleName.Handle);
                }
            }

            foreach (ParameterEntity param  in _seenEntities[TableIndex.Param])
            {
                builder.AddParameter(
                    param.Attributes,
                    param.Name is null ? default : builder.GetOrAddString(param.Name),
                    param.Sequence);

                if (param.MarshallingDescriptor is not null)
                {
                    builder.AddMarshallingDescriptor(param.Handle, builder.GetOrAddBlob(param.MarshallingDescriptor));
                }
            }

            foreach (InterfaceImplementationEntity impl in _seenEntities[TableIndex.InterfaceImpl])
            {
                builder.AddInterfaceImplementation(
                    (TypeDefinitionHandle)impl.Type.Handle,
                    impl.InterfaceType is FakeTypeEntity fakeType ? fakeType.TypeColumnHandle : impl.InterfaceType.Handle);
            }

            foreach (MemberReferenceEntity memberRef in _memberReferences)
            {
                builder.AddMemberReference(
                    memberRef.Parent.Handle,
                    builder.GetOrAddString(memberRef.Name),
                    builder.GetOrAddBlob(memberRef.Signature));
            }

            foreach (DeclarativeSecurityAttributeEntity declSecurity in _seenEntities[TableIndex.DeclSecurity])
            {
                builder.AddDeclarativeSecurityAttribute(
                    declSecurity.Parent?.Handle ?? default,
                    declSecurity.Action,
                    builder.GetOrAddBlob(declSecurity.PermissionSet));
            }

            foreach (StandaloneSignatureEntity standaloneSig in _seenEntities[TableIndex.StandAloneSig])
            {
                builder.AddStandaloneSignature(
                    builder.GetOrAddBlob(standaloneSig.Signature));
            }

            foreach (EventEntity evt in _seenEntities[TableIndex.Event])
            {
                builder.AddEvent(
                    evt.Attributes,
                    builder.GetOrAddString(evt.Name),
                    evt.Type.Handle);

                foreach (var accessor in evt.Accessors)
                {
                    builder.AddMethodSemantics(evt.Handle, accessor.Semantic, (MethodDefinitionHandle)accessor.Method.Handle);
                }
            }

            foreach (PropertyEntity prop in _seenEntities[TableIndex.Property])
            {
                builder.AddProperty(
                    prop.Attributes,
                    builder.GetOrAddString(prop.Name),
                    builder.GetOrAddBlob(prop.Type));

                foreach (var accessor in prop.Accessors)
                {
                    builder.AddMethodSemantics(prop.Handle, accessor.Semantic, (MethodDefinitionHandle)accessor.Method.Handle);
                }
            }

            foreach (ModuleReferenceEntity moduleRef in _seenEntities[TableIndex.ModuleRef])
            {
                builder.AddModuleReference(builder.GetOrAddString(moduleRef.Name));
            }

            foreach (TypeSpecificationEntity typeSpec in _seenEntities[TableIndex.TypeSpec])
            {
                builder.AddTypeSpecification(builder.GetOrAddBlob(typeSpec.Signature));
            }

            if (Assembly is not null)
            {
                builder.AddAssembly(
                    builder.GetOrAddString(Assembly.Name),
                    Assembly.Version ?? new Version(),
                    Assembly.Culture is null ? default : builder.GetOrAddString(Assembly.Culture),
                    Assembly.PublicKeyOrToken is null ? default : builder.GetOrAddBlob(Assembly.PublicKeyOrToken),
                    Assembly.Flags,
                    Assembly.HashAlgorithm);
            }

            foreach (FileEntity file in _seenEntities[TableIndex.File])
            {
                builder.AddAssemblyFile(
                    builder.GetOrAddString(file.Name),
                    file.Hash is not null ? builder.GetOrAddBlob(file.Hash) : default,
                    file.HasMetadata);
            }

            foreach (ExportedTypeEntity exportedType in _seenEntities[TableIndex.ExportedType])
            {
                builder.AddExportedType(
                    exportedType.Attributes,
                    builder.GetOrAddString(exportedType.Name),
                    builder.GetOrAddString(exportedType.Namespace),
                    exportedType.Implementation?.Handle ?? default,
                    exportedType.TypeDefinitionId);
            }

            foreach (ManifestResourceEntity resource in _seenEntities[TableIndex.ManifestResource])
            {
                builder.AddManifestResource(
                    resource.Attributes,
                    builder.GetOrAddString(resource.Name),
                    resource.Implementation?.Handle ?? default,
                    resource.Offset);
            }

            // TODO: MethodSpec

            static EntityHandle GetHandleForList(IReadOnlyList<EntityBase> list, IReadOnlyList<EntityBase> listOwner, Func<EntityBase, IReadOnlyList<EntityBase>> getList, int ownerIndex, TableIndex tokenType)
            {
                // Return the first entry in the list.
                // If the list is empty, return the start of the next list.
                // If there is no next list, return one past the end of the previous list.
                if (list.Count != 0)
                {
                    return list[0].Handle;
                }

                for (int i = 0; i < listOwner.Count; i++)
                {
                    var otherList = getList(listOwner[i]);
                    if (otherList.Count != 0)
                    {
                        return otherList[0].Handle;
                    }
                }

                for (int i = ownerIndex - 1; i >= 0; i--)
                {
                    var otherList = getList(listOwner[i]);
                    if (otherList.Count != 0)
                    {
                        return MetadataTokens.EntityHandle(tokenType, MetadataTokens.GetRowNumber(otherList[otherList.Count - 1].Handle) + 1);
                    }
                }
                return MetadataTokens.EntityHandle(tokenType, 0);
            }
        }

        public TypeEntity? ResolveImplicitBaseType(WellKnownBaseType? type)
        {
            if (type is null)
            {
                return null;
            }
            return type switch
            {
                WellKnownBaseType.System_Object => SystemObjectType,
                WellKnownBaseType.System_ValueType => SystemValueTypeType,
                WellKnownBaseType.System_Enum => SystemEnumType,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        private TypeEntity? _systemObject;
        public TypeEntity SystemObjectType
        {
            get
            {
                return _systemObject ??= ResolveFromCoreAssembly("System.Object");
            }
        }

        private TypeEntity? _systemValueType;
        public TypeEntity SystemValueTypeType
        {
            get
            {
                return _systemValueType ??= ResolveFromCoreAssembly("System.ValueType");
            }
        }

        private TypeEntity? _systemEnum;
        public TypeEntity SystemEnumType
        {
            get
            {
                return _systemEnum ??= ResolveFromCoreAssembly("System.Enum");
            }
        }

        public TypeDefinitionEntity ModuleType { get; }

        public ModuleEntity Module { get; } = new ModuleEntity();

        public AssemblyEntity? Assembly { get; set; }

        private TypeEntity ResolveFromCoreAssembly(string typeName)
        {
            throw new NotImplementedException();
        }

        public interface IHasHandle
        {
            EntityHandle Handle { get; }
            void SetHandle(EntityHandle token);
        }

        public void RecordEntityInTable(TableIndex table, EntityBase entity)
        {
            if (!_seenEntities.TryGetValue(table, out List<EntityBase> entities))
            {
                _seenEntities[table] = entities = new List<EntityBase>();
            }
            entities.Add(entity);
            ((IHasHandle)entity).SetHandle(MetadataTokens.EntityHandle(table, entities.Count));
        }

        private TEntity GetOrCreateEntity<TKey, TEntity>(TKey key, TableIndex table, Dictionary<TKey, TEntity> cache, Func<TKey, TEntity> constructor, Action<TEntity> onCreate)
            where TEntity : EntityBase
        {
            if (cache.TryGetValue(key, out TEntity entity))
            {
                return entity;
            }
            entity = constructor(key);
            RecordEntityInTable(table, entity);
            cache.Add(key, entity);
            onCreate(entity);
            return entity;
        }

        private TEntity CreateEntity<TEntity>(TableIndex table, List<TEntity> cache, Func<TEntity> constructor)
            where TEntity : EntityBase
        {
            TEntity entity = constructor();
            RecordEntityInTable(table, entity);
            cache.Add(entity);
            return entity;
        }

        public TypeDefinitionEntity GetOrCreateTypeDefinition(TypeDefinitionEntity? containingType, string @namespace, string name, Action<TypeDefinitionEntity> onCreateType)
        {
            return GetOrCreateEntity((containingType, @namespace, name), TableIndex.TypeDef, _seenTypeDefs, (key) => new(key.Item1, key.Item2, key.Item3), onCreateType);
        }

        public TypeDefinitionEntity? FindTypeDefinition(TypeDefinitionEntity? containingType, string @namespace, string @name)
        {
            if (_seenTypeDefs.TryGetValue((containingType, @namespace, name), out var typeDef))
            {
                return typeDef;
            }
            return null;
        }

        public AssemblyReferenceEntity GetOrCreateAssemblyReference(string name, Action<AssemblyReferenceEntity> onCreateAssemblyReference)
        {
            return GetOrCreateEntity(new(name), TableIndex.AssemblyRef, _seenAssemblyRefs, _ => new(name), onCreateAssemblyReference);
        }

        public ModuleReferenceEntity GetOrCreateModuleReference(string name, Action<ModuleReferenceEntity> onCreateModuleReference)
        {
            return GetOrCreateEntity(name, TableIndex.ModuleRef, _seenModuleRefs, name => new(name), onCreateModuleReference);
        }

        public ModuleReferenceEntity? FindModuleReference(string name)
        {
            if (_seenModuleRefs.TryGetValue(name, out var moduleRef))
            {
                return moduleRef;
            }
            return null;
        }

        public static GenericParameterEntity CreateGenericParameter(GenericParameterAttributes attributes, string name)
        {
            GenericParameterEntity param = new(attributes, name);
            return param;
        }

        public static GenericParameterConstraintEntity CreateGenericConstraint(TypeEntity baseType)
        {
            GenericParameterConstraintEntity constraint = new(baseType);
            return constraint;
        }

        public TypeSpecificationEntity GetOrCreateTypeSpec(BlobBuilder signature)
        {
            return GetOrCreateEntity(signature, TableIndex.TypeSpec, _seenTypeSpecs, signature => new(signature), _ => { });
        }

        public EntityBase ResolveHandleToEntity(EntityHandle entityHandle)
        {
            _ = MetadataTokens.TryGetTableIndex(entityHandle.Kind, out var tableIndex);
            if (_seenEntities.TryGetValue(tableIndex, out var entity))
            {
                int rowNumber = MetadataTokens.GetRowNumber(entityHandle);
                if (entity.Count < rowNumber - 1)
                {
                    return entity[rowNumber - 1];
                }
            }
            // Row entry does not exist. Use our FakeTypeEntity type to record the invalid handle.
            return new FakeTypeEntity(entityHandle);
        }

        public TypeReferenceEntity GetOrCreateTypeReference(EntityBase resolutionContext, TypeName name)
        {
            Stack<(string Namespace, string Name)> allTypeNames = new();
            // Record all of the containing type names
            for (TypeName? containingType = name; containingType is not null; containingType = containingType.ContainingTypeName)
            {
                allTypeNames.Push(NameHelpers.SplitDottedNameToNamespaceAndName(containingType.DottedName));
            }

            EntityBase scope = resolutionContext;
            while (scope is TypeReferenceEntity typeRef)
            {
                allTypeNames.Push((typeRef.Namespace, typeRef.Name));
                scope = typeRef.ResolutionScope;
            }
            while(allTypeNames.Count > 0)
            {
                var typeName = allTypeNames.Pop();
                scope = GetOrCreateEntity((scope, typeName.Namespace, typeName.Name), TableIndex.TypeRef, _seenTypeRefs, value => new TypeReferenceEntity(scope, value.Namespace, value.Name), typeRef =>
                {
                    StringBuilder builder = new(typeRef.Namespace.Length + typeRef.Name.Length + 1);
                    builder.AppendFormat("{0}.{1}", typeRef.Namespace, typeRef.Name);
                    if (resolutionContext is AssemblyReferenceEntity asmRef)
                    {
                        // TODO: Do full assembly name here
                        builder.Append(asmRef.Name);
                    }
                    typeRef.ReflectionNotation = builder.ToString();
                });
            }
            return (TypeReferenceEntity)scope;
        }

        public static MethodDefinitionEntity CreateUnrecordedMethodDefinition(TypeDefinitionEntity containingType, string name)
        {
            return new MethodDefinitionEntity(containingType, name);
        }

        public static bool TryAddMethodDefinitionToContainingType(MethodDefinitionEntity methodDef)
        {
            if (methodDef.MethodSignature is null)
            {
                throw new ArgumentException("The method signature must be defined before recording the method definition, to enable detecting duplicate methods.");
            }
            bool allowDuplicate = (methodDef.MethodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope;
            if (!allowDuplicate)
            {
                foreach (var method in methodDef.ContainingType.Methods)
                {
                    if (methodDef.Name == method.Name
                        && methodDef.MethodSignature.ContentEquals(method.MethodSignature!)
                        && (method.MethodAttributes & MethodAttributes.MemberAccessMask) != MethodAttributes.PrivateScope)
                    {
                        return false;
                    }
                }
            }
            methodDef.ContainingType.Methods.Add(methodDef);
            return true;
        }

        public static FieldDefinitionEntity? CreateUnrecordedFieldDefinition(FieldAttributes attributes, TypeDefinitionEntity containingType, string name, BlobBuilder signature)
        {
            var field = new FieldDefinitionEntity(attributes, containingType, name, signature);
            bool allowDuplicate = (field.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope;
            if (!allowDuplicate)
            {
                foreach (var fieldDef in field.ContainingType.Fields)
                {
                    if (fieldDef.Name == field.Name
                        && fieldDef.Signature.ContentEquals(field.Signature!)
                        && (fieldDef.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.PrivateScope)
                    {
                        return null;
                    }
                }
            }
            field.ContainingType.Fields.Add(field);
            return field;
        }

        public static InterfaceImplementationEntity CreateUnrecordedInterfaceImplementation(TypeDefinitionEntity implementingType, TypeEntity interfaceType)
        {
            return new InterfaceImplementationEntity(implementingType, interfaceType);
        }

        public static ParameterEntity CreateParameter(ParameterAttributes attributes, string? name, BlobBuilder marshallingDescriptor, int sequence)
        {
            return new ParameterEntity(attributes, name, marshallingDescriptor, sequence);
        }

        public static MemberReferenceEntity CreateLazilyRecordedMemberReference(TypeEntity containingType, string name, BlobBuilder signature)
        {
            var entity = new MemberReferenceEntity(containingType, name, signature);
            _memberReferences.Add(entity);
            return entity;
        }

        private void ResolveAndRecordMemberReference(MemberReferenceEntity memberRef)
        {
            // TODO: Resolve the member reference to a local method or field reference or record
            // it into the member reference table.
            RecordEntityInTable(TableIndex.MemberRef, memberRef);
        }

        public StandaloneSignatureEntity GetOrCreateStandaloneSignature(BlobBuilder signature)
        {
            return GetOrCreateEntity(signature, TableIndex.StandAloneSig, _seenStandaloneSignatures, (sig) => new(sig), _ => { });
        }

        public DeclarativeSecurityAttributeEntity CreateDeclarativeSecurityAttribute(DeclarativeSecurityAction action, BlobBuilder permissionSet)
        {
            var entity = new DeclarativeSecurityAttributeEntity(action, permissionSet);
            RecordEntityInTable(TableIndex.DeclSecurity, entity);
            return entity;
        }

        public CustomAttributeEntity CreateCustomAttribute(EntityBase constructor, BlobBuilder value)
        {
            var entity = new CustomAttributeEntity(constructor, value);
            RecordEntityInTable(TableIndex.CustomAttribute, entity);
            return entity;
        }

        public static MethodImplementationEntity CreateUnrecordedMethodImplementation(MethodDefinitionEntity methodBody, MemberReferenceEntity methodDeclaration)
        {
            return new MethodImplementationEntity(methodBody, methodDeclaration);
        }

        public FileEntity GetOrCreateFile(string name, bool hasMetadata, BlobBuilder? hash)
        {
            return GetOrCreateEntity(name, TableIndex.File, _seenFiles, (name) => new FileEntity(name), entity =>
            {
                entity.HasMetadata = hasMetadata;
                entity.Hash = hash;
            });
        }

        public FileEntity? FindFile(string name)
        {
            if (_seenFiles.TryGetValue(name, out var file))
            {
                return file;
            }
            return null;
        }

        public AssemblyReferenceEntity? FindAssemblyReference(string name)
        {
            if (_seenAssemblyRefs.TryGetValue(new AssemblyName(name), out var file))
            {
                return file;
            }
            return null;
        }

        public AssemblyReferenceEntity GetOrCreateAssemblyReference(string name, Version version, string? culture, BlobBuilder? publicKeyOrToken, AssemblyFlags flags, ProcessorArchitecture architecture)
        {
            AssemblyName key = new AssemblyName(name)
            {
                Version = version,
                CultureName = culture,
                Flags = (AssemblyNameFlags)flags,
                ProcessorArchitecture = architecture,
                KeyPair = publicKeyOrToken is null ? null : new StrongNameKeyPair(publicKeyOrToken.ToArray())
            };
            return GetOrCreateEntity(key, TableIndex.AssemblyRef, _seenAssemblyRefs, (value) => new AssemblyReferenceEntity(name), entity =>
            {
                entity.Version = version;
                entity.Culture = culture;
                entity.PublicKeyOrToken = publicKeyOrToken;
                entity.Flags = flags;
                entity.ProcessorArchitecture = architecture;
            });
        }

        public ManifestResourceEntity CreateManifestResource(string name, uint offset)
        {
            return CreateEntity(TableIndex.ManifestResource, _manifestResourceEntities, () => new ManifestResourceEntity(name, offset));
        }

        public ExportedTypeEntity GetOrCreateExportedType(EntityBase? implementation, string @namespace, string name, Action<ExportedTypeEntity> onCreateType)
        {
            // We only key on the implementation if the type is nested.
            return GetOrCreateEntity((implementation as ExportedTypeEntity, @namespace, name), TableIndex.ExportedType, _seenExportedTypes, (key) => new(key.Item3, key.Item2, key.Item1), onCreateType);
        }

        public ExportedTypeEntity? FindExportedType(ExportedTypeEntity? containingType, string @namespace, string @name)
        {
            if (_seenExportedTypes.TryGetValue((containingType, @namespace, name), out var typeDef))
            {
                return typeDef;
            }
            return null;
        }

        public IHasHandle? EntryPoint { get; set; }

        public abstract class EntityBase : IHasHandle
        {
            public EntityHandle Handle { get; private set; }

            protected virtual void SetHandle(EntityHandle token)
            {
                Handle = token;
            }

            void IHasHandle.SetHandle(EntityHandle token) => SetHandle(token);
        }

        public abstract class TypeEntity : EntityBase
        {
        }

        public interface IHasReflectionNotation
        {
            string ReflectionNotation { get; }
        }

        // COMPAT: The ilasm grammar allows ModuleRefs and AssemblyRefs to be returned in addition to types in typeSpec rules and arbitrary tokens to be returned
        // by the mdtoken grammar, which can replace a type amongst other things.
        // We'll record the actual handle as callers might need to retrieve it,
        // but for emit we'll use the specialized properties depending on the case where the invalid handle is used.
        public sealed class FakeTypeEntity : TypeEntity
        {
            public FakeTypeEntity(EntityHandle realEntity)
            {
                ((IHasHandle)this).SetHandle(realEntity);
                TypeColumnHandle = default(TypeDefinitionHandle);
                ResolutionScopeColumnHandle = default(ModuleDefinitionHandle);
                TypeSignatureHandle = MetadataTokens.TypeDefinitionHandle(MetadataTokens.GetRowNumber(realEntity));
                ImplementationHandle = default(AssemblyReferenceHandle);
            }

            /// <summary>
            /// For cases where an arbitrary (non-TypeDefOrRefOrSpec) token is referenced by a column in a metadata table that uses coded indices, the encoding fails,
            /// ilasm asserts, and a nil token for 0 option (TypeDef) of the coded index is emitted.
            /// </summary>
            public EntityHandle TypeColumnHandle { get; }

            /// <summary>
            /// For cases where an arbitrary (non-ResolutionScope) token is referenced by a column in a metadata table that uses coded indices, the encoding fails,
            /// ilasm asserts, and a nil token for 0 option (ModuleDefinition) of the coded index is emitted.
            /// </summary>
            public EntityHandle ResolutionScopeColumnHandle { get; }

            /// <summary>
            /// In cases where an arbitrary (non-TypeDefOrRefOrSpec) token is referenced in a metadata blob (like a signature blob),
            /// the token is emitted as a compressed integer of the row entry, shifted to account for encoding the table type.
            /// </summary>
            public EntityHandle TypeSignatureHandle { get; }

            /// <summary>
            /// In cases where an arbitrary (non-Implementation) token is referenced in a column in a metadata table,
            /// the the token is emitted as the nil token.
            /// </summary>
            public EntityHandle ImplementationHandle { get; }
        }

        public sealed class TypeDefinitionEntity : TypeEntity, IHasReflectionNotation
        {
            public TypeDefinitionEntity(TypeDefinitionEntity? containingType, string @namespace, string name)
            {
                ContainingType = containingType;
                Namespace = @namespace;
                Name = name;

                ReflectionNotation = CreateReflectionNotation(this);

                static string CreateReflectionNotation(TypeDefinitionEntity typeDefinition)
                {
                    StringBuilder builder = new();
                    Stack<TypeDefinitionEntity> containingTypes = new();
                    for (TypeDefinitionEntity? containingType = typeDefinition; containingType is not null; containingType = containingType.ContainingType)
                    {
                        containingTypes.Push(containingType);
                    }
                    while (containingTypes.Count != 0)
                    {
                        TypeDefinitionEntity containingType = containingTypes.Pop();
                        builder.Append(containingType.Namespace);
                        builder.Append('.');
                        builder.Append(containingType.Name);
                        if (containingTypes.Count > 0)
                        {
                            builder.Append('+');
                        }
                    }
                    return builder.ToString();
                }
            }
            public TypeDefinitionEntity? ContainingType { get; }
            public string Namespace { get; }
            public string Name { get; }
            public TypeAttributes Attributes { get; set; }
            public TypeEntity? BaseType { get; set; }

            public NamedElementList<GenericParameterEntity> GenericParameters { get; } = new();

            // COMPAT: Save the list of generic parameter constraints here to ensure we can match ILASM's emit order for generic parameter constraints exactly.
            public List<GenericParameterConstraintEntity> GenericParameterConstraints { get; } = new();

            public List<MethodDefinitionEntity> Methods { get; } = new();

            public List<MethodImplementationEntity> MethodImplementations { get; } = new();

            public List<FieldDefinitionEntity> Fields { get; } = new();

            public List<PropertyEntity> Properties { get; } = new();

            public List<EventEntity> Events { get; } = new();

            public List<InterfaceImplementationEntity> InterfaceImplementations { get; } = new();

            public string ReflectionNotation { get; }
        }

        public sealed class TypeReferenceEntity(EntityBase resolutionScope, string @namespace, string name) : TypeEntity, IHasReflectionNotation
        {
            public EntityBase ResolutionScope { get; } = resolutionScope;

            public string Namespace { get; } = @namespace;

            public string Name { get; } = name;

            public string ReflectionNotation { get; set; } = string.Empty;
        }

        public sealed class TypeSpecificationEntity(BlobBuilder signature) : TypeEntity
        {
            public BlobBuilder Signature { get; } = signature;
        }

        public sealed class GenericParameterEntity(GenericParameterAttributes attributes, string name) : EntityBase, INamed
        {
            public GenericParameterAttributes Attributes { get; } = attributes;

            public EntityBase? Owner { get; set; }

            public int Index { get; set; }

            public List<GenericParameterConstraintEntity> Constraints { get; } = new();

            public string Name { get; } = name;
        }

        public sealed class GenericParameterConstraintEntity(TypeEntity baseType) : EntityBase
        {
            public GenericParameterEntity? Owner { get; set; }

            public TypeEntity BaseType { get; } = baseType;
        }

        public sealed class ModuleReferenceEntity(string name) : EntityBase
        {
            public string Name { get; } = name;
        }

        public sealed class MethodDefinitionEntity(TypeDefinitionEntity containingType, string name) : EntityBase, IHasHandle
        {
            public TypeDefinitionEntity ContainingType { get; } = containingType;
            public string Name { get; } = name;

            public MethodAttributes MethodAttributes { get; set; }

            public List<ParameterEntity> Parameters { get; } = new();

            public NamedElementList<GenericParameterEntity> GenericParameters { get; } = new();

            // COMPAT: Save the list of generic parameter constraints here to ensure we can match ILASM's emit order for generic parameter constraints exactly.
            public List<GenericParameterConstraintEntity> GenericParameterConstraints { get; } = new();

            public SignatureHeader SignatureHeader { get; set; }

            public BlobBuilder? MethodSignature { get; set; }

            public StandaloneSignatureEntity? LocalsSignature { get; set; }

            public InstructionEncoder MethodBody { get; } = new(new BlobBuilder(), new ControlFlowBuilder());

            public MethodBodyAttributes BodyAttributes { get; set; }

            public int MaxStack { get; set; }

            public (ModuleReferenceEntity ModuleName, string? EntryPointName, MethodImportAttributes Attributes)? MethodImportInformation { get; set; }
            public MethodImplAttributes ImplementationAttributes { get; set; }
        }

        public sealed class ParameterEntity(ParameterAttributes attributes, string? name, BlobBuilder marshallingDescriptor, int sequence) : EntityBase
        {
            public ParameterAttributes Attributes { get; } = attributes;
            public string? Name { get; } = name;
            public BlobBuilder MarshallingDescriptor { get; set; } = marshallingDescriptor;
            public bool HasCustomAttributes { get; set; }
            public int Sequence { get; } = sequence;
        }

        public sealed class MemberReferenceEntity(EntityBase parent, string name, BlobBuilder signature) : EntityBase
        {
            public EntityBase Parent { get; } = parent;
            public string Name { get; } = name;
            public BlobBuilder Signature { get; } = signature;

            private readonly List<Blob> _placesToWriteResolvedHandle = new();

            public void RecordBlobToWriteResolvedHandle(Blob blob)
            {
                _placesToWriteResolvedHandle.Add(blob);
            }

            protected override void SetHandle(EntityHandle token)
            {
                base.SetHandle(token);
                // Now that we've set the handle, backpatch all the blobs that need to be updated.
                // This way we can determine the right token to use for the member reference
                // after we've processed all of the source code.
                foreach (var blob in _placesToWriteResolvedHandle)
                {
                    var writer = new BlobWriter(blob);
                    writer.WriteInt32(MetadataTokens.GetToken(token));
                }
            }
        }

        public sealed class StandaloneSignatureEntity(BlobBuilder signature) : EntityBase
        {
            public BlobBuilder Signature { get; } = signature;
        }

        public sealed class DeclarativeSecurityAttributeEntity(DeclarativeSecurityAction action, BlobBuilder permissionSet) : EntityBase
        {
            public EntityBase? Parent { get; set; }
            public DeclarativeSecurityAction Action { get; } = action;
            public BlobBuilder PermissionSet { get; } = permissionSet;
        }

        public sealed class CustomAttributeEntity(EntityBase constructor, BlobBuilder value) : EntityBase
        {
            public EntityBase? Owner { get; set; }
            public EntityBase Constructor { get; } = constructor;
            public BlobBuilder Value { get; } = value;
        }

        public sealed class MethodImplementationEntity(MethodDefinitionEntity methodBody, MemberReferenceEntity methodDeclaration) : EntityBase
        {
            public MethodDefinitionEntity MethodBody { get; } = methodBody;
            public MemberReferenceEntity MethodDeclaration { get; } = methodDeclaration;
        }

        public sealed class FieldDefinitionEntity(FieldAttributes attributes, TypeDefinitionEntity type, string name, BlobBuilder signature) : EntityBase
        {
            public FieldAttributes Attributes { get; } = attributes;
            public TypeDefinitionEntity ContainingType { get; } = type;
            public string Name { get; } = name;
            public BlobBuilder Signature { get; } = signature;

            public BlobBuilder? MarshallingDescriptor { get; set; }
            public string? DataDeclarationName { get; set; }
        }

        public sealed class InterfaceImplementationEntity(TypeDefinitionEntity type, TypeEntity interfaceType) : EntityBase
        {
            public TypeDefinitionEntity Type { get; } = type;
            public TypeEntity InterfaceType { get; } = interfaceType;
        }

        public sealed class EventEntity(EventAttributes attributes, TypeEntity type, string name) : EntityBase
        {
            public EventAttributes Attributes { get; } = attributes;
            public TypeEntity Type { get; } = type;
            public string Name { get; } = name;

            public List<(MethodSemanticsAttributes Semantic, EntityBase Method)> Accessors { get; } = new();
        }

        public sealed class PropertyEntity(PropertyAttributes attributes, BlobBuilder type, string name) : EntityBase
        {
            public PropertyAttributes Attributes { get; } = attributes;
            public BlobBuilder Type { get; } = type;
            public string Name { get; } = name;

            public List<(MethodSemanticsAttributes Semantic, EntityBase Method)> Accessors { get; } = new();
        }

        public sealed class FileEntity(string name) : EntityBase
        {
            public string Name { get; } = name;
            public bool HasMetadata { get; set; }
            public BlobBuilder? Hash { get; set; }
        }

        public sealed class ModuleEntity : EntityBase
        {
            public string? Name { get; set; }
        }

        public abstract class AssemblyOrRefEntity(string name) : EntityBase
        {
            public string Name { get; set; } = name;
            public Version? Version { get; set; }
            public string? Culture { get; set; }
            public BlobBuilder? PublicKeyOrToken { get; set; }
            public AssemblyFlags Flags { get; set; }
            public ProcessorArchitecture ProcessorArchitecture { get; set; }
        }

        public sealed class AssemblyEntity(string name) : AssemblyOrRefEntity(name)
        {
            public AssemblyHashAlgorithm HashAlgorithm { get; set; }
        }

        public sealed class AssemblyReferenceEntity(string name) : AssemblyOrRefEntity(name)
        {
            public BlobBuilder? Hash { get; set; }
        }

        public sealed class ManifestResourceEntity(string name, uint offset) : EntityBase
        {
            public string Name { get; } = name;
            public uint Offset { get; } = offset;
            public ManifestResourceAttributes Attributes { get; set; }
            public EntityBase? Implementation { get; set; }
        }

        public sealed class ExportedTypeEntity : EntityBase
        {
            public ExportedTypeEntity(string name, string @namespace, EntityBase? implementation)
            {
                Name = name;
                Namespace = @namespace;
                Implementation = implementation;
            }

            public string Name { get; }
            public string Namespace { get; }
            public TypeAttributes Attributes { get; set; }
            public EntityBase? Implementation { get; }

            public int TypeDefinitionId { get; set; }
        }
    }
}
