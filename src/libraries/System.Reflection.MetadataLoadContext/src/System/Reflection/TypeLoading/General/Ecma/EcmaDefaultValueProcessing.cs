// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace System.Reflection.TypeLoading.Ecma
{
    internal static class EcmaDefaultValueProcessing
    {
        public static object ToRawObject(this ConstantHandle constantHandle, MetadataReader metadataReader)
        {
            if (constantHandle.IsNil)
                throw new BadImageFormatException();

            Constant constantValue = metadataReader.GetConstant(constantHandle);
            if (constantValue.Value.IsNil)
                throw new BadImageFormatException();

            BlobReader reader = metadataReader.GetBlobReader(constantValue.Value);
            switch (constantValue.TypeCode)
            {
                case ConstantTypeCode.Boolean:
                    return reader.ReadBoolean();

                case ConstantTypeCode.Char:
                    return reader.ReadChar();

                case ConstantTypeCode.SByte:
                    return reader.ReadSByte();

                case ConstantTypeCode.Int16:
                    return reader.ReadInt16();

                case ConstantTypeCode.Int32:
                    return reader.ReadInt32();

                case ConstantTypeCode.Int64:
                    return reader.ReadInt64();

                case ConstantTypeCode.Byte:
                    return reader.ReadByte();

                case ConstantTypeCode.UInt16:
                    return reader.ReadUInt16();

                case ConstantTypeCode.UInt32:
                    return reader.ReadUInt32();

                case ConstantTypeCode.UInt64:
                    return reader.ReadUInt64();

                case ConstantTypeCode.Single:
                    return reader.ReadSingle();

                case ConstantTypeCode.Double:
                    return reader.ReadDouble();

                case ConstantTypeCode.String:
                    return reader.ReadUTF16(reader.Length);

                case ConstantTypeCode.NullReference:
                    // Partition II section 22.9:
                    // The encoding of Type for the nullref value is ELEMENT_TYPE_CLASS with a Value of a 4-byte zero.
                    // Unlike uses of ELEMENT_TYPE_CLASS in signatures, this one is not followed by a type token.
                    if (reader.ReadUInt32() == 0)
                        return null;
                    break;
            }
            throw new BadImageFormatException();
        }

        public static bool TryFindRawDefaultValueFromCustomAttributes(this CustomAttributeHandleCollection handles, EcmaModule module, out object rawDefaultValue)
        {
            rawDefaultValue = default;

            MetadataReader reader = module.Reader;
            foreach (CustomAttributeHandle handle in handles)
            {
                CustomAttribute ca = handle.GetCustomAttribute(reader);
                EntityHandle declaringTypeHandle = ca.TryGetDeclaringTypeHandle(reader);
                if (declaringTypeHandle.IsNil)
                    continue;

                if (declaringTypeHandle.TypeMatchesNameAndNamespace(Utf8Constants.SystemRuntimeCompilerServices, Utf8Constants.DateTimeConstantAttribute, reader))
                {
                    CustomAttributeData cad = handle.ToCustomAttributeData(module);
                    IList<CustomAttributeTypedArgument> cats = cad.ConstructorArguments;
                    if (cats.Count != 1)
                        return false;

                    CoreTypes ct = module.Loader.GetAllFoundCoreTypes();
                    if (cats[0].ArgumentType != ct[CoreType.Int64])
                        return false;

                    long ticks = (long)(cats[0].Value);
                    rawDefaultValue = new DateTimeConstantAttribute(ticks).Value;
                    return true;
                }

                if (declaringTypeHandle.TypeMatchesNameAndNamespace(Utf8Constants.SystemRuntimeCompilerServices, Utf8Constants.DecimalConstantAttribute, reader))
                {
                    CustomAttributeData cad = handle.ToCustomAttributeData(module);
                    IList<CustomAttributeTypedArgument> cats = cad.ConstructorArguments;
                    if (cats.Count != 5)
                        return false;

                    CoreTypes ct = module.Loader.GetAllFoundCoreTypes();
                    if (cats[0].ArgumentType != ct[CoreType.Byte] ||
                        cats[1].ArgumentType != ct[CoreType.Byte])
                        return false;

                    byte scale = (byte)cats[0].Value;
                    byte sign = (byte)cats[1].Value;

                    if (cats[2].ArgumentType == ct[CoreType.Int32] && cats[3].ArgumentType == ct[CoreType.Int32] && cats[4].ArgumentType == ct[CoreType.Int32])
                    {
                        int hi = (int)cats[2].Value;
                        int mid = (int)cats[3].Value;
                        int lo = (int)cats[4].Value;
                        rawDefaultValue = new DecimalConstantAttribute(scale, sign, hi, mid, lo).Value;
                        return true;
                    }

                    if (cats[2].ArgumentType == ct[CoreType.UInt32] && cats[3].ArgumentType == ct[CoreType.UInt32] && cats[4].ArgumentType == ct[CoreType.UInt32])
                    {
                        uint hi = (uint)cats[2].Value;
                        uint mid = (uint)cats[3].Value;
                        uint lo = (uint)cats[4].Value;
                        rawDefaultValue = new DecimalConstantAttribute(scale, sign, hi, mid, lo).Value;
                        return true;
                    }

                    return false;
                }

                // Should we also look for CustomConstantAttribute too? Who uses that (other than DateTimeConstantAttribute which
                // we handled above?) CustomConstantAttribute is an abstract class which means we have to figure out how a subclass
                // we've never heard of would set the "Value" property which is kinda hard to do when you can't Invoke().
                // Even the CLR doesn't return consistent values for this between the raw and non-raw versions.
                // Even doing the subclass check would open the door to resolving types and dependency assemblies and their
                // resulting FileNotFoundExceptions. Which is exactly what we're trying to avoid with this name-based lookup approach. 
            }

            return false;
        }
    }
}
