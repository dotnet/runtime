// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Linq;

using Internal.TypeSystem;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the StandaloneSignature table.
    /// </summary>
    public sealed class StandaloneSignatureNode : TokenBasedNode
    {
        public StandaloneSignatureNode(EcmaModule module, StandaloneSignatureHandle handle)
            : base(module, handle)
        {
        }

        private StandaloneSignatureHandle Handle => (StandaloneSignatureHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            // @TODO: These need to go EcmaSignatureParser
            // Cannot think of a design that can move this logic to that struct

            MetadataReader reader = _module.MetadataReader;

            StandaloneSignature standaloneSig = reader.GetStandaloneSignature(Handle);

            BlobReader signatureReader = reader.GetBlobReader(standaloneSig.Signature);

            if (signatureReader.ReadSignatureHeader().Kind != SignatureKind.LocalVariables)
                ThrowHelper.ThrowInvalidProgramException();

            int count = signatureReader.ReadCompressedInteger();

            for (int i = 0; i < count; i++)
            {
                SignatureTypeCode typeCode = signatureReader.ReadSignatureTypeCode();
                switch (typeCode)
                {
                    case SignatureTypeCode.TypeHandle:
                        TypeDefinitionHandle typeDefHandle = (TypeDefinitionHandle)signatureReader.ReadTypeHandle();
                        yield return new DependencyListEntry(factory.TypeDefinition(_module, typeDefHandle), "Local variable type");
                        break;
                }
            }

            yield break;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            EcmaSignatureParser signatureParser = new EcmaSignatureParser(_module.MetadataReader, writeContext.TokenMap);
            byte[] blobBytes = signatureParser.GetLocalVariableBlob(Handle);

            var builder = writeContext.MetadataBuilder;
            return builder.AddStandaloneSignature(
                builder.GetOrAddBlob(blobBytes));

        }

        public override string ToString()
        {
            return "Standalone signature";
        }
    }
}
