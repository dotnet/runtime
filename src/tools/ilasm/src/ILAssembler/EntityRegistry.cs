// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private readonly Dictionary<string, AssemblyReferenceEntity> _seenAssemblyRefs = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ModuleReferenceEntity> _seenModuleRefs = new();
        private readonly Dictionary<BlobBuilder, TypeSpecificationEntity> _seenTypeSpecs = new(new BlobBuilderContentEqualityComparer());
        private readonly Dictionary<BlobBuilder, StandaloneSignatureEntity> _seenStandaloneSignatures = new(new BlobBuilderContentEqualityComparer());
        private readonly Dictionary<string, FileEntity> _seenFiles = new();
        private readonly List<ManifestResourceEntity> _manifestResourceEntities = new();
        private readonly Dictionary<(ExportedTypeEntity? ContainingType, string Namespace, string Name), ExportedTypeEntity> _seenExportedTypes = new();
        private readonly List<TypeReferenceEntity> _typeReferences = new();
        private readonly List<MemberReferenceEntity> _memberReferences = new();
        private readonly Dictionary<(EntityBase, BlobBuilder), MethodSpecificationEntity> _seenMethodSpecs = new(new MethodSpecEqualityComparer());

        private sealed class BlobBuilderContentEqualityComparer : IEqualityComparer<BlobBuilder>
        {
            public bool Equals(BlobBuilder? x, BlobBuilder? y)
            {
                if (x is null && y is null)
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                return x.ContentEquals(y!);
            }

            public int GetHashCode(BlobBuilder obj)
            {
                HashCode hash = default;
                foreach (var b in obj.GetBlobs())
                {
                    hash.AddBytes(b.GetBytes());
                }
                return hash.ToHashCode();
            }
        }

        private sealed class MethodSpecEqualityComparer : IEqualityComparer<(EntityBase, BlobBuilder)>
        {
            public bool Equals((EntityBase, BlobBuilder) x, (EntityBase, BlobBuilder) y)
            {
                return x.Item1 == y.Item1 && x.Item2.ContentEquals(y.Item2);
            }

            public int GetHashCode((EntityBase, BlobBuilder) obj)
            {
                return (obj.Item1.GetHashCode(), obj.Item2.Count).GetHashCode();
            }
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

        public IReadOnlyList<EntityBase> GetSeenEntities(TableIndex table)
        {
            if (_seenEntities.TryGetValue(table, out var entities))
            {
                return entities;
            }
            return Array.Empty<EntityBase>();
        }

        public void WriteContentTo(MetadataBuilder builder, BlobBuilder ilStream, IReadOnlyDictionary<string, int> mappedFieldDataNames)
        {
            // Set the assembly handle early since DeclarativeSecurityAttribute needs it
            // The assembly definition handle is always row 1 (there's only ever one assembly per module)
            // Assembly table token = 0x20000001
            if (Assembly is not null)
            {
                ((IHasHandle)Assembly).SetHandle(MetadataTokens.EntityHandle(0x20000001));
            }

            List<GenericParameterEntity> allGenericParams = [];
            List<GenericParameterConstraintEntity> allGenericConstraints = [];

            // Now that we've seen all of the entities, we can write them out in the correct order.
            // Record the entities in the correct order so they are assigned handles.
            // After this, we'll write out the content of the entities in the correct order.
            foreach (TypeDefinitionEntity type in GetSeenEntities(TableIndex.TypeDef))
            {
                // Record entries for members defined in list columns
                foreach (var method in type.Methods)
                {
                    RecordEntityInTable(TableIndex.MethodDef, method);
                    foreach (var param in method.Parameters)
                    {
                        // COMPAT: Always emit Param rows for explicit parameters (sequence > 0)
                        // to match native ilasm behavior. For the return type parameter (sequence 0),
                        // only emit if it has attributes, a name, or associated metadata.
                        if (param.Sequence > 0
                            || param.Name is not null
                            || param.Attributes != ParameterAttributes.None
                            || param.MarshallingDescriptor.Count != 0
                            || param.HasCustomAttributes
                            || param.HasConstant)
                        {
                            RecordEntityInTable(TableIndex.Param, param);
                        }
                    }

                    // Don't record generic parameters or constraints for methods.
                    // Entries need to be sorted by the value of the TypeOrMethodDef coded index,
                    // which can intermix TypeDef and MethodDef generic parameters based on the order of the TypeDef and MethodDef entries.
                    // We'll record these after processing all TypeDefs and MethodDefs.
                    allGenericParams.AddRange(method.GenericParameters);
                    allGenericConstraints.AddRange(method.GenericParameterConstraints);
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

                // Don't record generic parameters or constraints for methods.
                // Entries need to be sorted by the value of the TypeOrMethodDef coded index,
                // which can intermix TypeDef and MethodDef generic parameters based on the order of the TypeDef and MethodDef entries.
                // We'll record these after processing all TypeDefs and MethodDefs.
                allGenericParams.AddRange(type.GenericParameters);
                allGenericConstraints.AddRange(type.GenericParameterConstraints);
            }

            // Now that we've processed all TypeDefs and their corresponding GenericParam and GenericParamConstraint entries,
            // we can process the GenericParam and GenericParamConstrain entries
            // and maintain ordering requirements for TypeOrMethodDef coded index values.
            allGenericParams.Sort((gp1, gp2) =>
            {
                var owner1 = gp1.Owner!.Handle;
                var owner2 = gp2.Owner!.Handle;
                int row1 = MetadataTokens.GetRowNumber(owner1);
                int row2 = MetadataTokens.GetRowNumber(owner2);
                int tag1 = owner1.Kind == HandleKind.TypeDefinition ? 0 : 1;
                int tag2 = owner2.Kind == HandleKind.TypeDefinition ? 0 : 1;
                int compare = (row1 << 1 | tag1).CompareTo(row2 << 1 | tag2);
                if (compare != 0)
                {
                    return compare;
                }
                return gp1.Index.CompareTo(gp2.Index);
            });

            foreach (GenericParameterEntity genericParam in allGenericParams)
            {
                // GenericParam index is stored as a 2-byte value; skip params beyond the limit
                if (genericParam.Index > ushort.MaxValue)
                    continue;

                RecordEntityInTable(TableIndex.GenericParam, genericParam);
            }

            allGenericConstraints.Sort((c1, c2) =>
            {
                var owner1 = c1.Owner!.Handle;
                var owner2 = c2.Owner!.Handle;
                int row1 = MetadataTokens.GetRowNumber(owner1);
                int row2 = MetadataTokens.GetRowNumber(owner2);
                return row1.CompareTo(row2);
            });

            foreach (GenericParameterConstraintEntity constraint in allGenericConstraints)
            {
                RecordEntityInTable(TableIndex.GenericParamConstraint, constraint);
            }

            // Resolve TypeRef entities to local TypeDef entities when possible.
            // This must happen before MemberRef resolution so that MemberRef parents
            // that point to local types already have TypeDef handles.
            ResolveTypeReferences();

            // Create a signature rewriter that remaps PseudoHandle-based TypeRef coded indices
            // in blobs to the resolved real handles via list index lookup.
            SignatureRewriter signatureRewriter = new(_typeReferences);

            foreach (MemberReferenceEntity memberReferenceEntity in _memberReferences)
            {
                ResolveAndRecordMemberReference(memberReferenceEntity);
            }

            // Now that we've recorded all of the entities that wouldn't have had handles before,
            // we can start writing out the content of the entities.
            builder.AddModule(0, Module.Name is null ? default : builder.GetOrAddString(Module.Name), builder.GetOrAddGuid(Guid.NewGuid()), default, default);

            foreach (TypeReferenceEntity type in GetSeenEntities(TableIndex.TypeRef))
            {
                EntityBase resolutionScope = type.ResolutionScope;
                // For nested TypeRefs whose outer type was resolved to a TypeDef,
                // use the resolved handle's TypeDef handle for the resolution scope.
                EntityHandle scopeHandle = resolutionScope switch
                {
                    FakeTypeEntity fakeScope => fakeScope.ResolutionScopeColumnHandle,
                    TypeReferenceEntity { Handle.Kind: HandleKind.TypeDefinition } resolvedOuter => resolvedOuter.Handle,
                    _ => resolutionScope.Handle
                };
                builder.AddTypeReference(
                    scopeHandle,
                    builder.GetOrAddString(type.Namespace),
                    builder.GetOrAddString(type.Name));
            }

            for (int i = 0; i < GetSeenEntities(TableIndex.TypeDef).Count; i++)
            {
                TypeDefinitionEntity type = (TypeDefinitionEntity)GetSeenEntities(TableIndex.TypeDef)[i];
                builder.AddTypeDefinition(
                    type.Attributes,
                    builder.GetOrAddString(type.Namespace),
                    builder.GetOrAddString(type.Name),
                    type.BaseType is null ? default : type.BaseType.Handle,
                    GetFieldHandleForList(type.Fields, GetSeenEntities(TableIndex.TypeDef), type => ((TypeDefinitionEntity)type).Fields, i),
                    GetMethodHandleForList(type.Methods, GetSeenEntities(TableIndex.TypeDef), type => ((TypeDefinitionEntity)type).Methods, i));

                if (type.Events.Count > 0)
                {
                    builder.AddEventMap(
                        (TypeDefinitionHandle)type.Handle,
                        GetEventHandleForList(type.Events, GetSeenEntities(TableIndex.TypeDef), type => ((TypeDefinitionEntity)type).Events, i));
                }
                if (type.Properties.Count > 0)
                {
                    builder.AddPropertyMap(
                        (TypeDefinitionHandle)type.Handle,
                        GetPropertyHandleForList(type.Properties, GetSeenEntities(TableIndex.TypeDef), type => ((TypeDefinitionEntity)type).Properties, i));
                }

                if (type.PackingSize is not null || type.ClassSize is not null
                    || (type.Attributes & TypeAttributes.LayoutMask) is TypeAttributes.ExplicitLayout)
                {
                    builder.AddTypeLayout(
                        (TypeDefinitionHandle)type.Handle,
                        (ushort)(type.PackingSize ?? 0),
                        (uint)(type.ClassSize ?? 0));
                }

                if (type.ContainingType is not null)
                {
                    builder.AddNestedType((TypeDefinitionHandle)type.Handle, (TypeDefinitionHandle)type.ContainingType.Handle);
                }
            }

            foreach (FieldDefinitionEntity fieldDef in GetSeenEntities(TableIndex.Field))
            {
                var fieldAttributes = fieldDef.Attributes;
                if (fieldDef.HasConstant)
                {
                    fieldAttributes |= FieldAttributes.HasDefault;
                }
                if (fieldDef.MarshallingDescriptor is { Count: > 0 })
                {
                    fieldAttributes |= FieldAttributes.HasFieldMarshal;
                }
                if (fieldDef.DataDeclarationName is not null && mappedFieldDataNames.ContainsKey(fieldDef.DataDeclarationName))
                {
                    fieldAttributes |= FieldAttributes.HasFieldRVA;
                }
                builder.AddFieldDefinition(
                    fieldAttributes,
                    builder.GetOrAddString(fieldDef.Name),
                    fieldDef.Signature!.Count == 0 ? default : builder.GetOrAddBlob(RewriteSignatureBlob(fieldDef.Signature, signatureRewriter)));

                if (fieldDef.Offset is not null)
                {
                    builder.AddFieldLayout((FieldDefinitionHandle)fieldDef.Handle, fieldDef.Offset.Value);
                }

                if (fieldDef.DataDeclarationName is not null && mappedFieldDataNames.TryGetValue(fieldDef.DataDeclarationName, out int dataOffset))
                {
                    builder.AddFieldRelativeVirtualAddress((FieldDefinitionHandle)fieldDef.Handle, dataOffset);
                }

                if (fieldDef.MarshallingDescriptor is not null)
                {
                    builder.AddMarshallingDescriptor(fieldDef.Handle, builder.GetOrAddBlob(fieldDef.MarshallingDescriptor));
                }

                if (fieldDef.HasConstant)
                {
                    builder.AddConstant(fieldDef.Handle, fieldDef.ConstantValue);
                }
            }

            var bodyStreamEncoder = new MethodBodyStreamEncoder(ilStream);

            for (int i = 0; i < GetSeenEntities(TableIndex.MethodDef).Count; i++)
            {
                MethodDefinitionEntity methodDef = (MethodDefinitionEntity)GetSeenEntities(TableIndex.MethodDef)[i];

                int bodyOffset = -1;
                if (methodDef.MethodBody.CodeBuilder.Count != 0)
                {
                    StandaloneSignatureHandle localsSigHandle = methodDef.LocalsSignature is not null
                        ? (StandaloneSignatureHandle)methodDef.LocalsSignature.Handle
                        : default;
                    try
                    {
                        bodyOffset = bodyStreamEncoder.AddMethodBody(
                            methodDef.MethodBody,
                            methodDef.MaxStack,
                            localsSigHandle,
                            methodDef.BodyAttributes);
                    }
                    catch (InvalidOperationException)
                    {
                        // Method has unresolved labels or other body errors.
                        // Write raw IL bytes as a fallback so the PE can still be emitted
                        // (error diagnostics are already recorded).
                        bodyOffset = ilStream.Count;
                        methodDef.MethodBody.CodeBuilder.WriteContentTo(ilStream);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Exception handler regions have invalid ranges (e.g., from parse
                        // errors that produced malformed control flow).
                        bodyOffset = ilStream.Count;
                        methodDef.MethodBody.CodeBuilder.WriteContentTo(ilStream);
                    }
                }

                var methodAttributes = methodDef.MethodAttributes;
                if (methodDef.MethodImportInformation is not null)
                {
                    methodAttributes |= MethodAttributes.PinvokeImpl;
                }
                builder.AddMethodDefinition(
                    methodAttributes,
                    methodDef.ImplementationAttributes,
                    builder.GetOrAddString(methodDef.Name),
                    builder.GetOrAddBlob(RewriteSignatureBlob(methodDef.MethodSignature!, signatureRewriter)),
                    bodyOffset,
                    GetParameterHandleForList(methodDef.Parameters, GetSeenEntities(TableIndex.MethodDef), method => ((MethodDefinitionEntity)method).Parameters, i));

                if (methodDef.MethodImportInformation is not null)
                {
                    builder.AddMethodImport(
                        (MethodDefinitionHandle)methodDef.Handle,
                        methodDef.MethodImportInformation.Value.Attributes,
                        methodDef.MethodImportInformation.Value.EntryPointName is null ? default : builder.GetOrAddString(methodDef.MethodImportInformation.Value.EntryPointName),
                        (ModuleReferenceHandle)methodDef.MethodImportInformation.Value.ModuleName.Handle);
                }
            }

            foreach (ParameterEntity param in GetSeenEntities(TableIndex.Param))
            {
                var paramAttributes = param.Attributes;
                if (param.HasConstant)
                {
                    paramAttributes |= ParameterAttributes.HasDefault;
                }
                if (param.MarshallingDescriptor.Count != 0)
                {
                    paramAttributes |= ParameterAttributes.HasFieldMarshal;
                }
                builder.AddParameter(
                    paramAttributes,
                    param.Name is null ? default : builder.GetOrAddString(param.Name),
                    param.Sequence);

                if (param.MarshallingDescriptor.Count != 0)
                {
                    builder.AddMarshallingDescriptor(param.Handle, builder.GetOrAddBlob(param.MarshallingDescriptor));
                }

                if (param.HasConstant)
                {
                    builder.AddConstant(param.Handle, param.ConstantValue);
                }
            }

            foreach (InterfaceImplementationEntity impl in GetSeenEntities(TableIndex.InterfaceImpl))
            {
                builder.AddInterfaceImplementation(
                    (TypeDefinitionHandle)impl.Type.Handle,
                    impl.InterfaceType is FakeTypeEntity fakeType ? fakeType.TypeColumnHandle : impl.InterfaceType.Handle);
            }

            foreach (MethodImplementationEntity impl in GetSeenEntities(TableIndex.MethodImpl))
            {
                builder.AddMethodImplementation(
                    (TypeDefinitionHandle)impl.MethodBody.ContainingType.Handle,
                    impl.MethodBody.Handle,
                    impl.MethodDeclaration.Handle);
            }

            foreach (MemberReferenceEntity memberRef in _memberReferences)
            {
                // Skip member references that were resolved to local MethodDef or FieldDef tokens.
                if (memberRef.Handle.Kind is HandleKind.MethodDefinition or HandleKind.FieldDefinition)
                {
                    continue;
                }
                builder.AddMemberReference(
                    memberRef.Parent.Handle,
                    builder.GetOrAddString(memberRef.Name),
                    builder.GetOrAddBlob(RewriteSignatureBlob(memberRef.Signature, signatureRewriter)));
            }

            foreach (DeclarativeSecurityAttributeEntity declSecurity in GetSeenEntities(TableIndex.DeclSecurity))
            {
                builder.AddDeclarativeSecurityAttribute(
                    declSecurity.Parent?.Handle ?? default,
                    declSecurity.Action,
                    builder.GetOrAddBlob(declSecurity.PermissionSet));
            }

            foreach (CustomAttributeEntity customAttr in GetSeenEntities(TableIndex.CustomAttribute))
            {
                EntityHandle parent = customAttr.Owner switch
                {
                    AssemblyEntity => EntityHandle.AssemblyDefinition,
                    ModuleEntity => EntityHandle.ModuleDefinition,
                    { Handle: var h } => h,
                    _ => default
                };
                builder.AddCustomAttribute(
                    parent,
                    customAttr.Constructor.Handle,
                    builder.GetOrAddBlob(customAttr.Value));
            }

            foreach (StandaloneSignatureEntity standaloneSig in GetSeenEntities(TableIndex.StandAloneSig))
            {
                builder.AddStandaloneSignature(
                    builder.GetOrAddBlob(RewriteSignatureBlob(standaloneSig.Signature, signatureRewriter)));
            }

            foreach (EventEntity evt in GetSeenEntities(TableIndex.Event))
            {
                builder.AddEvent(
                    evt.Attributes,
                    builder.GetOrAddString(evt.Name),
                    evt.Type.Handle);

                foreach (var accessor in evt.Accessors)
                {
                    if (accessor.Method.Handle.Kind == HandleKind.MethodDefinition)
                    {
                        builder.AddMethodSemantics(evt.Handle, accessor.Semantic, (MethodDefinitionHandle)accessor.Method.Handle);
                    }
                }
            }

            foreach (PropertyEntity prop in GetSeenEntities(TableIndex.Property))
            {
                builder.AddProperty(
                    prop.Attributes,
                    builder.GetOrAddString(prop.Name),
                    builder.GetOrAddBlob(RewriteSignatureBlob(prop.Type, signatureRewriter)));

                foreach (var accessor in prop.Accessors)
                {
                    if (accessor.Method.Handle.Kind == HandleKind.MethodDefinition)
                    {
                        builder.AddMethodSemantics(prop.Handle, accessor.Semantic, (MethodDefinitionHandle)accessor.Method.Handle);
                    }
                }

                if (prop.HasConstant)
                {
                    builder.AddConstant(prop.Handle, prop.ConstantValue);
                }
            }

            foreach (ModuleReferenceEntity moduleRef in GetSeenEntities(TableIndex.ModuleRef))
            {
                builder.AddModuleReference(builder.GetOrAddString(moduleRef.Name));
            }

            foreach (AssemblyReferenceEntity asmRef in GetSeenEntities(TableIndex.AssemblyRef))
            {
                builder.AddAssemblyReference(
                    builder.GetOrAddString(asmRef.Name),
                    asmRef.Version ?? new Version(0, 0, 0, 0),
                    asmRef.Culture is null ? default : builder.GetOrAddString(asmRef.Culture),
                    asmRef.PublicKeyOrToken is null ? default : builder.GetOrAddBlob(asmRef.PublicKeyOrToken),
                    asmRef.Flags,
                    asmRef.Hash is null ? default : builder.GetOrAddBlob(asmRef.Hash));
            }

            foreach (TypeSpecificationEntity typeSpec in GetSeenEntities(TableIndex.TypeSpec))
            {
                builder.AddTypeSpecification(builder.GetOrAddBlob(RewriteTypeSpecBlob(typeSpec.Signature, signatureRewriter)));
            }

            if (Assembly is not null)
            {
                // Combine the base flags with the architecture bits
                var assemblyFlags = Assembly.Flags | (AssemblyFlags)((int)Assembly.ProcessorArchitecture << 4);
                builder.AddAssembly(
                    builder.GetOrAddString(Assembly.Name),
                    Assembly.Version ?? new Version(0, 0, 0, 0),
                    Assembly.Culture is null ? default : builder.GetOrAddString(Assembly.Culture),
                    Assembly.PublicKeyOrToken is null ? default : builder.GetOrAddBlob(Assembly.PublicKeyOrToken),
                    assemblyFlags,
                    Assembly.HashAlgorithm);
            }

            foreach (FileEntity file in GetSeenEntities(TableIndex.File))
            {
                builder.AddAssemblyFile(
                    builder.GetOrAddString(file.Name),
                    file.Hash is not null ? builder.GetOrAddBlob(file.Hash) : default,
                    file.HasMetadata);
            }

            foreach (ExportedTypeEntity exportedType in GetSeenEntities(TableIndex.ExportedType))
            {
                // Implementation must be a valid handle type: AssemblyFileHandle, AssemblyReferenceHandle, or ExportedTypeHandle
                // COMPAT: If implementation is null, skip emitting this exported type
                if (exportedType.Implementation is null)
                {
                    continue;
                }
                builder.AddExportedType(
                    exportedType.Attributes,
                    builder.GetOrAddString(exportedType.Namespace),
                    builder.GetOrAddString(exportedType.Name),
                    exportedType.Implementation.Handle,
                    exportedType.TypeDefinitionId);
            }

            foreach (ManifestResourceEntity resource in GetSeenEntities(TableIndex.ManifestResource))
            {
                builder.AddManifestResource(
                    resource.Attributes,
                    builder.GetOrAddString(resource.Name),
                    resource.Implementation?.Handle ?? default,
                    resource.Offset);
            }

            foreach (MethodSpecificationEntity methodSpec in GetSeenEntities(TableIndex.MethodSpec))
            {
                builder.AddMethodSpecification(methodSpec.Parent.Handle, builder.GetOrAddBlob(RewriteMethodSpecBlob(methodSpec.Signature, signatureRewriter)));
            }

            foreach (GenericParameterEntity genericParam in GetSeenEntities(TableIndex.GenericParam))
            {
                // GenericParam index is stored as a 2-byte value; skip params beyond the limit
                if (genericParam.Index > ushort.MaxValue)
                    continue;
                builder.AddGenericParameter(
                    genericParam.Owner!.Handle,
                    genericParam.Attributes,
                    builder.GetOrAddString(genericParam.Name),
                    genericParam.Index);
            }

            foreach (GenericParameterConstraintEntity constraint in GetSeenEntities(TableIndex.GenericParamConstraint))
            {
                builder.AddGenericParameterConstraint(
                    (GenericParameterHandle)constraint.Owner!.Handle,
                    constraint.BaseType.Handle);
            }

            static FieldDefinitionHandle GetFieldHandleForList(IReadOnlyList<EntityBase> list, IReadOnlyList<EntityBase> listOwner, Func<EntityBase, IReadOnlyList<EntityBase>> getList, int ownerIndex)
                => (FieldDefinitionHandle)GetHandleForList(list, listOwner, getList, ownerIndex, TableIndex.Field);

            static MethodDefinitionHandle GetMethodHandleForList(IReadOnlyList<EntityBase> list, IReadOnlyList<EntityBase> listOwner, Func<EntityBase, IReadOnlyList<EntityBase>> getList, int ownerIndex)
                => (MethodDefinitionHandle)GetHandleForList(list, listOwner, getList, ownerIndex, TableIndex.MethodDef);

            static PropertyDefinitionHandle GetPropertyHandleForList(IReadOnlyList<EntityBase> list, IReadOnlyList<EntityBase> listOwner, Func<EntityBase, IReadOnlyList<EntityBase>> getList, int ownerIndex)
                => (PropertyDefinitionHandle)GetHandleForList(list, listOwner, getList, ownerIndex, TableIndex.Property);

            static EventDefinitionHandle GetEventHandleForList(IReadOnlyList<EntityBase> list, IReadOnlyList<EntityBase> listOwner, Func<EntityBase, IReadOnlyList<EntityBase>> getList, int ownerIndex)
                => (EventDefinitionHandle)GetHandleForList(list, listOwner, getList, ownerIndex, TableIndex.Event);

            static ParameterHandle GetParameterHandleForList(IReadOnlyList<EntityBase> list, IReadOnlyList<EntityBase> listOwner, Func<EntityBase, IReadOnlyList<EntityBase>> getList, int ownerIndex)
                => (ParameterHandle)GetHandleForList(list, listOwner, getList, ownerIndex, TableIndex.Param);

            static EntityHandle GetHandleForList(IReadOnlyList<EntityBase> list, IReadOnlyList<EntityBase> listOwner, Func<EntityBase, IReadOnlyList<EntityBase>> getList, int ownerIndex, TableIndex tokenType)
            {
                // Return the first entry in the list that has a handle.
                // If no item has a handle, return the start of the next list.
                // If there is no next list, return one past the end of the previous list.
                foreach (var item in list)
                {
                    if (!item.Handle.IsNil)
                    {
                        return item.Handle;
                    }
                }

                for (int i = ownerIndex + 1; i < listOwner.Count; i++)
                {
                    var otherList = getList(listOwner[i]);
                    foreach (var item in otherList)
                    {
                        if (!item.Handle.IsNil)
                        {
                            return item.Handle;
                        }
                    }
                }

                for (int i = ownerIndex - 1; i >= 0; i--)
                {
                    var otherList = getList(listOwner[i]);
                    if (otherList.Count != 0 && !otherList[otherList.Count - 1].Handle.IsNil)
                    {
                        return MetadataTokens.EntityHandle(tokenType, MetadataTokens.GetRowNumber(otherList[otherList.Count - 1].Handle) + 1);
                    }
                }
                // If all lists are empty, return row 1 (first potential entry).
                // ECMA-335 metadata rows are 1-indexed, so row 0 is invalid.
                return MetadataTokens.EntityHandle(tokenType, 1);
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

        private TypeReferenceEntity ResolveFromCoreAssembly(string typeName)
        {
            // Check for assembly refs in order of preference then fall back to creating mscorlib if none found
            AssemblyReferenceEntity coreAsmRef = GetCoreLibAssemblyReference();
            return GetOrCreateTypeReference(coreAsmRef, new TypeName(null, typeName));
        }

        public AssemblyReferenceEntity GetCoreLibAssemblyReference()
        {
            return FindAssemblyReference("System.Private.CoreLib")
                ?? FindAssemblyReference("System.Runtime")
                ?? FindAssemblyReference("mscorlib")
                ?? FindAssemblyReference("netstandard")
                ?? GetOrCreateAssemblyReference("mscorlib", new Version(0, 0, 0, 0), culture: null, publicKeyOrToken: null, 0, ProcessorArchitecture.None);
        }

        private static bool IsCoreLibAssemblyName(string name)
        {
            return name is "mscorlib" or "System.Runtime" or "System.Private.CoreLib" or "netstandard";
        }

        public interface IHasHandle
        {
            EntityHandle Handle { get; }
            void SetHandle(EntityHandle token);
        }

        public void RecordEntityInTable(TableIndex table, EntityBase entity)
        {
            if (!_seenEntities.TryGetValue(table, out List<EntityBase>? entities))
            {
                _seenEntities[table] = entities = new List<EntityBase>();
            }
            entities.Add(entity);
            ((IHasHandle)entity).SetHandle(MetadataTokens.EntityHandle(table, entities.Count));
        }

        private TEntity GetOrCreateEntity<TKey, TEntity>(TKey key, TableIndex table, Dictionary<TKey, TEntity> cache, Func<TKey, TEntity> constructor, Action<TEntity> onCreate)
            where TKey : notnull
            where TEntity : EntityBase
        {
            if (cache.TryGetValue(key, out TEntity? entity))
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
            return GetOrCreateEntity(name, TableIndex.AssemblyRef, _seenAssemblyRefs, _ => new(name), onCreateAssemblyReference);
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
                if (rowNumber >= 1 && rowNumber <= entity.Count)
                {
                    return entity[rowNumber - 1];
                }
            }
            // Row entry does not exist. Use our FakeTypeEntity type to record the invalid handle.
            return new FakeTypeEntity(entityHandle);
        }

        public TypeReferenceEntity GetOrCreateTypeReference(EntityBase resolutionContext, TypeName name)
        {
            // COMPAT: When the resolution scope is a corelib assembly ref (mscorlib, System.Runtime, etc.),
            // redirect to the preferred corelib assembly ref to match native ilasm behavior.
            // Native ilasm always uses the preferred corelib for well-known types.
            if (resolutionContext is AssemblyReferenceEntity asmRefScope && IsCoreLibAssemblyName(asmRefScope.Name))
            {
                var preferredCoreLib = GetCoreLibAssemblyReference();
                if (preferredCoreLib != asmRefScope)
                {
                    resolutionContext = preferredCoreLib;
                }
            }

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
            while (allTypeNames.Count > 0)
            {
                var typeName = allTypeNames.Pop();
                var key = (scope, typeName.Namespace, typeName.Name);
                if (!_seenTypeRefs.TryGetValue(key, out TypeReferenceEntity? typeRef))
                {
                    typeRef = new TypeReferenceEntity(scope, typeName.Namespace, typeName.Name);
                    _seenTypeRefs.Add(key, typeRef);
                    _typeReferences.Add(typeRef);
                    typeRef.PseudoHandle = MetadataTokens.TypeReferenceHandle(_typeReferences.Count);

                    StringBuilder builder = new(typeRef.Namespace.Length + typeRef.Name.Length + 1);
                    builder.AppendFormat("{0}.{1}", typeRef.Namespace, typeRef.Name);
                    if (resolutionContext is AssemblyReferenceEntity asmRef)
                    {
                        var assemblyNameInfo = new AssemblyNameInfo(
                            asmRef.Name,
                            asmRef.Version,
                            string.IsNullOrEmpty(asmRef.Culture) ? null : asmRef.Culture,
                            asmRef.PublicKeyOrToken is null ? AssemblyNameFlags.None : AssemblyNameFlags.PublicKey,
                            asmRef.PublicKeyOrToken?.ToImmutableArray() ?? []);
                        builder.Append(", ");
                        builder.Append(assemblyNameInfo.FullName);
                    }
                    typeRef.ReflectionNotation = builder.ToString();
                }
                scope = typeRef;
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

        public MemberReferenceEntity CreateLazilyRecordedMemberReference(TypeEntity containingType, string name, BlobBuilder signature)
        {
            var entity = new MemberReferenceEntity(containingType, name, signature);
            _memberReferences.Add(entity);
            return entity;
        }

        private sealed class SignatureRewriter : ISignatureTypeProvider<SignatureRewriter.BlobOrHandle, SignatureRewriter.EmptyGenericContext>
        {
            private readonly List<TypeReferenceEntity>? _typeReferences;

            public SignatureRewriter() { }

            public SignatureRewriter(List<TypeReferenceEntity> typeReferences)
            {
                _typeReferences = typeReferences;
            }
            public readonly struct BlobOrHandle
            {
                public BlobOrHandle(BlobBuilder? blob)
                {
                    Blob = blob;
                    Handle = default;
                    HandleIsValueType = false;
                }

                public BlobOrHandle(EntityHandle handle, bool handleIsValueType)
                {
                    Blob = default;
                    Handle = handle;
                    HandleIsValueType = handleIsValueType;
                }

                private BlobBuilder? Blob { get; }
                public EntityHandle Handle { get; }
                public bool HandleIsValueType { get; }

                public static implicit operator BlobOrHandle(BlobBuilder blob) => new(blob);
                public static implicit operator BlobBuilder(BlobOrHandle blobOrHandle)
                {
                    if (blobOrHandle.Blob is not null)
                    {
                        return blobOrHandle.Blob;
                    }
                    var signatureTypeEncoder = new SignatureTypeEncoder(new BlobBuilder());
                    signatureTypeEncoder.Type(blobOrHandle.Handle, blobOrHandle.HandleIsValueType);
                    return signatureTypeEncoder.Builder;
                }

                public void WriteBlobTo(BlobBuilder builder)
                {
                    ((BlobBuilder)this).WriteContentTo(builder);
                }
            }

            public BlobOrHandle GetArrayType(BlobOrHandle elementType, ArrayShape shape)
            {
                var encoder = new ArrayShapeEncoder(elementType);
                encoder.Shape(shape.Rank, shape.Sizes, shape.LowerBounds);
                return encoder.Builder;
            }

            public BlobOrHandle GetByReferenceType(BlobOrHandle elementType)
            {
                var paramEncoder = new ParameterTypeEncoder(new BlobBuilder());
                paramEncoder.Type(isByRef: true);
                elementType.WriteBlobTo(paramEncoder.Builder);
                return paramEncoder.Builder;
            }

            public BlobOrHandle GetFunctionPointerType(MethodSignature<BlobOrHandle> signature)
            {
                var sig = new SignatureTypeEncoder(new BlobBuilder());
                sig.FunctionPointer(signature.Header.CallingConvention, (FunctionPointerAttributes)signature.Header.Attributes, signature.GenericParameterCount)
                    .Parameters(signature.ParameterTypes.Length, out var retTypeBuilder, out var parametersEncoder);
                signature.ReturnType.WriteBlobTo(retTypeBuilder.Builder);
                for (int i = 0; i < signature.ParameterTypes.Length; i++)
                {
                    if (i == signature.RequiredParameterCount)
                    {
                        parametersEncoder.StartVarArgs();
                    }
                    BlobBuilder paramType = signature.ParameterTypes[i];
                    paramType.WriteContentTo(parametersEncoder.AddParameter().Builder);
                }
                return sig.Builder;
            }

            public BlobOrHandle GetGenericInstantiation(BlobOrHandle genericType, ImmutableArray<BlobOrHandle> typeArguments)
            {
                var encoder = new SignatureTypeEncoder(new BlobBuilder());
                var parameterEncoder = encoder.GenericInstantiation(genericType.Handle, typeArguments.Length, genericType.HandleIsValueType);
                foreach (var typeArg in typeArguments)
                {
                    typeArg.WriteBlobTo(parameterEncoder.AddArgument().Builder);
                }
                return encoder.Builder;
            }

            public BlobOrHandle GetGenericMethodParameter(EmptyGenericContext genericContext, int index)
            {
                var encoder = new SignatureTypeEncoder(new BlobBuilder());
                encoder.GenericMethodTypeParameter(index);
                return encoder.Builder;
            }
            public BlobOrHandle GetGenericTypeParameter(EmptyGenericContext genericContext, int index)
            {
                var encoder = new SignatureTypeEncoder(new BlobBuilder());
                encoder.GenericTypeParameter(index);
                return encoder.Builder;
            }
            public BlobOrHandle GetModifiedType(BlobOrHandle modifier, BlobOrHandle unmodifiedType, bool isRequired)
            {
                var builder = new BlobBuilder();
                if (isRequired)
                {
                    builder.WriteByte((byte)SignatureTypeCode.RequiredModifier);
                }
                else
                {
                    builder.WriteByte((byte)SignatureTypeCode.OptionalModifier);
                }
                // The modifier is a TypeDefOrRefOrSpec coded index (no CLASS/VALUETYPE prefix).
                builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(modifier.Handle));
                unmodifiedType.WriteBlobTo(builder);
                return builder;
            }
            public BlobOrHandle GetPinnedType(BlobOrHandle elementType)
            {
                var builder = new BlobBuilder();
                builder.WriteByte((byte)SignatureTypeCode.Pinned);
                elementType.WriteBlobTo(builder);
                return builder;
            }
            public BlobOrHandle GetPointerType(BlobOrHandle elementType)
            {
                var paramEncoder = new ParameterTypeEncoder(new BlobBuilder());
                paramEncoder.Type().Pointer();
                elementType.WriteBlobTo(paramEncoder.Builder);
                return paramEncoder.Builder;
            }
            public BlobOrHandle GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                var paramEncoder = new ParameterTypeEncoder(new BlobBuilder());
                if ((int)typeCode >= 2 && (int)typeCode <= 14)
                {
                    paramEncoder.Type().PrimitiveType(typeCode);
                }
                else
                {
                    // Invalid type code from malformed signature - write raw byte
                    paramEncoder.Builder.WriteByte((byte)typeCode);
                }
                return paramEncoder.Builder;
            }
            public BlobOrHandle GetSZArrayType(BlobOrHandle elementType)
            {
                var paramEncoder = new ParameterTypeEncoder(new BlobBuilder());
                paramEncoder.Type().SZArray();
                elementType.WriteBlobTo(paramEncoder.Builder);
                return paramEncoder.Builder;
            }
            public BlobOrHandle GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                return new BlobOrHandle(handle, rawTypeKind == (byte)SignatureTypeKind.ValueType);
            }
            public BlobOrHandle GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                bool isValueType = rawTypeKind == (byte)SignatureTypeKind.ValueType;
                if (_typeReferences is not null)
                {
                    int row = MetadataTokens.GetRowNumber(handle);
                    if (row >= 1 && row <= _typeReferences.Count)
                    {
                        return new BlobOrHandle(_typeReferences[row - 1].Handle, isValueType);
                    }
                }
                return new BlobOrHandle(handle, isValueType);
            }

            public BlobOrHandle GetTypeFromSpecification(MetadataReader reader, EmptyGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                return new BlobOrHandle(handle, rawTypeKind == (byte)SignatureTypeKind.ValueType);
            }

            public struct EmptyGenericContext { }

        }

        private void ResolveTypeReferences()
        {
            // Resolve TypeRef entities that refer to locally-defined types.
            // For each TypeRef, if the resolution scope matches the current assembly
            // and a matching TypeDef exists, assign the TypeDef handle to the TypeRef entity.
            // Otherwise, record it in the TypeRef table with a real TypeRef handle.
            // Process outermost types first so nested types can check if their parent was resolved.
            foreach (TypeReferenceEntity typeRef in _typeReferences)
            {
                if (TryResolveTypeReferenceToDefinition(typeRef))
                {
                    continue;
                }
                RecordEntityInTable(TableIndex.TypeRef, typeRef);
            }
        }

        private bool TryResolveTypeReferenceToDefinition(TypeReferenceEntity typeRef)
        {
            EntityBase resolutionScope = typeRef.ResolutionScope;

            // Nested TypeRef: resolution scope is another TypeRef.
            // If the outer type was resolved to a TypeDef, look up the nested type.
            if (resolutionScope is TypeReferenceEntity outerTypeRef)
            {
                if (outerTypeRef.Handle.Kind == HandleKind.TypeDefinition)
                {
                    var outerTypeDef = (TypeDefinitionEntity)GetSeenEntities(TableIndex.TypeDef)[MetadataTokens.GetRowNumber(outerTypeRef.Handle) - 1];
                    var nestedTypeDef = FindTypeDefinition(outerTypeDef, typeRef.Namespace, typeRef.Name);
                    if (nestedTypeDef is not null)
                    {
                        ((IHasHandle)typeRef).SetHandle(nestedTypeDef.Handle);
                        return true;
                    }
                }
                return false;
            }

            // Top-level TypeRef: check if the resolution scope is a self-referencing assembly.
            if (resolutionScope is AssemblyReferenceEntity asmRef)
            {
                if (Assembly is not null && string.Equals(asmRef.Name, Assembly.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var typeDef = FindTypeDefinition(null, typeRef.Namespace, typeRef.Name);
                    if (typeDef is not null)
                    {
                        ((IHasHandle)typeRef).SetHandle(typeDef.Handle);
                        return true;
                    }
                }
                return false;
            }

            // Resolution scope is module-level (ModuleEntity/ModuleReferenceEntity) — local type.
            if (resolutionScope is ModuleEntity or ModuleReferenceEntity)
            {
                var typeDef = FindTypeDefinition(null, typeRef.Namespace, typeRef.Name);
                if (typeDef is not null)
                {
                    ((IHasHandle)typeRef).SetHandle(typeDef.Handle);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Rewrites a signature blob, replacing PseudoHandle-based TypeRef coded indices
        /// with resolved Handle-based coded indices. Returns the original blob if no mapping
        /// is needed or if the signature cannot be decoded.
        /// </summary>
        private static BlobBuilder RewriteSignatureBlob(BlobBuilder original, SignatureRewriter rewriter)
        {
            var bytes = original.ToArray();
            if (bytes.Length == 0)
            {
                return original;
            }

            try
            {
                var header = new SignatureHeader(bytes[0]);
                return header.Kind switch
                {
                    SignatureKind.Method => RewriteMethodSignatureBlob(bytes, rewriter),
                    SignatureKind.Field => RewriteFieldSignatureBlob(bytes, rewriter),
                    SignatureKind.LocalVariables => RewriteLocalSignatureBlob(bytes, rewriter),
                    SignatureKind.Property => RewritePropertySignatureBlob(bytes, rewriter),
                    _ => original
                };
            }
            catch
            {
                return original;
            }
        }

        /// <summary>
        /// Rewrites a TypeSpec signature blob (which is just a type, not a full signature with header).
        /// </summary>
        private BlobBuilder RewriteTypeSpecBlob(BlobBuilder original, SignatureRewriter rewriter)
        {
            var bytes = original.ToArray();
            if (bytes.Length == 0)
            {
                return original;
            }

            try
            {
                var decoder = new SignatureDecoder<SignatureRewriter.BlobOrHandle, SignatureRewriter.EmptyGenericContext>(rewriter, null!, default);
                unsafe
                {
                    fixed (byte* ptr = bytes)
                    {
                        var reader = new BlobReader(ptr, bytes.Length);
                        var decoded = decoder.DecodeType(ref reader);
                        BlobBuilder result = decoded;
                        return result;
                    }
                }
            }
            catch
            {
                return original;
            }
        }

        /// <summary>
        /// Rewrites a MethodSpec instantiation blob (generic type arguments).
        /// </summary>
        private BlobBuilder RewriteMethodSpecBlob(BlobBuilder original, SignatureRewriter rewriter)
        {
            var bytes = original.ToArray();
            if (bytes.Length == 0)
            {
                return original;
            }

            try
            {
                var decoder = new SignatureDecoder<SignatureRewriter.BlobOrHandle, SignatureRewriter.EmptyGenericContext>(rewriter, null!, default);
                unsafe
                {
                    fixed (byte* ptr = bytes)
                    {
                        var reader = new BlobReader(ptr, bytes.Length);
                        var typeArgs = decoder.DecodeMethodSpecificationSignature(ref reader);
                        var newBlob = new BlobBuilder();
                        newBlob.WriteByte((byte)SignatureAttributes.Generic);
                        newBlob.WriteCompressedInteger(typeArgs.Length);
                        foreach (var typeArg in typeArgs)
                        {
                            typeArg.WriteBlobTo(newBlob);
                        }
                        return newBlob;
                    }
                }
            }
            catch
            {
                return original;
            }
        }

        private static BlobBuilder RewriteMethodSignatureBlob(byte[] bytes, SignatureRewriter rewriter)
        {
            var decoder = new SignatureDecoder<SignatureRewriter.BlobOrHandle, SignatureRewriter.EmptyGenericContext>(rewriter, null!, default);
            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    var reader = new BlobReader(ptr, bytes.Length);
                    var sig = decoder.DecodeMethodSignature(ref reader);

                    var newBlob = new BlobBuilder();
                    var encoder = new BlobEncoder(newBlob);
                    encoder.MethodSignature(sig.Header.CallingConvention, sig.GenericParameterCount, sig.Header.Attributes.HasFlag(SignatureAttributes.Instance))
                        .Parameters(sig.ParameterTypes.Length, out var retBuilder, out var paramsBuilder);
                    sig.ReturnType.WriteBlobTo(retBuilder.Builder);
                    for (int i = 0; i < sig.ParameterTypes.Length; i++)
                    {
                        if (sig.RequiredParameterCount != sig.ParameterTypes.Length && i == sig.RequiredParameterCount)
                        {
                            paramsBuilder.StartVarArgs();
                        }
                        sig.ParameterTypes[i].WriteBlobTo(paramsBuilder.AddParameter().Builder);
                    }
                    return newBlob;
                }
            }
        }

        private static BlobBuilder RewriteFieldSignatureBlob(byte[] bytes, SignatureRewriter rewriter)
        {
            var decoder = new SignatureDecoder<SignatureRewriter.BlobOrHandle, SignatureRewriter.EmptyGenericContext>(rewriter, null!, default);
            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    var reader = new BlobReader(ptr, bytes.Length);
                    var fieldType = decoder.DecodeFieldSignature(ref reader);

                    var newBlob = new BlobBuilder();
                    newBlob.WriteByte((byte)SignatureKind.Field); // 0x06
                    fieldType.WriteBlobTo(newBlob);
                    return newBlob;
                }
            }
        }

        private static BlobBuilder RewriteLocalSignatureBlob(byte[] bytes, SignatureRewriter rewriter)
        {
            var decoder = new SignatureDecoder<SignatureRewriter.BlobOrHandle, SignatureRewriter.EmptyGenericContext>(rewriter, null!, default);
            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    var reader = new BlobReader(ptr, bytes.Length);
                    var localTypes = decoder.DecodeLocalSignature(ref reader);

                    var newBlob = new BlobBuilder();
                    var encoder = new BlobEncoder(newBlob);
                    var localsEncoder = encoder.LocalVariableSignature(localTypes.Length);
                    foreach (var localType in localTypes)
                    {
                        localType.WriteBlobTo(localsEncoder.AddVariable().Builder);
                    }
                    return newBlob;
                }
            }
        }

        private static BlobBuilder RewritePropertySignatureBlob(byte[] bytes, SignatureRewriter rewriter)
        {
            var decoder = new SignatureDecoder<SignatureRewriter.BlobOrHandle, SignatureRewriter.EmptyGenericContext>(rewriter, null!, default);
            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    var reader = new BlobReader(ptr, bytes.Length);
                    var sig = decoder.DecodeMethodSignature(ref reader);

                    var newBlob = new BlobBuilder();
                    var encoder = new BlobEncoder(newBlob);
                    encoder.PropertySignature(sig.Header.Attributes.HasFlag(SignatureAttributes.Instance))
                        .Parameters(sig.ParameterTypes.Length, out var retBuilder, out var paramsBuilder);
                    sig.ReturnType.WriteBlobTo(retBuilder.Builder);
                    for (int i = 0; i < sig.ParameterTypes.Length; i++)
                    {
                        sig.ParameterTypes[i].WriteBlobTo(paramsBuilder.AddParameter().Builder);
                    }
                    return newBlob;
                }
            }
        }

        private void ResolveAndRecordMemberReference(MemberReferenceEntity memberRef)
        {
            // We need to resolve a MemberReference in a few scenarios:
            // 1. The MemberReference references a local MethodDefinition
            //   - This case may occur when a method is referenced by a property or event, which can only reference MethodDefinition entities
            //   - This also produces compat with the existing ILASM, which always resolves local method references to MethodDef tokens
            // 2. The MemberReference refers to a local FieldDefinition
            //   - This produces compat with the existing ILASM, which always resolves local field references to FieldDef tokens

            var signature = memberRef.Signature.ToArray();
            SignatureHeader header = new(signature[0]);
            if (header.Kind == SignatureKind.Method)
            {
                if (header.CallingConvention == SignatureCallingConvention.VarArgs)
                {
                    UpdateMemberRefForVarargSignatures(memberRef, signature);
                }
                if (TryResolveMethodReference(memberRef))
                {
                    return;
                }
            }
            else if (header.Kind == SignatureKind.Field)
            {
                if (TryResolveFieldReference(memberRef))
                {
                    return;
                }
            }
            RecordEntityInTable(TableIndex.MemberRef, memberRef);
        }

        private bool TryResolveMethodReference(MemberReferenceEntity memberRef)
        {
            switch (memberRef.Parent)
            {
                // Use this weird construction to look up TypeDefs as we may change TypeRef resolution to use a similar model to MemberReference
                // where we always return a TypeReference type, but it might just point to a TypeDef handle.
                case TypeEntity { Handle.Kind: HandleKind.TypeDefinition } type:
                    {
                        var typeDef = (TypeDefinitionEntity)GetSeenEntities(TableIndex.TypeDef)[MetadataTokens.GetRowNumber(type.Handle) - 1];
                        foreach (var method in typeDef.Methods)
                        {
                            if (method.Name == memberRef.Name
                                && method.MethodSignature!.ContentEquals(memberRef.Signature))
                            {
                                ((IHasHandle)memberRef).SetHandle(method.Handle);
                                return true;
                            }
                        }
                    }
                    break;
            }
            return false;
        }

        private bool TryResolveFieldReference(MemberReferenceEntity memberRef)
        {
            switch (memberRef.Parent)
            {
                case TypeEntity { Handle.Kind: HandleKind.TypeDefinition } type:
                    {
                        var typeDef = (TypeDefinitionEntity)GetSeenEntities(TableIndex.TypeDef)[MetadataTokens.GetRowNumber(type.Handle) - 1];
                        foreach (var field in typeDef.Fields)
                        {
                            if (field.Name == memberRef.Name
                                && field.Signature.ContentEquals(memberRef.Signature))
                            {
                                ((IHasHandle)memberRef).SetHandle(field.Handle);
                                return true;
                            }
                        }
                    }
                    break;
            }
            return false;
        }

        private void UpdateMemberRefForVarargSignatures(MemberReferenceEntity memberRef, byte[] signature)
        {
            var decoder = new SignatureDecoder<SignatureRewriter.BlobOrHandle, SignatureRewriter.EmptyGenericContext>(new SignatureRewriter(), null!, default);
            BlobEncoder methodDefSig = new(new BlobBuilder());
            bool hasVarargParameters = false;
            // TODO-SRM: Propose a public API to construct a blob reader over a byte array or ReadOnlyMemory<byte>
            // to avoid the unsafe block.
            // Alternatively, propose an API to get the corresponding MethodDefSig for a MethodRefSig and move all of this logic into SRM.
            try
            {
                unsafe
                {
                    fixed (byte* ptr = &signature[0])
                    {
                        var reader = new BlobReader(ptr, signature.Length);
                        var methodSignature = decoder.DecodeMethodSignature(ref reader);

                        if (methodSignature.RequiredParameterCount != methodSignature.ParameterTypes.Length)
                        {
                            hasVarargParameters = true;

                            methodDefSig.MethodSignature(methodSignature.Header.CallingConvention, methodSignature.GenericParameterCount, methodSignature.Header.Attributes.HasFlag(SignatureAttributes.Instance))
                                .Parameters(methodSignature.RequiredParameterCount, out var retTypeBuilder, out var parametersEncoder);
                            methodSignature.ReturnType.WriteBlobTo(retTypeBuilder.Builder);
                            for (int i = 0; i < methodSignature.RequiredParameterCount; i++)
                            {
                                methodSignature.ParameterTypes[i].WriteBlobTo(parametersEncoder.AddParameter().Builder);
                            }
                        }
                    }
                }
            }
            catch (BadImageFormatException)
            {
                // Signature contains constructs (e.g., sentinel markers) that the
                // SignatureDecoder cannot parse. Skip vararg processing and emit
                // the MemberRef with its original signature.
                return;
            }

            // If the method has vararg parameters, then this needs to be a MemberRef whose parent is a reference to the method with the signature without any vararg parameters.
            if (hasVarargParameters)
            {
                var methodRef = new MemberReferenceEntity(memberRef.Parent, memberRef.Name, methodDefSig.Builder);
                ResolveAndRecordMemberReference(methodRef);
                // Only reparent the call-site MemberRef if the base method resolved to a MethodDef.
                // MemberRef is not a valid MemberRefParent in the coded index, so we can only
                // reparent when the inner ref resolved to MethodDef.
                if (methodRef.Handle.Kind == HandleKind.MethodDefinition)
                {
                    memberRef.SetMemberRefParent(methodRef);
                }
            }
        }

        public MethodSpecificationEntity GetOrCreateMethodSpecification(EntityBase method, BlobBuilder signature)
        {
            return GetOrCreateEntity(
                (method, signature),
                TableIndex.MethodSpec,
                _seenMethodSpecs,
                ((EntityBase method, BlobBuilder signature) value) => new(method, signature),
                _ => { });
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
            if (_seenAssemblyRefs.TryGetValue(name, out var asmRef))
            {
                return asmRef;
            }
            return null;
        }

        public AssemblyReferenceEntity GetOrCreateAssemblyReference(string name, Version version, string? culture, BlobBuilder? publicKeyOrToken, AssemblyFlags flags, ProcessorArchitecture architecture)
        {
            return GetOrCreateEntity(name, TableIndex.AssemblyRef, _seenAssemblyRefs, _ => new AssemblyReferenceEntity(name), entity =>
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
            // We only key on the implementation if the type is nested (ExportedTypeEntity).
            // For forwarders, implementation is AssemblyReferenceEntity which is not used in the key.
            // However, we need to pass the actual implementation to the entity constructor.
            return GetOrCreateEntity((implementation as ExportedTypeEntity, @namespace, name), TableIndex.ExportedType, _seenExportedTypes, (key) => new(key.Item3, key.Item2, implementation), onCreateType);
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
            public virtual EntityHandle Handle { get; private set; }

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

            // ClassLayout table fields
            public int? PackingSize { get; set; }
            public int? ClassSize { get; set; }
        }

        public sealed class TypeReferenceEntity(EntityBase resolutionScope, string @namespace, string name) : TypeEntity, IHasReflectionNotation
        {
            public EntityBase ResolutionScope { get; } = resolutionScope;

            public string Namespace { get; } = @namespace;

            public string Name { get; } = name;

            public string ReflectionNotation { get; set; } = string.Empty;

            /// <summary>
            /// Temporary handle assigned during parsing for signature blob encoding.
            /// The real handle is assigned during emission after TypeRef → TypeDef resolution.
            /// </summary>
            public TypeReferenceHandle PseudoHandle { get; set; }

            /// <summary>
            /// Returns the real handle if set (during emission), otherwise the PseudoHandle
            /// (during parsing). This allows code that reads Handle during parsing
            /// (e.g., catch clauses, base type references) to get a valid TypeRef handle.
            /// </summary>
            public override EntityHandle Handle => base.Handle.IsNil ? PseudoHandle : base.Handle;

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

            /// <summary>
            /// Debug information for this method (sequence points, document).
            /// </summary>
            public MethodDebugInfo DebugInfo { get; } = new();

            /// <summary>
            /// Export ordinal for this method (from .export directive). -1 means not exported.
            /// </summary>
            public int ExportOrdinal { get; set; } = -1;

            /// <summary>
            /// Export alias name (from .export [n] as alias). Null means use method name.
            /// </summary>
            public string? ExportAlias { get; set; }

            /// <summary>
            /// 1-based VTable entry index (from .vtentry directive). 0 means not in vtable.
            /// </summary>
            public int VTableEntry { get; set; }

            /// <summary>
            /// 1-based slot within the VTable entry (from .vtentry directive). 0 means not in vtable.
            /// </summary>
            public int VTableSlot { get; set; }
        }

        public sealed class ParameterEntity(ParameterAttributes attributes, string? name, BlobBuilder marshallingDescriptor, int sequence) : EntityBase
        {
            public ParameterAttributes Attributes { get; } = attributes;
            public string? Name { get; } = name;
            public BlobBuilder MarshallingDescriptor { get; set; } = marshallingDescriptor;
            public bool HasCustomAttributes { get; set; }
            public int Sequence { get; } = sequence;
            public bool HasConstant { get; set; }
            public object? ConstantValue { get; set; }
        }

        public sealed class MemberReferenceEntity(EntityBase parent, string name, BlobBuilder signature) : EntityBase
        {
            // In the case of a MemberRef to a specific instantiation of a vararg method, we need to update the owner at emit time.
            public EntityBase Parent { get; private set; } = parent;
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

            internal void SetMemberRefParent(MemberReferenceEntity parent)
            {
                Parent = parent;
            }
        }

        public sealed class MethodSpecificationEntity(EntityBase parent, BlobBuilder signature) : EntityBase
        {
            public EntityBase Parent { get; } = parent;
            public BlobBuilder Signature { get; } = signature;
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

            // FieldLayout table field (explicit field offset)
            public int? Offset { get; set; }

            // Constant table entry
            public bool HasConstant { get; set; }
            public object? ConstantValue { get; set; }
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
            public PropertyAttributes Attributes { get; set; } = attributes;
            public BlobBuilder Type { get; } = type;
            public string Name { get; } = name;
            public bool HasConstant { get; set; }
            public object? ConstantValue { get; set; }

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

        /// <summary>
        /// Represents a sequence point mapping IL offset to source location.
        /// </summary>
        public readonly struct SequencePoint
        {
            public SequencePoint(int ilOffset, int startLine, int startColumn, int endLine, int endColumn)
            {
                ILOffset = ilOffset;
                StartLine = startLine;
                StartColumn = startColumn;
                EndLine = endLine;
                EndColumn = endColumn;
            }

            public int ILOffset { get; }
            public int StartLine { get; }
            public int StartColumn { get; }
            public int EndLine { get; }
            public int EndColumn { get; }

            /// <summary>
            /// Creates a hidden sequence point (used for compiler-generated code).
            /// </summary>
            public static SequencePoint Hidden(int ilOffset) => new(ilOffset, 0xFEEFEE, 0, 0xFEEFEE, 0);

            public bool IsHidden => StartLine == 0xFEEFEE;
        }

        /// <summary>
        /// Debug information for a method, including sequence points and local scopes.
        /// </summary>
        public sealed class MethodDebugInfo
        {
            public string? DocumentPath { get; set; }
            public List<SequencePoint> SequencePoints { get; } = new();
        }
    }
}
