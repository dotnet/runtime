// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace R2RDump
{
    struct RuntimeFunction
    {
        /// <summary>
        /// The index of the runtime function
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The relative virtual address to the start of the code block
        /// </summary>
        public int StartAddress { get; }

        /// <summary>
        /// The size of the code block in bytes
        /// </summary>
        /// /// <remarks>
        /// The EndAddress field in the runtime functions section is conditional on machine type
        /// Size is -1 for images without the EndAddress field
        /// </remarks>
        public int Size { get; }

        /// <summary>
        /// The relative virtual address to the unwind info
        /// </summary>
        public int UnwindRVA { get; }

        public RuntimeFunction(int id, int startRva, int endRva, int unwindRva)
        {
            Id = id;
            StartAddress = startRva;
            Size = endRva - startRva;
            if (endRva == -1)
                Size = -1;
            UnwindRVA = unwindRva;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat($"Id: {Id}\n");
            sb.AppendFormat($"StartAddress: 0x{StartAddress:X8}\n");
            if (Size == -1)
            {
                sb.Append("Size: Unavailable\n");
            }
            else
            {
                sb.AppendFormat($"Size: {Size} bytes\n");
            }

            return sb.ToString();
        }
    }

    class R2RMethod
    {
        private const int _mdtMethodDef = 0x06000000;

        MetadataReader _mdReader;
        MethodDefinition _methodDef;

        /// <summary>
        /// The name of the method
        /// </summary>
        public string Name { get; }

        public bool IsGeneric { get; }

        /// <summary>
        /// The return type of the method
        /// </summary>
        public SignatureType ReturnType { get; }

        /// <summary>
        /// The argument types of the method
        /// </summary>
        public SignatureType[] ArgTypes { get; }

        /// <summary>
        /// The type that the method belongs to
        /// </summary>
        public string DeclaringType { get; }

        /// <summary>
        /// The token of the method consisting of the table code (0x06) and row id
        /// </summary>
        public uint Token { get; }

        /// <summary>
        /// All the runtime functions of this method
        /// </summary>
        public List<RuntimeFunction> RuntimeFunctions { get; }

        /// <summary>
        /// The id of the entrypoint runtime function
        /// </summary>
        public int EntryPointRuntimeFunctionId { get; }

        /// <summary>
        /// Maps all the generic parameters to the type in the instance
        /// </summary>
        Dictionary<string, GenericInstance> _genericParamInstanceMap;

        [Flags]
        public enum EncodeMethodSigFlags
        {
            NONE = 0x00,
            ENCODE_METHOD_SIG_UnboxingStub = 0x01,
            ENCODE_METHOD_SIG_InstantiatingStub = 0x02,
            ENCODE_METHOD_SIG_MethodInstantiation = 0x04,
            ENCODE_METHOD_SIG_SlotInsteadOfToken = 0x08,
            ENCODE_METHOD_SIG_MemberRefToken = 0x10,
            ENCODE_METHOD_SIG_Constrained = 0x20,
            ENCODE_METHOD_SIG_OwnerType = 0x40,
        };

        public enum GenericElementTypes
        {
            __Canon = 0x3e,
            Void = 0x01,
            Boolean = 0x02,
            Char = 0x03,
            Int8 = 0x04,
            UInt8 = 0x05,
            Int16 = 0x06,
            UInt16 = 0x07,
            Int32 = 0x08,
            UInt32 = 0x09,
            Int64 = 0x0a,
            UInt64 = 0x0b,
            Float = 0x0c,
            Double = 0x0d,
            String = 0x0e,
            ValueType = 0x11,
            Object = 0x1c,
            Array = 0x1d,
        };

        /// <summary>
        /// Extracts the method signature from the metadata by rid
        /// </summary>
        public R2RMethod(byte[] image, MetadataReader mdReader, uint rid, int entryPointId, GenericElementTypes[] instanceArgs, uint[] tok)
        {
            Token = _mdtMethodDef | rid;
            EntryPointRuntimeFunctionId = entryPointId;

            _mdReader = mdReader;
            RuntimeFunctions = new List<RuntimeFunction>();

            // get the method signature from the MethodDefhandle
            MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle((int)rid);
            _methodDef = mdReader.GetMethodDefinition(methodDefHandle);
            Name = mdReader.GetString(_methodDef.Name);
            BlobReader signatureReader = mdReader.GetBlobReader(_methodDef.Signature);

            TypeDefinitionHandle declaringTypeHandle = _methodDef.GetDeclaringType();
            TypeDefinition declaringTypeDef;
            while (!declaringTypeHandle.IsNil)
            {
                declaringTypeDef = mdReader.GetTypeDefinition(declaringTypeHandle);
                DeclaringType = mdReader.GetString(declaringTypeDef.Name) + "." + DeclaringType;
                declaringTypeHandle = declaringTypeDef.GetDeclaringType();
            }
            
            NamespaceDefinitionHandle namespaceHandle = declaringTypeDef.NamespaceDefinition;
            while (!namespaceHandle.IsNil)
            {
                NamespaceDefinition namespaceDef = mdReader.GetNamespaceDefinition(namespaceHandle);
                DeclaringType = mdReader.GetString(namespaceDef.Name) + "." + DeclaringType;
                namespaceHandle = namespaceDef.Parent;
            }

            SignatureHeader signatureHeader = signatureReader.ReadSignatureHeader();
            IsGeneric = signatureHeader.IsGeneric;
            GenericParameterHandleCollection genericParams = _methodDef.GetGenericParameters();
            _genericParamInstanceMap = new Dictionary<string, GenericInstance>();
            
            int argCount = signatureReader.ReadCompressedInteger();
            if (IsGeneric)
            {
                argCount = signatureReader.ReadCompressedInteger();
            }

            ReturnType = new SignatureType(ref signatureReader, mdReader, genericParams);
            ArgTypes = new SignatureType[argCount];
            for (int i = 0; i < argCount; i++)
            {
                ArgTypes[i] = new SignatureType(ref signatureReader, mdReader, genericParams);
            }

            if (IsGeneric && instanceArgs != null && tok != null)
            {
                InitGenericInstances(genericParams, instanceArgs, tok);
            }
        }

        private void InitGenericInstances(GenericParameterHandleCollection genericParams, GenericElementTypes[] instanceArgs, uint[] tok)
        {
            if (instanceArgs.Length != genericParams.Count || tok.Length != genericParams.Count)
            {
                throw new BadImageFormatException("Generic param indices out of bounds");
            }

            for (int i = 0; i < genericParams.Count; i++)
            {
                var key = _mdReader.GetString(_mdReader.GetGenericParameter(genericParams.ElementAt(i)).Name);

                if (instanceArgs[i] == GenericElementTypes.ValueType)
                {
                    string classname = _mdReader.GetString(_mdReader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle((int)tok[i])).Name);
                    _genericParamInstanceMap[key] = new GenericInstance(instanceArgs[i], classname);
                }
                else
                {
                    _genericParamInstanceMap[key] = new GenericInstance(instanceArgs[i], Enum.GetName(typeof(GenericElementTypes), instanceArgs[i]));
                }
            }

            if ((ReturnType.Flags & SignatureType.SignatureTypeFlags.GENERIC) != 0)
            {
                ReturnType.GenericInstance = _genericParamInstanceMap[ReturnType.TypeName];
            }

            for (int i = 0; i < ArgTypes.Length; i++)
            {
                if ((ArgTypes[i].Flags & SignatureType.SignatureTypeFlags.GENERIC) != 0)
                {
                    ArgTypes[i].GenericInstance = _genericParamInstanceMap[ArgTypes[i].TypeName];
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (Name != null)
            {
                sb.AppendFormat($"{ReturnType.ToString()} ");
                sb.AppendFormat($"{DeclaringType}{Name}");

                if (IsGeneric)
                {
                    sb.Append("<");
                    int i = 0;
                    foreach (var instance in _genericParamInstanceMap.Values)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        sb.AppendFormat($"{instance.TypeName}");
                        i++;
                    }
                    sb.Append(">");
                }

                sb.Append("(");
                for (int i = 0; i < ArgTypes.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.AppendFormat($"{ArgTypes[i].ToString()}");
                }
                sb.Append(")\n");
            }

            sb.AppendFormat($"Token: 0x{Token:X8}\n");
            sb.AppendFormat($"EntryPointRuntimeFunctionId: {EntryPointRuntimeFunctionId}\n");
            sb.AppendFormat($"Number of RuntimeFunctions: {RuntimeFunctions.Count}\n\n");

            foreach (RuntimeFunction runtimeFunction in RuntimeFunctions)
            {
                sb.AppendFormat($"{runtimeFunction}\n");
            }

            return sb.ToString();
        }
    }
}
