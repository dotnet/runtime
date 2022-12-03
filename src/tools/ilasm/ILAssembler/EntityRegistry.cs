// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILAssembler
{
    internal sealed class EntityRegistry
    {
        private readonly List<TypeDefinitionEntity> _typeDefs = new();
        private readonly Dictionary<(TypeDefinitionEntity? ContainingType, string Namespace, string Name), TypeDefinitionEntity> _seenTypeDefs = new();

        public enum WellKnownBaseType
        {
            System_Object,
            System_ValueType,
            System_Enum
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

        private TypeEntity ResolveFromCoreAssembly(string typeName)
        {
            throw new NotImplementedException();
        }

        private interface IHasHandle
        {
            EntityHandle Handle { get; }
            void SetHandle(EntityHandle token);
        }

        public TypeDefinitionEntity GetOrCreateTypeDefinition(TypeDefinitionEntity? containingType, string @namespace, string name, Action<TypeDefinitionEntity> onCreateNewType)
        {
            if (_seenTypeDefs.TryGetValue((containingType, @namespace, name), out TypeDefinitionEntity def))
            {
                return def;
            }
            def = new TypeDefinitionEntity(null, name);
            AddTypeDefinition(containingType, @namespace, name, def, onCreateNewType);
            return def;
        }

        private void AddTypeDefinition(TypeDefinitionEntity? containingType, string @namespace, string name, TypeDefinitionEntity type, Action<TypeDefinitionEntity> onCreateNewType)
        {
            _typeDefs.Add(type);
            ((IHasHandle)type).SetHandle(MetadataTokens.TypeDefinitionHandle(_typeDefs.Count));
            _seenTypeDefs.Add((containingType, @namespace, name), type);
            onCreateNewType(type);
        }

        public abstract class EntityBase : IHasHandle, IEquatable<EntityBase?>
        {
            public EntityHandle Handle { get; private set; }

            void IHasHandle.SetHandle(EntityHandle token) => Handle = token;

            public bool Equals(EntityBase? other)
            {
                if (other is null)
                {
                    return false;
                }
                // The handle value should be set before this handle is handed out from the registry
                // to calling code.
                Debug.Assert(Handle != default);
                Debug.Assert(other.Handle != default);
                return Handle == other.Handle;
            }

            public override bool Equals(object obj) => obj is EntityBase other && Equals(other);

            public override int GetHashCode() => Handle.GetHashCode();
        }

        public abstract class TypeEntity : EntityBase
        {
        }

        public sealed class TypeDefinitionEntity : TypeEntity
        {
            public TypeDefinitionEntity(string? @namespace, string name)
            {
                Namespace = @namespace;
                Name = name;
            }

            public string? Namespace { get; }
            public string Name { get; }
            public TypeAttributes Attributes { get; set; }
            public TypeEntity? BaseType { get; set; }
            public TypeDefinitionEntity? ContainingType { get; set; }

            // TODO: Add fields, methods, properties, etc.
            // TODO: Add generic type parameters
        }

        public sealed class TypeReferenceEntity : TypeEntity
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
        }

        public sealed class TypeSpecificationEntity : TypeEntity
        {
            public TypeSpecificationEntity(BlobBuilder signature)
            {
                Signature = signature;
            }

            public BlobBuilder Signature { get; }
        }
    }
}
