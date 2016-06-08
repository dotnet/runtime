// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335
{
    // TODO: debug metadata blobs
    // TODO: revisit ctors (public vs internal vs static factories)?

    public struct BlobEncoder
    {
        public BlobBuilder Builder { get; }

        public BlobEncoder(BlobBuilder builder)
        {
            if (builder == null)
            {
                Throw.BuilderArgumentNull();
            }

            Builder = builder;
        }

        public SignatureTypeEncoder FieldSignature()
        {
            Builder.WriteByte((byte)SignatureKind.Field);
            return new SignatureTypeEncoder(Builder);
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="genericArgumentCount"/> is not in range [0, 0xffff].</exception>
        public GenericTypeArgumentsEncoder MethodSpecificationSignature(int genericArgumentCount)
        {
            if (unchecked((uint)genericArgumentCount) > ushort.MaxValue) 
            {
                Throw.ArgumentOutOfRange(nameof(genericArgumentCount));
            }

            Builder.WriteByte((byte)SignatureKind.MethodSpecification);
            Builder.WriteCompressedInteger(genericArgumentCount);

            return new GenericTypeArgumentsEncoder(Builder);
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="genericParameterCount"/> is not in range [0, 0xffff].</exception>
        public MethodSignatureEncoder MethodSignature(
            SignatureCallingConvention convention = SignatureCallingConvention.Default,
            int genericParameterCount = 0, 
            bool isInstanceMethod = false)
        {
            if (unchecked((uint)genericParameterCount) > ushort.MaxValue)
            {
                Throw.ArgumentOutOfRange(nameof(genericParameterCount));
            }

            var attributes = 
                (genericParameterCount != 0 ? SignatureAttributes.Generic : 0) | 
                (isInstanceMethod ? SignatureAttributes.Instance : 0);

            Builder.WriteByte(new SignatureHeader(SignatureKind.Method, convention, attributes).RawValue);

            if (genericParameterCount != 0)
            {
                Builder.WriteCompressedInteger(genericParameterCount);
            }

            return new MethodSignatureEncoder(Builder, hasVarArgs: convention == SignatureCallingConvention.VarArgs);
        }

        public MethodSignatureEncoder PropertySignature(bool isInstanceProperty = false)
        {
            Builder.WriteByte(new SignatureHeader(SignatureKind.Property, SignatureCallingConvention.Default, (isInstanceProperty ? SignatureAttributes.Instance : 0)).RawValue);
            return new MethodSignatureEncoder(Builder, hasVarArgs: false);
        }

        public void CustomAttributeSignature(out FixedArgumentsEncoder fixedArguments, out CustomAttributeNamedArgumentsEncoder namedArguments)
        {
            Builder.WriteUInt16(0x0001);

            fixedArguments = new FixedArgumentsEncoder(Builder);
            namedArguments = new CustomAttributeNamedArgumentsEncoder(Builder);
        }

        public void CustomAttributeSignature(Action<FixedArgumentsEncoder> fixedArguments, Action<CustomAttributeNamedArgumentsEncoder> namedArguments)
        {
            if (fixedArguments == null) Throw.ArgumentNull(nameof(fixedArguments));
            if (namedArguments == null) Throw.ArgumentNull(nameof(namedArguments));

            FixedArgumentsEncoder fixedArgumentsEncoder;
            CustomAttributeNamedArgumentsEncoder namedArgumentsEncoder;
            CustomAttributeSignature(out fixedArgumentsEncoder, out namedArgumentsEncoder);
            fixedArguments(fixedArgumentsEncoder);
            namedArguments(namedArgumentsEncoder);
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="variableCount"/> is not in range [0, 0x1fffffff].</exception>
        public LocalVariablesEncoder LocalVariableSignature(int variableCount)
        {
            if (unchecked((uint)variableCount) > BlobWriterImpl.MaxCompressedIntegerValue)
            {
                Throw.ArgumentOutOfRange(nameof(variableCount));
            }

            Builder.WriteByte((byte)SignatureKind.LocalVariables);
            Builder.WriteCompressedInteger(variableCount);
            return new LocalVariablesEncoder(Builder);
        }

        // TODO: TypeSpec is limited to structured types (doesn't have primitive types)
        public SignatureTypeEncoder TypeSpecificationSignature()
        {
            return new SignatureTypeEncoder(Builder);
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="attributeCount"/> is not in range [0, 0x1fffffff].</exception>
        public PermissionSetEncoder PermissionSetBlob(int attributeCount)
        {
            if (unchecked((uint)attributeCount) > BlobWriterImpl.MaxCompressedIntegerValue)
            {
                Throw.ArgumentOutOfRange(nameof(attributeCount));
            }

            Builder.WriteByte((byte)'.');
            Builder.WriteCompressedInteger(attributeCount);
            return new PermissionSetEncoder(Builder);
        }

        public NamedArgumentsEncoder PermissionSetArguments(int argumentCount)
        {
            if (unchecked((uint)argumentCount) > BlobWriterImpl.MaxCompressedIntegerValue)
            {
                Throw.ArgumentOutOfRange(nameof(argumentCount));
            }

            Builder.WriteCompressedInteger(argumentCount);
            return new NamedArgumentsEncoder(Builder);
        }
    }

    public struct MethodSignatureEncoder
    {
        public BlobBuilder Builder { get; }
        public bool HasVarArgs { get; }

        public MethodSignatureEncoder(BlobBuilder builder, bool hasVarArgs)
        {
            Builder = builder;
            HasVarArgs = hasVarArgs;
        }

        public void Parameters(int parameterCount, out ReturnTypeEncoder returnType, out ParametersEncoder parameters)
        {
            if (unchecked((uint)parameterCount) > BlobWriterImpl.MaxCompressedIntegerValue)
            {
                Throw.ArgumentOutOfRange(nameof(parameterCount));
            }

            Builder.WriteCompressedInteger(parameterCount);

            returnType = new ReturnTypeEncoder(Builder);
            parameters = new ParametersEncoder(Builder, hasVarArgs: HasVarArgs);
        }

        public void Parameters(int parameterCount, Action<ReturnTypeEncoder> returnType, Action<ParametersEncoder> parameters)
        {
            if (returnType == null) Throw.ArgumentNull(nameof(returnType));
            if (parameters == null) Throw.ArgumentNull(nameof(parameters));

            ReturnTypeEncoder returnTypeEncoder;
            ParametersEncoder parametersEncoder;
            Parameters(parameterCount, out returnTypeEncoder, out parametersEncoder);
            returnType(returnTypeEncoder);
            parameters(parametersEncoder);
        }
    }

    public struct LocalVariablesEncoder
    {
        public BlobBuilder Builder { get; }

        public LocalVariablesEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public LocalVariableTypeEncoder AddVariable()
        {
            return new LocalVariableTypeEncoder(Builder);
        }
    }

    public struct LocalVariableTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public LocalVariableTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomModifiersEncoder CustomModifiers()
        {
            return new CustomModifiersEncoder(Builder);
        }

        public SignatureTypeEncoder Type(bool isByRef = false, bool isPinned = false)
        {
            if (isPinned)
            {
                Builder.WriteByte((byte)SignatureTypeCode.Pinned);
            }

            if (isByRef)
            {
                Builder.WriteByte((byte)SignatureTypeCode.ByReference);
            }

            return new SignatureTypeEncoder(Builder);
        }

        public void TypedReference()
        {
            Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
        }
    }

    public struct ParameterTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public ParameterTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomModifiersEncoder CustomModifiers()
        {
            return new CustomModifiersEncoder(Builder);
        }

        public SignatureTypeEncoder Type(bool isByRef = false)
        {
            if (isByRef)
            {
                Builder.WriteByte((byte)SignatureTypeCode.ByReference);
            }

            return new SignatureTypeEncoder(Builder);
        }

        public void TypedReference()
        {
            Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
        }
    }

    public struct PermissionSetEncoder
    {
        public BlobBuilder Builder { get; }

        public PermissionSetEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public PermissionSetEncoder AddPermission(string typeName, ImmutableArray<byte> encodedArguments)
        {
            if (typeName == null)
            {
                Throw.ArgumentNull(nameof(typeName));
            }

            if (encodedArguments.IsDefault)
            {
                Throw.ArgumentNull(nameof(encodedArguments));
            }

            if (encodedArguments.Length > BlobWriterImpl.MaxCompressedIntegerValue)
            {
                Throw.BlobTooLarge(nameof(encodedArguments));
            }

            Builder.WriteSerializedString(typeName);
            Builder.WriteCompressedInteger(encodedArguments.Length);
            Builder.WriteBytes(encodedArguments);
            return new PermissionSetEncoder(Builder);
        }

        public PermissionSetEncoder AddPermission(string typeName, BlobBuilder encodedArguments)
        {
            if (typeName == null)
            {
                Throw.ArgumentNull(nameof(typeName));
            }

            if (encodedArguments == null)
            {
                Throw.ArgumentNull(nameof(encodedArguments));
            }

            if (encodedArguments.Count > BlobWriterImpl.MaxCompressedIntegerValue)
            {
                Throw.BlobTooLarge(nameof(encodedArguments));
            }

            Builder.WriteSerializedString(typeName);
            Builder.WriteCompressedInteger(encodedArguments.Count);
            encodedArguments.WriteContentTo(Builder);
            return new PermissionSetEncoder(Builder);
        }
    }

    public struct GenericTypeArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public GenericTypeArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public SignatureTypeEncoder AddArgument()
        {
            return new SignatureTypeEncoder(Builder);
        }
    }

    public struct FixedArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public FixedArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public LiteralEncoder AddArgument()
        {
            return new LiteralEncoder(Builder);
        }
    }

    public struct LiteralEncoder
    {
        public BlobBuilder Builder { get; }

        public LiteralEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public VectorEncoder Vector()
        {
            return new VectorEncoder(Builder);
        }

        public void TaggedVector(out CustomAttributeArrayTypeEncoder arrayType, out VectorEncoder vector)
        {
            arrayType = new CustomAttributeArrayTypeEncoder(Builder);
            vector = new VectorEncoder(Builder);
        }

        public void TaggedVector(Action<CustomAttributeArrayTypeEncoder> arrayType, Action<VectorEncoder> vector)
        {
            if (arrayType == null) Throw.ArgumentNull(nameof(arrayType));
            if (vector == null) Throw.ArgumentNull(nameof(vector));

            CustomAttributeArrayTypeEncoder arrayTypeEncoder;
            VectorEncoder vectorEncoder;
            TaggedVector(out arrayTypeEncoder, out vectorEncoder);
            arrayType(arrayTypeEncoder);
            vector(vectorEncoder);
        }

        public ScalarEncoder Scalar()
        {
            return new ScalarEncoder(Builder);
        }

        public void TaggedScalar(out CustomAttributeElementTypeEncoder type, out ScalarEncoder scalar)
        {
            type = new CustomAttributeElementTypeEncoder(Builder);
            scalar = new ScalarEncoder(Builder);
        }

        public void TaggedScalar(Action<CustomAttributeElementTypeEncoder> type, Action<ScalarEncoder> scalar)
        {
            if (type == null) Throw.ArgumentNull(nameof(type));
            if (scalar == null) Throw.ArgumentNull(nameof(scalar));

            CustomAttributeElementTypeEncoder typeEncoder;
            ScalarEncoder scalarEncoder;
            TaggedScalar(out typeEncoder, out scalarEncoder);
            type(typeEncoder);
            scalar(scalarEncoder);
        }
    }

    public struct ScalarEncoder
    {
        public BlobBuilder Builder { get; }

        public ScalarEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void NullArray()
        {
            Builder.WriteInt32(-1);
        }

        public void Constant(object value)
        {
            string str = value as string;
            if (str != null || value == null)
            {
                String(str);
            }
            else
            {
                Builder.WriteConstant(value);
            }
        }

        public void SystemType(string serializedTypeName)
        {
            String(serializedTypeName);
        }

        private void String(string value)
        {
            Builder.WriteSerializedString(value);
        }
    }

    public struct LiteralsEncoder
    {
        public BlobBuilder Builder { get; }

        public LiteralsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public LiteralEncoder AddLiteral()
        {
            return new LiteralEncoder(Builder);
        }
    }

    public struct VectorEncoder
    {
        public BlobBuilder Builder { get; }

        public VectorEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public LiteralsEncoder Count(int count)
        {
            Builder.WriteUInt32((uint)count);
            return new LiteralsEncoder(Builder);
        }
    }

    public struct NameEncoder
    {
        public BlobBuilder Builder { get; }

        public NameEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void Name(string name)
        {
            Builder.WriteSerializedString(name);
        }
    }

    public struct CustomAttributeNamedArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomAttributeNamedArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public NamedArgumentsEncoder Count(int count)
        {
            if (unchecked((ushort)count) > ushort.MaxValue)
            {
                Throw.ArgumentOutOfRange(nameof(count));
            }
            
            Builder.WriteUInt16((ushort)count);
            return new NamedArgumentsEncoder(Builder);
        }
    }

    public struct NamedArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public NamedArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void AddArgument(bool isField, out NamedArgumentTypeEncoder type, out NameEncoder name, out LiteralEncoder literal)
        {
            Builder.WriteByte(isField ? (byte)CustomAttributeNamedArgumentKind.Field : (byte)CustomAttributeNamedArgumentKind.Property);
            type = new NamedArgumentTypeEncoder(Builder);
            name = new NameEncoder(Builder);
            literal = new LiteralEncoder(Builder);
        }

        public void AddArgument(bool isField, Action<NamedArgumentTypeEncoder> type, Action<NameEncoder> name, Action<LiteralEncoder> literal)
        {
            if (type == null) Throw.ArgumentNull(nameof(type));
            if (name == null) Throw.ArgumentNull(nameof(name));
            if (literal == null) Throw.ArgumentNull(nameof(literal));

            NamedArgumentTypeEncoder typeEncoder;
            NameEncoder nameEncoder;
            LiteralEncoder literalEncoder;
            AddArgument(isField, out typeEncoder, out nameEncoder, out literalEncoder);
            type(typeEncoder);
            name(nameEncoder);
            literal(literalEncoder);
        }
    }

    public struct NamedArgumentTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public NamedArgumentTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomAttributeElementTypeEncoder ScalarType()
        {
            return new CustomAttributeElementTypeEncoder(Builder);
        }

        public void Object()
        {
            Builder.WriteByte((byte)SerializationTypeCode.TaggedObject);
        }

        public CustomAttributeArrayTypeEncoder SZArray()
        {
            return new CustomAttributeArrayTypeEncoder(Builder);
        }
    }

    public struct CustomAttributeArrayTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomAttributeArrayTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void ObjectArray()
        {
            Builder.WriteByte((byte)SerializationTypeCode.SZArray);
            Builder.WriteByte((byte)SerializationTypeCode.TaggedObject);
        }

        public CustomAttributeElementTypeEncoder ElementType()
        {
            Builder.WriteByte((byte)SerializationTypeCode.SZArray);
            return new CustomAttributeElementTypeEncoder(Builder);
        }
    }

    public struct CustomAttributeElementTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomAttributeElementTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        private void WriteTypeCode(SerializationTypeCode value)
        {
            Builder.WriteByte((byte)value);
        }

        public void Boolean() => WriteTypeCode(SerializationTypeCode.Boolean);
        public void Char() => WriteTypeCode(SerializationTypeCode.Char);
        public void SByte() => WriteTypeCode(SerializationTypeCode.SByte);
        public void Byte() => WriteTypeCode(SerializationTypeCode.Byte);
        public void Int16() => WriteTypeCode(SerializationTypeCode.Int16);
        public void UInt16() => WriteTypeCode(SerializationTypeCode.UInt16);
        public void Int32() => WriteTypeCode(SerializationTypeCode.Int32);
        public void UInt32() => WriteTypeCode(SerializationTypeCode.UInt32);
        public void Int64() => WriteTypeCode(SerializationTypeCode.Int64);
        public void UInt64() => WriteTypeCode(SerializationTypeCode.UInt64);
        public void Single() => WriteTypeCode(SerializationTypeCode.Single);
        public void Double() => WriteTypeCode(SerializationTypeCode.Double);
        public void String() => WriteTypeCode(SerializationTypeCode.String);

        public void PrimitiveType(PrimitiveSerializationTypeCode type)
        {
            switch (type)
            {
                case PrimitiveSerializationTypeCode.Boolean:
                case PrimitiveSerializationTypeCode.Byte:
                case PrimitiveSerializationTypeCode.SByte:
                case PrimitiveSerializationTypeCode.Char:
                case PrimitiveSerializationTypeCode.Int16:
                case PrimitiveSerializationTypeCode.UInt16:
                case PrimitiveSerializationTypeCode.Int32:
                case PrimitiveSerializationTypeCode.UInt32:
                case PrimitiveSerializationTypeCode.Int64:
                case PrimitiveSerializationTypeCode.UInt64:
                case PrimitiveSerializationTypeCode.Single:
                case PrimitiveSerializationTypeCode.Double:
                case PrimitiveSerializationTypeCode.String:
                    WriteTypeCode((SerializationTypeCode)type);
                    return;

                default:
                    Throw.ArgumentOutOfRange(nameof(type));
                    return;
            }
        }

        public void SystemType()
        {
            WriteTypeCode(SerializationTypeCode.Type);
        }

        public void Enum(string enumTypeName)
        {
            WriteTypeCode(SerializationTypeCode.Enum);
            Builder.WriteSerializedString(enumTypeName);
        }
    }

    public enum FunctionPointerAttributes
    {
        None = SignatureAttributes.None,
        HasThis = SignatureAttributes.Instance,
        HasExplicitThis = SignatureAttributes.Instance | SignatureAttributes.ExplicitThis
    }

    public struct SignatureTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public SignatureTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        private void WriteTypeCode(SignatureTypeCode value)
        {
            Builder.WriteByte((byte)value);
        }

        private void ClassOrValue(bool isValueType)
        {
            Builder.WriteByte(isValueType ? (byte)SignatureTypeKind.ValueType : (byte)SignatureTypeKind.Class);
        }

        public void Boolean() => WriteTypeCode(SignatureTypeCode.Boolean);
        public void Char() => WriteTypeCode(SignatureTypeCode.Char);
        public void SByte() => WriteTypeCode(SignatureTypeCode.SByte);
        public void Byte() => WriteTypeCode(SignatureTypeCode.Byte);
        public void Int16() => WriteTypeCode(SignatureTypeCode.Int16);
        public void UInt16() => WriteTypeCode(SignatureTypeCode.UInt16);
        public void Int32() => WriteTypeCode(SignatureTypeCode.Int32);
        public void UInt32() => WriteTypeCode(SignatureTypeCode.UInt32);
        public void Int64() => WriteTypeCode(SignatureTypeCode.Int64);
        public void UInt64() => WriteTypeCode(SignatureTypeCode.UInt64);
        public void Single() => WriteTypeCode(SignatureTypeCode.Single);
        public void Double() => WriteTypeCode(SignatureTypeCode.Double);
        public void String() => WriteTypeCode(SignatureTypeCode.String);
        public void IntPtr() => WriteTypeCode(SignatureTypeCode.IntPtr);
        public void UIntPtr() => WriteTypeCode(SignatureTypeCode.UIntPtr);
        public void Object() => WriteTypeCode(SignatureTypeCode.Object);

        /// <summary>
        /// Writes primitive type code.
        /// </summary>
        /// <param name="type">Any primitive type code except for <see cref="PrimitiveTypeCode.TypedReference"/> and <see cref="PrimitiveTypeCode.Void"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not valid in this context.</exception>
        public void PrimitiveType(PrimitiveTypeCode type)
        {
            switch (type)
            {
                case PrimitiveTypeCode.Boolean:
                case PrimitiveTypeCode.Byte:
                case PrimitiveTypeCode.SByte:
                case PrimitiveTypeCode.Char:
                case PrimitiveTypeCode.Int16:
                case PrimitiveTypeCode.UInt16:
                case PrimitiveTypeCode.Int32:
                case PrimitiveTypeCode.UInt32:
                case PrimitiveTypeCode.Int64:
                case PrimitiveTypeCode.UInt64:
                case PrimitiveTypeCode.Single:
                case PrimitiveTypeCode.Double:
                case PrimitiveTypeCode.IntPtr:
                case PrimitiveTypeCode.UIntPtr:
                case PrimitiveTypeCode.String:
                case PrimitiveTypeCode.Object:
                    Builder.WriteByte((byte)type);
                    return;

                case PrimitiveTypeCode.TypedReference:
                case PrimitiveTypeCode.Void:
                default:
                    Throw.ArgumentOutOfRange(nameof(type));
                    return;
            }
        }

        public void Array(out SignatureTypeEncoder elementType, out ArrayShapeEncoder arrayShape)
        {
            Builder.WriteByte((byte)SignatureTypeCode.Array);
            elementType = this;
            arrayShape = new ArrayShapeEncoder(Builder);
        }

        public void Array(Action<SignatureTypeEncoder> elementType, Action<ArrayShapeEncoder> arrayShape)
        {
            if (elementType == null) Throw.ArgumentNull(nameof(elementType));
            if (arrayShape == null) Throw.ArgumentNull(nameof(arrayShape));

            SignatureTypeEncoder elementTypeEncoder;
            ArrayShapeEncoder arrayShapeEncoder;
            Array(out elementTypeEncoder, out arrayShapeEncoder);
            elementType(elementTypeEncoder);
            arrayShape(arrayShapeEncoder);
        }

        /// <summary>
        /// Encodes a reference to a type.
        /// </summary>
        /// <param name="type"><see cref="TypeDefinitionHandle"/> or <see cref="TypeReferenceHandle"/>.</param>
        /// <param name="isValueType">True to mark the type as value type, false to mark it as a reference type in the signature.</param>
        /// <exception cref="ArgumentException"><paramref name="type"/> doesn't have the expected handle kind.</exception>
        public void Type(EntityHandle type, bool isValueType)
        {
            // Get the coded index before we start writing anything (might throw argument exception):
            // Note: We don't allow TypeSpec as per https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/Ecma-335-Issues.md#proposed-specification-change
            int codedIndex = CodedIndex.TypeDefOrRef(type);

            ClassOrValue(isValueType);
            Builder.WriteCompressedInteger(codedIndex);
        }

        /// <summary>
        /// Starts a function pointer signature.
        /// </summary>
        /// <param name="convention">Calling convention.</param>
        /// <param name="attributes">Function pointer attributes.</param>
        /// <param name="genericParameterCount">Generic parameter count.</param>
        /// <exception cref="ArgumentException"><paramref name="attributes"/> is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="genericParameterCount"/> is not in range [0, 0xffff].</exception>
        public MethodSignatureEncoder FunctionPointer(
            SignatureCallingConvention convention = SignatureCallingConvention.Default, 
            FunctionPointerAttributes attributes = FunctionPointerAttributes.None, 
            int genericParameterCount = 0)
        {
            // Spec:
            // The EXPLICITTHIS (0x40) bit can be set only in signatures for function pointers.
            // If EXPLICITTHIS (0x40) in the signature is set, then HASTHIS (0x20) shall also be set.

            if (attributes != FunctionPointerAttributes.None &&
                attributes != FunctionPointerAttributes.HasThis &&
                attributes != FunctionPointerAttributes.HasExplicitThis)
            {
                throw new ArgumentException(SR.InvalidSignature, nameof(attributes));
            }

            if (unchecked((uint)genericParameterCount) > ushort.MaxValue)
            {
                Throw.ArgumentOutOfRange(nameof(genericParameterCount));
            }

            Builder.WriteByte((byte)SignatureTypeCode.FunctionPointer);
            Builder.WriteByte(new SignatureHeader(SignatureKind.Method, convention, (SignatureAttributes)attributes).RawValue);

            if (genericParameterCount != 0)
            {
                Builder.WriteCompressedInteger(genericParameterCount);
            }

            return new MethodSignatureEncoder(Builder, hasVarArgs: convention == SignatureCallingConvention.VarArgs);
        }

        /// <summary>
        /// Starts a generic instantiation signature.
        /// </summary>
        /// <param name="genericType"><see cref="TypeDefinitionHandle"/> or <see cref="TypeReferenceHandle"/>.</param>
        /// <param name="genericArgumentCount">Generic argument count.</param>
        /// <param name="isValueType">True to mark the type as value type, false to mark it as a reference type in the signature.</param>
        /// <exception cref="ArgumentException"><paramref name="genericType"/> doesn't have the expected handle kind.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="genericArgumentCount"/> is not in range [1, 0xffff].</exception>
        public GenericTypeArgumentsEncoder GenericInstantiation(EntityHandle genericType, int genericArgumentCount, bool isValueType)
        {
            if (unchecked((uint)(genericArgumentCount - 1)) > ushort.MaxValue - 1)
            {
                Throw.ArgumentOutOfRange(nameof(genericArgumentCount));
            }

            // Get the coded index before we start writing anything (might throw argument exception):
            // Note: We don't allow TypeSpec as per https://github.com/dotnet/corefx/blob/master/src/System.Reflection.Metadata/specs/Ecma-335-Issues.md#proposed-specification-change
            int codedIndex = CodedIndex.TypeDefOrRef(genericType);

            Builder.WriteByte((byte)SignatureTypeCode.GenericTypeInstance);
            ClassOrValue(isValueType);
            Builder.WriteCompressedInteger(codedIndex);
            Builder.WriteCompressedInteger(genericArgumentCount);
            return new GenericTypeArgumentsEncoder(Builder);
        }

        /// <summary>
        /// Encodes a reference to type parameter of a containing generic method.
        /// </summary>
        /// <param name="parameterIndex">Parameter index.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameterIndex"/> is not in range [0, 0xffff].</exception>
        public void GenericMethodTypeParameter(int parameterIndex)
        {
            if (unchecked((uint)parameterIndex) > ushort.MaxValue)
            {
                Throw.ArgumentOutOfRange(nameof(parameterIndex));
            }

            Builder.WriteByte((byte)SignatureTypeCode.GenericMethodParameter);
            Builder.WriteCompressedInteger(parameterIndex);
        }

        /// <summary>
        /// Encodes a reference to type parameter of a containing generic type.
        /// </summary>
        /// <param name="parameterIndex">Parameter index.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameterIndex"/> is not in range [0, 0xffff].</exception>
        public void GenericTypeParameter(int parameterIndex)
        {
            if (unchecked((uint)parameterIndex) > ushort.MaxValue)
            {
                Throw.ArgumentOutOfRange(nameof(parameterIndex));
            }

            Builder.WriteByte((byte)SignatureTypeCode.GenericTypeParameter);
            Builder.WriteCompressedInteger(parameterIndex);
        }

        /// <summary>
        /// Starts pointer signature.
        /// </summary>
        public SignatureTypeEncoder Pointer()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Pointer);
            return this;
        }

        /// <summary>
        /// Encodes <code>void*</code>.
        /// </summary>
        public void VoidPointer()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Pointer);
            Builder.WriteByte((byte)SignatureTypeCode.Void);
        }

        /// <summary>
        /// Starts SZ array (vector) signature.
        /// </summary>
        public SignatureTypeEncoder SZArray()
        {
            Builder.WriteByte((byte)SignatureTypeCode.SZArray);
            return this;
        }

        /// <summary>
        /// Starts a signature of a type with custom modifiers.
        /// </summary>
        public CustomModifiersEncoder CustomModifiers()
        {
            return new CustomModifiersEncoder(Builder);
        }
    }

    public struct CustomModifiersEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomModifiersEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomModifiersEncoder AddModifier(bool isOptional, EntityHandle typeDefRefSpec)
        {
            if (isOptional)
            {
                Builder.WriteByte((byte)SignatureTypeCode.OptionalModifier);
            }
            else
            {
                Builder.WriteByte((byte)SignatureTypeCode.RequiredModifier);
            }

            Builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(typeDefRefSpec));
            return this;
        }
    }

    public struct ArrayShapeEncoder
    {
        public BlobBuilder Builder { get; }

        public ArrayShapeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void Shape(int rank, ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
        {
            Builder.WriteCompressedInteger(rank);
            Builder.WriteCompressedInteger(sizes.Length);
            foreach (int size in sizes)
            {
                Builder.WriteCompressedInteger(size);
            }

            if (lowerBounds.IsDefault)
            {
                Builder.WriteCompressedInteger(rank);
                for (int i = 0; i < rank; i++)
                {
                    Builder.WriteCompressedSignedInteger(0);
                }
            }
            else
            {
                Builder.WriteCompressedInteger(lowerBounds.Length);
                foreach (int lowerBound in lowerBounds)
                {
                    Builder.WriteCompressedSignedInteger(lowerBound);
                }
            }
        }
    }

    public struct ReturnTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public ReturnTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomModifiersEncoder CustomModifiers()
        {
            return new CustomModifiersEncoder(Builder);
        }

        public SignatureTypeEncoder Type(bool isByRef = false)
        {
            if (isByRef)
            {
                Builder.WriteByte((byte)SignatureTypeCode.ByReference);
            }

            return new SignatureTypeEncoder(Builder);
        }

        public void TypedReference()
        {
            Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
        }

        public void Void()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Void);
        }
    }

    public struct ParametersEncoder
    {
        public BlobBuilder Builder { get; }
        public bool HasVarArgs { get; }

        public ParametersEncoder(BlobBuilder builder, bool hasVarArgs)
        {
            Builder = builder;
            HasVarArgs = hasVarArgs;
        }

        public ParameterTypeEncoder AddParameter()
        {
            return new ParameterTypeEncoder(Builder);
        }

        public ParametersEncoder StartVarArgs()
        {
            if (!HasVarArgs)
            {
                Throw.SignatureNotVarArg();
            }

            Builder.WriteByte((byte)SignatureTypeCode.Sentinel);
            return new ParametersEncoder(Builder, hasVarArgs: false);
        }
    }
}
