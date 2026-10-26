// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Metadata.NativeFormat;
using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;
using Constant = Internal.Metadata.NativeFormat.Writer.Constant;

namespace ILCompiler.Metadata
{
    internal partial class Transform<TPolicy>
    {
        private Constant HandleConstant(Cts.Ecma.EcmaModule module, Ecma.ConstantHandle constantHandle)
        {
            Ecma.MetadataReader reader = module.MetadataReader;
            Ecma.Constant constant = reader.GetConstant(constantHandle);
            Ecma.BlobReader blob = reader.GetBlobReader(constant.Value);
            Constant result = new Constant { TypeCode = (ConstantTypeCode)constant.TypeCode };

            // ECMA-335 II.22.9: transcode UTF16 string constants to inline SerStrings.
            if (constant.TypeCode == Ecma.ConstantTypeCode.String)
            {
                Ecma.BlobBuilder builder = new Ecma.BlobBuilder();
                builder.WriteSerializedString(blob.ReadUTF16(blob.Length));
                result.Blob = builder.ToArray();
            }
            else
            {
                int size = constant.TypeCode switch
                {
                    Ecma.ConstantTypeCode.Boolean or Ecma.ConstantTypeCode.SByte or Ecma.ConstantTypeCode.Byte => sizeof(byte),
                    Ecma.ConstantTypeCode.Char or Ecma.ConstantTypeCode.Int16 or Ecma.ConstantTypeCode.UInt16 => sizeof(short),
                    Ecma.ConstantTypeCode.Int32 or Ecma.ConstantTypeCode.UInt32 or Ecma.ConstantTypeCode.Single => sizeof(int),
                    Ecma.ConstantTypeCode.Int64 or Ecma.ConstantTypeCode.UInt64 or Ecma.ConstantTypeCode.Double => sizeof(long),
                    Ecma.ConstantTypeCode.NullReference => blob.Length,
                    _ => throw new BadImageFormatException(),
                };
                result.Blob = blob.ReadBytes(size);
            }

            return result;
        }
    }
}
