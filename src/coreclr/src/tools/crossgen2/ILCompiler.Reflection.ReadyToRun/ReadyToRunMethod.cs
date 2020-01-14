// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    public abstract class BaseUnwindInfo
    {
        public int Size { get; set; }
    }

    public abstract class BaseGcTransition
    {
        public int CodeOffset { get; set; }

        public BaseGcTransition() { }

        public BaseGcTransition(int codeOffset)
        {
            CodeOffset = codeOffset;
        }
    }

    public abstract class BaseGcSlot
    {
        public abstract GcSlotFlags WriteTo(StringBuilder sb, Machine machine, GcSlotFlags prevFlags);
    }

    public abstract class BaseGcInfo
    {
        public int Size { get; set; }
        public int Offset { get; set; }
        public int CodeLength { get; set; }
        public Dictionary<int, List<BaseGcTransition>> Transitions { get; set; }
        public List<List<BaseGcSlot>> LiveSlotsAtSafepoints { get; set; }
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/pal/inc/pal.h">src/pal/inc/pal.h</a> _RUNTIME_FUNCTION
    /// </summary>
    public class RuntimeFunction
    {
        private ReadyToRunReader _readyToRunReader;
        private DebugInfo _debugInfo;

        /// <summary>
        /// The index of the runtime function
        /// </summary>
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
        public ReadyToRunMethod Method { get; }

        public BaseUnwindInfo UnwindInfo { get; }

        public EHInfo EHInfo { get; }

        public DebugInfo DebugInfo
        {
            get
            {
                if (_debugInfo == null)
                {
                    _readyToRunReader.RuntimeFunctionToDebugInfo.TryGetValue(Id, out _debugInfo);
                }
                return _debugInfo;
            }
        }

        public RuntimeFunction(
            ReadyToRunReader readyToRunReader,
            int id,
            int startRva,
            int endRva,
            int unwindRva,
            int codeOffset,
            ReadyToRunMethod method,
            BaseUnwindInfo unwindInfo,
            BaseGcInfo gcInfo,
            EHInfo ehInfo)
        {
            _readyToRunReader = readyToRunReader;
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
            else
            {
                Size = -1;
            }
            CodeOffset = codeOffset;
            method.GcInfo = gcInfo;
            EHInfo = ehInfo;
        }
    }

    public class ReadyToRunMethod
    {
        private const int _mdtMethodDef = 0x06000000;

        /// <summary>
        /// MetadataReader representing the method module.
        /// </summary>
        public MetadataReader MetadataReader { get; private set; }

        /// <summary>
        /// An unique index for the method
        /// </summary>
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

        public BaseGcInfo GcInfo { get; set; }

        public FixupCell[] Fixups { get; set; }

        /// <summary>
        /// Extracts the method signature from the metadata by rid
        /// </summary>
        public ReadyToRunMethod(
            int index,
            MetadataReader metadataReader,
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

            MetadataReader = metadataReader;
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
                        MethodDefinition methodDef = MetadataReader.GetMethodDefinition((MethodDefinitionHandle)MethodHandle);
                        Name = MetadataReader.GetString(methodDef.Name);
                        Signature = methodDef.DecodeSignature<string, DisassemblingGenericContext>(typeProvider, genericContext);
                        owningTypeHandle = methodDef.GetDeclaringType();
                        genericParams = methodDef.GetGenericParameters();
                    }
                    break;

                case HandleKind.MemberReference:
                    {
                        MemberReference memberRef = MetadataReader.GetMemberReference((MemberReferenceHandle)MethodHandle);
                        Name = MetadataReader.GetString(memberRef.Name);
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
                DeclaringType = MetadataNameFormatter.FormatHandle(MetadataReader, owningTypeHandle);
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
    }
}
