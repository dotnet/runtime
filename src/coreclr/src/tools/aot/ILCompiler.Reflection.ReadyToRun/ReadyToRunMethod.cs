// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using Internal.Runtime;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// This structure represents a single precode fixup cell decoded from the
    /// nibble-oriented per-method fixup blob. Each method entrypoint fixup
    /// represents an array of cells that must be fixed up before the method
    /// can start executing.
    /// </summary>
    public struct FixupCell
    {
        public int Index { get; set; }

        /// <summary>
        /// Zero-based index of the import table within the import tables section.
        /// </summary>
        public uint TableIndex;

        /// <summary>
        /// Zero-based offset of the entry in the import table; it must be a multiple
        /// of the target architecture pointer size.
        /// </summary>
        public uint CellOffset;

        /// <summary>
        /// Fixup cell signature (textual representation of the typesystem object).
        /// </summary>
        public string Signature;

        public FixupCell(int index, uint tableIndex, uint cellOffset, string signature)
        {
            Index = index;
            TableIndex = tableIndex;
            CellOffset = cellOffset;
            Signature = signature;
        }
    }

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
    /// A runtime function corresponds to a contiguous fragment of code that implements a method.
    /// </summary>
    public class RuntimeFunction
    {
        // based on <a href= "https://github.com/dotnet/runtime/blob/master/src/coreclr/src/pal/inc/pal.h" > src / pal / inc / pal.h </ a > _RUNTIME_FUNCTION
        private ReadyToRunReader _readyToRunReader;
        private EHInfo _ehInfo;
        private DebugInfo _debugInfo;

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

        /// <summary>
        /// The start offset of the runtime function with is non-zero for methods with multiple runtime functions
        /// </summary>
        public int CodeOffset { get; }

        /// <summary>
        /// The method that this runtime function belongs to
        /// </summary>
        public ReadyToRunMethod Method { get; }

        public BaseUnwindInfo UnwindInfo { get; }

        public EHInfo EHInfo
        {
            get
            {
                if (_ehInfo == null)
                {
                    _readyToRunReader.RuntimeFunctionToEHInfo.TryGetValue(StartAddress, out _ehInfo);
                }
                return _ehInfo;
            }
        }

        public DebugInfo DebugInfo
        {
            get
            {
                if (_debugInfo == null)
                {
                    int offset;
                    if (_readyToRunReader.RuntimeFunctionToDebugInfo.TryGetValue(Id, out offset))
                    {
                        this._debugInfo = new DebugInfo(this, offset);
                    }
                }
                return _debugInfo;
            }
        }

        internal ReadyToRunReader ReadyToRunReader
        {
            get
            {
                return _readyToRunReader;
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
            BaseGcInfo gcInfo)
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
        /// The name of the method
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The signature with format: namespace.class.methodName<S, T, ...>(S, T, ...)
        /// </summary>
        public string SignatureString { get; set; }

        public MethodSignature<string> Signature { get; }

        public ImmutableArray<string> LocalSignature { get; }

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
        public IReadOnlyList<RuntimeFunction> RuntimeFunctions
        {
            get
            {
                EnsureRuntimeFunctions();
                return _runtimeFunctions;
            }
        }

        private void EnsureRuntimeFunctions()
        {
            if (this._runtimeFunctions == null)
            {
                this._runtimeFunctions = new List<RuntimeFunction>();
                this.ParseRuntimeFunctions();
            }
        }

        /// <summary>
        /// The id of the entrypoint runtime function
        /// </summary>
        public int EntryPointRuntimeFunctionId { get; set; }

        public BaseGcInfo GcInfo { get; set; }

        private ReadyToRunReader _readyToRunReader;
        private List<FixupCell> _fixupCells;
        private int? _fixupOffset;
        private List<RuntimeFunction> _runtimeFunctions;

        public IReadOnlyList<FixupCell> Fixups
        {
            get
            {
                EnsureFixupCells();
                return _fixupCells;
            }
        }

        internal int RuntimeFunctionCount { get; set; }

        /// <summary>
        /// Extracts the method signature from the metadata by rid
        /// </summary>
        public ReadyToRunMethod(
            ReadyToRunReader readyToRunReader,
            PEReader peReader,
            MetadataReader metadataReader,
            EntityHandle methodHandle,
            int entryPointId,
            string owningType,
            string constrainedType,
            string[] instanceArgs,
            int? fixupOffset)
        {
            _readyToRunReader = readyToRunReader;
            _fixupOffset = fixupOffset;
            MethodHandle = methodHandle;
            EntryPointRuntimeFunctionId = entryPointId;

            MetadataReader = metadataReader;

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
                        if (methodDef.RelativeVirtualAddress != 0)
                        {
                            MethodBodyBlock mbb = peReader.GetMethodBody(methodDef.RelativeVirtualAddress);
                            if (!mbb.LocalSignature.IsNil)
                            {
                                StandaloneSignature ss = MetadataReader.GetStandaloneSignature(mbb.LocalSignature);
                                LocalSignature = ss.DecodeLocalSignature(typeProvider, genericContext);
                            }
                        }
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

        private void EnsureFixupCells()
        {
            if (_fixupCells != null)
            {
                return;
            }
            if (!_fixupOffset.HasValue)
            {
                return;
            }
            _fixupCells = new List<FixupCell>();
            NibbleReader reader = new NibbleReader(_readyToRunReader.Image, _fixupOffset.Value);

            // The following algorithm has been loosely ported from CoreCLR,
            // src\vm\ceeload.inl, BOOL Module::FixupDelayListAux
            uint curTableIndex = reader.ReadUInt();

            while (true)
            {
                uint fixupIndex = reader.ReadUInt(); // Accumulate the real rva from the delta encoded rva

                while (true)
                {
                    ReadyToRunImportSection importSection = _readyToRunReader.ImportSections[(int)curTableIndex];
                    ReadyToRunImportSection.ImportSectionEntry entry = importSection.Entries[(int)fixupIndex];
                    _fixupCells.Add(new FixupCell(_fixupCells.Count, curTableIndex, fixupIndex, entry.Signature));

                    uint delta = reader.ReadUInt();

                    // Delta of 0 means end of entries in this table
                    if (delta == 0)
                        break;

                    fixupIndex += delta;
                }

                uint tableIndex = reader.ReadUInt();

                if (tableIndex == 0)
                    break;

                curTableIndex = curTableIndex + tableIndex;

            } // Done with all entries in this table
        }

        /// <summary>
        /// Get the RVAs of the runtime functions for each method
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/zap/zapcode.cpp">ZapUnwindInfo::Save</a>
        /// </summary>
        private void ParseRuntimeFunctions()
        {
            int runtimeFunctionId = EntryPointRuntimeFunctionId;
            int runtimeFunctionSize = _readyToRunReader.CalculateRuntimeFunctionSize();
            int runtimeFunctionOffset = _readyToRunReader.PEReader.GetOffset(_readyToRunReader.ReadyToRunHeader.Sections[ReadyToRunSectionType.RuntimeFunctions].RelativeVirtualAddress);
            int curOffset = runtimeFunctionOffset + runtimeFunctionId * runtimeFunctionSize;
            BaseGcInfo gcInfo = null;
            int codeOffset = 0;
            for (int i = 0; i < RuntimeFunctionCount; i++)
            {
                int startRva = NativeReader.ReadInt32(_readyToRunReader.Image, ref curOffset);
                int endRva = -1;
                if (_readyToRunReader.Machine == Machine.Amd64)
                {
                    endRva = NativeReader.ReadInt32(_readyToRunReader.Image, ref curOffset);
                }
                int unwindRva = NativeReader.ReadInt32(_readyToRunReader.Image, ref curOffset);
                int unwindOffset = _readyToRunReader.PEReader.GetOffset(unwindRva);

                BaseUnwindInfo unwindInfo = null;
                if (_readyToRunReader.Machine == Machine.Amd64)
                {
                    unwindInfo = new Amd64.UnwindInfo(_readyToRunReader.Image, unwindOffset);
                    if (i == 0)
                    {
                        gcInfo = new Amd64.GcInfo(_readyToRunReader.Image, unwindOffset + unwindInfo.Size, _readyToRunReader.Machine, _readyToRunReader.ReadyToRunHeader.MajorVersion);
                    }
                }
                else if (_readyToRunReader.Machine == Machine.I386)
                {
                    unwindInfo = new x86.UnwindInfo(_readyToRunReader.Image, unwindOffset);
                    if (i == 0)
                    {
                        gcInfo = new x86.GcInfo(_readyToRunReader.Image, unwindOffset, _readyToRunReader.Machine, _readyToRunReader.ReadyToRunHeader.MajorVersion);
                    }
                }
                else if (_readyToRunReader.Machine == Machine.ArmThumb2)
                {
                    unwindInfo = new Arm.UnwindInfo(_readyToRunReader.Image, unwindOffset);
                    if (i == 0)
                    {
                        gcInfo = new Amd64.GcInfo(_readyToRunReader.Image, unwindOffset + unwindInfo.Size, _readyToRunReader.Machine, _readyToRunReader.ReadyToRunHeader.MajorVersion); // Arm and Arm64 use the same GcInfo format as x64
                    }
                }
                else if (_readyToRunReader.Machine == Machine.Arm64)
                {
                    unwindInfo = new Arm64.UnwindInfo(_readyToRunReader.Image, unwindOffset);
                    if (i == 0)
                    {
                        gcInfo = new Amd64.GcInfo(_readyToRunReader.Image, unwindOffset + unwindInfo.Size, _readyToRunReader.Machine, _readyToRunReader.ReadyToRunHeader.MajorVersion);
                    }
                }

                RuntimeFunction rtf = new RuntimeFunction(
                    _readyToRunReader,
                    runtimeFunctionId,
                    startRva,
                    endRva,
                    unwindRva,
                    codeOffset,
                    this,
                    unwindInfo,
                    gcInfo);

                _runtimeFunctions.Add(rtf);
                runtimeFunctionId++;
                codeOffset += rtf.Size;
            }
        }
    }
}
