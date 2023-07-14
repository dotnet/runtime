// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335
{
    // TODO: debug metadata blobs
    // TODO: revisit ctors (public vs internal vs static factories)?

    public readonly struct BlobEncoder
    {
        public BlobBuilder Builder { get; }

        public BlobEncoder(BlobBuilder builder)
        {
            if (builder is null)
            {
                Throw.ArgumentNull(nameof(builder));
            }

            Builder = builder;
        }

        /// <summary>
        /// Encodes Field Signature blob, with additional support for
        /// encoding ref fields, custom modifiers and typed references.
        /// </summary>
        /// <returns>Encoder of the field type.</returns>
        public FieldTypeEncoder Field()
        {
            Builder.WriteByte((byte)SignatureKind.Field);
            return new FieldTypeEncoder(Builder);
        }

        /// <summary>
        /// Encodes Field Signature blob.
        /// </summary>
        /// <returns>Encoder of the field type.</returns>
        /// <remarks>To encode byref fields, custom modifiers or typed
        /// references use <see cref="Field"/> instead.</remarks>
        public SignatureTypeEncoder FieldSignature()
        {
            return Field().Type(isByRef: false);
        }

        /// <summary>
        /// Encodes Method Specification Signature blob.
        /// </summary>
        /// <param name="genericArgumentCount">Number of generic arguments.</param>
        /// <returns>Encoder of generic arguments.</returns>
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

        /// <summary>
        /// Encodes Method Signature blob.
        /// </summary>
        /// <param name="convention">Calling convention.</param>
        /// <param name="genericParameterCount">Number of generic parameters.</param>
        /// <param name="isInstanceMethod">True to encode an instance method signature, false to encode a static method signature.</param>
        /// <returns>An Encoder of the rest of the signature including return value and parameters.</returns>
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

        /// <summary>
        /// Encodes Property Signature blob.
        /// </summary>
        /// <param name="isInstanceProperty">True to encode an instance property signature, false to encode a static property signature.</param>
        /// <returns>An Encoder of the rest of the signature including return value and parameters, which has the same structure as Method Signature.</returns>
        public MethodSignatureEncoder PropertySignature(bool isInstanceProperty = false)
        {
            Builder.WriteByte(new SignatureHeader(SignatureKind.Property, SignatureCallingConvention.Default, (isInstanceProperty ? SignatureAttributes.Instance : 0)).RawValue);
            return new MethodSignatureEncoder(Builder, hasVarArgs: false);
        }

        /// <summary>
        /// Encodes Custom Attribute Signature blob.
        /// Returns a pair of encoders that must be used in the order they appear in the parameter list.
        /// </summary>
        /// <param name="fixedArguments">Use first, to encode fixed arguments.</param>
        /// <param name="namedArguments">Use second, to encode named arguments.</param>
        public void CustomAttributeSignature(out FixedArgumentsEncoder fixedArguments, out CustomAttributeNamedArgumentsEncoder namedArguments)
        {
            Builder.WriteUInt16(0x0001);

            fixedArguments = new FixedArgumentsEncoder(Builder);
            namedArguments = new CustomAttributeNamedArgumentsEncoder(Builder);
        }

        /// <summary>
        /// Encodes Custom Attribute Signature blob.
        /// </summary>
        /// <param name="fixedArguments">Called first, to encode fixed arguments.</param>
        /// <param name="namedArguments">Called second, to encode named arguments.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fixedArguments"/> or <paramref name="namedArguments"/> is null.</exception>
        public void CustomAttributeSignature(Action<FixedArgumentsEncoder> fixedArguments, Action<CustomAttributeNamedArgumentsEncoder> namedArguments)
        {
            if (fixedArguments is null)
            {
                Throw.ArgumentNull(nameof(fixedArguments));
            }
            if (namedArguments is null)
            {
                Throw.ArgumentNull(nameof(namedArguments));
            }

            FixedArgumentsEncoder fixedArgumentsEncoder;
            CustomAttributeNamedArgumentsEncoder namedArgumentsEncoder;
            CustomAttributeSignature(out fixedArgumentsEncoder, out namedArgumentsEncoder);
            fixedArguments(fixedArgumentsEncoder);
            namedArguments(namedArgumentsEncoder);
        }

        /// <summary>
        /// Encodes Local Variable Signature.
        /// </summary>
        /// <param name="variableCount">Number of local variables.</param>
        /// <returns>Encoder of a sequence of local variables.</returns>
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

        /// <summary>
        /// Encodes Type Specification Signature.
        /// </summary>
        /// <returns>
        /// Type encoder of the structured type represented by the Type Specification (it shall not encode a primitive type).
        /// </returns>
        public SignatureTypeEncoder TypeSpecificationSignature()
        {
            return new SignatureTypeEncoder(Builder);
        }

        /// <summary>
        /// Encodes a Permission Set blob.
        /// </summary>
        /// <param name="attributeCount">Number of attributes in the set.</param>
        /// <returns>Permission Set encoder.</returns>
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

        /// <summary>
        /// Encodes Permission Set arguments.
        /// </summary>
        /// <param name="argumentCount">Number of arguments in the set.</param>
        /// <returns>Encoder of the arguments of the set.</returns>
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

    public readonly struct MethodSignatureEncoder
    {
        public BlobBuilder Builder { get; }
        public bool HasVarArgs { get; }

        public MethodSignatureEncoder(BlobBuilder builder, bool hasVarArgs)
        {
            Builder = builder;
            HasVarArgs = hasVarArgs;
        }

        /// <summary>
        /// Encodes return type and parameters.
        /// Returns a pair of encoders that must be used in the order they appear in the parameter list.
        /// </summary>
        /// <param name="parameterCount">Number of parameters.</param>
        /// <param name="returnType">Use first, to encode the return types.</param>
        /// <param name="parameters">Use second, to encode the actual parameters.</param>
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

        /// <summary>
        /// Encodes return type and parameters.
        /// </summary>
        /// <param name="parameterCount">Number of parameters.</param>
        /// <param name="returnType">Called first, to encode the return type.</param>
        /// <param name="parameters">Called second, to encode the actual parameters.</param>
        /// <exception cref="ArgumentNullException"><paramref name="returnType"/> or <paramref name="parameters"/> is null.</exception>
        public void Parameters(int parameterCount, Action<ReturnTypeEncoder> returnType, Action<ParametersEncoder> parameters)
        {
            if (returnType is null)
            {
                Throw.ArgumentNull(nameof(returnType));
            }
            if (parameters is null)
            {
                Throw.ArgumentNull(nameof(parameters));
            }

            ReturnTypeEncoder returnTypeEncoder;
            ParametersEncoder parametersEncoder;
            Parameters(parameterCount, out returnTypeEncoder, out parametersEncoder);
            returnType(returnTypeEncoder);
            parameters(parametersEncoder);
        }
    }

    public readonly struct LocalVariablesEncoder
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

    public readonly struct LocalVariableTypeEncoder
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

    public readonly struct ParameterTypeEncoder
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

    public readonly struct PermissionSetEncoder
    {
        public BlobBuilder Builder { get; }

        public PermissionSetEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public PermissionSetEncoder AddPermission(string typeName, ImmutableArray<byte> encodedArguments)
        {
            if (typeName is null)
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
            return this;
        }

        public PermissionSetEncoder AddPermission(string typeName, BlobBuilder encodedArguments)
        {
            if (typeName is null)
            {
                Throw.ArgumentNull(nameof(typeName));
            }
            if (encodedArguments is null)
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
            return this;
        }
    }

    public readonly struct GenericTypeArgumentsEncoder
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

    public readonly struct FieldTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public FieldTypeEncoder(BlobBuilder builder)
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

    public readonly struct FixedArgumentsEncoder
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

    public readonly struct LiteralEncoder
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

        /// <summary>
        /// Encodes the type and the items of a vector literal.
        /// Returns a pair of encoders that must be used in the order they appear in the parameter list.
        /// </summary>
        /// <param name="arrayType">Use first, to encode the type of the vector.</param>
        /// <param name="vector">Use second, to encode the items of the vector.</param>
        public void TaggedVector(out CustomAttributeArrayTypeEncoder arrayType, out VectorEncoder vector)
        {
            arrayType = new CustomAttributeArrayTypeEncoder(Builder);
            vector = new VectorEncoder(Builder);
        }

        /// <summary>
        /// Encodes the type and the items of a vector literal.
        /// </summary>
        /// <param name="arrayType">Called first, to encode the type of the vector.</param>
        /// <param name="vector">Called second, to encode the items of the vector.</param>
        /// <exception cref="ArgumentNullException"><paramref name="arrayType"/> or <paramref name="vector"/> is null.</exception>
        public void TaggedVector(Action<CustomAttributeArrayTypeEncoder> arrayType, Action<VectorEncoder> vector)
        {
            if (arrayType is null)
            {
                Throw.ArgumentNull(nameof(arrayType));
            }
            if (vector is null)
            {
                Throw.ArgumentNull(nameof(vector));
            }

            CustomAttributeArrayTypeEncoder arrayTypeEncoder;
            VectorEncoder vectorEncoder;
            TaggedVector(out arrayTypeEncoder, out vectorEncoder);
            arrayType(arrayTypeEncoder);
            vector(vectorEncoder);
        }

        /// <summary>
        /// Encodes a scalar literal.
        /// </summary>
        /// <returns>Encoder of the literal value.</returns>
        public ScalarEncoder Scalar()
        {
            return new ScalarEncoder(Builder);
        }

        /// <summary>
        /// Encodes the type and the value of a literal.
        /// Returns a pair of encoders that must be used in the order they appear in the parameter list.
        /// </summary>
        /// <param name="type">Called first, to encode the type of the literal.</param>
        /// <param name="scalar">Called second, to encode the value of the literal.</param>
        public void TaggedScalar(out CustomAttributeElementTypeEncoder type, out ScalarEncoder scalar)
        {
            type = new CustomAttributeElementTypeEncoder(Builder);
            scalar = new ScalarEncoder(Builder);
        }

        /// <summary>
        /// Encodes the type and the value of a literal.
        /// </summary>
        /// <param name="type">Called first, to encode the type of the literal.</param>
        /// <param name="scalar">Called second, to encode the value of the literal.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="scalar"/> is null.</exception>
        public void TaggedScalar(Action<CustomAttributeElementTypeEncoder> type, Action<ScalarEncoder> scalar)
        {
            if (type is null)
            {
                Throw.ArgumentNull(nameof(type));
            }
            if (scalar is null)
            {
                Throw.ArgumentNull(nameof(scalar));
            }

            CustomAttributeElementTypeEncoder typeEncoder;
            ScalarEncoder scalarEncoder;
            TaggedScalar(out typeEncoder, out scalarEncoder);
            type(typeEncoder);
            scalar(scalarEncoder);
        }
    }

    public readonly struct ScalarEncoder
    {
        public BlobBuilder Builder { get; }

        public ScalarEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        /// <summary>
        /// Encodes <c>null</c> literal of type <see cref="Array"/>.
        /// </summary>
        public void NullArray()
        {
            Builder.WriteInt32(-1);
        }

        /// <summary>
        /// Encodes constant literal.
        /// </summary>
        /// <param name="value">
        /// Constant of type
        /// <see cref="bool"/>,
        /// <see cref="byte"/>,
        /// <see cref="sbyte"/>,
        /// <see cref="short"/>,
        /// <see cref="ushort"/>,
        /// <see cref="int"/>,
        /// <see cref="uint"/>,
        /// <see cref="long"/>,
        /// <see cref="ulong"/>,
        /// <see cref="float"/>,
        /// <see cref="double"/>,
        /// <see cref="char"/> (encoded as two-byte Unicode character),
        /// <see cref="string"/> (encoded as SerString), or
        /// <see cref="Enum"/> (encoded as the underlying integer value).
        /// </param>
        /// <exception cref="ArgumentException">Unexpected constant type.</exception>
        public void Constant(object? value)
        {
            string? str = value as string;
            if (str != null || value == null)
            {
                String(str);
            }
            else
            {
                Builder.WriteConstant(value);
            }
        }

        /// <summary>
        /// Encodes literal of type <see cref="Type"/> (possibly null).
        /// </summary>
        /// <param name="serializedTypeName">The name of the type, or null.</param>
        /// <exception cref="ArgumentException"><paramref name="serializedTypeName"/> is empty.</exception>
        public void SystemType(string? serializedTypeName)
        {
            if (serializedTypeName != null && serializedTypeName.Length == 0)
            {
                Throw.ArgumentEmptyString(nameof(serializedTypeName));
            }

            String(serializedTypeName);
        }

        private void String(string? value)
        {
            Builder.WriteSerializedString(value);
        }
    }

    public readonly struct LiteralsEncoder
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

    public readonly struct VectorEncoder
    {
        public BlobBuilder Builder { get; }

        public VectorEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public LiteralsEncoder Count(int count)
        {
            if (count < 0)
            {
                Throw.ArgumentOutOfRange(nameof(count));
            }

            Builder.WriteUInt32((uint)count);
            return new LiteralsEncoder(Builder);
        }
    }

    public readonly struct NameEncoder
    {
        public BlobBuilder Builder { get; }

        public NameEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void Name(string name)
        {
            if (name is null)
            {
                Throw.ArgumentNull(nameof(name));
            }

            if (name.Length == 0) Throw.ArgumentEmptyString(nameof(name));

            Builder.WriteSerializedString(name);
        }
    }

    public readonly struct CustomAttributeNamedArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomAttributeNamedArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public NamedArgumentsEncoder Count(int count)
        {
            if (unchecked((uint)count) > ushort.MaxValue)
            {
                Throw.ArgumentOutOfRange(nameof(count));
            }

            Builder.WriteUInt16((ushort)count);
            return new NamedArgumentsEncoder(Builder);
        }
    }

    public readonly struct NamedArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public NamedArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        /// <summary>
        /// Encodes a named argument (field or property).
        /// Returns a triplet of encoders that must be used in the order they appear in the parameter list.
        /// </summary>
        /// <param name="isField">True to encode a field, false to encode a property.</param>
        /// <param name="type">Use first, to encode the type of the argument.</param>
        /// <param name="name">Use second, to encode the name of the field or property.</param>
        /// <param name="literal">Use third, to encode the literal value of the argument.</param>
        public void AddArgument(bool isField, out NamedArgumentTypeEncoder type, out NameEncoder name, out LiteralEncoder literal)
        {
            Builder.WriteByte(isField ? (byte)CustomAttributeNamedArgumentKind.Field : (byte)CustomAttributeNamedArgumentKind.Property);
            type = new NamedArgumentTypeEncoder(Builder);
            name = new NameEncoder(Builder);
            literal = new LiteralEncoder(Builder);
        }

        /// <summary>
        /// Encodes a named argument (field or property).
        /// </summary>
        /// <param name="isField">True to encode a field, false to encode a property.</param>
        /// <param name="type">Called first, to encode the type of the argument.</param>
        /// <param name="name">Called second, to encode the name of the field or property.</param>
        /// <param name="literal">Called third, to encode the literal value of the argument.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/> or <paramref name="literal"/> is null.</exception>
        public void AddArgument(bool isField, Action<NamedArgumentTypeEncoder> type, Action<NameEncoder> name, Action<LiteralEncoder> literal)
        {
            if (type is null)
            {
                Throw.ArgumentNull(nameof(type));
            }
            if (name is null)
            {
                Throw.ArgumentNull(nameof(name));
            }
            if (literal is null)
            {
                Throw.ArgumentNull(nameof(literal));
            }

            NamedArgumentTypeEncoder typeEncoder;
            NameEncoder nameEncoder;
            LiteralEncoder literalEncoder;
            AddArgument(isField, out typeEncoder, out nameEncoder, out literalEncoder);
            type(typeEncoder);
            name(nameEncoder);
            literal(literalEncoder);
        }
    }

    public readonly struct NamedArgumentTypeEncoder
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

    public readonly struct CustomAttributeArrayTypeEncoder
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

    public readonly struct CustomAttributeElementTypeEncoder
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
            if (enumTypeName is null)
            {
                Throw.ArgumentNull(nameof(enumTypeName));
            }

            if (enumTypeName.Length == 0) Throw.ArgumentEmptyString(nameof(enumTypeName));

            WriteTypeCode(SerializationTypeCode.Enum);
            Builder.WriteSerializedString(enumTypeName);
        }
    }

    /// <summary>
    /// Encodes a type in a signature.
    /// </summary>
    public readonly struct SignatureTypeEncoder
    {
        /// <summary>
        /// The <see cref="BlobBuilder"/> where the signature is written to.
        /// </summary>
        public BlobBuilder Builder { get; }

        /// <summary>
        /// Creates a <see cref="SignatureTypeEncoder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="BlobBuilder"/> where the signature will be written.</param>
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

        /// <summary>
        /// Encodes <see cref="bool"/>.
        /// </summary>
        public void Boolean() => WriteTypeCode(SignatureTypeCode.Boolean);
        /// <summary>
        /// Encodes <see cref="char"/>.
        /// </summary>
        public void Char() => WriteTypeCode(SignatureTypeCode.Char);
        /// <summary>
        /// Encodes <see cref="sbyte"/>.
        /// </summary>
        public void SByte() => WriteTypeCode(SignatureTypeCode.SByte);
        /// <summary>
        /// Encodes <see cref="byte"/>.
        /// </summary>
        public void Byte() => WriteTypeCode(SignatureTypeCode.Byte);
        /// <summary>
        /// Encodes <see cref="short"/>.
        /// </summary>
        public void Int16() => WriteTypeCode(SignatureTypeCode.Int16);
        /// <summary>
        /// Encodes <see cref="ushort"/>.
        /// </summary>
        public void UInt16() => WriteTypeCode(SignatureTypeCode.UInt16);
        /// <summary>
        /// Encodes <see cref="int"/>.
        /// </summary>
        public void Int32() => WriteTypeCode(SignatureTypeCode.Int32);
        /// <summary>
        /// Encodes <see cref="uint"/>.
        /// </summary>
        public void UInt32() => WriteTypeCode(SignatureTypeCode.UInt32);
        /// <summary>
        /// Encodes <see cref="long"/>.
        /// </summary>
        public void Int64() => WriteTypeCode(SignatureTypeCode.Int64);
        /// <summary>
        /// Encodes <see cref="ulong"/>.
        /// </summary>
        public void UInt64() => WriteTypeCode(SignatureTypeCode.UInt64);
        /// <summary>
        /// Encodes <see cref="float"/>.
        /// </summary>
        public void Single() => WriteTypeCode(SignatureTypeCode.Single);
        /// <summary>
        /// Encodes <see cref="double"/>.
        /// </summary>
        public void Double() => WriteTypeCode(SignatureTypeCode.Double);
        /// <summary>
        /// Encodes <see cref="string"/>.
        /// </summary>
        public void String() => WriteTypeCode(SignatureTypeCode.String);
        /// <summary>
        /// Encodes <see cref="System.TypedReference"/>.
        /// </summary>
        public void TypedReference() => WriteTypeCode(SignatureTypeCode.TypedReference);
        /// <summary>
        /// Encodes <see cref="System.IntPtr"/>.
        /// </summary>
        public void IntPtr() => WriteTypeCode(SignatureTypeCode.IntPtr);
        /// <summary>
        /// Encodes <see cref="System.UIntPtr"/>.
        /// </summary>
        public void UIntPtr() => WriteTypeCode(SignatureTypeCode.UIntPtr);
        /// <summary>
        /// Encodes <see cref="object"/>.
        /// </summary>
        public void Object() => WriteTypeCode(SignatureTypeCode.Object);

        /// <summary>
        /// Encodes a primitive type.
        /// </summary>
        /// <param name="type">Any primitive type code except for <see cref="PrimitiveTypeCode.Void"/>.</param>
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
                case PrimitiveTypeCode.TypedReference:
                case PrimitiveTypeCode.IntPtr:
                case PrimitiveTypeCode.UIntPtr:
                case PrimitiveTypeCode.String:
                case PrimitiveTypeCode.Object:
                    Builder.WriteByte((byte)type);
                    return;

                case PrimitiveTypeCode.Void:
                default:
                    Throw.ArgumentOutOfRange(nameof(type));
                    return;
            }
        }

        /// <summary>
        /// Starts encoding an array type.
        /// Returns a pair of encoders that must be used in the order they appear in the parameter list.
        /// </summary>
        /// <param name="elementType">Use first, to encode the type of the element.</param>
        /// <param name="arrayShape">Use second, to encode the shape of the array.</param>
        public void Array(out SignatureTypeEncoder elementType, out ArrayShapeEncoder arrayShape)
        {
            Builder.WriteByte((byte)SignatureTypeCode.Array);
            elementType = this;
            arrayShape = new ArrayShapeEncoder(Builder);
        }

        /// <summary>
        /// Encodes an array type.
        /// </summary>
        /// <param name="elementType">Called first, to encode the type of the element.</param>
        /// <param name="arrayShape">Called second, to encode the shape of the array.</param>
        /// <exception cref="ArgumentNullException"><paramref name="elementType"/> or <paramref name="arrayShape"/> is null.</exception>
        public void Array(Action<SignatureTypeEncoder> elementType, Action<ArrayShapeEncoder> arrayShape)
        {
            if (elementType is null)
            {
                Throw.ArgumentNull(nameof(elementType));
            }
            if (arrayShape is null)
            {
                Throw.ArgumentNull(nameof(arrayShape));
            }

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
            // Note: We don't allow TypeSpec as per https://github.com/dotnet/runtime/blob/main/src/libraries/System.Reflection.Metadata/specs/Ecma-335-Issues.md#proposed-specification-change
            int codedIndex = CodedIndex.TypeDefOrRef(type);

            ClassOrValue(isValueType);
            Builder.WriteCompressedInteger(codedIndex);
        }

        /// <summary>
        /// Starts encoding a function pointer signature.
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
        /// Starts encoding a generic instantiation signature.
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
            // Note: We don't allow TypeSpec as per https://github.com/dotnet/runtime/blob/main/src/libraries/System.Reflection.Metadata/specs/Ecma-335-Issues.md#proposed-specification-change
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
        /// Starts encoding a pointer signature.
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
        /// Starts encoding an SZ array (vector) signature.
        /// </summary>
        public SignatureTypeEncoder SZArray()
        {
            Builder.WriteByte((byte)SignatureTypeCode.SZArray);
            return this;
        }

        /// <summary>
        /// Starts encoding a signature of a type with custom modifiers.
        /// </summary>
        public CustomModifiersEncoder CustomModifiers()
        {
            return new CustomModifiersEncoder(Builder);
        }
    }

    public readonly struct CustomModifiersEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomModifiersEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        /// <summary>
        /// Encodes a custom modifier.
        /// </summary>
        /// <param name="type"><see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/> or <see cref="TypeSpecificationHandle"/>.</param>
        /// <param name="isOptional">Is optional modifier.</param>
        /// <returns>Encoder of subsequent modifiers.</returns>
        /// <exception cref="ArgumentException"><paramref name="type"/> is nil or of an unexpected kind.</exception>
        public CustomModifiersEncoder AddModifier(EntityHandle type, bool isOptional)
        {
            if (type.IsNil)
            {
                Throw.InvalidArgument_Handle(nameof(type));
            }

            if (isOptional)
            {
                Builder.WriteByte((byte)SignatureTypeCode.OptionalModifier);
            }
            else
            {
                Builder.WriteByte((byte)SignatureTypeCode.RequiredModifier);
            }

            Builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(type));
            return this;
        }
    }

    public readonly struct ArrayShapeEncoder
    {
        public BlobBuilder Builder { get; }

        public ArrayShapeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        /// <summary>
        /// Encodes array shape.
        /// </summary>
        /// <param name="rank">The number of dimensions in the array (shall be 1 or more).</param>
        /// <param name="sizes">
        /// Dimension sizes. The array may be shorter than <paramref name="rank"/> but not longer.
        /// </param>
        /// <param name="lowerBounds">
        /// Dimension lower bounds, or <c>default(<see cref="ImmutableArray{Int32}"/>)</c> to set all <paramref name="rank"/> lower bounds to 0.
        /// The array may be shorter than <paramref name="rank"/> but not longer.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="rank"/> is outside of range [1, 0xffff],
        /// smaller than <paramref name="sizes"/>.Length, or
        /// smaller than <paramref name="lowerBounds"/>.Length.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="sizes"/> is null.</exception>
        public void Shape(int rank, ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
        {
            // The specification doesn't impose a limit on the max number of array dimensions.
            // The CLR supports <64. More than 0xffff is causing crashes in various tools (ildasm).
            if (unchecked((uint)(rank - 1)) > ushort.MaxValue - 1)
            {
                Throw.ArgumentOutOfRange(nameof(rank));
            }

            if (sizes.IsDefault)
            {
                Throw.ArgumentNull(nameof(sizes));
            }

            // rank
            Builder.WriteCompressedInteger(rank);

            // sizes
            if (sizes.Length > rank)
            {
                Throw.ArgumentOutOfRange(nameof(rank));
            }

            Builder.WriteCompressedInteger(sizes.Length);
            foreach (int size in sizes)
            {
                Builder.WriteCompressedInteger(size);
            }

            // lower bounds
            if (lowerBounds.IsDefault) // TODO: remove -- update Roslyn
            {
                Builder.WriteCompressedInteger(rank);
                for (int i = 0; i < rank; i++)
                {
                    Builder.WriteCompressedSignedInteger(0);
                }
            }
            else
            {
                if (lowerBounds.Length > rank)
                {
                    Throw.ArgumentOutOfRange(nameof(rank));
                }

                Builder.WriteCompressedInteger(lowerBounds.Length);
                foreach (int lowerBound in lowerBounds)
                {
                    Builder.WriteCompressedSignedInteger(lowerBound);
                }
            }
        }
    }

    public readonly struct ReturnTypeEncoder
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

    public readonly struct ParametersEncoder
    {
        public BlobBuilder Builder { get; }
        public bool HasVarArgs { get; }

        public ParametersEncoder(BlobBuilder builder, bool hasVarArgs = false)
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
