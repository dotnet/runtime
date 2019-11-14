// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Internal.TypeSystem.Ecma
{
    public partial class EcmaModule : ModuleDesc
    {
        private PEReader _peReader;
        protected MetadataReader _metadataReader;

        internal interface IEntityHandleObject
        {
            EntityHandle Handle
            {
                get;
            }
        }

        private sealed class EcmaObjectLookupWrapper : IEntityHandleObject
        {
            private EntityHandle _handle;
            private object _obj;

            public EcmaObjectLookupWrapper(EntityHandle handle, object obj)
            {
                _obj = obj;
                _handle = handle;
            }

            public EntityHandle Handle
            {
                get
                {
                    return _handle;
                }
            }

            public object Object
            {
                get
                {
                    return _obj;
                }
            }
        }

        internal class EcmaObjectLookupHashtable : LockFreeReaderHashtable<EntityHandle, IEntityHandleObject>
        {
            private EcmaModule _module;

            public EcmaObjectLookupHashtable(EcmaModule module)
            {
                _module = module;
            }

            protected override int GetKeyHashCode(EntityHandle key)
            {
                return key.GetHashCode();
            }

            protected override int GetValueHashCode(IEntityHandleObject value)
            {
                return value.Handle.GetHashCode();
            }

            protected override bool CompareKeyToValue(EntityHandle key, IEntityHandleObject value)
            {
                return key.Equals(value.Handle);
            }

            protected override bool CompareValueToValue(IEntityHandleObject value1, IEntityHandleObject value2)
            {
                if (Object.ReferenceEquals(value1, value2))
                    return true;
                else
                    return value1.Handle.Equals(value2.Handle);
            }

            protected override IEntityHandleObject CreateValueFromKey(EntityHandle handle)
            {
                object item;
                switch (handle.Kind)
                {
                    case HandleKind.TypeDefinition:
                        item = new EcmaType(_module, (TypeDefinitionHandle)handle);
                        break;

                    case HandleKind.MethodDefinition:
                        {
                            MethodDefinitionHandle methodDefinitionHandle = (MethodDefinitionHandle)handle;
                            TypeDefinitionHandle typeDefinitionHandle = _module._metadataReader.GetMethodDefinition(methodDefinitionHandle).GetDeclaringType();
                            EcmaType type = (EcmaType)_module.GetObject(typeDefinitionHandle);
                            item = new EcmaMethod(type, methodDefinitionHandle);
                        }
                        break;

                    case HandleKind.FieldDefinition:
                        {
                            FieldDefinitionHandle fieldDefinitionHandle = (FieldDefinitionHandle)handle;
                            TypeDefinitionHandle typeDefinitionHandle = _module._metadataReader.GetFieldDefinition(fieldDefinitionHandle).GetDeclaringType();
                            EcmaType type = (EcmaType)_module.GetObject(typeDefinitionHandle);
                            item = new EcmaField(type, fieldDefinitionHandle);
                        }
                        break;

                    case HandleKind.TypeReference:
                        item = _module.ResolveTypeReference((TypeReferenceHandle)handle);
                        break;

                    case HandleKind.MemberReference:
                        item = _module.ResolveMemberReference((MemberReferenceHandle)handle);
                        break;

                    case HandleKind.AssemblyReference:
                        item = _module.ResolveAssemblyReference((AssemblyReferenceHandle)handle);
                        break;

                    case HandleKind.TypeSpecification:
                        item = _module.ResolveTypeSpecification((TypeSpecificationHandle)handle);
                        break;

                    case HandleKind.MethodSpecification:
                        item = _module.ResolveMethodSpecification((MethodSpecificationHandle)handle);
                        break;

                    case HandleKind.ExportedType:
                        item = _module.ResolveExportedType((ExportedTypeHandle)handle);
                        break;

                    case HandleKind.StandaloneSignature:
                        item = _module.ResolveStandaloneSignature((StandaloneSignatureHandle)handle);
                        break;

                    case HandleKind.ModuleDefinition:
                        // ECMA-335 Partition 2 II.22.38 1d: This should not occur in a CLI ("compressed metadata") module,
                        // but resolves to "current module".
                        item = _module;
                        break;

                    case HandleKind.ModuleReference:
                        item = _module.ResolveModuleReference((ModuleReferenceHandle)handle);
                        break;

                    default:
                        throw new BadImageFormatException("Unknown metadata token type: " + handle.Kind);
                }

                switch (handle.Kind)
                {
                    case HandleKind.TypeDefinition:
                    case HandleKind.MethodDefinition:
                    case HandleKind.FieldDefinition:
                        // type/method/field definitions directly correspond to their target item.
                        return (IEntityHandleObject)item;
                    default:
                        // Everything else is some form of reference which cannot be self-describing
                        return new EcmaObjectLookupWrapper(handle, item);
                }
            }
        }

        private object ResolveModuleReference(ModuleReferenceHandle handle)
        {
            ModuleReference moduleReference = _metadataReader.GetModuleReference(handle);
            string fileName = _metadataReader.GetString(moduleReference.Name);
            return Context.ResolveModule(this.Assembly, fileName);
        }

        private LockFreeReaderHashtable<EntityHandle, IEntityHandleObject> _resolvedTokens;

        internal EcmaModule(TypeSystemContext context, PEReader peReader, MetadataReader metadataReader, IAssemblyDesc containingAssembly)
            : base(context, containingAssembly)
        {
            _peReader = peReader;
            _metadataReader = metadataReader;
            _resolvedTokens = new EcmaObjectLookupHashtable(this);
        }

        public static EcmaModule Create(TypeSystemContext context, PEReader peReader, IAssemblyDesc containingAssembly)
        {
            MetadataReader metadataReader = CreateMetadataReader(context, peReader);

            if (containingAssembly == null)
                return new EcmaAssembly(context, peReader, metadataReader);
            else
                return new EcmaModule(context, peReader, metadataReader, containingAssembly);
        }

        private static MetadataReader CreateMetadataReader(TypeSystemContext context, PEReader peReader)
        {
            if (!peReader.HasMetadata)
            {
                ThrowHelper.ThrowBadImageFormatException();
            }

            var stringDecoderProvider = context as IMetadataStringDecoderProvider;

            MetadataReader metadataReader = peReader.GetMetadataReader(MetadataReaderOptions.None /* MetadataReaderOptions.ApplyWindowsRuntimeProjections */,
                (stringDecoderProvider != null) ? stringDecoderProvider.GetMetadataStringDecoder() : null);

            return metadataReader;
        }

        public PEReader PEReader
        {
            get
            {
                return _peReader;
            }
        }

        public MetadataReader MetadataReader
        {
            get
            {
                return _metadataReader;
            }
        }

        /// <summary>
        /// Gets the managed entrypoint method of this module or null if the module has no managed entrypoint.
        /// </summary>
        public MethodDesc EntryPoint
        {
            get
            {
                CorHeader corHeader = _peReader.PEHeaders.CorHeader;
                if ((corHeader.Flags & CorFlags.NativeEntryPoint) != 0)
                {
                    // Entrypoint is an RVA to an unmanaged method
                    return null;
                }

                int entryPointToken = corHeader.EntryPointTokenOrRelativeVirtualAddress;
                if (entryPointToken == 0)
                {
                    // No entrypoint
                    return null;
                }

                EntityHandle handle = MetadataTokens.EntityHandle(entryPointToken);

                if (handle.Kind == HandleKind.MethodDefinition)
                {
                    return GetMethod(handle);
                }
                else if (handle.Kind == HandleKind.AssemblyFile)
                {
                    // Entrypoint not in the manifest assembly
                    throw new NotImplementedException();
                }

                // Bad metadata
                throw new BadImageFormatException();
            }
        }

        public sealed override MetadataType GetType(string nameSpace, string name, bool throwIfNotFound = true)
        {
            var stringComparer = _metadataReader.StringComparer;

            // TODO: More efficient implementation?
            foreach (var typeDefinitionHandle in _metadataReader.TypeDefinitions)
            {
                var typeDefinition = _metadataReader.GetTypeDefinition(typeDefinitionHandle);
                if (stringComparer.Equals(typeDefinition.Name, name) &&
                    stringComparer.Equals(typeDefinition.Namespace, nameSpace))
                {
                    return (MetadataType)GetType((EntityHandle)typeDefinitionHandle);
                }
            }

            foreach (var exportedTypeHandle in _metadataReader.ExportedTypes)
            {
                var exportedType = _metadataReader.GetExportedType(exportedTypeHandle);
                if (stringComparer.Equals(exportedType.Name, name) &&
                    stringComparer.Equals(exportedType.Namespace, nameSpace))
                {
                    if (exportedType.IsForwarder)
                    {
                        Object implementation = GetObject(exportedType.Implementation);

                        if (implementation is ModuleDesc)
                        {
                            return ((ModuleDesc)(implementation)).GetType(nameSpace, name);
                        }

                        // TODO
                        throw new NotImplementedException();
                    }
                    // TODO:
                    throw new NotImplementedException();
                }
            }

            if (throwIfNotFound)
                ThrowHelper.ThrowTypeLoadException(nameSpace, name, this);

            return null;
        }

        public TypeDesc GetType(EntityHandle handle)
        {
            TypeDesc type = GetObject(handle) as TypeDesc;
            if (type == null)
                throw new BadImageFormatException("Type expected");
            return type;
        }

        public MethodDesc GetMethod(EntityHandle handle)
        {
            MethodDesc method = GetObject(handle) as MethodDesc;
            if (method == null)
                throw new BadImageFormatException("Method expected");
            return method;
        }

        public FieldDesc GetField(EntityHandle handle)
        {
            FieldDesc field = GetObject(handle) as FieldDesc;
            if (field == null)
                throw new BadImageFormatException("Field expected");
            return field;
        }

        public Object GetObject(EntityHandle handle)
        {
            IEntityHandleObject obj = _resolvedTokens.GetOrCreateValue(handle);
            if (obj is EcmaObjectLookupWrapper)
            {
                return ((EcmaObjectLookupWrapper)obj).Object;
            }
            else
            {
                return obj;
            }
        }

        private Object ResolveMethodSpecification(MethodSpecificationHandle handle)
        {
            MethodSpecification methodSpecification = _metadataReader.GetMethodSpecification(handle);

            MethodDesc methodDef = GetMethod(methodSpecification.Method);

            BlobReader signatureReader = _metadataReader.GetBlobReader(methodSpecification.Signature);
            EcmaSignatureParser parser = new EcmaSignatureParser(this, signatureReader);

            TypeDesc[] instantiation = parser.ParseMethodSpecSignature();
            return Context.GetInstantiatedMethod(methodDef, new Instantiation(instantiation));
        }

        private Object ResolveStandaloneSignature(StandaloneSignatureHandle handle)
        {
            StandaloneSignature signature = _metadataReader.GetStandaloneSignature(handle);
            BlobReader signatureReader = _metadataReader.GetBlobReader(signature.Signature);
            EcmaSignatureParser parser = new EcmaSignatureParser(this, signatureReader);

            MethodSignature methodSig = parser.ParseMethodSignature();
            return methodSig;
        }

        private Object ResolveTypeSpecification(TypeSpecificationHandle handle)
        {
            TypeSpecification typeSpecification = _metadataReader.GetTypeSpecification(handle);

            BlobReader signatureReader = _metadataReader.GetBlobReader(typeSpecification.Signature);
            EcmaSignatureParser parser = new EcmaSignatureParser(this, signatureReader);

            return parser.ParseType();
        }

        private Object ResolveMemberReference(MemberReferenceHandle handle)
        {
            MemberReference memberReference = _metadataReader.GetMemberReference(handle);

            Object parent = GetObject(memberReference.Parent);

            TypeDesc parentTypeDesc = parent as TypeDesc;
            if (parentTypeDesc != null)
            {
                BlobReader signatureReader = _metadataReader.GetBlobReader(memberReference.Signature);

                EcmaSignatureParser parser = new EcmaSignatureParser(this, signatureReader);

                string name = _metadataReader.GetString(memberReference.Name);

                if (parser.IsFieldSignature)
                {
                    FieldDesc field = parentTypeDesc.GetField(name);
                    if (field != null)
                        return field;

                    ThrowHelper.ThrowMissingFieldException(parentTypeDesc, name);
                }
                else
                {
                    MethodSignature sig = parser.ParseMethodSignature();
                    TypeDesc typeDescToInspect = parentTypeDesc;

                    // Try to resolve the name and signature in the current type, or any of the base types.
                    do
                    {
                        // TODO: handle substitutions
                        MethodDesc method = typeDescToInspect.GetMethod(name, sig);
                        if (method != null)
                        {
                            // If this resolved to one of the base types, make sure it's not a constructor.
                            // Instance constructors are not inherited.
                            if (typeDescToInspect != parentTypeDesc && method.IsConstructor)
                                break;

                            return method;
                        }
                        typeDescToInspect = typeDescToInspect.BaseType;
                    } while (typeDescToInspect != null);

                    ThrowHelper.ThrowMissingMethodException(parentTypeDesc, name, sig);
                }
            }
            else if (parent is MethodDesc)
            {
                ThrowHelper.ThrowInvalidProgramException(ExceptionStringID.InvalidProgramVararg, (MethodDesc)parent);
            }
            else if (parent is ModuleDesc)
            {
                throw new NotImplementedException("MemberRef to a global function or variable.");
            }

            throw new BadImageFormatException();
        }

        private Object ResolveTypeReference(TypeReferenceHandle handle)
        {
            TypeReference typeReference = _metadataReader.GetTypeReference(handle);

            Object resolutionScope = GetObject(typeReference.ResolutionScope);

            if (resolutionScope is ModuleDesc)
            {
                return ((ModuleDesc)(resolutionScope)).GetType(_metadataReader.GetString(typeReference.Namespace), _metadataReader.GetString(typeReference.Name));
            }
            else
            if (resolutionScope is MetadataType)
            {
                string typeName = _metadataReader.GetString(typeReference.Name);
                if (!typeReference.Namespace.IsNil)
                    typeName = _metadataReader.GetString(typeReference.Namespace) + "." + typeName;
                MetadataType result = ((MetadataType)(resolutionScope)).GetNestedType(typeName);
                if (result != null)
                    return result;

                ThrowHelper.ThrowTypeLoadException(typeName, ((MetadataType)resolutionScope).Module);
            }

            // TODO
            throw new NotImplementedException();
        }

        private Object ResolveAssemblyReference(AssemblyReferenceHandle handle)
        {
            AssemblyReference assemblyReference = _metadataReader.GetAssemblyReference(handle);

            AssemblyName an = new AssemblyName();
            an.Name = _metadataReader.GetString(assemblyReference.Name);
            an.Version = assemblyReference.Version;

            var publicKeyOrToken = _metadataReader.GetBlobBytes(assemblyReference.PublicKeyOrToken);
            if ((assemblyReference.Flags & AssemblyFlags.PublicKey) != 0)
            {
                an.SetPublicKey(publicKeyOrToken);
            }
            else
            {
                an.SetPublicKeyToken(publicKeyOrToken);
            }

            an.CultureName = _metadataReader.GetString(assemblyReference.Culture);
            an.ContentType = GetContentTypeFromAssemblyFlags(assemblyReference.Flags);

            return Context.ResolveAssembly(an);
        }

        private Object ResolveExportedType(ExportedTypeHandle handle)
        {
            ExportedType exportedType = _metadataReader.GetExportedType(handle);

            var implementation = GetObject(exportedType.Implementation);
            if (implementation is ModuleDesc)
            {
                var module = (ModuleDesc)implementation;
                string nameSpace = _metadataReader.GetString(exportedType.Namespace);
                string name = _metadataReader.GetString(exportedType.Name);
                return module.GetType(nameSpace, name);
            }
            else
            if (implementation is MetadataType)
            {
                var type = (MetadataType)implementation;
                string name = _metadataReader.GetString(exportedType.Name);
                var nestedType = type.GetNestedType(name);
                if (nestedType == null)
                    ThrowHelper.ThrowTypeLoadException(name, this);
                return nestedType;
            }
            else
            {
                throw new BadImageFormatException("Unknown metadata token type for exported type");
            }
        }

        public sealed override IEnumerable<MetadataType> GetAllTypes()
        {
            foreach (var typeDefinitionHandle in _metadataReader.TypeDefinitions)
            {
                yield return (MetadataType)GetType(typeDefinitionHandle);
            }
        }

        public sealed override MetadataType GetGlobalModuleType()
        {
            int typeDefinitionsCount = _metadataReader.TypeDefinitions.Count;
            if (typeDefinitionsCount == 0)
                return null;

            return (MetadataType)GetType(MetadataTokens.EntityHandle(0x02000001 /* COR_GLOBAL_PARENT_TOKEN */));
        }

        protected static AssemblyContentType GetContentTypeFromAssemblyFlags(AssemblyFlags flags)
        {
            return (AssemblyContentType)(((int)flags & 0x0E00) >> 9);
        }

        public string GetUserString(UserStringHandle userStringHandle)
        {
            // String literals are not cached
            return _metadataReader.GetUserString(userStringHandle);
        }

        public override string ToString()
        {
            ModuleDefinition moduleDefinition = _metadataReader.GetModuleDefinition();
            return _metadataReader.GetString(moduleDefinition.Name);
        }
    }
}
