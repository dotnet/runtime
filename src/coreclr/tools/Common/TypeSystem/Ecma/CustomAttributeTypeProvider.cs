// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Ecma
{
    public struct CustomAttributeTypeProvider : ICustomAttributeTypeProvider<TypeDesc>
    {
        private EcmaModule _module;
        private bool _throwIfTypeNotFound;

        public CustomAttributeTypeProvider(EcmaModule module)
            : this(module, throwIfTypeNotFound: true)
        {
        }

        public CustomAttributeTypeProvider(EcmaModule module, bool throwIfTypeNotFound)
        {
            _module = module;
            _throwIfTypeNotFound = throwIfTypeNotFound;
        }

        public TypeDesc GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return PrimitiveTypeProvider.GetPrimitiveType(_module.Context, typeCode);
        }

        public TypeDesc GetSystemType()
        {
            MetadataType systemType = _module.Context.SystemModule.GetType("System"u8, "Type"u8);
            return systemType;
        }

        public TypeDesc GetSZArrayType(TypeDesc elementType)
        {
            return elementType?.MakeArrayType();
        }

        public TypeDesc GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            Debug.Assert(reader == _module.MetadataReader);
            return _module.GetType(handle);
        }

        public TypeDesc GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            Debug.Assert(reader == _module.MetadataReader);
            return _module.GetType(handle);
        }

        public TypeDesc GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            Debug.Assert(reader == _module.MetadataReader);
            return _module.GetType(handle);
        }

        public TypeDesc GetTypeFromSerializedName(string name)
        {
            if (name == null)
                return null;

            return _module.GetTypeByCustomAttributeTypeName(name, _throwIfTypeNotFound);
        }

        public PrimitiveTypeCode GetUnderlyingEnumType(TypeDesc type)
        {
            if (type is null)
                throw new BadImageFormatException("The underlying type of an unresolved enum is unknown.");

            switch (type.UnderlyingType.Category)
            {
                case TypeFlags.Byte:
                    return PrimitiveTypeCode.Byte;
                case TypeFlags.SByte:
                    return PrimitiveTypeCode.SByte;
                case TypeFlags.UInt16:
                    return PrimitiveTypeCode.UInt16;
                case TypeFlags.Int16:
                    return PrimitiveTypeCode.Int16;
                case TypeFlags.UInt32:
                    return PrimitiveTypeCode.UInt32;
                case TypeFlags.Int32:
                    return PrimitiveTypeCode.Int32;
                case TypeFlags.UInt64:
                    return PrimitiveTypeCode.UInt64;
                case TypeFlags.Int64:
                    return PrimitiveTypeCode.Int64;
                default:
                    throw new BadImageFormatException();
            }
        }

        public bool IsSystemType(TypeDesc type)
        {
            var metadataType = type as MetadataType;
            return metadataType != null
                && metadataType.Name == "Type"u8
                && metadataType.Module == _module.Context.SystemModule
                && metadataType.Namespace == "System"u8;
        }
    }
}
