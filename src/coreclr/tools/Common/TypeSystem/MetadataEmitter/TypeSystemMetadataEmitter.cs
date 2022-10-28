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

namespace Internal.TypeSystem
{
#pragma warning disable CA1852
    internal class TypeSystemMetadataEmitter
#pragma warning restore CA1852
    {
        private MetadataBuilder _metadataBuilder;
        private BlobBuilder _ilBuilder;
        private MethodBodyStreamEncoder _methodBodyStream;
        private Dictionary<string, AssemblyReferenceHandle> _assemblyRefNameHandles = new Dictionary<string, AssemblyReferenceHandle>();
        private Dictionary<IAssemblyDesc, AssemblyReferenceHandle> _assemblyRefs = new Dictionary<IAssemblyDesc, AssemblyReferenceHandle>();
        private Dictionary<TypeDesc, EntityHandle> _typeRefs = new Dictionary<TypeDesc, EntityHandle>();
        private Dictionary<MethodDesc, EntityHandle> _methodRefs = new Dictionary<MethodDesc, EntityHandle>();
        private Dictionary<FieldDesc, EntityHandle> _fieldRefs = new Dictionary<FieldDesc, EntityHandle>();
        private Blob _mvidFixup;
        private BlobHandle _noArgsVoidReturnStaticMethodSigHandle;
        protected TypeSystemContext _typeSystemContext;

        public TypeSystemMetadataEmitter(AssemblyName assemblyName, TypeSystemContext context, AssemblyFlags flags = default(AssemblyFlags), byte[] publicKeyArray = null, AssemblyHashAlgorithm hashAlgorithm = AssemblyHashAlgorithm.None)
        {
            _typeSystemContext = context;
            _metadataBuilder = new MetadataBuilder();
            _ilBuilder = new BlobBuilder();
            _methodBodyStream = new MethodBodyStreamEncoder(_ilBuilder);
            StringHandle assemblyNameHandle = _metadataBuilder.GetOrAddString(assemblyName.Name);
            if (assemblyName.CultureName != null)
                throw new ArgumentException("assemblyName");

            var mvid = _metadataBuilder.ReserveGuid();
            _mvidFixup = mvid.Content;

            _metadataBuilder.AddModule(0, assemblyNameHandle, mvid.Handle, default(GuidHandle), default(GuidHandle));
            _metadataBuilder.AddAssembly(assemblyNameHandle, assemblyName.Version ?? new Version(0, 0, 0, 0), default(StringHandle), publicKey: publicKeyArray != null ? _metadataBuilder.GetOrAddBlob(publicKeyArray) : default(BlobHandle), flags, AssemblyHashAlgorithm.None);

            _metadataBuilder.AddTypeDefinition(
               default(TypeAttributes),
               default(StringHandle),
               _metadataBuilder.GetOrAddString("<Module>"),
               baseType: default(EntityHandle),
               fieldList: MetadataTokens.FieldDefinitionHandle(1),
               methodList: MetadataTokens.MethodDefinitionHandle(1));
        }

        public void InjectSystemPrivateCanon()
        {
            var canonAssemblyNameHandle = _metadataBuilder.GetOrAddString("System.Private.Canon");
            var canonAssemblyRef = _metadataBuilder.AddAssemblyReference(canonAssemblyNameHandle, new Version(0, 0, 0, 0), default(StringHandle), default(BlobHandle), (AssemblyFlags)0, default(BlobHandle));
            var systemStringHandle = _metadataBuilder.GetOrAddString("System");
            var canonStringHandle = _metadataBuilder.GetOrAddString("__Canon");
            var canonTypeRef = _metadataBuilder.AddTypeReference(canonAssemblyRef, systemStringHandle, canonStringHandle);
            _typeRefs.Add(_typeSystemContext.CanonType, canonTypeRef);
        }

        public void AllowUseOfAddGlobalMethod()
        {
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

        public MetadataBuilder Builder => _metadataBuilder;

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

        // Generate only the metadata blob as a byte[]
        public byte[] EmitToMetadataBlob()
        {
            MetadataRootBuilder metadataRootBuilder = new MetadataRootBuilder(_metadataBuilder);
            BlobBuilder metadataBlobBuilder = new BlobBuilder();
            metadataRootBuilder.Serialize(metadataBlobBuilder, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);

            // Clear some variables to catch any caller trying to emit data after writing the output file
            _metadataBuilder = null;

            return metadataBlobBuilder.ToArray();
        }

        public AssemblyReferenceHandle GetAssemblyRef(AssemblyName name)
        {
            if (!_assemblyRefNameHandles.TryGetValue(name.FullName, out var handle))
            {
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

                Version version = name.Version;
                if (version == null)
                    version = new Version(0, 0);

                handle = _metadataBuilder.AddAssemblyReference(assemblyName, version, cultureName, publicTokenBlob, flags, default(BlobHandle));

                _assemblyRefNameHandles[name.FullName] = handle;
            }
            return handle;
        }

        public AssemblyReferenceHandle GetAssemblyRef(IAssemblyDesc assemblyDesc)
        {
            if (_assemblyRefs.TryGetValue(assemblyDesc, out var handle))
            {
                return handle;
            }
            AssemblyName name = assemblyDesc.GetName();
            var referenceHandle = GetAssemblyRef(name);
            _assemblyRefs.Add(assemblyDesc, referenceHandle);
            return referenceHandle;
        }

        public EntityHandle EmitMetadataHandleForTypeSystemEntity(TypeSystemEntity entity)
        {
            switch (entity)
            {
                case FieldDesc field: return GetFieldRef(field);
                case MethodDesc method: return GetMethodRef(method);
                case TypeDesc type: return GetTypeRef(type);
                case ModuleDesc assembly: return GetAssemblyRef(assembly.Assembly);
                case MethodSignature methodSignature: return GetStandaloneSig(methodSignature);

                default:
                    throw new NotSupportedException();
            }
        }

        public IEnumerable<KeyValuePair<TypeSystemEntity, EntityHandle>> TypeSystemEntitiesKnown
        {
            get
            {
                foreach (var item in _typeRefs)
                {
                    yield return new KeyValuePair<TypeSystemEntity, EntityHandle>(item.Key, item.Value);
                }

                foreach (var item in _methodRefs)
                {
                    yield return new KeyValuePair<TypeSystemEntity, EntityHandle>(item.Key, item.Value);
                }

                foreach (var item in _fieldRefs)
                {
                    yield return new KeyValuePair<TypeSystemEntity, EntityHandle>(item.Key, item.Value);
                }
            }
        }

        protected virtual EntityHandle GetNonNestedResolutionScope(MetadataType metadataType)
        {
            return GetAssemblyRef(metadataType.Module.Assembly);
        }

        public EntityHandle GetTypeRef(TypeDesc type)
        {
            if (_typeRefs.TryGetValue(type, out var handle))
            {
                return handle;
            }

            if (type.IsFunctionPointer)
            {
                throw new ArgumentException("type");
            }

            EntityHandle typeHandle;

            if (type.IsTypeDefinition && type is MetadataType metadataType)
            {
                // Make a typeref
                StringHandle typeName = _metadataBuilder.GetOrAddString(metadataType.Name);
                StringHandle typeNamespace = metadataType.Namespace != null ? _metadataBuilder.GetOrAddString(metadataType.Namespace) : default(StringHandle);
                EntityHandle resolutionScope;

                if (metadataType.ContainingType == null)
                {
                    // non-nested type
                    resolutionScope = GetNonNestedResolutionScope(metadataType);
                }
                else
                {
                    // nested type
                    resolutionScope = GetTypeRef((MetadataType)metadataType.ContainingType);
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

        private BlobHandle GetMethodSignatureBlobHandle(MethodSignature sig)
        {
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
            return sigBlob;
        }

        private BlobHandle GetFieldSignatureBlobHandle(FieldDesc field)
        {
            var embeddedSigData = field.GetEmbeddedSignatureData();
            EmbeddedSignatureDataEmitter signatureDataEmitter;
            if (embeddedSigData != null && embeddedSigData.Length != 0)
            {
                signatureDataEmitter = new EmbeddedSignatureDataEmitter(embeddedSigData, this);
            }
            else
            {
                signatureDataEmitter = EmbeddedSignatureDataEmitter.EmptySingleton;
            }

            BlobBuilder memberRefSig = new BlobBuilder();
            EncodeFieldSignature(memberRefSig, field.FieldType, signatureDataEmitter);

            if (!signatureDataEmitter.Complete)
                throw new ArgumentException();

            var sigBlob = _metadataBuilder.GetOrAddBlob(memberRefSig);
            return sigBlob;
        }

        public EntityHandle GetStandaloneSig(MethodSignature sig)
        {
            var sigBlob = GetMethodSignatureBlobHandle(sig);
            return _metadataBuilder.AddStandaloneSignature(sigBlob);
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
                EntityHandle typeHandle = GetTypeRef(method.OwningType);
                StringHandle methodName = _metadataBuilder.GetOrAddString(method.Name);
                var sig = method.GetTypicalMethodDefinition().Signature;
                var sigBlob = GetMethodSignatureBlobHandle(sig);

                methodHandle = _metadataBuilder.AddMemberReference(typeHandle, methodName, sigBlob);
            }

            _methodRefs.Add(method, methodHandle);
            return methodHandle;
        }

        public EntityHandle GetFieldRef(FieldDesc field)
        {
            if (_fieldRefs.TryGetValue(field, out var handle))
            {
                return handle;
            }

            EntityHandle fieldHandle;

            EntityHandle typeHandle = GetTypeRef((MetadataType)field.OwningType);
            StringHandle fieldName = _metadataBuilder.GetOrAddString(field.Name);

            var sigBlob = GetFieldSignatureBlobHandle(field.GetTypicalFieldDefinition());
            fieldHandle = _metadataBuilder.AddMemberReference(typeHandle, fieldName, sigBlob);

            _fieldRefs.Add(field, fieldHandle);
            return fieldHandle;
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

                signatureDataEmitter.EmitArrayShapeAtCurrentIndexStack(blobBuilder, arrayType.Rank);
            }
            else if (type.IsPointer)
            {
                blobBuilder.WriteByte((byte)SignatureTypeCode.Pointer);
                EncodeType(blobBuilder, type.GetParameterType(), signatureDataEmitter);
            }
            else if (type.IsFunctionPointer)
            {
                FunctionPointerType fnptrType = (FunctionPointerType)type;
                blobBuilder.WriteByte((byte)SignatureTypeCode.FunctionPointer);
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
            else if (type is SignatureVariable sigVar)
            {
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
            else if (type is MetadataType metadataType)
            {
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

        private sealed class EmbeddedSignatureDataEmitter
        {
            private EmbeddedSignatureData[] _embeddedData;
            private int _embeddedDataIndex;
            private Stack<int> _indexStack = new Stack<int>();
            private TypeSystemMetadataEmitter _metadataEmitter;

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

            public void EmitArrayShapeAtCurrentIndexStack(BlobBuilder signatureBuilder, int rank)
            {
                var shapeEncoder = new ArrayShapeEncoder(signatureBuilder);

                bool emittedWithShape = false;

                if (!Complete)
                {
                    if (_embeddedDataIndex < _embeddedData.Length)
                    {
                        if (_embeddedData[_embeddedDataIndex].kind == EmbeddedSignatureDataKind.ArrayShape)
                        {
                            string indexData = string.Join(".", _indexStack);

                            var arrayShapePossibility = _embeddedData[_embeddedDataIndex].index.Split('|');
                            if (arrayShapePossibility[0] == indexData)
                            {
                                string[] boundsStr = arrayShapePossibility[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                                string[] loBoundsStr = arrayShapePossibility[2].Split(',', StringSplitOptions.RemoveEmptyEntries);
                                int[] bounds = new int[boundsStr.Length];
                                int[] loBounds = new int[loBoundsStr.Length];

                                for (int i = 0; i < boundsStr.Length; i++)
                                {
                                    bounds[i] = int.Parse(boundsStr[i]);
                                }
                                for (int i = 0; i < loBoundsStr.Length; i++)
                                {
                                    loBounds[i] = int.Parse(loBoundsStr[i]);
                                }

                                shapeEncoder.Shape(rank, ImmutableArray.Create(bounds), ImmutableArray.Create(loBounds));
                                _embeddedDataIndex++;
                                return;
                            }
                        }
                    }
                }

                if (!emittedWithShape)
                {
                    shapeEncoder.Shape(rank, ImmutableArray<int>.Empty, GetZeroedImmutableArrayOfSize(rank));
                }
            }

            private static ImmutableArray<int>[] ImmutableArraysFilledWithZeroes = CreateStaticArrayOfImmutableArraysFilledWithZeroes(33); // The max rank of an array is 32

            private static ImmutableArray<int> GetZeroedImmutableArrayOfSize(int rank)
            {
                if (rank < ImmutableArraysFilledWithZeroes.Length)
                    return ImmutableArraysFilledWithZeroes[rank];

                return new int[rank].ToImmutableArray();
            }
            private static ImmutableArray<int>[] CreateStaticArrayOfImmutableArraysFilledWithZeroes(int count)
            {
                ImmutableArray<int>[] result = new ImmutableArray<int>[count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = new int[i].ToImmutableArray();
                }
                return result;
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

                                case EmbeddedSignatureDataKind.ArrayShape:
                                    return;

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

        private void EncodeMethodSignature(BlobBuilder signatureBuilder, MethodSignature sig, EmbeddedSignatureDataEmitter signatureDataEmitter)
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
            EncodeType(signatureBuilder, sig.ReturnType, signatureDataEmitter);
            for (int i = 0; i < sig.Length; i++)
                EncodeType(signatureBuilder, sig[i], signatureDataEmitter);

            signatureDataEmitter.Pop();
        }

        private void EncodeFieldSignature(BlobBuilder signatureBuilder, TypeDesc fieldType, EmbeddedSignatureDataEmitter signatureDataEmitter)
        {
            signatureDataEmitter.Push();
            BlobEncoder signatureEncoder = new BlobEncoder(signatureBuilder);
            signatureEncoder.FieldSignature();
            EncodeType(signatureBuilder, fieldType, signatureDataEmitter);
            signatureDataEmitter.Pop();
        }

        public UserStringHandle GetUserStringHandle(string userString)
        {
            return _metadataBuilder.GetOrAddUserString(userString);
        }
    }
}
