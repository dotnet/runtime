// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Internal.TypeSystem;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    class TypeSystemMetadataEmitter
    {
        MetadataBuilder _metadataBuilder;
        BlobBuilder _ilBuilder;
        MethodBodyStreamEncoder _methodBodyStream;
        Dictionary<IAssemblyDesc, AssemblyReferenceHandle> _assemblyRefs = new Dictionary<IAssemblyDesc, AssemblyReferenceHandle>();
        Dictionary<TypeDesc, EntityHandle> _typeRefs = new Dictionary<TypeDesc, EntityHandle>();
        Dictionary<MethodDesc, EntityHandle> _methodRefs = new Dictionary<MethodDesc, EntityHandle>();
        Blob _mvidFixup;
        BlobHandle _noArgsVoidReturnStaticMethodSigHandle;

        public TypeSystemMetadataEmitter(AssemblyName assemblyName, TypeSystemContext context, AssemblyFlags flags = default(AssemblyFlags))
        {
            _metadataBuilder = new MetadataBuilder();
            _ilBuilder = new BlobBuilder();
            _methodBodyStream = new MethodBodyStreamEncoder(_ilBuilder);
            StringHandle assemblyNameHandle = _metadataBuilder.GetOrAddString(assemblyName.Name);
            if (assemblyName.CultureName != null)
                throw new ArgumentException("assemblyName");

            if (assemblyName.GetPublicKeyToken() != null)
                throw new ArgumentException("assemblyName");

            var mvid = _metadataBuilder.ReserveGuid();
            _mvidFixup = mvid.Content;

            _metadataBuilder.AddModule(0, assemblyNameHandle, mvid.Handle, default(GuidHandle), default(GuidHandle));
            _metadataBuilder.AddAssembly(assemblyNameHandle, assemblyName.Version ?? new Version(0,0,0,0), default(StringHandle), default(BlobHandle), flags, AssemblyHashAlgorithm.None);

            var canonAssemblyNameHandle = _metadataBuilder.GetOrAddString("System.Private.Canon");
            var canonAssemblyRef = _metadataBuilder.AddAssemblyReference(canonAssemblyNameHandle, new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), (AssemblyFlags)0, default(BlobHandle));
            var systemStringHandle = _metadataBuilder.GetOrAddString("System");
            var canonStringHandle = _metadataBuilder.GetOrAddString("__Canon");
            var canonTypeRef = _metadataBuilder.AddTypeReference(canonAssemblyRef, systemStringHandle, canonStringHandle);
            _typeRefs.Add(context.CanonType, canonTypeRef);

            _metadataBuilder.AddTypeDefinition(
               default(TypeAttributes),
               default(StringHandle),
               _metadataBuilder.GetOrAddString("<Module>"),
               baseType: default(EntityHandle),
               fieldList: MetadataTokens.FieldDefinitionHandle(1),
               methodList: MetadataTokens.MethodDefinitionHandle(1));

            BlobBuilder noArgsNoReturnStaticMethodSig = new BlobBuilder();
            BlobEncoder signatureEncoder = new BlobEncoder(noArgsNoReturnStaticMethodSig);

            signatureEncoder.MethodSignature(SignatureCallingConvention.Default, 0, false);
            noArgsNoReturnStaticMethodSig.WriteCompressedInteger(0);
            noArgsNoReturnStaticMethodSig.WriteByte((byte)SignatureTypeCode.Void);
            _noArgsVoidReturnStaticMethodSigHandle = _metadataBuilder.GetOrAddBlob(noArgsNoReturnStaticMethodSig);
        }

        public MethodDefinitionHandle AddGlobalMethod(string name, InstructionEncoder il, int maxStack)
        {
            int methodILOffset = _methodBodyStream.AddMethodBody(il, maxStack);
            return _metadataBuilder.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static,
                MethodImplAttributes.IL, _metadataBuilder.GetOrAddString(name),
                _noArgsVoidReturnStaticMethodSigHandle,
                methodILOffset,
                default(ParameterHandle));
        }

        private static readonly Guid s_guid = new Guid("97F4DBD4-F6D1-4FAD-91B3-1001F92068E5");
        private static readonly BlobContentId s_contentId = new BlobContentId(s_guid, 0x04030201);

        public void SerializeToStream(Stream peStream)
        {
            var peHeaderBuilder = new PEHeaderBuilder();
            var peBuilder = new ManagedPEBuilder(peHeaderBuilder, new MetadataRootBuilder(_metadataBuilder), _ilBuilder,
                deterministicIdProvider: content => s_contentId);

            var peBlob = new BlobBuilder();
            var contentId = peBuilder.Serialize(peBlob);
            new BlobWriter(_mvidFixup).WriteGuid(contentId.Guid);
            peBlob.WriteContentTo(peStream);
        }

        public AssemblyReferenceHandle GetAssemblyRef(IAssemblyDesc assemblyDesc)
        {
            if (_assemblyRefs.TryGetValue(assemblyDesc, out var handle))
            {
                return handle;
            }
            AssemblyName name = assemblyDesc.GetName();
            StringHandle assemblyName = _metadataBuilder.GetOrAddString(name.Name);
            StringHandle cultureName = (name.CultureName != null) ? _metadataBuilder.GetOrAddString(name.CultureName) : default(StringHandle);
            BlobHandle publicTokenBlob = name.GetPublicKeyToken() != null ? _metadataBuilder.GetOrAddBlob(name.GetPublicKeyToken()) : default(BlobHandle);
            AssemblyFlags flags = default(AssemblyFlags);
            if (name.Flags.HasFlag(AssemblyNameFlags.Retargetable))
            {
                flags |= AssemblyFlags.Retargetable;
            }
            if (name.ContentType == AssemblyContentType.WindowsRuntime)
            {
                flags |= AssemblyFlags.WindowsRuntime;
            }

            var referenceHandle = _metadataBuilder.AddAssemblyReference(assemblyName, name.Version, cultureName, publicTokenBlob, flags, default(BlobHandle));
            _assemblyRefs.Add(assemblyDesc, referenceHandle);
            return referenceHandle;
        }

        public EntityHandle GetTypeRef(MetadataType type)
        {
            if (_typeRefs.TryGetValue(type, out var handle))
            {
                return handle;
            }

            if (type.IsParameterizedType)
            {
                throw new ArgumentException("type");
            }
            else if (type.IsFunctionPointer)
            {
                throw new ArgumentException("type");
            }

            EntityHandle typeHandle;

            if (type.IsTypeDefinition)
            {
                // Make a typeref
                StringHandle typeName = _metadataBuilder.GetOrAddString(type.Name);
                StringHandle typeNamespace = type.Namespace != null ? _metadataBuilder.GetOrAddString(type.Namespace) : default(StringHandle);
                EntityHandle resolutionScope;

                if (type.ContainingType == null)
                {
                    // non-nested type
                    resolutionScope = GetAssemblyRef(type.Module.Assembly);
                }
                else
                {
                    // nested type
                    resolutionScope = GetTypeRef((MetadataType)type.ContainingType);
                }

                typeHandle = _metadataBuilder.AddTypeReference(resolutionScope, typeNamespace, typeName);
            }
            else
            {
                var typeSpecSignature = new BlobBuilder();
                EncodeType(typeSpecSignature, type, EmbeddedSignatureDataEmitter.EmptySingleton);
                var blobSigHandle = _metadataBuilder.GetOrAddBlob(typeSpecSignature);
                typeHandle = _metadataBuilder.AddTypeSpecification(blobSigHandle);
            }

            _typeRefs.Add(type, typeHandle);
            return typeHandle;
        }

        public EntityHandle GetMethodRef(MethodDesc method)
        {
            if (_methodRefs.TryGetValue(method, out var handle))
            {
                return handle;
            }

            EntityHandle methodHandle;

            if (method.HasInstantiation && (method.GetMethodDefinition() != method))
            {
                EntityHandle uninstantiatedHandle = GetMethodRef(method.GetMethodDefinition());
                BlobBuilder methodSpecSig = new BlobBuilder();
                BlobEncoder methodSpecEncoder = new BlobEncoder(methodSpecSig);
                methodSpecEncoder.MethodSpecificationSignature(method.Instantiation.Length);
                foreach (var type in method.Instantiation)
                    EncodeType(methodSpecSig, type, EmbeddedSignatureDataEmitter.EmptySingleton);

                var methodSpecSigHandle = _metadataBuilder.GetOrAddBlob(methodSpecSig);
                methodHandle = _metadataBuilder.AddMethodSpecification(uninstantiatedHandle, methodSpecSigHandle);
            }
            else
            {
                EntityHandle typeHandle = GetTypeRef((MetadataType)method.OwningType);
                StringHandle methodName = _metadataBuilder.GetOrAddString(method.Name);
                var sig = method.GetTypicalMethodDefinition().Signature;

                EmbeddedSignatureDataEmitter signatureDataEmitter;
                if (sig.HasEmbeddedSignatureData)
                {
                    signatureDataEmitter = new EmbeddedSignatureDataEmitter(sig.GetEmbeddedSignatureData(), this);
                }
                else
                {
                    signatureDataEmitter = EmbeddedSignatureDataEmitter.EmptySingleton;
                }

                BlobBuilder memberRefSig = new BlobBuilder();
                EncodeMethodSignature(memberRefSig, sig, signatureDataEmitter);

                if (!signatureDataEmitter.Complete)
                    throw new ArgumentException();

                var sigBlob = _metadataBuilder.GetOrAddBlob(memberRefSig);
                methodHandle = _metadataBuilder.AddMemberReference(typeHandle, methodName, sigBlob);
            }

            _methodRefs.Add(method, methodHandle);
            return methodHandle;
        }

        private void EncodeType(BlobBuilder blobBuilder, TypeDesc type, EmbeddedSignatureDataEmitter signatureDataEmitter)
        {
            signatureDataEmitter.Push();
            signatureDataEmitter.Push();
            signatureDataEmitter.EmitAtCurrentIndexStack(blobBuilder);
            signatureDataEmitter.Pop();

            signatureDataEmitter.Push();
            if (type.IsPrimitive)
            {
                SignatureTypeCode primitiveCode;
                switch (type.Category)
                {
                    case TypeFlags.Void:
                        primitiveCode = SignatureTypeCode.Void;
                        break;
                    case TypeFlags.Boolean:
                        primitiveCode = SignatureTypeCode.Boolean;
                        break;
                    case TypeFlags.Char:
                        primitiveCode = SignatureTypeCode.Char;
                        break;
                    case TypeFlags.SByte:
                        primitiveCode = SignatureTypeCode.SByte;
                        break;
                    case TypeFlags.Byte:
                        primitiveCode = SignatureTypeCode.Byte;
                        break;
                    case TypeFlags.Int16:
                        primitiveCode = SignatureTypeCode.Int16;
                        break;
                    case TypeFlags.UInt16:
                        primitiveCode = SignatureTypeCode.UInt16;
                        break;
                    case TypeFlags.Int32:
                        primitiveCode = SignatureTypeCode.Int32;
                        break;
                    case TypeFlags.UInt32:
                        primitiveCode = SignatureTypeCode.UInt32;
                        break;
                    case TypeFlags.Int64:
                        primitiveCode = SignatureTypeCode.Int64;
                        break;
                    case TypeFlags.UInt64:
                        primitiveCode = SignatureTypeCode.UInt64;
                        break;
                    case TypeFlags.IntPtr:
                        primitiveCode = SignatureTypeCode.IntPtr;
                        break;
                    case TypeFlags.UIntPtr:
                        primitiveCode = SignatureTypeCode.UIntPtr;
                        break;
                    case TypeFlags.Single:
                        primitiveCode = SignatureTypeCode.Single;
                        break;
                    case TypeFlags.Double:
                        primitiveCode = SignatureTypeCode.Double;
                        break;
                    default:
                        throw new Exception("Unknown primitive type");
                }

                blobBuilder.WriteByte((byte)primitiveCode);
            }
            else if (type.IsSzArray)
            {
                blobBuilder.WriteByte((byte)SignatureTypeCode.SZArray);
                EncodeType(blobBuilder, type.GetParameterType(), signatureDataEmitter);
            }
            else if (type.IsArray)
            {
                var arrayType = (ArrayType)type;
                blobBuilder.WriteByte((byte)SignatureTypeCode.Array);
                EncodeType(blobBuilder, type.GetParameterType(), signatureDataEmitter);
                var shapeEncoder = new ArrayShapeEncoder(blobBuilder);
                // TODO Add support for non-standard array shapes
                shapeEncoder.Shape(arrayType.Rank, default(ImmutableArray<int>), default(ImmutableArray<int>));
            }
            else if (type.IsPointer)
            {
                blobBuilder.WriteByte((byte)SignatureTypeCode.Pointer);
                EncodeType(blobBuilder, type.GetParameterType(), signatureDataEmitter);
            }
            else if (type.IsFunctionPointer)
            {
                FunctionPointerType fnptrType = (FunctionPointerType)type;
                EncodeMethodSignature(blobBuilder, fnptrType.Signature, signatureDataEmitter);
            }
            else if (type.IsByRef)
            {
                blobBuilder.WriteByte((byte)SignatureTypeCode.ByReference);
                EncodeType(blobBuilder, type.GetParameterType(), signatureDataEmitter);
            }
            else if (type.IsObject)
            {
                blobBuilder.WriteByte((byte)SignatureTypeCode.Object);
            }
            else if (type.IsString)
            {
                blobBuilder.WriteByte((byte)SignatureTypeCode.String);
            }
            else if (type.IsWellKnownType(WellKnownType.TypedReference))
            {
                blobBuilder.WriteByte((byte)SignatureTypeCode.TypedReference);
            }
            else if (type.IsWellKnownType(WellKnownType.Void))
            {
                blobBuilder.WriteByte((byte)SignatureTypeCode.Void);
            }
            else if (type is SignatureVariable)
            {
                SignatureVariable sigVar = (SignatureVariable)type;
                SignatureTypeCode code = sigVar.IsMethodSignatureVariable ? SignatureTypeCode.GenericMethodParameter : SignatureTypeCode.GenericTypeParameter;
                blobBuilder.WriteByte((byte)code);
                blobBuilder.WriteCompressedInteger(sigVar.Index);
            }
            else if (type is InstantiatedType)
            {
                blobBuilder.WriteByte((byte)SignatureTypeCode.GenericTypeInstance);
                EncodeType(blobBuilder, type.GetTypeDefinition(), signatureDataEmitter);
                blobBuilder.WriteCompressedInteger(type.Instantiation.Length);
                foreach (var instantiationArg in type.Instantiation)
                    EncodeType(blobBuilder, instantiationArg, signatureDataEmitter);
            }
            else if (type is MetadataType)
            {
                var metadataType = (MetadataType)type;
                // Must be class or valuetype
                blobBuilder.WriteByte(type.IsValueType ? (byte)SignatureTypeKind.ValueType : (byte)SignatureTypeKind.Class);
                int codedIndex = CodedIndex.TypeDefOrRef(GetTypeRef(metadataType));
                blobBuilder.WriteCompressedInteger(codedIndex);
            }
            else
            {
                throw new Exception("Unexpected type");
            }
            signatureDataEmitter.Pop();
            signatureDataEmitter.Pop();
        }

        class EmbeddedSignatureDataEmitter
        {
            EmbeddedSignatureData[] _embeddedData;
            int _embeddedDataIndex;
            Stack<int> _indexStack = new Stack<int>();
            TypeSystemMetadataEmitter _metadataEmitter;

            public static EmbeddedSignatureDataEmitter EmptySingleton = new EmbeddedSignatureDataEmitter(null, null);

            public EmbeddedSignatureDataEmitter(EmbeddedSignatureData[] embeddedData, TypeSystemMetadataEmitter metadataEmitter)
            {
                _embeddedData = embeddedData;
                _indexStack.Push(0);
                _metadataEmitter = metadataEmitter;
            }

            public void Push()
            {
                if (!Complete)
                {
                    int was = _indexStack.Pop();
                    _indexStack.Push(was + 1);
                    _indexStack.Push(0);
                }
            }

            public void EmitAtCurrentIndexStack(BlobBuilder signatureBuilder)
            {
                if (!Complete)
                {
                    if (_embeddedDataIndex < _embeddedData.Length)
                    {
                        string indexData = string.Join(".", _indexStack);
                        while ((_embeddedDataIndex < _embeddedData.Length) && _embeddedData[_embeddedDataIndex].index == indexData)
                        {
                            switch (_embeddedData[_embeddedDataIndex].kind)
                            {
                                case EmbeddedSignatureDataKind.OptionalCustomModifier:
                                    {
                                        signatureBuilder.WriteByte((byte)SignatureTypeCode.OptionalModifier);
                                        EntityHandle handle = _metadataEmitter.GetTypeRef((MetadataType)_embeddedData[_embeddedDataIndex].type);
                                        signatureBuilder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(handle));
                                    }
                                    break;

                                case EmbeddedSignatureDataKind.RequiredCustomModifier:
                                    {
                                        signatureBuilder.WriteByte((byte)SignatureTypeCode.RequiredModifier);
                                        EntityHandle handle = _metadataEmitter.GetTypeRef((MetadataType)_embeddedData[_embeddedDataIndex].type);
                                        signatureBuilder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(handle));
                                    }
                                    break;

                                default:
                                    throw new NotImplementedException();
                            }

                            _embeddedDataIndex++;
                        }
                    }
                }
            }

            public bool Complete
            {
                get
                {
                    if (_embeddedData == null)
                        return true;

                    return _embeddedDataIndex >= _embeddedData.Length;
                }
            }

            public void Pop()
            {
                if (!Complete)
                {
                    _indexStack.Pop();
                }
            }
        }

        void EncodeMethodSignature(BlobBuilder signatureBuilder, MethodSignature sig, EmbeddedSignatureDataEmitter signatureDataEmitter)
        {
            signatureDataEmitter.Push();
            BlobEncoder signatureEncoder = new BlobEncoder(signatureBuilder);
            int genericParameterCount = sig.GenericParameterCount;
            bool isInstanceMethod = !sig.IsStatic;
            SignatureCallingConvention sigCallingConvention = SignatureCallingConvention.Default;
            switch (sig.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask)
            {
                case MethodSignatureFlags.CallingConventionVarargs:
                    sigCallingConvention = SignatureCallingConvention.VarArgs;
                    break;
                case MethodSignatureFlags.UnmanagedCallingConventionCdecl:
                    sigCallingConvention = SignatureCallingConvention.CDecl;
                    break;
                case MethodSignatureFlags.UnmanagedCallingConventionStdCall:
                    sigCallingConvention = SignatureCallingConvention.StdCall;
                    break;
                case MethodSignatureFlags.UnmanagedCallingConventionThisCall:
                    sigCallingConvention = SignatureCallingConvention.ThisCall;
                    break;
                case MethodSignatureFlags.UnmanagedCallingConvention:
                    // [TODO] sigCallingConvention = SignatureCallingConvention.Unmanaged;
                    sigCallingConvention = (SignatureCallingConvention)9;
                    break;
            }

            signatureEncoder.MethodSignature(sigCallingConvention, genericParameterCount, isInstanceMethod);
            signatureBuilder.WriteCompressedInteger(sig.Length);
            // TODO Process custom modifiers in some way
            EncodeType(signatureBuilder, sig.ReturnType, signatureDataEmitter);
            for (int i = 0; i < sig.Length; i++)
                EncodeType(signatureBuilder, sig[i], signatureDataEmitter);

            signatureDataEmitter.Pop();
        }

        public UserStringHandle GetUserStringHandle(string userString)
        {
            return _metadataBuilder.GetOrAddUserString(userString);
        }
    }
}
