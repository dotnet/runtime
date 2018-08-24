// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml.Serialization;

namespace R2RDump
{
    public abstract class BaseUnwindInfo
    {
        public int Size { get; set; }
    }

    public abstract class BaseGcTransition
    {
        [XmlAttribute("Index")]
        public int CodeOffset { get; set; }

        public BaseGcTransition() { }

        public BaseGcTransition(int codeOffset)
        {
            CodeOffset = codeOffset;
        }
    }

    public abstract class BaseGcInfo
    {
        public int Size { get; set; }
        public int Offset { get; set; }
        public int CodeLength { get; set; }
        [XmlIgnore]
        public Dictionary<int, List<BaseGcTransition>> Transitions { get; set; }
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/pal/inc/pal.h">src/pal/inc/pal.h</a> _RUNTIME_FUNCTION
    /// </summary>
    public class RuntimeFunction
    {
        /// <summary>
        /// The index of the runtime function
        /// </summary>
        [XmlAttribute("Index")]
        public int Id { get; set; }

        /// <summary>
        /// The relative virtual address to the start of the code block
        /// </summary>
        public int StartAddress { get; set; }

        /// <summary>
        /// The size of the code block in bytes
        /// </summary>
        /// /// <remarks>
        /// The EndAddress field in the runtime functions section is conditional on machine type
        /// Size is -1 for images without the EndAddress field
        /// </remarks>
        public int Size { get; set; }

        /// <summary>
        /// The relative virtual address to the unwind info
        /// </summary>
        public int UnwindRVA { get; set; }

        /// <summary>
        /// The start offset of the runtime function with is non-zero for methods with multiple runtime functions
        /// </summary>
        public int CodeOffset { get; set; }

        /// <summary>
        /// The method that this runtime function belongs to
        /// </summary>
        public R2RMethod Method { get; }

        public BaseUnwindInfo UnwindInfo { get; }

        public RuntimeFunction() { }

        public RuntimeFunction(int id, int startRva, int endRva, int unwindRva, int codeOffset, R2RMethod method, BaseUnwindInfo unwindInfo, BaseGcInfo gcInfo)
        {
            Id = id;
            StartAddress = startRva;
            UnwindRVA = unwindRva;
            Method = method;
            UnwindInfo = unwindInfo;
            if (endRva != -1)
            {
                Size = endRva - startRva;
            }
            else if (unwindInfo is x86.UnwindInfo)
            {
                Size = (int)((x86.UnwindInfo)unwindInfo).FunctionLength;
            }
            else if (unwindInfo is Arm.UnwindInfo)
            {
                Size = (int)((Arm.UnwindInfo)unwindInfo).FunctionLength;
            }
            else if (unwindInfo is Arm64.UnwindInfo)
            {
                Size = (int)((Arm64.UnwindInfo)unwindInfo).FunctionLength;
            }
            else if (gcInfo != null)
            {
                Size = gcInfo.CodeLength;
            }
            CodeOffset = codeOffset;
            method.GcInfo = gcInfo;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"StartAddress: 0x{StartAddress:X8}");
            if (Size == -1)
            {
                sb.AppendLine("Size: Unavailable");
            }
            else
            {
                sb.AppendLine($"Size: {Size} bytes");
            }
            sb.AppendLine($"UnwindRVA: 0x{UnwindRVA:X8}");

            return sb.ToString();
        }
    }

    public class R2RMethod
    {
        private const int _mdtMethodDef = 0x06000000;

        MetadataReader _mdReader;
        MethodDefinition _methodDef;

        /// <summary>
        /// An unique index for the method
        /// </summary>
        [XmlAttribute("Index")]
        public int Index { get; set; }

        /// <summary>
        /// The name of the method
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The signature with format: namespace.class.methodName<S, T, ...>(S, T, ...)
        /// </summary>
        public string SignatureString { get; set; }

        public bool IsGeneric { get; set; }

        public MethodSignature<string> Signature { get; }

        /// <summary>
        /// The type that the method belongs to
        /// </summary>
        public string DeclaringType { get; set; }

        /// <summary>
        /// The token of the method consisting of the table code (0x06) and row id
        /// </summary>
        public uint Token { get; set; }

        /// <summary>
        /// The row id of the method
        /// </summary>
        public uint Rid { get; set; }

        /// <summary>
        /// All the runtime functions of this method
        /// </summary>
        public IList<RuntimeFunction> RuntimeFunctions { get; }

        /// <summary>
        /// The id of the entrypoint runtime function
        /// </summary>
        public int EntryPointRuntimeFunctionId { get; set; }

        [XmlIgnore]
        public BaseGcInfo GcInfo { get; set; }

        public FixupCell[] Fixups { get; set; }

        /// <summary>
        /// Maps all the generic parameters to the type in the instance
        /// </summary>
        private Dictionary<string, string> _genericParamInstanceMap;

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

        public R2RMethod() { }

        /// <summary>
        /// Extracts the method signature from the metadata by rid
        /// </summary>
        public R2RMethod(int index, MetadataReader mdReader, uint rid, int entryPointId, GenericElementTypes[] instanceArgs, uint[] tok, FixupCell[] fixups)
        {
            Index = index;
            Token = _mdtMethodDef | rid;
            Rid = rid;
            EntryPointRuntimeFunctionId = entryPointId;

            _mdReader = mdReader;
            RuntimeFunctions = new List<RuntimeFunction>();

            // get the method signature from the MethodDefhandle
            MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle((int)rid);
            _methodDef = mdReader.GetMethodDefinition(methodDefHandle);
            Name = mdReader.GetString(_methodDef.Name);
            BlobReader signatureReader = mdReader.GetBlobReader(_methodDef.Signature);

            TypeDefinitionHandle declaringTypeHandle = _methodDef.GetDeclaringType();
            DeclaringType = R2RReader.GetTypeDefFullName(mdReader, declaringTypeHandle);

            SignatureHeader signatureHeader = signatureReader.ReadSignatureHeader();
            IsGeneric = signatureHeader.IsGeneric;
            GenericParameterHandleCollection genericParams = _methodDef.GetGenericParameters();
            _genericParamInstanceMap = new Dictionary<string, string>();
            
            int argCount = signatureReader.ReadCompressedInteger();
            if (IsGeneric)
            {
                argCount = signatureReader.ReadCompressedInteger();
            }

            Fixups = fixups;

            DisassemblingTypeProvider provider = new DisassemblingTypeProvider();
            if (IsGeneric && instanceArgs != null && tok != null)
            {
                InitGenericInstances(genericParams, instanceArgs, tok);
            }

            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(new string[0], _genericParamInstanceMap.Values.ToArray());
            Signature = _methodDef.DecodeSignature(provider, genericContext);

            SignatureString = GetSignature();
        }

        /// <summary>
        /// Initialize map of generic parameters names to the type in the instance
        /// </summary>
        private void InitGenericInstances(GenericParameterHandleCollection genericParams, GenericElementTypes[] instanceArgs, uint[] tok)
        {
            if (instanceArgs.Length != genericParams.Count || tok.Length != genericParams.Count)
            {
                throw new BadImageFormatException("Generic param indices out of bounds");
            }

            for (int i = 0; i < instanceArgs.Length; i++)
            {
                string key = _mdReader.GetString(_mdReader.GetGenericParameter(genericParams.ElementAt(i)).Name); // name of the generic param, eg. "T"
                string type = instanceArgs[i].ToString(); // type of the generic param instance
                if (instanceArgs[i] == GenericElementTypes.ValueType)
                {
                    var t = _mdReader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle((int)tok[i]));
                    type = _mdReader.GetString(t.Name); // name of the struct

                }
                _genericParamInstanceMap[key] = type;
            }
        }

        /// <summary>
        /// Returns a string with format DeclaringType.Name<GenericTypes,...>(ArgTypes,...)
        /// </summary>
        private string GetSignature()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat($"{DeclaringType}.{Name}");

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
                    sb.AppendFormat($"{instance}");
                    i++;
                }
                sb.Append(">");
            }

            sb.Append("(");
            for (int i = 0; i < Signature.ParameterTypes.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.AppendFormat($"{Signature.ParameterTypes[i]}");
            }
            sb.Append(")");

            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"{Signature.ReturnType} {SignatureString}");

            sb.AppendLine($"Token: 0x{Token:X8}");
            sb.AppendLine($"Rid: {Rid}");
            sb.AppendLine($"EntryPointRuntimeFunctionId: {EntryPointRuntimeFunctionId}");
            sb.AppendLine($"Number of RuntimeFunctions: {RuntimeFunctions.Count}");
            if (Fixups != null)
            {
                sb.AppendLine($"Number of fixups: {Fixups.Count()}");
                foreach (FixupCell cell in Fixups)
                {
                    sb.AppendLine($"    TableIndex {cell.TableIndex}, Offset {cell.CellOffset:X4}");
                }
            }

            return sb.ToString();
        }
    }
}
