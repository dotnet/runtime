// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using Internal.CorConstants;
using Internal.ReadyToRunConstants;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// This represents all possible signatures that is
    /// </summary>
    public abstract class ReadyToRunSignature
    {
        private SignatureDecoder _decoder;
        public ReadyToRunFixupKind FixupKind {get;private set;}
        public ReadyToRunSignature(SignatureDecoder decoder, ReadyToRunFixupKind fixupKind)
        {
            _decoder = decoder;
            FixupKind = fixupKind;
        }

        public string ToString(SignatureFormattingOptions options)
        {
            StringBuilder builder = new StringBuilder();
            _decoder.Reset();
            _decoder.Context.Options = options;
            _decoder.ReadR2RSignature(builder);
            return builder.ToString();
        }
    }

    /// <summary>
    /// For now, this means the signature is not parsed yet
    /// </summary>
    public class TodoSignature : ReadyToRunSignature
    {
        public TodoSignature(SignatureDecoder decoder, ReadyToRunFixupKind fixupKind) : base(decoder, fixupKind)
        {
        }
    }

    public class MethodDefEntrySignature : ReadyToRunSignature
    {
        public uint MethodDefToken { get; set; }

        public MethodDefEntrySignature(SignatureDecoder decoder) : base(decoder, ReadyToRunFixupKind.MethodEntry_DefToken)
        {
        }
    }

    public class MethodRefEntrySignature : ReadyToRunSignature
    {
        public uint MethodRefToken { get; set; }

        public MethodRefEntrySignature(SignatureDecoder decoder) : base(decoder, ReadyToRunFixupKind.MethodEntry_RefToken)
        {
        }
    }

    /// <summary>
    /// Helper class for converting metadata tokens into their textual representation.
    /// </summary>
    public class MetadataNameFormatter : DisassemblingTypeProvider
    {
        /// <summary>
        /// Metadata reader used for the purpose of metadata-based name formatting.
        /// </summary>
        private readonly MetadataReader _metadataReader;

        public MetadataNameFormatter(MetadataReader metadataReader)
        {
            _metadataReader = metadataReader;
        }

        /// <summary>
        /// Construct the textual representation of a given metadata handle.
        /// </summary>
        /// <param name="metadataReader">Metadata reader corresponding to the handle</param>
        /// <param name="handle">Metadata handle to parse</param>
        /// <param name="namespaceQualified">Include namespace in type names</param>
        public static string FormatHandle(MetadataReader metadataReader, Handle handle, bool namespaceQualified = true, string owningTypeOverride = null, string signaturePrefix = "")
        {
            MetadataNameFormatter formatter = new MetadataNameFormatter(metadataReader);
            return formatter.EmitHandleName(handle, namespaceQualified, owningTypeOverride, signaturePrefix);
        }

        public static ReadyToRunSignature FormatSignature(IAssemblyResolver assemblyResolver, ReadyToRunReader r2rReader, int imageOffset)
        {
            SignatureFormattingOptions dummyOptions = new SignatureFormattingOptions();
            SignatureDecoder decoder = new SignatureDecoder(assemblyResolver, dummyOptions, r2rReader.GetGlobalMetadata()?.MetadataReader, r2rReader, imageOffset);
            StringBuilder dummyBuilder = new StringBuilder();
            return decoder.ReadR2RSignature(dummyBuilder);
        }

        /// <summary>
        /// Emit a given token to a specified string builder.
        /// </summary>
        /// <param name="methodToken">ECMA token to provide string representation for</param>
        private string EmitHandleName(Handle handle, bool namespaceQualified, string owningTypeOverride, string signaturePrefix = "")
        {
            try
            {
                switch (handle.Kind)
                {
                    case HandleKind.MemberReference:
                        return EmitMemberReferenceName((MemberReferenceHandle)handle, owningTypeOverride, signaturePrefix);

                    case HandleKind.MethodSpecification:
                        return EmitMethodSpecificationName((MethodSpecificationHandle)handle, owningTypeOverride, signaturePrefix);

                    case HandleKind.MethodDefinition:
                        return EmitMethodDefinitionName((MethodDefinitionHandle)handle, owningTypeOverride, signaturePrefix);

                    case HandleKind.TypeReference:
                        return EmitTypeReferenceName((TypeReferenceHandle)handle, namespaceQualified, signaturePrefix);

                    case HandleKind.TypeSpecification:
                        return EmitTypeSpecificationName((TypeSpecificationHandle)handle, namespaceQualified, signaturePrefix);

                    case HandleKind.TypeDefinition:
                        return EmitTypeDefinitionName((TypeDefinitionHandle)handle, namespaceQualified, signaturePrefix);

                    case HandleKind.FieldDefinition:
                        return EmitFieldDefinitionName((FieldDefinitionHandle)handle, namespaceQualified, owningTypeOverride, signaturePrefix);

                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                return $"$$INVALID-{handle.Kind}-{MetadataTokens.GetRowNumber((EntityHandle)handle):X6}: {ex.Message}";
            }
        }

        /// <summary>
        /// Check that the metadata handle has valid range in the appropriate table context.
        /// </summary>
        /// <param name="handle">Metadata handle to validate</param>
        private void ValidateHandle(EntityHandle handle, TableIndex tableIndex)
        {
            int rowid = MetadataTokens.GetRowNumber(handle);
            int tableRowCount = _metadataReader.GetTableRowCount(tableIndex);
            if (rowid <= 0 || rowid > tableRowCount)
            {
                throw new NotImplementedException($"Invalid handle {MetadataTokens.GetToken(handle):X8} in table {tableIndex.ToString()} ({tableRowCount} rows)");
            }
        }

        /// <summary>
        /// Emit a method specification.
        /// </summary>
        /// <param name="methodSpecHandle">Method specification handle</param>
        private string EmitMethodSpecificationName(MethodSpecificationHandle methodSpecHandle, string owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(methodSpecHandle, TableIndex.MethodSpec);
            MethodSpecification methodSpec = _metadataReader.GetMethodSpecification(methodSpecHandle);
            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            return EmitHandleName(methodSpec.Method, namespaceQualified: true, owningTypeOverride: owningTypeOverride, signaturePrefix: signaturePrefix)
                + methodSpec.DecodeSignature<string, DisassemblingGenericContext>(this, genericContext);
        }

        /// <summary>
        /// Emit a method reference.
        /// </summary>
        /// <param name="memberRefHandle">Member reference handle</param>
        private string EmitMemberReferenceName(MemberReferenceHandle memberRefHandle, string owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(memberRefHandle, TableIndex.MemberRef);
            MemberReference memberRef = _metadataReader.GetMemberReference(memberRefHandle);
            StringBuilder builder = new StringBuilder();
            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            switch (memberRef.GetKind())
            {
                case MemberReferenceKind.Field:
                    {
                        string fieldSig = memberRef.DecodeFieldSignature<string, DisassemblingGenericContext>(this, genericContext);
                        builder.Append(fieldSig);
                        builder.Append(" ");
                        builder.Append(EmitContainingTypeAndMemberName(memberRef, owningTypeOverride, signaturePrefix));
                        break;
                    }

                case MemberReferenceKind.Method:
                    {
                        MethodSignature<String> methodSig = memberRef.DecodeMethodSignature<string, DisassemblingGenericContext>(this, genericContext);
                        builder.Append(methodSig.ReturnType);
                        builder.Append(" ");
                        builder.Append(EmitContainingTypeAndMemberName(memberRef, owningTypeOverride, signaturePrefix));
                        builder.Append(EmitMethodSignature(methodSig));
                        break;
                    }

                default:
                    throw new NotImplementedException(memberRef.GetKind().ToString());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Emit a method definition.
        /// </summary>
        /// <param name="methodSpecHandle">Method definition handle</param>
        private string EmitMethodDefinitionName(MethodDefinitionHandle methodDefinitionHandle, string owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(methodDefinitionHandle, TableIndex.MethodDef);
            MethodDefinition methodDef = _metadataReader.GetMethodDefinition(methodDefinitionHandle);
            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            MethodSignature<string> methodSig = methodDef.DecodeSignature<string, DisassemblingGenericContext>(this, genericContext);
            StringBuilder builder = new StringBuilder();
            builder.Append(methodSig.ReturnType);
            builder.Append(" ");
            if (owningTypeOverride == null)
            {
                owningTypeOverride = EmitHandleName(methodDef.GetDeclaringType(), namespaceQualified: true, owningTypeOverride: null);
            }
            builder.Append(owningTypeOverride);
            builder.Append(".");
            builder.Append(signaturePrefix);
            builder.Append(EmitString(methodDef.Name));
            builder.Append(EmitMethodSignature(methodSig));
            return builder.ToString();
        }

        /// <summary>
        /// Emit method generic arguments and parameter list.
        /// </summary>
        /// <param name="methodSignature">Method signature to format</param>
        private string EmitMethodSignature(MethodSignature<string> methodSignature)
        {
            StringBuilder builder = new StringBuilder();
            if (methodSignature.GenericParameterCount != 0)
            {
                builder.Append("<");
                bool firstTypeArg = true;
                for (int typeArgIndex = 0; typeArgIndex < methodSignature.GenericParameterCount; typeArgIndex++)
                {
                    if (firstTypeArg)
                    {
                        firstTypeArg = false;
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                    builder.Append("!!");
                    builder.Append(typeArgIndex);
                }
                builder.Append(">");
            }
            builder.Append("(");
            bool firstMethodArg = true;
            foreach (string paramType in methodSignature.ParameterTypes)
            {
                if (firstMethodArg)
                {
                    firstMethodArg = false;
                }
                else
                {
                    builder.Append(", ");
                }
                builder.Append(paramType);
            }
            builder.Append(")");
            return builder.ToString();
        }

        /// <summary>
        /// Emit containing type and member name.
        /// </summary>
        /// <param name="memberRef">Member reference to format</param>
        /// <param name="owningTypeOverride">Optional override for the owning type, null = MemberReference.Parent</param>
        /// <param name="signaturePrefix">Optional member signature prefix</param>
        private string EmitContainingTypeAndMemberName(MemberReference memberRef, string owningTypeOverride, string signaturePrefix)
        {
            if (owningTypeOverride == null)
            {
                owningTypeOverride = EmitHandleName(memberRef.Parent, namespaceQualified: true, owningTypeOverride: null);
            }
            return owningTypeOverride + "." + signaturePrefix + EmitString(memberRef.Name);
        }

        /// <summary>
        /// Emit type reference.
        /// </summary>
        /// <param name="typeRefHandle">Type reference handle</param>
        /// <param name="namespaceQualified">When set to true, include namespace information</param>
        /// <param name="signaturePrefix">Optional type name signature prefix</param>
        private string EmitTypeReferenceName(TypeReferenceHandle typeRefHandle, bool namespaceQualified, string signaturePrefix)
        {
            ValidateHandle(typeRefHandle, TableIndex.TypeRef);
            TypeReference typeRef = _metadataReader.GetTypeReference(typeRefHandle);
            string typeName = EmitString(typeRef.Name);
            string output = "";
            if (typeRef.ResolutionScope.Kind != HandleKind.AssemblyReference)
            {
                // Nested type - format enclosing type followed by the nested type
                return EmitHandleName(typeRef.ResolutionScope, namespaceQualified, owningTypeOverride: null) + "+" + typeName;
            }
            if (namespaceQualified)
            {
                output = EmitString(typeRef.Namespace);
                if (!string.IsNullOrEmpty(output))
                {
                    output += ".";
                }
            }
            return output + signaturePrefix + typeName;
        }

        /// <summary>
        /// Emit a type definition.
        /// </summary>
        /// <param name="typeDefHandle">Type definition handle</param>
        /// <param name="namespaceQualified">true = prefix type name with namespace information</param>
        /// <param name="signaturePrefix">Optional type name signature prefix</param>
        /// <returns></returns>
        private string EmitTypeDefinitionName(TypeDefinitionHandle typeDefHandle, bool namespaceQualified, string signaturePrefix)
        {
            ValidateHandle(typeDefHandle, TableIndex.TypeDef);
            TypeDefinition typeDef = _metadataReader.GetTypeDefinition(typeDefHandle);
            string typeName = signaturePrefix + EmitString(typeDef.Name);
            if (typeDef.IsNested)
            {
                // Nested type
                return EmitHandleName(typeDef.GetDeclaringType(), namespaceQualified, owningTypeOverride: null) + "+" + typeName;
            }

            string output;
            if (namespaceQualified)
            {
                output = EmitString(typeDef.Namespace);
                if (!string.IsNullOrEmpty(output))
                {
                    output += ".";
                }
            }
            else
            {
                output = "";
            }
            return output + typeName;
        }

        /// <summary>
        /// Emit an arbitrary type specification.
        /// </summary>
        /// <param name="typeSpecHandle">Type specification handle</param>
        /// <param name="namespaceQualified">When set to true, include namespace information</param>
        private string EmitTypeSpecificationName(TypeSpecificationHandle typeSpecHandle, bool namespaceQualified, string signaturePrefix)
        {
            ValidateHandle(typeSpecHandle, TableIndex.TypeSpec);
            TypeSpecification typeSpec = _metadataReader.GetTypeSpecification(typeSpecHandle);
            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            return typeSpec.DecodeSignature<string, DisassemblingGenericContext>(this, genericContext);
        }

        /// <summary>
        /// Emit the textual representation of a FieldDef metadata record.
        /// </summary>
        /// <param name="fieldDefHandle">Field definition handle to format</param>
        /// <param name="namespaceQualified">True = display namespace information for the owning type</param>
        /// <param name="owningTypeOverride">Owning type override when non-null</param>
        /// <param name="signaturePrefix">Optional field name signature prefix</param>
        /// <returns>Textual representation of the field declaration</returns>
        private string EmitFieldDefinitionName(FieldDefinitionHandle fieldDefHandle, bool namespaceQualified, string owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(fieldDefHandle, TableIndex.Field);
            FieldDefinition fieldDef = _metadataReader.GetFieldDefinition(fieldDefHandle);
            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            StringBuilder output = new StringBuilder();
            output.Append(fieldDef.DecodeSignature<string, DisassemblingGenericContext>(this, genericContext));
            output.Append(' ');
            output.Append(EmitHandleName(fieldDef.GetDeclaringType(), namespaceQualified, owningTypeOverride));
            output.Append('.');
            output.Append(signaturePrefix);
            output.Append(_metadataReader.GetString(fieldDef.Name));
            return output.ToString();
        }

        private string EmitString(StringHandle handle)
        {
            return _metadataReader.GetString(handle);
        }
    }

    public interface IR2RSignatureTypeProvider<TType, TMethod, TGenericContext> : ISignatureTypeProvider<TType, TGenericContext>
    {
        TType GetCanonType();
        TMethod GetMethodFromMethodDef(MetadataReader reader, MethodDefinitionHandle handle, TType owningTypeOverride);
        TMethod GetMethodFromMemberRef(MetadataReader reader, MemberReferenceHandle handle, TType owningTypeOverride);
        TMethod GetInstantiatedMethod(TMethod uninstantiatedMethod, ImmutableArray<TType> instantiation);
        TMethod GetConstrainedMethod(TMethod method, TType constraint);
        TMethod GetMethodWithFlags(ReadyToRunMethodSigFlags flags, TMethod method);
    }

    /// <summary>
    /// Helper class used as state machine for decoding a single signature.
    /// </summary>
    public class R2RSignatureDecoder<TType, TMethod, TGenericContext>
    {
        /// <summary>
        /// ECMA reader is used to access the embedded MSIL metadata blob in the R2R file.
        /// </summary>
        protected readonly MetadataReader _metadataReader;

        /// <summary>
        /// Outer ECMA reader is used as the default context for generic parameters.
        /// </summary>
        private readonly MetadataReader _outerReader;

        /// <summary>
        /// ECMA reader representing the reference module of the signature being decoded.
        /// </summary>
        protected readonly ReadyToRunReader _contextReader;

        /// <summary>
        /// Byte array representing the R2R PE file read from disk.
        /// </summary>
        protected readonly byte[] _image;

        /// <summary>
        /// Offset within the image file.
        /// </summary>
        private int _offset;

        /// <summary>
        /// Offset within the image file when the object is constructed.
        /// </summary>
        private readonly int _originalOffset;

        /// <summary>
        /// Query signature parser for the current offset.
        /// </summary>
        public int Offset => _offset;

        private IR2RSignatureTypeProvider<TType, TMethod, TGenericContext> _provider;

        protected void UpdateOffset(int offset)
        {
            _offset = offset;
        }

        public TGenericContext Context { get; }

        /// <summary>
        /// Construct the signature decoder by storing the image byte array and offset within the array.
        /// </summary>
        /// <param name="r2rReader">R2RReader object representing the PE file containing the ECMA metadata</param>
        /// <param name="offset">Signature offset within the PE file byte array</param>
        public R2RSignatureDecoder(IR2RSignatureTypeProvider<TType, TMethod, TGenericContext> provider, TGenericContext context, MetadataReader metadataReader, ReadyToRunReader r2rReader, int offset, bool skipOverrideMetadataReader = false)
        {
            Context = context;
            _provider = provider;
            _image = r2rReader.Image;
            _originalOffset = _offset = offset;
            _contextReader = r2rReader;
            MetadataReader moduleOverrideMetadataReader = null;
            if (!skipOverrideMetadataReader)
                moduleOverrideMetadataReader = TryGetModuleOverrideMetadataReader();
            _metadataReader = moduleOverrideMetadataReader ?? metadataReader;
            _outerReader = moduleOverrideMetadataReader ?? metadataReader;
            Reset();
        }

        /// <summary>
        /// Construct the signature decoder by storing the image byte array and offset within the array.
        /// </summary>
        /// <param name="options">Dump options and paths</param>
        /// <param name="metadataReader">Metadata reader for the R2R image</param>
        /// <param name="signature">Signature to parse</param>
        /// <param name="offset">Signature offset within the signature byte array</param>
        /// <param name="outerReader">Metadata reader representing the outer signature context</param>
        /// <param name="contextReader">Top-level signature context reader</param>
        public R2RSignatureDecoder(IR2RSignatureTypeProvider<TType, TMethod, TGenericContext> provider, TGenericContext context, MetadataReader metadataReader, byte[] signature, int offset, MetadataReader outerReader, ReadyToRunReader contextReader, bool skipOverrideMetadataReader = false)
        {
            Context = context;
            _provider = provider;
            _image = signature;
            _originalOffset = _offset = offset;
            _contextReader = contextReader;
            MetadataReader moduleOverrideMetadataReader = null;
            if (!skipOverrideMetadataReader)
                moduleOverrideMetadataReader = TryGetModuleOverrideMetadataReader();
            _metadataReader = moduleOverrideMetadataReader ?? metadataReader;
            _outerReader = moduleOverrideMetadataReader ?? outerReader;
            Reset();
        }

        private MetadataReader TryGetModuleOverrideMetadataReader()
        {
            bool moduleOverride = (ReadByte() & (byte)ReadyToRunFixupKind.ModuleOverride) != 0;
            // Check first byte for a module override being encoded
            if (moduleOverride)
            {
                int moduleIndex = (int)ReadUInt();
                IAssemblyMetadata refAsmEcmaReader = _contextReader.OpenReferenceAssembly(moduleIndex);
                return refAsmEcmaReader.MetadataReader;
            }

            return null;
        }

        /// <summary>
        /// Reset the offset back to the point where the decoder is constructed to allow re-decoding the same signature.
        /// </summary>
        internal void Reset()
        {
            this._offset = _originalOffset;
        }

        /// <summary>
        /// Read a single byte from the signature stream and advances the current offset.
        /// </summary>
        public byte ReadByte()
        {
            return _image[_offset++];
        }

        /// <summary>
        /// Read a single unsigned 32-bit in from the signature stream. Adapted from CorSigUncompressData,
        /// <a href="">https://github.com/dotnet/coreclr/blob/master/src/inc/cor.h</a>.
        /// </summary>
        /// <param name="data"></param>
        public uint ReadUInt()
        {
            // Handle smallest data inline.
            byte firstByte = ReadByte();
            if ((firstByte & 0x80) == 0x00) // 0??? ????
                return firstByte;

            uint res;
            // Medium.
            if ((firstByte & 0xC0) == 0x80)  // 10?? ????
            {
                res = ((uint)(firstByte & 0x3f) << 8);
                res |= ReadByte();
            }
            else // 110? ????
            {
                res = (uint)(firstByte & 0x1f) << 24;
                res |= (uint)ReadByte() << 16;
                res |= (uint)ReadByte() << 8;
                res |= (uint)ReadByte();
            }
            return res;
        }

        /// <summary>
        /// Read a signed integer from the signature stream. Signed integer is basically encoded
        /// as an unsigned integer after converting it to the unsigned number 2 * abs(x) + (x &gt;= 0 ? 0 : 1).
        /// Adapted from CorSigUncompressSignedInt, <a href="">https://github.com/dotnet/coreclr/blob/master/src/inc/cor.h</a>.
        /// </summary>
        public int ReadInt()
        {
            uint rawData = ReadUInt();
            int data = (int)(rawData >> 1);
            return ((rawData & 1) == 0 ? +data : -data);
        }

        /// <summary>
        /// Read an encoded token from the stream. This encoding left-shifts the token RID twice and
        /// fills in the two least-important bits with token type (typeDef, typeRef, typeSpec, baseType).
        /// </summary>
        public uint ReadToken()
        {
            uint encodedToken = ReadUInt();
            uint rid = encodedToken >> 2;
            CorTokenType type;
            switch (encodedToken & 3)
            {
                case 0:
                    type = CorTokenType.mdtTypeDef;
                    break;

                case 1:
                    type = CorTokenType.mdtTypeRef;
                    break;

                case 2:
                    type = CorTokenType.mdtTypeSpec;
                    break;

                case 3:
                    type = CorTokenType.mdtBaseType;
                    break;

                default:
                    // This should never happen
                    throw new NotImplementedException();
            }
            return (uint)type | rid;
        }

        /// <summary>
        /// Read a single element type from the signature stream. Adapted from CorSigUncompressElementType,
        /// <a href="">https://github.com/dotnet/coreclr/blob/master/src/inc/cor.h</a>.
        /// </summary>
        /// <returns></returns>
        public CorElementType ReadElementType()
        {
            return (CorElementType)(ReadByte() & 0x7F);
        }

        public CorElementType PeekElementType()
        {
            return (CorElementType)(_image[_offset] & 0x7F);
        }

        /// <summary>
        /// Decode a type from the signature stream.
        /// </summary>
        /// <param name="builder"></param>
        public TType ParseType()
        {
            CorElementType corElemType = ReadElementType();
            switch (corElemType)
            {
                case CorElementType.ELEMENT_TYPE_VOID:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                case CorElementType.ELEMENT_TYPE_CHAR:
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_I2:
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_I4:
                case CorElementType.ELEMENT_TYPE_I8:
                case CorElementType.ELEMENT_TYPE_U4:
                case CorElementType.ELEMENT_TYPE_U8:
                case CorElementType.ELEMENT_TYPE_R4:
                case CorElementType.ELEMENT_TYPE_R8:
                case CorElementType.ELEMENT_TYPE_STRING:
                case CorElementType.ELEMENT_TYPE_OBJECT:
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
                case CorElementType.ELEMENT_TYPE_TYPEDBYREF:
                    return _provider.GetPrimitiveType((PrimitiveTypeCode)corElemType);

                case CorElementType.ELEMENT_TYPE_PTR:
                    return _provider.GetPointerType(ParseType());

                case CorElementType.ELEMENT_TYPE_BYREF:
                    return _provider.GetByReferenceType(ParseType());

                case CorElementType.ELEMENT_TYPE_VALUETYPE:
                case CorElementType.ELEMENT_TYPE_CLASS:
                    return ParseTypeDefOrRef(corElemType);

                case CorElementType.ELEMENT_TYPE_VAR:
                    {
                        uint varIndex = ReadUInt();
                        return _provider.GetGenericTypeParameter(Context, (int)varIndex);
                    }

                case CorElementType.ELEMENT_TYPE_ARRAY:
                    {
                        TType elementType = ParseType();
                        uint rank = ReadUInt();
                        if (rank == 0)
                            return _provider.GetSZArrayType(elementType);

                        uint sizeCount = ReadUInt(); // number of sizes
                        uint[] sizes = new uint[sizeCount];
                        for (uint sizeIndex = 0; sizeIndex < sizeCount; sizeIndex++)
                        {
                            sizes[sizeIndex] = ReadUInt();
                        }
                        uint lowerBoundCount = ReadUInt(); // number of lower bounds
                        int[] lowerBounds = new int[lowerBoundCount];
                        for (uint lowerBoundIndex = 0; lowerBoundIndex < lowerBoundCount; lowerBoundIndex++)
                        {
                            lowerBounds[lowerBoundIndex] = ReadInt();
                        }
                        ArrayShape arrayShape = new ArrayShape((int)rank, ((int[])(object)sizes).ToImmutableArray(), lowerBounds.ToImmutableArray());
                        return _provider.GetArrayType(elementType, arrayShape);
                    }

                case CorElementType.ELEMENT_TYPE_GENERICINST:
                    {
                        TType genericType = ParseType();
                        uint typeArgCount = ReadUInt();
                        var outerDecoder = new R2RSignatureDecoder<TType, TMethod, TGenericContext>(_provider, Context, _outerReader, _image, _offset, _outerReader, _contextReader);
                        List<TType> parsedTypes = new List<TType>();
                        for (uint paramIndex = 0; paramIndex < typeArgCount; paramIndex++)
                        {
                            parsedTypes.Add(outerDecoder.ParseType());
                        }
                        _offset = outerDecoder.Offset;
                        return _provider.GetGenericInstantiation(genericType, parsedTypes.ToImmutableArray());
                    }

                case CorElementType.ELEMENT_TYPE_FNPTR:
                    var sigHeader = new SignatureHeader(ReadByte());
                    int genericParamCount = 0;
                    if (sigHeader.IsGeneric)
                    {
                        genericParamCount = (int)ReadUInt();
                    }
                    int paramCount = (int)ReadUInt();
                    TType returnType = ParseType();
                    TType[] paramTypes = new TType[paramCount];
                    int requiredParamCount = -1;
                    for (int i = 0; i < paramCount; i++)
                    {
                        while (PeekElementType() == CorElementType.ELEMENT_TYPE_SENTINEL)
                        {
                            requiredParamCount = i;
                            ReadElementType(); // Skip over sentinel
                        }
                        paramTypes[i] = ParseType();
                    }
                    if (requiredParamCount == -1)
                        requiredParamCount = paramCount;

                    MethodSignature<TType> methodSig = new MethodSignature<TType>(sigHeader, returnType, requiredParamCount, genericParamCount, paramTypes.ToImmutableArray());
                    return _provider.GetFunctionPointerType(methodSig);

                case CorElementType.ELEMENT_TYPE_SZARRAY:
                    return _provider.GetSZArrayType(ParseType());

                case CorElementType.ELEMENT_TYPE_MVAR:
                    {
                        uint varIndex = ReadUInt();
                        return _provider.GetGenericMethodParameter(Context, (int)varIndex);
                    }

                case CorElementType.ELEMENT_TYPE_CMOD_REQD:
                    return _provider.GetModifiedType(ParseTypeDefOrRefOrSpec(corElemType), ParseType(), true);

                case CorElementType.ELEMENT_TYPE_CMOD_OPT:
                    return _provider.GetModifiedType(ParseTypeDefOrRefOrSpec(corElemType), ParseType(), false);

                case CorElementType.ELEMENT_TYPE_HANDLE:
                    throw new BadImageFormatException("handle");

                case CorElementType.ELEMENT_TYPE_SENTINEL:
                    throw new BadImageFormatException("sentinel");

                case CorElementType.ELEMENT_TYPE_PINNED:
                    return _provider.GetPinnedType(ParseType());

                case CorElementType.ELEMENT_TYPE_VAR_ZAPSIG:
                    throw new BadImageFormatException("var_zapsig");

                case CorElementType.ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
                    throw new BadImageFormatException("native_valuetype_zapsig");

                case CorElementType.ELEMENT_TYPE_CANON_ZAPSIG:
                    return _provider.GetCanonType();

                case CorElementType.ELEMENT_TYPE_MODULE_ZAPSIG:
                    {
                        int moduleIndex = (int)ReadUInt();
                        IAssemblyMetadata refAsmReader = _contextReader.OpenReferenceAssembly(moduleIndex);
                        var refAsmDecoder = new R2RSignatureDecoder<TType, TMethod, TGenericContext>(_provider, Context, refAsmReader.MetadataReader, _image, _offset, _outerReader, _contextReader);
                        var result = refAsmDecoder.ParseType();
                        _offset = refAsmDecoder.Offset;
                        return result;
                    }

                default:
                    throw new NotImplementedException();
            }
        }


        private TType ParseTypeDefOrRef(CorElementType corElemType)
        {
            uint token = ReadToken();
            var handle = MetadataTokens.Handle((int)token);
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return _provider.GetTypeFromDefinition(_metadataReader, (TypeDefinitionHandle)handle, (byte)corElemType);
                case HandleKind.TypeReference:
                    return _provider.GetTypeFromReference(_metadataReader, (TypeReferenceHandle)handle, (byte)corElemType);
                default:
                    throw new BadImageFormatException();
            }
        }

        private TType ParseTypeDefOrRefOrSpec(CorElementType corElemType)
        {
            uint token = ReadToken();
            var handle = MetadataTokens.Handle((int)token);
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    return _provider.GetTypeFromDefinition(_metadataReader, (TypeDefinitionHandle)handle, (byte)corElemType);
                case HandleKind.TypeReference:
                    return _provider.GetTypeFromReference(_metadataReader, (TypeReferenceHandle)handle, (byte)corElemType);
                case HandleKind.TypeSpecification:
                    return _provider.GetTypeFromSpecification(_metadataReader, Context, (TypeSpecificationHandle)handle, (byte)corElemType);
                default:
                    throw new BadImageFormatException();
            }
        }

        public TMethod ParseMethod()
        {
            uint methodFlags = ReadUInt();

            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_UpdateContext) != 0)
            {
                int moduleIndex = (int)ReadUInt();
                IAssemblyMetadata refAsmReader = _contextReader.OpenReferenceAssembly(moduleIndex);
                var refAsmDecoder = new R2RSignatureDecoder<TType, TMethod, TGenericContext>(_provider, Context, refAsmReader.MetadataReader, _image, _offset, _outerReader, _contextReader, skipOverrideMetadataReader: true);
                var result = refAsmDecoder.ParseMethodWithMethodFlags(methodFlags);
                _offset = refAsmDecoder.Offset;
                return result;
            }
            else
            {
                return ParseMethodWithMethodFlags(methodFlags);
            }
        }


        private TMethod ParseMethodWithMethodFlags(uint methodFlags)
        {
            TType owningTypeOverride = default(TType);
            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_OwnerType) != 0)
            {
                owningTypeOverride = ParseType();
                methodFlags &= ~(uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_OwnerType;
            }

            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_SlotInsteadOfToken) != 0)
            {
                throw new NotImplementedException();
            }

            TMethod result;
            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MemberRefToken) != 0)
            {
                methodFlags &= ~(uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MemberRefToken;
                result = ParseMethodRefToken(owningTypeOverride: owningTypeOverride);
            }
            else
            {
                result = ParseMethodDefToken(owningTypeOverride: owningTypeOverride);
            }

            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MethodInstantiation) != 0)
            {
                methodFlags &= ~(uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MethodInstantiation;
                uint typeArgCount = ReadUInt();
                TType[] instantiationArgs = new TType[typeArgCount];
                for (int typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
                {
                    instantiationArgs[typeArgIndex] = ParseType();
                }
                result = _provider.GetInstantiatedMethod(result, instantiationArgs.ToImmutableArray());
            }

            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_Constrained) != 0)
            {
                methodFlags &= ~(uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_Constrained;
                result = _provider.GetConstrainedMethod(result, ParseType());
            }

            // Any other flags should just be directly recorded
            if (methodFlags != 0)
                result = _provider.GetMethodWithFlags((ReadyToRunMethodSigFlags)methodFlags, result);

            return result;
        }

        /// <summary>
        /// Read a methodDef token from the signature and output the corresponding object to the builder.
        /// </summary>
        /// <param name="builder">Output string builder</param>
        private TMethod ParseMethodDefToken(TType owningTypeOverride)
        {
            uint rid = ReadUInt();
            return _provider.GetMethodFromMethodDef(_metadataReader, MetadataTokens.MethodDefinitionHandle((int)rid), owningTypeOverride);
        }


        /// <summary>
        /// Read a memberRef token from the signature and output the corresponding object to the builder.
        /// </summary>
        /// <param name="builder">Output string builder</param>
        /// <param name="owningTypeOverride">Explicit owning type override</param>
        private TMethod ParseMethodRefToken(TType owningTypeOverride)
        {
            uint rid = ReadUInt();
            return _provider.GetMethodFromMemberRef(_metadataReader, MetadataTokens.MemberReferenceHandle((int)rid), owningTypeOverride);
        }

    }
    public class TextSignatureDecoderContext
    {
        public TextSignatureDecoderContext(IAssemblyResolver assemblyResolver, SignatureFormattingOptions options)
        {
            AssemblyResolver = assemblyResolver;
            Options = options;
        }

        /// <summary>
        /// AssemblyResolver is used to find where the dependent assembly are
        /// </summary>
        public IAssemblyResolver AssemblyResolver { get; }

        /// <summary>
        /// SignatureFormattingOptions are used to specify details of signature formatting.
        /// </summary>
        public SignatureFormattingOptions Options { get; set;  }
    }

    /// <summary>
    /// Helper class used as state machine for decoding a single signature.
    /// </summary>
    public class SignatureDecoder : R2RSignatureDecoder<string, string, TextSignatureDecoderContext>
    {
        private class TextTypeProvider : StringTypeProviderBase<TextSignatureDecoderContext>, IR2RSignatureTypeProvider<string, string, TextSignatureDecoderContext>
        {
            private TextTypeProvider()
            {
            }

            public static readonly TextTypeProvider Singleton = new TextTypeProvider();

            public override string GetGenericMethodParameter(TextSignatureDecoderContext genericContext, int index)
            {
                return $"mvar #{index}";
            }

            public override string GetGenericTypeParameter(TextSignatureDecoderContext genericContext, int index)
            {
                return $"var #{index}";
            }

            public override string GetTypeFromSpecification(MetadataReader reader, TextSignatureDecoderContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                return MetadataNameFormatter.FormatHandle(reader, handle);
            }

            public string GetCanonType()
            {
                return "__Canon";
            }

            public string GetMethodFromMethodDef(MetadataReader reader, MethodDefinitionHandle handle, string owningTypeOverride)
            {
                uint methodDefToken = (uint)MetadataTokens.GetToken(handle);
                return MetadataNameFormatter.FormatHandle(
                    reader,
                    MetadataTokens.Handle((int)methodDefToken),
                    namespaceQualified: true,
                    owningTypeOverride: owningTypeOverride);
            }

            public string GetMethodFromMemberRef(MetadataReader reader, MemberReferenceHandle handle, string owningTypeOverride)
            {
                uint methodRefToken = (uint)MetadataTokens.GetToken(handle);
                return MetadataNameFormatter.FormatHandle(
                    reader,
                    MetadataTokens.Handle((int)methodRefToken),
                    namespaceQualified: true,
                    owningTypeOverride: owningTypeOverride);
            }

            public string GetInstantiatedMethod(string uninstantiatedMethod, ImmutableArray<string> instantiation)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(uninstantiatedMethod);
                builder.Append("<");
                for (int typeArgIndex = 0; typeArgIndex < instantiation.Length; typeArgIndex++)
                {
                    if (typeArgIndex != 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(instantiation[typeArgIndex]);
                }
                builder.Append(">");
                return builder.ToString();
            }

            public string GetConstrainedMethod(string method, string constraint)
            {
                return $"{method} @ {constraint}";
            }

            public string GetMethodWithFlags(ReadyToRunMethodSigFlags flags, string method)
            {
                StringBuilder builder = new StringBuilder();
                if ((flags & ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_UnboxingStub) != 0)
                {
                    builder.Append("[UNBOX] ");
                }
                if ((flags & ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_InstantiatingStub) != 0)
                {
                    builder.Append("[INST] ");
                }
                builder.Append(method);
                return builder.ToString();
            }
        }

        /// <summary>
        /// Construct the signature decoder by storing the image byte array and offset within the array.
        /// </summary>
        /// <param name="assemblyResolver">Assembly Resolver used to locate dependent assembly</param>
        /// <param name="options">SignatureFormattingOptions for signature formatting</param>
        /// <param name="r2rReader">R2RReader object representing the PE file containing the ECMA metadata</param>
        /// <param name="offset">Signature offset within the PE file byte array</param>
        public SignatureDecoder(IAssemblyResolver assemblyResolver, SignatureFormattingOptions options, MetadataReader metadataReader, ReadyToRunReader r2rReader, int offset) :
            base(TextTypeProvider.Singleton, new TextSignatureDecoderContext(assemblyResolver, options), metadataReader, r2rReader, offset)
        {
        }

        /// <summary>
        /// Construct the signature decoder by storing the image byte array and offset within the array.
        /// </summary>
        /// <param name="assemblyResolver">Assembly Resolver used to locate dependent assembly</param>
        /// <param name="options">SignatureFormattingOptions for signature formatting</param>
        /// <param name="metadataReader">Metadata reader for the R2R image</param>
        /// <param name="signature">Signature to parse</param>
        /// <param name="offset">Signature offset within the signature byte array</param>
        /// <param name="outerReader">Metadata reader representing the outer signature context</param>
        /// <param name="contextReader">Top-level signature context reader</param>
        private SignatureDecoder(IAssemblyResolver assemblyResolver, SignatureFormattingOptions options, MetadataReader metadataReader, byte[] signature, int offset, MetadataReader outerReader, ReadyToRunReader contextReader) :
            base(TextTypeProvider.Singleton, new TextSignatureDecoderContext(assemblyResolver, options), metadataReader, signature, offset, outerReader, contextReader)
        {
        }


        /// <summary>
        /// Decode a R2R import signature. The signature starts with the fixup type followed
        /// by custom encoding per fixup type.
        /// </summary>
        /// <returns></returns>
        internal ReadyToRunSignature ReadR2RSignature(StringBuilder builder)
        {
            int startOffset = Offset;
            ReadyToRunSignature result = ParseSignature(builder);
            EmitSignatureBinaryFrom(builder, startOffset);
            return result;
        }

        public string ReadTypeSignature()
        {
            StringBuilder builder = new StringBuilder();
            int startOffset = Offset;
            try
            {
                ParseType(builder);
                EmitSignatureBinaryFrom(builder, startOffset);
            }
            catch (Exception ex)
            {
                builder.Append(" - ");
                builder.Append(ex.Message);
            }
            return builder.ToString();
        }

        public string ReadTypeSignatureNoEmit()
        {
            StringBuilder builder = new StringBuilder();
            try
            {
                ParseType(builder);
            }
            catch (Exception ex)
            {
                builder.Append(" - ");
                builder.Append(ex.Message);
            }
            return builder.ToString();
        }

        private void EmitInlineSignatureBinaryForm(StringBuilder builder, int startOffset)
        {
            EmitInlineSignatureBinaryBytes(builder, Offset - startOffset);
        }

        private void EmitInlineSignatureBinaryBytes(StringBuilder builder, int count)
        {
            if (Context.Options.InlineSignatureBinary)
            {
                if (builder.Length > 0 && Char.IsDigit(builder[builder.Length - 1]))
                {
                    builder.Append('-');
                }

                for (int index = 0; index < count; index++)
                {
                    if (index != 0)
                    {
                        builder.Append('-');
                    }
                    builder.Append(_image[Offset - count + index].ToString("x2"));
                }
                builder.Append("-");
            }
        }

        private uint ReadUIntAndEmitInlineSignatureBinary(StringBuilder builder)
        {
            int startOffset = Offset;
            uint value = ReadUInt();
            EmitInlineSignatureBinaryForm(builder, startOffset);
            return value;
        }

        private void EmitSignatureBinaryFrom(StringBuilder builder, int startOffset)
        {
            if (Context.Options.SignatureBinary)
            {
                for (int offset = startOffset; offset < Offset; offset++)
                {
                    builder.Append(offset == startOffset ? " [" : "-");
                    builder.Append(_image[offset].ToString("x2"));
                }
                builder.Append("]");
            }
        }

        /// <summary>
        /// Parse the signature into a given output string builder.
        /// </summary>
        /// <param name="builder">Output signature builder</param>
        private ReadyToRunSignature ParseSignature(StringBuilder builder)
        {
            uint fixupType = ReadByte();
            EmitInlineSignatureBinaryBytes(builder, 1);
            bool moduleOverride = (fixupType & (byte)ReadyToRunFixupKind.ModuleOverride) != 0;
            SignatureDecoder moduleDecoder = this;

            // Check first byte for a module override being encoded. The metadata reader for the module
            // override is configured in the R2RSignatureDecoder constructor.
            if (moduleOverride)
            {
                fixupType &= ~(uint)ReadyToRunFixupKind.ModuleOverride;
                ReadUIntAndEmitInlineSignatureBinary(builder);
            }

            ReadyToRunSignature result = ParseSignature((ReadyToRunFixupKind)fixupType, builder);
            return result;
        }

        /// <summary>
        /// Parse the signature with a given fixup type after module overrides have been resolved.
        /// </summary>
        /// <param name="fixupType">Fixup type to parse</param>
        /// <param name="builder">Output signature builder</param>
        private ReadyToRunSignature ParseSignature(ReadyToRunFixupKind fixupType, StringBuilder builder)
        {
            ReadyToRunSignature result = new TodoSignature(this, fixupType);
            switch (fixupType)
            {
                case ReadyToRunFixupKind.ThisObjDictionaryLookup:
                    builder.Append("THISOBJ_DICTIONARY_LOOKUP @ ");
                    ParseType(builder);
                    builder.Append(": ");
                    // It looks like ReadyToRunSignature is potentially a composite pattern
                    ParseSignature(builder);
                    break;

                case ReadyToRunFixupKind.TypeDictionaryLookup:
                    builder.Append("TYPE_DICTIONARY_LOOKUP: ");
                    ParseSignature(builder);
                    break;

                case ReadyToRunFixupKind.MethodDictionaryLookup:
                    builder.Append("METHOD_DICTIONARY_LOOKUP: ");
                    ParseSignature(builder);
                    break;

                case ReadyToRunFixupKind.TypeHandle:
                    ParseType(builder);
                    builder.Append(" (TYPE_HANDLE)");
                    break;

                case ReadyToRunFixupKind.MethodHandle:
                    ParseMethod(builder);
                    builder.Append(" (METHOD_HANDLE)");
                    break;

                case ReadyToRunFixupKind.FieldHandle:
                    ParseField(builder);
                    builder.Append(" (FIELD_HANDLE)");
                    break;


                case ReadyToRunFixupKind.MethodEntry:
                    ParseMethod(builder);
                    builder.Append(" (METHOD_ENTRY)");
                    break;

                case ReadyToRunFixupKind.MethodEntry_DefToken:
                    uint methodDefToken = ParseMethodDefToken(builder, owningTypeOverride: null);
                    builder.Append(" (METHOD_ENTRY");
                    builder.Append(Context.Options.Naked ? ")" : "_DEF_TOKEN)");
                    result = new MethodDefEntrySignature(this) { MethodDefToken = methodDefToken };
                    break;

                case ReadyToRunFixupKind.MethodEntry_RefToken:
                    uint methodRefToken = ParseMethodRefToken(builder, owningTypeOverride: null);
                    builder.Append(" (METHOD_ENTRY");
                    builder.Append(Context.Options.Naked ? ")" : "_REF_TOKEN)");
                    result = new MethodRefEntrySignature(this) { MethodRefToken = methodRefToken };
                    break;


                case ReadyToRunFixupKind.VirtualEntry:
                    ParseMethod(builder);
                    builder.Append(" (VIRTUAL_ENTRY)");
                    break;

                case ReadyToRunFixupKind.VirtualEntry_DefToken:
                    ParseMethodDefToken(builder, owningTypeOverride: null);
                    builder.Append(" (VIRTUAL_ENTRY");
                    builder.Append(Context.Options.Naked ? ")" : "_DEF_TOKEN)");
                    break;

                case ReadyToRunFixupKind.VirtualEntry_RefToken:
                    ParseMethodRefToken(builder, owningTypeOverride: null);
                    builder.Append(" (VIRTUAL_ENTRY");
                    builder.Append(Context.Options.Naked ? ")" : "_REF_TOKEN)");
                    break;

                case ReadyToRunFixupKind.VirtualEntry_Slot:
                    {
                        uint slot = ReadUIntAndEmitInlineSignatureBinary(builder);
                        ParseType(builder);

                        builder.Append($@" #{slot} (VIRTUAL_ENTRY_SLOT)");
                    }
                    break;


                case ReadyToRunFixupKind.Helper:
                    ParseHelper(builder);
                    builder.Append(" (HELPER)");
                    break;

                case ReadyToRunFixupKind.StringHandle:
                    ParseStringHandle(builder);
                    builder.Append(" (STRING_HANDLE)");
                    break;


                case ReadyToRunFixupKind.NewObject:
                    ParseType(builder);
                    builder.Append(" (NEW_OBJECT)");
                    break;

                case ReadyToRunFixupKind.NewArray:
                    ParseType(builder);
                    builder.Append(" (NEW_ARRAY)");
                    break;


                case ReadyToRunFixupKind.IsInstanceOf:
                    ParseType(builder);
                    builder.Append(" (IS_INSTANCE_OF)");
                    break;

                case ReadyToRunFixupKind.ChkCast:
                    ParseType(builder);
                    builder.Append(" (CHK_CAST)");
                    break;


                case ReadyToRunFixupKind.FieldAddress:
                    ParseField(builder);
                    builder.Append(" (FIELD_ADDRESS)");
                    break;

                case ReadyToRunFixupKind.CctorTrigger:
                    ParseType(builder);
                    builder.Append(" (CCTOR_TRIGGER)");
                    break;


                case ReadyToRunFixupKind.StaticBaseNonGC:
                    ParseType(builder);
                    builder.Append(" (STATIC_BASE_NON_GC)");
                    break;

                case ReadyToRunFixupKind.StaticBaseGC:
                    ParseType(builder);
                    builder.Append(" (STATIC_BASE_GC)");
                    break;

                case ReadyToRunFixupKind.ThreadStaticBaseNonGC:
                    ParseType(builder);
                    builder.Append(" (THREAD_STATIC_BASE_NON_GC)");
                    break;

                case ReadyToRunFixupKind.ThreadStaticBaseGC:
                    ParseType(builder);
                    builder.Append(" (THREAD_STATIC_BASE_GC)");
                    break;


                case ReadyToRunFixupKind.FieldBaseOffset:
                    ParseType(builder);
                    builder.Append(" (FIELD_BASE_OFFSET)");
                    break;

                case ReadyToRunFixupKind.FieldOffset:
                    ParseField(builder);
                    builder.Append(" (FIELD_OFFSET)");
                    // TODO
                    break;


                case ReadyToRunFixupKind.TypeDictionary:
                    ParseType(builder);
                    builder.Append(" (TYPE_DICTIONARY)");
                    break;

                case ReadyToRunFixupKind.MethodDictionary:
                    ParseMethod(builder);
                    builder.Append(" (METHOD_DICTIONARY)");
                    break;


                case ReadyToRunFixupKind.Check_TypeLayout:
                case ReadyToRunFixupKind.Verify_TypeLayout:
                    ParseType(builder);
                    ReadyToRunTypeLayoutFlags layoutFlags = (ReadyToRunTypeLayoutFlags)ReadUInt();
                    builder.Append($" Flags {layoutFlags}");
                    int actualSize = (int)ReadUInt();
                    builder.Append($" Size {actualSize}");

                    if (layoutFlags.HasFlag(ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_HFA))
                    {
                        builder.Append($" HFAType {ReadUInt()}");
                    }

                    if (layoutFlags.HasFlag(ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_Alignment))
                    {
                        if (!layoutFlags.HasFlag(ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_Alignment_Native))
                        {
                            builder.Append($" Align {ReadUInt()}");
                        }
                    }

                    if (layoutFlags.HasFlag(ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_GCLayout))
                    {
                        if (!layoutFlags.HasFlag(ReadyToRunTypeLayoutFlags.READYTORUN_LAYOUT_GCLayout_Empty))
                        {
                            int cbGCRefMap = (actualSize / _contextReader.TargetPointerSize + 7) / 8;
                            builder.Append(" GCLayout ");
                            for (int i = 0; i < cbGCRefMap; i++)
                            {
                                builder.Append(ReadByte().ToString("X"));
                            }
                        }
                    }

                    if (fixupType == ReadyToRunFixupKind.Check_TypeLayout)
                        builder.Append(" (CHECK_TYPE_LAYOUT)");
                    else
                        builder.Append(" (VERIFY_TYPE_LAYOUT)");
                    break;

                case ReadyToRunFixupKind.Check_VirtualFunctionOverride:
                case ReadyToRunFixupKind.Verify_VirtualFunctionOverride:
                    ReadyToRunVirtualFunctionOverrideFlags flags = (ReadyToRunVirtualFunctionOverrideFlags)ReadUInt();
                    ParseMethod(builder);
                    builder.Append($" ImplType :");
                    ParseType(builder);
                    if (flags.HasFlag(ReadyToRunVirtualFunctionOverrideFlags.VirtualFunctionOverriden))
                    {
                        builder.Append($" ImplMethod :");
                        ParseMethod(builder);
                    }
                    else
                    {
                        builder.Append("Not Overriden");
                    }

                    if (fixupType == ReadyToRunFixupKind.Check_TypeLayout)
                        builder.Append(" (CHECK_VIRTUAL_FUNCTION_OVERRIDE)");
                    else
                        builder.Append(" (VERIFY_VIRTUAL_FUNCTION_OVERRIDE)");
                    break;

                case ReadyToRunFixupKind.Check_FieldOffset:
                    builder.Append($"{ReadUInt()} ");
                    ParseField(builder);
                    builder.Append(" (CHECK_FIELD_OFFSET)");
                    break;

                case ReadyToRunFixupKind.Verify_FieldOffset:
                    builder.Append($"{ReadUInt()} ");
                    builder.Append($"{ReadUInt()} ");
                    ParseField(builder);
                    builder.Append(" (VERIFY_FIELD_OFFSET)");
                    break;

                case ReadyToRunFixupKind.Check_InstructionSetSupport:
                    builder.Append("CHECK_InstructionSetSupport");
                    uint countOfInstructionSets = ReadUIntAndEmitInlineSignatureBinary(builder);
                    for (uint i = 0; i < countOfInstructionSets; i++)
                    {
                        uint instructionSetEncoded = ReadUIntAndEmitInlineSignatureBinary(builder);
                        ReadyToRunInstructionSet instructionSet = (ReadyToRunInstructionSet)(instructionSetEncoded >> 1);
                        bool supported = (instructionSetEncoded & 1) == 1;
                        builder.Append($" {instructionSet}{(supported ? "+" : "-")}");
                    }
                    break;

                case ReadyToRunFixupKind.DelegateCtor:
                    ParseMethod(builder);
                    builder.Append(" => ");
                    ParseType(builder);
                    builder.Append(" (DELEGATE_CTOR)");
                    break;

                case ReadyToRunFixupKind.DeclaringTypeHandle:
                    ParseType(builder);
                    builder.Append(" (DECLARING_TYPE_HANDLE)");
                    break;

                case ReadyToRunFixupKind.IndirectPInvokeTarget:
                    ParseMethod(builder);
                    builder.Append(" (INDIRECT_PINVOKE_TARGET)");
                    break;

                case ReadyToRunFixupKind.PInvokeTarget:
                    ParseMethod(builder);
                    builder.Append(" (PINVOKE_TARGET)");
                    break;

                default:
                    throw new BadImageFormatException();
            }
            return result;
        }

        /// <summary>
        /// Decode a type from the signature stream.
        /// </summary>
        /// <param name="builder"></param>
        private void ParseType(StringBuilder builder)
        {
            builder.Append(base.ParseType());
        }

        public IAssemblyMetadata GetMetadataReaderFromModuleOverride()
        {
            if (PeekElementType() == CorElementType.ELEMENT_TYPE_MODULE_ZAPSIG)
            {
                var currentOffset = Offset;

                ReadElementType();
                int moduleIndex = (int)ReadUInt();
                IAssemblyMetadata refAsmReader = _contextReader.OpenReferenceAssembly(moduleIndex);

                UpdateOffset(currentOffset);

                return refAsmReader;
            }
            return null;
        }

        /// <summary>
        /// Parse an arbitrary method signature.
        /// </summary>
        /// <param name="builder">Output string builder to receive the textual signature representation</param>
        private void ParseMethod(StringBuilder builder)
        {
            builder.Append(ParseMethod());
        }

        /// <summary>
        /// Read a methodDef token from the signature and output the corresponding object to the builder.
        /// </summary>
        /// <param name="builder">Output string builder</param>
        private uint ParseMethodDefToken(StringBuilder builder, string owningTypeOverride)
        {
            StringBuilder signaturePrefixBuilder = new StringBuilder();
            uint methodDefToken = ReadUIntAndEmitInlineSignatureBinary(signaturePrefixBuilder) | (uint)CorTokenType.mdtMethodDef;
            builder.Append(MetadataNameFormatter.FormatHandle(
                _metadataReader,
                MetadataTokens.Handle((int)methodDefToken),
                namespaceQualified: true,
                owningTypeOverride: owningTypeOverride,
                signaturePrefix: signaturePrefixBuilder.ToString()));
            return methodDefToken;
        }

        /// <summary>
        /// Read a memberRef token from the signature and output the corresponding object to the builder.
        /// </summary>
        /// <param name="builder">Output string builder</param>
        /// <param name="owningTypeOverride">Explicit owning type override</param>
        private uint ParseMethodRefToken(StringBuilder builder, string owningTypeOverride)
        {
            StringBuilder signaturePrefixBuilder = new StringBuilder();
            uint methodRefToken = ReadUIntAndEmitInlineSignatureBinary(signaturePrefixBuilder) | (uint)CorTokenType.mdtMemberRef;
            builder.Append(MetadataNameFormatter.FormatHandle(
                _metadataReader,
                MetadataTokens.Handle((int)methodRefToken),
                namespaceQualified: false,
                owningTypeOverride: owningTypeOverride,
                signaturePrefix: signaturePrefixBuilder.ToString()));
            return methodRefToken;
        }

        /// <summary>
        /// Parse field signature and output its textual representation into the given string builder.
        /// </summary>
        /// <param name="builder">Output string builder</param>
        private void ParseField(StringBuilder builder)
        {
            uint flags = ReadUIntAndEmitInlineSignatureBinary(builder);
            string owningTypeOverride = null;
            if ((flags & (uint)ReadyToRunFieldSigFlags.READYTORUN_FIELD_SIG_OwnerType) != 0)
            {
                StringBuilder owningTypeBuilder = new StringBuilder();
                ParseType(owningTypeBuilder);
                owningTypeOverride = owningTypeBuilder.ToString();
            }
            StringBuilder signaturePrefixBuilder = new StringBuilder();
            uint fieldToken;
            if ((flags & (uint)ReadyToRunFieldSigFlags.READYTORUN_FIELD_SIG_MemberRefToken) != 0)
            {
                fieldToken = ReadUIntAndEmitInlineSignatureBinary(signaturePrefixBuilder) | (uint)CorTokenType.mdtMemberRef;
            }
            else
            {
                fieldToken = ReadUIntAndEmitInlineSignatureBinary(signaturePrefixBuilder) | (uint)CorTokenType.mdtFieldDef;
            }
            builder.Append(MetadataNameFormatter.FormatHandle(
                _metadataReader,
                MetadataTokens.Handle((int)fieldToken),
                namespaceQualified: false,
                owningTypeOverride: owningTypeOverride,
                signaturePrefix: signaturePrefixBuilder.ToString()));
        }

        /// <summary>
        /// Read R2R helper signature.
        /// </summary>
        /// <returns></returns>
        private void ParseHelper(StringBuilder builder)
        {
            uint helperType = ReadUIntAndEmitInlineSignatureBinary(builder);

            switch ((ReadyToRunHelper)helperType)
            {
                case ReadyToRunHelper.Invalid:
                    builder.Append("INVALID");
                    break;

                // Not a real helper - handle to current module passed to delay load helpers.
                case ReadyToRunHelper.Module:
                    builder.Append("MODULE");
                    break;

                case ReadyToRunHelper.GSCookie:
                    builder.Append("GC_COOKIE");
                    break;

                case ReadyToRunHelper.IndirectTrapThreads:
                    builder.Append("INDIRECT_TRAP_THREADS");
                    break;

                //
                // Delay load helpers
                //

                // All delay load helpers use custom calling convention:
                // - scratch register - address of indirection cell. 0 = address is inferred from callsite.
                // - stack - section index, module handle
                case ReadyToRunHelper.DelayLoad_MethodCall:
                    builder.Append("DELAYLOAD_METHODCALL");
                    break;

                case ReadyToRunHelper.DelayLoad_Helper:
                    builder.Append("DELAYLOAD_HELPER");
                    break;

                case ReadyToRunHelper.DelayLoad_Helper_Obj:
                    builder.Append("DELAYLOAD_HELPER_OBJ");
                    break;

                case ReadyToRunHelper.DelayLoad_Helper_ObjObj:
                    builder.Append("DELAYLOAD_HELPER_OBJ_OBJ");
                    break;

                // JIT helpers

                // Exception handling helpers
                case ReadyToRunHelper.Throw:
                    builder.Append("THROW");
                    break;

                case ReadyToRunHelper.Rethrow:
                    builder.Append("RETHROW");
                    break;

                case ReadyToRunHelper.Overflow:
                    builder.Append("OVERFLOW");
                    break;

                case ReadyToRunHelper.RngChkFail:
                    builder.Append("RNG_CHK_FAIL");
                    break;

                case ReadyToRunHelper.FailFast:
                    builder.Append("FAIL_FAST");
                    break;

                case ReadyToRunHelper.ThrowNullRef:
                    builder.Append("THROW_NULL_REF");
                    break;

                case ReadyToRunHelper.ThrowDivZero:
                    builder.Append("THROW_DIV_ZERO");
                    break;

                // Write barriers
                case ReadyToRunHelper.WriteBarrier:
                    builder.Append("WRITE_BARRIER");
                    break;

                case ReadyToRunHelper.CheckedWriteBarrier:
                    builder.Append("CHECKED_WRITE_BARRIER");
                    break;

                case ReadyToRunHelper.ByRefWriteBarrier:
                    builder.Append("BYREF_WRITE_BARRIER");
                    break;

                // Array helpers
                case ReadyToRunHelper.Stelem_Ref:
                    builder.Append("STELEM_REF");
                    break;

                case ReadyToRunHelper.Ldelema_Ref:
                    builder.Append("LDELEMA_REF");
                    break;

                case ReadyToRunHelper.MemSet:
                    builder.Append("MEM_SET");
                    break;

                case ReadyToRunHelper.MemCpy:
                    builder.Append("MEM_CPY");
                    break;

                // PInvoke helpers
                case ReadyToRunHelper.PInvokeBegin:
                    builder.Append("PINVOKE_BEGIN");
                    break;

                case ReadyToRunHelper.PInvokeEnd:
                    builder.Append("PINVOKE_END");
                    break;

                case ReadyToRunHelper.GCPoll:
                    builder.Append("GCPOLL");
                    break;

                case ReadyToRunHelper.GetCurrentManagedThreadId:
                    builder.Append("GET_CURRENT_MANAGED_THREAD_ID");
                    break;

                case ReadyToRunHelper.ReversePInvokeEnter:
                    builder.Append("REVERSE_PINVOKE_ENTER");
                    break;

                case ReadyToRunHelper.ReversePInvokeExit:
                    builder.Append("REVERSE_PINVOKE_EXIT");
                    break;

                // Get string handle lazily
                case ReadyToRunHelper.GetString:
                    builder.Append("GET_STRING");
                    break;

                // Used by /Tuning for Profile optimizations
                case ReadyToRunHelper.LogMethodEnter:
                    builder.Append("LOG_METHOD_ENTER");
                    break;

                // Reflection helpers
                case ReadyToRunHelper.GetRuntimeTypeHandle:
                    builder.Append("GET_RUNTIME_TYPE_HANDLE");
                    break;

                case ReadyToRunHelper.GetRuntimeMethodHandle:
                    builder.Append("GET_RUNTIME_METHOD_HANDLE");
                    break;

                case ReadyToRunHelper.GetRuntimeFieldHandle:
                    builder.Append("GET_RUNTIME_FIELD_HANDLE");
                    break;

                case ReadyToRunHelper.Box:
                    builder.Append("BOX");
                    break;

                case ReadyToRunHelper.Box_Nullable:
                    builder.Append("BOX_NULLABLE");
                    break;

                case ReadyToRunHelper.Unbox:
                    builder.Append("UNBOX");
                    break;

                case ReadyToRunHelper.Unbox_Nullable:
                    builder.Append("UNBOX_NULLABLE");
                    break;

                case ReadyToRunHelper.NewMultiDimArr:
                    builder.Append("NEW_MULTI_DIM_ARR");
                    break;

                case ReadyToRunHelper.MonitorEnter:
                    builder.Append("MONITOR_ENTER");
                    break;

                case ReadyToRunHelper.MonitorExit:
                    builder.Append("MONITOR_EXIT");
                    break;

                // Helpers used with generic handle lookup cases
                case ReadyToRunHelper.NewObject:
                    builder.Append("NEW_OBJECT");
                    break;

                case ReadyToRunHelper.NewArray:
                    builder.Append("NEW_ARRAY");
                    break;

                case ReadyToRunHelper.CheckCastAny:
                    builder.Append("CHECK_CAST_ANY");
                    break;

                case ReadyToRunHelper.CheckInstanceAny:
                    builder.Append("CHECK_INSTANCE_ANY");
                    break;

                case ReadyToRunHelper.GenericGcStaticBase:
                    builder.Append("GENERIC_GC_STATIC_BASE");
                    break;

                case ReadyToRunHelper.GenericNonGcStaticBase:
                    builder.Append("GENERIC_NON_GC_STATIC_BASE");
                    break;

                case ReadyToRunHelper.GenericGcTlsBase:
                    builder.Append("GENERIC_GC_TLS_BASE");
                    break;

                case ReadyToRunHelper.GenericNonGcTlsBase:
                    builder.Append("GENERIC_NON_GC_TLS_BASE");
                    break;

                case ReadyToRunHelper.VirtualFuncPtr:
                    builder.Append("VIRTUAL_FUNC_PTR");
                    break;

                // Long mul/div/shift ops
                case ReadyToRunHelper.LMul:
                    builder.Append("LMUL");
                    break;

                case ReadyToRunHelper.LMulOfv:
                    builder.Append("LMUL_OFV");
                    break;

                case ReadyToRunHelper.ULMulOvf:
                    builder.Append("ULMUL_OVF");
                    break;

                case ReadyToRunHelper.LDiv:
                    builder.Append("LDIV");
                    break;

                case ReadyToRunHelper.LMod:
                    builder.Append("LMOD");
                    break;

                case ReadyToRunHelper.ULDiv:
                    builder.Append("ULDIV");
                    break;

                case ReadyToRunHelper.ULMod:
                    builder.Append("ULMOD");
                    break;

                case ReadyToRunHelper.LLsh:
                    builder.Append("LLSH");
                    break;

                case ReadyToRunHelper.LRsh:
                    builder.Append("LRSH");
                    break;

                case ReadyToRunHelper.LRsz:
                    builder.Append("LRSZ");
                    break;

                case ReadyToRunHelper.Lng2Dbl:
                    builder.Append("LNG2DBL");
                    break;

                case ReadyToRunHelper.ULng2Dbl:
                    builder.Append("ULNG2DBL");
                    break;

                // 32-bit division helpers
                case ReadyToRunHelper.Div:
                    builder.Append("DIV");
                    break;

                case ReadyToRunHelper.Mod:
                    builder.Append("MOD");
                    break;

                case ReadyToRunHelper.UDiv:
                    builder.Append("UDIV");
                    break;

                case ReadyToRunHelper.UMod:
                    builder.Append("UMOD");
                    break;

                // Floating point conversions
                case ReadyToRunHelper.Dbl2Int:
                    builder.Append("DBL2INT");
                    break;

                case ReadyToRunHelper.Dbl2IntOvf:
                    builder.Append("DBL2INTOVF");
                    break;

                case ReadyToRunHelper.Dbl2Lng:
                    builder.Append("DBL2LNG");
                    break;

                case ReadyToRunHelper.Dbl2LngOvf:
                    builder.Append("DBL2LNGOVF");
                    break;

                case ReadyToRunHelper.Dbl2UInt:
                    builder.Append("DBL2UINT");
                    break;

                case ReadyToRunHelper.Dbl2UIntOvf:
                    builder.Append("DBL2UINTOVF");
                    break;

                case ReadyToRunHelper.Dbl2ULng:
                    builder.Append("DBL2ULNG");
                    break;

                case ReadyToRunHelper.Dbl2ULngOvf:
                    builder.Append("DBL2ULNGOVF");
                    break;

                // Floating point ops
                case ReadyToRunHelper.DblRem:
                    builder.Append("DBL_REM");
                    break;
                case ReadyToRunHelper.FltRem:
                    builder.Append("FLT_REM");
                    break;
                case ReadyToRunHelper.DblRound:
                    builder.Append("DBL_ROUND");
                    break;
                case ReadyToRunHelper.FltRound:
                    builder.Append("FLT_ROUND");
                    break;

                // Personality rountines
                case ReadyToRunHelper.PersonalityRoutine:
                    builder.Append("PERSONALITY_ROUTINE");
                    break;
                case ReadyToRunHelper.PersonalityRoutineFilterFunclet:
                    builder.Append("PERSONALITY_ROUTINE_FILTER_FUNCLET");
                    break;

                //
                // Deprecated/legacy
                //

                // JIT32 x86-specific write barriers
                case ReadyToRunHelper.WriteBarrier_EAX:
                    builder.Append("WRITE_BARRIER_EAX");
                    break;
                case ReadyToRunHelper.WriteBarrier_EBX:
                    builder.Append("WRITE_BARRIER_EBX");
                    break;
                case ReadyToRunHelper.WriteBarrier_ECX:
                    builder.Append("WRITE_BARRIER_ECX");
                    break;
                case ReadyToRunHelper.WriteBarrier_ESI:
                    builder.Append("WRITE_BARRIER_ESI");
                    break;
                case ReadyToRunHelper.WriteBarrier_EDI:
                    builder.Append("WRITE_BARRIER_EDI");
                    break;
                case ReadyToRunHelper.WriteBarrier_EBP:
                    builder.Append("WRITE_BARRIER_EBP");
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EAX:
                    builder.Append("CHECKED_WRITE_BARRIER_EAX");
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EBX:
                    builder.Append("CHECKED_WRITE_BARRIER_EBX");
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_ECX:
                    builder.Append("CHECKED_WRITE_BARRIER_ECX");
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_ESI:
                    builder.Append("CHECKED_WRITE_BARRIER_ESI");
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EDI:
                    builder.Append("CHECKED_WRITE_BARRIER_EDI");
                    break;
                case ReadyToRunHelper.CheckedWriteBarrier_EBP:
                    builder.Append("CHECKED_WRITE_BARRIER_EBP");
                    break;

                // JIT32 x86-specific exception handling
                case ReadyToRunHelper.EndCatch:
                    builder.Append("END_CATCH");
                    break;

                case ReadyToRunHelper.StackProbe:
                    builder.Append("STACK_PROBE");
                    break;

                default:
                    throw new BadImageFormatException();
            }
        }

        /// <summary>
        /// Read a string token from the signature stream and convert it to the actual string.
        /// </summary>
        private void ParseStringHandle(StringBuilder builder)
        {
            uint rid = ReadUIntAndEmitInlineSignatureBinary(builder);
            UserStringHandle stringHandle = MetadataTokens.UserStringHandle((int)rid);
            builder.AppendEscapedString(_metadataReader.GetUserString(stringHandle));
        }
    }
}
