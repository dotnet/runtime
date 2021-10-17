// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.IL;
using Internal.TypeSystem.Ecma;

using ILTrim.DependencyAnalysis;

namespace ILTrim
{
    /// <summary>
    /// Context passed to nodes when writing out the output. Captures all state necessary for writing.
    /// </summary>
    public sealed class ModuleWritingContext
    {
        private BlobBuilder _blobBuilder = new BlobBuilder();

        public MetadataBuilder MetadataBuilder { get; } = new MetadataBuilder();

        public BlobBuilder FieldDataBuilder { get; } = new BlobBuilder();

        public BlobBuilder ManagedResourceBuilder { get; } = new BlobBuilder();

        public MethodBodyStreamEncoder MethodBodyEncoder { get; } = new MethodBodyStreamEncoder(new BlobBuilder());

        public TokenMap TokenMap { get; }

        public NodeFactory Factory { get; }

        public ModuleWritingContext(NodeFactory factory, TokenMap tokenMap)
        {
            Factory = factory;
            TokenMap = tokenMap;
        }

        /// <summary>
        /// Gets the shared blob builder associated with this context.
        /// </summary>
        public BlobBuilder GetSharedBlobBuilder()
        {
            _blobBuilder.Clear();
            return _blobBuilder;
        }

        int? _unreachableBodyOffset;

        public int WriteUnreachableMethodBody(MethodDefinitionHandle methodHandle, EcmaModule module)
        {
            if (module.MetadataReader.GetMethodDefinition(methodHandle).RelativeVirtualAddress == 0) {
                return -1;
            }

            if (_unreachableBodyOffset is int offset)
                return offset;

            BlobBuilder outputBodyBuilder = GetSharedBlobBuilder();
            outputBodyBuilder.WriteByte((byte)ILOpcode.ldnull);
            outputBodyBuilder.WriteByte((byte)ILOpcode.throw_);

            MethodBodyStreamEncoder.MethodBody bodyEncoder = MethodBodyEncoder.AddMethodBody(
                outputBodyBuilder.Count,
                maxStack: 1,
                exceptionRegionCount: 0,
                hasSmallExceptionRegions: true,
                localVariablesSignature: default,
                MethodBodyAttributes.None);
            BlobWriter instructionsWriter = new(bodyEncoder.Instructions);
            outputBodyBuilder.WriteContentTo(ref instructionsWriter);
            _unreachableBodyOffset = bodyEncoder.Offset;
            return _unreachableBodyOffset.GetValueOrDefault();
        }
    }
}
