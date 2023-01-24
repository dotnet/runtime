// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;


namespace ILCompiler
{
    // Alternate form of metadata that represents a single method. Self contained except for type references
    // The behavior of this code must exactly match that of the logic in the VM
    // That code can be found in src\coreclr\vm\readytorunstandalonemethodmetadata.cpp
    public class ReadyToRunStandaloneMethodMetadata
    {
        public byte[] ConstantData;
        public TypeDesc[] TypeRefs;

        public static ReadyToRunStandaloneMethodMetadata Compute(EcmaMethod wrappedMethod)
        {
            var metadataReader = wrappedMethod.MetadataReader;
            var _module = wrappedMethod.Module;
            var rva = wrappedMethod.MetadataReader.GetMethodDefinition(wrappedMethod.Handle).RelativeVirtualAddress;
            var _methodBody = _module.PEReader.GetMethodBody(rva);

            byte[] _alternateILStream = _methodBody.GetILBytes();

            var exceptionRegions = _methodBody.ExceptionRegions;
            ILExceptionRegion[] _exceptionRegions;

            int length = exceptionRegions.Length;
            if (length == 0)
            {
                _exceptionRegions = Array.Empty<ILExceptionRegion>();
            }
            else
            {
                _exceptionRegions = new ILExceptionRegion[length];
                for (int i = 0; i < length; i++)
                {
                    var exceptionRegion = exceptionRegions[i];

                    _exceptionRegions[i] = new ILExceptionRegion(
                        (ILExceptionRegionKind)exceptionRegion.Kind, // assumes that ILExceptionRegionKind and ExceptionRegionKind enums are in sync
                        exceptionRegion.TryOffset,
                        exceptionRegion.TryLength,
                        exceptionRegion.HandlerOffset,
                        exceptionRegion.HandlerLength,
                        MetadataTokens.GetToken(exceptionRegion.CatchType),
                        exceptionRegion.FilterOffset);
                }
            }

            BlobReader localsBlob = default(BlobReader);
            if (!_methodBody.LocalSignature.IsNil)
            {
                localsBlob = wrappedMethod.MetadataReader.GetBlobReader(wrappedMethod.MetadataReader.GetStandaloneSignature(_methodBody.LocalSignature).Signature);
            }

            AlternativeTypeRefProvider alternateTypes = new AlternativeTypeRefProvider();
            EcmaSignatureEncoder<AlternativeTypeRefProvider> alternateEncoder = new EcmaSignatureEncoder<AlternativeTypeRefProvider>(alternateTypes);
            Dictionary<int, int> _alternateNonTokenStrings = new Dictionary<int, int>();
            BlobBuilder _alternateNonTypeRefStream = new BlobBuilder();
            BlobBuilder _nonCodeAlternateBlob = new BlobBuilder();
            {
                // Generate alternate stream for exceptions.
                _nonCodeAlternateBlob.WriteCompressedInteger(_exceptionRegions.Length);

                for (int i = 0; i < _exceptionRegions.Length; i++)
                {
                    var region = _exceptionRegions[i];
                    _nonCodeAlternateBlob.WriteCompressedInteger((int)region.Kind);
                    _nonCodeAlternateBlob.WriteCompressedInteger((int)region.TryOffset);
                    _nonCodeAlternateBlob.WriteCompressedInteger((int)region.TryLength);
                    _nonCodeAlternateBlob.WriteCompressedInteger((int)region.HandlerOffset);
                    _nonCodeAlternateBlob.WriteCompressedInteger((int)region.HandlerLength);
                    if (region.Kind == ILExceptionRegionKind.Catch)
                    {
                        int alternateToken = GetAlternateStreamToken(region.ClassToken);
                        int encodedToken = CodedIndex.TypeDefOrRefOrSpec(MetadataTokens.EntityHandle(alternateToken));
                        _nonCodeAlternateBlob.WriteCompressedInteger(encodedToken);
                    }
                    else if (region.Kind == ILExceptionRegionKind.Filter)
                    {
                        _nonCodeAlternateBlob.WriteCompressedInteger(region.FilterOffset);
                    }
                }

                if (localsBlob.Length == 0)
                {
                    // No locals. Encode a 2 to indicate this
                    _nonCodeAlternateBlob.WriteByte(2);
                }
                else
                {
                    _nonCodeAlternateBlob.WriteByte(_methodBody.LocalVariablesInitialized ? (byte)1 : (byte)0);
                    EcmaSignatureTranslator sigTranslator = new EcmaSignatureTranslator(localsBlob, _nonCodeAlternateBlob, GetAlternateStreamToken);
                    sigTranslator.ParseLocalsSignature();
                }
            }

            ILTokenReplacer.Replace(_alternateILStream, GetAlternateStreamToken);

            // Add all the streams together into the _nonCodeAlternateBlob
            int expectedFinalSize = _nonCodeAlternateBlob.Count + _alternateILStream.Length + _alternateNonTypeRefStream.Count;
            _nonCodeAlternateBlob.WriteBytes(_alternateILStream);
            _nonCodeAlternateBlob.LinkSuffix(_alternateNonTypeRefStream);
            Debug.Assert(expectedFinalSize == _nonCodeAlternateBlob.Count);

            ReadyToRunStandaloneMethodMetadata methodBlock = new ReadyToRunStandaloneMethodMetadata();
            methodBlock.ConstantData = _nonCodeAlternateBlob.ToArray();
            methodBlock.TypeRefs = alternateTypes._alternateTypeRefStream.ToArray();
            return methodBlock;

            ///// HELPER FUNCTIONS

            unsafe int GetAlternateStreamToken(int token)
            {
                if (token == 0 || !_alternateNonTokenStrings.TryGetValue(token, out int alternateToken))
                {
                    var handle = MetadataTokens.Handle(token);

                    if (handle.Kind == HandleKind.TypeDefinition || handle.Kind == HandleKind.TypeReference)
                    {
                        EcmaType ecmaType = (EcmaType)wrappedMethod.Module.GetObject(MetadataTokens.EntityHandle(token));
                        alternateToken = MetadataTokens.GetToken(alternateTypes.GetTypeDefOrRefHandleForTypeDesc(ecmaType));
                    }
                    else
                    {
                        BlobBuilder blob = new BlobBuilder();
                        int flag = 0;
                        if (handle.Kind == HandleKind.UserString)
                        {
                            string strAlternate = metadataReader.GetUserString((UserStringHandle)handle);
                            flag = 0x70000000;
                            blob.WriteCompressedInteger(strAlternate.Length);
                            fixed (char* charData = strAlternate)
                            {
                                blob.WriteBytes((byte*)charData, strAlternate.Length * 2);
                            }
                            // TODO: consider encoding via wtf-8, or possibly utf-8
                        }
                        else if (handle.Kind == HandleKind.TypeSpecification)
                        {
                            flag = 0x1B000000;
                            var sigBlob = metadataReader.GetBlobReader(metadataReader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
                            EcmaSignatureTranslator sigTranslator = new EcmaSignatureTranslator(sigBlob, blob, GetAlternateStreamToken);
                            sigTranslator.ParseType();
                        }
                        else if (handle.Kind == HandleKind.MemberReference)
                        {
                            flag = 0x0a000000;
                            var memberReference = metadataReader.GetMemberReference((MemberReferenceHandle)handle);
                            var sigBlob = metadataReader.GetBlobReader(memberReference.Signature);
                            EcmaSignatureTranslator sigTranslator = new EcmaSignatureTranslator(sigBlob, blob, GetAlternateStreamToken);
                            sigTranslator.ParseMemberRefSignature();
                            blob.WriteSerializedString(metadataReader.GetString(memberReference.Name));
                            blob.WriteCompressedInteger(CodedIndex.MemberRefParent(MetadataTokens.EntityHandle(GetAlternateStreamToken(MetadataTokens.GetToken(memberReference.Parent)))));
                        }
                        else if (handle.Kind == HandleKind.MethodDefinition)
                        {
                            flag = 0x0a000000;
                            var methodDefinition = metadataReader.GetMethodDefinition((MethodDefinitionHandle)handle);
                            var sigBlob = metadataReader.GetBlobReader(methodDefinition.Signature);
                            EcmaSignatureTranslator sigTranslator = new EcmaSignatureTranslator(sigBlob, blob, GetAlternateStreamToken);
                            sigTranslator.ParseMethodSignature();
                            blob.WriteSerializedString(metadataReader.GetString(methodDefinition.Name));
                            blob.WriteCompressedInteger(CodedIndex.MemberRefParent(MetadataTokens.EntityHandle(GetAlternateStreamToken(MetadataTokens.GetToken(methodDefinition.GetDeclaringType())))));
                        }
                        else if (handle.Kind == HandleKind.FieldDefinition)
                        {
                            flag = 0x0a000000;
                            var fieldDefinition = metadataReader.GetFieldDefinition((FieldDefinitionHandle)handle);
                            var sigBlob = metadataReader.GetBlobReader(fieldDefinition.Signature);
                            EcmaSignatureTranslator sigTranslator = new EcmaSignatureTranslator(sigBlob, blob, GetAlternateStreamToken);
                            sigTranslator.ParseFieldSignature();
                            blob.WriteSerializedString(metadataReader.GetString(fieldDefinition.Name));
                            blob.WriteCompressedInteger(CodedIndex.MemberRefParent(MetadataTokens.EntityHandle(GetAlternateStreamToken(MetadataTokens.GetToken(fieldDefinition.GetDeclaringType())))));
                        }
                        else if (handle.Kind == HandleKind.MethodSpecification)
                        {
                            flag = 0x2B000000;
                            var methodSpecification = metadataReader.GetMethodSpecification((MethodSpecificationHandle)handle);
                            var sigBlob = metadataReader.GetBlobReader(methodSpecification.Signature);
                            blob.WriteCompressedInteger(MetadataTokens.GetRowNumber(MetadataTokens.EntityHandle(GetAlternateStreamToken(MetadataTokens.GetToken(methodSpecification.Method)))));
                            EcmaSignatureTranslator sigTranslator = new EcmaSignatureTranslator(sigBlob, blob, GetAlternateStreamToken);
                            sigTranslator.ParseMethodSpecSignature();
                        }
                        else if (handle.Kind == HandleKind.StandaloneSignature)
                        {
                            flag = 0x11000000;
                            var reader = wrappedMethod.Module.MetadataReader;
                            var sigBlob = reader.GetBlobReader(reader.GetStandaloneSignature((StandaloneSignatureHandle)handle).Signature);
                            EcmaSignatureTranslator sigTranslator = new EcmaSignatureTranslator(sigBlob, blob, GetAlternateStreamToken);
                            sigTranslator.ParseMethodSignature();
                        }

                        _alternateNonTypeRefStream.WriteBytes(blob.ToArray());
                        alternateToken = (_alternateNonTokenStrings.Count + 1) | flag;
                    }
                    _alternateNonTokenStrings.Add(token, alternateToken);
                }

                return alternateToken;
            }
        }

        class AlternativeTypeRefProvider : IEntityHandleProvider
        {
            public List<TypeDesc> _alternateTypeRefStream = new List<TypeDesc>();
            Dictionary<TypeDesc, EntityHandle> _alternateTypeRefTokens = new Dictionary<TypeDesc, EntityHandle>();
            public EntityHandle GetTypeDefOrRefHandleForTypeDesc(TypeDesc type)
            {
                if (_alternateTypeRefTokens.TryGetValue(type, out EntityHandle result))
                {
                    return result;
                }
                _alternateTypeRefStream.Add(type);
                result = MetadataTokens.TypeReferenceHandle(_alternateTypeRefStream.Count);
                _alternateTypeRefTokens.Add(type, result);
                return result;
            }
        }
    }
}
