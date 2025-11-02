// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// Represents the debug information for a single method in the ready-to-run image.
    /// See <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/cordebuginfo.h">src\inc\cordebuginfo.h</a> for
    /// the fundamental types this is based on.
    /// </summary>
    public class DebugInfo
    {
        private readonly RuntimeFunction _runtimeFunction;
        private readonly int _offset;
        private List<DebugInfoBoundsEntry> _boundsList;
        private byte[] _boundsBytes;
        private List<NativeVarInfo> _variablesList;
        private Machine _machine;

        public DebugInfo(RuntimeFunction runtimeFunction, int offset)
        {
            this._runtimeFunction = runtimeFunction;
            this._offset = offset;
        }

        public List<DebugInfoBoundsEntry> BoundsList
        {
            get
            {
                EnsureInitialized();
                return _boundsList;
            }
        }

        public byte[] BoundsBytes
        {
            get
            {
                EnsureInitialized();
                return _boundsBytes;
            }
        }

        public List<NativeVarInfo> VariablesList
        {
            get
            {
                EnsureInitialized();
                return _variablesList;
            }
        }

        public Machine Machine
        {
            get
            {
                EnsureInitialized();
                return _machine;
            }
        }

        /// <summary>
        /// Convert a register number in debug info into a machine-specific register
        /// </summary>
        public static string GetPlatformSpecificRegister(Machine machine, int regnum)
        {
            switch (machine)
            {
                case Machine.I386:
                    return ((x86.Registers)regnum).ToString();
                case Machine.Amd64:
                    return ((Amd64.Registers)regnum).ToString();
                case Machine.Arm:
                case Machine.ArmThumb2:
                    return ((Arm.Registers)regnum).ToString();
                case Machine.Arm64:
                    return ((Arm64.Registers)regnum).ToString();
                case Machine.LoongArch64:
                    return ((LoongArch64.Registers)regnum).ToString();
                case Machine.RiscV64:
                    return ((RiscV64.Registers)regnum).ToString();
                default:
                    throw new NotImplementedException($"No implementation for machine type {machine}.");
            }
        }

        private void EnsureInitialized()
        {
            if (_boundsList != null)
            {
                return;
            }
            ReadyToRunReader _readyToRunReader = _runtimeFunction.ReadyToRunReader;
            int offset = _offset;
            _boundsList = new List<DebugInfoBoundsEntry>();
            _variablesList = new List<NativeVarInfo>();
            Machine machine = _readyToRunReader.Machine;
            NativeReader imageReader = _readyToRunReader.ImageReader;
            _machine = machine;

            // Get the id of the runtime function from the NativeArray
            uint lookback = 0;
            uint debugInfoOffset = imageReader.DecodeUnsigned((uint)offset, ref lookback);

            if (lookback != 0)
            {
                System.Diagnostics.Debug.Assert(0 < lookback && lookback < offset);
                debugInfoOffset = (uint)offset - lookback;
            }

            NibbleReader reader = new NibbleReader(imageReader, (int)debugInfoOffset);
            uint boundsByteCount = reader.ReadUInt();
            uint variablesByteCount = reader.ReadUInt();
            int boundsOffset = reader.GetNextByteOffset();
            int variablesOffset = (int)(boundsOffset + boundsByteCount);

            _boundsBytes = new byte[boundsByteCount];
            int boundsOffsetMutable = boundsOffset;
            imageReader.ReadSpanAt(ref boundsOffsetMutable, _boundsBytes.AsSpan());

            if (boundsByteCount > 0)
            {
                ParseBounds(imageReader, boundsOffset);
            }

            if (variablesByteCount > 0)
            {
                ParseNativeVarInfo(imageReader, variablesOffset);
            }
        }

        private void ParseBounds(NativeReader imageReader, int offset)
        {
            // Bounds info contains (Native Offset, IL Offset, flags)
            // - Sorted by native offset (so use a delta encoding for that).
            // - IL offsets aren't sorted
            //   They may also include a sentinel value from MappingTypes.
            // - flags is 3 independent bits.
            if (_runtimeFunction.ReadyToRunReader.ReadyToRunHeader.MajorVersion >= 16)
            {
                NibbleReader reader = new NibbleReader(imageReader, offset);
                uint boundsEntryCount = reader.ReadUInt();
                Debug.Assert(boundsEntryCount > 0);
                uint bitsForNativeDelta = reader.ReadUInt() + 1; // Number of bits needed for native deltas
                uint bitsForILOffsets = reader.ReadUInt() + 1; // Number of bits needed for IL offsets

                uint bitsPerEntry = bitsForNativeDelta + bitsForILOffsets + 2; // 2 bits for source type
                ulong bitsMeaningfulMask = (1UL << ((int)bitsPerEntry)) - 1;
                int offsetOfActualBoundsData = reader.GetNextByteOffset();

                uint bitsCollected = 0;
                ulong bitTemp = 0;
                uint curBoundsProcessed = 0;

                uint previousNativeOffset = 0;

                while (curBoundsProcessed < boundsEntryCount)
                {
                    bitTemp |= ((uint)imageReader[offsetOfActualBoundsData++]) << (int)bitsCollected;
                    bitsCollected += 8;
                    while (bitsCollected >= bitsPerEntry)
                    {
                        ulong mappingDataEncoded = bitsMeaningfulMask & bitTemp;
                        bitTemp >>= (int)bitsPerEntry;
                        bitsCollected -= bitsPerEntry;

                        var entry = new DebugInfoBoundsEntry();
                        switch (mappingDataEncoded & 0x3)
                        {
                            case 0:
                                entry.SourceTypes = SourceTypes.SourceTypeInvalid;
                                break;
                            case 1:
                                entry.SourceTypes = SourceTypes.CallInstruction;
                                break;
                            case 2:
                                entry.SourceTypes = SourceTypes.StackEmpty;
                                break;
                            case 3:
                                entry.SourceTypes = SourceTypes.StackEmpty | SourceTypes.CallInstruction;
                                break;
                        }
                        mappingDataEncoded >>= 2;
                        uint nativeOffsetDelta = (uint)(mappingDataEncoded & ((1UL << (int)bitsForNativeDelta) - 1));
                        previousNativeOffset += nativeOffsetDelta;
                        entry.NativeOffset = previousNativeOffset;

                        mappingDataEncoded >>= (int)bitsForNativeDelta;
                        entry.ILOffset = (uint)(mappingDataEncoded) + (uint)DebugInfoBoundsType.MaxMappingValue;

                        _boundsList.Add(entry);
                        curBoundsProcessed++;
                    }
                }
            }
            else
            {
                NibbleReader reader = new NibbleReader(imageReader, offset);
                uint boundsEntryCount = reader.ReadUInt();
                Debug.Assert(boundsEntryCount > 0);

                uint previousNativeOffset = 0;
                for (int i = 0; i < boundsEntryCount; ++i)
                {
                    var entry = new DebugInfoBoundsEntry();
                    previousNativeOffset += reader.ReadUInt();
                    entry.NativeOffset = previousNativeOffset;
                    entry.ILOffset = reader.ReadUInt() + (uint)DebugInfoBoundsType.MaxMappingValue;
                    entry.SourceTypes = (SourceTypes)reader.ReadUInt();
                    _boundsList.Add(entry);
                }
            }
        }

        private void ParseNativeVarInfo(NativeReader imageReader, int offset)
        {
            // Each Varinfo has a:
            // - native start +End offset. We can use a delta for the end offset.
            // - Il variable number. These are usually small.
            // - VarLoc information. This is a tagged variant.
            // The entries aren't sorted in any particular order.
            NibbleReader reader = new NibbleReader(imageReader, offset);
            uint nativeVarCount = reader.ReadUInt();

            for (int i = 0; i < nativeVarCount; ++i)
            {
                var entry = new NativeVarInfo();
                entry.StartOffset = reader.ReadUInt();
                entry.EndOffset = entry.StartOffset + reader.ReadUInt();
                entry.VariableNumber = (uint)(reader.ReadUInt() + (int)ImplicitILArguments.Max);
                entry.Variable = new Variable();
                // TODO: This is probably incomplete
                // This does not handle any implicit arguments or var args
                if (entry.VariableNumber < this._runtimeFunction.Method.Signature.ParameterTypes.Length)
                {
                    entry.Variable.Type = VariableType.Parameter;
                    entry.Variable.Index = (int)entry.VariableNumber;
                }
                else
                {
                    entry.Variable.Type = VariableType.Local;
                    entry.Variable.Index = (int)entry.VariableNumber - this._runtimeFunction.Method.Signature.ParameterTypes.Length;
                }

                var varLoc = new VarLoc();
                varLoc.VarLocType = (VarLocType)reader.ReadUInt();
                switch (varLoc.VarLocType)
                {
                    case VarLocType.VLT_REG:
                    case VarLocType.VLT_REG_FP:
                    case VarLocType.VLT_REG_BYREF:
                        varLoc.Data1 = (int)reader.ReadUInt();
                        break;
                    case VarLocType.VLT_STK:
                    case VarLocType.VLT_STK_BYREF:
                        varLoc.Data1 = (int)reader.ReadUInt();
                        varLoc.Data2 = ReadEncodedStackOffset(reader);
                        break;
                    case VarLocType.VLT_REG_REG:
                        varLoc.Data1 = (int)reader.ReadUInt();
                        varLoc.Data2 = (int)reader.ReadUInt();
                        break;
                    case VarLocType.VLT_REG_STK:
                        varLoc.Data1 = (int)reader.ReadUInt();
                        varLoc.Data2 = (int)reader.ReadUInt();
                        varLoc.Data3 = ReadEncodedStackOffset(reader);
                        break;
                    case VarLocType.VLT_STK_REG:
                        varLoc.Data1 = ReadEncodedStackOffset(reader);
                        varLoc.Data2 = (int)reader.ReadUInt();
                        varLoc.Data3 = (int)reader.ReadUInt();
                        break;
                    case VarLocType.VLT_STK2:
                        varLoc.Data1 = (int)reader.ReadUInt();
                        varLoc.Data2 = ReadEncodedStackOffset(reader);
                        break;
                    case VarLocType.VLT_FPSTK:
                        varLoc.Data1 = (int)reader.ReadUInt();
                        break;
                    case VarLocType.VLT_FIXED_VA:
                        varLoc.Data1 = (int)reader.ReadUInt();
                        break;
                    default:
                        throw new BadImageFormatException("Unexpected var loc type");
                }

                entry.VariableLocation = varLoc;
                _variablesList.Add(entry);
            }
        }

        private int ReadEncodedStackOffset(NibbleReader reader)
        {
            int offset = reader.ReadInt();
            if (_machine == Machine.I386)
            {
                offset *= 4; // sizeof(DWORD)
            }

            return offset;
        }
    }
}
