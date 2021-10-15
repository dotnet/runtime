// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the constant metadata table.
    /// </summary>
    public sealed class ConstantNode : TokenBasedNode
    {
        public ConstantNode(EcmaModule module, ConstantHandle handle)
            : base(module, handle)
        {
        }

        private ConstantHandle Handle => (ConstantHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            Constant constant = _module.MetadataReader.GetConstant(Handle);
            // TODO: the Value might depend on other tokens
            yield break;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            Constant constant = reader.GetConstant(Handle);

            var builder = writeContext.MetadataBuilder;

            // TODO: the value blob might contain references to tokens we need to rewrite
            var valueBlob = reader.GetBlobReader(constant.Value);
            object value = constant.TypeCode switch
            {
                ConstantTypeCode.Boolean => valueBlob.ReadBoolean(),
                ConstantTypeCode.Byte => valueBlob.ReadByte(),
                ConstantTypeCode.Char => valueBlob.ReadChar(),
                ConstantTypeCode.Double => valueBlob.ReadDouble(),
                ConstantTypeCode.Int16 => valueBlob.ReadInt16(),
                ConstantTypeCode.Int32 => valueBlob.ReadInt32(),
                ConstantTypeCode.Int64 => valueBlob.ReadInt64(),
                ConstantTypeCode.NullReference => null,
                ConstantTypeCode.SByte => valueBlob.ReadSByte(),
                ConstantTypeCode.Single => valueBlob.ReadSingle(),
                ConstantTypeCode.String => valueBlob.ReadUTF16(valueBlob.Length),
                ConstantTypeCode.UInt16 => valueBlob.ReadUInt16(),
                ConstantTypeCode.UInt32 => valueBlob.ReadUInt32(),
                ConstantTypeCode.UInt64 => valueBlob.ReadUInt64(),
                _ => throw new InvalidOperationException($"Not expected ConstantTypeCode {constant.TypeCode}"),
            };

            return builder.AddConstant(writeContext.TokenMap.MapToken(constant.Parent),
                value);
        }

        public override string ToString()
        {
            // TODO: Need to write a helper to get the parent name
            return "Constant";
        }
    }
}
