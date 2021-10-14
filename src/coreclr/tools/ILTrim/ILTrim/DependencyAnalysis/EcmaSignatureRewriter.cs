// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ILTrim.DependencyAnalysis
{
    public struct EcmaSignatureRewriter
    {
        private BlobReader _blobReader;
        private readonly TokenMap _tokenMap;

        private EcmaSignatureRewriter(BlobReader blobReader, TokenMap tokenMap)
        {
            _blobReader = blobReader;
            _tokenMap = tokenMap;
        }

        private void RewriteCustomModifier(SignatureTypeCode typeCode, CustomModifiersEncoder encoder)
        {
            encoder.AddModifier(
                _tokenMap.MapToken(_blobReader.ReadTypeHandle()),
                typeCode == SignatureTypeCode.OptionalModifier);
        }

        private void RewriteType(SignatureTypeEncoder encoder)
        {
            RewriteType(_blobReader.ReadSignatureTypeCode(), encoder);
        }

        private void RewriteType(SignatureTypeCode typeCode, SignatureTypeEncoder encoder)
        {
            switch (typeCode)
            {
                case SignatureTypeCode.Boolean:
                    encoder.Boolean(); break;
                case SignatureTypeCode.SByte:
                    encoder.SByte(); break;
                case SignatureTypeCode.Byte:
                    encoder.Byte(); break;
                case SignatureTypeCode.Int16:
                    encoder.Int16(); break;
                case SignatureTypeCode.UInt16:
                    encoder.UInt16(); break;
                case SignatureTypeCode.Int32:
                    encoder.Int32(); break;
                case SignatureTypeCode.UInt32:
                    encoder.UInt32(); break;
                case SignatureTypeCode.Int64:
                    encoder.Int64(); break;
                case SignatureTypeCode.UInt64:
                    encoder.UInt64(); break;
                case SignatureTypeCode.Single:
                    encoder.Single(); break;
                case SignatureTypeCode.Double:
                    encoder.Double(); break;
                case SignatureTypeCode.Char:
                    encoder.Char(); break;
                case SignatureTypeCode.String:
                    encoder.String(); break;
                case SignatureTypeCode.IntPtr:
                    encoder.IntPtr(); break;
                case SignatureTypeCode.UIntPtr:
                    encoder.UIntPtr(); break;
                case SignatureTypeCode.Object:
                    encoder.Object(); break;
                case SignatureTypeCode.TypeHandle:
                    {
                        // S.R.Metadata "helpfully" removed the Class/ValueType bit and collapsed this
                        // to a single TypeHandle but we need to know what this was.
                        // Extract it from the blobReader.
                        _blobReader.Offset = _blobReader.Offset - 1;
                        byte classOrValueType = _blobReader.ReadByte();
                        System.Diagnostics.Debug.Assert(classOrValueType == 0x12 || classOrValueType == 0x11);
                        encoder.Type(
                            _tokenMap.MapToken(_blobReader.ReadTypeHandle()),
                            isValueType: classOrValueType == 0x11);
                    }
                    break;
                case SignatureTypeCode.SZArray:
                    RewriteType(encoder.SZArray());
                    break;
                case SignatureTypeCode.Array:
                    throw new NotImplementedException();
                case SignatureTypeCode.Pointer:
                    RewriteType(encoder.Pointer());
                    break;
                case SignatureTypeCode.GenericTypeParameter:
                    encoder.GenericTypeParameter(_blobReader.ReadCompressedInteger());
                    break;
                case SignatureTypeCode.GenericMethodParameter:
                    encoder.GenericMethodTypeParameter(_blobReader.ReadCompressedInteger());
                    break;
                case SignatureTypeCode.RequiredModifier:
                case SignatureTypeCode.OptionalModifier:
                    RewriteCustomModifier(typeCode, encoder.CustomModifiers());
                    break;
                case SignatureTypeCode.GenericTypeInstance:
                    {
                        int classOrValueType = _blobReader.ReadCompressedInteger();
                        System.Diagnostics.Debug.Assert(classOrValueType == 0x12 || classOrValueType == 0x11);
                        EntityHandle genericTypeDefHandle = _blobReader.ReadTypeHandle();
                        int numGenericArgs = _blobReader.ReadCompressedInteger();

                        GenericTypeArgumentsEncoder genericArgsEncoder = encoder.GenericInstantiation(
                            _tokenMap.MapToken(genericTypeDefHandle),
                            numGenericArgs,
                            isValueType: classOrValueType == 0x11
                            );

                        for (int i = 0; i < numGenericArgs; i++)
                        {
                            RewriteType(genericArgsEncoder.AddArgument());
                        }
                    }
                    break;
                case SignatureTypeCode.TypedReference:
                    encoder.PrimitiveType(PrimitiveTypeCode.TypedReference); break;
                case SignatureTypeCode.FunctionPointer:
                    throw new NotImplementedException();
                default:
                    throw new BadImageFormatException();
            }
        }

        public static void RewriteLocalVariableBlob(BlobReader signatureReader, TokenMap tokenMap, BlobBuilder blobBuilder)
        {
            new EcmaSignatureRewriter(signatureReader, tokenMap).RewriteLocalVariableBlob(blobBuilder);
        }

        private void RewriteLocalVariableBlob(BlobBuilder blobBuilder)
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            int varCount = _blobReader.ReadCompressedInteger();
            var encoder = new BlobEncoder(blobBuilder);
            var localEncoder = encoder.LocalVariableSignature(varCount);

            for (int i = 0; i < varCount; i++)
            {
                var localVarTypeEncoder = localEncoder.AddVariable();
                bool isPinned = false;
                bool isByRef = false;

            again:
                SignatureTypeCode typeCode = _blobReader.ReadSignatureTypeCode();
                if (typeCode == SignatureTypeCode.RequiredModifier || typeCode == SignatureTypeCode.OptionalModifier)
                {
                    RewriteCustomModifier(typeCode, localVarTypeEncoder.CustomModifiers());
                    goto again;
                }
                if (typeCode == SignatureTypeCode.Pinned)
                {
                    isPinned = true;
                    goto again;
                }
                if (typeCode == SignatureTypeCode.ByReference)
                {
                    isByRef = true;
                    goto again;
                }
                RewriteType(typeCode, localVarTypeEncoder.Type(isByRef, isPinned));
            }
        }

        public static void RewriteMethodSignature(BlobReader signatureReader, TokenMap tokenMap, BlobBuilder blobBuilder)
        {
            new EcmaSignatureRewriter(signatureReader, tokenMap).RewriteMethodSignature(blobBuilder);
        }

        private void RewriteMethodSignature(BlobBuilder blobBuilder)
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            RewriteMethodSignature(blobBuilder, header);
        }

        private void RewriteMethodSignature(BlobBuilder blobBuilder, SignatureHeader header)
        {
            int arity = header.IsGeneric ? _blobReader.ReadCompressedInteger() : 0;
            var encoder = new BlobEncoder(blobBuilder);
            var sigEncoder = encoder.MethodSignature(header.CallingConvention, arity, header.IsInstance);

            int count = _blobReader.ReadCompressedInteger();

            sigEncoder.Parameters(count, out ReturnTypeEncoder returnTypeEncoder, out ParametersEncoder paramsEncoder);

            bool isByRef = false;
        againReturnType:
            SignatureTypeCode typeCode = _blobReader.ReadSignatureTypeCode();
            if (typeCode == SignatureTypeCode.ByReference)
            {
                isByRef = true;
                goto againReturnType;
            }
            if (typeCode == SignatureTypeCode.RequiredModifier || typeCode == SignatureTypeCode.OptionalModifier)
            {
                RewriteCustomModifier(typeCode, returnTypeEncoder.CustomModifiers());
                goto againReturnType;
            }

            if (typeCode == SignatureTypeCode.Void)
            {
                returnTypeEncoder.Void();
            }
            else
            {
                RewriteType(typeCode, returnTypeEncoder.Type(isByRef));
            }

            for (int i = 0; i < count; i++)
            {
                ParameterTypeEncoder paramEncoder = paramsEncoder.AddParameter();

                isByRef = false;

            againParameter:
                typeCode = _blobReader.ReadSignatureTypeCode();
                if (typeCode == SignatureTypeCode.RequiredModifier || typeCode == SignatureTypeCode.OptionalModifier)
                {
                    RewriteCustomModifier(typeCode, paramEncoder.CustomModifiers());
                    goto againParameter;
                }
                if (typeCode == SignatureTypeCode.ByReference)
                {
                    isByRef = true;
                    goto againParameter;
                }
                RewriteType(typeCode, paramEncoder.Type(isByRef));
            }
        }

        public static void RewriteFieldSignature(BlobReader signatureReader, TokenMap tokenMap, BlobBuilder blobBuilder)
        {
            new EcmaSignatureRewriter(signatureReader, tokenMap).RewriteFieldSignature(blobBuilder);
        }

        private void RewriteFieldSignature(BlobBuilder blobBuilder)
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            RewriteFieldSignature(blobBuilder, header);
        }

        private void RewriteFieldSignature(BlobBuilder blobBuilder, SignatureHeader header)
        {
            var encoder = new BlobEncoder(blobBuilder);
            var sigEncoder = encoder.FieldSignature();

            RewriteType(sigEncoder);
        }

        public static void RewriteMemberReferenceSignature(BlobReader signatureReader, TokenMap tokenMap, BlobBuilder blobBuilder)
        {
            new EcmaSignatureRewriter(signatureReader, tokenMap).RewriteMemberReferenceSignature(blobBuilder);
        }

        private void RewriteMemberReferenceSignature(BlobBuilder blobBuilder)
        {
            SignatureHeader header = _blobReader.ReadSignatureHeader();
            if (header.Kind == SignatureKind.Method)
            {
                RewriteMethodSignature(blobBuilder, header);
            }
            else
            {
                System.Diagnostics.Debug.Assert(header.Kind == SignatureKind.Field);
                RewriteFieldSignature(blobBuilder, header);
            }
        }

        public static void RewriteTypeSpecSignature(BlobReader signatureReader, TokenMap tokenMap, BlobBuilder blobBuilder)
        {
            new EcmaSignatureRewriter(signatureReader, tokenMap).RewriteTypeSpecSignature(blobBuilder);
        }

        private void RewriteTypeSpecSignature(BlobBuilder blobBuilder)
        {
            var encoder = new SignatureTypeEncoder(blobBuilder);
            RewriteType(encoder);
        }

    }
}
