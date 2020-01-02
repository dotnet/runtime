﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using Internal.CorConstants;
using Internal.ReadyToRunConstants;

namespace ILCompiler.Reflection.ReadyToRun
{
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

        public static string FormatSignature(IAssemblyResolver assemblyResolver, R2RReader r2rReader, int imageOffset)
        {
            SignatureDecoder decoder = new SignatureDecoder(assemblyResolver, r2rReader, imageOffset);
            string result = decoder.ReadR2RSignature();
            return result;
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

    /// <summary>
    /// Helper class used as state machine for decoding a single signature.
    /// </summary>
    public class SignatureDecoder
    {
        /// <summary>
        /// ECMA reader is used to access the embedded MSIL metadata blob in the R2R file.
        /// </summary>
        private readonly MetadataReader _metadataReader;

        /// <summary>
        /// ECMA reader representing the top-level signature context.
        /// </summary>
        private readonly R2RReader _contextReader;

        /// <summary>
        /// Dump options are used to specify details of signature formatting.
        /// </summary>
        private readonly IAssemblyResolver _options;

        /// <summary>
        /// Byte array representing the R2R PE file read from disk.
        /// </summary>
        private readonly byte[] _image;

        /// <summary>
        /// Offset within the image file.
        /// </summary>
        private int _offset;

        /// <summary>
        /// Query signature parser for the current offset.
        /// </summary>
        public int Offset => _offset;

        /// <summary>
        /// Construct the signature decoder by storing the image byte array and offset within the array.
        /// </summary>
        /// <param name="options">Dump options and paths</param>
        /// <param name="r2rReader">R2RReader object representing the PE file containing the ECMA metadata</param>
        /// <param name="offset">Signature offset within the PE file byte array</param>
        public SignatureDecoder(IAssemblyResolver options, R2RReader r2rReader, int offset)
        {
            _metadataReader = r2rReader.MetadataReader;
            _options = options;
            _image = r2rReader.Image;
            _offset = offset;
            _contextReader = r2rReader;
        }

        /// <summary>
        /// Construct the signature decoder by storing the image byte array and offset within the array.
        /// </summary>
        /// <param name="options">Dump options and paths</param>
        /// <param name="metadataReader">Metadata reader for the R2R image</param>
        /// <param name="signature">Signature to parse</param>
        /// <param name="offset">Signature offset within the signature byte array</param>
        /// <param name="contextReader">Top-level signature context reader</param>
        private SignatureDecoder(IAssemblyResolver options, MetadataReader metadataReader, byte[] signature, int offset, R2RReader contextReader)
        {
            _metadataReader = metadataReader;
            _options = options;
            _image = signature;
            _offset = offset;
            _contextReader = contextReader;
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
        /// Decode a R2R import signature. The signature starts with the fixup type followed
        /// by custom encoding per fixup type.
        /// </summary>
        /// <returns></returns>
        public string ReadR2RSignature()
        {
            StringBuilder builder = new StringBuilder();
            int startOffset = _offset;
            try
            {
                ParseSignature(builder);
                EmitSignatureBinaryFrom(builder, startOffset);
            }
            catch (Exception ex)
            {
                builder.Append(" - ");
                builder.Append(ex.Message);
            }
            return builder.ToString();
        }

        public string ReadTypeSignature()
        {
            StringBuilder builder = new StringBuilder();
            int startOffset = _offset;
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
            EmitInlineSignatureBinaryBytes(builder, _offset - startOffset);
        }

        private void EmitInlineSignatureBinaryBytes(StringBuilder builder, int count)
        {
            if (_options.InlineSignatureBinary)
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
                    builder.Append(_image[_offset - count + index].ToString("x2"));
                }
                builder.Append("-");
            }
        }

        private uint ReadUIntAndEmitInlineSignatureBinary(StringBuilder builder)
        {
            int startOffset = _offset;
            uint value = ReadUInt();
            EmitInlineSignatureBinaryForm(builder, startOffset);
            return value;
        }

        private int ReadIntAndEmitInlineSignatureBinary(StringBuilder builder)
        {
            int startOffset = _offset;
            int value = ReadInt();
            EmitInlineSignatureBinaryForm(builder, startOffset);
            return value;
        }

        private uint ReadTokenAndEmitInlineSignatureBinary(StringBuilder builder)
        {
            int startOffset = _offset;
            uint value = ReadToken();
            EmitInlineSignatureBinaryForm(builder, startOffset);
            return value;
        }

        private void EmitSignatureBinaryFrom(StringBuilder builder, int startOffset)
        {
            if (_options.SignatureBinary)
            {
                for (int offset = startOffset; offset < _offset; offset++)
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
        private void ParseSignature(StringBuilder builder)
        {
            uint fixupType = ReadByte();
            EmitInlineSignatureBinaryBytes(builder, 1);
            bool moduleOverride = (fixupType & (byte)ReadyToRunFixupKind.ModuleOverride) != 0;
            SignatureDecoder moduleDecoder = this;

            // Check first byte for a module override being encoded
            if (moduleOverride)
            {
                fixupType &= ~(uint)ReadyToRunFixupKind.ModuleOverride;
                int moduleIndex = (int)ReadUIntAndEmitInlineSignatureBinary(builder);
                MetadataReader refAsmEcmaReader = _contextReader.OpenReferenceAssembly(moduleIndex);
                moduleDecoder = new SignatureDecoder(_options, refAsmEcmaReader, _image, _offset, _contextReader);
            }

            moduleDecoder.ParseSignature((ReadyToRunFixupKind)fixupType, builder);
            _offset = moduleDecoder.Offset;
        }

        /// <summary>
        /// Parse the signature with a given fixup type after module overrides have been resolved.
        /// </summary>
        /// <param name="fixupType">Fixup type to parse</param>
        /// <param name="builder">Output signature builder</param>
        private void ParseSignature(ReadyToRunFixupKind fixupType, StringBuilder builder)
        {
            switch (fixupType)
            {
                case ReadyToRunFixupKind.ThisObjDictionaryLookup:
                    builder.Append("THISOBJ_DICTIONARY_LOOKUP @ ");
                    ParseType(builder);
                    builder.Append(": ");
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
                    ParseMethodDefToken(builder, owningTypeOverride: null);
                    builder.Append(" (METHOD_ENTRY");
                    builder.Append(_options.Naked ? ")" : "_DEF_TOKEN)");
                    break;

                case ReadyToRunFixupKind.MethodEntry_RefToken:
                    ParseMethodRefToken(builder, owningTypeOverride: null);
                    builder.Append(" (METHOD_ENTRY");
                    builder.Append(_options.Naked ? ")" : "_REF_TOKEN)");
                    break;


                case ReadyToRunFixupKind.VirtualEntry:
                    ParseMethod(builder);
                    builder.Append(" (VIRTUAL_ENTRY)");
                    break;

                case ReadyToRunFixupKind.VirtualEntry_DefToken:
                    ParseMethodDefToken(builder, owningTypeOverride: null);
                    builder.Append(" (VIRTUAL_ENTRY");
                    builder.Append(_options.Naked ? ")" : "_DEF_TOKEN)");
                    break;

                case ReadyToRunFixupKind.VirtualEntry_RefToken:
                    ParseMethodRefToken(builder, owningTypeOverride: null);
                    builder.Append(" (VIRTUAL_ENTRY");
                    builder.Append(_options.Naked ? ")" : "_REF_TOKEN)");
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
                    ParseType(builder);
                    builder.Append(" (CHECK_TYPE_LAYOUT)");
                    break;

                case ReadyToRunFixupKind.Check_FieldOffset:
                    builder.Append("CHECK_FIELD_OFFSET");
                    // TODO
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
                    builder.Append(string.Format("Unknown fixup type: {0:X2}", fixupType));
                    break;
            }
        }

        /// <summary>
        /// Decode a type from the signature stream.
        /// </summary>
        /// <param name="builder"></param>
        private void ParseType(StringBuilder builder)
        {
            CorElementType corElemType = ReadElementType();
            EmitInlineSignatureBinaryBytes(builder, 1);
            switch (corElemType)
            {
                case CorElementType.ELEMENT_TYPE_VOID:
                    builder.Append("void");
                    break;

                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    builder.Append("bool");
                    break;

                case CorElementType.ELEMENT_TYPE_CHAR:
                    builder.Append("char");
                    break;

                case CorElementType.ELEMENT_TYPE_I1:
                    builder.Append("sbyte");
                    break;

                case CorElementType.ELEMENT_TYPE_U1:
                    builder.Append("byte");
                    break;

                case CorElementType.ELEMENT_TYPE_I2:
                    builder.Append("short");
                    break;

                case CorElementType.ELEMENT_TYPE_U2:
                    builder.Append("ushort");
                    break;

                case CorElementType.ELEMENT_TYPE_I4:
                    builder.Append("int");
                    break;

                case CorElementType.ELEMENT_TYPE_U4:
                    builder.Append("uint");
                    break;

                case CorElementType.ELEMENT_TYPE_I8:
                    builder.Append("long");
                    break;

                case CorElementType.ELEMENT_TYPE_U8:
                    builder.Append("ulong");
                    break;

                case CorElementType.ELEMENT_TYPE_R4:
                    builder.Append("float");
                    break;

                case CorElementType.ELEMENT_TYPE_R8:
                    builder.Append("double");
                    break;

                case CorElementType.ELEMENT_TYPE_STRING:
                    builder.Append("string");
                    break;

                case CorElementType.ELEMENT_TYPE_PTR:
                    ParseType(builder);
                    builder.Append('*');
                    break;

                case CorElementType.ELEMENT_TYPE_BYREF:
                    builder.Append("byref");
                    break;

                case CorElementType.ELEMENT_TYPE_VALUETYPE:
                case CorElementType.ELEMENT_TYPE_CLASS:
                    ParseTypeToken(builder);
                    break;

                case CorElementType.ELEMENT_TYPE_VAR:
                    uint varIndex = ReadUIntAndEmitInlineSignatureBinary(builder);
                    builder.Append("var #");
                    builder.Append(varIndex);
                    break;

                case CorElementType.ELEMENT_TYPE_ARRAY:
                    ParseType(builder);
                    {
                        builder.Append('[');
                        int startOffset = _offset;
                        uint rank = ReadUIntAndEmitInlineSignatureBinary(builder);
                        if (rank != 0)
                        {
                            uint sizeCount = ReadUIntAndEmitInlineSignatureBinary(builder); // number of sizes
                            uint[] sizes = new uint[sizeCount];
                            for (uint sizeIndex = 0; sizeIndex < sizeCount; sizeIndex++)
                            {
                                sizes[sizeIndex] = ReadUIntAndEmitInlineSignatureBinary(builder);
                            }
                            uint lowerBoundCount = ReadUIntAndEmitInlineSignatureBinary(builder); // number of lower bounds
                            int[] lowerBounds = new int[lowerBoundCount];
                            for (uint lowerBoundIndex = 0; lowerBoundIndex < lowerBoundCount; lowerBoundIndex++)
                            {
                                lowerBounds[lowerBoundIndex] = ReadIntAndEmitInlineSignatureBinary(builder);
                            }
                            for (int index = 0; index < rank; index++)
                            {
                                if (index > 0)
                                {
                                    builder.Append(',');
                                }
                                if (lowerBoundCount > index && lowerBounds[index] != 0)
                                {
                                    builder.Append(lowerBounds[index]);
                                    builder.Append("..");
                                    if (sizeCount > index)
                                    {
                                        builder.Append(lowerBounds[index] + sizes[index] - 1);
                                    }
                                }
                                else if (sizeCount > index)
                                {
                                    builder.Append(sizes[index]);
                                }
                                else if (rank == 1)
                                {
                                    builder.Append('*');
                                }
                            }
                        }
                        builder.Append(']');
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_GENERICINST:
                    ParseGenericTypeInstance(builder);
                    break;

                case CorElementType.ELEMENT_TYPE_TYPEDBYREF:
                    builder.Append("typedbyref");
                    break;

                case CorElementType.ELEMENT_TYPE_I:
                    builder.Append("IntPtr");
                    break;

                case CorElementType.ELEMENT_TYPE_U:
                    builder.Append("UIntPtr");
                    break;

                case CorElementType.ELEMENT_TYPE_FNPTR:
                    builder.Append("fnptr");
                    break;

                case CorElementType.ELEMENT_TYPE_OBJECT:
                    builder.Append("object");
                    break;

                case CorElementType.ELEMENT_TYPE_SZARRAY:
                    ParseType(builder);
                    builder.Append("[]");
                    break;

                case CorElementType.ELEMENT_TYPE_MVAR:
                    uint mvarIndex = ReadUIntAndEmitInlineSignatureBinary(builder);
                    builder.Append("mvar #");
                    builder.Append(mvarIndex);
                    break;

                case CorElementType.ELEMENT_TYPE_CMOD_REQD:
                    builder.Append("cmod_reqd");
                    break;

                case CorElementType.ELEMENT_TYPE_CMOD_OPT:
                    builder.Append("cmod_opt");
                    break;

                case CorElementType.ELEMENT_TYPE_HANDLE:
                    builder.Append("handle");
                    break;

                case CorElementType.ELEMENT_TYPE_SENTINEL:
                    builder.Append("sentinel");
                    break;

                case CorElementType.ELEMENT_TYPE_PINNED:
                    builder.Append("pinned");
                    break;

                case CorElementType.ELEMENT_TYPE_VAR_ZAPSIG:
                    builder.Append("var_zapsig");
                    break;

                case CorElementType.ELEMENT_TYPE_NATIVE_ARRAY_TEMPLATE_ZAPSIG:
                    builder.Append("native_array_template_zapsig");
                    break;

                case CorElementType.ELEMENT_TYPE_NATIVE_VALUETYPE_ZAPSIG:
                    builder.Append("native_valuetype_zapsig");
                    break;

                case CorElementType.ELEMENT_TYPE_CANON_ZAPSIG:
                    builder.Append("__Canon");
                    break;

                case CorElementType.ELEMENT_TYPE_MODULE_ZAPSIG:
                    {
                        int moduleIndex = (int)ReadUIntAndEmitInlineSignatureBinary(builder);
                        MetadataReader refAsmReader = _contextReader.OpenReferenceAssembly(moduleIndex);
                        SignatureDecoder refAsmDecoder = new SignatureDecoder(_options, refAsmReader, _image, _offset, _contextReader);
                        refAsmDecoder.ParseType(builder);
                        _offset = refAsmDecoder.Offset;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public MetadataReader GetMetadataReaderFromModuleOverride()
        {
            if (PeekElementType() == CorElementType.ELEMENT_TYPE_MODULE_ZAPSIG)
            {
                var currentOffset = _offset;

                ReadElementType();
                int moduleIndex = (int)ReadUInt();
                MetadataReader refAsmReader = _contextReader.OpenReferenceAssembly(moduleIndex);

                _offset = currentOffset;

                return refAsmReader;
            }
            return null;
        }

        private void ParseGenericTypeInstance(StringBuilder builder)
        {
            ParseType(builder);
            uint typeArgCount = ReadUIntAndEmitInlineSignatureBinary(builder);
            builder.Append("<");
            for (uint paramIndex = 0; paramIndex < typeArgCount; paramIndex++)
            {
                if (paramIndex > 0)
                {
                    builder.Append(", ");
                }
                ParseType(builder);
            }
            builder.Append(">");
        }

        private void ParseTypeToken(StringBuilder builder)
        {
            StringBuilder signaturePrefixBuilder = new StringBuilder();
            uint token = ReadTokenAndEmitInlineSignatureBinary(signaturePrefixBuilder);
            builder.Append(MetadataNameFormatter.FormatHandle(
                _metadataReader,
                MetadataTokens.Handle((int)token),
                owningTypeOverride: null,
                signaturePrefix: signaturePrefixBuilder.ToString()));
        }

        /// <summary>
        /// Parse an arbitrary method signature.
        /// </summary>
        /// <param name="builder">Output string builder to receive the textual signature representation</param>
        private void ParseMethod(StringBuilder builder)
        {
            uint methodFlags = ReadUIntAndEmitInlineSignatureBinary(builder);

            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_UnboxingStub) != 0)
            {
                builder.Append("[UNBOX] ");
            }
            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_InstantiatingStub) != 0)
            {
                builder.Append("[INST] ");
            }

            string owningTypeOverride = null;
            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_OwnerType) != 0)
            {
                SignatureDecoder owningTypeDecoder = new SignatureDecoder(_options, _metadataReader, _image, _offset, _contextReader);
                owningTypeOverride = owningTypeDecoder.ReadTypeSignatureNoEmit();
                _offset = owningTypeDecoder._offset;
            }
            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_SlotInsteadOfToken) != 0)
            {
                throw new NotImplementedException();
            }
            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MemberRefToken) != 0)
            {
                ParseMethodRefToken(builder, owningTypeOverride: owningTypeOverride);
            }
            else
            {
                ParseMethodDefToken(builder, owningTypeOverride: owningTypeOverride);
            }

            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MethodInstantiation) != 0)
            {
                uint typeArgCount = ReadUIntAndEmitInlineSignatureBinary(builder);
                builder.Append("<");
                for (int typeArgIndex = 0; typeArgIndex < typeArgCount; typeArgIndex++)
                {
                    if (typeArgIndex != 0)
                    {
                        builder.Append(", ");
                    }
                    ParseType(builder);
                }
                builder.Append(">");
            }

            if ((methodFlags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_Constrained) != 0)
            {
                builder.Append(" @ ");
                ParseType(builder);
            }
        }

        /// <summary>
        /// Read a methodDef token from the signature and output the corresponding object to the builder.
        /// </summary>
        /// <param name="builder">Output string builder</param>
        private void ParseMethodDefToken(StringBuilder builder, string owningTypeOverride)
        {
            StringBuilder signaturePrefixBuilder = new StringBuilder();
            uint methodDefToken = ReadUIntAndEmitInlineSignatureBinary(signaturePrefixBuilder) | (uint)CorTokenType.mdtMethodDef;
            builder.Append(MetadataNameFormatter.FormatHandle(
                _metadataReader,
                MetadataTokens.Handle((int)methodDefToken),
                namespaceQualified: true,
                owningTypeOverride: owningTypeOverride,
                signaturePrefix: signaturePrefixBuilder.ToString()));
        }

        /// <summary>
        /// Read a memberRef token from the signature and output the corresponding object to the builder.
        /// </summary>
        /// <param name="builder">Output string builder</param>
        /// <param name="owningTypeOverride">Explicit owning type override</param>
        private void ParseMethodRefToken(StringBuilder builder, string owningTypeOverride)
        {
            StringBuilder signaturePrefixBuilder = new StringBuilder();
            uint methodRefToken = ReadUIntAndEmitInlineSignatureBinary(signaturePrefixBuilder) | (uint)CorTokenType.mdtMemberRef;
            builder.Append(MetadataNameFormatter.FormatHandle(
                _metadataReader,
                MetadataTokens.Handle((int)methodRefToken),
                namespaceQualified: false,
                owningTypeOverride: owningTypeOverride,
                signaturePrefix: signaturePrefixBuilder.ToString()));
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

                case ReadyToRunHelper.NewMultiDimArr_NonVarArg:
                    builder.Append("NEW_MULTI_DIM_ARR__NON_VAR_ARG");
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
                    builder.Append(string.Format("Unknown helper: {0:X2}", helperType));
                    break;
            }
        }

        /// <summary>
        /// Read a string token from the signature stream and convert it to the actual string.
        /// </summary>
        /// <returns></returns>
        private void ParseStringHandle(StringBuilder builder)
        {
            uint rid = ReadUIntAndEmitInlineSignatureBinary(builder);
            UserStringHandle stringHandle = MetadataTokens.UserStringHandle((int)rid);
            builder.Append(_metadataReader.GetUserString(stringHandle));
        }
    }
}
