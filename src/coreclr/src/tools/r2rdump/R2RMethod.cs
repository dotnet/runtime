﻿// Licensed to the .NET Foundation under one or more agreements.
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

        public EHInfo EHInfo { get; }

        public DebugInfo DebugInfo { get; }

        public RuntimeFunction() { }

        public RuntimeFunction(
            int id, 
            int startRva, 
            int endRva, 
            int unwindRva, 
            int codeOffset, 
            R2RMethod method, 
            BaseUnwindInfo unwindInfo, 
            BaseGcInfo gcInfo, 
            EHInfo ehInfo,
            DebugInfo debugInfo)
        {
            Id = id;
            StartAddress = startRva;
            UnwindRVA = unwindRva;
            Method = method;
            UnwindInfo = unwindInfo;
            DebugInfo = debugInfo;

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
            else
            {
                Size = -1;
            }
            CodeOffset = codeOffset;
            method.GcInfo = gcInfo;
            EHInfo = ehInfo;
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
            if (UnwindInfo is Amd64.UnwindInfo amd64UnwindInfo)
            {
                string parsedFlags = "";
                if ((amd64UnwindInfo.Flags & (int)Amd64.UnwindFlags.UNW_FLAG_EHANDLER) != 0)
                {
                    parsedFlags += " EHANDLER";
                }
                if ((amd64UnwindInfo.Flags & (int)Amd64.UnwindFlags.UNW_FLAG_UHANDLER) != 0)
                {
                    parsedFlags += " UHANDLER";
                }
                if ((amd64UnwindInfo.Flags & (int)Amd64.UnwindFlags.UNW_FLAG_CHAININFO) != 0)
                {
                    parsedFlags += " CHAININFO";
                }
                if (parsedFlags.Length == 0)
                {
                    parsedFlags = " NHANDLER";
                }
                sb.AppendLine($"Version:            {amd64UnwindInfo.Version}");
                sb.AppendLine($"Flags:              0x{amd64UnwindInfo.Flags:X2}{parsedFlags}");
                sb.AppendLine($"SizeOfProlog:       0x{amd64UnwindInfo.SizeOfProlog:X4}");
                sb.AppendLine($"CountOfUnwindCodes: {amd64UnwindInfo.CountOfUnwindCodes}");
                sb.AppendLine($"FrameRegister:      {amd64UnwindInfo.FrameRegister}");
                sb.AppendLine($"FrameOffset:        0x{amd64UnwindInfo.FrameOffset}");
                sb.AppendLine($"PersonalityRVA:     0x{amd64UnwindInfo.PersonalityRoutineRVA:X4}");

                for (int unwindCodeIndex = 0; unwindCodeIndex < amd64UnwindInfo.CountOfUnwindCodes; unwindCodeIndex++)
                {
                    Amd64.UnwindCode unwindCode = amd64UnwindInfo.UnwindCodeArray[unwindCodeIndex];
                    sb.Append($"UnwindCode[{unwindCode.Index}]: ");
                    sb.Append($"CodeOffset 0x{unwindCode.CodeOffset:X4} ");
                    sb.Append($"FrameOffset 0x{unwindCode.FrameOffset:X4} ");
                    sb.Append($"NextOffset 0x{unwindCode.NextFrameOffset} ");
                    sb.Append($"Op {unwindCode.OpInfoStr}");
                    sb.AppendLine();
                }
            }
            sb.AppendLine();

            if (EHInfo != null)
            {
                sb.AppendLine($@"EH info @ {EHInfo.EHInfoRVA:X4}, #clauses = {EHInfo.EHClauses.Length}");
                EHInfo.WriteTo(sb);
                sb.AppendLine();
            }

            if (DebugInfo != null)
            {
                sb.AppendLine(DebugInfo.ToString());
            }

            return sb.ToString();
        }
    }

    public class R2RMethod
    {
        private const int _mdtMethodDef = 0x06000000;

        /// <summary>
        /// R2R reader representing the method module.
        /// </summary>
        public R2RReader R2RReader { get; }

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

        public MethodSignature<string> Signature { get; }

        /// <summary>
        /// The type that the method belongs to
        /// </summary>
        public string DeclaringType { get; set; }

        /// <summary>
        /// The method metadata handle
        /// </summary>
        public EntityHandle MethodHandle { get; set; }

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

        public R2RMethod() { }

        /// <summary>
        /// Extracts the method signature from the metadata by rid
        /// </summary>
        public R2RMethod(
            int index, 
            R2RReader r2rReader, 
            EntityHandle methodHandle, 
            int entryPointId, 
            string owningType, 
            string constrainedType, 
            string[] instanceArgs,
            FixupCell[] fixups)
        {
            Index = index;
            MethodHandle = methodHandle;
            EntryPointRuntimeFunctionId = entryPointId;

            R2RReader = r2rReader;
            RuntimeFunctions = new List<RuntimeFunction>();

            EntityHandle owningTypeHandle;
            GenericParameterHandleCollection genericParams = default(GenericParameterHandleCollection);

            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(typeParameters: Array.Empty<string>(), methodParameters: instanceArgs);
            DisassemblingTypeProvider typeProvider = new DisassemblingTypeProvider();

            // get the method signature from the method handle
            switch (MethodHandle.Kind)
            {
                case HandleKind.MethodDefinition:
                    {
                        MethodDefinition methodDef = R2RReader.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)MethodHandle);
                        Name = R2RReader.MetadataReader.GetString(methodDef.Name);
                        Signature = methodDef.DecodeSignature<string, DisassemblingGenericContext>(typeProvider, genericContext);
                        owningTypeHandle = methodDef.GetDeclaringType();
                        genericParams = methodDef.GetGenericParameters();
                    }
                    break;

                case HandleKind.MemberReference:
                    {
                        MemberReference memberRef = R2RReader.MetadataReader.GetMemberReference((MemberReferenceHandle)MethodHandle);
                        Name = R2RReader.MetadataReader.GetString(memberRef.Name);
                        Signature = memberRef.DecodeMethodSignature<string, DisassemblingGenericContext>(typeProvider, genericContext);
                        owningTypeHandle = memberRef.Parent;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (owningType != null)
            {
                DeclaringType = owningType;
            }
            else
            {
                DeclaringType = MetadataNameFormatter.FormatHandle(R2RReader.MetadataReader, owningTypeHandle);
            }

            Fixups = fixups;

            StringBuilder sb = new StringBuilder();
            sb.Append(Signature.ReturnType);
            sb.Append(" ");
            sb.Append(DeclaringType);
            sb.Append(".");
            sb.Append(Name);

            if (Signature.GenericParameterCount != 0)
            {
                sb.Append("<");
                for (int i = 0; i < Signature.GenericParameterCount; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    if (instanceArgs != null && instanceArgs.Length > i)
                    {
                        sb.Append(instanceArgs[i]);
                    }
                    else
                    {
                        sb.Append("!");
                        sb.Append(i);
                    }
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

            SignatureString = sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(SignatureString);

            sb.AppendLine($"Handle: 0x{MetadataTokens.GetToken(R2RReader.MetadataReader, MethodHandle):X8}");
            sb.AppendLine($"Rid: {MetadataTokens.GetRowNumber(R2RReader.MetadataReader, MethodHandle)}");
            sb.AppendLine($"EntryPointRuntimeFunctionId: {EntryPointRuntimeFunctionId}");
            sb.AppendLine($"Number of RuntimeFunctions: {RuntimeFunctions.Count}");
            if (Fixups != null)
            {
                sb.AppendLine($"Number of fixups: {Fixups.Count()}");
                foreach (FixupCell cell in Fixups)
                {
                    R2RImportSection importSection = R2RReader.ImportSections[(int)cell.TableIndex];
                    R2RImportSection.ImportSectionEntry entry = importSection.Entries[(int)cell.CellOffset];
                    sb.AppendLine($"    TableIndex {cell.TableIndex}, Offset {cell.CellOffset:X4}: {entry.Signature}");
                }
            }

            return sb.ToString();
        }
    }
}
