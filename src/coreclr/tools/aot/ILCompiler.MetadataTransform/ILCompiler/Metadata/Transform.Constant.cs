// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;
using ConstantTypeCode = System.Reflection.Metadata.ConstantTypeCode;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private MetadataRecord HandleConstant(Cts.Ecma.EcmaModule module, Ecma.ConstantHandle constantHandle)
        {
            Ecma.MetadataReader reader = module.MetadataReader;
            Ecma.Constant constant = reader.GetConstant(constantHandle);

            Ecma.BlobReader blob = reader.GetBlobReader(constant.Value);

            switch (constant.TypeCode)
            {
                case ConstantTypeCode.Boolean:
                    return new ConstantBooleanValue { Value = blob.ReadBoolean() };
                case ConstantTypeCode.Byte:
                    return new ConstantByteValue { Value = blob.ReadByte() };
                case ConstantTypeCode.Char:
                    return new ConstantCharValue { Value = blob.ReadChar() };
                case ConstantTypeCode.Double:
                    return new ConstantDoubleValue { Value = blob.ReadDouble() };
                case ConstantTypeCode.Int16:
                    return new ConstantInt16Value { Value = blob.ReadInt16() };
                case ConstantTypeCode.Int32:
                    return new ConstantInt32Value { Value = blob.ReadInt32() };
                case ConstantTypeCode.Int64:
                    return new ConstantInt64Value { Value = blob.ReadInt64() };
                case ConstantTypeCode.SByte:
                    return new ConstantSByteValue { Value = blob.ReadSByte() };
                case ConstantTypeCode.Single:
                    return new ConstantSingleValue { Value = blob.ReadSingle() };
                case ConstantTypeCode.String:
                    return HandleString(blob.ReadUTF16(blob.Length));
                case ConstantTypeCode.UInt16:
                    return new ConstantUInt16Value { Value = blob.ReadUInt16() };
                case ConstantTypeCode.UInt32:
                    return new ConstantUInt32Value { Value = blob.ReadUInt32() };
                case ConstantTypeCode.UInt64:
                    return new ConstantUInt64Value { Value = blob.ReadUInt64() };
                case ConstantTypeCode.NullReference:
                    return new ConstantReferenceValue();
                default:
                    throw new BadImageFormatException();
            }
        }
    }
}
