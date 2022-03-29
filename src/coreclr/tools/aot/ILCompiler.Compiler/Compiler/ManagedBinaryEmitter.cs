// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using System.IO;
using System.Diagnostics;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler
{
    public class ManagedBinaryEmitter
    {
        public class EmittedTypeDefinition
        {
            private readonly ManagedBinaryEmitter _managedEmitter;
            private List<EmittedMethodDefinition> _methods = new List<EmittedMethodDefinition>();

            public string Name { get; private set; }
            public bool IsValueType { get; private set; }
            public IReadOnlyList<EmittedMethodDefinition> Methods => _methods;

            internal EmittedTypeDefinition(string name, bool isValueType, ManagedBinaryEmitter managedEmitter)
            {
                Name = name;
                IsValueType = isValueType;
                _managedEmitter = managedEmitter;
            }

            public EmittedMethodDefinition EmitMethodDefinition(string name, MethodSignature signature)
            {
                var newMethodDef = new EmittedMethodDefinition(name, signature, _managedEmitter);
                _methods.Add(newMethodDef);
                return newMethodDef;
            }
        }
        public class EmittedMethodDefinition
        {
            private readonly ManagedBinaryEmitter _managedEmitter;

            public string Name { get; private set; }
            public MethodSignature Signature { get; private set; }
            public InstructionEncoder Code { get; private set; }

            internal EmittedMethodDefinition(string name, MethodSignature signature, ManagedBinaryEmitter managedEmitter)
            {
                Name = name;
                Signature = signature;
                _managedEmitter = managedEmitter;
                Code = new InstructionEncoder(new BlobBuilder());
            }
        }
        
        private readonly TypeSystemContext _typeSystemContext;

        private MetadataBuilder _metadataBuilder;
        private MethodBodyStreamEncoder _methodBodyStream;
        private List<EmittedTypeDefinition> _emittedTypes;

        protected MetadataBuilder Builder => _metadataBuilder;

        public ManagedBinaryEmitter(TypeSystemContext typeSystemContext, string assemblyName)
        {
            _typeSystemContext = typeSystemContext;

            _metadataBuilder = new MetadataBuilder();
            _methodBodyStream = new MethodBodyStreamEncoder(new BlobBuilder());
            _emittedTypes = new List<EmittedTypeDefinition>();

            _metadataBuilder.AddAssembly(
                _metadataBuilder.GetOrAddString(assemblyName),
                new Version(0, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: default(BlobHandle),
                flags: default(AssemblyFlags), 
                hashAlgorithm: AssemblyHashAlgorithm.None);

            _metadataBuilder.AddModule(
                0,
                _metadataBuilder.GetOrAddString(assemblyName),
                default(GuidHandle), default(GuidHandle), default(GuidHandle));

            // Module type
            _metadataBuilder.AddTypeDefinition(
               default(TypeAttributes),
               default(StringHandle),
               _metadataBuilder.GetOrAddString("<Module>"),
               baseType: default(EntityHandle),
               fieldList: MetadataTokens.FieldDefinitionHandle(1),
               methodList: MetadataTokens.MethodDefinitionHandle(1));

            _signatureEmitter = new EcmaSignatureEncoder<EntityProviderForEcmaSignature>(new EntityProviderForEcmaSignature(this));
        }

        public EmittedTypeDefinition EmitTypeDefinition(string typeName, bool isValueType)
        {
            EmittedTypeDefinition newTypeDef = new EmittedTypeDefinition(typeName, isValueType, this);
            _emittedTypes.Add(newTypeDef);
            return newTypeDef;
        }

        public EntityHandle EmitMetadataHandleForTypeSystemEntity(TypeSystemEntity entity)
        {
            switch (entity)
            {
                case FieldDesc field: return MakeMemberReferenceHandle(field);
                case MethodDesc method: return MakeMemberReferenceHandle(method);
                case TypeDesc type: return MakeTypeRefOrSpecHandle(type);

                default:
                    throw new NotSupportedException();
            }
        }

        public BlobHandle EmitSignatureBlobForMethodSignature(MethodSignature signature)
        {
            return MakeSignatureHandle(signature);
        }

        /// <summary>
        /// Encode a type signature into a specified blob.
        /// </summary>
        /// <param name="blobBuilder">Blob to encode type signature into. Must not be null</param>
        public void EncodeSignatureForType(TypeDesc type, BlobBuilder blobBuilder)
        {
            SignatureTypeEncoder sigEncoder = new SignatureTypeEncoder(blobBuilder);
            _signatureEmitter.EncodeTypeSignature(sigEncoder, type);
        }

        public void EmitOutputFile(string outputPath)
        {
            using (FileStream sw = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite))
            {
                EmitToStream(sw);
            }
        }

        public void EmitToStream(Stream stream)
        {
            foreach (var typeDef in _emittedTypes)
            {
                MethodDefinitionHandle? firstMethodHandle = null;

                foreach (var methodDef in typeDef.Methods)
                {
                    int bodyOffset = _methodBodyStream.AddMethodBody(methodDef.Code);

                    BlobHandle signature = MakeSignatureHandle(methodDef.Signature);

                    MethodDefinitionHandle methodHandle = _metadataBuilder.AddMethodDefinition(
                        MethodAttributes.PrivateScope | MethodAttributes.Static,
                        MethodImplAttributes.IL | MethodImplAttributes.Managed,
                        _metadataBuilder.GetOrAddString(methodDef.Name),
                        signature,
                        bodyOffset,
                        parameterList: default(ParameterHandle));

                    if (firstMethodHandle == null)
                        firstMethodHandle = methodHandle;
                }

                _metadataBuilder.AddTypeDefinition(
                    default(TypeAttributes),
                    default(StringHandle),
                    _metadataBuilder.GetOrAddString(typeDef.Name),
                    typeDef.IsValueType ?
                        MakeTypeRefHandle(_typeSystemContext.GetWellKnownType(WellKnownType.ValueType)) :
                        MakeTypeRefHandle(_typeSystemContext.GetWellKnownType(WellKnownType.Object)),
                    fieldList: MetadataTokens.FieldDefinitionHandle(1),
                    methodList: firstMethodHandle.Value);
            }

            BlobBuilder peBlob = new BlobBuilder();
            new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(_metadataBuilder), _methodBodyStream.Builder).Serialize(peBlob);

            peBlob.WriteContentTo(stream);

            // Clear some variables to catch any caller trying to emit data after writing the output file
            _emittedTypes = null;
            _metadataBuilder = null;
            _methodBodyStream = default(MethodBodyStreamEncoder);
        }

        #region TypeSystem Entities To Handle Encoders
        private Dictionary<IAssemblyDesc, AssemblyReferenceHandle> _assemblyRefHandles = new Dictionary<IAssemblyDesc, AssemblyReferenceHandle>();
        private Dictionary<TypeDesc, EntityHandle> _typeRefOrSpecHandles = new Dictionary<TypeDesc, EntityHandle>();
        private Dictionary<TypeSystemEntity, EntityHandle> _memberRefOrSpecHandles = new Dictionary<TypeSystemEntity, EntityHandle>();
        private Dictionary<MethodSignature, BlobHandle> _methodSignatureHandles = new Dictionary<MethodSignature, BlobHandle>();
        private Dictionary<FieldDesc, BlobHandle> _fieldSignatureHandles = new Dictionary<FieldDesc, BlobHandle>();

        private struct EntityProviderForEcmaSignature : IEntityHandleProvider
        {
            private ManagedBinaryEmitter _emitter;

            public EntityProviderForEcmaSignature(ManagedBinaryEmitter emitter)
            {
                _emitter = emitter;
            }

            public EntityHandle GetTypeDefOrRefHandleForTypeDesc(TypeDesc type)
            {
                return _emitter.MakeTypeRefHandle(type);
            }
        }

        private EcmaSignatureEncoder<EntityProviderForEcmaSignature> _signatureEmitter;

        private BlobHandle MakeSignatureHandle(MethodSignature signature)
        {
            BlobHandle handle;

            if (!_methodSignatureHandles.TryGetValue(signature, out handle))
            {
                BlobBuilder metadataSignature = new BlobBuilder();

                _signatureEmitter.EncodeMethodSignature(metadataSignature, signature);

                _methodSignatureHandles[signature] = handle = _metadataBuilder.GetOrAddBlob(metadataSignature);
            }

            return handle;
        }

        private BlobHandle MakeSignatureHandle(TypeSystemEntity methodOrField)
        {
            if (methodOrField is MethodDesc)
            {
                return MakeSignatureHandle(((MethodDesc)methodOrField).Signature);
            }
            else
            {
                BlobHandle handle;
                FieldDesc field = (FieldDesc)methodOrField;

                if (!_fieldSignatureHandles.TryGetValue(field, out handle))
                {
                    BlobBuilder metadataSignature = new BlobBuilder();

                    SignatureTypeEncoder fieldSigEncoder = new BlobEncoder(metadataSignature).FieldSignature();
                    _signatureEmitter.EncodeTypeSignature(fieldSigEncoder, field.FieldType);

                    _fieldSignatureHandles[field] = handle = _metadataBuilder.GetOrAddBlob(metadataSignature);
                }

                return handle;
            }
        }

        private AssemblyReferenceHandle MakeAssemblyReferenceHandle(IAssemblyDesc assemblyRef)
        {
            AssemblyReferenceHandle handle;

            if (!_assemblyRefHandles.TryGetValue(assemblyRef, out handle))
            {
                AssemblyName assemblyName = assemblyRef.GetName();

                handle = _metadataBuilder.AddAssemblyReference(
                    _metadataBuilder.GetOrAddString(assemblyName.Name),
                    assemblyName.Version,
                    default(StringHandle),
                    _metadataBuilder.GetOrAddBlob(ImmutableArray.Create<byte>(assemblyName.GetPublicKeyToken())),
                    default(AssemblyFlags),
                    default(BlobHandle));

                _assemblyRefHandles[assemblyRef] = handle;
            }

            return handle;
        }

        private EntityHandle MakeTypeRefHandle(TypeDesc type)
        {
            Debug.Assert(type.IsTypeDefinition);
            Debug.Assert(type is MetadataType);

            EntityHandle handle;

            if (!_typeRefOrSpecHandles.TryGetValue(type, out handle))
            {
                EntityHandle scope;
                MetadataType typeAsMetadataType = (MetadataType)type;

                if (typeAsMetadataType.ContainingType != null)
                    scope = MakeTypeRefHandle(typeAsMetadataType.ContainingType);
                else
                    scope = MakeAssemblyReferenceHandle((IAssemblyDesc)typeAsMetadataType.Module);

                handle = _metadataBuilder.AddTypeReference(
                    scope,
                    _metadataBuilder.GetOrAddString(typeAsMetadataType.Namespace),
                    _metadataBuilder.GetOrAddString(typeAsMetadataType.Name));

                _typeRefOrSpecHandles[type] = handle;
            }

            return handle;
        }

        private EntityHandle MakeTypeRefOrSpecHandle(TypeDesc type)
        {
            EntityHandle handle;

            if (!_typeRefOrSpecHandles.TryGetValue(type, out handle))
            {
                if(!type.IsDefType || !type.IsTypeDefinition || type is RuntimeDeterminedType)
                {
                    SignatureTypeEncoder sigEncoder = new SignatureTypeEncoder(new BlobBuilder());
                    _signatureEmitter.EncodeTypeSignature(sigEncoder, type);
                    handle = _metadataBuilder.AddTypeSpecification(_metadataBuilder.GetOrAddBlob(sigEncoder.Builder));
                }
                else
                {
                    handle = MakeTypeRefHandle(type);
                }

                _typeRefOrSpecHandles[type] = handle;
            }

            return handle;
        }

        private EntityHandle MakeMemberReferenceHandle(TypeSystemEntity methodOrField)
        {
            EntityHandle handle;

            if (!_memberRefOrSpecHandles.TryGetValue(methodOrField, out handle))
            {
                MethodDesc method = methodOrField as MethodDesc;
                FieldDesc field = methodOrField as FieldDesc;
                TypeDesc owningType = (method != null ? method.OwningType : field.OwningType);
                string name = (method != null ? method.Name : field.Name);

                BlobHandle signature = method != null ?
                    MakeSignatureHandle(method.GetTypicalMethodDefinition()) :
                    MakeSignatureHandle(field);

                handle = _metadataBuilder.AddMemberReference(
                    MakeTypeRefOrSpecHandle(owningType),
                    _metadataBuilder.GetOrAddString(name),
                    signature);

                if (method != null && method.HasInstantiation && !method.IsTypicalMethodDefinition)
                {
                    BlobEncoder methodSpecEncoder = new BlobEncoder(new BlobBuilder());

                    GenericTypeArgumentsEncoder argEncoder = methodSpecEncoder.MethodSpecificationSignature(method.Instantiation.Length);
                    for (int i = 0; i < method.Instantiation.Length; i++)
                    {
                        SignatureTypeEncoder argTypeEncoder = argEncoder.AddArgument();
                        _signatureEmitter.EncodeTypeSignature(argTypeEncoder, method.Instantiation[i]);
                    }

                    handle = _metadataBuilder.AddMethodSpecification(handle, _metadataBuilder.GetOrAddBlob(methodSpecEncoder.Builder));
                }

                _memberRefOrSpecHandles[methodOrField] = handle;
            }

            return handle;
        }
        #endregion
    }

    public static class InstructionEncoderExtensions
    {
        public static void EmitLdToken(this InstructionEncoder code, TypeSystemEntity typeSystemEntity, ManagedBinaryEmitter emitter)
        {
            code.OpCode(ILOpCode.Ldtoken);
            code.Token(emitter.EmitMetadataHandleForTypeSystemEntity(typeSystemEntity));
        }
        public static void EmitI4Constant(this InstructionEncoder code, int value)
        {
            code.OpCode(ILOpCode.Ldc_i4);
            code.CodeBuilder.WriteInt32(value);
        }
    }
}
