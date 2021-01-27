// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        public int Index { get; }

        /// <summary>
        /// Zero-based index of the import table within the import tables section.
        /// </summary>
        public uint TableIndex { get; }

        /// <summary>
        /// Zero-based offset of the entry in the import table; it must be a multiple
        /// of the target architecture pointer size.
        /// </summary>
        public uint CellOffset { get; }

        /// <summary>
        /// Fixup cell signature
        /// </summary>
        public ReadyToRunSignature Signature { get; }

        public FixupCell(int index, uint tableIndex, uint cellOffset, ReadyToRunSignature signature)
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
    /// <remarks>
    /// Based on <a href="https://github.com/dotnet/runtime/blob/master/src/coreclr/pal/inc/pal.h">src/pal/inc/pal.h</a> _RUNTIME_FUNCTION
    /// </remarks>
    public class RuntimeFunction
    {
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
        /// The relative virtual address to the end of the code block
        /// </summary>
        public int EndAddress { get;  }

        /// <summary>
        /// The size of the code block in bytes
        /// </summary>
        /// /// <remarks>
        /// The EndAddress field in the runtime functions section is conditional on machine type
        /// Size is -1 for images without the EndAddress field
        /// </remarks>
        public int Size
        {
            get
            {
                EnsureInitialized();
                return _size;
            }
        }

        private int _size = -1;

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
            BaseUnwindInfo unwindInfo)
        {
            _readyToRunReader = readyToRunReader;

            Id = id;
            StartAddress = startRva;
            EndAddress = endRva;
            UnwindRVA = unwindRva;
            Method = method;
            UnwindInfo = unwindInfo;
            CodeOffset = codeOffset;
        }

        private void EnsureInitialized()
        {
            if (_size < 0)
            {
                _size = GetSize();
            }
        }

        private int GetSize()
        {
            if (EndAddress != -1)
            {
                return EndAddress - StartAddress;
            }
            else if (UnwindInfo is x86.UnwindInfo x86Info)
            {
                return (int)x86Info.FunctionLength;
            }
            else if (UnwindInfo is Arm.UnwindInfo armInfo)
            {
                return (int)armInfo.FunctionLength;
            }
            else if (UnwindInfo is Arm64.UnwindInfo arm64Info)
            {
                return (int)arm64Info.FunctionLength;
            }
            else if (Method.GcInfo != null)
            {
                return Method.GcInfo.CodeLength; 
            }
            else
            {
                return -1;
            }
        }
    }

    public class PgoInfoKey : IEquatable<PgoInfoKey>
    {
        /// <summary>
        /// The type that the method belongs to
        /// </summary>
        public string DeclaringType { get; }

        public string Name { get; }

        /// <summary>
        /// The method metadata handle
        /// </summary>
        public EntityHandle MethodHandle { get; }

        public IAssemblyMetadata ComponentReader { get; }

        public MethodSignature<string> Signature { get; }

        /// <summary>
        /// The signature with format: namespace.class.methodName<S, T, ...>(S, T, ...)
        /// </summary>
        public string SignatureString { get; }

        public PgoInfoKey(IAssemblyMetadata componentReader, string owningType, EntityHandle methodHandle, string[] instanceArgs)
        {
            ComponentReader = componentReader;
            EntityHandle owningTypeHandle;
            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(typeParameters: Array.Empty<string>(), methodParameters: instanceArgs);
            DisassemblingTypeProvider typeProvider = new DisassemblingTypeProvider();
            MethodHandle = methodHandle;

            // get the method signature from the method handle
            switch (MethodHandle.Kind)
            {
                case HandleKind.MethodDefinition:
                    {
                        MethodDefinition methodDef = componentReader.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)MethodHandle);
                        Name = componentReader.MetadataReader.GetString(methodDef.Name);
                        Signature = methodDef.DecodeSignature<string, DisassemblingGenericContext>(typeProvider, genericContext);
                        owningTypeHandle = methodDef.GetDeclaringType();
                    }
                    break;

                case HandleKind.MemberReference:
                    {
                        MemberReference memberRef = componentReader.MetadataReader.GetMemberReference((MemberReferenceHandle)MethodHandle);
                        Name = componentReader.MetadataReader.GetString(memberRef.Name);
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
                DeclaringType = MetadataNameFormatter.FormatHandle(componentReader.MetadataReader, owningTypeHandle);
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

        public override string ToString() => SignatureString;
        public override int GetHashCode() => SignatureString.GetHashCode();
        public override bool Equals(object obj) => obj is PgoInfoKey other && other.Equals(this);

        // Equality check. This isn't precisely accurate, but it should be good enough
        public bool Equals(PgoInfoKey other)
        {
            return SignatureString.Equals(other.SignatureString);
        }

        public static PgoInfoKey FromReadyToRunMethod(ReadyToRunMethod method)
        {
            var key = new PgoInfoKey(method.ComponentReader, method.DeclaringType, method.MethodHandle, method.InstanceArgs);
            Debug.Assert(key.SignatureString == method.SignatureString);
            return key;
        }
    }

    public class PgoInfo
    {
        public PgoInfoKey Key { get; }
        public int Offset { get; }
        public int PgoFormatVersion { get; }
        public byte[] Image { get; }
        private ReadyToRunReader _r2rReader;

        public PgoInfo(PgoInfoKey key, ReadyToRunReader r2rReader, int pgoFormatVersion, byte[] image, int offset)
        {
            PgoFormatVersion = pgoFormatVersion;
            Key = key;
            Offset = offset;
            Image = image;
            _r2rReader = r2rReader;
        }

        PgoSchemaElem[] _pgoData;
        int _size;

        class PgoDataLoader : IPgoSchemaDataLoader<string>
        {
            ReadyToRunReader _r2rReader;
            SignatureFormattingOptions _formatOptions;

            public PgoDataLoader(ReadyToRunReader r2rReader, SignatureFormattingOptions formatOptions)
            {
                _formatOptions = formatOptions;
                _r2rReader = r2rReader;
            }

            string IPgoSchemaDataLoader<string>.TypeFromLong(long input)
            {
                int tableIndex = checked((int)(input & 0xF));
                int fixupIndex = checked((int)(input >> 4));
                if (tableIndex == 0xF)
                {
                    return $"Unknown type {fixupIndex}";
                }
                else
                {
                    return _r2rReader.ImportSections[tableIndex].Entries[fixupIndex].Signature.ToString(_formatOptions);
                }
            }
        }

        void EnsurePgoData()
        {
            if (_pgoData == null)
            {
                var compressedIntParser = new PgoProcessor.PgoEncodedCompressedIntParser(Image, Offset);

                SignatureFormattingOptions formattingOptions = new SignatureFormattingOptions();

                _pgoData = PgoProcessor.ParsePgoData<string>(new PgoDataLoader(_r2rReader, formattingOptions), compressedIntParser, true).ToArray();
                _size = compressedIntParser.Offset - Offset;
            }
        }

        public PgoSchemaElem[] PgoData
        {
            get
            {
                EnsurePgoData();
                return _pgoData;
            }
        }

        public int Size
        {
            get
            {
                EnsurePgoData();
                return _size;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (PgoSchemaElem elem in PgoData)
            {
                sb.AppendLine($"ILOffset: {elem.ILOffset} InstrumentationKind: {elem.InstrumentationKind} Other: {elem.Other} Count: {elem.Count}");
                if (elem.DataHeldInDataLong)
                {
                    sb.AppendLine(elem.DataLong.ToString());
                }
                else
                {
                    foreach (object o in elem.DataObject)
                    {
                        sb.AppendLine(o.ToString());
                    }
                }
            }

            return sb.ToString();
        }
    }

    public class ReadyToRunMethod
    {
        private const int _mdtMethodDef = 0x06000000;

        /// <summary>
        /// MSIL module containing the method.
        /// </summary>
        public IAssemblyMetadata ComponentReader { get; private set; }

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
        /// Total method size (sum of the sizes of all runtime functions).
        /// </summary>
        private int _size;

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

        public int Size
        {
            get
            {
                EnsureRuntimeFunctions();
                return _size;
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

        public int GcInfoRva { get; set; }

        public BaseGcInfo GcInfo
        {
            get
            {
                EnsureInitialized();
                return _gcInfo;
            }
        }

        private BaseGcInfo _gcInfo;

        public PgoInfo PgoInfo
        {
            get
            {
                EnsureInitialized();
                return _pgoInfo as PgoInfo;
            }
        }

        private object _pgoInfo;

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

        public string[] InstanceArgs { get; set; }

        public int RuntimeFunctionCount { get; set; }

        /// <summary>
        /// Extracts the method signature from the metadata by rid
        /// </summary>
        public ReadyToRunMethod(
            ReadyToRunReader readyToRunReader,
            IAssemblyMetadata componentReader,
            EntityHandle methodHandle,
            int entryPointId,
            string owningType,
            string constrainedType,
            string[] instanceArgs,
            int? fixupOffset)
        {
            InstanceArgs = (string[])instanceArgs?.Clone();
            _readyToRunReader = readyToRunReader;
            _fixupOffset = fixupOffset;
            MethodHandle = methodHandle;
            EntryPointRuntimeFunctionId = entryPointId;

            ComponentReader = componentReader;

            EntityHandle owningTypeHandle;
            GenericParameterHandleCollection genericParams = default(GenericParameterHandleCollection);

            DisassemblingGenericContext genericContext = new DisassemblingGenericContext(typeParameters: Array.Empty<string>(), methodParameters: instanceArgs);
            DisassemblingTypeProvider typeProvider = new DisassemblingTypeProvider();

            // get the method signature from the method handle
            switch (MethodHandle.Kind)
            {
                case HandleKind.MethodDefinition:
                    {
                        MethodDefinition methodDef = ComponentReader.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)MethodHandle);
                        if (methodDef.RelativeVirtualAddress != 0)
                        {
                            MethodBodyBlock mbb = ComponentReader.ImageReader.GetMethodBody(methodDef.RelativeVirtualAddress);
                            if (!mbb.LocalSignature.IsNil)
                            {
                                StandaloneSignature ss = ComponentReader.MetadataReader.GetStandaloneSignature(mbb.LocalSignature);
                                LocalSignature = ss.DecodeLocalSignature(typeProvider, genericContext);
                            }
                        }
                        Name = ComponentReader.MetadataReader.GetString(methodDef.Name);
                        Signature = methodDef.DecodeSignature<string, DisassemblingGenericContext>(typeProvider, genericContext);
                        owningTypeHandle = methodDef.GetDeclaringType();
                        genericParams = methodDef.GetGenericParameters();
                    }
                    break;

                case HandleKind.MemberReference:
                    {
                        MemberReference memberRef = ComponentReader.MetadataReader.GetMemberReference((MemberReferenceHandle)MethodHandle);
                        Name = ComponentReader.MetadataReader.GetString(memberRef.Name);
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
                DeclaringType = MetadataNameFormatter.FormatHandle(ComponentReader.MetadataReader, owningTypeHandle);
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

        private void EnsureInitialized()
        {
            if (_gcInfo == null && GcInfoRva != 0)
            {
                int gcInfoOffset = _readyToRunReader.CompositeReader.GetOffset(GcInfoRva);
                if (_readyToRunReader.Machine == Machine.I386)
                {
                    _gcInfo = new x86.GcInfo(_readyToRunReader.Image, gcInfoOffset, _readyToRunReader.Machine, _readyToRunReader.ReadyToRunHeader.MajorVersion);
                }
                else
                {
                    // Arm and Arm64 use the same GcInfo format as Amd64
                    _gcInfo = new Amd64.GcInfo(_readyToRunReader.Image, gcInfoOffset, _readyToRunReader.Machine, _readyToRunReader.ReadyToRunHeader.MajorVersion);
                }
            }
            if (_pgoInfo == null)
            {
                _pgoInfo = _readyToRunReader.GetPgoInfoByKey(PgoInfoKey.FromReadyToRunMethod(this));
                if (_pgoInfo == null)
                {
                    _pgoInfo = new object();
                }
            }
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
            int runtimeFunctionOffset = _readyToRunReader.CompositeReader.GetOffset(_readyToRunReader.ReadyToRunHeader.Sections[ReadyToRunSectionType.RuntimeFunctions].RelativeVirtualAddress);
            int curOffset = runtimeFunctionOffset + runtimeFunctionId * runtimeFunctionSize;
            int codeOffset = 0;

            for (int i = 0; i < RuntimeFunctionCount; i++)
            {
                int startRva = NativeReader.ReadInt32(_readyToRunReader.Image, ref curOffset);
                if (_readyToRunReader.Machine == Machine.ArmThumb2)
                {
                    // The low bit of this address is set since the function contains thumb code.
                    // Clear this bit in order to get the "real" RVA of the start of the function.
                    startRva = (int)(startRva & ~1);
                }
                int endRva = -1;
                if (_readyToRunReader.Machine == Machine.Amd64)
                {
                    endRva = NativeReader.ReadInt32(_readyToRunReader.Image, ref curOffset);
                }
                int unwindRva = NativeReader.ReadInt32(_readyToRunReader.Image, ref curOffset);
                int unwindOffset = _readyToRunReader.CompositeReader.GetOffset(unwindRva);

                BaseUnwindInfo unwindInfo = null;
                if (_readyToRunReader.Machine == Machine.I386)
                {
                    unwindInfo = new x86.UnwindInfo(_readyToRunReader.Image, unwindOffset);
                }
                else if (_readyToRunReader.Machine == Machine.Amd64)
                {
                    unwindInfo = new Amd64.UnwindInfo(_readyToRunReader.Image, unwindOffset);
                }
                else if (_readyToRunReader.Machine == Machine.ArmThumb2)
                {
                    unwindInfo = new Arm.UnwindInfo(_readyToRunReader.Image, unwindOffset);
                }
                else if (_readyToRunReader.Machine == Machine.Arm64)
                {
                    unwindInfo = new Arm64.UnwindInfo(_readyToRunReader.Image, unwindOffset);
                }

                if (i == 0 && unwindInfo != null)
                {
                    if (_readyToRunReader.Machine == Machine.I386)
                    {
                        GcInfoRva = unwindRva;
                    }
                    else
                    {
                        GcInfoRva = unwindRva + unwindInfo.Size;
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
                    unwindInfo);

                _runtimeFunctions.Add(rtf);
                runtimeFunctionId++;
                codeOffset += rtf.Size;
            }

            _size = codeOffset;
        }
    }
}
