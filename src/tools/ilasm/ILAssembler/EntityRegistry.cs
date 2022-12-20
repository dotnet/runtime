// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml.Linq;

namespace ILAssembler
{
    internal sealed class EntityRegistry
    {
        private readonly Dictionary<TableIndex, List<EntityBase>> _seenEntities = new();
        private readonly Dictionary<(TypeDefinitionEntity? ContainingType, string Namespace, string Name), TypeDefinitionEntity> _seenTypeDefs = new();
        private readonly Dictionary<(EntityBase ResolutionScope, string Namespace, string Name), TypeReferenceEntity> _seenTypeRefs = new();
        private readonly Dictionary<string, AssemblyReferenceEntity> _seenAssemblyRefs = new();
        private readonly Dictionary<string, ModuleReferenceEntity> _seenModuleRefs = new();
        private readonly Dictionary<BlobBuilder, TypeSpecificationEntity> _seenTypeSpecs = new(new BlobBuilderContentEqualityComparer());

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
            return GetOrCreateEntity(name, TableIndex.AssemblyRef, _seenAssemblyRefs, name => new(name), onCreateAssemblyReference);
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

#pragma warning disable CA1822 // Mark members as static
        public GenericParameterEntity CreateGenericParameter(GenericParameterAttributes attributes, string name)
#pragma warning restore CA1822 // Mark members as static
        {
            GenericParameterEntity param = new(attributes, name);
            return param;
        }

#pragma warning disable CA1822 // Mark members as static
        public GenericParameterConstraintEntity CreateGenericConstraint(TypeEntity baseType)
#pragma warning restore CA1822 // Mark members as static
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

        public bool TryRecordMethodDefinition(MethodDefinitionEntity methodDef)
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

            RecordEntityInTable(TableIndex.MethodDef, methodDef);
            methodDef.ContainingType.Methods.Add(methodDef);
            return true;
        }

        public abstract class EntityBase : IHasHandle
        {
            public EntityHandle Handle { get; private set; }

            void IHasHandle.SetHandle(EntityHandle token) => Handle = token;
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

            // TODO: Add fields, methods, properties, etc.

            public NamedElementList<GenericParameterEntity> GenericParameters { get; } = new();

            // COMPAT: Save the list of generic parameter constraints here to ensure we can match ILASM's emit order for generic parameter constraints exactly.
            public List<GenericParameterConstraintEntity> GenericParameterConstraints { get; } = new();

            public List<MethodDefinitionEntity> Methods { get; } = new();

            public string ReflectionNotation { get; }
        }

        public sealed class TypeReferenceEntity : TypeEntity, IHasReflectionNotation
        {
            public TypeReferenceEntity(EntityBase resolutionScope, string @namespace, string name)
            {
                ResolutionScope = resolutionScope;
                Namespace = @namespace;
                Name = name;
            }
            public EntityBase ResolutionScope { get; }

            public string Namespace { get; }

            public string Name { get; }

            public string ReflectionNotation { get; set; } = string.Empty;
        }

        public sealed class TypeSpecificationEntity : TypeEntity
        {
            public TypeSpecificationEntity(BlobBuilder signature)
            {
                Signature = signature;
            }

            public BlobBuilder Signature { get; }
        }

        public sealed class GenericParameterEntity : EntityBase, INamed
        {
            public GenericParameterEntity(GenericParameterAttributes attributes, string name)
            {
                Attributes = attributes;
                Name = name;
            }

            public GenericParameterAttributes Attributes { get; }

            public EntityBase? Owner { get; set; }

            public int Index { get; set; }

            public List<GenericParameterConstraintEntity> Constraints { get; } = new();

            public string Name { get; }
        }

        public sealed class GenericParameterConstraintEntity : EntityBase
        {
            public GenericParameterConstraintEntity(TypeEntity baseType)
            {
                BaseType = baseType;
            }

            public GenericParameterEntity? Owner { get; set; }

            public TypeEntity BaseType { get; }
        }

        public sealed class AssemblyReferenceEntity : EntityBase
        {
            public AssemblyReferenceEntity(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        public sealed class ModuleReferenceEntity : EntityBase
        {
            public ModuleReferenceEntity(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        public sealed class MethodDefinitionEntity : EntityBase, IHasHandle
        {
            public MethodDefinitionEntity(TypeDefinitionEntity containingType, string name)
            {
                ContainingType = containingType;
                Name = name;
            }

            public TypeDefinitionEntity ContainingType { get; }
            public string Name { get; }

            public MethodAttributes MethodAttributes { get; set; }

            public NamedElementList<GenericParameterEntity> GenericParameters { get; } = new();

            // COMPAT: Save the list of generic parameter constraints here to ensure we can match ILASM's emit order for generic parameter constraints exactly.
            public List<GenericParameterConstraintEntity> GenericParameterConstraints { get; } = new();

            public BlobBuilder? MethodSignature { get; set; }

            public BlobBuilder? LocalsSignature { get; set; }

            public BlobBuilder? MethodBody { get; set; }

            public (ModuleReferenceEntity ModuleName, string? EntryPointName, MethodImportAttributes Attributes)? MethodImportInformation { get; set; }
            public MethodImplAttributes ImplementationAttributes { get; set; }
        }
    }
}
