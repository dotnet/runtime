// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Metadata;
using System.Text;

namespace R2RDump
{
    class SignatureType
    {
        /// <summary>
        /// Indicates if the type is an array, reference, or generic
        /// </summary>
        public SignatureTypeFlags Flags { get; }

        /// <summary>
        /// Name of the object or primitive type, the placeholder type for generic methods
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// The type that the generic method was instantiated to
        /// </summary>
        public GenericInstance GenericInstance { get; set; }

        [Flags]
        public enum SignatureTypeFlags
        {
            NONE = 0x00,
            ARRAY = 0x01,
            REFERENCE = 0x02,
            GENERIC = 0x04,
        };

        public SignatureType(ref BlobReader signatureReader, MetadataReader mdReader, GenericParameterHandleCollection genericParams)
        {
            SignatureTypeCode signatureTypeCode = signatureReader.ReadSignatureTypeCode();
            Flags = 0;
            if (signatureTypeCode == SignatureTypeCode.SZArray)
            {
                Flags |= SignatureTypeFlags.ARRAY;
                signatureTypeCode = signatureReader.ReadSignatureTypeCode();
            }

            TypeName = signatureTypeCode.ToString();
            if (signatureTypeCode == SignatureTypeCode.TypeHandle || signatureTypeCode == SignatureTypeCode.ByReference)
            {
                if (signatureTypeCode == SignatureTypeCode.ByReference)
                {
                    Flags |= SignatureTypeFlags.REFERENCE;
                }

                EntityHandle handle = signatureReader.ReadTypeHandle();
                if (handle.Kind == HandleKind.TypeDefinition)
                {
                    TypeDefinition typeDef = mdReader.GetTypeDefinition((TypeDefinitionHandle)handle);
                    TypeName = mdReader.GetString(typeDef.Name);
                }
                else if (handle.Kind == HandleKind.TypeReference)
                {
                    TypeReference typeRef = mdReader.GetTypeReference((TypeReferenceHandle)handle);
                    TypeName = mdReader.GetString(typeRef.Name);
                }
            }
            else if (signatureTypeCode == SignatureTypeCode.GenericMethodParameter)
            {
                int index = signatureReader.ReadCompressedInteger();
                GenericParameter generic = mdReader.GetGenericParameter(genericParams[index]);
                TypeName = mdReader.GetString(generic.Name);
                Flags |= SignatureTypeFlags.GENERIC;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if ((Flags & SignatureTypeFlags.REFERENCE) != 0)
            {
                sb.Append("ref ");
            }

            if ((Flags & SignatureTypeFlags.GENERIC) != 0)
            {
                sb.AppendFormat($"{GenericInstance.TypeName}");
            }
            else
            {
                sb.AppendFormat($"{TypeName}");
            }
            if ((Flags & SignatureTypeFlags.ARRAY) != 0)
            {
                sb.Append("[]");
            }
            return sb.ToString();
        }
    }

    struct GenericInstance
    {
        /// <summary>
        /// The type of the instance for generic a type
        /// </summary>
        public R2RMethod.GenericElementTypes Instance { get; }

        /// <summary>
        /// The type name of the instance for generic a type. Different from GenericInstance.Instance for structs (ValueType)
        /// </summary>
        public string TypeName { get; }

        public GenericInstance(R2RMethod.GenericElementTypes instance, string name)
        {
            Instance = instance;
            TypeName = name;
        }
    }
}
