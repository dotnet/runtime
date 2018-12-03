// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace R2RDump
{
    /// <summary>
    /// Represents the debug information for a single method in the ready-to-run image.
    /// See <a href="https://github.com/dotnet/coreclr/blob/master/src/inc/cordebuginfo.h">src\inc\cordebuginfo.h</a> for 
    /// the fundamental types this is based on.
    /// </summary>
    public class DebugInfo
    {
        private List<DebugInfoBoundsEntry> _boundsList = new List<DebugInfoBoundsEntry>();
        private List<NativeVarInfo> _variablesList = new List<NativeVarInfo>();
        private Machine _machine;

        public DebugInfo(byte[] image, int offset, Machine machine)
        {
            _machine = machine;

            // Get the id of the runtime function from the NativeArray
            uint lookback = 0;
            uint debugInfoOffset = NativeReader.DecodeUnsigned(image, (uint)offset, ref lookback);

            if (lookback != 0)
            {
                System.Diagnostics.Debug.Assert(0 < lookback && lookback < offset);
                debugInfoOffset = (uint)offset - lookback;
            }

            NibbleReader reader = new NibbleReader(image, (int)debugInfoOffset);
            uint boundsByteCount = reader.ReadUInt();
            uint variablesByteCount = reader.ReadUInt();
            int boundsOffset = reader.GetNextByteOffset();
            int variablesOffset = (int)(boundsOffset + boundsByteCount);

            if (boundsByteCount > 0)
            {
                ParseBounds(image, boundsOffset);
            }

            if (variablesByteCount > 0)
            {
                ParseNativeVarInfo(image, variablesOffset);
            }
        }

        public void WriteTo(TextWriter writer, DumpOptions dumpOptions)
        {
            if (_boundsList.Count > 0)
                writer.WriteLine("Debug Info");

            writer.WriteLine("\tBounds:");
            for (int i = 0; i < _boundsList.Count; ++i)
            {
                writer.Write('\t');
                if (!dumpOptions.Naked)
                {
                    writer.Write($"Native Offset: 0x{_boundsList[i].NativeOffset:X}, ");
                }
                writer.WriteLine($"IL Offset: 0x{_boundsList[i].ILOffset:X}, Source Types: {_boundsList[i].SourceTypes}");
            }

            writer.WriteLine("");

            if (_variablesList.Count > 0)
                writer.WriteLine("\tVariable Locations:");

            for (int i = 0; i < _variablesList.Count; ++i)
            {
                var varLoc = _variablesList[i];
                writer.WriteLine($"\tVariable Number: {varLoc.VariableNumber}");
                writer.WriteLine($"\tStart Offset: 0x{varLoc.StartOffset:X}");
                writer.WriteLine($"\tEnd Offset: 0x{varLoc.EndOffset:X}");
                writer.WriteLine($"\tLoc Type: {varLoc.VariableLocation.VarLocType}");

                switch (varLoc.VariableLocation.VarLocType)
                {
                    case VarLocType.VLT_REG:
                    case VarLocType.VLT_REG_FP:
                    case VarLocType.VLT_REG_BYREF:
                        writer.WriteLine($"\tRegister: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data1)}");
                        break;
                    case VarLocType.VLT_STK:
                    case VarLocType.VLT_STK_BYREF:
                        writer.WriteLine($"\tBase Register: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data1)}");
                        writer.WriteLine($"\tStack Offset: {varLoc.VariableLocation.Data2}");
                        break;
                    case VarLocType.VLT_REG_REG:
                        writer.WriteLine($"\tRegister 1: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data1)}");
                        writer.WriteLine($"\tRegister 2: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data2)}");
                        break;
                    case VarLocType.VLT_REG_STK:
                        writer.WriteLine($"\tRegister: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data1)}");
                        writer.WriteLine($"\tBase Register: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data2)}");
                        writer.WriteLine($"\tStack Offset: {varLoc.VariableLocation.Data3}");
                        break;
                    case VarLocType.VLT_STK_REG:
                        writer.WriteLine($"\tStack Offset: {varLoc.VariableLocation.Data1}");
                        writer.WriteLine($"\tBase Register: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data2)}");
                        writer.WriteLine($"\tRegister: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data3)}");                        
                        break;
                    case VarLocType.VLT_STK2:
                        writer.WriteLine($"\tBase Register: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data1)}");
                        writer.WriteLine($"\tStack Offset: {varLoc.VariableLocation.Data2}");
                        break;
                    case VarLocType.VLT_FPSTK:
                        writer.WriteLine($"\tOffset: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data1)}");
                        break;
                    case VarLocType.VLT_FIXED_VA:
                        writer.WriteLine($"\tOffset: {GetPlatformSpecificRegister(_machine, varLoc.VariableLocation.Data1)}");
                        break;
                    default:
                        throw new BadImageFormatException("Unexpected var loc type");
                }

                writer.WriteLine("");
            }
        }

        /// <summary>
        /// Convert a register number in debug info into a machine-specific register
        /// </summary>
        private static string GetPlatformSpecificRegister(Machine machine, int regnum)
        {
            switch (machine)
            {
                case Machine.I386:
                    return ((x86.Registers)regnum).ToString();
                case Machine.Amd64:
                    return ((Amd64.Registers)regnum).ToString();
                case Machine.Arm:
                    return ((Arm.Registers)regnum).ToString();
                case Machine.Arm64:
                    return ((Arm64.Registers)regnum).ToString();
                default:
                    throw new NotImplementedException($"No implementation for machine type {machine}.");
            }
        }

        private void ParseBounds(byte[] image, int offset)
        {
            // Bounds info contains (Native Offset, IL Offset, flags)
            // - Sorted by native offset (so use a delta encoding for that).
            // - IL offsets aren't sorted, but they should be close to each other (so a signed delta encoding)
            //   They may also include a sentinel value from MappingTypes.
            // - flags is 3 indepedent bits.
            NibbleReader reader = new NibbleReader(image, offset);
            uint boundsEntryCount = reader.ReadUInt();
            Debug.Assert(boundsEntryCount > 0);

            uint previousNativeOffset = 0;
            for (int i = 0; i < boundsEntryCount; ++i)
            {
                var entry = new DebugInfoBoundsEntry();
                previousNativeOffset += reader.ReadUInt();
                entry.NativeOffset = previousNativeOffset;
                entry.ILOffset = (uint)(reader.ReadUInt() + (int)MappingTypes.MaxMappingValue);
                entry.SourceTypes = (SourceTypes)reader.ReadUInt();
                _boundsList.Add(entry);
            }
        }

        private void ParseNativeVarInfo(byte[] image, int offset)
        {
            // Each Varinfo has a:
            // - native start +End offset. We can use a delta for the end offset. 
            // - Il variable number. These are usually small.
            // - VarLoc information. This is a tagged variant. 
            // The entries aren't sorted in any particular order.
            NibbleReader reader = new NibbleReader(image, offset);
            uint nativeVarCount = reader.ReadUInt();

            for (int i = 0; i < nativeVarCount; ++i)
            {
                var entry = new NativeVarInfo();
                entry.StartOffset = reader.ReadUInt();
                entry.EndOffset = entry.StartOffset + reader.ReadUInt();
                entry.VariableNumber = (uint)(reader.ReadUInt() + (int)ImplicitILArguments.Max);

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
