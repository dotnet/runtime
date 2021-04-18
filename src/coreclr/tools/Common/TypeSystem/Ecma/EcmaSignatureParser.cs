// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Internal.TypeSystem;
using System.Collections.Generic;

namespace Internal.TypeSystem.Ecma
{
    public struct EcmaSignatureParser
    {
        private TypeSystemContext _tsc;
        private Func<EntityHandle, NotFoundBehavior, TypeDesc> _typeResolver;
        private NotFoundBehavior _notFoundBehavior;
        private EcmaModule _ecmaModule;
        private BlobReader _reader;
        private ResolutionFailure _resolutionFailure;

        private Stack<int> _indexStack;
        private List<EmbeddedSignatureData> _embeddedSignatureDataList;


        public EcmaSignatureParser(TypeSystemContext tsc, Func<EntityHandle, NotFoundBehavior, TypeDesc> typeResolver, BlobReader reader, NotFoundBehavior notFoundBehavior)
        {
            _notFoundBehavior = notFoundBehavior;
            _ecmaModule = null;
            _tsc = tsc;
            _typeResolver = typeResolver;
            _reader = reader;
            _indexStack = null;
            _embeddedSignatureDataList = null;
            _resolutionFailure = null;
        }

        public EcmaSignatureParser(EcmaModule ecmaModule, BlobReader reader, NotFoundBehavior notFoundBehavior)
        {
            _notFoundBehavior = notFoundBehavior;
            _ecmaModule = ecmaModule;
            _tsc = ecmaModule.Context;
            _typeResolver = null;
            _reader = reader;
            _indexStack = null;
            _embeddedSignatureDataList = null;
            _resolutionFailure = null;
        }

        void SetResolutionFailure(ResolutionFailure failure)
        {
            if (_resolutionFailure == null)
                _resolutionFailure = failure;
        }

        public ResolutionFailure ResolutionFailure => _resolutionFailure;

        private TypeDesc ResolveHandle(EntityHandle handle)
        {
            object resolvedValue;
            if (_ecmaModule != null)
            {
                resolvedValue = _ecmaModule.GetObject(handle, _notFoundBehavior);
            }
            else
            {
                resolvedValue = _typeResolver(handle, _notFoundBehavior);
            }

            if (resolvedValue == null)
                return null;
            if (resolvedValue is ResolutionFailure failure)
            {
                SetResolutionFailure(failure);
                return null;
            }
            if (resolvedValue is TypeDesc type)
            {
                return type;
            }
            else
            {
                throw new BadImageFormatException("Type expected");
            }
        }

        private TypeDesc GetWellKnownType(WellKnownType wellKnownType)
        {
            return _tsc.GetWellKnownType(wellKnownType);
        }

        private TypeDesc ParseType(SignatureTypeCode typeCode)
        {
            if (_indexStack != null)
            {
                int was = _indexStack.Pop();
                _indexStack.Push(was + 1);
                _indexStack.Push(0);
            }
            TypeDesc result = ParseTypeImpl(typeCode);
            if (_indexStack != null)
            {
                _indexStack.Pop();
            }
            return result;
        }

        private TypeDesc ParseTypeImpl(SignatureTypeCode typeCode)
        {
            // Switch on the type.
            switch (typeCode)
            {
                case SignatureTypeCode.Void:
                    return GetWellKnownType(WellKnownType.Void);
                case SignatureTypeCode.Boolean:
                    return GetWellKnownType(WellKnownType.Boolean);
                case SignatureTypeCode.SByte:
                    return GetWellKnownType(WellKnownType.SByte);
                case SignatureTypeCode.Byte:
                    return GetWellKnownType(WellKnownType.Byte);
                case SignatureTypeCode.Int16:
                    return GetWellKnownType(WellKnownType.Int16);
                case SignatureTypeCode.UInt16:
                    return GetWellKnownType(WellKnownType.UInt16);
                case SignatureTypeCode.Int32:
                    return GetWellKnownType(WellKnownType.Int32);
                case SignatureTypeCode.UInt32:
                    return GetWellKnownType(WellKnownType.UInt32);
                case SignatureTypeCode.Int64:
                    return GetWellKnownType(WellKnownType.Int64);
                case SignatureTypeCode.UInt64:
                    return GetWellKnownType(WellKnownType.UInt64);
                case SignatureTypeCode.Single:
                    return GetWellKnownType(WellKnownType.Single);
                case SignatureTypeCode.Double:
                    return GetWellKnownType(WellKnownType.Double);
                case SignatureTypeCode.Char:
                    return GetWellKnownType(WellKnownType.Char);
                case SignatureTypeCode.String:
                    return GetWellKnownType(WellKnownType.String);
                case SignatureTypeCode.IntPtr:
                    return GetWellKnownType(WellKnownType.IntPtr);
                case SignatureTypeCode.UIntPtr:
                    return GetWellKnownType(WellKnownType.UIntPtr);
                case SignatureTypeCode.Object:
                    return GetWellKnownType(WellKnownType.Object);
                case SignatureTypeCode.TypeHandle:
                    return ResolveHandle(_reader.ReadTypeHandle());
                case SignatureTypeCode.SZArray:
                    {
                        var elementType = ParseType();
                        if (elementType == null)
                            return null;
                        return _tsc.GetArrayType(elementType);
                    }
                case SignatureTypeCode.Array:
                    {
                        var elementType = ParseType();
                        var rank = _reader.ReadCompressedInteger();

                        // TODO: Bounds for multi-dimmensional arrays
                        var boundsCount = _reader.ReadCompressedInteger();
                        for (int i = 0; i < boundsCount; i++)
                            _reader.ReadCompressedInteger();
                        var lowerBoundsCount = _reader.ReadCompressedInteger();
                        for (int j = 0; j < lowerBoundsCount; j++)
                            _reader.ReadCompressedInteger();

                        if (elementType != null)
                            return _tsc.GetArrayType(elementType, rank);
                        else
                            return null;
                    }
                case SignatureTypeCode.ByReference:
                    {
                        TypeDesc byRefedType = ParseType();
                        if (byRefedType != null)
                            return byRefedType.MakeByRefType();
                        else
                            return null;
                    }
                case SignatureTypeCode.Pointer:
                    {
                        TypeDesc pointedAtType = ParseType();
                        if (pointedAtType != null)
                            return _tsc.GetPointerType(pointedAtType);
                        else
                            return null;
                    }
                case SignatureTypeCode.GenericTypeParameter:
                    return _tsc.GetSignatureVariable(_reader.ReadCompressedInteger(), false);
                case SignatureTypeCode.GenericMethodParameter:
                    return _tsc.GetSignatureVariable(_reader.ReadCompressedInteger(), true);
                case SignatureTypeCode.GenericTypeInstance:
                    {
                        TypeDesc typeDef = ParseType();
                        MetadataType metadataTypeDef = null;

                        if (typeDef != null)
                        {
                            metadataTypeDef = typeDef as MetadataType;
                            if (metadataTypeDef == null)
                                throw new BadImageFormatException();
                        }

                        TypeDesc[] instance = new TypeDesc[_reader.ReadCompressedInteger()];
                        for (int i = 0; i < instance.Length; i++)
                        {
                            instance[i] = ParseType();
                            if (instance[i] == null)
                                metadataTypeDef = null;
                        }

                        if (metadataTypeDef != null)
                            return _tsc.GetInstantiatedType(metadataTypeDef, new Instantiation(instance));
                        else
                            return null;
                    }
                case SignatureTypeCode.TypedReference:
                    return GetWellKnownType(WellKnownType.TypedReference);
                case SignatureTypeCode.FunctionPointer:
                    MethodSignature sig = ParseMethodSignatureInternal(skipEmbeddedSignatureData: true);
                    if (sig != null)
                        return _tsc.GetFunctionPointerType(sig);
                    else
                        return null;
                default:
                    throw new BadImageFormatException();
            }
        }

        private SignatureTypeCode ParseTypeCode(bool skipPinned = true)
        {
            if (_indexStack != null)
            {
                int was = _indexStack.Pop();
                _indexStack.Push(was + 1);
                _indexStack.Push(0);
            }
            SignatureTypeCode result = ParseTypeCodeImpl(skipPinned);
            if (_indexStack != null)
            {
                _indexStack.Pop();
            }
            return result;
        }

        private SignatureTypeCode ParseTypeCodeImpl(bool skipPinned = true)
        {
            for (; ; )
            {
                SignatureTypeCode typeCode = _reader.ReadSignatureTypeCode();

                if (typeCode == SignatureTypeCode.RequiredModifier)
                {
                    EntityHandle typeHandle = _reader.ReadTypeHandle();
                    if (_embeddedSignatureDataList != null)
                    {
                        _embeddedSignatureDataList.Add(new EmbeddedSignatureData { index = string.Join(".", _indexStack), kind = EmbeddedSignatureDataKind.RequiredCustomModifier, type = ResolveHandle(typeHandle) });
                    }
                    continue;
                }

                if (typeCode == SignatureTypeCode.OptionalModifier)
                {
                    EntityHandle typeHandle = _reader.ReadTypeHandle();
                    if (_embeddedSignatureDataList != null)
                    {
                        _embeddedSignatureDataList.Add(new EmbeddedSignatureData { index = string.Join(".", _indexStack), kind = EmbeddedSignatureDataKind.OptionalCustomModifier, type = ResolveHandle(typeHandle) });
                    }
                    continue;
                }

                // TODO: treat PINNED in the signature same as modopts (it matters
                // in signature matching - you can actually define overloads on this)
                if (skipPinned && typeCode == SignatureTypeCode.Pinned)
                {
                    continue;
                }

                return typeCode;
            }
        }

        public TypeDesc ParseType()
        {
            if (_indexStack != null)
            {
                int was = _indexStack.Pop();
                _indexStack.Push(was + 1);
                _indexStack.Push(0);
            }
            TypeDesc result = ParseTypeImpl();
            if (_indexStack != null)
            {
                _indexStack.Pop();
            }
            return result;
        }

        private TypeDesc ParseTypeImpl()
        {
            return ParseType(ParseTypeCode());
        }

        public bool IsFieldSignature
        {
            get
            {
                BlobReader peek = _reader;
                return peek.ReadSignatureHeader().Kind == SignatureKind.Field;
            }
        }

        public MethodSignature ParseMethodSignature()
        {
            try
            {
                _indexStack = new Stack<int>();
                _indexStack.Push(0);
                _embeddedSignatureDataList = new List<EmbeddedSignatureData>();
                return ParseMethodSignatureInternal(skipEmbeddedSignatureData: false);
            }
            finally
            {
                _indexStack = null;
                _embeddedSignatureDataList = null;
            }

        }

        private MethodSignature ParseMethodSignatureInternal(bool skipEmbeddedSignatureData)
        {
            if (_indexStack != null)
            {
                int was = _indexStack.Pop();
                _indexStack.Push(was + 1);
                _indexStack.Push(0);
            }
            MethodSignature result = ParseMethodSignatureImpl(skipEmbeddedSignatureData);
            if (_indexStack != null)
            {
                _indexStack.Pop();
            }
            return result;
        }

        private MethodSignature ParseMethodSignatureImpl(bool skipEmbeddedSignatureData)
        {
            SignatureHeader header = _reader.ReadSignatureHeader();

            MethodSignatureFlags flags = 0;

            SignatureCallingConvention signatureCallConv = header.CallingConvention;
            if (signatureCallConv != SignatureCallingConvention.Default)
            {
                // Verify that it is safe to convert CallingConvention to MethodSignatureFlags via a simple cast
                Debug.Assert((int)MethodSignatureFlags.UnmanagedCallingConventionCdecl == (int)SignatureCallingConvention.CDecl);
                Debug.Assert((int)MethodSignatureFlags.UnmanagedCallingConventionStdCall == (int)SignatureCallingConvention.StdCall);
                Debug.Assert((int)MethodSignatureFlags.UnmanagedCallingConventionThisCall == (int)SignatureCallingConvention.ThisCall);
                Debug.Assert((int)MethodSignatureFlags.CallingConventionVarargs == (int)SignatureCallingConvention.VarArgs);
                Debug.Assert((int)MethodSignatureFlags.UnmanagedCallingConvention == (int)SignatureCallingConvention.Unmanaged);

                flags = (MethodSignatureFlags)signatureCallConv;
            }

            if (!header.IsInstance)
                flags |= MethodSignatureFlags.Static;

            int arity = header.IsGeneric ? _reader.ReadCompressedInteger() : 0;

            int count = _reader.ReadCompressedInteger();

            TypeDesc returnType = ParseType();
            TypeDesc[] parameters;

            if (count > 0)
            {
                // Get all of the parameters.
                parameters = new TypeDesc[count];
                for (int i = 0; i < count; i++)
                {
                    parameters[i] = ParseType();
                }
            }
            else
            {
                parameters = TypeDesc.EmptyTypes;
            }

            EmbeddedSignatureData[] embeddedSignatureDataArray = (_embeddedSignatureDataList == null || _embeddedSignatureDataList.Count == 0 || skipEmbeddedSignatureData) ? null : _embeddedSignatureDataList.ToArray();

            if (_resolutionFailure == null)
                return new MethodSignature(flags, arity, returnType, parameters, embeddedSignatureDataArray);
            else
                return null;

        }

        public PropertySignature ParsePropertySignature()
        {
            // As PropertySignature is a struct, we cannot return null
            if (_notFoundBehavior != NotFoundBehavior.Throw)
                throw new ArgumentException();

            SignatureHeader header = _reader.ReadSignatureHeader();
            if (header.Kind != SignatureKind.Property)
                throw new BadImageFormatException();

            bool isStatic = !header.IsInstance;

            int count = _reader.ReadCompressedInteger();

            TypeDesc returnType = ParseType();
            TypeDesc[] parameters;

            if (count > 0)
            {
                // Get all of the parameters.
                parameters = new TypeDesc[count];
                for (int i = 0; i < count; i++)
                {
                    parameters[i] = ParseType();
                }
            }
            else
            {
                parameters = TypeDesc.EmptyTypes;
            }

            return new PropertySignature(isStatic, parameters, returnType);
        }

        public TypeDesc ParseFieldSignature()
        {
            if (_reader.ReadSignatureHeader().Kind != SignatureKind.Field)
                throw new BadImageFormatException();

            return ParseType();
        }

        public LocalVariableDefinition[] ParseLocalsSignature()
        {
            if (_reader.ReadSignatureHeader().Kind != SignatureKind.LocalVariables)
                throw new BadImageFormatException();

            int count = _reader.ReadCompressedInteger();

            LocalVariableDefinition[] locals;

            if (count > 0)
            {
                locals = new LocalVariableDefinition[count];
                for (int i = 0; i < count; i++)
                {
                    bool isPinned = false;

                    SignatureTypeCode typeCode = ParseTypeCode(skipPinned: false);
                    if (typeCode == SignatureTypeCode.Pinned)
                    {
                        isPinned = true;
                        typeCode = ParseTypeCode();
                    }

                    locals[i] = new LocalVariableDefinition(ParseType(typeCode), isPinned);
                }
            }
            else
            {
                locals = Array.Empty<LocalVariableDefinition>();
            }
            if (_resolutionFailure == null)
                return locals;
            else
                return null;
        }

        public TypeDesc[] ParseMethodSpecSignature()
        {
            if (_reader.ReadSignatureHeader().Kind != SignatureKind.MethodSpecification)
                throw new BadImageFormatException();

            int count = _reader.ReadCompressedInteger();

            if (count <= 0)
                throw new BadImageFormatException();

            TypeDesc[] arguments = new TypeDesc[count];
            for (int i = 0; i < count; i++)
            {
                arguments[i] = ParseType();
            }
            if (_resolutionFailure == null)
                return arguments;
            else
                return null;
        }

        public MarshalAsDescriptor ParseMarshalAsDescriptor()
        {
            Debug.Assert(_reader.RemainingBytes != 0);

            NativeTypeKind type = (NativeTypeKind)_reader.ReadByte();
            NativeTypeKind arraySubType = NativeTypeKind.Default;
            uint? paramNum = null, numElem = null;

            switch (type)
            {
                case NativeTypeKind.Array:
                    {
                        if (_reader.RemainingBytes != 0)
                        {
                            arraySubType = (NativeTypeKind)_reader.ReadByte();
                        }

                        if (_reader.RemainingBytes != 0)
                        {
                            paramNum = (uint)_reader.ReadCompressedInteger();
                        }

                        if (_reader.RemainingBytes != 0)
                        {
                            numElem = (uint)_reader.ReadCompressedInteger();
                        }

                        if (_reader.RemainingBytes != 0)
                        {
                            int flag = _reader.ReadCompressedInteger();
                            if (flag == 0)
                            {
                                paramNum = null; //paramNum is just a place holder so that numElem can be present
                            }
                        }

                    }
                    break;
                case NativeTypeKind.ByValArray:
                    {
                        if (_reader.RemainingBytes != 0)
                        {
                            numElem = (uint)_reader.ReadCompressedInteger();
                        }

                        if (_reader.RemainingBytes != 0)
                        {
                            arraySubType = (NativeTypeKind)_reader.ReadByte();
                        }
                    }
                    break;
                case NativeTypeKind.ByValTStr:
                    {
                        if (_reader.RemainingBytes != 0)
                        {
                            numElem = (uint)_reader.ReadCompressedInteger();
                        }
                    }
                    break;
                case NativeTypeKind.SafeArray:
                    {
                        // There's nobody to consume SafeArrays, so let's just parse the data
                        // to avoid asserting later.

                        // Get optional VARTYPE for the element
                        if (_reader.RemainingBytes != 0)
                        {
                            _reader.ReadCompressedInteger();
                        }

                        // VARTYPE can be followed by optional type name
                        if (_reader.RemainingBytes != 0)
                        {
                            _reader.ReadSerializedString();
                        }
                    }
                    break;
                case NativeTypeKind.CustomMarshaler:
                    {
                        // There's nobody to consume CustomMarshaller, so let's just parse the data
                        // to avoid asserting later.

                        // Read typelib guid
                        _reader.ReadSerializedString();

                        // Read native type name
                        _reader.ReadSerializedString();

                        // Read managed marshaler name
                        _reader.ReadSerializedString();

                        // Read cookie
                        _reader.ReadSerializedString();
                    }
                    break;
                default:
                    break;
            }

            Debug.Assert(_reader.RemainingBytes == 0);

            return new MarshalAsDescriptor(type, arraySubType, paramNum, numElem);
        }
    }
}
