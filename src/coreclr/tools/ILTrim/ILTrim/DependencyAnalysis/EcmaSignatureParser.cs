// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;

using System.Reflection.Metadata;

namespace ILTrim.DependencyAnalysis
{
    public struct EcmaSignatureParser
    {
        private MetadataReader _reader;
        private BlobBuilder _builder;
        private TokenMap _tokenMap;

        public EcmaSignatureParser(MetadataReader reader, TokenMap tokenMap)
        {
            _reader = reader;
            _builder = new BlobBuilder();
            _tokenMap = tokenMap;
        }

        public byte[] GetLocalVariableBlob(StandaloneSignatureHandle handle)
        {
            StandaloneSignature standaloneSig = _reader.GetStandaloneSignature(handle);
            BlobReader signatureReader = _reader.GetBlobReader(standaloneSig.Signature);
            SignatureHeader header = signatureReader.ReadSignatureHeader();
            int varCount = signatureReader.ReadCompressedInteger();
            var blobBuilder = new BlobBuilder();
            var encoder = new BlobEncoder(blobBuilder);
            var localEncoder = encoder.LocalVariableSignature(varCount);

            for (int i = 0; i < varCount; i++)
            {
                SignatureTypeCode typeCode = signatureReader.ReadSignatureTypeCode();
                switch (typeCode)
                {
                    case SignatureTypeCode.TypeHandle:
                        {
                            var localVarTypeEncoder = localEncoder.AddVariable();

                            var signatureTypeEncoder = localVarTypeEncoder.Type();
                            signatureTypeEncoder.Type(_tokenMap.MapToken((TypeDefinitionHandle)signatureReader.ReadTypeHandle()), isValueType: false);
                            break;
                        }

                    case SignatureTypeCode.Int32:
                        {
                            var localVarTypeEncoder = localEncoder.AddVariable();

                            var signatureTypeEncoder = localVarTypeEncoder.Type();
                            signatureTypeEncoder.Int32();
                            break;
                        }

                    default:
                        break;
                }
            }

            return blobBuilder.ToArray();
        }

    }
}
