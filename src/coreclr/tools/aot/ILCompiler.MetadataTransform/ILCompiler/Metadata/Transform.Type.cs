// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.Metadata.NativeFormat.Writer;

using Ecma = System.Reflection.Metadata;
using Cts = Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using TypeAttributes = System.Reflection.TypeAttributes;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private const TypeAttributes TypeAttributesExtendedLayout = (TypeAttributes)0x00000018;

        internal EntityMap<Cts.TypeDesc, MetadataRecord> _types =
            new EntityMap<Cts.TypeDesc, MetadataRecord>(EqualityComparer<Cts.TypeDesc>.Default);

        private Action<Cts.MetadataType, TypeDefinition> _initTypeDef;
        private Action<Cts.MetadataType, TypeReference> _initTypeRef;
        private Action<Cts.TypeDesc, TypeSpecification> _initTypeSpec;

        public override MetadataRecord HandleType(Cts.TypeDesc type)
        {
            MetadataRecord rec;
            if (_types.TryGet(type, out rec))
            {
                return rec;
            }

            if (!type.IsDefType || !type.IsTypeDefinition)
            {
                rec = _types.Create(type, _initTypeSpec ??= InitializeTypeSpecification);
            }
            else
            {
                var metadataType = (Cts.MetadataType)type;
                if (_policy.GeneratesMetadata(metadataType))
                {
                    Debug.Assert(!_policy.IsBlocked(metadataType));
                    rec = _types.Create(metadataType, _initTypeDef ??= InitializeTypeDef);
                }
                else
                {
                    rec = _types.Create(metadataType, _initTypeRef ??= InitializeTypeRef);
                }
            }

            Debug.Assert(rec is TypeDefinition || rec is TypeReference || rec is TypeSpecification);

            return rec;
        }

        private void InitializeTypeSpecification(Cts.TypeDesc entity, TypeSpecification record)
        {
            WriteTypeSignature(record.Blob, entity);
        }

        private TypeReference GetNestedReferenceParent(Cts.MetadataType entity)
        {
            // This special code deals with the metadata format requirement saying that
            // nested type *references* need to have a type *reference* as their containing type.
            // This is potentially in conflict with our other rule that says to always resolve
            // references to their definition records (we are avoiding emitting references
            // to things that have a definition within the same blob to save space).

            Cts.MetadataType containingType = (Cts.MetadataType)entity.ContainingType;
            MetadataRecord parentRecord = HandleType(containingType);
            TypeReference parentReferenceRecord = parentRecord as TypeReference;

            if (parentReferenceRecord != null)
            {
                // Easy case - parent type doesn't have a definition record.
                return parentReferenceRecord;
            }

            // Parent has a type definition record. We need to make a new record that's a reference.
            // We don't bother with interning these because this will be rare and metadata writer
            // will do the interning anyway.
            Debug.Assert(parentRecord is TypeDefinition);

            parentReferenceRecord = new TypeReference
            {
                TypeName = HandleString(containingType.GetName()),
            };

            if (containingType.ContainingType != null)
            {
                parentReferenceRecord.NamespaceOrEnclosingType = GetNestedReferenceParent(containingType);
            }
            else
            {
                parentReferenceRecord.NamespaceOrEnclosingType = HandleNamespaceReference(containingType.Module, containingType.GetNamespace());
            }

            return parentReferenceRecord;
        }

        private void InitializeTypeRef(Cts.MetadataType entity, TypeReference record)
        {
            Debug.Assert(entity.IsTypeDefinition);

            if (entity.ContainingType != null)
            {
                record.NamespaceOrEnclosingType = GetNestedReferenceParent(entity);
            }
            else
            {
                record.NamespaceOrEnclosingType = HandleNamespaceReference(entity.Module, entity.GetNamespace());
            }

            record.TypeName = HandleString(entity.GetName());
        }

        private void InitializeTypeDef(Cts.MetadataType entity, TypeDefinition record)
        {
            Debug.Assert(entity.IsTypeDefinition);

            Cts.MetadataType containingType = (Cts.MetadataType)entity.ContainingType;
            if (containingType != null)
            {
                var enclosingType = (TypeDefinition)HandleType(containingType);
                record.NamespaceOrEnclosingType = enclosingType;
                enclosingType.NestedTypes.Add(record);
            }
            else
            {
                var namespaceDefinition = HandleNamespaceDefinition(entity.Module, entity.GetNamespace());
                record.NamespaceOrEnclosingType = namespaceDefinition;

                if (entity.IsModuleType)
                {
                    // These don't get added to the scope's type definitions.
                    // Instead, they have a dedicated field on the scope record.
                }
                else
                {
                    HandleScopeDefinition(entity.Module).TypeDefinitions.Add(record);
                }
            }

            record.Name = HandleString(entity.GetName());

            Cts.ClassLayoutMetadata layoutMetadata = entity.GetClassLayout();
            record.Size = checked((uint)layoutMetadata.Size);
            record.PackingSize = checked((ushort)layoutMetadata.PackingSize);
            record.Flags = GetTypeAttributes(entity);

            try
            {
                if (entity.HasBaseType)
                {
                    record.BaseType = HandleType(entity.BaseType);
                }
            }
            catch (Cts.TypeSystemException) when (HasNestedTypes(entity))
            {
                // We might have been forced to generate metadata for a type
                // that wasn't looked at during code generation because it's an owning
                // type of a type we did look at. Allow those to generate incomplete
                // metadata. The ultimate fix is to rewrite metadata generation to be
                // System.Reflection.Metadata-based as opposed to type system based.
                // If there's no nested types, this is a bug and should tear down
                // the compiler at this point.
            }

            try
            {
                record.Interfaces.Capacity = entity.ExplicitlyImplementedInterfaces.Length;
                foreach (var interfaceType in entity.ExplicitlyImplementedInterfaces)
                {
                    if (IsBlocked(interfaceType))
                        continue;
                    if (!_policy.GeneratesInterfaceImpl(entity, (Cts.MetadataType)interfaceType))
                        continue;

                    record.Interfaces.Add(HandleType(interfaceType));
                }
            }
            catch (Cts.TypeSystemException) when (HasNestedTypes(entity))
            {
                // We might have been forced to generate metadata for a type
                // that wasn't looked at during code generation because it's an owning
                // type of a type we did look at. Allow those to generate incomplete
                // metadata. The ultimate fix is to rewrite metadata generation to be
                // System.Reflection.Metadata-based as opposed to type system based.
                // If there's no nested types, this is a bug and should tear down
                // the compiler at this point.
            }

            if (entity.HasInstantiation)
            {
                record.GenericParameters.Capacity = entity.Instantiation.Length;
                foreach (var p in entity.Instantiation)
                    record.GenericParameters.Add(HandleGenericParameter((Cts.GenericParameterDesc)p));
            }

            foreach (var field in entity.GetFields())
            {
                if (_policy.GeneratesMetadata(field))
                {
                    record.Fields.Add(HandleFieldDefinition(field));
                }
            }

            foreach (var method in entity.GetMethods())
            {
                if (_policy.GeneratesMetadata(method))
                {
                    record.Methods.Add(HandleMethodDefinition(method));
                }
            }

            var ecmaEntity = entity as Cts.Ecma.EcmaType;
            if (ecmaEntity != null)
            {
                Ecma.TypeDefinition ecmaRecord = ecmaEntity.MetadataReader.GetTypeDefinition(ecmaEntity.Handle);

                foreach (var e in ecmaRecord.GetEvents())
                {
                    Event evt = HandleEvent(ecmaEntity.Module, e);
                    if (evt != null)
                        record.Events.Add(evt);
                }

                foreach (var property in ecmaRecord.GetProperties())
                {
                    Property prop = HandleProperty(ecmaEntity.Module, property);
                    if (prop != null)
                        record.Properties.Add(prop);
                }

                Ecma.CustomAttributeHandleCollection customAttributes = ecmaRecord.GetCustomAttributes();
                if (customAttributes.Count > 0)
                {
                    record.CustomAttributes = HandleCustomAttributes(ecmaEntity.Module, customAttributes);
                }

                /* COMPLETENESS
                foreach (var miHandle in ecmaRecord.GetMethodImplementations())
                {
                    Ecma.MetadataReader reader = ecmaEntity.Module.MetadataReader;

                    Ecma.MethodImplementation miDef = reader.GetMethodImplementation(miHandle);

                    Cts.MethodDesc methodBody = (Cts.MethodDesc)ecmaEntity.Module.GetObject(miDef.MethodBody);
                    if (_policy.IsBlocked(methodBody))
                        continue;

                    Cts.MethodDesc methodDecl = (Cts.MethodDesc)ecmaEntity.Module.GetObject(miDef.MethodDeclaration);
                    if (_policy.IsBlocked(methodDecl.GetTypicalMethodDefinition()))
                        continue;

                    MethodImpl methodImplRecord = new MethodImpl
                    {
                        MethodBody = HandleQualifiedMethod(methodBody),
                        MethodDeclaration = HandleQualifiedMethod(methodDecl)
                    };

                    record.MethodImpls.Add(methodImplRecord);
                }*/
            }

            static bool HasNestedTypes(Cts.MetadataType entity)
                => entity.GetNestedTypes().GetEnumerator().MoveNext();
        }

        private static TypeAttributes GetTypeAttributes(Cts.MetadataType type)
        {
            TypeAttributes result;

            var ecmaType = type as Cts.Ecma.EcmaType;
            if (ecmaType != null)
            {
                Ecma.TypeDefinition ecmaRecord = ecmaType.MetadataReader.GetTypeDefinition(ecmaType.Handle);
                result = ecmaRecord.Attributes;
            }
            else
            {
                result = 0;

                if (type.IsExplicitLayout)
                    result |= TypeAttributes.ExplicitLayout;
                if (type.IsSequentialLayout)
                    result |= TypeAttributes.SequentialLayout;
                if (type.IsExtendedLayout)
                    result |= TypeAttributesExtendedLayout;
                if (type.IsInterface)
                    result |= TypeAttributes.Interface;
                if (type.IsSealed)
                    result |= TypeAttributes.Sealed;
                if (type.IsBeforeFieldInit)
                    result |= TypeAttributes.BeforeFieldInit;

                // Not set: Abstract, Ansi/Unicode/Auto, HasSecurity, Import, visibility, Serializable,
                //          WindowsRuntime, HasSecurity, SpecialName, RTSpecialName
            }

            return result;
        }
    }
}
