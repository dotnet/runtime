// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Diagnostics;

namespace System.Runtime.Serialization.Formatters.Binary
{
    // Routines to convert between the runtime type and the type as it appears on the wire
    internal static class BinaryTypeConverter
    {
        // From the type create the BinaryTypeEnum and typeInformation which describes the type on the wire
        internal static BinaryTypeEnum GetBinaryTypeInfo(Type type, WriteObjectInfo objectInfo, string typeName, ObjectWriter objectWriter, out object typeInformation, out int assemId)
        {
            BinaryTypeEnum binaryTypeEnum;

            assemId = 0;
            typeInformation = null;

            if (ReferenceEquals(type, Converter.s_typeofString))
            {
                binaryTypeEnum = BinaryTypeEnum.String;
            }
            else if (((objectInfo == null) || ((objectInfo != null) && !objectInfo._isSi)) && (ReferenceEquals(type, Converter.s_typeofObject)))
            {
                // If objectInfo.Si then can be a surrogate which will change the type
                binaryTypeEnum = BinaryTypeEnum.Object;
            }
            else if (ReferenceEquals(type, Converter.s_typeofStringArray))
            {
                binaryTypeEnum = BinaryTypeEnum.StringArray;
            }
            else if (ReferenceEquals(type, Converter.s_typeofObjectArray))
            {
                binaryTypeEnum = BinaryTypeEnum.ObjectArray;
            }
            else if (Converter.IsPrimitiveArray(type, out typeInformation))
            {
                binaryTypeEnum = BinaryTypeEnum.PrimitiveArray;
            }
            else
            {
                InternalPrimitiveTypeE primitiveTypeEnum = objectWriter.ToCode(type);
                switch (primitiveTypeEnum)
                {
                    case InternalPrimitiveTypeE.Invalid:
                        string assembly = null;
                        if (objectInfo == null)
                        {
                            assembly = type.Assembly.FullName;
                            typeInformation = type.FullName;
                        }
                        else
                        {
                            assembly = objectInfo.GetAssemblyString();
                            typeInformation = objectInfo.GetTypeFullName();
                        }

                        if (assembly.Equals(Converter.s_urtAssemblyString))
                        {
                            binaryTypeEnum = BinaryTypeEnum.ObjectUrt;
                            assemId = 0;
                        }
                        else
                        {
                            binaryTypeEnum = BinaryTypeEnum.ObjectUser;
                            Debug.Assert(objectInfo != null, "[BinaryConverter.GetBinaryTypeInfo]objectInfo null for user object");
                            assemId = (int)objectInfo._assemId;
                            if (assemId == 0)
                            {
                                throw new SerializationException(SR.Format(SR.Serialization_AssemblyId, typeInformation));
                            }
                        }
                        break;
                    default:
                        binaryTypeEnum = BinaryTypeEnum.Primitive;
                        typeInformation = primitiveTypeEnum;
                        break;
                }
            }

            return binaryTypeEnum;
        }

        // Used for non Si types when Parsing
        internal static BinaryTypeEnum GetParserBinaryTypeInfo(Type type, out object typeInformation)
        {
            BinaryTypeEnum binaryTypeEnum;
            typeInformation = null;

            if (ReferenceEquals(type, Converter.s_typeofString))
            {
                binaryTypeEnum = BinaryTypeEnum.String;
            }
            else if (ReferenceEquals(type, Converter.s_typeofObject))
            {
                binaryTypeEnum = BinaryTypeEnum.Object;
            }
            else if (ReferenceEquals(type, Converter.s_typeofObjectArray))
            {
                binaryTypeEnum = BinaryTypeEnum.ObjectArray;
            }
            else if (ReferenceEquals(type, Converter.s_typeofStringArray))
            {
                binaryTypeEnum = BinaryTypeEnum.StringArray;
            }
            else if (Converter.IsPrimitiveArray(type, out typeInformation))
            {
                binaryTypeEnum = BinaryTypeEnum.PrimitiveArray;
            }
            else
            {
                InternalPrimitiveTypeE primitiveTypeEnum = Converter.ToCode(type);
                switch (primitiveTypeEnum)
                {
                    case InternalPrimitiveTypeE.Invalid:
                        binaryTypeEnum = type.Assembly == Converter.s_urtAssembly ?
                            BinaryTypeEnum.ObjectUrt :
                            BinaryTypeEnum.ObjectUser;
                        typeInformation = type.FullName;
                        break;
                    default:
                        binaryTypeEnum = BinaryTypeEnum.Primitive;
                        typeInformation = primitiveTypeEnum;
                        break;
                }
            }

            return binaryTypeEnum;
        }

        // Writes the type information on the wire
        internal static void WriteTypeInfo(BinaryTypeEnum binaryTypeEnum, object typeInformation, int assemId, BinaryFormatterWriter output)
        {
            switch (binaryTypeEnum)
            {
                case BinaryTypeEnum.Primitive:
                case BinaryTypeEnum.PrimitiveArray:
                    Debug.Assert(typeInformation != null, "[BinaryConverter.WriteTypeInfo]typeInformation!=null");
                    output.WriteByte((byte)((InternalPrimitiveTypeE)typeInformation));
                    break;
                case BinaryTypeEnum.String:
                case BinaryTypeEnum.Object:
                case BinaryTypeEnum.StringArray:
                case BinaryTypeEnum.ObjectArray:
                    break;
                case BinaryTypeEnum.ObjectUrt:
                    Debug.Assert(typeInformation != null, "[BinaryConverter.WriteTypeInfo]typeInformation!=null");
                    output.WriteString(typeInformation.ToString());
                    break;
                case BinaryTypeEnum.ObjectUser:
                    Debug.Assert(typeInformation != null, "[BinaryConverter.WriteTypeInfo]typeInformation!=null");
                    output.WriteString(typeInformation.ToString());
                    output.WriteInt32(assemId);
                    break;
                default:
                    throw new SerializationException(SR.Format(SR.Serialization_TypeWrite, binaryTypeEnum.ToString()));
            }
        }

        // Reads the type information from the wire
        internal static object ReadTypeInfo(BinaryTypeEnum binaryTypeEnum, BinaryParser input, out int assemId)
        {
            object var = null;
            int readAssemId = 0;

            switch (binaryTypeEnum)
            {
                case BinaryTypeEnum.Primitive:
                case BinaryTypeEnum.PrimitiveArray:
                    var = (InternalPrimitiveTypeE)input.ReadByte();
                    break;
                case BinaryTypeEnum.String:
                case BinaryTypeEnum.Object:
                case BinaryTypeEnum.StringArray:
                case BinaryTypeEnum.ObjectArray:
                    break;
                case BinaryTypeEnum.ObjectUrt:
                    var = input.ReadString();
                    break;
                case BinaryTypeEnum.ObjectUser:
                    var = input.ReadString();
                    readAssemId = input.ReadInt32();
                    break;
                default:
                    throw new SerializationException(SR.Format(SR.Serialization_TypeRead, binaryTypeEnum.ToString()));
            }
            assemId = readAssemId;
            return var;
        }

        // Given the wire type information, returns the actual type and additional information
        internal static void TypeFromInfo(BinaryTypeEnum binaryTypeEnum,
                                          object typeInformation,
                                          ObjectReader objectReader,
                                          BinaryAssemblyInfo assemblyInfo,
                                          out InternalPrimitiveTypeE primitiveTypeEnum,
                                          out string typeString,
                                          out Type type,
                                          out bool isVariant)
        {
            isVariant = false;
            primitiveTypeEnum = InternalPrimitiveTypeE.Invalid;
            typeString = null;
            type = null;

            switch (binaryTypeEnum)
            {
                case BinaryTypeEnum.Primitive:
                    primitiveTypeEnum = (InternalPrimitiveTypeE)typeInformation;
                    typeString = Converter.ToComType(primitiveTypeEnum);
                    type = Converter.ToType(primitiveTypeEnum);
                    break;
                case BinaryTypeEnum.String:
                    type = Converter.s_typeofString;
                    break;
                case BinaryTypeEnum.Object:
                    type = Converter.s_typeofObject;
                    isVariant = true;
                    break;
                case BinaryTypeEnum.ObjectArray:
                    type = Converter.s_typeofObjectArray;
                    break;
                case BinaryTypeEnum.StringArray:
                    type = Converter.s_typeofStringArray;
                    break;
                case BinaryTypeEnum.PrimitiveArray:
                    primitiveTypeEnum = (InternalPrimitiveTypeE)typeInformation;
                    type = Converter.ToArrayType(primitiveTypeEnum);
                    break;
                case BinaryTypeEnum.ObjectUser:
                case BinaryTypeEnum.ObjectUrt:
                    if (typeInformation != null)
                    {
                        typeString = typeInformation.ToString();
                        type = objectReader.GetType(assemblyInfo, typeString);
                        if (ReferenceEquals(type, Converter.s_typeofObject))
                        {
                            isVariant = true;
                        }
                    }
                    break;
                default:
                    throw new SerializationException(SR.Format(SR.Serialization_TypeRead, binaryTypeEnum.ToString()));
            }
        }
    }
}
