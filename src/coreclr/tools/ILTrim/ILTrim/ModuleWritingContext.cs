// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

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
    }
}
